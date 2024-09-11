// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Immutable;
using Aspire.Dashboard.Model;
using System.Text.Json;

namespace Aspire.Dashboard.Extensibility;

public interface IExtensionRegistry
{
    /// <summary>
    /// Gets data for a top-level page provided by an extension. This data is looked up via the
    /// unique URL "slug" included that identifies the extension.
    /// </summary>
    /// <remarks>
    /// For example, navigating to https://localhost:1234/extension/my-extension means the user
    /// is requesting the top-level page with slug <c>my-extension</c>.
    /// </remarks>
    /// <param name="urlSlug">
    /// A string that's included in a URL that uniquely identifies the top-level page, and implies a specific extension.
    /// </param>
    /// <param name="cancellationToken">A token that signals a loss of interest in the operation.</param>
    /// <returns></returns>
    Task<TopLevelPageData?> GetTopLevelPageConfigurationAsync(string urlSlug, CancellationToken cancellationToken);

    /// <summary>
    /// Returns configuration data for each top-level page, in priority (display) order.
    /// </summary>
    /// <param name="callback">
    /// A callback via which data updates are provided. If extensions are known when this method is called,
    /// then <paramref name="callback"/> will be invoked immediately (even if the set of pages is empty).
    /// </param>
    /// <returns>An object that, when disposed, cancels the subscription.</returns>
    IDisposable SubscribeToTopLevelPageConfiguration(Action<ImmutableArray<TopLevelPageConfiguration>> callback);
}

public record class TopLevelPageData(TopLevelPageConfiguration Configuration, string ExtensionBaseUrl);

/// <summary>
/// Tracks dashboard extensions provided via the resource service, and gathers their configuration for use within the dashboard.
/// </summary>
/// <remarks>
/// Uses <see cref="IDashboardClient"/> to subscribe to resources. When dashboard extensions
/// become available, this component will connect to them and retrieve their configuration.
/// </remarks>
internal sealed class ExtensionRegistry : IExtensionRegistry, IAsyncDisposable
{
    private readonly Dictionary<string, Instance> _instanceByResourceName = new(StringComparers.ResourceName);
    private readonly TaskCompletionSource _extensionsAvailable = new();
    private readonly CancellationTokenSource _cts = new();

    private readonly IDashboardClient _dashboardClient;
    private readonly ILogger<ExtensionRegistry> _logger;

    private ImmutableDictionary<string, ExtensionConfiguration> _extensionConfigByResourceName = ImmutableDictionary<string, ExtensionConfiguration>.Empty.WithComparers(StringComparers.ResourceName);
    private ImmutableDictionary<string, TopLevelPageData> _pageByUrlSlug = ImmutableDictionary<string, TopLevelPageData>.Empty.WithComparers(StringComparers.TopLevelPageUrlSlug);
    private ImmutableArray<TopLevelPageConfiguration> _pages;
    private ImmutableHashSet<Action<ImmutableArray<TopLevelPageConfiguration>>> _topLevelPageConfigurationSubscriptions = [];

    public ExtensionRegistry(IDashboardClient dashboardClient, ILoggerFactory loggerFactory)
    {
        _dashboardClient = dashboardClient;
        _logger = loggerFactory.CreateLogger<ExtensionRegistry>();

        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await SubscribeToResourcesAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _extensionsAvailable.TrySetCanceled();
            }
            catch (Exception ex)
            {
                _extensionsAvailable.TrySetException(ex);
            }
        });

        async Task SubscribeToResourcesAsync()
        {
            _logger.LogDebug("Subscribing for resource data...");

            var (snapshot, subscription) = await dashboardClient.SubscribeResourcesAsync(token).ConfigureAwait(false);

            _logger.LogDebug("Received {Length} resources in initial snapshot", snapshot.Length);

            // Integrate initial state
            foreach (var resource in snapshot)
            {
                await IntegrateAsync(resource).ConfigureAwait(false);
            }

            _logger.LogDebug("Initial data integrated. Listening for updates.");

            // Signal that extensions are available.
            _extensionsAvailable.SetResult();

            // Process changes, until cancellation (via disposal).
            await foreach (var changes in subscription.WithCancellation(token).ConfigureAwait(false))
            {
                foreach (var (changeType, resource) in changes)
                {
                    _logger.LogDebug("Received {ChangeType} for resource {Name}", changeType, resource.Name);

                    if (changeType is ResourceViewModelChangeType.Upsert)
                    {
                        await IntegrateAsync(resource).ConfigureAwait(false);
                    }
                    else if (changeType is ResourceViewModelChangeType.Delete)
                    {
                        if (resource.IsDashboardExtension)
                        {
                            var removed = _instanceByResourceName.Remove(resource.Name);

                            Debug.Assert(removed, "Dashboard extension should have been removed.");
                        }
                    }
                    else
                    {
                        Debug.Fail($"Unexpected {nameof(ResourceViewModelChangeType)}: {changeType}");
                    }
                }
            }

            async Task IntegrateAsync(ResourceViewModel resource)
            {
                // NOTE a resource's status as a dashboard extension does not change over time, by design. So throughout this method we only add, not update, config.
                // The exception is when a resource exits, in which case we remove its configuration.
                if (resource.IsDashboardExtension)
                {
                    if (resource.KnownState == KnownResourceState.Running)
                    {
                        // Only consider running resources.
                        if (!_instanceByResourceName.ContainsKey(resource.Name))
                        {
                            await AddAsync().ConfigureAwait(false);
                        }
                    }
                    else if (resource.KnownState is KnownResourceState.Exited or KnownResourceState.Finished)
                    {
                        // Clean up if the resource is no longer running.
                        Remove();
                    }
                }

                async Task AddAsync()
                {
                    _logger.LogDebug("Adding dashboard extension {Name}", resource.Name);

                    // NOTE we use the first URL as the base URL for the extension.
                    var extensionsContainerUri = resource.Urls.First().Url;

                    Instance instance = new(resource.Name, extensionsContainerUri, _logger);

                    _instanceByResourceName.Add(resource.Name, instance);

                    var extensionConfig = await instance.InitializeAsync(token).ConfigureAwait(false);

                    if (extensionConfig is null)
                    {
                        return;
                    }

                    if (ImmutableInterlocked.TryAdd(ref _extensionConfigByResourceName, resource.Name, extensionConfig))
                    {
                        var pagesChanged = false;

                        foreach (var pageConfig in extensionConfig.TopLevelPages)
                        {
                            if (ImmutableInterlocked.TryAdd(ref _pageByUrlSlug, pageConfig.UrlSlug, new TopLevelPageData(pageConfig, extensionsContainerUri.ToString())))
                            {
                                pagesChanged = true;
                            }
                            else
                            {
                                _logger.LogWarning("Duplicate top-level page URL slug {UrlSlug} in extension {ResourceName}", pageConfig.UrlSlug, resource.Name);
                            }
                        }

                        if (pagesChanged)
                        {
                            UpdatePages();
                        }
                    }
                    else
                    {
                        Debug.Fail("Extension configuration should have been added.");
                    }
                }

                void Remove()
                {
                    _logger.LogDebug("Removing dashboard extension {Name}", resource.Name);

                    if (_instanceByResourceName.Remove(resource.Name))
                    {
                        if (ImmutableInterlocked.TryRemove(ref _extensionConfigByResourceName, resource.Name, out var extensionConfig))
                        {
                            var pagesChanged = false;

                            foreach (var pageConfig in extensionConfig.TopLevelPages)
                            {
                                if (ImmutableInterlocked.TryRemove(ref _pageByUrlSlug, pageConfig.UrlSlug, out _))
                                {
                                    pagesChanged = true;
                                }
                            }

                            if (pagesChanged)
                            {
                                UpdatePages();
                            }
                        }
                    }
                }

                void UpdatePages()
                {
                    // Produce the ordered set of pages.
                    _pages = _pageByUrlSlug.Values.Select(p => p.Configuration).OrderBy(c => c.Priority).ThenBy(c => c.Title).ToImmutableArray();

                    foreach (var subscription in _topLevelPageConfigurationSubscriptions)
                    {
                        subscription(_pages);
                    }
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);

        _cts.Dispose();
    }

    async Task<TopLevelPageData?> IExtensionRegistry.GetTopLevelPageConfigurationAsync(string urlSlug, CancellationToken cancellationToken)
    {
        await _extensionsAvailable.Task.ConfigureAwait(false);

        _pageByUrlSlug.TryGetValue(urlSlug, out var pageConfig);

        return pageConfig;
    }

    IDisposable IExtensionRegistry.SubscribeToTopLevelPageConfiguration(Action<ImmutableArray<TopLevelPageConfiguration>> callback)
    {
        var added = ImmutableInterlocked.Update(ref _topLevelPageConfigurationSubscriptions, (set, c) => set.Add(c), callback);

        Debug.Assert(added, "Subscription should have been added.");

        if (_extensionsAvailable.Task.IsCompletedSuccessfully)
        {
            // Data is already available, so call back immediately (even if empty).
            callback(_pages);
        }

        return new DisposableAction(() =>
        {
            var removed = ImmutableInterlocked.Update(ref _topLevelPageConfigurationSubscriptions, (set, c) => set.Remove(c), callback);

            Debug.Assert(removed, "Subscription should have been removed.");
        });
    }

    private sealed class Instance(string resourceName, Uri extensionsContainerUri, ILogger<ExtensionRegistry> logger)
    {
        private static readonly HttpClient s_sharedClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static readonly JsonSerializerOptions? s_options = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public async Task<ExtensionConfiguration?> InitializeAsync(CancellationToken token)
        {
            UriBuilder builder = new(extensionsContainerUri);

            if (!builder.Path.EndsWith('/'))
            {
                builder.Path += "/";
            }

            builder.Path += ".well-known/dotnet-aspire/dashboard-extension.json";

            var uri = builder.Uri;

            logger.LogDebug("Fetching extension configuration from {Uri} for resource {Name}", uri, resourceName);

            try
            {
                var configuration = await s_sharedClient.GetFromJsonAsync<ExtensionConfiguration>(uri, s_options, token).ConfigureAwait(false);

                if (configuration is null)
                {
                    logger.LogDebug("No configuration provided for resource {Name}", resourceName);
                }
                else
                {
                    logger.LogDebug("Received configuration for resource {Name} with {TopLevelPageCount} top-level pages", resourceName, configuration.TopLevelPages.Length);

                    List<string>? errors = null;
                    configuration.Validate(ref errors);

                    if (errors is { Count: not 0 })
                    {
                        logger.LogWarning("Error(s) in configuration for resource {Name}:", resourceName);

                        foreach (var error in errors)
                        {
                            logger.LogWarning("- {Error}", error);
                        }
                    }
                }

                return configuration;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error fetching extension configuration for resource {Name}", resourceName);
                return null;
            }
        }
    }

    private sealed class DisposableAction(Action action) : IDisposable
    {
        public void Dispose() => Interlocked.Exchange(ref action!, null)?.Invoke();
    }
}

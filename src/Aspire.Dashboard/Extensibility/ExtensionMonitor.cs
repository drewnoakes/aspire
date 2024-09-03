// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Immutable;
using Aspire.Dashboard.Model;
using System.Text.Json;

namespace Aspire.Dashboard.Extensibility;

internal interface IExtensionMonitor
{
    event Action ExtensionsChanged;

    ImmutableDictionary<string, ExtensionConfiguration> ExtensionConfigByResourceName { get; }
}

/// <summary>
/// Tracks dashboard extensions and registers them in the dashboard.
/// </summary>
/// <remarks>
/// Uses <see cref="IDashboardClient"/> to subscribe to resources. When dashboard extensions
/// become available, this component will connect to them and retrieve their configuration.
/// </remarks>
internal sealed class ExtensionMonitor : IExtensionMonitor, IAsyncDisposable
{
    // TODO logging, even if debug level

    public event Action? ExtensionsChanged;

    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<string, Instance> _instanceByResourceName = new(StringComparers.ResourceName);
    private readonly IDashboardClient _dashboardClient;

    private ImmutableDictionary<string, ExtensionConfiguration> _extensionConfigByResourceName = ImmutableDictionary<string, ExtensionConfiguration>.Empty.WithComparers(StringComparers.ResourceName);

    ImmutableDictionary<string, ExtensionConfiguration> IExtensionMonitor.ExtensionConfigByResourceName => _extensionConfigByResourceName;

    public ExtensionMonitor(IDashboardClient dashboardClient)
    {
        _dashboardClient = dashboardClient;

        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            var (snapshot, subscription) = await dashboardClient.SubscribeResourcesAsync(token).ConfigureAwait(false);

            // Integrate initial state
            foreach (var resource in snapshot)
            {
                await IntegrateAsync(resource).ConfigureAwait(false);
            }

            await foreach (var changes in subscription.WithCancellation(token).ConfigureAwait(false))
            {
                foreach (var (changeType, resource) in changes)
                {
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
        });

        async Task IntegrateAsync(ResourceViewModel resource)
        {
            // NOTE a resource's status as a dashboard extension does not change over time, by design.
            if (resource.IsDashboardExtension)
            {
                // Only consider running resources.
                if (resource.KnownState == KnownResourceState.Running)
                {
                    if (!_instanceByResourceName.ContainsKey(resource.Name))
                    {
                        Instance instance = new(resource.Name, resource.Urls);

                        _instanceByResourceName.Add(resource.Name, instance);

                        var configuration = await instance.InitializeAsync(token).ConfigureAwait(false);

                        if (configuration is not null)
                        {
                            Add(configuration);
                        }
                    }
                }
            }

            void Add(ExtensionConfiguration extensionConfig)
            {
                ImmutableInterlocked.TryAdd(ref _extensionConfigByResourceName, resource.Name, extensionConfig);
                ExtensionsChanged?.Invoke();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);

        _cts.Dispose();
    }

    private sealed class Instance
    {
        private static readonly HttpClient s_sharedClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private static readonly JsonSerializerOptions? s_options = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        private readonly string _resourceName;
        private readonly ImmutableArray<UrlViewModel> _urls;

        public Instance(string resourceName, ImmutableArray<UrlViewModel> urls)
        {
            _resourceName = resourceName;
            _urls = urls;
        }

        public async Task<ExtensionConfiguration?> InitializeAsync(CancellationToken token)
        {
            // TODO error handling and resilience

            UriBuilder builder = new(_urls.First().Url);

            if (!builder.Path.EndsWith('/'))
            {
                builder.Path += "/";
            }

            builder.Path += ".well-known/dotnet-aspire/dashboard-extension.json";

            var configuration = await s_sharedClient.GetFromJsonAsync<ExtensionConfiguration>(builder.Uri, s_options, token).ConfigureAwait(false);

            return configuration;
        }
    }
}

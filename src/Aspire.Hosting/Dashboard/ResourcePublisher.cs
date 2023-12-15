// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Builds a collection of resources by integrating incoming resource changes,
/// and allowing multiple subscribers to receive the current resource collection
/// snapshot and future updates.
/// </summary>
internal sealed class ResourcePublisher(CancellationToken cancellationToken)
{
    private readonly object _syncLock = new();
    private readonly Dictionary<string, ResourceSnapshot> _snapshot = [];
    private ImmutableHashSet<Channel<ResourceChange>> _outgoingChannels = [];

    internal bool TryGetResource(string resourceName, [NotNullWhen(returnValue: true)] out ResourceSnapshot? resource)
    {
        lock (_syncLock)
        {
            return _snapshot.TryGetValue(resourceName, out resource);
        }
    }

    public ResourceSubscription Subscribe()
    {
        lock (_syncLock)
        {
            var channel = Channel.CreateUnbounded<ResourceChange>(
                new UnboundedChannelOptions { AllowSynchronousContinuations = true, SingleReader = true, SingleWriter = true });

            ImmutableInterlocked.Update(ref _outgoingChannels, static (set, channel) => set.Add(channel), channel);

            var builder = ImmutableArray.CreateBuilder<ResourceViewModel>(_snapshot.Values.Count);

            foreach (var (_, value) in _snapshot)
            {
                builder.Add(ToViewModel(value));
            }

            return new ResourceSubscription(
                InitialState: builder.MoveToImmutable(),
                Subscription: StreamUpdates());

            async IAsyncEnumerable<ResourceChange> StreamUpdates()
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        yield return await channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    ImmutableInterlocked.Update(ref _outgoingChannels, static (set, channel) => set.Remove(channel), channel);
                }
            }
        }
    }

    /// <summary>
    /// Integrates a changed resource within the cache, and broadcasts the update to any subscribers.
    /// </summary>
    /// <param name="resource">The resource that was modified.</param>
    /// <param name="changeType">The change type (Added, Modified, Deleted).</param>
    /// <returns>A task that completes when the cache has been updated and all subscribers notified.</returns>
    public async ValueTask IntegrateAsync(ResourceSnapshot resource, ResourceChangeType changeType)
    {
        lock (_syncLock)
        {
            switch (changeType)
            {
                case ResourceChangeType.Upsert:
                    _snapshot[resource.Name] = resource;
                    break;

                case ResourceChangeType.Delete:
                    _snapshot.Remove(resource.Name);
                    break;
            }
        }

        foreach (var channel in _outgoingChannels)
        {
            await channel.Writer.WriteAsync(new(changeType, ToViewModel(resource)), cancellationToken).ConfigureAwait(false);
        }
    }

    private static ResourceViewModel ToViewModel(ResourceSnapshot snapshot)
    {
        // NOTE this is a temporary method -- ultimately, snapshots will be converted to gRPC messages,
        // then on the other side those gRPC messages will be turned into ResourceViewModel objects, or
        // maybe just used to update existing ones.

        return new()
        {
            Name = snapshot.Name,
            ResourceType = snapshot.ResourceType,
            DisplayName = snapshot.DisplayName,
            Uid = snapshot.Uid,
            State = snapshot.State,
            CreationTimeStamp = snapshot.CreationTimeStamp,
            Environment = snapshot.Environment.Select(e => new EnvironmentVariableViewModel { Name = e.Name, Value = e.Value, FromSpec = e.FromSpec }).ToImmutableArray(),
            Endpoints = snapshot.Endpoints,
            Services = snapshot.Services.Select(s => new ResourceServiceViewModel(s.Name, s.AllocatedAddress, s.AllocatedPort)).ToImmutableArray(),
            ExpectedEndpointsCount = snapshot.ExpectedEndpointsCount,
            CustomData = snapshot.Data.ToFrozenDictionary(pair => pair.Key, pair => pair.Value, StringComparers.ResourceDataKey)
        };
    }
}

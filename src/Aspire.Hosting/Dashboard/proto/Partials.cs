// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.ResourceService.Proto.V1;

partial class Resource
{
    public static Resource FromSnapshot(ResourceSnapshot snapshot)
    {
        var resource = new Resource
        {
            Name = snapshot.Name,
            ResourceType = snapshot.ResourceType,
            DisplayName = snapshot.DisplayName,
            Uid = snapshot.Uid,
            State = snapshot.State ?? "",
            StateStyle = snapshot.StateStyle ?? "",
        };

        if (snapshot.HealthState is HealthStateKind healthState)
        {
            resource.HealthState = healthState;
        }

        if (snapshot.CreationTimeStamp.HasValue)
        {
            resource.CreatedAt = Timestamp.FromDateTime(snapshot.CreationTimeStamp.Value.ToUniversalTime());
        }

        foreach (var env in snapshot.Environment)
        {
            resource.Environment.Add(new EnvironmentVariable { Name = env.Name, Value = env.Value ?? "", IsFromSpec = env.IsFromSpec });
        }

        foreach (var url in snapshot.Urls)
        {
            resource.Urls.Add(new Url { Name = url.Name, FullUrl = url.Url, IsInternal = url.IsInternal });
        }

        foreach (var property in snapshot.Properties)
        {
            resource.Properties.Add(new ResourceProperty { Name = property.Name, Value = property.Value, IsSensitive = property.IsSensitive });
        }

        foreach (var volume in snapshot.Volumes)
        {
            resource.Volumes.Add(new Volume
            {
                Source = volume.Source,
                Target = volume.Target,
                MountType = volume.MountType,
                IsReadOnly = volume.IsReadOnly
            });
        }

        foreach (var command in snapshot.Commands)
        {
            resource.Commands.Add(new ResourceCommand { CommandType = command.Type, DisplayName = command.DisplayName, IconName = command.IconName ?? string.Empty, IconVariant = MapIconVariant(command.IconVariant), IsHighlighted = command.IsHighlighted, State = MapCommandState(command.State) });
        }

        foreach (var waitFor in snapshot.WaitFors)
        {
            resource.WaitFors.Add(new WaitFor { ResourceName = waitFor.ResourceName, WaitType = MapWaitType(waitFor.WaitType), ExitCode = waitFor.ExitCode });
        }

        return resource;

        static WaitType MapWaitType(Hosting.ApplicationModel.WaitType waitType)
        {
            return waitType switch
            {
                Hosting.ApplicationModel.WaitType.WaitUntilHealthy => WaitType.WaitUntilHealthy,
                Hosting.ApplicationModel.WaitType.WaitForCompletion => WaitType.WaitForCompletion,
                _ => throw new InvalidOperationException("Unknown wait type: " + waitType),
            };
        }
    }

    private static IconVariant MapIconVariant(Hosting.ApplicationModel.IconVariant? iconVariant)
    {
        return iconVariant switch
        {
            Hosting.ApplicationModel.IconVariant.Regular => IconVariant.Regular,
            Hosting.ApplicationModel.IconVariant.Filled => IconVariant.Filled,
            null => IconVariant.Regular,
            _ => throw new InvalidOperationException("Unexpected icon variant: " + iconVariant)
        };
    }

    private static ResourceCommandState MapCommandState(Hosting.ApplicationModel.ResourceCommandState state)
    {
        return state switch
        {
            Hosting.ApplicationModel.ResourceCommandState.Enabled => ResourceCommandState.Enabled,
            Hosting.ApplicationModel.ResourceCommandState.Disabled => ResourceCommandState.Disabled,
            Hosting.ApplicationModel.ResourceCommandState.Hidden => ResourceCommandState.Hidden,
            _ => throw new InvalidOperationException("Unexpected state: " + state)
        };
    }

}

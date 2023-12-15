// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Model;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Immutable snapshot of a container's state at a point in time.
/// </summary>
public sealed class ContainerSnapshot : ResourceSnapshot
{
    public override string ResourceType => KnownResourceTypes.Container;

    public required string? ContainerId { get; init; }
    public required string Image { get; init; }
    public required ImmutableArray<int> Ports { get; init; }
    public required string? Command { get; init; }
    public required ImmutableArray<string>? Args { get; init; }

    protected override IEnumerable<(string Key, Value Value)> GetCustomData()
    {
        yield return (ResourceDataKeys.Container.Id, Value.ForString(ContainerId));
        yield return (ResourceDataKeys.Container.Image, Value.ForString(Image));
        yield return (ResourceDataKeys.Container.Ports, Value.ForList(Ports.Select(port => Value.ForNumber(port)).ToArray()));
        yield return (ResourceDataKeys.Container.Command, Command is null ? Value.ForNull() : Value.ForString(Command));
        yield return (ResourceDataKeys.Container.Args, Args is null ? Value.ForNull() : Value.ForList(Args.Value.Select(port => Value.ForString(port)).ToArray()));
    }
}
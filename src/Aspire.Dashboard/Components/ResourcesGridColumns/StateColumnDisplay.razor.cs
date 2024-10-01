// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Dashboard.Extensions;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Storage;
using Aspire.Dashboard.Resources;
using Humanizer;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.ResourcesGridColumns;

public partial class StateColumnDisplay
{
    private IJSObjectReference? _jsModule;

    [Parameter, EditorRequired]
    public required ResourceViewModel Resource { get; init; }

    [Parameter, EditorRequired]
    public required Dictionary<ApplicationKey, int>? UnviewedErrorCounts { get; init; }

    [Inject]
    public required IStringLocalizer<Columns> Loc { get; init; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _jsModule ??= await JS.InvokeAsync<IJSObjectReference>("import", "/Components/ResourcesGridColumns/StateColumnDisplay.razor.js");

        await _jsModule.InvokeVoidAsync("addWaitForMouseEventListeners");
    }

    /// <summary>
    /// Gets the tooltip for the state column.
    /// </summary>
    /// <remarks>
    /// This is a static method as it needs to be called by the parent column component, as the
    /// cell's tooltip is set at that level.
    /// </remarks>
    public static string? GetResourceStateTooltip(ResourceViewModel resource, IStringLocalizer<Columns> Loc)
    {
        if (resource.IsStopped())
        {
            if (resource.TryGetExitCode(out var exitCode) && exitCode is not 0)
            {
                // Process completed unexpectedly, hence the non-zero code. This is almost certainly an error, so warn users.
                return string.Format(CultureInfo.CurrentCulture, Loc[Columns.StateColumnResourceExitedUnexpectedly], resource.ResourceType, exitCode);
            }
            else
            {
                // Process completed, which may not have been unexpected.
                return string.Format(CultureInfo.CurrentCulture, Loc[Columns.StateColumnResourceExited], resource.ResourceType);
            }
        }
        else if (resource.KnownState is KnownResourceState.Waiting && resource.WaitFors.Length is not 0)
        {
            // Resource is waiting.
            if (resource.WaitFors.Length is 1)
            {
                return string.Format(CultureInfo.CurrentCulture, Loc[Columns.WaitingSingleResourceStateToolTip], resource.WaitFors[0].ResourceName);
            }
            else
            {
                return string.Format(CultureInfo.CurrentCulture, Loc[Columns.WaitingMultipleResourcesStateToolTip], string.Join(", ", resource.WaitFors.Select(r => r.ResourceName)));
            }
        }
        else if (resource.KnownState is KnownResourceState.Running && !resource.HealthReports.All(r => r.HealthStatus is HealthStatus.Healthy))
        {
            // Resource is running but not healthy (initializing).
            return Loc[nameof(Columns.RunningAndUnhealthyResourceStateToolTip)];
        }

        return null;
    }

    /// <summary>
    /// Gets data needed to populate the content of the state column.
    /// </summary>
    private ResourceStateViewModel GetStateViewModel()
    {
        Icon icon;
        Color color;

        if (Resource.IsStopped())
        {
            if (Resource.TryGetExitCode(out var exitCode) && exitCode is not 0)
            {
                // Process completed unexpectedly, hence the non-zero code. This is almost certainly an error, so warn users.
                icon = new Icons.Filled.Size16.ErrorCircle();
                color = Color.Error;
            }
            else if (Resource.IsFinishedState())
            {
                // Process completed successfully.
                icon = new Icons.Filled.Size16.CheckmarkUnderlineCircle();
                color = Color.Success;
            }
            else
            {
                // Process completed, which may not have been unexpected.
                icon = new Icons.Filled.Size16.Warning();
                color = Color.Warning;
            }
        }
        else if (Resource.IsUnusableTransitoryState() || Resource.IsUnknownState())
        {
            icon = new Icons.Filled.Size16.CircleHint();
            color = Color.Info;
        }
        else if (Resource.HasNoState())
        {
            icon = new Icons.Filled.Size16.Circle();
            color = Color.Neutral;
        }
        else if (!string.IsNullOrEmpty(Resource.StateStyle))
        {
            (icon, color) = Resource.StateStyle switch
            {
                "warning" => ((Icon)new Icons.Filled.Size16.Warning(), Color.Warning),
                "error" => (new Icons.Filled.Size16.ErrorCircle(), Color.Error),
                "success" => (new Icons.Filled.Size16.CheckmarkCircle(), Color.Success),
                "info" => (new Icons.Filled.Size16.Info(), Color.Info),
                _ => (new Icons.Filled.Size16.Circle(), Color.Neutral)
            };
        }
        else
        {
            (icon, color) = Resource.IsHealthy
                ? (new Icons.Filled.Size16.CheckmarkCircle(), Color.Success)
                : ((Icon)new Icons.Regular.Size16.CheckmarkCircleWarning(), Color.Neutral);
        }

        var text = Resource switch
        {
            { State: null or "" } => Loc[Columns.UnknownStateLabel],
            { KnownState: KnownResourceState.Running, IsHealthy: false } => Loc[Columns.RunningAndUnhealthyResourceStateName],
            _ => Resource.State.Humanize()
        };

        return new ResourceStateViewModel(text, icon, color);
    }

    private record class ResourceStateViewModel(string Text, Icon Icon, Color Color);
}

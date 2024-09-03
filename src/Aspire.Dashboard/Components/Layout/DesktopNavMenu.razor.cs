// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//using Aspire.Dashboard.Extensibility;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Layout;

public partial class DesktopNavMenu : ComponentBase
{
    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    internal static Icon ResourcesIcon(bool active = false) => active
        ? new Icons.Filled.Size24.AppFolder()
        : new Icons.Regular.Size24.AppFolder();

    internal static Icon ConsoleLogsIcon(bool active = false) => active
        ? new Icons.Filled.Size24.SlideText()
        : new Icons.Regular.Size24.SlideText();

    internal static Icon StructuredLogsIcon(bool active = false) => active
        ? new Icons.Filled.Size24.SlideTextSparkle()
        : new Icons.Regular.Size24.SlideTextSparkle();

    internal static Icon TracesIcon(bool active = false) => active
        ? new Icons.Filled.Size24.GanttChart()
        : new Icons.Regular.Size24.GanttChart();

    internal static Icon MetricsIcon(bool active = false) => active
        ? new Icons.Filled.Size24.ChartMultiple()
        : new Icons.Regular.Size24.ChartMultiple();

    protected override void OnParametersSet()
    {
        // TODO unhook this in component dispose
        ExtensionMonitor.ExtensionsChanged += delegate
        {
            _ = InvokeAsync(StateHasChanged);
        };

        base.OnParametersSet();
    }
}

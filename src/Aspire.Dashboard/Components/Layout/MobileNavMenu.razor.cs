// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Aspire.Dashboard.Components.CustomIcons;
using Aspire.Dashboard.Extensibility;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aspire.Dashboard.Components.Layout;

public partial class MobileNavMenu : ComponentBase
{
    private ImmutableArray<TopLevelPageConfiguration> _topLevelPages = [];
    private IDisposable? _topLevelPageSubscription;

    [Inject]
    public required NavigationManager NavigationManager { get; init; }

    [Inject]
    public required IDashboardClient DashboardClient { get; init; }

    [Inject]
    public required IStringLocalizer<Resources.Layout> Loc { get; init; }

    [Inject]
    public required IExtensionRegistry ExtensionRegistry { get; init; }

    [Inject]
    public required IJSRuntime JS { get; init; }

    protected override void OnInitialized()
    {
        _topLevelPageSubscription = ExtensionRegistry.SubscribeToTopLevelPageConfiguration(OnTopLevelPagesChanged);

        void OnTopLevelPagesChanged(ImmutableArray<TopLevelPageConfiguration> pages)
        {
            if (_topLevelPages.IsEmpty && pages.IsEmpty)
            {
                return;
            }

            _topLevelPages = pages;

            _ = InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        _topLevelPageSubscription?.Dispose();
    }

    private Task NavigateToAsync(string url)
    {
        NavigationManager.NavigateTo(url);
        return Task.CompletedTask;
    }

    private IEnumerable<MobileNavMenuEntry> GetMobileNavMenuEntries()
    {
        if (DashboardClient.IsEnabled)
        {
            yield return new MobileNavMenuEntry(
                Loc[nameof(Resources.Layout.NavMenuResourcesTab)],
                () => NavigateToAsync(DashboardUrls.ResourcesUrl()),
                DesktopNavMenu.ResourcesIcon(),
                LinkMatchRegex: new Regex($"^{DashboardUrls.ResourcesUrl()}$")
            );

            yield return new MobileNavMenuEntry(
                Loc[nameof(Resources.Layout.NavMenuConsoleLogsTab)],
                () => NavigateToAsync(DashboardUrls.ConsoleLogsUrl()),
                DesktopNavMenu.ConsoleLogsIcon(),
                LinkMatchRegex: GetNonIndexPageRegex(DashboardUrls.ConsoleLogsUrl())
            );
        }

        yield return new MobileNavMenuEntry(
            Loc[nameof(Resources.Layout.NavMenuStructuredLogsTab)],
            () => NavigateToAsync(DashboardUrls.StructuredLogsUrl()),
            DesktopNavMenu.StructuredLogsIcon(),
            LinkMatchRegex: GetNonIndexPageRegex(DashboardUrls.StructuredLogsUrl())
        );

        yield return new MobileNavMenuEntry(
            Loc[nameof(Resources.Layout.NavMenuTracesTab)],
            () => NavigateToAsync(DashboardUrls.TracesUrl()),
            DesktopNavMenu.TracesIcon(),
            LinkMatchRegex: GetNonIndexPageRegex(DashboardUrls.TracesUrl())
        );

        yield return new MobileNavMenuEntry(
            Loc[nameof(Resources.Layout.NavMenuMetricsTab)],
            () => NavigateToAsync(DashboardUrls.MetricsUrl()),
            DesktopNavMenu.MetricsIcon(),
            LinkMatchRegex: GetNonIndexPageRegex(DashboardUrls.MetricsUrl())
        );

        yield return new MobileNavMenuEntry(
            Loc[nameof(Resources.Layout.MainLayoutAspireRepoLink)],
            async () =>
            {
                await JS.InvokeVoidAsync("open", ["https://aka.ms/dotnet/aspire/repo", "_blank"]);
            },
            new AspireIcons.Size24.GitHub()
        );

        yield return new MobileNavMenuEntry(
            Loc[nameof(Resources.Layout.MainLayoutAspireDashboardHelpLink)],
            LaunchHelpAsync,
            new Icons.Regular.Size24.QuestionCircle()
        );

        yield return new MobileNavMenuEntry(
            Loc[nameof(Resources.Layout.MainLayoutLaunchSettings)],
            LaunchSettingsAsync,
            new Icons.Regular.Size24.Settings()
        );

        foreach (var topLevelPage in _topLevelPages)
        {
            yield return new MobileNavMenuEntry(
                topLevelPage.Title,
                () => NavigateToAsync($"/extension/{topLevelPage.UrlSlug}"),
                IconCache.GetIcon(topLevelPage.IconName, IconSize.Size24)
            );
        }
    }

    private static Regex GetNonIndexPageRegex(string pageRelativeBasePath)
    {
        pageRelativeBasePath = Regex.Escape(pageRelativeBasePath);
        return new Regex($"^({pageRelativeBasePath}|{pageRelativeBasePath}/.+)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }
}


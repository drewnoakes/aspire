// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Extensibility;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;

namespace Aspire.Dashboard.Components.Layout;

/// <summary>
/// Subscribes to data about top-level pages provided by extensions.
/// Base class for the various navigation menu implementations (e.g. Desktop vs Mobile).
/// </summary>
public abstract class NavMenuBase : ComponentBase, IDisposable
{
    [Inject]
    public required IExtensionRegistry ExtensionRegistry { get; init; }

    protected ImmutableArray<TopLevelPageConfiguration> TopLevelPages { get; private set; } = [];

    private IDisposable? _topLevelPageSubscription;

    protected override void OnInitialized()
    {
        _topLevelPageSubscription = ExtensionRegistry.SubscribeToTopLevelPageConfiguration(OnTopLevelPagesChanged);

        void OnTopLevelPagesChanged(ImmutableArray<TopLevelPageConfiguration> pages)
        {
            if (TopLevelPages.IsEmpty && pages.IsEmpty)
            {
                return;
            }

            TopLevelPages = pages;

            _ = InvokeAsync(StateHasChanged);
        }
    }

    public virtual void Dispose()
    {
        _topLevelPageSubscription?.Dispose();
    }
}

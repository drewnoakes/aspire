// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Extensibility;

/// <summary>
/// Immutable snapshot of configuration data for all dashboard extensions.
/// </summary>
public sealed class ExtensionConfiguration
{
    [JsonPropertyName("topLevelPages")]
    public ImmutableArray<TopLevelPageConfiguration> TopLevelPages { get; init; } = [];

    public void Validate(ref List<string>? errors)
    {
        foreach (var topLevelPage in TopLevelPages)
        {
            topLevelPage.Validate(ref errors);
        }
    }
}

/// <summary>
/// Immutable snapshot of configuration data for a top-level page
/// that's proffered by a dashboard extension.
/// </summary>
/// <remarks>
/// An extension may provide zero or more top-level pages.
/// </remarks>
public sealed class TopLevelPageConfiguration
{
    /// <summary>
    /// Gets a displayable title for the page.
    /// </summary>
    /// <remarks>
    /// Used beneath the icon in the dashboard's navigation,
    /// so should be quite a short string.
    /// </remarks>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// Used in URLs to uniquely identify this top-level page.
    /// </summary>
    /// <remarks>
    /// To view top-level pages, users navigate to a URL resembling
    /// <c>http://localhost:1234/extension/my-extension</c>, where
    /// the slug is <c>my-extension</c>.
    /// </remarks>
    [JsonPropertyName("urlName")]
    public required string UrlSlug { get; init; }

    /// <summary>
    /// Gets the URL of the top-level page's content, to be hosted within
    /// an iframe by the dashboard.
    /// </summary>
    /// <remarks>
    /// This URL is not displayed to the user. The user sees a URL in their
    /// browser that contains <see cref="UrlSlug"/>.
    /// </remarks>
    [JsonPropertyName("targetUrl")]
    public required string TargetUrl { get; init; }

    /// <summary>
    /// Gets the name of the icon that represents this top-level page.
    /// </summary>
    [JsonPropertyName("icon")]
    public string IconName { get; init; } = "PuzzlePiece";

    /// <summary>
    /// Gets the relative priority of this top-level page in lists presented to the user.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Top-level navigation items are ordered by increasing <see cref="Priority"/>,
    /// then alphabetically by <see cref="Title"/>.
    /// </para>
    /// <para>
    /// Defaults to zero.
    /// </para>
    /// </remarks>
    [JsonPropertyName("priority")]
    public int Priority { get; init; }

    /// <summary>
    /// Adds any validation errors to <paramref name="errors"/>.
    /// </summary>
    /// <param name="errors">
    /// A reference to a list of errors. If an error is being added
    /// and this value is null, it will be initialized.
    /// </param>
    public void Validate(ref List<string>? errors)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            errors ??= [];
            errors.Add($"{nameof(Title)} is required.");
        }

        if (string.IsNullOrWhiteSpace(IconName))
        {
            errors ??= [];
            errors.Add($"{nameof(IconName)} is required.");
        }

        if (string.IsNullOrWhiteSpace(UrlSlug))
        {
            errors ??= [];
            errors.Add($"{nameof(UrlSlug)} is required.");
        }

        if (string.IsNullOrWhiteSpace(TargetUrl))
        {
            errors ??= [];
            errors.Add($"{nameof(TargetUrl)} is required.");
        }
    }
}

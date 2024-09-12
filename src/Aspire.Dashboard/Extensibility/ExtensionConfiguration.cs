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
/// Extensions do not have to provide top-level pages.
/// Extensions may provide multiple top-level pages.
/// </remarks>
public sealed class TopLevelPageConfiguration
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("urlName")]
    public required string UrlSlug { get; init; }

    [JsonPropertyName("targetUrl")]
    public required string TargetUrl { get; init; }

    [JsonPropertyName("icon")]
    public string IconName { get; init; } = "PuzzlePiece";

    [JsonPropertyName("priority")]
    public int Priority { get; init; }

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

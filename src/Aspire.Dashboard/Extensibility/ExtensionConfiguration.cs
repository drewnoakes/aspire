// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Extensibility;

internal sealed class ExtensionConfiguration
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

internal sealed class TopLevelPageConfiguration
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }
    // The name of an icon
    // TODO map from this to Microsoft.FluentUI.AspNetCore.Components.Icons.Filled.Size20
    [JsonPropertyName("icon")]
    public required string Icon { get; init; }
    [JsonPropertyName("urlName")]
    public required string UrlName { get; init; }
    [JsonPropertyName("targetUrl")]
    public required string TargetUrl { get; init; }
    [JsonPropertyName("priority")]
    public int Priority { get; init; }

    public void Validate(ref List<string>? errors)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            errors ??= [];
            errors.Add("Title is required.");
        }

        // TODO icon validation, once icon representation is defined?

        if (string.IsNullOrWhiteSpace(UrlName))
        {
            errors ??= [];
            errors.Add("UrlName is required.");
        }

        if (string.IsNullOrWhiteSpace(TargetUrl))
        {
            errors ??= [];
            errors.Add("TargetUrl is required.");
        }
    }
}

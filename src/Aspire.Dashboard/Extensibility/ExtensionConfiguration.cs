// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
//using System.Text.Json;

namespace Aspire.Dashboard.Extensibility;

internal sealed class ExtensionConfiguration
{
    [JsonPropertyName("topLevelPages")]
    public ImmutableArray<TopLevelPageConfiguration> TopLevelPages { get; init; } = [];
}

internal sealed class TopLevelPageConfiguration
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }
    // The name of an icon
    // TODO map from this to Microsoft.FluentUI.AspNetCore.Components.Icons.Filled.Size20
    [JsonPropertyName("icon")]
    public required string Icon { get; init; }
    [JsonPropertyName("targetUrl")]
    public required string TargetUrl { get; init; }
    [JsonPropertyName("urlName")]
    public required string UrlName { get; init; }
}

//internal static class ExtensionConfigurationReader
//{
//    private static readonly JsonSerializerOptions? s_options = new()
//    {
//        ReadCommentHandling = JsonCommentHandling.Skip
//    };

//    public static ValueTask<ExtensionConfiguration?> ReadExtensionConfiguration(Stream stream, CancellationToken token)
//    {
//        return JsonSerializer.DeserializeAsync<ExtensionConfiguration>(stream, s_options, token);
//    }
//}

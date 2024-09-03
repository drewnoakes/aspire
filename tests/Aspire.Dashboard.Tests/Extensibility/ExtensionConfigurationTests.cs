// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Dashboard.Extensibility;
using Xunit;

namespace Aspire.Dashboard.Tests.Extensibility;

public sealed class ExtensionConfigurationTests
{
    private static readonly JsonSerializerOptions? s_jsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    [Fact]
    public void ExtensionConfiguration_DeserializeFromJson()
    {
        const string json = """
            // The contents of this configuration file are read once by the dashboard, when
            // the extension is first identified as running. The data here is otherwise static.

            {
              "topLevelPages": [
                {
                  "title": "My Extension",
                  // TODO identify icon somehow
                  "icon": "",
                  // relative paths are relative to the extension's endpoint
                  "targetUrl": "/index.html"
                }
              ]
            }
            """;

        var configuration = JsonSerializer.Deserialize<ExtensionConfiguration>(json, s_jsonOptions);

        Assert.NotNull(configuration);
        Assert.False(configuration.TopLevelPages.IsDefaultOrEmpty);

        var topLevelPage = Assert.Single(configuration.TopLevelPages);

        Assert.Equal("My Extension", topLevelPage.Title);
        Assert.Equal("", topLevelPage.Icon);
        Assert.Equal("/index.html", topLevelPage.TargetUrl);
    }
}

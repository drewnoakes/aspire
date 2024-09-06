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
    public void DeserializeFromJson()
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
                  "targetUrl": "/index.html",
                  "urlName": "my-extension",
                  "priority": 100
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
        Assert.Equal("my-extension", topLevelPage.UrlName);
        Assert.Equal(100, topLevelPage.Priority);
    }

    [Fact]
    public void TopLevelPageConfiguration_Validation_Success()
    {
        TopLevelPageConfiguration config = new()
        {
            Title = "My Extension",
            Icon = "",
            TargetUrl = "/index.html",
            UrlName = "my-extension",
            Priority = 100
        };

        List<string>? errors = null;
        config.Validate(ref errors);
        Assert.Null(errors);
    }

    [Fact]
    public void TopLevelPageConfiguration_Validation_MissingTitle()
    {
        TopLevelPageConfiguration config = new()
        {
            Title = "", // Title is required
            Icon = "",
            TargetUrl = "/index.html",
            UrlName = "my-extension",
            Priority = 100
        };

        List<string>? errors = null;
        config.Validate(ref errors);

        Assert.NotNull(errors);
        Assert.Collection(
            errors,
            error => Assert.Equal("Title is required.", error));
    }

    [Fact]
    public void TopLevelPageConfiguration_Validation_MissingTargetUrl()
    {
        TopLevelPageConfiguration config = new()
        {
            Title = "My Extension",
            Icon = "",
            TargetUrl = "", // TargetUrl is required
            UrlName = "my-extension",
            Priority = 100
        };

        List<string>? errors = null;
        config.Validate(ref errors);

        Assert.NotNull(errors);
        Assert.Collection(
            errors,
            error => Assert.Equal("TargetUrl is required.", error));
    }

    [Fact]
    public void TopLevelPageConfiguration_Validation_MissingUrlName()
    {
        TopLevelPageConfiguration config = new()
        {
            Title = "My Extension",
            Icon = "",
            TargetUrl = "/index.html",
            UrlName = "", // UrlName is required
            Priority = 100
        };

        List<string>? errors = null;
        config.Validate(ref errors);

        Assert.NotNull(errors);
        Assert.Collection(
            errors,
            error => Assert.Equal("UrlName is required.", error));
    }
}

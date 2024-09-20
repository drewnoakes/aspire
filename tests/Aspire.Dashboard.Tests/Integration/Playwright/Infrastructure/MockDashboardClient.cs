// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using Aspire.Dashboard.Model;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Dashboard.Tests.Integration.Playwright.Infrastructure;

public sealed class MockDashboardClient : IDashboardClient
{
    private static readonly BrowserTimeProvider s_timeProvider = new(NullLoggerFactory.Instance);

    public static readonly ResourceViewModel TestResource1 = new()
    {
        Name = "TestResource",
        DisplayName = "TestResource",
        Commands = [],
        CreationTimeStamp = DateTime.Now,
        Environment = [],
        ResourceType = KnownResourceTypes.Project,
        Properties = new[]
        {
            new KeyValuePair<string, ResourcePropertyViewModel>(
                KnownProperties.Project.Path,
                new ResourcePropertyViewModel(
                    KnownProperties.Project.Path,
                    new Value()
                    {
                        StringValue = "C:/MyProjectPath/Project.csproj"
                    },
                    isValueSensitive: false,
                    knownProperty: new(KnownProperties.Project.Path, "Path"),
                    priority: 0,
                    timeProvider: s_timeProvider))
        }.ToFrozenDictionary(),
        State = "Running",
        Uid = Guid.NewGuid().ToString(),
        StateStyle = null,
        ReadinessState = ReadinessState.Ready,
        Urls = [],
        Volumes = [],
        WaitsFor = []
    };

    public bool IsEnabled => true;
    public Task WhenConnected => Task.CompletedTask;
    public string ApplicationName => "IntegrationTestApplication";
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    public Task<ResourceCommandResponseViewModel> ExecuteResourceCommandAsync(string resourceName, string resourceType, CommandViewModel command, CancellationToken cancellationToken) => throw new NotImplementedException();
    public IAsyncEnumerable<IReadOnlyList<ResourceLogLine>> SubscribeConsoleLogs(string resourceName, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<ResourceViewModelSubscription> SubscribeResourcesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new ResourceViewModelSubscription(
            [TestResource1],
            Test()
        ));
    }

    private static async IAsyncEnumerable<IReadOnlyList<ResourceViewModelChange>> Test()
    {
        await Task.CompletedTask;
        yield return [];
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.TryAddLifecycleHook<TestResourceLifecycleHook>();

var ex = GetException();

AddTestResource("healthy", HealthStatus.Healthy, "I'm fine, thanks for asking.");
AddTestResource("unhealthy", HealthStatus.Unhealthy, "I can't do that, Dave.", exception: ex.ToString());
AddTestResource("degraded", HealthStatus.Degraded, "Had better days.", exception: ex.ToString(), waitFors: [ new("healthy", WaitType.WaitUntilHealthy, 0) ]);

AddTestResource("frontend", HealthStatus.Healthy, waitFors: [
    new("healthy", WaitType.WaitUntilHealthy, 0),
    new("unhealthy", WaitType.WaitUntilHealthy, 0),
    new("degraded", WaitType.WaitUntilHealthy, 0)
]);

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

builder.Build().Run();

static Exception GetException()
{
    try
    {
        throw new InvalidOperationException("This is a test exception.");
    }
    catch (InvalidOperationException e)
    {
        return e;
    }
}

IResourceBuilder<TestResource> AddTestResource(string name, HealthStatus status, string? description = null, string? exception = null, ImmutableArray<WaitForSnapshot>? waitFors = null)
{
    return builder
        .AddResource(new TestResource(name))
        .WithInitialState(new()
        {
            ResourceType = "Test Resource",
            State = "Starting",
            Properties = [],
            HealthReports = [new HealthReportSnapshot("test_check", status, description, exception)],
            WaitFors = waitFors ?? []
        })
        .ExcludeFromManifest();
}

internal sealed class TestResource(string name) : Resource(name);

internal sealed class TestResourceLifecycleHook(ResourceNotificationService notificationService) : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        foreach (var resource in appModel.Resources.OfType<TestResource>())
        {
            Task.Run(
                async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));

                    await notificationService.PublishUpdateAsync(
                        resource,
                        state => state with { State = new("Running", "success") });
                },
                cancellationToken);
        }

        return Task.CompletedTask;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Health;

internal class ResourceNotificationHealthCheckPublisher(DistributedApplicationModel model, ResourceNotificationService resourceNotificationService) : IHealthCheckPublisher
{
    public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        foreach (var resource in model.Resources)
        {
            if (resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var annotations))
            {
                ImmutableArray<HealthReportSnapshot>.Builder? healthReportsBuilder = null;

                foreach (var annotation in annotations)
                {
                    healthReportsBuilder ??= ImmutableArray.CreateBuilder<HealthReportSnapshot>();

                    if (!report.Entries.TryGetValue(annotation.Key, out var entry))
                    {
                        // TODO better exception type
                        //throw new InvalidOperationException($"Configuration error. No health check report exists for '{annotation.Key}'.");
                    }
                    else
                    {
                        // TODO do we want more information, e.g. tags, ...
                        healthReportsBuilder.Add(new(annotation.Key, entry.Status, entry.Description, entry.Exception?.ToString()));
                    }
                }

                var healthReports = healthReportsBuilder?.ToImmutable() ?? [];

                await resourceNotificationService
                    .PublishUpdateAsync(resource, s => s with { HealthReports = healthReports })
                    .ConfigureAwait(false);

                if (resource.TryGetLastAnnotation<ReplicaInstancesAnnotation>(out var replicaAnnotation))
                {
                    foreach (var (id, _) in replicaAnnotation.Instances)
                    {
                        await resourceNotificationService
                            .PublishUpdateAsync(resource, id, s => s with { HealthReports = healthReports })
                            .ConfigureAwait(false);
                    }
                }
            }
        }
    }
}

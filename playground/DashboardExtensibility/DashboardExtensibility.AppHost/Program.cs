// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

// This extension is modelled as a project, because this is a playground. In general,
// such extensions will be provided by containers, and may have semantically meaningful
// extension methods to add (and even configure) them.
builder.AddProject<Projects.DashboardExtensibility_DemoExtension>("dashboard-extension-demo")
    .WithAnnotation(new Aspire.Hosting.Dashboard.DashboardExtensionAnnotation());

builder.Build().Run();

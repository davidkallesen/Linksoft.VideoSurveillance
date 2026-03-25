#pragma warning disable MA0048 // File name must match type name — Aspire convention uses AppHost.cs for top-level Program
var builder = DistributedApplication.CreateBuilder(args);

var api = builder
    .AddProject<Projects.Linksoft_VideoSurveillance_Api>("api")
    .WithHttpEndpoint(port: 5000, name: "public");

builder
    .AddProject<Projects.Linksoft_VideoSurveillance_Blazor_App>("blazor")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder
    .AddProject<Projects.Linksoft_VideoSurveillance_Wpf_App>("wpf")
    .WithReference(api)
    .WaitFor(api);

builder
    .AddProject<Projects.Linksoft_CameraWall_Wpf_App>("CameraWall");

await builder
    .Build()
    .RunAsync()
    .ConfigureAwait(false);
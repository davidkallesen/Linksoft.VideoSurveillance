var builder = DistributedApplication.CreateBuilder(args);

var api = builder
    .AddProject<Projects.Linksoft_VideoSurveillance_Api>("api")
    .WithHttpEndpoint(port: 5000, name: "public");

builder
    .AddProject<Projects.Linksoft_VideoSurveillance_BlazorApp>("blazor")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

await builder
    .Build()
    .RunAsync()
    .ConfigureAwait(false);
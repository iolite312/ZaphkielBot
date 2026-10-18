using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using ZaphkielCore;
using ZaphkielCore.Abstractions;
using ZaphkielCore.Cqrs;
using ZaphkielData;
using ZaphkielProcessor.GitHub;
using ZaphkielProcessor.Handlers;
using ZaphkielShared.Options;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

// Fail at startup, with a clear message, instead of on the first incident.
builder.Services
    .AddOptions<GitHubOptions>()
    .Bind(builder.Configuration.GetSection(GitHubOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddZaphkielCore()
    .AddZaphkielData(builder.Configuration)
    .AddHandlersFromAssembly(typeof(CreateIncidentHandler).Assembly);

builder.Services.AddSingleton<IGitHubSignatureVerifier, GitHubSignatureVerifier>();
builder.Services.AddScoped<IGitHubWebhookService, GitHubWebhookService>();
builder.Services.AddHttpClient<IGitHubIssueClient, GitHubRestClient>();

builder.Build().Run();

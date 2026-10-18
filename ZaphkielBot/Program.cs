using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using ZaphkielBot.Discord;
using ZaphkielBot.GitHub;
using ZaphkielBot.Handlers;
using ZaphkielCore;
using ZaphkielCore.Abstractions;
using ZaphkielCore.Cqrs;
using ZaphkielData;
using ZaphkielShared.Options;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

// Fail at startup, with a clear message, instead of on the first slash command.
builder.Services
    .AddOptions<DiscordOptions>()
    .Bind(builder.Configuration.GetSection(DiscordOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<GitHubWebhookOptions>()
    .Bind(builder.Configuration.GetSection(GitHubWebhookOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddZaphkielCore()
    .AddZaphkielData(builder.Configuration)
    .AddHandlersFromAssembly(typeof(SendDiscordMessageHandler).Assembly)
    .AddZaphkielDiscord();

// GitHub webhooks arrive on the same public URL as the Discord interactions.
builder.Services.AddSingleton<IGitHubSignatureVerifier, GitHubSignatureVerifier>();
builder.Services.AddScoped<IGitHubWebhookService, GitHubWebhookService>();

builder.Build().Run();

using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using ZaphkielBot.Discord;
using ZaphkielBot.Handlers;
using ZaphkielCore;
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
    .AddZaphkielCore()
    .AddZaphkielData(builder.Configuration)
    .AddHandlersFromAssembly(typeof(SendDiscordMessageHandler).Assembly)
    .AddZaphkielDiscord();

builder.Build().Run();

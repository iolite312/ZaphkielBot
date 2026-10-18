using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ZaphkielBot.Discord;

/// <summary>Registers the slash commands in the background at startup, so a Discord hiccup never blocks the host from starting.</summary>
public sealed class SlashCommandRegistrationService(
    ISlashCommandRegistrar registrar,
    ILogger<SlashCommandRegistrationService> logger) : BackgroundService
{
    private const int MaxAttempts = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await registrar.RegisterAsync(stoppingToken);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Registering slash commands failed (attempt {Attempt}/{Max})", attempt, MaxAttempts);
                if (attempt == MaxAttempts) return;
                await Task.Delay(TimeSpan.FromSeconds(5 * attempt), stoppingToken);
            }
        }
    }
}

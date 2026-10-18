using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZaphkielBot.Interfaces;
using ZaphkielShared.Options;

namespace ZaphkielBot.Discord;

public class DiscordInteractionHandler(
    IDiscordRestClientProvider provider,
    InteractionService interactions,
    IServiceScopeFactory scopes,
    IEmbedMapper embeds,
    IOptions<DiscordOptions> options,
    ILogger<DiscordInteractionHandler> logger) : IDiscordInteractionHandler
{
    private const string FallbackError =
        """{"type":4,"data":{"embeds":[{"title":"⚠️ Something went wrong","description":"Please try again in a moment.","color":15548997}],"flags":64}}""";

    public async Task<InteractionResult> HandleAsync(
        string? signature, string? timestamp, byte[] body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp))
            return Rejected("missing signature headers");

        RestInteraction interaction;
        try
        {
            // doApiCallOnCreation: false -> everything comes from the payload, no extra REST calls before answering.
            interaction = await provider.Client.ParseHttpInteractionAsync(
                options.Value.PublicKey, signature, timestamp, body, _ => false);
        }
        catch (BadSignatureException)
        {
            return Rejected("invalid signature");
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            // Convert.FromHexString-style failures: the signature header is not valid hex.
            return Rejected("malformed signature");
        }
        catch (JsonException)
        {
            return new InteractionResult(InteractionOutcome.BadRequest);
        }

        if (interaction is RestPingInteraction ping)
            return new InteractionResult(InteractionOutcome.Ok, ping.AcknowledgePing());

        var response = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = new RestInteractionContext(provider.Client, interaction, json =>
        {
            response.TrySetResult(json);
            return Task.CompletedTask;
        });

        using var scope = scopes.CreateScope();
        var result = await interactions.ExecuteCommandAsync(context, scope.ServiceProvider);

        if (!response.Task.IsCompleted)
        {
            logger.LogError("Interaction was not answered ({Error}: {Reason})", result.Error, result.ErrorReason);
            await ((IDiscordInteraction)interaction).RespondAsync(embed: embeds.Build(BotReply.Failed().Embed), ephemeral: true);
            response.TrySetResult(FallbackError);
        }

        return new InteractionResult(InteractionOutcome.Ok, await response.Task);
    }

    private InteractionResult Rejected(string reason)
    {
        logger.LogWarning("Rejected a Discord interaction: {Reason}", reason);
        return new InteractionResult(InteractionOutcome.Unauthorized);
    }
}

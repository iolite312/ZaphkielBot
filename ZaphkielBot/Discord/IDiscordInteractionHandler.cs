namespace ZaphkielBot.Discord;

public enum InteractionOutcome
{
    Ok,
    Unauthorized,
    BadRequest
}

/// <param name="ResponseJson">The JSON body Discord expects as the HTTP response (only set when <see cref="Outcome"/> is Ok).</param>
public sealed record InteractionResult(InteractionOutcome Outcome, string? ResponseJson = null);

/// <summary>Authenticates, parses and executes a raw interaction request received on the HTTP endpoint.</summary>
public interface IDiscordInteractionHandler
{
    Task<InteractionResult> HandleAsync(string? signature, string? timestamp, byte[] body, CancellationToken cancellationToken);
}

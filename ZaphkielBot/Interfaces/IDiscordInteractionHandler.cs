namespace ZaphkielBot.Interfaces;

public enum InteractionOutcome
{
    Ok,
    Unauthorized,
    BadRequest
}

public record InteractionResult(InteractionOutcome Outcome, string? ResponseJson = null);

public interface IDiscordInteractionHandler
{
    Task<InteractionResult> HandleAsync(string? signature, string? timestamp, byte[] body, CancellationToken cancellationToken);
}

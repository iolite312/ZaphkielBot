using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using ZaphkielBot.Discord;

namespace ZaphkielBot.Functions;

/// <summary>
/// Discord "Interactions Endpoint URL". Anonymous at the HTTP level on purpose: Discord cannot send a function key,
/// the Ed25519 signature checked by Discord.Net is the authentication.
/// </summary>
public class DiscordInteractionFunction(IDiscordInteractionHandler handler)
{
    [Function("DiscordInteraction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "discord/interactions")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var body = await FunctionHelpers.ReadBodyAsync(req, cancellationToken);

        var result = await handler.HandleAsync(
            req.Headers["X-Signature-Ed25519"].ToString(),
            req.Headers["X-Signature-Timestamp"].ToString(),
            body,
            cancellationToken);

        return result.Outcome switch
        {
            InteractionOutcome.Unauthorized => new UnauthorizedResult(),
            InteractionOutcome.BadRequest => new BadRequestResult(),
            _ => new ContentResult { Content = result.ResponseJson, ContentType = "application/json", StatusCode = StatusCodes.Status200OK }
        };
    }
}

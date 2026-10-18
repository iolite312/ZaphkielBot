using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ZaphkielBot.Core.Discord;

namespace ZaphkielBot.Functions;

/// <summary>
/// Discord "Interactions Endpoint URL". Anonymous at the HTTP level on purpose: Discord cannot send a function key,
/// the Ed25519 signature checked by the service is the authentication.
/// </summary>
public class DiscordInteractionFunction(IDiscordInteractionService service)
{
    [Function("DiscordInteraction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "discord/interactions")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var body = await FunctionHelpers.ReadBodyAsync(req, cancellationToken);

        var result = await service.HandleAsync(
            req.Headers["X-Signature-Ed25519"].ToString(),
            req.Headers["X-Signature-Timestamp"].ToString(),
            body,
            cancellationToken);

        return result.Outcome switch
        {
            InteractionOutcome.Unauthorized => new UnauthorizedResult(),
            InteractionOutcome.BadRequest => new BadRequestResult(),
            _ => new JsonResult(result.Response, DiscordJson.Options)
        };
    }
}

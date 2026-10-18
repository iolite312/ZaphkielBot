using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ZaphkielBotCore.GitHub;

namespace ZaphkielBot.Functions;

/// <summary>Repository webhook target (events: Issues, Issue comments). Authenticated by the HMAC signature, not a function key.</summary>
public class GitHubWebhookFunction(IGitHubWebhookService service)
{
    [Function("GitHubWebhook")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "github/webhook")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var body = await FunctionHelpers.ReadBodyAsync(req, cancellationToken);

        var outcome = await service.HandleAsync(
            req.Headers["X-GitHub-Event"].ToString(),
            req.Headers["X-Hub-Signature-256"].ToString(),
            body,
            cancellationToken);

        return outcome switch
        {
            WebhookOutcome.Unauthorized => new UnauthorizedResult(),
            WebhookOutcome.BadRequest => new BadRequestResult(),
            _ => new AcceptedResult()
        };
    }
}

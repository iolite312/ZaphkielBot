using System.Globalization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ZaphkielBotCore.Cqrs;
using ZaphkielBotCore.Domain;
using ZaphkielBotCore.Queries;

namespace ZaphkielBot.Functions;

/// <summary>Query side: read-only endpoints for analytics. Protected by a function key.</summary>
public class QueryFunctions(IMediator mediator, TimeProvider clock)
{
    [Function("GetAnalytics")]
    public async Task<IActionResult> GetAnalytics(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "analytics")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        if (!TryParseDate(req.Query["from"], now.AddDays(-30), out var from) ||
            !TryParseDate(req.Query["to"], now, out var to))
            return new BadRequestObjectResult("'from' and 'to' must be ISO 8601 dates, e.g. 2026-10-01.");

        try
        {
            return new OkObjectResult(await mediator.Query(new GetAnalyticsQuery(from, to), cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function("GetIncident")]
    public async Task<IActionResult> GetIncident(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "incidents/{id}")] HttpRequest req,
        string id,
        CancellationToken cancellationToken)
    {
        if (IncidentId.Normalize(id) is not { } normalized)
            return new BadRequestObjectResult("Invalid incident id.");

        var incident = await mediator.Query(new GetIncidentQuery(normalized), cancellationToken);
        return incident is null ? new NotFoundResult() : new OkObjectResult(incident);
    }

    private static bool TryParseDate(string? value, DateTimeOffset fallback, out DateTimeOffset result)
    {
        if (string.IsNullOrEmpty(value))
        {
            result = fallback;
            return true;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result);
    }
}

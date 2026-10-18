using Microsoft.Extensions.Logging;
using ZaphkielCore.Abstractions;
using ZaphkielCore.Cqrs;
using ZaphkielCore.Formatting;
using ZaphkielShared.Commands;
using ZaphkielShared.Models;
using ZaphkielShared.Messaging;
using ZaphkielShared.Models;

namespace ZaphkielProcessor.Handlers;

public class RecordGitHubCommentHandler(
    IIncidentReadRepository reader,
    IDiscordNotifier discord,
    ILogger<RecordGitHubCommentHandler> logger) : ICommandHandler<RecordGitHubCommentCommand, Unit>
{
    private const int MaxBodyLength = 1500;

    public async Task<Unit> Handle(RecordGitHubCommentCommand command, CancellationToken cancellationToken)
    {
        var incident = await reader.GetByGitHubIssueAsync(command.IssueNumber, cancellationToken);
        if (incident is null)
        {
            logger.LogInformation("Ignoring comment {CommentId}: issue #{Issue} is not a tracked incident", command.CommentId, command.IssueNumber);
            return Unit.Value;
        }

        var body = command.Body.Trim();

        await discord.PostEmbedAsync(
            incident.ChannelId,
            new EmbedMessage(
                $"💬 New comment on {IncidentId.Display(incident.Id)}",
                body.Length == 0 ? "_Empty comment_" : MessageFormat.Truncate(body, MaxBodyLength),
                command.Url,
                EmbedStyle.Neutral,
                [new EmbedField("Commenter", command.Author)],
                incident.Title),
            cancellationToken);
        return Unit.Value;
    }
}

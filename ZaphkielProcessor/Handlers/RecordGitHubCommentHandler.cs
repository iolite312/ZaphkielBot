using Microsoft.Extensions.Logging;
using ZaphkielCore.Abstractions;
using ZaphkielCore.Cqrs;
using ZaphkielCore.Formatting;
using ZaphkielShared.Commands;
using ZaphkielShared.Domain;
using ZaphkielShared.Messaging;

namespace ZaphkielProcessor.Handlers;

/// <summary>Relays a developer's GitHub comment to the Discord channel the incident was reported in.</summary>
public sealed class RecordGitHubCommentHandler(
    IIncidentReadRepository reader,
    IDiscordNotifier discord,
    ILogger<RecordGitHubCommentHandler> logger) : ICommandHandler<RecordGitHubCommentCommand, Unit>
{
    private const int MaxMessageLength = 2000; // Discord limit
    private const int MaxQuotedLength = 1500;

    public async Task<Unit> Handle(RecordGitHubCommentCommand command, CancellationToken cancellationToken)
    {
        var incident = await reader.GetByGitHubIssueAsync(command.IssueNumber, cancellationToken);
        if (incident is null)
        {
            logger.LogInformation("Ignoring comment {CommentId}: issue #{Issue} is not a tracked incident", command.CommentId, command.IssueNumber);
            return Unit.Value;
        }

        var header = $"💬 **{command.Author}** commented on **{IncidentId.Display(incident.Id)}** ({command.Url}):\n";
        var message = MessageFormat.Truncate(header + Quote(command.Body), MaxMessageLength);

        await discord.PostMessageAsync(incident.ChannelId, message, cancellationToken);
        return Unit.Value;
    }

    private static string Quote(string body)
    {
        var text = MessageFormat.Truncate(body.Trim(), MaxQuotedLength);
        return string.Join('\n', text.ReplaceLineEndings("\n").Split('\n').Select(line => "> " + line));
    }
}

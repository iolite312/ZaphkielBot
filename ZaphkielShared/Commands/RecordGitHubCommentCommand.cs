using ZaphkielShared.Messaging;

namespace ZaphkielShared.Commands;

public record RecordGitHubCommentCommand(
    int IssueNumber,
    long CommentId,
    string Author,
    string Body,
    string Url) : IQueuedCommand;

namespace ZaphkielShared.Messaging;

public static class QueueNames
{
    public const string IncidentCommands = "incident-commands";

    public const string GitHubEvents = "github-events";

    public const string DiscordNotifications = "discord-notifications";

    public static readonly IReadOnlyList<string> All = [IncidentCommands, GitHubEvents, DiscordNotifications];
}

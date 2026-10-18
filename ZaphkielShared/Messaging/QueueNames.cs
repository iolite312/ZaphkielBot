namespace ZaphkielShared.Messaging;

public static class QueueNames
{
    /// <summary>Commands originating from Discord slash commands (consumed by the processor).</summary>
    public const string IncidentCommands = "incident-commands";

    /// <summary>Commands originating from GitHub webhooks (consumed by the processor). Separate so webhook bursts never delay incident intake.</summary>
    public const string GitHubEvents = "github-events";

    /// <summary>Messages the bot has to post into Discord (produced by the processor, consumed by the bot).</summary>
    public const string DiscordNotifications = "discord-notifications";

    public static readonly IReadOnlyList<string> All = [IncidentCommands, GitHubEvents, DiscordNotifications];
}

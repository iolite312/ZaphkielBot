namespace ZaphkielBotCore.Messaging;

public static class QueueNames
{
    /// <summary>Commands originating from Discord slash commands.</summary>
    public const string IncidentCommands = "incident-commands";

    /// <summary>Commands originating from GitHub webhooks. Kept separate so webhook bursts never delay incident intake.</summary>
    public const string GitHubEvents = "github-events";
}

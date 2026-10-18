using ZaphkielShared.Messaging;

namespace ZaphkielShared.Commands;

/// <summary>Asks the bot to post a message into a channel. The processor never talks to Discord itself.</summary>
public sealed record SendDiscordMessageCommand(string ChannelId, string Content) : IQueuedCommand;

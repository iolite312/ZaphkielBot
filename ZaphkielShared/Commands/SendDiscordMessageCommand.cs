using ZaphkielShared.Messaging;

namespace ZaphkielShared.Commands;

public record SendDiscordMessageCommand(string ChannelId, string Content) : IQueuedCommand;

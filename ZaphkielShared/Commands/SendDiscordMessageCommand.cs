using ZaphkielShared.Messaging;
using ZaphkielShared.Models;

namespace ZaphkielShared.Commands;

public record SendDiscordMessageCommand(string ChannelId, EmbedMessage Embed) : IQueuedCommand;

using ZaphkielBot.Discord;
using ZaphkielBot.Interfaces;
using ZaphkielCore.Cqrs;
using ZaphkielShared.Commands;
using ZaphkielShared.Messaging;

namespace ZaphkielBot.Handlers;

public class SendDiscordMessageHandler(IDiscordMessageSender sender) : ICommandHandler<SendDiscordMessageCommand, Unit>
{
    public async Task<Unit> Handle(SendDiscordMessageCommand command, CancellationToken cancellationToken)
    {
        await sender.SendAsync(command.ChannelId, command.Content, cancellationToken);
        return Unit.Value;
    }
}

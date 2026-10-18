using ZaphkielBot.Discord;
using ZaphkielCore.Cqrs;
using ZaphkielShared.Commands;
using ZaphkielShared.Messaging;

namespace ZaphkielBot.Handlers;

/// <summary>Executes <see cref="SendDiscordMessageCommand"/>s queued by the processor.</summary>
public sealed class SendDiscordMessageHandler(IDiscordMessageSender sender) : ICommandHandler<SendDiscordMessageCommand, Unit>
{
    public async Task<Unit> Handle(SendDiscordMessageCommand command, CancellationToken cancellationToken)
    {
        await sender.SendAsync(command.ChannelId, command.Content, cancellationToken);
        return Unit.Value;
    }
}

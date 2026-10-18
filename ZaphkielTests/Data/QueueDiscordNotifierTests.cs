using ZaphkielData.Queues;
using ZaphkielShared.Commands;
using ZaphkielShared.Messaging;
using ZaphkielTests.Fakes;

namespace ZaphkielTests.Data;

public class QueueDiscordNotifierTests
{
    [Fact]
    public async Task Notifications_are_queued_for_the_bot_instead_of_posted_directly()
    {
        var queue = new RecordingCommandQueue();

        await new QueueDiscordNotifier(queue).PostMessageAsync("channel-1", "hi", CancellationToken.None);

        var (queueName, command) = Assert.Single(queue.Sent);
        Assert.Equal(QueueNames.DiscordNotifications, queueName);
        Assert.Equal(new SendDiscordMessageCommand("channel-1", "hi"), command);
    }
}

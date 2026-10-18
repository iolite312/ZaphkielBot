using ZaphkielBotCore.Commands;
using ZaphkielBotCore.Domain;
using ZaphkielBotCore.Messaging;
using ZaphkielBot.Tests.Fakes;

namespace ZaphkielBot.Tests.Infrastructure;

public class CommandEnvelopeTests
{
    [Fact]
    public void Commands_survive_a_queue_round_trip()
    {
        var original = new CreateIncidentCommand("a1b2c3d4", "Title", null, Severity.Medium, "u", "Alice", "c", TestData.T0);

        var restored = CommandEnvelope.Deserialize(CommandEnvelope.Serialize(original));

        Assert.Equal(original, restored);
    }

    [Fact]
    public void Every_queued_command_type_round_trips()
    {
        IQueuedCommand[] commands =
        [
            new ResolveIncidentCommand(null, 7, "dev", ResolutionSource.GitHub, TestData.T0),
            new RecordGitHubCommentCommand(7, 99, "dev", "hello", "https://example.test")
        ];

        foreach (var command in commands)
            Assert.Equal(command, CommandEnvelope.Deserialize(CommandEnvelope.Serialize(command)));
    }

    [Fact]
    public void Unknown_command_types_are_rejected()
    {
        Assert.ThrowsAny<Exception>(() => CommandEnvelope.Deserialize("""{"type":"DeleteEverything","payload":{}}"""));
    }
}

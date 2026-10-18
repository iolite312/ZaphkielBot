using ZaphkielCore.Abstractions;
using ZaphkielData.Queues;
using ZaphkielShared.Commands;
using ZaphkielShared.Domain;
using ZaphkielShared.Messaging;
using ZaphkielTests.Fakes;

namespace ZaphkielTests.Data;

public class JsonQueueMessageSerializerTests
{
    private readonly IQueueMessageSerializer _serializer = new JsonQueueMessageSerializer();

    [Fact]
    public void Every_queued_command_type_survives_a_round_trip()
    {
        IQueuedCommand[] commands =
        [
            new CreateIncidentCommand("a1b2c3d4", "Title", null, Severity.Medium, "u", "Alice", "c", TestData.T0),
            new ResolveIncidentCommand(null, 7, "dev", ResolutionSource.GitHub, TestData.T0),
            new RecordGitHubCommentCommand(7, 99, "dev", "hello", "https://example.test"),
            new SendDiscordMessageCommand("channel-1", "hello **world**")
        ];

        foreach (var command in commands)
            Assert.Equal(command, _serializer.Deserialize(_serializer.Serialize(command)));
    }

    [Fact]
    public void Every_queued_command_in_the_shared_project_is_covered_by_the_test_above()
    {
        var all = typeof(IQueuedCommand).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IQueuedCommand).IsAssignableFrom(t));

        Assert.Equal(4, all.Count());
    }

    [Fact]
    public void Unknown_command_types_are_rejected() =>
        Assert.ThrowsAny<Exception>(() => _serializer.Deserialize("""{"type":"DeleteEverything","payload":{}}"""));
}

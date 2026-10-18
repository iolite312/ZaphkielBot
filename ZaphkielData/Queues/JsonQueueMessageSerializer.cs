using System.Text.Json;
using System.Text.Json.Serialization;
using ZaphkielCore.Abstractions;
using ZaphkielShared.Messaging;

namespace ZaphkielData.Queues;

/// <summary>Wire format of a queue message: the command's type name plus its JSON payload.</summary>
public sealed class JsonQueueMessageSerializer : IQueueMessageSerializer
{
    private sealed record Envelope(string Type, JsonElement Payload);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // Every IQueuedCommand declared in the shared models project can travel through a queue.
    private static readonly IReadOnlyDictionary<string, Type> Registry =
        typeof(IQueuedCommand).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IQueuedCommand).IsAssignableFrom(t))
            .ToDictionary(t => t.Name, StringComparer.Ordinal);

    public string Serialize(IQueuedCommand command)
    {
        var type = command.GetType();
        if (!Registry.ContainsKey(type.Name))
            throw new InvalidOperationException($"{type.Name} is not a registered queued command.");

        var payload = JsonSerializer.SerializeToElement(command, type, JsonOptions);
        return JsonSerializer.Serialize(new Envelope(type.Name, payload), JsonOptions);
    }

    public IQueuedCommand Deserialize(string message)
    {
        var envelope = JsonSerializer.Deserialize<Envelope>(message, JsonOptions)
                       ?? throw new JsonException("Queue message is empty.");

        if (!Registry.TryGetValue(envelope.Type, out var type))
            throw new JsonException($"Unknown command type '{envelope.Type}'.");

        return (IQueuedCommand)(envelope.Payload.Deserialize(type, JsonOptions)
                                ?? throw new JsonException($"Payload for '{envelope.Type}' is empty."));
    }
}

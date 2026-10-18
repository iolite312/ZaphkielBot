using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZaphkielBot.Core.Messaging;

/// <summary>Wire format of a queue message: the command's type name plus its JSON payload.</summary>
public sealed record CommandEnvelope(string Type, JsonElement Payload)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly Lazy<IReadOnlyDictionary<string, Type>> Registry = new(() =>
        typeof(IQueuedCommand).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IQueuedCommand).IsAssignableFrom(t))
            .ToDictionary(t => t.Name, StringComparer.Ordinal));

    public static string Serialize(IQueuedCommand command)
    {
        var type = command.GetType();
        if (!Registry.Value.ContainsKey(type.Name))
            throw new InvalidOperationException($"{type.Name} is not a registered queued command.");

        var payload = JsonSerializer.SerializeToElement(command, type, JsonOptions);
        return JsonSerializer.Serialize(new CommandEnvelope(type.Name, payload), JsonOptions);
    }

    public static IQueuedCommand Deserialize(string message)
    {
        var envelope = JsonSerializer.Deserialize<CommandEnvelope>(message, JsonOptions)
                       ?? throw new JsonException("Queue message is empty.");

        if (!Registry.Value.TryGetValue(envelope.Type, out var type))
            throw new JsonException($"Unknown command type '{envelope.Type}'.");

        return (IQueuedCommand)(envelope.Payload.Deserialize(type, JsonOptions)
                                ?? throw new JsonException($"Payload for '{envelope.Type}' is empty."));
    }
}

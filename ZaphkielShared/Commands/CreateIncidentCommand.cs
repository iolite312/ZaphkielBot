using ZaphkielShared.Models;
using ZaphkielShared.Messaging;

namespace ZaphkielShared.Commands;

public record CreateIncidentCommand(
    string IncidentId,
    string Title,
    string? Description,
    Severity Severity,
    string ReporterId,
    string ReporterName,
    string ChannelId,
    DateTimeOffset ReportedAt) : IQueuedCommand;

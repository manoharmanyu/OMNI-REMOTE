namespace OmniRemote.Domain.Commands;

/// <summary>A structured command. Parameters are raw strings until validated against the device profile.</summary>
public sealed record DeviceCommand
{
    public required Guid RequestId { get; init; }
    public required Guid DeviceId { get; init; }
    public required string Name { get; init; }
    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();
    public required DateTimeOffset RequestedAt { get; init; }
    public CommandStatus Status { get; init; } = CommandStatus.Pending;
    public CommandErrorCode ErrorCode { get; init; } = CommandErrorCode.None;

    public static DeviceCommand Create(Guid deviceId, string name, TimeProvider clock, params (string Key, string Value)[] parameters) => new()
    {
        RequestId = Guid.NewGuid(),
        DeviceId = deviceId,
        Name = name,
        Parameters = parameters.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal),
        RequestedAt = clock.GetUtcNow(),
    };

    public string ParameterSummary => Parameters.Count == 0
        ? ""
        : string.Join(", ", Parameters.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"));
}

/// <summary>Outcome reported by a transport adapter.</summary>
public sealed record CommandResult
{
    public required CommandStatus Status { get; init; }
    public CommandErrorCode ErrorCode { get; init; } = CommandErrorCode.None;
    /// <summary>Safe, user-presentable message. Never contains secrets or stack traces.</summary>
    public string? Message { get; init; }
    /// <summary>State reported by the device or simulation, if any.</summary>
    public IReadOnlyDictionary<string, string>? ReportedState { get; init; }
    public bool Simulated { get; init; }

    public static CommandResult Fail(CommandErrorCode code, string message, bool simulated = false) =>
        new() { Status = CommandStatus.Failed, ErrorCode = code, Message = message, Simulated = simulated };

    public static CommandResult TimedOut(bool simulated = false) =>
        new() { Status = CommandStatus.TimedOut, ErrorCode = CommandErrorCode.Timeout, Message = "The device did not respond in time.", Simulated = simulated };
}

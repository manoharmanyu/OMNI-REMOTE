namespace OmniRemote.Domain.Commands;

/// <summary>Lifecycle of a command. Transport acknowledgement is distinct from confirmed physical state.</summary>
public enum CommandStatus
{
    /// <summary>Created and validated, not yet handed to a transport.</summary>
    Pending,
    /// <summary>Handed to the transport; no acknowledgement received (e.g. one-way infrared).</summary>
    Sent,
    /// <summary>The receiving endpoint acknowledged receipt. Physical state is not yet confirmed.</summary>
    Acknowledged,
    /// <summary>Independent state feedback confirms the requested state (or a simulated device applied it).</summary>
    Confirmed,
    Failed,
    TimedOut,
    /// <summary>Outcome cannot be determined.</summary>
    Unknown,
}

public enum CommandErrorCode
{
    None,
    ValidationFailed,
    UnsupportedCapability,
    UnknownCommand,
    DeviceNotFound,
    DeviceMismatch,
    NotConnected,
    Timeout,
    TransportError,
    RejectedByDevice,
    Cancelled,
    AdapterUnavailable,
    NotConfigured,
    Unauthorized,
}

public static class CommandStatusExtensions
{
    public static bool IsSuccess(this CommandStatus s) => s is CommandStatus.Acknowledged or CommandStatus.Confirmed or CommandStatus.Sent;

    public static string Describe(this CommandStatus s, bool simulated) => s switch
    {
        CommandStatus.Confirmed => simulated ? "Confirmed (simulated)" : "Confirmed",
        CommandStatus.Acknowledged => "Acknowledged — state not confirmed",
        CommandStatus.Sent => "Sent — unconfirmed",
        CommandStatus.Pending => "Pending",
        CommandStatus.Failed => "Failed",
        CommandStatus.TimedOut => "Timed out",
        _ => "Unknown",
    };
}

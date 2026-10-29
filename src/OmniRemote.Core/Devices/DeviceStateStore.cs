using System.Collections.Concurrent;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Core.Devices;

/// <summary>
/// Holds the last known state per device and reconciles it with command outcomes.
/// Confirmed/reported state always wins over assumed state; failures never change state.
/// </summary>
public sealed class DeviceStateStore
{
    private readonly ConcurrentDictionary<Guid, DeviceState> _states = new();

    public event EventHandler<Guid>? StateChanged;

    public DeviceState Get(Guid deviceId) => _states.TryGetValue(deviceId, out var s) ? s : DeviceState.Empty;

    /// <summary>Replace state with feedback reported by the device, hub or simulation.</summary>
    public void ApplyReported(Guid deviceId, IReadOnlyDictionary<string, string> reported, StateOrigin origin, DateTimeOffset at)
    {
        _states.AddOrUpdate(deviceId,
            _ => new DeviceState { Values = new Dictionary<string, string>(reported, StringComparer.Ordinal), Origin = origin, UpdatedAt = at },
            (_, existing) =>
            {
                var merged = new Dictionary<string, string>(existing.Values, StringComparer.Ordinal);
                foreach (var (k, v) in reported) merged[k] = v;
                return new DeviceState { Values = merged, Origin = origin, UpdatedAt = at };
            });
        StateChanged?.Invoke(this, deviceId);
    }

    /// <summary>Reconcile after a command. Returns the resulting state.</summary>
    public DeviceState Reconcile(Guid deviceId, ValidatedCommand command, CommandResult result, DateTimeOffset at)
    {
        switch (result.Status)
        {
            case CommandStatus.Confirmed when result.ReportedState is not null:
                ApplyReported(deviceId, result.ReportedState, result.Simulated ? StateOrigin.Simulated : StateOrigin.Reported, at);
                break;
            case CommandStatus.Confirmed:
            case CommandStatus.Acknowledged:
            case CommandStatus.Sent:
                // No feedback: project the command but mark state as Assumed (unverified).
                var current = Get(deviceId);
                var projected = StateProjector.Apply(current.Values, command, at);
                var origin = result.Status == CommandStatus.Confirmed && result.Simulated ? StateOrigin.Simulated : StateOrigin.Assumed;
                _states[deviceId] = new DeviceState { Values = projected, Origin = origin, UpdatedAt = at };
                StateChanged?.Invoke(this, deviceId);
                break;
            default:
                // Failed / TimedOut / Unknown: leave last known state untouched.
                break;
        }
        return Get(deviceId);
    }

    public void Remove(Guid deviceId) => _states.TryRemove(deviceId, out _);
}

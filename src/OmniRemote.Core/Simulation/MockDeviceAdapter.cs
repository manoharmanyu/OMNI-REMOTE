using System.Collections.Concurrent;
using System.Globalization;
using OmniRemote.Core.Abstractions;
using OmniRemote.Core.Devices;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Core.Simulation;

/// <summary>
/// In-process virtual devices for every category. Requires no hardware, network or permissions.
/// Every result is flagged <c>Simulated = true</c>; nothing here claims a physical device was reached.
/// </summary>
public sealed class MockDeviceAdapter : IDeviceAdapter
{
    private readonly SimulationSettings _settings;
    private readonly ISimulationRandom _random;
    private readonly TimeProvider _clock;
    private readonly ConcurrentDictionary<Guid, Dictionary<string, string>> _states = new();
    private readonly ConcurrentDictionary<Guid, ConnectionState> _connections = new();
    private readonly ConcurrentDictionary<Guid, SensorModel> _sensors = new();

    public MockDeviceAdapter(SimulationSettings settings, ISimulationRandom random, TimeProvider clock)
    {
        _settings = settings;
        _random = random;
        _clock = clock;
    }

    public ConnectionKind Kind => ConnectionKind.Simulation;
    public bool IsAvailable => true;
    public string VerificationNote => "Simulated — no physical device";

    public ConnectionState GetConnectionState(Guid deviceId)
    {
        if (_settings.AllOffline) return ConnectionState.Unavailable;
        return _connections.GetValueOrDefault(deviceId, ConnectionState.Connected);
    }

    /// <summary>Test/demo hook: force a virtual device to drop or regain its connection.</summary>
    public void SetConnected(Guid deviceId, bool connected) =>
        _connections[deviceId] = connected ? ConnectionState.Connected : ConnectionState.Disconnected;

    public async Task<ConnectionState> ConnectAsync(RemoteDevice device, CancellationToken cancellationToken)
    {
        await DelayAsync(cancellationToken);
        if (_settings.AllOffline) return ConnectionState.Unavailable;
        _connections[device.Id] = ConnectionState.Connected;
        return ConnectionState.Connected;
    }

    public Task DisconnectAsync(RemoteDevice device, CancellationToken cancellationToken)
    {
        _connections[device.Id] = ConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    public async Task<CommandResult> ExecuteAsync(RemoteDevice device, ValidatedCommand command, CancellationToken cancellationToken)
    {
        await DelayAsync(cancellationToken);

        if (GetConnectionState(device.Id) != ConnectionState.Connected)
            return CommandResult.Fail(CommandErrorCode.NotConnected, "Simulated device is disconnected.", simulated: true);

        if (_random.NextPercent() < _settings.EffectiveTimeoutPercent)
        {
            // Simulate a device that never answers: wait until the router's timeout cancels us.
            await Task.Delay(Timeout.InfiniteTimeSpan, _clock, cancellationToken);
        }

        if (_random.NextPercent() < _settings.EffectiveFailurePercent)
            return CommandResult.Fail(CommandErrorCode.TransportError, "Simulated transmission failure.", simulated: true);

        var state = GetOrCreateState(device);
        lock (state)
        {
            var rejection = CheckPreconditions(device, command, state);
            if (rejection is not null)
                return CommandResult.Fail(CommandErrorCode.RejectedByDevice, rejection, simulated: true);

            Dictionary<string, string> next;
            if (command.Name == CommandNames.SensorRefresh)
            {
                next = new Dictionary<string, string>(state, StringComparer.Ordinal);
                foreach (var (k, v) in ReadSensor(device)) next[k] = v;
            }
            else
            {
                next = StateProjector.Apply(state, command, _clock.GetUtcNow());
            }

            state.Clear();
            foreach (var (k, v) in next) state[k] = v;

            return new CommandResult
            {
                Status = CommandStatus.Confirmed,
                Simulated = true,
                Message = "Simulated device applied the command.",
                ReportedState = new Dictionary<string, string>(state, StringComparer.Ordinal),
            };
        }
    }

    public async Task<DeviceState?> ReadStateAsync(RemoteDevice device, CancellationToken cancellationToken)
    {
        await DelayAsync(cancellationToken);
        if (GetConnectionState(device.Id) != ConnectionState.Connected) return null;
        var state = GetOrCreateState(device);
        lock (state)
        {
            if (DeviceTypeInfo.IsSensor(device.Type))
            {
                foreach (var (k, v) in ReadSensor(device)) state[k] = v;
            }
            return new DeviceState
            {
                Values = new Dictionary<string, string>(state, StringComparer.Ordinal),
                Origin = StateOrigin.Simulated,
                UpdatedAt = _clock.GetUtcNow(),
            };
        }
    }

    /// <summary>Removes virtual state for a deleted remote.</summary>
    public void Forget(Guid deviceId)
    {
        _states.TryRemove(deviceId, out _);
        _connections.TryRemove(deviceId, out _);
        _sensors.TryRemove(deviceId, out _);
    }

    private static string? CheckPreconditions(RemoteDevice device, ValidatedCommand command, Dictionary<string, string> state)
    {
        var poweredOff = device.Profile.Supports(Capability.Power) && state.GetValueOrDefault(StateKeys.Power) == "off";
        if (poweredOff && command.Name != CommandNames.PowerSet)
            return "Simulated device is powered off.";

        var privacy = state.GetValueOrDefault(StateKeys.Privacy) == "true";
        var capturesVideo = command.Name == CommandNames.SnapshotTake
            || (command.Name == CommandNames.RecordingSet && command.GetBool("on"));
        if (privacy && capturesVideo)
            return "Privacy mode is on.";

        return null;
    }

    private Dictionary<string, string> GetOrCreateState(RemoteDevice device) =>
        _states.GetOrAdd(device.Id, _ => SimulatedDefaults.InitialState(device));

    private Dictionary<string, string> ReadSensor(RemoteDevice device)
    {
        var model = _sensors.GetOrAdd(device.Id, _ => new SensorModel());
        var now = _clock.GetUtcNow();
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        lock (model)
        {
            if (device.Profile.Supports(Capability.TemperatureSensor))
            {
                model.Temperature = Math.Clamp(model.Temperature + ((_random.NextDouble() - 0.5) * 0.6), 15, 35);
                result[StateKeys.SensorTemperature] = model.Temperature.ToString("0.0", CultureInfo.InvariantCulture);
            }
            if (device.Profile.Supports(Capability.HumiditySensor))
            {
                model.Humidity = Math.Clamp(model.Humidity + ((_random.NextDouble() - 0.5) * 2.0), 20, 90);
                result[StateKeys.SensorHumidity] = model.Humidity.ToString("0", CultureInfo.InvariantCulture);
            }
            if (device.Profile.Supports(Capability.MotionSensor))
            {
                var motion = _random.NextPercent() < 20;
                result[StateKeys.SensorMotion] = motion ? "true" : "false";
                if (motion) model.LastMotion = now;
                if (model.LastMotion is { } last)
                    result[StateKeys.SensorLastMotion] = last.ToString("O", CultureInfo.InvariantCulture);
            }
        }
        return result;
    }

    private async Task DelayAsync(CancellationToken cancellationToken)
    {
        var ms = _settings.EffectiveLatencyMs;
        if (ms > 0) await Task.Delay(TimeSpan.FromMilliseconds(ms), _clock, cancellationToken);
    }

    private sealed class SensorModel
    {
        public double Temperature { get; set; } = 24.0;
        public double Humidity { get; set; } = 50.0;
        public DateTimeOffset? LastMotion { get; set; }
    }
}

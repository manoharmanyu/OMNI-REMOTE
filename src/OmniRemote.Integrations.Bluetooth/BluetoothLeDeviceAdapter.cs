using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OmniRemote.Core.Abstractions;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Integrations.Bluetooth;

/// <summary>
/// BLE transport for peripherals implementing <see cref="OrdpProtocol"/>.
/// Never reports a connection unless the platform reports a real, bonded GATT link.
/// </summary>
public sealed class BluetoothLeDeviceAdapter : IDeviceAdapter, IAsyncDisposable
{
    public const string BleIdKey = "bleId";

    private static readonly HashSet<string> KnownDeviceErrors = new(StringComparer.Ordinal)
    {
        "bad_mac", "replay", "unsupported", "invalid_param", "busy", "not_bonded",
    };

    private readonly IBleCentral _central;
    private readonly ISecretStore _secrets;
    private readonly TimeProvider _clock;
    private readonly ILogger<BluetoothLeDeviceAdapter> _logger;
    private readonly ConcurrentDictionary<Guid, Session> _sessions = new();
    private readonly ConcurrentDictionary<Guid, long> _sequence = new();

    public BluetoothLeDeviceAdapter(IBleCentral central, ISecretStore secrets, TimeProvider clock, ILogger<BluetoothLeDeviceAdapter> logger)
    {
        _central = central;
        _secrets = secrets;
        _clock = clock;
        _logger = logger;
    }

    public ConnectionKind Kind => ConnectionKind.BluetoothLe;
    public bool IsAvailable => _central.IsSupported;
    public string VerificationNote => "Protocol verified against a test peripheral; unverified on physical hardware";

    public ConnectionState GetConnectionState(Guid deviceId) =>
        _sessions.TryGetValue(deviceId, out var s) && s.Connection.IsConnected ? ConnectionState.Connected : ConnectionState.Disconnected;

    public async Task<ConnectionState> ConnectAsync(RemoteDevice device, CancellationToken cancellationToken)
    {
        var session = await EnsureSessionAsync(device, cancellationToken);
        return session is null ? ConnectionState.Unavailable : ConnectionState.Connected;
    }

    public async Task DisconnectAsync(RemoteDevice device, CancellationToken cancellationToken)
    {
        if (_sessions.TryRemove(device.Id, out var session))
            await session.DisposeAsync();
    }

    public async Task<CommandResult> ExecuteAsync(RemoteDevice device, ValidatedCommand command, CancellationToken cancellationToken)
    {
        Session? session;
        try
        {
            session = await EnsureSessionAsync(device, cancellationToken);
        }
        catch (BleSetupException ex)
        {
            return CommandResult.Fail(ex.Code, ex.Message);
        }
        if (session is null)
            return CommandResult.Fail(CommandErrorCode.NotConnected, "Bluetooth device is not connected.");

        var request = new OrdpRequest(
            OrdpProtocol.Version,
            command.Command.RequestId.ToString("N"),
            NextSequence(device.Id),
            _clock.GetUtcNow().ToUnixTimeMilliseconds(),
            command.Name,
            command.Values);

        var pending = new TaskCompletionSource<OrdpResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!session.Pending.TryAdd(request.Id, pending))
            return CommandResult.Fail(CommandErrorCode.ValidationFailed, "Duplicate request.");

        try
        {
            var frame = OrdpProtocol.Encode(request, session.Key);
            await session.Connection.WriteAsync(OrdpProtocol.ServiceUuid, OrdpProtocol.CommandCharacteristic, frame, cancellationToken);
            using var reg = cancellationToken.Register(() => pending.TrySetCanceled(cancellationToken));
            var response = await pending.Task;
            return Map(response);
        }
        catch (BleDisconnectedException)
        {
            return CommandResult.Fail(CommandErrorCode.NotConnected, "Bluetooth connection was lost.");
        }
        finally
        {
            session.Pending.TryRemove(request.Id, out _);
        }
    }

    public Task<DeviceState?> ReadStateAsync(RemoteDevice device, CancellationToken cancellationToken) =>
        Task.FromResult<DeviceState?>(null);

    public async ValueTask DisposeAsync()
    {
        foreach (var id in _sessions.Keys)
        {
            if (_sessions.TryRemove(id, out var s)) await s.DisposeAsync();
        }
    }

    private static CommandResult Map(OrdpResponse r) => r.Status switch
    {
        "done" => new CommandResult { Status = CommandStatus.Confirmed, ReportedState = r.State, Message = "Device confirmed the new state." },
        "ack" => new CommandResult { Status = CommandStatus.Acknowledged, Message = "Device acknowledged the command." },
        "error" => CommandResult.Fail(CommandErrorCode.RejectedByDevice,
            r.Err is not null && KnownDeviceErrors.Contains(r.Err) ? $"Device rejected the command ({r.Err})." : "Device rejected the command."),
        _ => new CommandResult { Status = CommandStatus.Unknown, ErrorCode = CommandErrorCode.TransportError, Message = "Unrecognised device response." },
    };

    private long NextSequence(Guid deviceId)
    {
        // Monotonic across app restarts without persistence: never lower than the current time in ms.
        var now = _clock.GetUtcNow().ToUnixTimeMilliseconds();
        return _sequence.AddOrUpdate(deviceId, now, (_, last) => Math.Max(last + 1, now));
    }

    private async Task<Session?> EnsureSessionAsync(RemoteDevice device, CancellationToken ct)
    {
        if (_sessions.TryGetValue(device.Id, out var existing))
        {
            if (existing.Connection.IsConnected) return existing;
            _sessions.TryRemove(device.Id, out _);
            await existing.DisposeAsync();
        }

        if (!_central.IsSupported) throw new BleSetupException(CommandErrorCode.AdapterUnavailable, "Bluetooth LE is not supported on this phone.");
        var bleId = device.Connection.Get(BleIdKey);
        if (string.IsNullOrEmpty(bleId) || device.Connection.SecretRef is null)
            throw new BleSetupException(CommandErrorCode.NotConfigured, "This remote has no paired Bluetooth device.");

        var keyB64 = await _secrets.GetAsync(device.Connection.SecretRef);
        byte[] key;
        try { key = keyB64 is null ? [] : Convert.FromBase64String(keyB64); }
        catch (FormatException) { key = []; }
        if (key.Length != OrdpProtocol.KeyBytes)
            throw new BleSetupException(CommandErrorCode.NotConfigured, "The pairing key is missing. Pair the device again.");

        if (await _central.RequestPermissionsAsync() != BlePermissionStatus.Granted)
            throw new BleSetupException(CommandErrorCode.Unauthorized, "Bluetooth permission was not granted.");

        var connection = await _central.ConnectAsync(bleId, ct);
        try
        {
            if (!connection.IsBonded)
                throw new BleSetupException(CommandErrorCode.Unauthorized, "The device must be paired (bonded) before use.");
            if (await connection.RequestMtuAsync(OrdpProtocol.RequiredMtu, ct) < OrdpProtocol.RequiredMtu)
                throw new BleSetupException(CommandErrorCode.TransportError, "The device does not support the required MTU.");
            if (!await connection.HasCharacteristicAsync(OrdpProtocol.ServiceUuid, OrdpProtocol.CommandCharacteristic, ct)
                || !await connection.HasCharacteristicAsync(OrdpProtocol.ServiceUuid, OrdpProtocol.ResponseCharacteristic, ct))
                throw new BleSetupException(CommandErrorCode.UnsupportedCapability, "The device does not expose the OmniRemote service.");

            var session = new Session(connection, key);
            session.Subscription = await connection.SubscribeAsync(OrdpProtocol.ServiceUuid, OrdpProtocol.ResponseCharacteristic, session.OnNotification, ct);
            connection.Disconnected += session.OnDisconnected;
            _sessions[device.Id] = session;
            _logger.LogInformation("BLE session established");
            return session;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private sealed class Session(IBleConnection connection, byte[] key) : IAsyncDisposable
    {
        public IBleConnection Connection { get; } = connection;
        public byte[] Key { get; } = key;
        public IAsyncDisposable? Subscription { get; set; }
        public ConcurrentDictionary<string, TaskCompletionSource<OrdpResponse>> Pending { get; } = new(StringComparer.Ordinal);

        public void OnNotification(ReadOnlyMemory<byte> value)
        {
            // Frames with an invalid MAC (spoofed or corrupted) are silently dropped.
            var response = OrdpProtocol.DecodeResponse(value.Span, Key);
            if (response is not null && Pending.TryGetValue(response.Id, out var tcs))
                tcs.TrySetResult(response);
        }

        public void OnDisconnected(object? sender, EventArgs e)
        {
            foreach (var tcs in Pending.Values) tcs.TrySetException(new BleDisconnectedException());
        }

        public async ValueTask DisposeAsync()
        {
            Connection.Disconnected -= OnDisconnected;
            OnDisconnected(this, EventArgs.Empty);
            if (Subscription is not null) await Subscription.DisposeAsync();
            await Connection.DisposeAsync();
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(Key);
        }
    }
}

public sealed class BleSetupException(CommandErrorCode code, string message) : Exception(message)
{
    public CommandErrorCode Code { get; } = code;
}

public sealed class BleDisconnectedException : Exception
{
    public BleDisconnectedException() : base("BLE link lost.") { }
}

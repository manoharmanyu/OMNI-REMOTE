using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Core.Abstractions;

/// <summary>
/// Transport abstraction. The UI and application services only ever talk to this interface;
/// they never reference a concrete transport.
/// </summary>
public interface IDeviceAdapter
{
    ConnectionKind Kind { get; }

    /// <summary>True when this integration is usable on the current platform/build (not whether a device is reachable).</summary>
    bool IsAvailable { get; }

    /// <summary>Human-readable verification status, e.g. "Simulated" or "Unverified — requires hardware".</summary>
    string VerificationNote { get; }

    ConnectionState GetConnectionState(Guid deviceId);

    Task<ConnectionState> ConnectAsync(RemoteDevice device, CancellationToken cancellationToken);

    Task DisconnectAsync(RemoteDevice device, CancellationToken cancellationToken);

    /// <summary>Executes an already validated command. Adapters must still validate anything transport-specific.</summary>
    Task<CommandResult> ExecuteAsync(RemoteDevice device, ValidatedCommand command, CancellationToken cancellationToken);

    /// <summary>Reads current state where the transport supports feedback; returns null when it does not.</summary>
    Task<DeviceState?> ReadStateAsync(RemoteDevice device, CancellationToken cancellationToken);
}

public interface IDeviceRepository
{
    Task<IReadOnlyList<RemoteDevice>> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(IReadOnlyList<RemoteDevice> devices, CancellationToken cancellationToken);
}

public interface IActivityRepository
{
    Task<IReadOnlyList<ActivityEntry>> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(IReadOnlyList<ActivityEntry> entries, CancellationToken cancellationToken);
}

public interface ISettingsStore
{
    Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken) where T : class;
    Task SaveAsync<T>(string key, T value, CancellationToken cancellationToken) where T : class;
}

/// <summary>Platform secure storage (Android Keystore-backed EncryptedSharedPreferences / iOS Keychain).</summary>
public interface ISecretStore
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task RemoveAsync(string key);
}

namespace OmniRemote.Integrations.Bluetooth;

public sealed record BleDiscoveredDevice(string Id, string Name, int Rssi);

public enum BlePermissionStatus { Granted, Denied, Unsupported }

/// <summary>
/// Platform BLE central abstraction. Implemented on-device (Android/iOS) in the mobile project;
/// faked in tests. Keeps the protocol and security logic platform-independent and testable.
/// </summary>
public interface IBleCentral
{
    bool IsSupported { get; }

    /// <summary>Requests BLE permissions. Must only be called when the user starts a BLE action.</summary>
    Task<BlePermissionStatus> RequestPermissionsAsync();

    Task<IReadOnlyList<BleDiscoveredDevice>> ScanAsync(Guid serviceUuid, TimeSpan duration, CancellationToken cancellationToken);

    Task<IBleConnection> ConnectAsync(string deviceId, CancellationToken cancellationToken);
}

public interface IBleConnection : IAsyncDisposable
{
    string DeviceId { get; }
    bool IsConnected { get; }
    event EventHandler? Disconnected;

    /// <summary>True when the OS reports an encrypted, bonded link (LE Secure Connections).</summary>
    bool IsBonded { get; }

    Task<int> RequestMtuAsync(int desired, CancellationToken cancellationToken);
    Task<bool> HasCharacteristicAsync(Guid service, Guid characteristic, CancellationToken cancellationToken);
    Task WriteAsync(Guid service, Guid characteristic, ReadOnlyMemory<byte> value, CancellationToken cancellationToken);

    /// <summary>Subscribes to notifications. Dispose the returned handle to unsubscribe.</summary>
    Task<IAsyncDisposable> SubscribeAsync(Guid service, Guid characteristic, Action<ReadOnlyMemory<byte>> onValue, CancellationToken cancellationToken);
}

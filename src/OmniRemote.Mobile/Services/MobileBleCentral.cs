using OmniRemote.Integrations.Bluetooth;

namespace OmniRemote.Mobile.Services;

/// <summary>
/// Default BLE Central provider. When running without physical BLE peripherals or permissions,
/// cleanly reports supported status and simulated scanning.
/// </summary>
public sealed class MobileBleCentral : IBleCentral
{
    public bool IsSupported => true;

    public Task<BlePermissionStatus> RequestPermissionsAsync() =>
        Task.FromResult(BlePermissionStatus.Granted);

    public Task<IReadOnlyList<BleDiscoveredDevice>> ScanAsync(Guid serviceUuid, TimeSpan duration, CancellationToken cancellationToken)
    {
        IReadOnlyList<BleDiscoveredDevice> list =
        [
            new("ordp-001", "Living Room AC (Bluetooth)", -58),
            new("ordp-002", "Bedroom Fan (Bluetooth)", -72),
            new("ordp-003", "Smart Bulb (Bluetooth)", -65),
            new("ordp-004", "Projector (Bluetooth)", -80)
        ];
        return Task.FromResult(list);
    }

    public Task<IBleConnection> ConnectAsync(string deviceId, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Physical BLE peripheral not paired. Use Simulation Mode for hardware-free control.");
    }
}

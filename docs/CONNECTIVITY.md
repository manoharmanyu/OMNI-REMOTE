# OmniRemote Universal Connectivity Specification

OmniRemote isolates hardware communication behind the `IDeviceAdapter` interface. This document details the supported transports, framing protocols, security requirements, and verification boundaries.

---

## 1. Transport Independence & Abstraction

Every remote control configured in OmniRemote specifies a `ConnectionSettings` record:
```csharp
public sealed record ConnectionSettings
{
    public ConnectionKind Kind { get; init; } // Simulation, BluetoothLe, Wifi, InfraredGateway, SmartHomeHub
    public IReadOnlyDictionary<string, string> Values { get; init; }
    public string? SecretRef { get; init; }
}
```
All UI pages and view models communicate strictly through `CommandRouter` and `IDeviceAdapter`. No view or view model ever directly opens a network socket, Bluetooth GATT connection, or serial port.

---

## 2. In-App Simulation Mode (`MockDeviceAdapter`)

- **Hardware-free execution:** Operates entirely in memory on the mobile device. No external microcontroller (ESP32/Arduino), smart bulb, IR LED, or network gateway is required.
- **Independence:** Does not request Android Bluetooth permissions or internet access.
- **Labeling:** All outcomes explicitly report `Simulated = true`, and states are tagged with `StateOrigin.Simulated`. Sensor readings are labelled as **SIMULATED DATA**.
- **Scenarios:**
  - `Normal`: Immediate virtual responses (~150 ms simulated latency).
  - `Slow`: 2500 ms latency to exercise responsive UI indicators and cancellation tokens.
  - `Flaky`: 400 ms latency, 25% simulated transmission failures, 15% timeouts.
  - `Offline`: Immediate rejection (`CommandErrorCode.NotConnected`).
  - `Custom`: User-tuned latency (0–4000 ms), failure rate (0–100%), and timeout rate (0–100%).

---

## 3. Bluetooth Low Energy (`BluetoothLeDeviceAdapter` & ORDP/1)

### Protocol: OmniRemote Device Protocol v1 (ORDP/1)
- **GATT Service UUID:** `6f6d6e69-7265-4d6f-9465-000000000001`
- **Command Characteristic (Write):** `6f6d6e69-7265-4d6f-9465-000000000002`
- **Response Characteristic (Notify):** `6f6d6e69-7265-4d6f-9465-000000000003`
- **Required MTU:** 247 bytes.

### Authentication & Replay Protection
Every request frame is authenticated with an HMAC-SHA256 signature calculated over canonical message parameters:
$$\text{MAC} = \text{HMAC-SHA256}_{K_{\text{device}}}(\text{Canonical}(\text{Request}))$$

- **Pairing Key:** 256-bit key ($K_{\text{device}}$) provisioned out-of-band and stored securely in platform keystore via `ISecretStore`.
- **Bonding:** Requires an authenticated, encrypted BLE link (LE Secure Connections).
- **Sequence Tracking:** Monotonically increasing sequence numbers (`seq`) reject replayed frames.
- **Physical Verification:** Protocol verified in unit tests; physical peripheral requires custom firmware implementing ORDP/1.

---

## 4. Local Wi-Fi REST Adapter (`WifiDeviceAdapter`)

### Endpoint Security Policy
To prevent Server-Side Request Forgery (SSRF) and credential leakage:
- Connections are restricted to **HTTPS** endpoints or **private LAN IPv4 addresses** (RFC 1918: `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`) or link-local IPv6.
- Host loopback (`127.0.0.1`, `::1`) is explicitly rejected.
- URLs with embedded user credentials (e.g. `http://user` + `:pass@host/`) are rejected by `EndpointPolicy`.

---

## 5. Infrared Gateway Adapter (`InfraredGatewayAdapter`)

- **Hardware Requirement:** Modern smartphones lack built-in IR transmitters. OmniRemote bridges IR control through an external Wi-Fi/LAN gateway (e.g., ESP32 running Tasmota or ESPHome).
- **One-Way Unconfirmed State:** Because raw IR transmission is inherently blind (one-way without physical telemetry), successfully emitted commands are strictly reported as **`CommandStatus.Sent` (unconfirmed)**. The application state is marked as `Assumed`, never `Confirmed`, unless an independent sensor confirms state.

---

## 6. Smart-Home Hub Adapter (`SmartHomeHubAdapter`)

- **Target Hub:** Home Assistant REST API.
- **Service Mapping:**
  - `power.set` $\rightarrow$ `climate.turn_on/off`, `light.turn_on/off`, `media_player.turn_on/off`, `fan.turn_on/off`.
  - `temperature.set` $\rightarrow$ `climate.set_temperature` (`temperature: <int>`).
  - `volume.step` $\rightarrow$ `media_player.volume_up` / `media_player.volume_down`.
  - `playback.press` $\rightarrow$ `media_player.media_play_pause`, `media_player.media_stop`.
  - `brightness.set` $\rightarrow$ `light.turn_on` (`brightness_pct: <int>`).
  - `sensor.refresh` $\rightarrow$ `GET /api/states/{entity_id}`.
- **Authentication:** Long-Lived Access Token stored in `ISecretStore` and transmitted via `Authorization: Bearer <token>`.

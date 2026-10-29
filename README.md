# OmniRemote

> **One app. Every connection. Total control.**

OmniRemote is a universal smart-remote and smart-home control application built with a **simulation-first, hardware-free architecture**. It provides a single mobile interface for controlling consumer electronic devices and smart-home appliances without requiring an ESP32, Arduino, smart bulb, IR blaster, or physical hub.

---

## 1. Supported Device Categories

OmniRemote provides domain capability models, command validation, and dedicated remote control layouts for:

1. **Air Conditioners:** Power, temperature setpoint, operating modes (Cool, Heat, Fan, Dry, Auto), fan speed, swing.
2. **Televisions:** Power, volume, mute, channel stepping, 5-way D-Pad (Up, Down, Left, Right, OK), Back, Home, Menu.
3. **Set-Top Boxes (STB):** Navigation, OK, Back, Volume/Channel stepping.
4. **TV Boxes (Apple TV / Roku / Fire TV):** Navigation pad, OK, Play/Pause/Stop media playback controls.
5. **Projectors:** Power, source input selection (HDMI1, HDMI2, USB, VGA), navigation.
6. **Fans:** Power, discrete speed levels (1, 2, 3), oscillation toggle.
7. **Light Bulbs & Smart Lighting:** Power, brightness slider (1%–100%), color presets (Warm, Daylight, RGB swatches).
8. **Air Purifiers:** Power, operational modes (Auto, Sleep, Manual), fan speed (Low, Med, High, Turbo).
9. **Power Amplifiers:** Power, master volume, mute, audio input switching (AUX, Optical, Coaxial, Bluetooth, HDMI-ARC).
10. **DVD Players:** Play, pause, stop, previous, next, rewind, fast-forward, seek.
11. **Water Heaters:** Power, target water temperature setpoint (30°C–75°C).
12. **Cameras:** Privacy shutter toggle, recording toggle, snapshot capture, motion detection toggle.
13. **Temperature & Humidity Sensors:** Ambient temperature, relative humidity telemetry, on-demand refresh.
14. **Motion Sensors:** Real-time occupancy state, last-detected motion timestamp.

---

## 2. Hardware-Free Simulation Mode

OmniRemote includes an in-process simulation engine (`MockDeviceAdapter`) that enables 100% of the application's capabilities to run on a single smartphone or dev machine without external hardware:

- **No External Dependencies:** Requires no Bluetooth permissions, no internet access, no external hardware, and no cloud accounts.
- **Explicit Labeling:** All virtual remotes display a prominent **SIMULATION** pill badge. Telemetry data is explicitly marked as **SIMULATED DATA**.
- **Configurable Test Scenarios:**
  - `Normal`: Immediate virtual responses (~150 ms simulated latency).
  - `Slow`: 2500 ms latency to verify responsive cancellation and loading spinners.
  - `Flaky`: 400 ms latency, 25% simulated transmission failures, 15% timeouts.
  - `Offline`: Simulates disconnected or unreachable appliances.
  - `Custom`: Slider-adjustable latency, failure rates, and timeout rates.

---

## 3. UI Design — Matched to Reference Screenshots

OmniRemote implements an Android-style dark interface matching the provided reference screens:
- **Color Palette:** Pure black background (`#000000`), dark-grey cards (`#181818`), rounded tiles (`#1C1C1E`), white typography (`#FFFFFF`), muted grey labels (`#8E8E93`), subtle dividers (`#222224`), and restrained blue accents (`#0A84FF`).
- **Screen A (Home):** Large "Smart Remote" title, profile icon, centered remote outline illustration + "No remote controls" empty state, floating blue plus button (`+`), and scrollable card collection when remotes are added.
- **Screen B (Select Type):** Three-column grid of rounded dark-grey device tiles with centered icons and wrapped labels in the exact 12-item sequence.
- **Screen C (Select Brand):** Dynamic search bar (`Search for brand Air conditioner`), horizontal "Top brands" chip bar, alphabetical A–Z index column, and live filtering.
- **Screen D (Configure Remote):** Friendly name entry, room location selector, connection options (Simulation default), and capability preview.
- **Screen E (Remote Control):** Category-specific interactive interfaces, featuring the circular temperature dial (`24°C`) with `−` and `+` round stepping buttons and a 2-row × 3-column mode grid (Cool, Heat, Fan, Dry, Auto, Swing) for air conditioners.

---

## 4. Universal Connectivity Architecture

```
OmniRemote Mobile (.NET MAUI)
         │
         ▼
Application Layer (DeviceRegistry · CommandRouter · StateStore · ActivityLog)
         │
         ▼
IDeviceAdapter
 ├── MockDeviceAdapter (Simulation Mode)
 ├── BluetoothLeDeviceAdapter (ORDP/1 Protocol with HMAC-SHA256 & BLE Bonding)
 ├── WifiDeviceAdapter (Private LAN / HTTPS Local REST API)
 ├── InfraredGatewayAdapter (External ESP32 Gateway · One-Way Unconfirmed Transmission)
 └── SmartHomeHubAdapter (Home Assistant REST Service Mapping & Entity States)
```

---

## 5. Security Controls Summary

OmniRemote strictly enforces all 20 mandatory security controls:
1. **Secure API Keys:** No production secrets in code. Credentials stored via platform `ISecretStore`.
2. **Hide all `.env` files:** Complete `.gitignore` rules with `.env.example` template.
3. **Never Hardcode Secrets:** Production startup fails immediately if `Jwt:SigningKey` is missing or < 32 bytes.
4. **Authentication:** JWT Bearer authentication; passwords hashed with PBKDF2 (SHA-512, 100k iterations, 128-bit salt).
5. **Server-Side Verification:** Ownership and authorization verified on every backend request.
6. **No Client User IDs:** User identity derived exclusively from validated server-side claims.
7. **User Data Isolation:** Multi-tenant separation across devices, scenes, and activity history.
8. **Lock Down Database:** SQLite accessed via parameterized EF Core queries; no raw SQL concatenation.
9. **No Cloud Vendor Lock-In:** Operates locally without Firebase or Supabase cloud dependencies.
10. **Protected Admin Routes:** Administrator endpoints require verified server-side `Role = Admin`.
11. **Production Debug Disabled:** Developer exception pages and debug endpoints disabled in production.
12. **Hide Detailed Errors:** `SafeErrorHandlingMiddleware` returns generic RFC7807 problem details.
13. **Input Validation:** Strict allow-list regex, length bounds, and profile capability checks.
14. **Sanitized User Content:** Invisible Unicode controls and CRLF characters rejected to prevent log injection.
15. **No File Uploads:** Upload attack surface eliminated.
16. **Injection Resistance:** Parameterized queries protect all filters.
17. **Rate Limiting:** Built-in fixed-window rate limiters on authentication and command routes.
18. **Secret Scanner:** Verified with TruffleHog (0 secrets in repo history).
19. **Security Headers:** `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `CSP`, `Referrer-Policy`.
20. **Untrusted User Testing:** Comprehensive automated integration tests verify defense against cross-tenant attacks and privilege escalation.

---

## 6. Build & Test Instructions

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)

### Build Solution
```bash
cd omniremote
dotnet build OmniRemote.slnx
```

### Run Unit Tests
```bash
dotnet test tests/OmniRemote.UnitTests/OmniRemote.UnitTests.csproj
```
*(68 automated unit tests covering profiles, commands, simulation, brand catalog, and security policies)*

### Run Integration Tests
```bash
dotnet test tests/OmniRemote.IntegrationTests/OmniRemote.IntegrationTests.csproj
```
*(8 automated integration tests verifying multi-tenant isolation, JWT auth, and admin protection)*

### Run Secret Scanner
```bash
trufflehog git file://$(pwd) --fail --no-verification
```

---

## 7. Demonstration Procedure

1. **Launch App:** Start OmniRemote on the mobile device or simulator.
2. **View Home Screen:** Observe the pure black background and centered "No remote controls" empty state.
3. **Add Remote:** Tap the blue floating action button (`+`) at the bottom right.
4. **Select Category:** Select **Air conditioner** from the 3-column tile grid.
5. **Select Brand:** Tap **Carrier** from the Top Brands section or search via the search bar.
6. **Configure Remote:** Enter friendly name (e.g. `Living Room AC`), pick a room, leave **Simulation Mode** selected, and tap **Save Remote**.
7. **Control Device:**
   - Tap the Power button (`⏻`) at the top right to turn the simulated AC on.
   - Tap `+` or `−` on either side of the circular dial to step temperature (e.g. `24°C` $\rightarrow$ `22°C`).
   - Tap mode buttons (Cool, Heat, Fan, Dry, Auto, Swing) to change state.
   - Observe the live feedback banner: `✓ Confirmed (simulated)`.
8. **Telemetry & History:** Tap **Activity Log** to view the timeline of commands and simulation tags.
9. **Simulate Scenarios:** Tap the profile icon on the Home screen to open **Simulation Settings**, toggle between `Normal`, `Slow`, and `Flaky` scenarios, and verify response behavior.

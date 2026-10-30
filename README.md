
# OmniRemote

> **One app. Every connection. Total control.**

OmniRemote is a universal smart-remote and smart-home control application built with a **simulation-first, hardware-free architecture**. It provides a unified interface for managing consumer electronics and smart-home appliances through a single mobile application.

OmniRemote supports virtual device control without requiring an ESP32, Arduino, IR blaster, smart bulb, physical hub, or cloud account for simulation mode.

**Simulation comes first. Hardware integration is designed to be modular.**

---

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Supported Devices](#supported-devices)
- [Simulation Engine](#simulation-engine)
- [Application Screens](#application-screens)
- [System Architecture](#system-architecture)
- [Security Architecture](#security-architecture)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Build and Test](#build-and-test)
- [Run the Secret Scanner](#run-the-secret-scanner)
- [Demonstration Workflow](#demonstration-workflow)
- [Project Structure](#project-structure)
- [Connectivity Roadmap](#connectivity-roadmap)
- [Security Reporting](#security-reporting)
- [Limitations](#limitations)
- [License](#license)

---

## Overview

OmniRemote brings multiple remote-control interfaces together in one mobile application.

Instead of requiring physical hardware during development, OmniRemote uses a local simulation engine to model device states, validate commands, simulate network conditions, and provide feedback.

### Design Principles

- **Simulation-first:** Core functionality can be tested without physical devices.
- **Modular architecture:** Device communication is separated from application logic.
- **Security by design:** Authentication, authorization, input validation, and data isolation are part of the architecture.
- **Consistent user experience:** Each device category receives an appropriate control interface.
- **Extensible connectivity:** Additional device adapters can be integrated without redesigning the entire application.
- **Transparent simulation:** Virtual commands and telemetry are explicitly identified as simulated.

## Key Features

- Universal remote interface for multiple device categories.
- Dedicated device profiles and command validation.
- Dynamic device-brand selection and search.
- Custom remote names and room assignments.
- Hardware-free simulation with configurable response behavior.
- Device state management and command routing.
- Activity logs with simulated command outcomes.
- Temperature, brightness, volume, and speed controls where supported.
- Authentication and role-based access controls in the backend.
- Multi-tenant device and activity-data isolation.
- Automated unit and integration testing.
- Modular adapters for future Bluetooth, Wi-Fi, infrared-gateway, and smart-home integrations.

---

## Supported Devices

OmniRemote defines category-specific capabilities and control layouts for the following devices.

| # | Device Category | Supported Controls |
|---|---|---|
| 1 | Air Conditioners | Power, temperature, mode, fan speed, swing |
| 2 | Televisions | Power, volume, mute, channels, D-pad, Back, Home, Menu |
| 3 | Set-Top Boxes | Navigation, OK, Back, volume, channel stepping |
| 4 | Streaming TV Boxes | Navigation, OK, Play, Pause, Stop |
| 5 | Projectors | Power, source selection, navigation |
| 6 | Fans | Power, speed levels, oscillation |
| 7 | Smart Lighting | Power, brightness, color presets |
| 8 | Air Purifiers | Power, mode, fan speed |
| 9 | Power Amplifiers | Power, volume, mute, audio input selection |
| 10 | DVD Players | Play, Pause, Stop, Previous, Next, Rewind, Fast-forward, Seek |
| 11 | Water Heaters | Power, temperature setpoint |
| 12 | Cameras | Privacy shutter, recording, snapshot, motion detection |
| 13 | Temperature and Humidity Sensors | Temperature, humidity, refresh |
| 14 | Motion Sensors | Occupancy state, last-detected motion timestamp |

**Important:** Device profiles describe supported control capabilities. Actual physical control requires a compatible device and a working communication adapter. Simulation mode does not communicate with real appliances.

---

## Simulation Engine

OmniRemote uses `MockDeviceAdapter` to simulate device communication and state transitions locally.

### Simulation Scenarios

| Scenario | Simulated Behavior |
|---|---|
| `Normal` | Approximately 150 ms response latency |
| `Slow` | Approximately 2,500 ms response latency |
| `Flaky` | Approximately 400 ms latency, 25% simulated transmission failures, 15% simulated timeouts |
| `Offline` | Device behaves as disconnected or unreachable |
| `Custom` | Configurable latency, failure rate, and timeout rate |

The exact observed timing and outcomes depend on implementation details and asynchronous scheduling.

### Simulation Guarantees

- No external hardware is required.
- Simulation does not require a cloud account.
- Virtual device states can be exercised through the application's control interfaces.
- Simulated telemetry is labelled `SIMULATED DATA`.
- Virtual remote controls display a `SIMULATION` badge.
- Successful simulated commands must not be represented as confirmed physical device actions.

---

## Application Screens

OmniRemote uses a dark, Android-style interface with a restrained blue accent.

### 1. Home Screen

- Black background (`#000000`).
- Large **Smart Remote** heading.
- Profile and simulation-settings access.
- Empty state displaying **No remote controls**.
- Blue floating action button for adding a remote.
- Scrollable collection of configured remotes.

### 2. Select Device Type

- Three-column device-category grid.
- Rounded dark-grey tiles.
- Centered device icons.
- Consistent labels and spacing.

### 3. Select Brand

- Search field with a category-specific placeholder.
- Top-brand shortcuts.
- Alphabetical A–Z navigation.
- Live brand filtering.

### 4. Configure Remote

- Custom remote name.
- Room selection.
- Simulation connection option selected by default.
- Device capability preview.
- Save Remote action.

### 5. Remote Control

Controls vary by device category.

For air conditioners, the interface includes:

- Power toggle.
- Circular temperature dial.
- Temperature increment and decrement controls.
- Cool, Heat, Fan, Dry, Auto, and Swing controls.
- Command feedback and simulated state updates.

### Visual Design Tokens

| Element | Value |
|---|---|
| Primary background | `#000000` |
| Card background | `#181818` |
| Tile background | `#1C1C1E` |
| Primary text | `#FFFFFF` |
| Secondary text | `#8E8E93` |
| Divider | `#222224` |
| Accent blue | `#0A84FF` |

---

## System Architecture

OmniRemote separates user-interface concerns, device capabilities, command processing, state management, and device communication.

```text
OmniRemote Mobile Application
          |
          v
Application Layer
  - DeviceRegistry
  - CommandRouter
  - StateStore
  - ActivityLog
          |
          v
      IDeviceAdapter
          |
          +-- MockDeviceAdapter
          |      Simulation mode
          |
          +-- BluetoothLeDeviceAdapter
          |      ORDP/1 protocol
          |      HMAC-SHA256 and BLE bonding
          |
          +-- WifiDeviceAdapter
          |      Private LAN / HTTPS REST API
          |
          +-- InfraredGatewayAdapter
          |      External IR gateway
          |
          +-- SmartHomeHubAdapter
                 Home Assistant integration
```

### Core Components

| Component | Responsibility |
|---|---|
| `DeviceRegistry` | Registers device profiles and available devices |
| `CommandRouter` | Validates and dispatches device commands |
| `StateStore` | Maintains device state |
| `ActivityLog` | Records command events and outcomes |
| `IDeviceAdapter` | Defines the device communication abstraction |
| `MockDeviceAdapter` | Simulates device responses and failures |
| `BluetoothLeDeviceAdapter` | Provides the planned BLE integration |
| `WifiDeviceAdapter` | Supports the planned local-network integration |
| `InfraredGatewayAdapter` | Supports the planned external IR gateway |
| `SmartHomeHubAdapter` | Maps commands and states to a smart-home hub |

The adapter interface isolates device-specific transport details from the application's core logic.

---

## Security Architecture

OmniRemote is designed around layered security controls for authentication, authorization, storage, input validation, and API protection.

### Security Controls

1. **Secure secret storage:** Use platform-supported secure storage for sensitive client-side values.
2. **Environment-file protection:** Exclude `.env` files and local configuration secrets from version control.
3. **Signing-key validation:** Reject production startup when `Jwt:SigningKey` is absent or shorter than 32 bytes.
4. **Password security:** Use PBKDF2 with SHA-512, a 128-bit salt, and the configured iteration count.
5. **Server-side authorization:** Validate resource ownership and permissions for every protected request.
6. **Trusted identity claims:** Derive authenticated user identity from validated server-side claims.
7. **Tenant isolation:** Restrict access to each user's devices, scenes, and activity history.
8. **Parameterized database access:** Use Entity Framework Core parameterized operations.
9. **Local-first design:** Avoid mandatory dependence on third-party cloud platforms.
10. **Administrator protection:** Require verified server-side administrator roles for administrative endpoints.
11. **Production hardening:** Disable developer exception pages and debug endpoints in production.
12. **Safe error handling:** Return generic RFC 7807 problem details without leaking internal implementation details.
13. **Input validation:** Enforce allow-lists, length limits, and device-capability checks.
14. **Log sanitization:** Reject or safely encode invisible Unicode controls and CRLF characters where appropriate.
15. **Minimal upload surface:** Do not expose file-upload endpoints unless a justified requirement exists.
16. **Injection prevention:** Avoid unsafe SQL construction and validate query inputs.
17. **Rate limiting:** Apply appropriate limits to authentication and command endpoints.
18. **Secret scanning:** Scan repository contents and history using TruffleHog.
19. **Security headers:** Configure appropriate content-type, framing, referrer, and content-security policies.
20. **Automated security tests:** Test cross-tenant access, unauthorized requests, and privilege escalation.

### Authentication and Storage

The planned backend uses:

- JWT Bearer authentication.
- PBKDF2 password hashing.
- SQLite with Entity Framework Core.
- Server-side authorization.
- Secure configuration for signing keys.
- Rate limiting and centralized error handling.

### Security Notes

Security requirements are design targets until verified against the actual implementation. Do not claim that a control is fully enforced, that the repository is free of secrets, or that all security tests pass without reviewing the code and test results.

For production, review password-hashing parameters, key rotation, JWT issuer and audience validation, token expiration, HTTPS configuration, authorization policies, and database backup protection.

---

## Technology Stack

| Technology | Purpose |
|---|---|
| .NET MAUI | Cross-platform mobile application |
| C# / .NET | Application and backend development |
| `IDeviceAdapter` | Extensible device connectivity abstraction |
| Entity Framework Core | Database access |
| SQLite | Local or application database storage |
| JWT Bearer | API authentication |
| PBKDF2 / SHA-512 | Password hashing |
| TruffleHog | Secret detection |
| Automated tests | Unit and integration verification |

**Prerequisite:** Install the .NET 10 SDK and the platform-specific workloads required by your target application.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- A compatible IDE or command-line environment
- Android SDK and emulator for Android development, if applicable
- The OmniRemote source repository

Check the installed SDK:

```bash
dotnet --info
dotnet --list-sdks
```

---

## Getting Started

### 📱 Option 1: Run in Expo Go on your Phone (Recommended)

Just like QRShield, OmniRemote can be launched instantly on your physical iPhone or Android device via **Expo Go** without requiring Xcode, Android Studio, or USB debugging:

```bash
# 1. From the project root, start the Expo development server:
npm run mobile
# or
cd apps/mobile && npx expo start
```

1. Install **Expo Go** from the iOS App Store or Google Play Store.
2. Scan the QR code displayed in your terminal with your phone camera (iOS) or inside the Expo Go app (Android).
3. The full OmniRemote dark-theme UI will launch immediately on your device!

---

### 💻 Option 2: Run the ASP.NET Core Backend & CLI Demo

### 1. Clone the Repository

Replace the repository URL below with the actual repository URL if needed.

```bash
git clone https://github.com/manoharmanyu/OMNI-REMOTE.git
cd OMNI-REMOTE
```

### 2. Restore Dependencies

```bash
dotnet restore OmniRemote.slnx
```

### 3. Build the Solution

```bash
dotnet build OmniRemote.slnx
```

### 4. Run the Application

Inspect the solution to identify the mobile application project:

```bash
dotnet sln OmniRemote.slnx list
```

Then run the relevant project using its actual path:

```bash
dotnet run --project path/to/OmniRemote.App.csproj
```

Replace the example project path with the application project's real path. A .NET MAUI application may require a target-specific run configuration or IDE deployment rather than a generic `dotnet run` command.

---

## Build and Test

### Build

```bash
dotnet build OmniRemote.slnx
```

### Unit Tests

```bash
dotnet test tests/OmniRemote.UnitTests/OmniRemote.UnitTests.csproj
```

The intended unit-test coverage includes:

- Device profiles and capabilities.
- Command validation.
- Simulation behavior.
- Brand catalog functionality.
- Security policies.

### Integration Tests

```bash
dotnet test tests/OmniRemote.IntegrationTests/OmniRemote.IntegrationTests.csproj
```

The intended integration-test coverage includes:

- JWT authentication.
- Multi-tenant isolation.
- Protected administrator endpoints.
- Unauthorized-access handling.

The project specification targets 68 unit tests and 8 integration tests. Actual test counts and results should be confirmed by running the commands above.

---

## Run the Secret Scanner

Install TruffleHog using its official installation instructions, then run the repository scan:

```bash
trufflehog git file://$(pwd) --fail --no-verification
```

Review the scanner's output and investigate any findings. A clean scan is evidence from that scan, not proof that credentials cannot exist elsewhere or that every secret is safe.

Never commit production keys, passwords, access tokens, signing secrets, or private certificates.

---

## Demonstration Workflow

Follow this workflow to demonstrate the hardware-free simulation.

1. Launch OmniRemote on a supported device or emulator.
2. Confirm that the Home screen displays **No remote controls** when no remotes have been configured.
3. Tap the blue `+` button.
4. Select **Air conditioner**.
5. Select **Carrier** or search for a brand.
6. Enter a remote name, such as `Living Room AC`.
7. Select a room and keep **Simulation** as the connection mode.
8. Save the remote.
9. Open the new remote and turn on the simulated air conditioner.
10. Adjust the temperature using the `+` and `−` controls.
11. Select Cool, Heat, Fan, Dry, Auto, or Swing.
12. Observe the simulated command result and device state.
13. Open Activity Log to inspect command history.
14. Open Simulation Settings and test Normal, Slow, Flaky, and Offline scenarios.
15. Confirm that simulated failures and timeouts are handled without incorrectly reporting success.

### Expected Behavior

- The remote displays a visible `SIMULATION` indicator.
- Successful virtual commands return simulated feedback.
- Temperature and mode changes update simulated state.
- Slow commands exercise loading and cancellation behavior.
- Failed or timed-out commands report appropriate errors.
- Offline devices are shown as unavailable.
- Activity history identifies simulated events.

---

## Project Structure

The following is a logical project structure. Actual directory names should match the repository.

```text
OMNI-REMOTE/
├── OmniRemote.slnx
├── README.md
├── .gitignore
├── .env.example
├── src/
│   ├── OmniRemote.App/
│   ├── OmniRemote.Core/
│   ├── OmniRemote.Infrastructure/
│   └── OmniRemote.Api/
├── tests/
│   ├── OmniRemote.UnitTests/
│   └── OmniRemote.IntegrationTests/
└── docs/
    ├── architecture.md
    └── security.md
```

Some folders above may not exist in the current repository. Keep this section synchronized with the actual solution structure.

---

## Connectivity Roadmap

| Integration | Purpose | Important Requirement |
|---|---|---|
| Simulation | Hardware-free development and testing | Local virtual state and command processing |
| Bluetooth LE | Communicate with compatible BLE devices | Supported hardware and a defined secure protocol |
| Wi-Fi | Communicate with local-network devices | Compatible device API and appropriate authentication |
| Infrared Gateway | Transmit IR commands through a gateway | External IR hardware and device-specific command codes |
| Home Assistant | Integrate compatible smart-home entities | Configured hub, API credentials, and network access |

The presence of an adapter in the architecture does not by itself establish that the integration is implemented or production-ready.

### Bluetooth Protocol

The proposed ORDP/1 protocol is intended to use HMAC-SHA256 for message authentication and BLE bonding where appropriate.

A production protocol should additionally define message framing, nonce or sequence handling, replay protection, key provisioning, and credential lifecycle management.

### Wi-Fi and Smart-Home Integrations

Real-world control depends on device compatibility, network configuration, API availability, and permissions. Simulation remains independent of these integrations.

---

## Security Reporting

If you discover a potential security vulnerability, report it privately to the repository maintainer before publicly disclosing technical details.

Include:

- Affected component and version.
- Reproduction steps.
- Expected and observed behavior.
- Potential impact.
- Suggested mitigation, if known.

Do not include real credentials, private user data, or sensitive production logs in public reports.

---

## Limitations

- Simulation mode does not control physical appliances.
- Device capabilities depend on the relevant device profile and adapter.
- Bluetooth, Wi-Fi, infrared, and smart-home integrations require compatible implementations and devices.
- Simulated latency and failures do not reproduce every real-world network condition.
- Actual security guarantees depend on the implementation, configuration, and test evidence.
- Hardware compatibility must be validated independently before physical deployment.

---

## License

No license has been specified yet.

Before publishing the repository for reuse, add a `LICENSE` file containing the license you have selected. Until then, do not assume that the project is licensed for unrestricted redistribution or modification.

---

## Project Philosophy

**One app. Every connection. Total control.**

OmniRemote is designed to make universal remote-control development accessible through a simulation-first architecture, extensible connectivity, a consistent mobile experience, and security-conscious engineering.

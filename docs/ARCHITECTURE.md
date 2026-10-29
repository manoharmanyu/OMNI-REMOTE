# OmniRemote Architecture

## 1. System Overview

OmniRemote is a universal smart-remote and smart-home control platform designed with a **simulation-first, hardware-free architecture**. The application provides a single mobile interface for controlling compatible consumer electronics and smart-home appliances without requiring physical hubs, gateways, or hardware transceivers during development and evaluation.

```
┌────────────────────────────────────────────────────────┐
│                   OmniRemote Mobile                    │
│             (.NET MAUI · MVVM Architecture)            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  HomeScreen  │  │SelectTypeView│  │SelectBrand   │  │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  │
│         │                 │                 │          │
│  ┌──────▼───────┐  ┌──────▼───────┐         │          │
│  │ConfigureView │  │RemoteControl │◄────────┘          │
│  └──────┬───────┘  └──────┬───────┘                    │
└─────────┼─────────────────┼────────────────────────────┘
          │                 │
┌─────────▼─────────────────▼────────────────────────────┐
│                   Application Layer                    │
│  ┌──────────────────┐         ┌─────────────────────┐  │
│  │  DeviceRegistry  │         │    CommandRouter    │  │
│  └────────┬─────────┘         └──────────┬──────────┘  │
│           │                              │             │
│  ┌────────▼─────────┐         ┌──────────▼──────────┐  │
│  │ DeviceStateStore │         │     ActivityLog     │  │
│  └──────────────────┘         └─────────────────────┘  │
└───────────────────────────┬────────────────────────────┘
                            │
┌───────────────────────────▼────────────────────────────┐
│                    IDeviceAdapter                      │
│   ┌───────────────┬───────────────┬────────────────┐   │
│   │  Simulation   │ Bluetooth LE  │  Wi-Fi (REST)  │   │
│   │ (MockAdapter) │(ORDP Protocol)│(Private LAN/TLS│   │
│   ├───────────────┼───────────────┴────────────────┤   │
│   │  IR Gateway   │        Smart Home Hub          │   │
│   │(Unconfirmed)  │      (Home Assistant)          │   │
│   └───────────────┴────────────────────────────────┘   │
└────────────────────────────────────────────────────────┘
```

## 2. Core Modules

| Module | Responsibility | Target |
| :--- | :--- | :--- |
| `OmniRemote.Domain` | Device categories, capabilities, profile validation, command specs, and secure input rules. Zero third-party dependencies. | net10.0 |
| `OmniRemote.Core` | `IDeviceAdapter` contract, `DeviceRegistry`, `CommandRouter`, `DeviceStateStore`, `ActivityLog`, `MockDeviceAdapter`, and embedded `BrandCatalog`. | net10.0 |
| `OmniRemote.Infrastructure` | Atomic JSON file persistence (`JsonFileStore`), safe serializer contexts, and device records. | net10.0 |
| `OmniRemote.Integrations.Bluetooth` | OmniRemote Device Protocol v1 (ORDP/1) with HMAC-SHA256 authenticated framing, bonding checks, MTU management, and GATT session management. | net10.0 |
| `OmniRemote.Integrations.Network` | Local Wi-Fi REST adapter governed by `EndpointPolicy` (restricting execution to private LAN IPs or HTTPS). | net10.0 |
| `OmniRemote.Integrations.Infrared` | External IR gateway adapter reporting one-way unconfirmed transmission (`CommandStatus.Sent`). | net10.0 |
| `OmniRemote.Integrations.SmartHomeHub` | Home Assistant REST integration mapping commands to domain service calls (`climate`, `media_player`, `light`). | net10.0 |
| `OmniRemote.Backend` | ASP.NET Core Web API with JWT auth, PBKDF2 password hashing, rate limiting, and multi-tenant isolation. | net10.0 |
| `OmniRemote.Mobile` | .NET MAUI multi-platform mobile application adhering to the dark UI design specifications. | net10.0 (universal) / net10.0-android |

## 3. Data Flow & Command Processing

1. **User Action:** The user taps a control (e.g. adjusts temperature on the AC remote).
2. **Command Creation:** The ViewModel constructs an immutable `DeviceCommand` with a unique `RequestId` and parameters.
3. **Validation Boundary:** `CommandValidator` inspects the command name, parameter types, bounds, and ensures the target device profile explicitly supports the capability.
4. **Command Routing:** `CommandRouter` verifies that the `RequestId` is not a replay, checks adapter availability, and manages timeout/cancellation tokens.
5. **Transport Execution:**
   - **Simulation:** In-process `MockDeviceAdapter` applies state transformations, simulates latency, and returns `CommandResult` with `Simulated = true`.
   - **Physical:** Transport adapter encodes frames (e.g. ORDP over BLE or REST over LAN), handles device responses, and verifies authenticating MACs.
6. **State Reconciliation:** `DeviceStateStore` updates the stored state. Confirmed device feedback updates the state directly; unconfirmed commands mark state as `Assumed`.
7. **History Logging:** `ActivityLog` appends an immutable entry recording timestamp, device, status, and whether data was simulated.
8. **UI Update:** The ViewModel reacts to the state store event and reflects the new state on the screen.

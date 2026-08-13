# Nexus Control Agent

**The Windows companion for Nexus Control.**

![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![UI](https://img.shields.io/badge/UI-WinForms-2D7DDB)

Nexus Control Agent connects the Nexus Control mobile app to a Windows PC.
The Agent runs compactly in the Windows system tray and provides the local
functionality required for system status, PC control, media control, screen
streaming, and file transfers.

The connection is designed for private networks and optionally for encrypted
remote access through Tailscale. No Windows passwords, PINs, or unlock
credentials are stored.

## Features

- QR code and numeric code pairing with time-limited codes
- trusted devices using hashed, randomly generated device tokens
- local device management with device name, platform, online status, pausing,
  removal, and individual feature permissions
- connection diagnostics for the Agent port, local addresses, firewall, paired
  devices, Windows startup, and Tailscale
- dedicated **Activity Log** button for a limited local history of connections,
  pairing events, and executed actions
- PC status for CPU, GPU, RAM, drives, network, and available temperature sensors
- lock, sleep, restart, and shut down the PC
- remote mouse, keyboard, media controls, and Windows volume
- active Windows media sessions such as browsers, Spotify, or YouTube
- screen streaming with multi-monitor selection
- file transfers up to a maximum of 100 MB per file
- Wake-on-LAN information for paired devices
- automatic startup with Windows and silent operation in the system tray
- local firewall configuration for the Agent port
- optional push notifications for monitored events
- one-time welcome screen with language selection on the first interactive
  launch
- built-in language selection with English as the default, English fallback,
  and German, French, Spanish, Italian, and Polish translations

## Requirements

### Installation

- Windows 10 or Windows 11
- a PC and smartphone on the same private network or the same Tailscale network
- administrator privileges for installation, firewall configuration, and
  Windows control commands

The published Windows x64 package is self-contained and does not require a
separately installed .NET runtime.

### Development

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio with the **.NET desktop development** and
  **ASP.NET and web development** workloads
- for MSI builds: WiX Toolset 7 through the included installer project

## Installation and Pairing

1. Download the latest MSI package from GitHub Releases.
2. Run `NexusControlAgent-Setup-vX.Y.Z-win-x64.msi`.
3. Select the desired installer options and confirm the firewall prompt.
4. Open Nexus Control Agent from the Start menu.
5. In the Nexus Control mobile app, select **Add PC**.
6. Scan the QR code or enter the six-digit pairing code.

After successful pairing, the Agent remains active in the system tray. The
compact window can be hidden at any time without shutting down the Agent.
When started automatically after Windows sign-in, the main window remains
closed. It can be opened by double-clicking the tray icon or selecting
**Open Nexus Control** from the tray menu. Starting the Agent manually from the
Start menu continues to open the window normally.

The green device count in the **Connection** section opens local device
management. Paired smartphones can be renamed, paused, removed, and granted
separate permissions for PC control, touchpad input, processes, media, screen
streaming, files, and power commands. The **Diagnostics** button runs local
connection tests and can copy a sanitized report without tokens.

The **Activity Log** button opens a separate compact window containing recent
local connections and actions. It updates automatically, can be copied or
cleared completely, and does not change the compact main window layout.

> [!IMPORTANT]
> Port `5188` must never be forwarded directly from the public internet. Use
> Tailscale for access from outside the local network.

## Local Development

Clone the repository and open `NexusControlAgent.sln` in Visual Studio.
**NexusControlAgent** must be selected as the startup project.

Alternatively, use a Windows terminal:

```powershell
dotnet restore .\NexusControlAgent.csproj
dotnet build .\NexusControlAgent.csproj --configuration Release
```

The included quick-start script builds the project and then starts the Agent:

```text
Scripts\start-agent.bat
```

## Configuration

Default values are located under `Agent` in `appsettings.json`.

| Setting | Default | Description |
| --- | ---: | --- |
| `Port` | `5188` | Local HTTP and WebSocket port |
| `PairingCodeLifetimeMinutes` | `10` | Lifetime of a pairing code |
| `MaximumPairingAttempts` | `8` | Allowed attempts per pairing code |
| `TelemetryIntervalMilliseconds` | `2000` | Interval for standard telemetry |
| `AllowedClockSkewMinutes` | `2` | Allowed clock skew for commands |
| `MaximumMessageSizeBytes` | `65536` | Maximum size of a WebSocket message |
| `PushTemperatureThresholdCelsius` | `85` | Temperature threshold for warnings |

.NET User Secrets can be used for local development values. Private tokens,
certificates, and credentials must not be stored in `appsettings.json` or
committed to Git.

## Languages

On the first interactive launch, the Agent displays a one-time welcome screen
where the application language can be selected. A silent Windows tray startup
does not open this screen; it appears when the Agent is opened by the user for
the first time. After setup is completed, the welcome screen is not shown
again.

Use the **Settings** button in the Agent window header to switch later between
**English**, **Deutsch**, **Français**, **Español**, **Italiano**, and
**Polski**. English is used for first launch and as the fallback whenever a
translation key is missing. The selected language and completed first-run
state are stored per Windows user under `%LocalAppData%\NexusControl` and
restored on the next launch.

UI translations are stored in `Localization/Strings.resx` and the matching
culture-specific `Strings.<language>.resx` files. New languages can be added
without changing the application logic.

## Publishing

Create a self-contained Windows x64 application:

```text
Scripts\publish-agent.bat
```

Build the application and the German WiX 7 MSI:

```text
Scripts\build-msi.bat
```

The generated files are not committed to Git. They are created under
`artifacts\`:

```text
artifacts\publish\win-x64\NexusControlAgent.exe
artifacts\installer\NexusControlAgent-Setup-vX.Y.Z-win-x64.msi
```

Before a public release, the EXE and MSI should be signed with a trusted
code-signing certificate. See `Installer/README.md` for additional information.

## Project Structure

```text
Application/         WinForms lifecycle and system tray
Configuration/       Settings and validation
Forms/               Main window, designer files, and resources
Localization/        Language selection and translated resource catalogs
Models/              API, telemetry, and message models
Networking/          HTTP, WebSocket, and network validation
Pairing/             Pairing and trusted devices
Security/            Secure local paths
Services/            Telemetry, media, screen, files, and startup
UI/                  Dark theme and standard dialogs
Windows/             Windows control and audio integration
Installer/           WiX 7 installer and installer assets
Scripts/             Build, installation, and validation routines
```

## Security and Privacy

- Pairing codes are time-limited and are regenerated after too many failed
  attempts.
- Device tokens are stored locally only as SHA-256 hashes.
- Feature permissions are not only displayed in the UI; they are enforced
  directly for HTTP and WebSocket commands.
- The local activity log stores only the timestamp, sanitized device name,
  platform, predefined action name, and result. Passwords, tokens, command
  parameters, text input, filenames, and file contents are not stored.
- Paused devices are rejected immediately and existing sessions are terminated.
- Commands are validated using time windows, allowlists, and rate limits.
- Connections are accepted only from loopback, private networks, and Tailscale
  networks.
- There is no Windows unlock feature, and no passwords or PINs are stored.
- The Agent does not install a Windows kernel service or Credential Provider.

Please do not report security issues publicly as regular GitHub issues. The
responsible disclosure process is described in [SECURITY.md](SECURITY.md).

## Contributing

Bug reports and improvement suggestions are welcome. For larger changes, please
open an issue first. Additional guidance is available in
[CONTRIBUTING.md](CONTRIBUTING.md).

## Additional Documentation

- [Changelog](CHANGELOG.md)
- [Contributing](CONTRIBUTING.md)
- [Security Policy](SECURITY.md)
- [Installer Documentation](Installer/README.md)

## License

No open-source license has been selected for the source code yet. As long as no
`LICENSE` file is present, all rights are reserved. Before publicly allowing
reuse, an appropriate license should be selected deliberately.

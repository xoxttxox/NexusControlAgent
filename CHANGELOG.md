# Changelog

All notable changes to Nexus Control Agent are documented in this file.
The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.11.5] – 2026-08-15

### Added

- added an extensible WinForms localization system with English as the default
  and automatic English fallback
- added selectable English, German, French, Spanish, Italian, and Polish UI
  languages
- added a one-time welcome screen with language selection on the first
  interactive launch
- added a compact **Settings** button and language settings dialog to the Agent
  window header
- added per-user persistence of the selected language under
  `%LocalAppData%\NexusControl`
- localized the main window, paired-device management, connection diagnostics,
  activity log, system tray, firewall prompts, and common dialogs

### Changed

- converted all project Markdown documentation to English
- silent Windows tray startup remains windowless; first-run setup waits until
  the user opens the Agent interactively
- activity log entries now use stable action identifiers and are translated
  when displayed, including entries written by earlier German versions

### Fixed

- Windows autostart now keeps the Agent window fully hidden instead of allowing
  the WinForms message loop to display it after initialization
- the Agent no longer registers its window as the application context's main
  form; the tray context now owns the process lifetime independently
- opening the Agent manually or from the tray restores a normal taskbar entry,
  while hiding it removes the taskbar entry and keeps the Agent running

## [0.11.4] – 2026-08-13

### Added

- added a dedicated **Activity Log** button at the bottom next to
  **Diagnostics** and **Hide**
- added a compact local activity log window with live updates, copy support,
  and safe log clearing
- added a limited history for smartphone connections, pairing events, executed
  commands, and successful, denied, or failed actions
- added compact local management for paired smartphones, including device name,
  platform, live status, last activity, pausing, and secure removal
- added individually configurable permissions for PC control, touchpad and
  keyboard input, processes, media and volume, screen streaming, files, and
  power commands
- added local connection diagnostics for the Agent port, network addresses,
  firewall, paired devices, silent Windows startup, and Tailscale
- added a copyable, sanitized diagnostic report without device or push tokens

### Changed

- the green device count now opens device management
- the previous **Refresh** button now opens **Diagnostics**; regular status
  information continues to update automatically
- the header now also displays active devices and the current connection mode,
  either LAN or Tailscale
- the local address list no longer uses a permanent blue selection highlight

### Security

- activity logs contain only the timestamp, sanitized device name, platform,
  predefined action name, and result; passwords, tokens, command parameters,
  text input, filenames, and file contents are not stored
- high-frequency mouse movement and scrolling events are intentionally excluded
  from logging
- permissions are enforced server-side for control commands as well as file,
  screen, media, and process data
- pausing or removing a device terminates its active connection and prevents
  new commands
- legacy device files are automatically migrated to the extended local format
  using compatible default permissions

### Fixed

- information and error dialogs no longer display a permanently visible bright
  scrollbar
- dialog height now automatically adjusts to wrapped message text so content
  remains readable without unnecessary scrolling
- existing Windows startup tasks are checked for the required `--tray`
  parameter when the Agent starts and are automatically repaired when needed;
  this prevents the Agent window from opening after Windows sign-in

## [0.11.3] – 2026-08-12

### Removed

- completely removed the integrated GitHub updater, including the startup check,
  background service, download logic, MSI helper, and local update result files
- removed the update window, update button in the main window, update entry in
  the tray menu, and related notifications
- removed the entire `Updates` section from `appsettings.json`
- removed automatic `.msi.sha256` generation from the build and signing workflow

New versions are installed manually as regular MSI packages.

## [0.11.2] – 2026-08-12

### Changed

- updated the project, Agent, MSI, and release versions consistently to 0.11.2

### Fixed

- download and checksum streams are now guaranteed to close before the MSI
  installer is moved or deleted, preventing the updater from locking its own
  file on Windows
- added a short retry during the final move to prevent sporadic failures when
  Windows Defender or another antivirus product is scanning the new file

## [0.11.1] – 2026-08-11

### Added

- added a compact Discord-style WinForms update screen shown before the server,
  tray icon, and main window are started
- added automatic installation of a detected update directly at startup
- added a short offline timeout so the Agent still starts when GitHub is
  unavailable

### Fixed

- each update attempt now uses its own temporary directory and a unique helper,
  preventing old or still-locked files from blocking the download
- the update helper now also waits until the MSI installer is readable before
  starting Windows Installer

## [0.11.0] – 2026-08-11

### Added

- added automatic update checks against published GitHub Releases at startup and
  subsequently at a configurable interval
- added a compact update window with version comparison, release notes, download
  size, and progress indication
- added an update notice to the main window header and tray menu
- added a separate update helper that cleanly shuts down the Agent, installs the
  WiX MSI as a Major Upgrade, and then restarts the Agent in the system tray
- added automatic `.sha256` release file generation during the MSI build and
  after signing

### Security

- added a fixed maximum download size and streaming downloads to a temporary file
- added mandatory SHA-256 verification through the GitHub asset digest or a
  separate `.sha256` asset
- added optional Authenticode and publisher verification for public, signed
  releases
- added Windows Installer logging and a visible success or error message after
  restart

## [0.10.3] – 2026-08-11

### Changed

- updated the borders of the connection, pairing, and behavior sections to match
  the dark Nexus theme
- the address list border now also uses the central theme color
- widened the **Refresh** button so its label remains fully visible

## [0.10.2] – 2026-08-11

### Fixed

- Agent shutdown now correctly passes the five-second timeout to `StopAsync`
  through a `CancellationTokenSource`
- fixed compiler errors caused by passing a `TimeSpan` to `StopAsync`

## [0.10.1] – 2026-08-11

### Changed

- consolidated project folders and namespaces consistently
- moved API routes from `Program.cs` into a dedicated networking component
- removed unused and obsolete code
- updated documentation and build scripts for the cleaned-up structure

### Fixed

- corrected the nullable context for automatically generated WinForms code
- fixed incorrect namespace resolution for `Application.EnableVisualStyles`

## [0.10.0] – 2026-08-11

### Changed

- migrated the desktop UI completely to WinForms
- removed WPF and XAML dependencies from the Agent
- introduced a compact, non-resizable 500 × 500 pixel window
- consolidated system tray operation, pairing, server hosting, and firewall
  setup into a single Agent process

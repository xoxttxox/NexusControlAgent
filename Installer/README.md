# Nexus Control Agent – MSI Installer 0.11.4

The installer is based on WiX Toolset 7 and installs only the interactive
Nexus Control Agent for Windows x64.

## Building the MSI

1. Install the .NET 10 SDK.
2. Run `Scripts\build-msi.bat`.
3. Explicitly accept the WiX v7 OSMF EULA in the script.

The generated installer is located at:

```text
artifacts\installer\NexusControlAgent-Setup-v0.11.4-win-x64.msi
```

## Installed Components

- Nexus Control Agent using port `5188`
- firewall rules for private LAN and Tailscale
- Start menu shortcut
- optional silent tray startup
- optional desktop shortcut

The Agent requires administrator privileges because of the Windows control
commands it provides. It does not install or configure a Windows kernel service,
Credential Provider, port `5189`, or any Windows login credentials.

## Upgrade from 0.7.1

When upgrading, version 0.8.0 automatically removes components from the previous
Windows unlock feature:

- `NexusControlCore` service
- Credential Provider and COM registration
- locally protected password file and unlock key
- related registry values

Normal pairing data stored under `%ProgramData%\NexusControl` is preserved.

## Validation

Before building, `Scripts\build-msi.bat` runs
`Scripts\validate-installer.ps1`. After a test installation,
`Scripts\verify-install.ps1` can be used to validate the installation.

## Signing

For public distribution, the EXE and MSI must be signed with a trusted
code-signing certificate:

```powershell
.\Scripts\sign-release.ps1 -CertificateThumbprint YOUR_CERTIFICATE_THUMBPRINT
```

Then verify the signature:

```powershell
signtool verify /pa /v .\artifacts\installer\NexusControlAgent-Setup-v0.11.4-win-x64.msi
```

The Agent does not include a built-in updater. New versions are downloaded and
installed manually as MSI packages; WiX performs a standard Major Upgrade of the
existing installation.

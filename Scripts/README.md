# Nexus Control Agent – Scripts 0.11.4

- `build-msi.bat`: publishes the Windows x64 Agent and builds the WiX MSI.
- `publish-agent.bat`: creates only the self-contained desktop application.
- `start-agent.bat`: builds and starts the Agent locally.
- `install-firewall.bat` / `install-firewall.ps1`: configures port `5188` for
  private LAN and Tailscale networks.
- `install-msi-test.bat`: installs the generated MSI with a detailed log.
- `validate-installer.ps1`: validates the WiX source before building.
- `verify-install.ps1`: verifies the Agent, firewall, and removal of legacy
  unlock components after installation.
- `sign-release.ps1`: signs the Agent and MSI using SHA-256 and a timestamp.

MSI output:

```text
artifacts\installer\NexusControlAgent-Setup-v0.11.4-win-x64.msi
```

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

function Assert-True {
    param(
        [Parameter(Mandatory = $true)]
        [bool]$Condition,

        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$agentKey = 'HKLM:\SOFTWARE\Nexus Control\Nexus Control Agent'
$installPath = (Get-ItemProperty -LiteralPath $agentKey).InstallPath
$agentPath = Join-Path $installPath 'NexusControlAgent.exe'
Assert-True (Test-Path -LiteralPath $agentPath -PathType Leaf) 'NexusControlAgent.exe wurde nicht gefunden.'
$productVersion = (Get-Item -LiteralPath $agentPath).VersionInfo.ProductVersion
Assert-True ($productVersion -like '0.11.0*') "Unerwartete Agent-Version: $productVersion"

$legacyService = Get-CimInstance Win32_Service -Filter "Name='NexusControlCore'"
Assert-True ($null -eq $legacyService) 'Der entfernte NexusControlCore-Dienst ist noch installiert.'

$providerClsid = '{9A0D3A8B-2E6F-4C48-A7D1-6B5F2A9E7C31}'
$providerKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\$providerClsid"
$classKey = "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Classes\CLSID\$providerClsid"
Assert-True (-not (Test-Path -LiteralPath $providerKey)) 'Der entfernte Nexus Credential Provider ist noch registriert.'
Assert-True (-not (Test-Path -LiteralPath $classKey)) 'Die alte Credential-Provider-COM-Registrierung ist noch vorhanden.'

$agentProperties = Get-ItemProperty -LiteralPath $agentKey
foreach ($legacyValue in @(
    'UnlockUserSid',
    'UnlockUserName',
    'UnlockConfiguredAt',
    'AdministratorRequired'
)) {
    Assert-True (
        $null -eq $agentProperties.PSObject.Properties[$legacyValue]
    ) "Alter Registry-Wert ist noch vorhanden: $legacyValue"
}

foreach ($ruleName in @(
    'Nexus Control Agent TCP 5188',
    'Nexus Control Agent - Tailscale'
)) {
    $rule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue |
        Where-Object { $_.Enabled -eq 'True' -and $_.Action -eq 'Allow' } |
        Select-Object -First 1
    Assert-True ($null -ne $rule) "Firewall-Regel fehlt oder ist deaktiviert: $ruleName"
}

foreach ($legacyRule in @(
    'Nexus Control Core - Heimnetz',
    'Nexus Control Core - Tailscale'
)) {
    $rule = Get-NetFirewallRule -DisplayName $legacyRule -ErrorAction SilentlyContinue
    Assert-True ($null -eq $rule) "Alte Firewall-Regel ist noch vorhanden: $legacyRule"
}

$nexusData = Join-Path $env:ProgramData 'NexusControl'
foreach ($legacyFile in @(
    'unlock-credential.bin',
    'unlock-approval.bin',
    'unlock-keys.json'
)) {
    Assert-True (-not (Test-Path -LiteralPath (Join-Path $nexusData $legacyFile))) "Alte Entsperrdatei ist noch vorhanden: $legacyFile"
}

Write-Host 'Nexus Control Agent 0.11.0 ist korrekt ohne Windows-Entsperrfunktion installiert.' -ForegroundColor Green

[CmdletBinding()]
param(
    [ValidateRange(1, 65535)]
    [int]$Port = 5188,

    [string]$ExecutablePath = "",

    [switch]$CheckOnly
)

$ErrorActionPreference = "Stop"

$localRuleName = "Nexus Control Agent TCP 5188"
$tailscaleRuleName = "Nexus Control Agent - Tailscale"
$obsoleteRuleNames = @(
    "Nexus Control Agent",
    "Nexus Control Agent - Heimnetz",
    $localRuleName,
    $tailscaleRuleName
)

if ([string]::IsNullOrWhiteSpace($ExecutablePath)) {
    $applicationDirectory = Split-Path -Parent $PSScriptRoot
    $ExecutablePath = Join-Path $applicationDirectory "NexusControlAgent.exe"
}

$ExecutablePath = [IO.Path]::GetFullPath($ExecutablePath)

if ($CheckOnly) {
    $rule = Get-NetFirewallRule `
        -DisplayName $localRuleName `
        -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Enabled -eq "True" -and
            $_.Direction -eq "Inbound" -and
            $_.Action -eq "Allow"
        } |
        Select-Object -First 1

    if ($null -eq $rule) {
        exit 1
    }

    $portFilter = $rule | Get-NetFirewallPortFilter -ErrorAction SilentlyContinue
    $addressFilter = $rule | Get-NetFirewallAddressFilter -ErrorAction SilentlyContinue
    $applicationFilter = $rule | Get-NetFirewallApplicationFilter -ErrorAction SilentlyContinue

    $portMatches = @($portFilter.LocalPort) -contains [string]$Port
    $addressMatches = @($addressFilter.RemoteAddress) -contains "LocalSubnet"
    $programMatches = [string]::Equals(
        [string]$applicationFilter.Program,
        $ExecutablePath,
        [StringComparison]::OrdinalIgnoreCase
    )

    if (-not ($portMatches -and $addressMatches -and $programMatches)) {
        exit 1
    }

    exit 0
}

$currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
$isAdministrator = $currentPrincipal.IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator
)

if (-not $isAdministrator) {
    $arguments = @(
        "-NoProfile",
        "-ExecutionPolicy Bypass",
        "-File `"$PSCommandPath`"",
        "-Port $Port",
        "-ExecutablePath `"$ExecutablePath`""
    ) -join " "

    $elevatedProcess = Start-Process powershell.exe `
        -Verb RunAs `
        -Wait `
        -PassThru `
        -ArgumentList $arguments

    exit $elevatedProcess.ExitCode
}

foreach ($ruleName in $obsoleteRuleNames) {
    Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue |
        Remove-NetFirewallRule -ErrorAction SilentlyContinue
}

$localRule = @{
    DisplayName   = $localRuleName
    Description   = "Erlaubt Nexus Control auf TCP-Port $Port ausschließlich aus dem lokalen Subnetz."
    Direction     = "Inbound"
    Action        = "Allow"
    Enabled       = "True"
    Protocol      = "TCP"
    LocalPort     = $Port
    RemoteAddress = "LocalSubnet"
    Profile       = "Any"
}

if (Test-Path -LiteralPath $ExecutablePath -PathType Leaf) {
    $localRule.Program = $ExecutablePath
}

New-NetFirewallRule @localRule | Out-Null

$tailscaleRule = @{
    DisplayName   = $tailscaleRuleName
    Description   = "Erlaubt Nexus Control auf TCP-Port $Port ausschließlich über das Tailscale-Netz."
    Direction     = "Inbound"
    Action        = "Allow"
    Enabled       = "True"
    Protocol      = "TCP"
    LocalPort     = $Port
    RemoteAddress = "100.64.0.0/10"
    Profile       = "Any"
}

if (Test-Path -LiteralPath $ExecutablePath -PathType Leaf) {
    $tailscaleRule.Program = $ExecutablePath
}

New-NetFirewallRule @tailscaleRule | Out-Null

Write-Host "Windows-Firewall wurde für Nexus Control eingerichtet." -ForegroundColor Green
Write-Host "Lokaler Zugriff: LocalSubnet auf TCP-Port $Port (alle Windows-Netzwerkprofile)."
Write-Host "Tailscale-Zugriff: 100.64.0.0/10 auf TCP-Port $Port."
exit 0

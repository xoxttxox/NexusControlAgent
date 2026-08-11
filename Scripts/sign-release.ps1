param(
    [Parameter(Mandatory = $true)]
    [string]$CertificateThumbprint,

    [string]$TimestampUrl = "https://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$appPath = Join-Path $projectRoot "artifacts\publish\win-x64\NexusControlAgent.exe"
$wixProject = Join-Path $projectRoot "Installer\Msi\NexusControlAgent.Installer.wixproj"
$msiPath = Join-Path $projectRoot "artifacts\installer\NexusControlAgent-Setup-v0.10.3-win-x64.msi"

$signtool = Get-Command signtool.exe -ErrorAction SilentlyContinue
if (-not $signtool) {
    throw "signtool.exe wurde nicht gefunden. Installiere das Windows SDK und starte die Developer PowerShell erneut."
}

if (-not (Test-Path $appPath)) {
    throw "Datei wurde nicht gefunden: $appPath"
}

& $signtool.Source sign `
    /sha1 $CertificateThumbprint `
    /fd SHA256 `
    /td SHA256 `
    /tr $TimestampUrl `
    $appPath
if ($LASTEXITCODE -ne 0) {
    throw "Signierung fehlgeschlagen: $appPath"
}

& dotnet build $wixProject `
    --configuration Release `
    --no-restore `
    -p:AcceptEula=wix7
if ($LASTEXITCODE -ne 0) {
    throw "Das MSI konnte mit dem signierten Agenten nicht neu gebaut werden."
}

& $signtool.Source sign `
    /sha1 $CertificateThumbprint `
    /fd SHA256 `
    /td SHA256 `
    /tr $TimestampUrl `
    $msiPath
if ($LASTEXITCODE -ne 0) {
    throw "MSI-Signierung fehlgeschlagen: $msiPath"
}

foreach ($path in @($appPath, $msiPath)) {
    & $signtool.Source verify /pa /v $path
    if ($LASTEXITCODE -ne 0) {
        throw "Signaturprüfung fehlgeschlagen: $path"
    }
}

Write-Host "Desktop-Begleiter und MSI wurden erfolgreich signiert und geprüft."

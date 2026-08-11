param(
    [Parameter(Mandatory = $true)]
    [string]$CertificateThumbprint,

    [string]$TimestampUrl = "https://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$appPath = Join-Path $projectRoot "artifacts\publish\win-x64\NexusControlAgent.exe"
$wixProject = Join-Path $projectRoot "Installer\Msi\NexusControlAgent.Installer.wixproj"
$msiPath = Join-Path $projectRoot "artifacts\installer\NexusControlAgent-Setup-v0.11.1-win-x64.msi"
$localizedMsiPath = Join-Path $projectRoot "artifacts\installer\de-DE\NexusControlAgent-Setup-v0.11.1-win-x64.msi"
$checksumScript = Join-Path $PSScriptRoot "New-ReleaseChecksum.ps1"

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

if (Test-Path -LiteralPath $localizedMsiPath) {
    Copy-Item -LiteralPath $localizedMsiPath -Destination $msiPath -Force
}
if (-not (Test-Path -LiteralPath $msiPath -PathType Leaf)) {
    throw "Das neu gebaute MSI wurde nicht gefunden: $msiPath"
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

& $checksumScript -Path $msiPath
if ($LASTEXITCODE -ne 0) {
    throw "Die SHA-256-Datei konnte nicht erstellt werden."
}

Write-Host "Desktop-Begleiter und MSI wurden erfolgreich signiert, geprüft und mit SHA-256 versehen."

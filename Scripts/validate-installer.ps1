[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [Parameter(Mandatory = $true)]
    [string]$WixProjectPath
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$packageFullPath = [System.IO.Path]::GetFullPath($PackagePath)
$projectFullPath = [System.IO.Path]::GetFullPath($WixProjectPath)

if (-not (Test-Path -LiteralPath $packageFullPath -PathType Leaf)) {
    throw "Package.wxs wurde nicht gefunden: $packageFullPath"
}

if (-not (Test-Path -LiteralPath $projectFullPath -PathType Leaf)) {
    throw "WiX-Projekt wurde nicht gefunden: $projectFullPath"
}

[xml]$package = Get-Content -LiteralPath $packageFullPath -Raw -Encoding UTF8
$namespace = New-Object System.Xml.XmlNamespaceManager($package.NameTable)
$namespace.AddNamespace('w', 'http://wixtoolset.org/schemas/v4/wxs')
$namespace.AddNamespace('ui', 'http://wixtoolset.org/schemas/v4/wxs/ui')
$namespace.AddNamespace('firewall', 'http://wixtoolset.org/schemas/v4/wxs/firewall')

$propertyNodes = @($package.SelectNodes('//w:Property', $namespace))
$duplicateProperties = $propertyNodes |
    Group-Object { $_.Id } |
    Where-Object { $_.Name -and $_.Count -gt 1 }

if ($duplicateProperties) {
    $ids = ($duplicateProperties | ForEach-Object Name) -join ', '
    throw "Doppelte WiX-Property-IDs gefunden: $ids"
}

$explicitInstallDirProperty = $package.SelectSingleNode(
    '//w:Property[@Id="WIXUI_INSTALLDIR"]',
    $namespace)
$wixUiWithInstallDirectory = $package.SelectSingleNode(
    '//ui:WixUI[@InstallDirectory]',
    $namespace)

if ($explicitInstallDirProperty -and $wixUiWithInstallDirectory) {
    throw ('WIXUI_INSTALLDIR ist doppelt definiert. ' +
        'Bei ui:WixUI mit InstallDirectory darf keine zusaetzliche ' +
        'Property Id="WIXUI_INSTALLDIR" vorhanden sein.')
}

# ICE38, ICE43 and ICE57 require shortcut components that install into the
# current user's profile to use an HKCU registry value as their KeyPath.
$shortcutComponents = @($package.SelectNodes('//w:Component[w:Shortcut]', $namespace))
foreach ($component in $shortcutComponents) {
    $keyPaths = @($component.SelectNodes('./w:RegistryValue[@KeyPath="yes"]', $namespace))
    if ($keyPaths.Count -ne 1) {
        throw "Shortcut-Komponente '$($component.Id)' benoetigt genau einen Registry-KeyPath."
    }

    if ($keyPaths[0].Root -ne 'HKCU') {
        throw ("Shortcut-Komponente '$($component.Id)' verwendet Root='$($keyPaths[0].Root)'. " +
            'Fuer Desktop-/Startmenue-Shortcuts muss der KeyPath unter HKCU liegen ' +
            '(ICE38/ICE43/ICE57).')
    }
}

# Avoid ICE90 for a custom Start-menu folder whose identifier is accidentally
# authored as a public MSI property (all uppercase).
$startMenuComponent = $package.SelectSingleNode(
    '//w:Component[@Id="StartMenuShortcutComponent"]',
    $namespace)
if ($startMenuComponent -and $startMenuComponent.Directory -cmatch '^[A-Z0-9_]+$') {
    throw ("Das Startmenue-Verzeichnis '$($startMenuComponent.Directory)' ist komplett grossgeschrieben. " +
        'Verwende eine gemischte Directory-ID, damit ICE90 nicht ausgeloest wird.')
}

[xml]$wixProject = Get-Content -LiteralPath $projectFullPath -Raw -Encoding UTF8
$bindPath = @($wixProject.Project.ItemGroup.BindPath) |
    Where-Object { $_.BindName -eq 'PublishedApp' }

if (-not $bindPath) {
    throw 'Im WiX-Projekt fehlt der BindPath mit BindName="PublishedApp".'
}

$filesNode = $package.SelectSingleNode(
    '//w:Files[contains(@Include, "bindpath.PublishedApp")]',
    $namespace)
if (-not $filesNode) {
    throw 'Package.wxs verwendet den Bind-Pfad PublishedApp nicht.'
}

$serviceNodes = @($package.SelectNodes('//w:ServiceInstall', $namespace))
if ($serviceNodes.Count -ne 0) {
    throw 'Der Installer darf keinen Windows-Kerndienst mehr installieren.'
}

$legacyServiceCleanup = $package.SelectSingleNode(
    '//w:ServiceControl[@Name="NexusControlCore" and @Stop="install" and @Remove="install"]',
    $namespace)
if (-not $legacyServiceCleanup) {
    throw 'Die Upgrade-Bereinigung für den früheren NexusControlCore-Dienst fehlt.'
}

$credentialProviderRegistration = $package.SelectSingleNode(
    '//w:RegistryValue[contains(@Key, "Authentication\Credential Providers")]',
    $namespace)
if ($credentialProviderRegistration) {
    throw 'Der Installer darf keinen Windows Credential Provider mehr registrieren.'
}

$legacyProviderCleanup = @($package.SelectNodes(
    '//w:RemoveRegistryKey[contains(@Key, "9A0D3A8B-2E6F-4C48-A7D1-6B5F2A9E7C31") and @Action="removeOnInstall"]',
    $namespace))
if ($legacyProviderCleanup.Count -lt 2) {
    throw 'Die Upgrade-Bereinigung der früheren Credential-Provider-Registrierung fehlt.'
}

$legacyUnlockFiles = @(
    'unlock-credential.bin',
    'unlock-approval.bin',
    'unlock-keys.json'
)
foreach ($legacyUnlockFile in $legacyUnlockFiles) {
    $escapedName = $legacyUnlockFile.Replace("'", "&apos;")
    $cleanupNode = $package.SelectSingleNode(
        "//w:RemoveFile[@Name='$escapedName' and @On='install']",
        $namespace)
    if (-not $cleanupNode) {
        throw "Die Upgrade-Bereinigung für '$legacyUnlockFile' fehlt."
    }
}

$removedPortRules = @($package.SelectNodes(
    '//firewall:FirewallException[@Port="5189"]',
    $namespace))
if ($removedPortRules.Count -ne 0) {
    throw 'Der entfernte Port 5189 darf nicht mehr freigegeben werden.'
}

$desktopFirewallRules = @($package.SelectNodes(
    '//firewall:FirewallException[@Port="5188"]',
    $namespace))
if ($desktopFirewallRules.Count -lt 2) {
    throw 'Die Firewall-Regeln für Heimnetz und Tailscale auf Port 5188 fehlen.'
}

# The whole built-in WixUI must be localized to German. Language="1031" alone
# only sets the MSI product language; the extension strings require Cultures.
$cultures = [string]$wixProject.Project.PropertyGroup.Cultures
if ($cultures -notmatch '(^|;)de-DE(;|$)') {
    throw 'Im WiX-Projekt fehlt <Cultures>de-DE</Cultures>.'
}

$licenseVariable = $package.SelectSingleNode(
    '//w:WixVariable[@Id="WixUILicenseRtf"]',
    $namespace)
if (-not $licenseVariable) {
    throw 'Die deutsche Lizenzdatei WixUILicenseRtf fehlt.'
}

$packageDirectory = Split-Path -Parent $packageFullPath
$licensePath = [System.IO.Path]::GetFullPath(
    (Join-Path $packageDirectory ([string]$licenseVariable.Value)))
if (-not (Test-Path -LiteralPath $licensePath -PathType Leaf)) {
    throw "Die deutsche Lizenzdatei wurde nicht gefunden: $licensePath"
}

$errorProgressRef = $package.SelectSingleNode(
    '//w:UIRef[@Id="WixUI_ErrorProgressText"]',
    $namespace)
if (-not $errorProgressRef) {
    throw 'WixUI_ErrorProgressText fehlt; Fortschrittstexte wären nicht vollständig deutsch.'
}

$enableAutoStartAction = $package.SelectSingleNode(
    '//w:CustomAction[@Id="EnableAutoStart"]',
    $namespace)
if ($enableAutoStartAction) {
    throw 'EnableAutoStart darf die MSI-Installation nicht mehr als CustomAction blockieren.'
}

$launchAction = $package.SelectSingleNode(
    '//w:CustomAction[@Id="LaunchAgent"]',
    $namespace)
if (-not $launchAction -or $launchAction.DllEntry -ne 'WixShellExec') {
    throw 'Der optionale Programmstart muss WixShellExec verwenden.'
}
if ($launchAction.Return -ne 'ignore') {
    throw 'LaunchAgent muss Return="ignore" verwenden und darf die fertige Installation nicht nachträglich als fehlgeschlagen markieren.'
}

$utilReference = @($wixProject.Project.ItemGroup.PackageReference) |
    Where-Object { $_.Include -eq 'WixToolset.Util.wixext' }
if (-not $utilReference) {
    throw 'Im WiX-Projekt fehlt WixToolset.Util.wixext für WixShellExec.'
}

$firewallReference = @($wixProject.Project.ItemGroup.PackageReference) |
    Where-Object { $_.Include -eq 'WixToolset.Firewall.wixext' }
if (-not $firewallReference) {
    throw 'Im WiX-Projekt fehlt WixToolset.Firewall.wixext.'
}

Write-Host 'Installer-Vorabpruefung erfolgreich.' -ForegroundColor Green

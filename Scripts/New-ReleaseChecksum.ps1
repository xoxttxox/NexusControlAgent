param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Path
)

$ErrorActionPreference = "Stop"

$resolvedPath = (Resolve-Path -LiteralPath $Path).Path
if (-not (Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
    throw "Release-Datei wurde nicht gefunden: $Path"
}

$file = Get-Item -LiteralPath $resolvedPath
if ($file.Length -le 0) {
    throw "Release-Datei ist leer: $resolvedPath"
}

$hash = (Get-FileHash -LiteralPath $resolvedPath -Algorithm SHA256).Hash.ToUpperInvariant()
$checksumPath = "$resolvedPath.sha256"
$checksumLine = "$hash  $($file.Name)"

[System.IO.File]::WriteAllText(
    $checksumPath,
    $checksumLine + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

Write-Host "SHA-256 erstellt: $checksumPath"
Write-Host $checksumLine

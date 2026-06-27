param(
    [string]$AssemblyPath
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$dllPath = if ([string]::IsNullOrWhiteSpace($AssemblyPath)) {
    Get-ChildItem -Path $scriptRoot -Filter "PztGenerator*.dll" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
} else {
    $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($AssemblyPath)
}

if (-not (Test-Path $dllPath)) {
    throw "Nie znaleziono PztGenerator.dll: $dllPath"
}

$addinDir = Join-Path $env:APPDATA "Autodesk\Revit\Addins\2025"
New-Item -ItemType Directory -Force -Path $addinDir | Out-Null

$addinPath = Join-Path $addinDir "PztGenerator.addin"
$addinXml = @"
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>PZT Generator</Name>
    <Assembly>$dllPath</Assembly>
    <AddInId>7F9AB5A8-27D8-4B6D-95D3-74C4C9D7F9A1</AddInId>
    <FullClassName>PztGenerator.App</FullClassName>
    <VendorId>GRAF</VendorId>
    <VendorDescription>GRAFEL</VendorDescription>
  </AddIn>
</RevitAddIns>
"@

Set-Content -Path $addinPath -Value $addinXml -Encoding UTF8

Write-Host "Zainstalowano PZT Generator dla Revit 2025:"
Write-Host $addinPath
Write-Host "Uruchom ponownie Revit 2025."

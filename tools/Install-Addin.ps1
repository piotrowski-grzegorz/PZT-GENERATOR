param(
    [string]$AssemblyPath
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$dllPath = if ([string]::IsNullOrWhiteSpace($AssemblyPath)) {
    Join-Path $projectRoot "src\PztGenerator\bin\Debug\net8.0-windows\PztGenerator.dll"
} else {
    $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($AssemblyPath)
}

if (-not (Test-Path $dllPath)) {
    throw "Nie znaleziono pliku DLL. Najpierw uruchom: dotnet build .\src\PztGenerator\PztGenerator.csproj -c Debug"
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

Write-Host "Zainstalowano manifest dodatku:"
Write-Host $addinPath
Write-Host "Uruchom ponownie Revit 2025."

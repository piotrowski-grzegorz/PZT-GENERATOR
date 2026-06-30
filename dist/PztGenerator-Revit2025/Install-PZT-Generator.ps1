param(
    [string]$RevitPath,
    [string]$SourceDll
)

$ErrorActionPreference = "Stop"

function Read-RequiredPath($promptText) {
    while ($true) {
        $value = Read-Host $promptText
        if (-not [string]::IsNullOrWhiteSpace($value) -and (Test-Path $value)) {
            return $value
        }

        Write-Host "Nie znaleziono sciezki. Sprobuj ponownie." -ForegroundColor Yellow
    }
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

if ([string]::IsNullOrWhiteSpace($SourceDll)) {
    $SourceDll = Join-Path $scriptRoot "PztGenerator.dll"
}

$sourceDllPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($SourceDll)
if (-not (Test-Path $sourceDllPath)) {
    throw "Nie znaleziono pliku PztGenerator.dll w paczce: $sourceDllPath"
}

if ([string]::IsNullOrWhiteSpace($RevitPath)) {
    $defaultRevitPath = "C:\Program Files\Autodesk\Revit 2025"
    if (Test-Path $defaultRevitPath) {
        $answer = Read-Host "Znaleziono domyslna sciezke Revita: $defaultRevitPath. Uzyc jej? [T/n]"
        if ([string]::IsNullOrWhiteSpace($answer) -or $answer.Trim().ToLowerInvariant() -in @("t", "tak", "y", "yes")) {
            $RevitPath = $defaultRevitPath
        }
    }
}

if ([string]::IsNullOrWhiteSpace($RevitPath)) {
    $RevitPath = Read-RequiredPath "Podaj pelna sciezke folderu Revit 2025, np. E:\Program Files\Autodesk\Revit 2025"
}

$revitPathResolved = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RevitPath)
$revitExe = Join-Path $revitPathResolved "Revit.exe"
if (-not (Test-Path $revitExe)) {
    Write-Host "Uwaga: w podanej sciezce nie znaleziono Revit.exe." -ForegroundColor Yellow
    Write-Host "Sciezka: $revitPathResolved"
    $continue = Read-Host "Kontynuowac instalacje dodatku mimo tego? [t/N]"
    if ($continue.Trim().ToLowerInvariant() -notin @("t", "tak", "y", "yes")) {
        throw "Przerwano instalacje."
    }
}

$installDir = Join-Path $env:APPDATA "GRAFEL\PZT Generator\Revit2025"
New-Item -ItemType Directory -Force -Path $installDir | Out-Null

$installedDll = Join-Path $installDir "PztGenerator.dll"
Copy-Item -Path $sourceDllPath -Destination $installedDll -Force

$addinDir = Join-Path $env:APPDATA "Autodesk\Revit\Addins\2025"
New-Item -ItemType Directory -Force -Path $addinDir | Out-Null

$addinPath = Join-Path $addinDir "PztGenerator.addin"
$addinXml = @"
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>PZT Generator</Name>
    <Assembly>$installedDll</Assembly>
    <AddInId>7F9AB5A8-27D8-4B6D-95D3-74C4C9D7F9A1</AddInId>
    <FullClassName>PztGenerator.App</FullClassName>
    <VendorId>GRAF</VendorId>
    <VendorDescription>GRAFEL</VendorDescription>
  </AddIn>
</RevitAddIns>
"@

Set-Content -Path $addinPath -Value $addinXml -Encoding UTF8

Write-Host ""
Write-Host "Zainstalowano PZT Generator MVP." -ForegroundColor Green
Write-Host "Revit: $revitPathResolved"
Write-Host "DLL: $installedDll"
Write-Host "Manifest: $addinPath"
Write-Host ""
Write-Host "Zamknij i uruchom ponownie Revit 2025. Po starcie powinna pojawic sie zakladka PZT."

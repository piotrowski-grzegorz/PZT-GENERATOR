$ErrorActionPreference = "Stop"

$addinPath = Join-Path $env:APPDATA "Autodesk\Revit\Addins\2025\PztGenerator.addin"
$installDir = Join-Path $env:APPDATA "GRAFEL\PZT Generator\Revit2025"

if (Test-Path $addinPath) {
    Remove-Item -Path $addinPath -Force
    Write-Host "Usunieto manifest: $addinPath"
} else {
    Write-Host "Nie znaleziono manifestu: $addinPath"
}

if (Test-Path $installDir) {
    Remove-Item -Path $installDir -Recurse -Force
    Write-Host "Usunieto folder dodatku: $installDir"
} else {
    Write-Host "Nie znaleziono folderu dodatku: $installDir"
}

Write-Host "Odinstalowano PZT Generator. Uruchom ponownie Revit 2025."

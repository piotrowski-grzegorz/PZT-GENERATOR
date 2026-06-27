$ErrorActionPreference = "SilentlyContinue"

$roots = @(
    "C:\Program Files\Autodesk",
    "C:\Program Files",
    "D:\",
    "E:\",
    "F:\"
)

foreach ($root in $roots) {
    if (-not (Test-Path $root)) {
        continue
    }

    Get-ChildItem -Path $root -Filter "RevitAPI.dll" -Recurse |
        ForEach-Object {
            [PSCustomObject]@{
                RevitApi = $_.FullName
                RevitInstallDir = $_.DirectoryName
            }
        }
}

#Requires -Version 5.1
$ErrorActionPreference = "Stop"

$InstallDir = Join-Path $env:LOCALAPPDATA "Programs\DevDesk"

Get-Process -Name "DevDesk" -ErrorAction SilentlyContinue | Stop-Process -Force

$startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\DevDesk.lnk"
$desktop = Join-Path ([Environment]::GetFolderPath("Desktop")) "DevDesk.lnk"
foreach ($link in @($startMenu, $desktop)) {
    if (Test-Path $link) { Remove-Item $link -Force }
}

$regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\DevDesk"
if (Test-Path $regPath) { Remove-Item $regPath -Recurse -Force }

if (Test-Path $InstallDir) {
    Remove-Item $InstallDir -Recurse -Force
}

Write-Host "DevDesk has been uninstalled."

#Requires -Version 5.1
param(
    [switch]$SkipPublish,
    [switch]$NoDesktopShortcut,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path $PSScriptRoot -Parent
$Project = Join-Path $Root "DevDesk.WinForms\DevDesk.WinForms.csproj"
$PublishDir = Join-Path $Root "publish\win-x64"
$InstallDir = Join-Path $env:LOCALAPPDATA "Programs\DevDesk"
$ExeName = "DevDesk.exe"

Write-Host "DevDesk portable copy-install (current user, no Setup.exe)." -ForegroundColor Cyan
Write-Host "For a Control Panel installer, run: scripts\Build-Installer.ps1" -ForegroundColor DarkGray
Write-Host ""

if (-not $SkipPublish) {
    Write-Host "Publishing self-contained win-x64 Release..."
    dotnet publish $Project `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $PublishDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}

$publishedExe = Join-Path $PublishDir $ExeName
if (-not (Test-Path $publishedExe)) {
    throw "Published exe not found: $publishedExe"
}

Get-Process -Name "DevDesk" -ErrorAction SilentlyContinue | Stop-Process -Force

New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Copy-Item -Path (Join-Path $PublishDir "*") -Destination $InstallDir -Recurse -Force

$uninstallScript = Join-Path $PSScriptRoot "Uninstall-DevDesk.ps1"
if (Test-Path $uninstallScript) {
    Copy-Item $uninstallScript -Destination (Join-Path $InstallDir "Uninstall-DevDesk.ps1") -Force
}

$exe = Join-Path $InstallDir $ExeName
$shell = New-Object -ComObject WScript.Shell

$startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\DevDesk.lnk"
$shortcut = $shell.CreateShortcut($startMenu)
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $InstallDir
$shortcut.Description = "DevDesk"
$shortcut.Save()
Write-Host "Start Menu shortcut: $startMenu"

if (-not $NoDesktopShortcut) {
    $desktop = Join-Path ([Environment]::GetFolderPath("Desktop")) "DevDesk.lnk"
    $desk = $shell.CreateShortcut($desktop)
    $desk.TargetPath = $exe
    $desk.WorkingDirectory = $InstallDir
    $desk.Description = "DevDesk"
    $desk.Save()
    Write-Host "Desktop shortcut: $desktop"
}

$uninstallCmd = "powershell.exe -NoProfile -ExecutionPolicy Bypass -File `"$(Join-Path $InstallDir 'Uninstall-DevDesk.ps1')`""
$regPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\DevDesk"
New-Item -Path $regPath -Force | Out-Null
Set-ItemProperty $regPath -Name "DisplayName" -Value "DevDesk"
Set-ItemProperty $regPath -Name "DisplayVersion" -Value "1.0.0"
Set-ItemProperty $regPath -Name "Publisher" -Value "DevDesk"
Set-ItemProperty $regPath -Name "InstallLocation" -Value $InstallDir
Set-ItemProperty $regPath -Name "DisplayIcon" -Value $exe
Set-ItemProperty $regPath -Name "UninstallString" -Value $uninstallCmd
Set-ItemProperty $regPath -Name "QuietUninstallString" -Value $uninstallCmd
Set-ItemProperty $regPath -Name "NoModify" -Value 1 -Type DWord
Set-ItemProperty $regPath -Name "NoRepair" -Value 1 -Type DWord

$localDb = Get-Command sqllocaldb -ErrorAction SilentlyContinue
if ($localDb) {
    & sqllocaldb start MSSQLLocalDB 2>$null | Out-Null
    Write-Host "SQL Server LocalDB is available."
} else {
    Write-Host "Warning: SQL Server LocalDB was not found. DevDesk needs LocalDB to run." -ForegroundColor Yellow
    Write-Host "Install 'SQL Server Express LocalDB' if the app fails to start." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Installed to $InstallDir" -ForegroundColor Green
Write-Host "Launch from the Start Menu or Desktop shortcut, or run:"
Write-Host "  $exe"

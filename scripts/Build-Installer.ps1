#Requires -Version 5.1
param(
    [switch]$SkipPublish,
    [switch]$OpenOutput,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$Root = Split-Path $PSScriptRoot -Parent
$Project = Join-Path $Root "DevDesk.WinForms\DevDesk.WinForms.csproj"
$PublishDir = Join-Path $Root "publish\win-x64"
$InstallerDir = Join-Path $Root "publish\installer"
$Iss = Join-Path $Root "installer\DevDesk.iss"
$InnoProject = Join-Path $Root "installer\InnoSetup.Build.csproj"
$InnoVersion = "6.7.1"

Write-Host "Building DevDesk Setup.exe" -ForegroundColor Cyan

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

$publishedExe = Join-Path $PublishDir "DevDesk.exe"
if (-not (Test-Path $publishedExe)) {
    throw "Published exe not found: $publishedExe. Run without -SkipPublish."
}

function Find-Iscc {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
        "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe"
        "${env:ProgramFiles}\Inno Setup 7\ISCC.exe"
        (Join-Path $env:USERPROFILE ".nuget\packages\tools.innosetup\$InnoVersion\tools\ISCC.exe")
        (Join-Path $Root "tools\innosetup\tools\ISCC.exe")
    )

    $propsPath = Join-Path $Root "installer\obj\InnoSetup.Build.csproj.nuget.g.props"
    if (Test-Path $propsPath) {
        $props = Get-Content $propsPath -Raw
        if ($props -match 'PkgTools_InnoSetup Condition[^>]*>\s*([^<]+)\s*<') {
            $candidates += Join-Path $Matches[1].Trim() "tools\ISCC.exe"
        }
    }

    Get-ChildItem -Path @(
        (Join-Path $env:USERPROFILE ".nuget\packages\tools.innosetup")
        (Join-Path $env:LOCALAPPDATA "Temp\cursor-sandbox-cache")
    ) -Filter ISCC.exe -Recurse -ErrorAction SilentlyContinue |
        ForEach-Object { $candidates += $_.FullName }

    foreach ($path in $candidates) {
        if ($path -and (Test-Path $path)) { return $path }
    }
    return $null
}

function Get-IsccPath {
    $existing = Find-Iscc
    if ($existing) { return $existing }

    Write-Host "Restoring Inno Setup compiler..."
    dotnet restore $InnoProject
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore Tools.InnoSetup"
    }

    $restored = Find-Iscc
    if (-not $restored) {
        throw "ISCC.exe not found after restore. Install Inno Setup 6 or restore Tools.InnoSetup."
    }
    return $restored
}

$csprojText = Get-Content $Project -Raw
$version = "1.0.0"
if ($csprojText -match "<Version>([^<]+)</Version>") {
    $version = $Matches[1].Trim()
}

$iscc = Get-IsccPath
New-Item -ItemType Directory -Force -Path $InstallerDir | Out-Null

$setup = Join-Path $InstallerDir "DevDesk-Setup.exe"
if (Test-Path $setup) {
    Remove-Item $setup -Force -ErrorAction SilentlyContinue
}

Write-Host "Compiling installer $version with $iscc"
& $iscc "/DMyAppVersion=$version" $Iss
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compile failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $setup)) {
    throw "Setup exe was not created: $setup"
}

Write-Host ""
Write-Host "Installer ready:" -ForegroundColor Green
Write-Host "  $setup"
Write-Host ""
Write-Host "Run that Setup.exe (UAC will ask for admin). It installs to Program Files,"
Write-Host "adds DevDesk to Control Panel > Programs, and creates unins000.exe."

if ($OpenOutput) {
    Start-Process explorer.exe -ArgumentList "/select,`"$setup`""
}

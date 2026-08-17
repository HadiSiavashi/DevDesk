@echo off
setlocal
cd /d "%~dp0"
title DevDesk Setup Builder

echo.
echo  Building DevDesk-Setup.exe from current source...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Build-Installer.ps1" -OpenOutput
if errorlevel 1 (
    echo.
    echo  BUILD FAILED
    echo.
    pause
    exit /b 1
)

echo.
echo  Done. DevDesk-Setup.exe is in publish\installer
echo.
pause

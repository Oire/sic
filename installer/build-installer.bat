@echo off
REM SIC! Installer Build Script (Batch wrapper)
REM Copyright © 2026 Oire Software SARL.

echo Building SIC! Installer...
echo.

REM Execute PowerShell script with execution policy bypass
powershell.exe -ExecutionPolicy Bypass -File "%~dp0build-installer.ps1" -OpenOutput %*

if %ERRORLEVEL% neq 0 (
    echo.
    echo Build failed with error code %ERRORLEVEL%
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Build completed successfully!
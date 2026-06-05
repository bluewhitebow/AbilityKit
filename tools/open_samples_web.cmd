@echo off
setlocal

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0open_samples_web.ps1" %*
if errorlevel 1 (
    echo.
    pause
)

endlocal

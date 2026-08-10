@echo off
REM Wrapper que evita el bloqueo de PowerShell Execution Policy.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0publish.ps1" %*

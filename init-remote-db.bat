@echo off
REM Wrapper que evita el bloqueo de PowerShell Execution Policy.
REM Usalo si te aparece el error "script is not digitally signed".
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0init-remote-db.ps1" %*

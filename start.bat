@echo off
REM Wrapper para usuarios que no quieran lidiar con la politica de ejecucion de PowerShell.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0start.ps1" %*

@echo off
REM Wrapper para correr migrate.ps1 sin problemas de ExecutionPolicy.
REM Pasa todos los argumentos al script de PowerShell.

powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0migrate.ps1" %*

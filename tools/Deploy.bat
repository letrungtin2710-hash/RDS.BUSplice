@echo off
rem Build impl dll + deploy macro RDS-BUSplice vao catalog Tekla 2020 (khong can tat Tekla khi deploy lai).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Deploy-Macro.ps1" %*
pause

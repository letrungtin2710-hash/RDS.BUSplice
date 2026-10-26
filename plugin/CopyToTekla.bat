@echo off
:: Copy ban RDS.BUSplice.dll MOI NHAT (bin\Debug hoac bin\Release) vao plugins Tekla 2020 + thumbnail catalog. Phai TAT Tekla truoc.
net session >nul 2>&1 || (powershell -Command "Start-Process '%~f0' -Verb RunAs" & exit /b)
tasklist /FI "IMAGENAME eq TeklaStructures.exe" | find /I "TeklaStructures.exe" >nul && (echo Tekla dang mo - hay tat Tekla roi chay lai. & pause & exit /b)
set DEST=C:\Program Files\Tekla Structures\2020.0\nt\bin\plugins
for /f "delims=" %%F in ('powershell -NoProfile -Command "Get-ChildItem -Path '%~dp0bin' -Recurse -Filter RDS.BUSplice.dll -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName"') do set SRC=%%F
if "%SRC%"=="" (echo Khong tim thay RDS.BUSplice.dll - hay Build truoc & pause & exit /b)
copy /Y "%SRC%" "%DEST%\RDS.BUSplice.dll" && echo Da copy: %SRC%
copy /Y "%~dp0et_element_RDS _ BU Splice.bmp" "C:\Program Files\Tekla Structures\2020.0\nt\bitmaps\" >nul 2>&1
pause

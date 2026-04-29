@echo off
chcp 437 >nul
setlocal enabledelayedexpansion

:: ============================================================
:: archive-change.bat
:: Usage: archive-change.bat <change-name>
:: Example: archive-change.bat add-npc-system
::
:: Moves openspec/changes/<name> to
::       openspec/changes/archive/YYYY-MM-DD-<name>
:: ============================================================

if "%~1"=="" (
    echo [ERROR] Please provide a change name.
    echo Usage: archive-change.bat ^<change-name^>
    echo.
    echo Available changes:
    for /D %%d in ("%~dp0*") do (
        if /I not "%%~nxd"=="archive" (
            echo   - %%~nxd
        )
    )
    pause
    exit /b 1
)

set CHANGE_NAME=%~nx1
set CHANGE_DIR=%~1
set ARCHIVE_BASE=%~dp0archive

:: Check if change directory exists
if not exist "%CHANGE_DIR%" (
    echo [ERROR] Change directory not found: %CHANGE_DIR%
    pause
    exit /b 1
)

:: Get current date YYYY-MM-DD
for /f "tokens=1-3 delims=-" %%a in ('powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd"') do (
    set TODAY=%%a-%%b-%%c
)

set ARCHIVE_TARGET=%ARCHIVE_BASE%\%TODAY%-%CHANGE_NAME%

:: Check if target already exists
if exist "%ARCHIVE_TARGET%" (
    echo [ERROR] Archive target already exists: %ARCHIVE_TARGET%
    echo Please check for duplicate archive or try again tomorrow.
    pause
    exit /b 1
)

:: Create archive directory if not exists
if not exist "%ARCHIVE_BASE%" (
    mkdir "%ARCHIVE_BASE%"
    echo [INFO] Created archive directory: %ARCHIVE_BASE%
)

:: Move the change directory
echo [INFO] Archiving...
echo   From: %CHANGE_DIR%
echo   To:   %ARCHIVE_TARGET%

move "%CHANGE_DIR%" "%ARCHIVE_TARGET%" >nul

if errorlevel 1 (
    echo [ERROR] Move failed. Please check if any file is in use.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo  Archive complete!
echo  Change:  %CHANGE_NAME%
echo  Archived to: %ARCHIVE_TARGET%
echo ============================================================

endlocal
pause


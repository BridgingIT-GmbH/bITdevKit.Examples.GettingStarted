@echo off
REM Simple wrapper to run dkx.ps1 from repository root
SETLOCAL
set SCRIPT_DIR=%~dp0
set SCRIPT=%SCRIPT_DIR%.dkx\dkx-cli.ps1
IF NOT EXIST "%SCRIPT%" (
  echo Cannot find dkx.ps1 in %SCRIPT_DIR%
  exit /b 2
)

REM Prefer pwsh (PowerShell Core)
where pwsh >nul 2>nul
IF %ERRORLEVEL%==0 (
  pwsh -NoProfile -File "%SCRIPT%" %*
  exit /b %ERRORLEVEL%
)

REM Fallback to Windows PowerShell
where powershell >nul 2>nul
IF %ERRORLEVEL%==0 (
  powershell -NoProfile -File "%SCRIPT%" %*
  exit /b %ERRORLEVEL%
)

echo No PowerShell interpreter found. Install PowerShell or use the repository on Windows.
exit /b 3

@echo off
echo ============================================
echo DS4 Customizer Pro - Beta Release Builder
echo ============================================
echo.

REM Clean previous builds
echo [1/4] Cleaning previous builds...
if exist publish rmdir /s /q publish
mkdir publish
mkdir publish\single-file
mkdir publish\portable

REM Build single-file executable
echo.
echo [2/4] Building single-file executable...
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:PublishReadyToRun=true ^
  -p:Version=0.9.0-beta ^
  -o publish\single-file

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Single-file build failed!
    pause
    exit /b %ERRORLEVEL%
)

REM Build portable version
echo.
echo [3/4] Building portable version...
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:Version=0.9.0-beta ^
  -o publish\portable

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Portable build failed!
    pause
    exit /b %ERRORLEVEL%
)

REM Copy Models folder to portable
echo.
echo [4/4] Copying assets...
xcopy /E /I /Y Models publish\portable\Models

REM Rename executables
echo.
echo Renaming files...
cd publish\single-file
ren Dualshock4Customizer.exe DS4Customizer-v0.9.0-beta-win-x64.exe
cd ..\..

REM Create ZIP for portable
echo.
echo Creating portable ZIP...
powershell Compress-Archive -Path publish\portable\* -DestinationPath publish\DS4Customizer-v0.9.0-beta-Portable.zip -Force

echo.
echo ============================================
echo Build completed successfully!
echo ============================================
echo.
echo Release files:
echo - Single-file: publish\single-file\DS4Customizer-v0.9.0-beta-win-x64.exe
echo - Portable ZIP: publish\DS4Customizer-v0.9.0-beta-Portable.zip
echo.
echo File sizes:
dir publish\single-file\*.exe | find "DS4"
dir publish\*.zip | find "DS4"
echo.
pause

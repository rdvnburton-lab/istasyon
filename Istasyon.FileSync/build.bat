@echo off
echo Istasyon.FileSync Derleniyor...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
if %errorlevel% neq 0 (
    echo Derleme basarisiz oldu! Lutfen .NET 9 SDK yuklu oldugundan emin olun.
    pause
    exit /b %errorlevel%
)
echo.
echo ===================================================
echo Derleme Basarili!
echo Uygulama yolu: bin\Release\net9.0-windows\win-x64\publish\Istasyon.FileSync.exe
echo ===================================================
pause

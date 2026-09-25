@echo off
setlocal
echo ======================================================================
echo Compilando CigamPrintTest (.NET Framework 4.8 / AnyCPU)
echo ======================================================================

if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" (
    set "PATH=%LOCALAPPDATA%\Microsoft\dotnet;%PATH%"
    set "DOTNET_CMD=%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe"
) else (
    set "DOTNET_CMD=dotnet"
)

echo Usando: %DOTNET_CMD%
%DOTNET_CMD% build CigamPrintTest.sln -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ERRO na compilacao!
    exit /b %ERRORLEVEL%
)

echo.
echo Executando testes unitarios...
%DOTNET_CMD% test CigamPrintTest.sln -c Release --no-build
if %ERRORLEVEL% NEQ 0 (
    echo ERRO nos testes unitarios!
    exit /b %ERRORLEVEL%
)

echo.
echo ======================================================================
echo Compilacao e testes concluidos com exito!
echo Binarios gerados em: src\CigamPrintTest\bin\Release\net48\
echo ======================================================================
endlocal

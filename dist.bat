@echo off
echo Compiling driver...

cd driver
lcc -S freq.c
lcc -S music.c
lcc -S noisefreq.c
lcc -S vib.c
cd player
lcc -S player.c
cd ../..

echo.
echo Building parser...

rem Build the project (not solution) with output folder specified
dotnet publish parser\src\MMLGB.csproj -c Release -o dist\tmp\parser

echo.
echo Creating archive...

rem Create folders if they don't exist
if not exist dist\tmp\parser mkdir dist\tmp\parser
if not exist dist\tmp\driver mkdir dist\tmp\driver
if not exist dist\tmp\driver\player mkdir dist\tmp\driver\player
if not exist dist\tmp\driver\song mkdir dist\tmp\driver\song

rem Copy scripts
copy compile.bat dist\tmp\ || exit /b 1
copy compile.sh dist\tmp\ || exit /b 1

rem Copy driver asm files
copy driver\music.asm dist\tmp\driver\ || exit /b 1
copy driver\freq.asm dist\tmp\driver\ || exit /b 1
copy driver\noisefreq.asm dist\tmp\driver\ || exit /b 1
copy driver\vib.asm dist\tmp\driver\ || exit /b 1

rem Copy player asm files (all files from driver\player)
xcopy driver\player\* dist\tmp\driver\player\ /E /I /Y || exit /b 1

rem Generate timestamp
for /f %%a in ('powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd-HHmmss"') do set timestamp=%%a

powershell Compress-Archive -Path dist\tmp\* -DestinationPath "dist\mmlgb-%timestamp%.zip" -Force

rmdir dist\tmp /s /q

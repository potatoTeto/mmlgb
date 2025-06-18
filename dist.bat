@echo off
echo -e "Compiling driver..."

cd driver
lcc -S freq.c
lcc -S music.c
lcc -S noisefreq.c
lcc -S vib.c
cd player
lcc -S player.c
cd ../..

echo -e "\nCompiling parser..."
cd parser
ant
cd ..

echo -e "\nCreate archive"
mkdir dist
mkdir tmp
mkdir dist\tmp\parser
mkdir dist\tmp\driver

copy compile.bat dist\tmp || exit 1
copy compile.sh dist\tmp || exit 1
copy parser\MMLGB.jar dist\tmp\parser || exit 1
copy driver\music.asm dist\tmp\driver || exit 1
copy driver\freq.asm dist\tmp\driver || exit 1
copy driver\noisefreq.asm dist\tmp\driver || exit 1
copy driver\vib.asm dist\tmp\driver || exit 1

powershell Compress-Archive -Path dist\tmp\* -DestinationPath "dist/mmlgb-$(get-date -f yyyy-MM-dd-HHmmss).zip" -Force
rmdir dist\tmp /s /q

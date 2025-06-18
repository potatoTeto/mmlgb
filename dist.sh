#!/bin/sh
set -e

echo "Compiling driver..."

cd driver
lcc -S freq.c
lcc -S music.c
lcc -S noisefreq.c
lcc -S vib.c
cd player
lcc -S player.c
cd ../..

echo
echo "Building parser..."

# Publish the C# project (adjust path if needed)
dotnet publish parser/src/MMLGB.csproj -c Release -o dist/tmp/parser

echo
echo "Creating archive..."

# Create necessary directories
mkdir -p dist/tmp/parser
mkdir -p dist/tmp/driver
mkdir -p dist/tmp/driver/player
mkdir -p dist/tmp/driver/song

# Copy scripts
cp compile.bat dist/tmp/
cp compile.sh dist/tmp/

# Copy driver asm files
cp driver/music.asm dist/tmp/driver/
cp driver/freq.asm dist/tmp/driver/
cp driver/noisefreq.asm dist/tmp/driver/
cp driver/vib.asm dist/tmp/driver/

# Copy all files from driver/player
cp -r driver/player/* dist/tmp/driver/player/

# Generate timestamp for archive filename
timestamp=$(date '+%F-%H%M%S')

# Create zip archive
zip -r "dist/mmlgb-$timestamp.zip" dist/tmp/*

# Cleanup
rm -rf dist/tmp

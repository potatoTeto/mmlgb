#!/bin/sh
set -e

# Run parser with dotnet
dotnet parser/src/bin/Release/net8.0/MMLGB.dll "$1" driver/song/song.asm

# Initialize source file lists
SOURCES=""
ASM_SOURCES=""

# Collect all .c files in driver/
for f in driver/*.c; do
    [ -f "$f" ] && SOURCES="$SOURCES $f"
done

# Collect all .c files in driver/player/
for f in driver/player/*.c; do
    [ -f "$f" ] && SOURCES="$SOURCES $f"
done

# Collect all .asm files in driver/song/
for f in driver/song/*.asm; do
    [ -f "$f" ] && ASM_SOURCES="$ASM_SOURCES $f"
done

# Compile with lcc
lcc -Wl-j -Wm-yS -o rom.gb -Idriver $SOURCES $ASM_SOURCES

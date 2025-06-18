@echo off
setlocal enabledelayedexpansion

java -jar parser\MMLGB.jar music/%1 driver\song\song.asm

rem Initialize source file list
set SOURCES=
set ASM_SOURCES=

rem Collect all .c files in driver\
for %%f in (driver\*.c) do (
    set SOURCES=!SOURCES! %%f
)

rem Collect all .c files in driver\player\
for %%f in (driver\player\*.c) do (
    set SOURCES=!SOURCES! %%f
)

rem Collect all .asm files in driver\song\
for %%f in (driver\song\*.asm) do (
    set ASM_SOURCES=!ASM_SOURCES! %%f
)

rem Now compile
lcc -Wl-j -Wm-yS -o rom.gb -Idriver !SOURCES! !ASM_SOURCES!

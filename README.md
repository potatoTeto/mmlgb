mmlgb
=====

A MML parser and GBDK sound driver for the Nintendo Game Boy.

Please refer to the [MMLGB Wiki](https://github.com/potatoTeto/mmlgb/wiki) for additional information regarding usage.

## Dependencies
- [GBDK](https://github.com/gbdk-2020/gbdk-2020)
  - ``gbdk/bin`` should also be added to PATH/Environment Variables.
- [Java Development Kit](https://www.oracle.com/java/technologies/downloads/)

# Usage
- Create or place your .mml file directly in the ``music/`` directory.
  - Subfolders are not allowed (but feel free to migrate the examples in the ``music/examples/`` directory for studying purposes!)
- Execute ``compile[.bat/.sh] your_file.mml`` in Terminal/Command Prompt to compile ``rom.gb``. Feel free to amend a line within compile.bat to auto-launch ``rom.gb`` in your favorite emulator.
  - For accuracy reasons, I highly recommend the [bgb emulator](https://bgb.bircd.org/).

# Build Distribution
- Launch dist.bat/dist.sh, depending on your OS.
- The distributable file will be in the ``dist`` directory, as a timestamped .zip

### Special Thanks
- Simon Larsen, Original developer - [mmlgb v1.0](https://github.com/SimonLarsen/mmlgb)
- jimmy-dsi, Programming assistance - [GitHub](https://github.com/jimmy-dsi)

## License

mmlgb Source Code - MIT

All demo songs - All rights reserved

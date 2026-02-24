DOOM Windows SDL2 fixes (mouse 360°, sound, music, console)
============================================================

If you still get: visible cursor, no 360° turn, no sound/music, no console messages
after rebuilding, your DOOM tree may not have the updated sources. Use this folder
to overwrite the files and do a clean rebuild.

WHAT TO DO
---------
1. Normal run (recommended):  COPY_TO_DOOM_AND_REBUILD.bat
   - Copies the 3 fixed .c files into your DOOM source.
   - If build\CMakeCache.txt exists: only runs "cmake --build" (keeps your SDL2).
   - If not: runs full cmake configure + build (needs SDL2; see below).

2. If something is broken (build errors, "doesn't work at all"):
   - Close doom_star.exe, then run:  COPY_TO_DOOM_AND_REBUILD.bat clean
   - This deletes the build folder and reconfigures from scratch. You may need
     SDL2 in a standard path or set SDL2_DIR / CMAKE_TOOLCHAIN_FILE first.

2. If your DOOM source is elsewhere, edit the batch file and set DOOM_SRC to your
   path (e.g. D:\Games\DOOM\linuxdoom-1.10).

3. Run doom_star.exe from build\Release\ and put doom2.wad in that folder.

First-time or clean build (SDL2)
   The script only runs "cmake .." when build\CMakeCache.txt is missing. Then it
   looks for SDL2 in: CMAKE_TOOLCHAIN_FILE, SDL2_DIR, C:\Source\vcpkg, C:\vcpkg,
   C:\SDL2\cmake, C:\Source\SDL2-2.30.2\cmake. If yours is elsewhere, set
   SDL2_DIR or CMAKE_TOOLCHAIN_FILE before running the script.

WHAT THESE FILES FIX
--------------------
- i_main.c      : AllocConsole / AttachConsole so a console window appears and you see
                  "I_InitSound: SDL audio opened (...)" and other messages.
- i_video_sdl2.c: Every frame: when not in menu, SDL_SetWindowGrab + relative mouse
                  + SDL_ShowCursor(0) so cursor is hidden and you can turn 360°.
- i_sound_win.c : SDL sound + MCI music; doom_sound.log written next to exe for debugging.

After running, check doom_sound.log (same folder as doom_star.exe) if there is still no sound.

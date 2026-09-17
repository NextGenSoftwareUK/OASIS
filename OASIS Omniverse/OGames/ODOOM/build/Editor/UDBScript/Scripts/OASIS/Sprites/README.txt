OASIS Display Pack – sprites for the editor
===========================================

PNG files here are named by OASIS thing type. The editor uses them when drawing
things on the map (2D and 3D). If a PNG is missing, the game config sprite is used
(e.g. Doom's Blue Keycard for type 5).

Naming:
  ODOOM:  5.png, 13.png, 2001.png, 3003.png, etc. (Doom thing type)
  OQUAKE: 5005.png, 5013.png, 5201.png, 5303.png, etc. (unique type, no sharing)

Important: Do NOT put Quake sprites in Doom-type filenames (5.png, 13.png, 2001.png).
Those numbers are used for ODOOM assets; if you overwrite them with Quake art,
Doom keys/weapons/monsters will show the wrong sprite. When switching to unique
OQUAKE types, remove any old Quake PNGs that used Doom numbers, then run
ExtractOquakeSprites to generate 5005.png, 5013.png, 5303.png, etc.

To (re)import Quake 1 sprites, run ExtractOquakeSprites and point the output to
THIS folder (or to Build\UDBScript\Scripts\OASIS\Sprites if that is where UDB
loads from):
  ExtractOquakeSprites.exe "C:\path\to\id1" "C:\path\to\this\Sprites\folder"
See IMPORT_FROM_QUAKE.txt in this folder.







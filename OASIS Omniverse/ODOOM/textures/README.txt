OASIS Beamed-In Face (OASFACE)
==============================

When you beam in (star beamin), the status bar can show a custom face. The game looks for a texture named "OASFACE".

PRE-BUILT: A ready-to-use odoom_face.pk3 is in the ODOOM folder (created by create_odoom_face_pk3.py). Copy odoom_face.pk3 next to ODOOM.exe and beam in to see the face.

To rebuild the pk3: run "python create_odoom_face_pk3.py" from the ODOOM folder. It creates textures/OASFACE.png and odoom_face.pk3.

Other methods:

RECOMMENDED: PK3 method
-----------------------
1. Create a 32x32 pixel image (PNG recommended) for your "beamed in" face.
2. Name the file exactly: OASFACE.png
3. Put OASFACE.png inside this folder: textures/
   (So you have: textures/OASFACE.png)
4. Zip the contents so that "textures" is the root inside the zip:
   - Select the "textures" folder (with OASFACE.png inside it)
   - Right-click -> Send to -> Compressed (zipped) folder
   - Or: zip -r odoom_face.pk3 textures/
5. Rename the .zip to .pk3 (e.g. odoom_face.pk3)
6. Put the .pk3 in the same folder as ODOOM.exe, or run:
   ODOOM.exe -file odoom_face.pk3

Quick test (reuse Doom guy face):
---------------------------------
- Open doom2.wad (or doom.wad) in SLADE.
- Find the lump "STFSTF00" (status bar face frame).
- Right-click -> Export -> save as OASFACE.png.
- Put OASFACE.png in textures/ and build the pk3 as above.

WAD method (using SLADE)
------------------------
1. Open SLADE (https://slade.mancubus.net/).
2. Open your WAD or create a new one.
3. Menu: Archive -> New -> Entry. Name the lump: OASFACE
4. Or: Graphics -> Import from PNG, choose your image, then rename lump to OASFACE.
5. Save the WAD. Run ODOOM with: ODOOM.exe -file your.wad

Image size: 32x32 pixels matches the classic status bar face; other sizes will be scaled.



#!/usr/bin/env python3
"""
Creates odoom_face.pk3 from textures/OASFACE.png.
"""
import os
import zipfile

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
TEXTURES_DIR = os.path.join(SCRIPT_DIR, "textures")
PK3_PATH = os.path.join(SCRIPT_DIR, "odoom_face.pk3")


def main():
    png_path = os.path.join(TEXTURES_DIR, "OASFACE.png")
    if not os.path.exists(png_path):
        raise FileNotFoundError(f"Missing face texture: {png_path}")
    print(f"Using {png_path}")

    with zipfile.ZipFile(PK3_PATH, "w", zipfile.ZIP_DEFLATED) as zf:
        zf.write(png_path, "textures/OASFACE.png")
    print(f"Created {PK3_PATH}")
    print("Put odoom_face.pk3 next to ODOOM.exe (or run with -file odoom_face.pk3).")


if __name__ == "__main__":
    main()





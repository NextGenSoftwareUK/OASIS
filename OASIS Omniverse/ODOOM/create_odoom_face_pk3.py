#!/usr/bin/env python3
"""
Creates textures/OASFACE.png (34x30 OASIS beamed-in face) and odoom_face.pk3.
Uses only the standard library (no Pillow required).
"""
import zlib
import struct
import os
import zipfile

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
TEXTURES_DIR = os.path.join(SCRIPT_DIR, "textures")
PK3_PATH = os.path.join(SCRIPT_DIR, "odoom_face.pk3")


def png_chunk(chunk_type: bytes, data: bytes) -> bytes:
    chunk = chunk_type + data
    return struct.pack(">I", len(data)) + chunk + struct.pack(">I", 0xFFFFFFFF & zlib.crc32(chunk))


def make_png_34x30():
    """Build a 34x30 PNG (OASIS-style face: tan/amber background, simple eyes and mouth)."""
    width, height = 34, 30
    # Raw image: filter byte 0 per row, then RGB (3 bytes per pixel)
    raw = bytearray()
    for y in range(height):
        raw.append(0)  # filter type: None
        for x in range(width):
            # Simple "face" shape: oval-ish face area in tan, dark eyes and mouth
            cx, cy = width / 2 - 0.5, height / 2 - 0.5
            dx, dy = (x - cx) / (width * 0.4), (y - cy) / (height * 0.45)
            in_face = dx * dx + dy * dy <= 1.0
            # Eyes (two small ellipses)
            e1 = (x - 10) ** 2 + (y - 12) ** 2 < 16
            e2 = (x - 22) ** 2 + (y - 12) ** 2 < 16
            # Mouth (horizontal line)
            mouth = 10 <= x <= 22 and 22 <= y <= 24
            if e1 or e2:
                r, g, b = 40, 30, 20
            elif mouth:
                r, g, b = 50, 35, 25
            elif in_face:
                r, g, b = 210, 180, 140  # tan
            else:
                r, g, b = 0, 0, 0  # transparent would need RGBA; use black border
            raw.extend((r, g, b))
    compressed = zlib.compress(bytes(raw), 9)
    signature = b"\x89PNG\r\n\x1a\n"
    ihdr = struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0)
    chunks = (
        signature
        + png_chunk(b"IHDR", ihdr)
        + png_chunk(b"IDAT", compressed)
        + png_chunk(b"IEND", b"")
    )
    return chunks


def main():
    os.makedirs(TEXTURES_DIR, exist_ok=True)
    png_path = os.path.join(TEXTURES_DIR, "OASFACE.png")
    png_data = make_png_34x30()
    with open(png_path, "wb") as f:
        f.write(png_data)
    print(f"Created {png_path}")

    with zipfile.ZipFile(PK3_PATH, "w", zipfile.ZIP_DEFLATED) as zf:
        zf.write(png_path, "textures/OASFACE.png")
    print(f"Created {PK3_PATH}")
    print("Put odoom_face.pk3 next to ODOOM.exe (or run with -file odoom_face.pk3).")


if __name__ == "__main__":
    main()





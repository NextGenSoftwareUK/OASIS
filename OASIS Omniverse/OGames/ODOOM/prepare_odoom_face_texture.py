#!/usr/bin/env python3
"""
Prepare OASFACE.png from face_anorak.png (resize to 34x30, offset 1,1).
Linux/macOS fallback when prepare_odoom_face_texture.ps1 fails (System.Drawing not available in pwsh).
Usage: python3 prepare_odoom_face_texture.py --source face_anorak.png --dest textures/OASFACE.png [--width 34] [--height 30] [--offset-x 1] [--offset-y 1]
"""
import argparse
import os
import sys

def main():
    ap = argparse.ArgumentParser(description="Prepare OASFACE texture from face_anorak.png")
    ap.add_argument("--source", "-s", required=True, help="Source image path (e.g. face_anorak.png)")
    ap.add_argument("--dest", "-d", required=True, help="Output PNG path (e.g. textures/OASFACE.png)")
    ap.add_argument("--width", "-w", type=int, default=34, help="Target width (default 34)")
    ap.add_argument("--height", "-H", type=int, default=30, help="Target height (default 30)")
    ap.add_argument("--offset-x", type=int, default=1, help="Draw X offset (default 1)")
    ap.add_argument("--offset-y", type=int, default=1, help="Draw Y offset (default 1)")
    args = ap.parse_args()

    if args.width < 1 or args.height < 1:
        print("Invalid target size", file=sys.stderr)
        return 1
    if not os.path.isfile(args.source):
        print(f"Face source not found: {args.source}", file=sys.stderr)
        return 1

    try:
        from PIL import Image
    except ImportError:
        print("PIL/Pillow not found. Install with: pip install Pillow (or pip3 install Pillow)", file=sys.stderr)
        return 1

    dest_dir = os.path.dirname(args.dest)
    if dest_dir and not os.path.isdir(dest_dir):
        os.makedirs(dest_dir, exist_ok=True)

    src = Image.open(args.source).convert("RGBA")
    dst = Image.new("RGBA", (args.width, args.height), (0, 0, 0, 0))
    try:
        resample = Image.Resampling.LANCZOS
    except AttributeError:
        resample = Image.LANCZOS
    resized = src.resize((args.width, args.height), resample)
    dst.paste(resized, (args.offset_x, args.offset_y), resized)
    dst.save(args.dest, "PNG")
    print(f"[face] Prepared OASFACE from '{args.source}' -> '{args.dest}' size {args.width}x{args.height} (drawn at x={args.offset_x}, y={args.offset_y})")
    return 0

if __name__ == "__main__":
    sys.exit(main())

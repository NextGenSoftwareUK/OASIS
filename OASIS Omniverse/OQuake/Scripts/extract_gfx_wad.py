#!/usr/bin/env python3
"""
Extract gfx.wad from Quake pak0.pak into the id1 directory.
Used when Steam/Quake install has id1/pak0.pak but no standalone gfx.wad;
vkQuake requires gfx.wad to be loadable (basedir or id1).

Quake PAK format: 12-byte header (PACK, diroffset, dirsize), then directory
entries of 64 bytes each (56-byte filename, 4-byte offset, 4-byte size).
"""
import os
import struct
import sys


def extract_gfx_wad_from_pak(pak_path: str, out_dir: str) -> bool:
    """Extract gfx.wad from pak_path into out_dir. Returns True if extracted."""
    if not os.path.isfile(pak_path):
        return False
    with open(pak_path, "rb") as f:
        sig = f.read(4)
        if sig != b"PACK":
            sys.stderr.write("Not a valid Quake PACK file (bad signature).\n")
            return False
        diroffset = struct.unpack("<I", f.read(4))[0]
        dirsize = struct.unpack("<I", f.read(4))[0]
        num_entries = dirsize // 64
        f.seek(diroffset)
        first_names = []
        for i in range(num_entries):
            name_buf = f.read(56)
            offset = struct.unpack("<I", f.read(4))[0]
            size = struct.unpack("<I", f.read(4))[0]
            raw = name_buf.split(b"\x00")[0]
            name = raw.decode("ascii", errors="replace").replace("\\", "/").strip()
            if i < 15:
                first_names.append(name or "(empty)")
            if not name:
                continue
            if name.lower().endswith("gfx.wad") or name.lower().strip() == "gfx.wad":
                os.makedirs(out_dir, exist_ok=True)
                out_path = os.path.join(out_dir, "gfx.wad")
                with open(out_path, "wb") as out:
                    f.seek(offset)
                    out.write(f.read(size))
                return True
        sys.stderr.write("gfx.wad not found in pak. First entries: %s\n" % ", ".join(first_names[:10]))
    return False


def find_pak0(id1_dir: str) -> str:
    """Return path to pak0.pak (any case: pak0.pak, PAK0.PAK, etc.) or empty string."""
    if not os.path.isdir(id1_dir):
        return ""
    for name in os.listdir(id1_dir):
        if name.lower() == "pak0.pak":
            return os.path.join(id1_dir, name)
    return ""


def main():
    if len(sys.argv) >= 2:
        basedir = sys.argv[1]
    else:
        basedir = os.environ.get(
            "OQUAKE_BASEDIR",
            os.path.expanduser("~/snap/steam/common/.local/share/Steam/steamapps/common/Quake"),
        )
    id1 = os.path.join(basedir, "id1")
    pak_path = find_pak0(id1)
    out_dir = id1

    if not pak_path or not os.path.isfile(pak_path):
        sys.stderr.write("pak0.pak not found in {} (looked for any case, e.g. pak0.pak or PAK0.PAK).\n".format(id1))
        if not os.path.isdir(basedir):
            sys.stderr.write("Basedir does not exist: {}\n".format(basedir))
        elif not os.path.isdir(id1):
            sys.stderr.write("id1 folder does not exist: {}\n".format(id1))
        else:
            try:
                contents = os.listdir(id1)
                sys.stderr.write("id1 contents ({} items): {}\n".format(
                    len(contents), ", ".join(sorted(contents)[:25]) + (" ..." if len(contents) > 25 else "")))
            except OSError as e:
                sys.stderr.write("Could not list id1: {}\n".format(e))
        sys.stderr.write("Set OQUAKE_BASEDIR to your Quake install (e.g. where id1/PAK0.PAK lives).\n")
        return 1

    if extract_gfx_wad_from_pak(pak_path, out_dir):
        print("Extracted gfx.wad from {} to {}".format(pak_path, os.path.join(out_dir, "gfx.wad")))
        return 0
    return 1


if __name__ == "__main__":
    sys.exit(main())

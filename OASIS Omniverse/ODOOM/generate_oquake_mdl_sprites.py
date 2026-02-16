#!/usr/bin/env python3
"""
Generate Doom-style multi-angle sprite PNGs from a Quake 1 MDL model.

Features:
- Reads MDL directly or from a PAK archive.
- Uses Quake palette (palette.lmp) for textured rendering.
- Renders every model frame from N angles (default 8).
- Writes PNG files with grAb anchor offsets for proper Doom/GZDoom placement.
- Emits a JSON manifest mapping MDL frame names to generated sprite names.
"""

from __future__ import annotations

import argparse
import dataclasses
import json
import math
import os
import pathlib
import re
import struct
import sys
import zlib
from typing import Dict, Generic, Iterable, List, Optional, Tuple, TypeVar


T = TypeVar("T")


@dataclasses.dataclass
class OASISResult(Generic[T]):
    ok: bool
    data: Optional[T] = None
    error: str = ""


def oasis_ok(value: T) -> OASISResult[T]:
    return OASISResult(ok=True, data=value)


def oasis_error(message: str) -> OASISResult[T]:
    return OASISResult(ok=False, data=None, error=message)


@dataclasses.dataclass
class MdlStVert:
    onseam: int
    s: int
    t: int


@dataclasses.dataclass
class MdlTriangle:
    facesfront: int
    vert_indices: Tuple[int, int, int]


@dataclasses.dataclass
class MdlFrame:
    name: str
    verts: List[Tuple[float, float, float]]


@dataclasses.dataclass
class MdlModel:
    skin_width: int
    skin_height: int
    skin_indices: bytes
    stverts: List[MdlStVert]
    triangles: List[MdlTriangle]
    frames: List[MdlFrame]


def read_file_bytes(path: str) -> OASISResult[bytes]:
    try:
        return oasis_ok(pathlib.Path(path).read_bytes())
    except Exception as ex:
        return oasis_error(f"Failed to read '{path}': {ex}")


def parse_pak_entries(pak_bytes: bytes) -> OASISResult[Dict[str, Tuple[int, int]]]:
    if len(pak_bytes) < 12:
        return oasis_error("PAK too small.")
    magic, dirofs, dirlen = struct.unpack_from("<4sii", pak_bytes, 0)
    if magic != b"PACK":
        return oasis_error("Invalid PAK magic.")
    if dirofs < 0 or dirlen < 0 or dirofs + dirlen > len(pak_bytes):
        return oasis_error("Invalid PAK directory range.")
    if dirlen % 64 != 0:
        return oasis_error("PAK directory length is not entry-aligned.")
    out: Dict[str, Tuple[int, int]] = {}
    for i in range(0, dirlen, 64):
        pos = dirofs + i
        name_raw, filepos, filelen = struct.unpack_from("<56sii", pak_bytes, pos)
        name = name_raw.split(b"\x00", 1)[0].decode("ascii", errors="ignore").lower()
        if not name:
            continue
        if filepos < 0 or filelen < 0 or filepos + filelen > len(pak_bytes):
            return oasis_error(f"PAK entry out of range: {name}")
        out[name] = (filepos, filelen)
    return oasis_ok(out)


def read_from_pak(pak_path: str, inner_path: str) -> OASISResult[bytes]:
    pak_res = read_file_bytes(pak_path)
    if not pak_res.ok:
        return pak_res
    pak_bytes = pak_res.data or b""
    idx_res = parse_pak_entries(pak_bytes)
    if not idx_res.ok:
        return oasis_error(f"Failed to parse PAK '{pak_path}': {idx_res.error}")
    entries = idx_res.data or {}
    key = inner_path.replace("\\", "/").lower()
    if key not in entries:
        return oasis_error(f"'{inner_path}' not found in PAK '{pak_path}'.")
    off, ln = entries[key]
    return oasis_ok(pak_bytes[off:off + ln])


def load_quake_palette(palette_bytes: bytes) -> OASISResult[List[Tuple[int, int, int, int]]]:
    if len(palette_bytes) < 768:
        return oasis_error("Palette must contain at least 768 bytes (256 RGB entries).")
    out: List[Tuple[int, int, int, int]] = []
    for i in range(256):
        r = palette_bytes[i * 3 + 0]
        g = palette_bytes[i * 3 + 1]
        b = palette_bytes[i * 3 + 2]
        out.append((r, g, b, 255))
    return oasis_ok(out)


def parse_mdl(mdl_bytes: bytes) -> OASISResult[MdlModel]:
    if len(mdl_bytes) < 84:
        return oasis_error("MDL too small.")
    (
        ident,
        version,
        sx,
        sy,
        sz,
        tx,
        ty,
        tz,
        _bounding_radius,
        _eyex,
        _eyey,
        _eyez,
        num_skins,
        skin_w,
        skin_h,
        num_verts,
        num_tris,
        num_frames,
        _synctype,
        _flags,
        _size,
    ) = struct.unpack_from("<ii3f3ff3f8if", mdl_bytes, 0)

    if ident != 1330660425 or version != 6:  # "IDPO"
        return oasis_error("Unsupported MDL signature/version (expected Quake v6).")
    if num_skins <= 0 or num_verts <= 0 or num_tris <= 0 or num_frames <= 0:
        return oasis_error("Invalid MDL counts.")

    p = 84
    skin_size = skin_w * skin_h
    if skin_size <= 0:
        return oasis_error("Invalid MDL skin size.")

    # First skin only. Group skins are supported by reading first frame.
    skin_group = struct.unpack_from("<i", mdl_bytes, p)[0]
    p += 4
    if skin_group == 0:
        if p + skin_size > len(mdl_bytes):
            return oasis_error("MDL truncated while reading single skin.")
        skin_indices = mdl_bytes[p:p + skin_size]
        p += skin_size
    else:
        if p + 4 > len(mdl_bytes):
            return oasis_error("MDL truncated while reading skin group count.")
        num_group_skins = struct.unpack_from("<i", mdl_bytes, p)[0]
        p += 4
        if num_group_skins <= 0:
            return oasis_error("Invalid MDL skin group count.")
        p += num_group_skins * 4  # group intervals
        if p + skin_size > len(mdl_bytes):
            return oasis_error("MDL truncated while reading grouped skin.")
        skin_indices = mdl_bytes[p:p + skin_size]
        p += skin_size
        # Skip any extra group skins.
        p += (num_group_skins - 1) * skin_size

    stverts: List[MdlStVert] = []
    for _ in range(num_verts):
        if p + 12 > len(mdl_bytes):
            return oasis_error("MDL truncated while reading ST verts.")
        onseam, s, t = struct.unpack_from("<iii", mdl_bytes, p)
        p += 12
        stverts.append(MdlStVert(onseam=onseam, s=s, t=t))

    triangles: List[MdlTriangle] = []
    for _ in range(num_tris):
        if p + 16 > len(mdl_bytes):
            return oasis_error("MDL truncated while reading triangles.")
        facesfront, v0, v1, v2 = struct.unpack_from("<iiii", mdl_bytes, p)
        p += 16
        triangles.append(MdlTriangle(facesfront=facesfront, vert_indices=(v0, v1, v2)))

    frames: List[MdlFrame] = []
    scale = (sx, sy, sz)
    trans = (tx, ty, tz)

    def parse_single_frame(pos: int) -> OASISResult[Tuple[MdlFrame, int]]:
        if pos + 8 + 16 > len(mdl_bytes):
            return oasis_error("MDL truncated while reading frame header.")
        pos += 4  # bboxmin trivertx
        pos += 4  # bboxmax trivertx
        raw_name = mdl_bytes[pos:pos + 16]
        pos += 16
        frame_name = raw_name.split(b"\x00", 1)[0].decode("ascii", errors="ignore") or "frame"
        verts: List[Tuple[float, float, float]] = []
        need = num_verts * 4
        if pos + need > len(mdl_bytes):
            return oasis_error("MDL truncated while reading frame vertices.")
        for vi in range(num_verts):
            x, y, z, _n = struct.unpack_from("<BBBB", mdl_bytes, pos + vi * 4)
            vx = x * scale[0] + trans[0]
            vy = y * scale[1] + trans[1]
            vz = z * scale[2] + trans[2]
            verts.append((vx, vy, vz))
        pos += need
        return oasis_ok((MdlFrame(name=frame_name, verts=verts), pos))

    for _ in range(num_frames):
        if p + 4 > len(mdl_bytes):
            return oasis_error("MDL truncated while reading frame type.")
        frame_type = struct.unpack_from("<i", mdl_bytes, p)[0]
        p += 4
        if frame_type == 0:
            sf = parse_single_frame(p)
            if not sf.ok:
                return oasis_error(sf.error)
            frame, p = sf.data  # type: ignore[misc]
            frames.append(frame)
        else:
            if p + 4 > len(mdl_bytes):
                return oasis_error("MDL truncated while reading frame group count.")
            group_count = struct.unpack_from("<i", mdl_bytes, p)[0]
            p += 4
            if group_count <= 0:
                return oasis_error("Invalid MDL frame group count.")
            p += 8  # bbox min/max trivertx
            p += group_count * 4  # intervals
            # Keep all group poses; they are useful as sprite frames.
            for _gi in range(group_count):
                sf = parse_single_frame(p)
                if not sf.ok:
                    return oasis_error(sf.error)
                frame, p = sf.data  # type: ignore[misc]
                frames.append(frame)

    return oasis_ok(
        MdlModel(
            skin_width=skin_w,
            skin_height=skin_h,
            skin_indices=skin_indices,
            stverts=stverts,
            triangles=triangles,
            frames=frames,
        )
    )


def rotation_letters() -> str:
    # Use filesystem-safe frame letters only; split additional frames
    # across extra 4-char sprite sets.
    return "ABCDEFGHIJKLMNOPQRSTUVWXYZ"


def set_char_for_index(i: int) -> str:
    chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    if i < len(chars):
        return chars[i]
    return chars[-1]


def split_sprite_series(base_prefix: str, frame_index: int) -> Tuple[str, str]:
    letters = rotation_letters()
    set_index = frame_index // len(letters)
    letter = letters[frame_index % len(letters)]
    prefix = base_prefix if set_index == 0 else (base_prefix[:3] + set_char_for_index(set_index))
    return prefix, letter


def barycentric(
    px: float,
    py: float,
    x0: float,
    y0: float,
    x1: float,
    y1: float,
    x2: float,
    y2: float,
) -> Tuple[float, float, float]:
    den = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
    if abs(den) < 1e-12:
        return (-1.0, -1.0, -1.0)
    w0 = ((y1 - y2) * (px - x2) + (x2 - x1) * (py - y2)) / den
    w1 = ((y2 - y0) * (px - x2) + (x0 - x2) * (py - y2)) / den
    w2 = 1.0 - w0 - w1
    return (w0, w1, w2)


def png_chunk(chunk_type: bytes, data: bytes) -> bytes:
    crc = zlib.crc32(chunk_type)
    crc = zlib.crc32(data, crc) & 0xFFFFFFFF
    return struct.pack(">I", len(data)) + chunk_type + data + struct.pack(">I", crc)


def write_png_rgba_with_grab(path: str, w: int, h: int, rgba: bytes, grab_x: int, grab_y: int) -> OASISResult[None]:
    if len(rgba) != w * h * 4:
        return oasis_error("RGBA byte count does not match width/height.")
    try:
        raw = bytearray()
        stride = w * 4
        for y in range(h):
            raw.append(0)  # filter type 0
            raw.extend(rgba[y * stride:(y + 1) * stride])
        compressed = zlib.compress(bytes(raw), level=9)
        ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
        grab = struct.pack(">ii", grab_x, grab_y)
        out = bytearray()
        out.extend(b"\x89PNG\r\n\x1a\n")
        out.extend(png_chunk(b"IHDR", ihdr))
        out.extend(png_chunk(b"grAb", grab))
        out.extend(png_chunk(b"IDAT", compressed))
        out.extend(png_chunk(b"IEND", b""))
        pathlib.Path(path).write_bytes(bytes(out))
        return oasis_ok(None)
    except Exception as ex:
        return oasis_error(f"Failed to write PNG '{path}': {ex}")


def compute_projection(
    model: MdlModel,
    yaw_angles: List[float],
    out_w: int,
    out_h: int,
    padding: int,
) -> OASISResult[Tuple[float, float, float, float, float]]:
    # Center model XY around origin for stable rotations.
    all_x = [v[0] for fr in model.frames for v in fr.verts]
    all_y = [v[1] for fr in model.frames for v in fr.verts]
    if not all_x or not all_y:
        return oasis_error("Model has no vertices.")
    cx = 0.5 * (min(all_x) + max(all_x))
    cy = 0.5 * (min(all_y) + max(all_y))

    min_rx = float("inf")
    max_rx = float("-inf")
    min_z = float("inf")
    max_z = float("-inf")
    for fr in model.frames:
        for yaw in yaw_angles:
            c = math.cos(yaw)
            s = math.sin(yaw)
            for vx, vy, vz in fr.verts:
                x = vx - cx
                y = vy - cy
                rx = x * c - y * s
                min_rx = min(min_rx, rx)
                max_rx = max(max_rx, rx)
                min_z = min(min_z, vz)
                max_z = max(max_z, vz)

    span_x = max(1e-6, max_rx - min_rx)
    span_z = max(1e-6, max_z - min_z)
    usable_w = max(1.0, float(out_w - 2 * padding))
    usable_h = max(1.0, float(out_h - 2 * padding))
    scale = min(usable_w / span_x, usable_h / span_z)
    if scale <= 0:
        return oasis_error("Invalid projection scale.")
    return oasis_ok((cx, cy, min_rx, max_z, scale))


def render_frame_angle(
    model: MdlModel,
    palette: List[Tuple[int, int, int, int]],
    frame: MdlFrame,
    yaw: float,
    out_w: int,
    out_h: int,
    padding: int,
    model_cx: float,
    model_cy: float,
    min_rx: float,
    max_z: float,
    scale: float,
) -> OASISResult[Tuple[bytes, int, int]]:
    tex_w = model.skin_width
    tex_h = model.skin_height
    skin = model.skin_indices

    c = math.cos(yaw)
    s = math.sin(yaw)
    transformed: List[Tuple[float, float, float]] = []
    for vx, vy, vz in frame.verts:
        x = vx - model_cx
        y = vy - model_cy
        rx = x * c - y * s
        ry = x * s + y * c
        transformed.append((rx, ry, vz))

    rgba = bytearray(out_w * out_h * 4)
    zbuf = [-1.0e30] * (out_w * out_h)

    def set_pixel(ix: int, iy: int, depth: float, color: Tuple[int, int, int, int]) -> None:
        if ix < 0 or iy < 0 or ix >= out_w or iy >= out_h:
            return
        idx = iy * out_w + ix
        if depth <= zbuf[idx]:
            return
        zbuf[idx] = depth
        o = idx * 4
        rgba[o + 0] = color[0]
        rgba[o + 1] = color[1]
        rgba[o + 2] = color[2]
        rgba[o + 3] = color[3]

    for tri in model.triangles:
        i0, i1, i2 = tri.vert_indices
        if i0 >= len(transformed) or i1 >= len(transformed) or i2 >= len(transformed):
            return oasis_error("Triangle vertex index out of range.")
        p0 = transformed[i0]
        p1 = transformed[i1]
        p2 = transformed[i2]

        x0 = padding + (p0[0] - min_rx) * scale
        y0 = padding + (max_z - p0[2]) * scale
        x1 = padding + (p1[0] - min_rx) * scale
        y1 = padding + (max_z - p1[2]) * scale
        x2 = padding + (p2[0] - min_rx) * scale
        y2 = padding + (max_z - p2[2]) * scale

        area2 = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0)
        if abs(area2) < 1e-8:
            continue

        st0 = model.stverts[i0]
        st1 = model.stverts[i1]
        st2 = model.stverts[i2]

        def tri_uv(st: MdlStVert) -> Tuple[float, float]:
            s_tex = st.s
            if tri.facesfront == 0 and st.onseam:
                s_tex += tex_w // 2
            u = s_tex / max(1.0, float(tex_w - 1))
            v = st.t / max(1.0, float(tex_h - 1))
            return (u, v)

        u0, v0 = tri_uv(st0)
        u1, v1 = tri_uv(st1)
        u2, v2 = tri_uv(st2)

        min_x = max(0, int(math.floor(min(x0, x1, x2))))
        max_x = min(out_w - 1, int(math.ceil(max(x0, x1, x2))))
        min_y = max(0, int(math.floor(min(y0, y1, y2))))
        max_y = min(out_h - 1, int(math.ceil(max(y0, y1, y2))))
        if min_x > max_x or min_y > max_y:
            continue

        for py in range(min_y, max_y + 1):
            fy = py + 0.5
            for px in range(min_x, max_x + 1):
                fx = px + 0.5
                w0, w1, w2 = barycentric(fx, fy, x0, y0, x1, y1, x2, y2)
                if w0 < 0.0 or w1 < 0.0 or w2 < 0.0:
                    continue
                depth = w0 * p0[1] + w1 * p1[1] + w2 * p2[1]
                uu = w0 * u0 + w1 * u1 + w2 * u2
                vv = w0 * v0 + w1 * v1 + w2 * v2
                tx = int(round(uu * (tex_w - 1)))
                ty = int(round(vv * (tex_h - 1)))
                tx = max(0, min(tex_w - 1, tx))
                ty = max(0, min(tex_h - 1, ty))
                color_index = skin[ty * tex_w + tx]
                set_pixel(px, py, depth, palette[color_index])

    # Compute grAb from visible pixels.
    min_x = out_w
    max_x = -1
    max_y = -1
    for y in range(out_h):
        row = y * out_w * 4
        for x in range(out_w):
            a = rgba[row + x * 4 + 3]
            if a != 0:
                min_x = min(min_x, x)
                max_x = max(max_x, x)
                max_y = max(max_y, y)
    if max_x < min_x or max_y < 0:
        grab_x = out_w // 2
        grab_y = out_h
    else:
        grab_x = int(round((min_x + max_x + 1) / 2.0))
        grab_y = max_y + 1

    return oasis_ok((bytes(rgba), grab_x, grab_y))


def generate_sprites(
    model: MdlModel,
    palette: List[Tuple[int, int, int, int]],
    out_dir: str,
    sprite_prefix: str,
    out_w: int,
    out_h: int,
    angle_count: int,
    padding: int,
    yaw_offset_deg: float,
    lock_grab_y: bool,
    doom_mirror_rotations: bool,
    write_manifest: bool,
) -> OASISResult[Dict[str, object]]:
    if len(sprite_prefix) != 4:
        return oasis_error("sprite_prefix must be exactly 4 characters.")
    if angle_count < 1:
        return oasis_error("angle_count must be >= 1.")

    pathlib.Path(out_dir).mkdir(parents=True, exist_ok=True)
    yaw_base = math.radians(yaw_offset_deg)
    yaw_angles = [yaw_base + (2.0 * math.pi * i / angle_count) for i in range(angle_count)]
    proj_res = compute_projection(model, yaw_angles, out_w, out_h, padding)
    if not proj_res.ok:
        return oasis_error(proj_res.error)
    model_cx, model_cy, min_rx, max_z, scale = proj_res.data  # type: ignore[misc]

    manifest_frames: List[Dict[str, object]] = []
    frame_groups: Dict[str, List[int]] = {}
    generated = 0
    baseline_grab_y: Optional[int] = None

    for frame_index, frame in enumerate(model.frames):
        set_prefix, frame_letter = split_sprite_series(sprite_prefix, frame_index)
        base_name = re.sub(r"\d+$", "", frame.name.lower())
        if not base_name:
            base_name = frame.name.lower() or f"frame{frame_index}"
        frame_groups.setdefault(base_name, []).append(frame_index)

        frame_entry: Dict[str, object] = {
            "mdl_frame_index": frame_index,
            "mdl_frame_name": frame.name,
            "mdl_frame_base": base_name,
            "sprite_set": set_prefix,
            "sprite_frame_letter": frame_letter,
            "angles": [],
        }
        for ai, yaw in enumerate(yaw_angles):
            angle_num = ai + 1
            rr = render_frame_angle(
                model=model,
                palette=palette,
                frame=frame,
                yaw=yaw,
                out_w=out_w,
                out_h=out_h,
                padding=padding,
                model_cx=model_cx,
                model_cy=model_cy,
                min_rx=min_rx,
                max_z=max_z,
                scale=scale,
            )
            if not rr.ok:
                return oasis_error(f"Render failed for frame {frame_index}, angle {angle_num}: {rr.error}")
            rgba, grab_x, grab_y = rr.data  # type: ignore[misc]
            if lock_grab_y:
                if baseline_grab_y is None:
                    baseline_grab_y = grab_y
                grab_y = baseline_grab_y
            angle_token = str(angle_num)
            if doom_mirror_rotations and angle_count == 8:
                if angle_num == 1:
                    angle_token = "1"
                elif angle_num == 2:
                    angle_token = f"2{frame_letter}8"
                elif angle_num == 3:
                    angle_token = f"3{frame_letter}7"
                elif angle_num == 4:
                    angle_token = f"4{frame_letter}6"
                elif angle_num == 5:
                    angle_token = "5"
                elif angle_num in (6, 7, 8):
                    # mirrored by 4/3/2 respectively; do not emit duplicate files
                    continue
            filename = f"{set_prefix}{frame_letter}{angle_token}.png"
            out_path = os.path.join(out_dir, filename)
            wr = write_png_rgba_with_grab(out_path, out_w, out_h, rgba, grab_x, grab_y)
            if not wr.ok:
                return oasis_error(wr.error)
            generated += 1
            frame_entry["angles"].append(
                {
                    "angle": angle_num,
                    "angle_token": angle_token,
                    "file": filename,
                    "grab_x": grab_x,
                    "grab_y": grab_y,
                }
            )
        manifest_frames.append(frame_entry)

    state_hints: Dict[str, List[str]] = {
        "spawn": [],
        "see": [],
        "missile_or_attack": [],
        "pain": [],
        "death": [],
        "xdeath": [],
        "other": [],
    }
    for group in frame_groups.keys():
        if any(k in group for k in ("stand", "idle", "wait")):
            state_hints["spawn"].append(group)
        elif any(k in group for k in ("run", "walk")):
            state_hints["see"].append(group)
        elif any(k in group for k in ("attack", "shoot", "fire", "melee")):
            state_hints["missile_or_attack"].append(group)
        elif "pain" in group:
            state_hints["pain"].append(group)
        elif any(k in group for k in ("death", "die")):
            state_hints["death"].append(group)
        elif any(k in group for k in ("gib", "xdeath")):
            state_hints["xdeath"].append(group)
        else:
            state_hints["other"].append(group)

    manifest: Dict[str, object] = {
        "sprite_prefix_base": sprite_prefix,
        "frame_count": len(model.frames),
        "angle_count": angle_count,
        "output_size": [out_w, out_h],
        "generated_png_count": generated,
        "yaw_offset_deg": yaw_offset_deg,
        "lock_grab_y": lock_grab_y,
        "doom_mirror_rotations": doom_mirror_rotations,
        "frame_groups": frame_groups,
        "state_hints": state_hints,
        "frames": manifest_frames,
    }
    manifest_path = ""
    if write_manifest:
        manifest_path = os.path.join(out_dir, f"{sprite_prefix}_manifest.json")
        pathlib.Path(manifest_path).write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    manifest["manifest_path"] = manifest_path
    return oasis_ok(manifest)


def sample_sequence(indices: List[int], count: int) -> List[int]:
    if count <= 0 or not indices:
        return []
    if len(indices) >= count:
        return indices[:count]
    out = indices[:]
    while len(out) < count:
        out.append(indices[-1])
    return out


def classify_frame_groups(model: MdlModel) -> Dict[str, List[int]]:
    groups: Dict[str, List[int]] = {}
    for idx, frame in enumerate(model.frames):
        base = re.sub(r"\d+$", "", frame.name.lower())
        if not base:
            base = frame.name.lower() or f"frame{idx}"
        groups.setdefault(base, []).append(idx)
    return groups


def first_non_empty(groups: Dict[str, List[int]], keys: Iterable[str]) -> List[int]:
    for k in keys:
        if k in groups and groups[k]:
            return groups[k]
    return []


def choose_frames_for_profile(model: MdlModel, profile: str) -> OASISResult[Dict[str, List[int]]]:
    groups = classify_frame_groups(model)
    if profile == "full":
        return oasis_ok({"all": list(range(len(model.frames)))})

    if profile == "zombieman":
        stand = first_non_empty(groups, ("stand", "idle", "wait"))
        run = first_non_empty(groups, ("run", "walk"))
        attack = first_non_empty(groups, ("attack", "atta", "attb", "attc", "shoot", "fire", "melee", "swing", "smash", "magic", "magatt", "runattack", "leap"))
        pain = first_non_empty(groups, ("pain", "paina", "painb"))
        death = first_non_empty(groups, ("death", "die", "deatha", "deathb", "deathc", "bdeath"))
        xdeath = first_non_empty(groups, ("gib", "xdeath", "deathb", "deathc", "bdeath"))

        # Fallbacks to keep output valid even with odd naming.
        if not stand:
            stand = list(range(min(1, len(model.frames))))
        if not run:
            run = stand[:]
        if not attack:
            attack = run[:]
        if not pain:
            pain = run[:]
        if not death:
            death = pain if pain else run[:]
        if not xdeath:
            xdeath = death[:]

        selection = {
            "spawn": sample_sequence(stand, 1),
            "see": sample_sequence(run, 4),
            "missile_or_attack": sample_sequence(attack, 3),
            "pain": sample_sequence(pain, 1),
            "death": sample_sequence(death, 5),
            "xdeath": sample_sequence(xdeath, 9),
        }
        return oasis_ok(selection)

    return oasis_error(f"Unsupported profile '{profile}'.")


def flatten_selection(selection: Dict[str, List[int]]) -> List[int]:
    out: List[int] = []
    order = ("spawn", "see", "missile_or_attack", "pain", "death", "xdeath", "all")
    for key in order:
        if key not in selection:
            continue
        for idx in selection[key]:
            out.append(idx)
    for key, arr in selection.items():
        if key in order:
            continue
        for idx in arr:
            out.append(idx)
    return out


def generate_sprites_with_selection(
    model: MdlModel,
    palette: List[Tuple[int, int, int, int]],
    out_dir: str,
    sprite_prefix: str,
    out_w: int,
    out_h: int,
    angle_count: int,
    padding: int,
    selection: Dict[str, List[int]],
    profile_name: str,
    yaw_offset_deg: float,
    lock_grab_y: bool,
    doom_mirror_rotations: bool,
    write_manifest: bool,
) -> OASISResult[Dict[str, object]]:
    selected = flatten_selection(selection)
    if not selected:
        return oasis_error("No frame indices selected for output.")
    sub_frames = [model.frames[i] for i in selected]
    sub_model = MdlModel(
        skin_width=model.skin_width,
        skin_height=model.skin_height,
        skin_indices=model.skin_indices,
        stverts=model.stverts,
        triangles=model.triangles,
        frames=sub_frames,
    )
    result = generate_sprites(
        model=sub_model,
        palette=palette,
        out_dir=out_dir,
        sprite_prefix=sprite_prefix,
        out_w=out_w,
        out_h=out_h,
        angle_count=angle_count,
        padding=padding,
        yaw_offset_deg=yaw_offset_deg,
        lock_grab_y=lock_grab_y,
        doom_mirror_rotations=doom_mirror_rotations,
        write_manifest=write_manifest,
    )
    if not result.ok:
        return result
    data = result.data or {}
    data["source_frame_count"] = len(model.frames)
    data["selected_frame_count"] = len(selected)
    data["source_frame_indices"] = selected
    data["profile"] = profile_name
    data["profile_selection"] = selection
    manifest_path = data.get("manifest_path")
    if isinstance(manifest_path, str) and manifest_path:
        pathlib.Path(manifest_path).write_text(json.dumps(data, indent=2), encoding="utf-8")
    return oasis_ok(data)


def load_model_bytes(args: argparse.Namespace) -> OASISResult[bytes]:
    if args.mdl_path:
        return read_file_bytes(args.mdl_path)
    if args.mdl_pak and args.mdl_entry:
        return read_from_pak(args.mdl_pak, args.mdl_entry)
    return oasis_error("Provide either --mdl-path or (--mdl-pak and --mdl-entry).")


def load_palette_bytes(args: argparse.Namespace) -> OASISResult[bytes]:
    if args.palette_path:
        return read_file_bytes(args.palette_path)
    if args.palette_pak and args.palette_entry:
        return read_from_pak(args.palette_pak, args.palette_entry)
    return oasis_error("Provide either --palette-path or (--palette-pak and --palette-entry).")


def parse_args(argv: List[str]) -> argparse.Namespace:
    p = argparse.ArgumentParser(description="Generate Doom-style sprite angles/frames from Quake MDL.")
    p.add_argument("--mdl-path", help="Path to .mdl file on disk.")
    p.add_argument("--mdl-pak", help="Path to PAK file containing the MDL.")
    p.add_argument("--mdl-entry", help="PAK entry path for the MDL, e.g. progs/zombie.mdl")
    p.add_argument("--palette-path", help="Path to palette.lmp (768 bytes).")
    p.add_argument("--palette-pak", help="Path to PAK containing palette.lmp.")
    p.add_argument("--palette-entry", default="gfx/palette.lmp", help="PAK entry path for palette.lmp")
    p.add_argument("--out-dir", required=True, help="Output directory for generated PNG sprites.")
    p.add_argument("--sprite-prefix", required=True, help="4-char Doom sprite prefix (e.g. OQM2).")
    p.add_argument("--width", type=int, default=64, help="Output sprite width.")
    p.add_argument("--height", type=int, default=64, help="Output sprite height.")
    p.add_argument("--angles", type=int, default=8, help="Number of view angles to render.")
    p.add_argument("--padding", type=int, default=1, help="Pixel padding inside output image.")
    p.add_argument(
        "--profile",
        default="zombieman",
        choices=("full", "zombieman"),
        help="Frame selection profile. 'zombieman' matches Doom-like sprite counts.",
    )
    p.add_argument(
        "--yaw-offset-deg",
        type=float,
        default=0.0,
        help="Global yaw offset for angle mapping (degrees).",
    )
    p.add_argument(
        "--doom-mirror-rotations",
        action="store_true",
        help="Write true Doom-style mirrored angle lumps (A2A8, A3A7, A4A6).",
    )
    p.add_argument(
        "--no-manifest",
        action="store_true",
        help="Do not write manifest JSON file.",
    )
    return p.parse_args(argv)


def main(argv: List[str]) -> int:
    args = parse_args(argv)

    mdl_res = load_model_bytes(args)
    if not mdl_res.ok:
        print(f"[ERROR] {mdl_res.error}")
        return 1

    pal_res = load_palette_bytes(args)
    if not pal_res.ok:
        print(f"[ERROR] {pal_res.error}")
        return 1

    model_res = parse_mdl(mdl_res.data or b"")
    if not model_res.ok:
        print(f"[ERROR] {model_res.error}")
        return 1

    palette_res = load_quake_palette(pal_res.data or b"")
    if not palette_res.ok:
        print(f"[ERROR] {palette_res.error}")
        return 1

    model = model_res.data  # type: ignore[assignment]
    palette = palette_res.data  # type: ignore[assignment]
    sel_res = choose_frames_for_profile(model, args.profile)
    if not sel_res.ok:
        print(f"[ERROR] {sel_res.error}")
        return 1

    gen_res = generate_sprites_with_selection(
        model=model,
        palette=palette,
        out_dir=args.out_dir,
        sprite_prefix=args.sprite_prefix.upper(),
        out_w=args.width,
        out_h=args.height,
        angle_count=args.angles,
        padding=max(0, args.padding),
        selection=sel_res.data or {},
        profile_name=args.profile,
        yaw_offset_deg=args.yaw_offset_deg,
        lock_grab_y=(args.profile != "full"),
        doom_mirror_rotations=(args.doom_mirror_rotations or args.profile != "full"),
        write_manifest=(not args.no_manifest),
    )
    if not gen_res.ok:
        print(f"[ERROR] {gen_res.error}")
        return 1

    data = gen_res.data or {}
    print(
        "[DONE] Generated "
        f"{data.get('generated_png_count', 0)} PNGs, "
        f"frames={data.get('selected_frame_count', data.get('frame_count', 0))}"
        f"/{data.get('source_frame_count', data.get('frame_count', 0))}, "
        f"angles={data.get('angle_count', 0)}"
    )
    print(f"[DONE] Manifest: {data.get('manifest_path', '')}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))







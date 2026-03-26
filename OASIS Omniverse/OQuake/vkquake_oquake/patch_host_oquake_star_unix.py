#!/usr/bin/env python3
"""
Ensure vkQuake Quake/host.c calls OQuake_STAR_PollItems() every client frame.

Placement rules:
1) NOT only inside if (cls.state == ca_connected) with CL_ReadFromServer (menu/console would never pump).
2) NOT immediately before SCR_UpdateScreen (true); — that sits right after if (host_speeds.value) / time1;
   inserting PollItems there with mixed tabs/spaces triggers GCC -Werror=misleading-indentation.

Correct anchor: after CL_ReadFromServer (); block (outside the if), blank line(s), then OQuake_STAR_PollItems ();
then blank line(s), then the existing "// update video" comment. Same indent as that comment line.
"""
from __future__ import annotations

import re
import sys


def _already_has_pollitems_call(text: str) -> bool:
    """True if host.c already calls OQuake_STAR_PollItems (e.g. patched by apply_oquake_to_vkquake.ps1 on Windows)."""
    return bool(re.search(r"\bOQuake_STAR_PollItems\s*\(\s*\)\s*;", text))


def _strip_scr_anchor_pollitems(text: str) -> str:
    """Remove PollItems line inserted immediately before SCR_UpdateScreen (true)."""
    return re.sub(
        r"(\r?\n)(\s*)OQuake_STAR_PollItems\s*\(\s*\)\s*;\r?\n(\s*)(SCR_UpdateScreen\s*\(\s*true\s*\)\s*;)",
        r"\1\3\4",
        text,
        count=1,
    )


def _strip_adjacent_after_readfromserver(text: str) -> str:
    """Remove legacy: CL_ReadFromServer (); then next line PollItems (often inside ca_connected)."""
    return re.sub(
        r"(CL_ReadFromServer\s*\(\s*\)\s*;)\s*\r?\n\s*OQuake_STAR_PollItems\s*\(\s*\)\s*;",
        r"\1",
        text,
        count=1,
    )


def _insert_before_update_video(text: str) -> tuple[str, bool]:
    """Insert OQuake_STAR_PollItems between CL_ReadFromServer block and // update video."""
    # Braced: ReadFromServer; newline }; blank line; // update video
    pat_braced = re.compile(
        r"(CL_ReadFromServer\s*\(\s*\)\s*;\s*\r?\n\s*\}\s*\r?\n\r?\n)(\s*)(// update video)",
        re.MULTILINE,
    )
    m = pat_braced.search(text)
    if m:
        indent = m.group(2)
        ins = (
            m.group(1)
            + indent
            + "OQuake_STAR_PollItems ();\n"
            + "\n"
            + indent
            + m.group(3)
        )
        return text[: m.start()] + ins + text[m.end() :], True

    # Unbraced vkQuake: ReadFromServer; blank line; // update video
    pat_open = re.compile(
        r"(CL_ReadFromServer\s*\(\s*\)\s*;\s*\r?\n\r?\n)(\s*)(// update video)",
        re.MULTILINE,
    )
    m = pat_open.search(text)
    if m:
        indent = m.group(2)
        ins = (
            m.group(1)
            + indent
            + "OQuake_STAR_PollItems ();\n"
            + "\n"
            + indent
            + m.group(3)
        )
        return text[: m.start()] + ins + text[m.end() :], True

    return text, False


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: patch_host_oquake_star_unix.py <path/to/Quake/host.c>", file=sys.stderr)
        return 2
    path = sys.argv[1]
    try:
        text = open(path, encoding="utf-8", errors="replace").read()
    except OSError as e:
        print(f"read {path}: {e}", file=sys.stderr)
        return 1

    if _already_has_pollitems_call(text):
        print(
            f"[OQuake] {path}: OQuake_STAR_PollItems() already present; skipping host.c patch "
            "(same tree may be shared with Windows after apply_oquake_to_vkquake.ps1)."
        )
        return 0

    text = _strip_scr_anchor_pollitems(text)
    text = _strip_adjacent_after_readfromserver(text)

    text, ok = _insert_before_update_video(text)
    if not ok:
        print(
            f"{path}: could not find CL_ReadFromServer + // update video anchor — patch host.c manually.",
            file=sys.stderr,
        )
        return 1

    if '#include "oquake_star_integration.h"' not in text:
        if "#include \"oquake_version.h\"" in text:
            text = text.replace(
                '#include "oquake_version.h"',
                '#include "oquake_version.h"\n#include "oquake_star_integration.h"',
                1,
            )
        elif '#include "quakedef.h"' in text:
            text = text.replace(
                '#include "quakedef.h"',
                '#include "quakedef.h"\n#include "oquake_star_integration.h"',
                1,
            )

    try:
        open(path, "w", encoding="utf-8", newline="\n").write(text)
    except OSError as e:
        print(f"write {path}: {e}", file=sys.stderr)
        return 1

    print(
        f"[OQuake] Patched {path}: OQuake_STAR_PollItems () before // update video (every frame; -Wmisleading-indentation safe)."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

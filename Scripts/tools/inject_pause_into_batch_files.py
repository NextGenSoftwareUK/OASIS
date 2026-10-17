#!/usr/bin/env python3
"""Insert standardized pause before final exit /b in .bat files."""
import os
import re

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SCRIPTS_ROOT = os.path.normpath(os.path.join(REPO, "Scripts"))
SKIP_DIR_PARTS = (
    "node_modules",
    ".git",
    "Archived",
    f"ODOOM{os.sep}build{os.sep}",
    "bsp-w32.bat",
)
BANNER = "Press any key to exit"
BLOCK = [
    "echo.",
    "echo ========================================",
    "echo   Press any key to exit",
    "echo ========================================",
    'if not "%OASIS_BAT_NO_PAUSE%"=="1" pause >nul',
]


def should_skip(path: str) -> bool:
    norm = os.path.normpath(path)
    if norm.startswith(SCRIPTS_ROOT + os.sep):
        return True
    for s in SKIP_DIR_PARTS:
        if s in norm.replace("/", os.sep):
            return True
    return False


def process_file(path: str) -> bool:
    try:
        with open(path, encoding="utf-8", errors="replace") as f:
            raw = f.read()
    except OSError:
        return False
    if BANNER in raw:
        return False
    lines = raw.splitlines()
    if not lines:
        return False
    le = "\r\n" if "\r\n" in raw else "\n"
    last_exit = None
    for i in range(len(lines) - 1, -1, -1):
        s = lines[i].strip()
        if not s or s.lower().startswith("rem"):
            continue
        if re.match(r"exit\s+/b", s, re.I):
            last_exit = i
            break
    if last_exit is None:
        lines.extend(["", "REM OASIS: Explorer pause (OASIS_BAT_NO_PAUSE=1 skips)"] + BLOCK)
        with open(path, "w", encoding="utf-8", newline="") as f:
            f.write(le.join(lines) + le)
        return True
    j = last_exit - 1
    while j >= 0 and not lines[j].strip():
        j -= 1
    if j >= 0 and re.match(r"^\s*pause\s*$", lines[j], re.I):
        lines[j : j + 1] = BLOCK
    else:
        lines[last_exit:last_exit] = BLOCK + [""]
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(le.join(lines) + le)
    return True


def main():
    n = 0
    for root, dirs, files in os.walk(REPO):
        dirs[:] = [d for d in dirs if d not in (".git", "node_modules")]
        for name in files:
            if not name.lower().endswith(".bat"):
                continue
            path = os.path.join(root, name)
            if should_skip(path):
                continue
            try:
                if process_file(path):
                    print("Updated:", os.path.relpath(path, REPO))
                    n += 1
            except Exception as e:
                print("Skip", path, e)
    print("Done,", n, "batch files.")


if __name__ == "__main__":
    main()

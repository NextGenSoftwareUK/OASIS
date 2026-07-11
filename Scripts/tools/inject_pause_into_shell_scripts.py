#!/usr/bin/env python3
"""One-off helper: inject OASIS pause-on-exit into .sh files. Not run at build time."""
import os

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SKIP_NAMES = {"pause_on_exit.inc.sh", "inject_pause_into_shell_scripts.py"}
MARKER = "Scripts/include/pause_on_exit.inc.sh"
BLOCK = """
# OASIS: pause before exit when run from GUI (CI: OASIS_SCRIPT_NO_PAUSE=1)
if [[ "${OASIS_SCRIPT_NO_PAUSE:-}" != "1" ]]; then
  _OASIS_TD="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  while [[ "$_OASIS_TD" != "/" ]]; do
    if [[ -f "$_OASIS_TD/Scripts/include/pause_on_exit.inc.sh" ]]; then
      # shellcheck disable=SC1091
      source "$_OASIS_TD/Scripts/include/pause_on_exit.inc.sh"
      break
    fi
    _OASIS_TD="$(dirname "$_OASIS_TD")"
  done
fi
""".lstrip("\n")

SKIP_PATH_PARTS = (
    "run_oasis_header.sh",  # banner helper; subshell use would double-pause workflow
    "node_modules",
    ".git",
    "Archived/",
)


def should_skip(path: str) -> bool:
    if not path.endswith(".sh"):
        return True
    base = os.path.basename(path)
    if base in SKIP_NAMES:
        return True
    for p in SKIP_PATH_PARTS:
        if p in path.replace("\\", "/"):
            return True
    try:
        with open(path, encoding="utf-8", errors="replace") as f:
            c = f.read()
    except OSError:
        return True
    if MARKER in c and "source " in c and "pause_on_exit" in c:
        return True
    if "# OASIS: pause before exit when run from GUI" in c:
        return True
    first = c[:800].lower()
    if "bin/sh" in c[:30] and "bash" not in c[:30]:
        return True
    return False


def insert_block(content: str) -> str:
    lines = content.splitlines(keepends=True)
    # After first set -e / set -euo in first 40 lines
    for i, line in enumerate(lines[:40]):
        stripped = line.strip()
        if stripped == "set -e" or stripped.startswith("set -e ") or stripped.startswith("set -eo "):
            # Insert after this line (and following blank lines optional)
            j = i + 1
            while j < len(lines) and lines[j].strip() == "":
                j += 1
            return "".join(lines[:j]) + "\n" + BLOCK + "\n" + "".join(lines[j:])
    # After shebang + first comment block (max 5 lines)
    insert_at = 1
    for i in range(1, min(8, len(lines))):
        if lines[i].strip().startswith("#") or lines[i].strip() == "":
            insert_at = i + 1
        else:
            break
    return "".join(lines[:insert_at]) + "\n" + BLOCK + "\n" + "".join(lines[insert_at:])


def main():
    n = 0
    for root, dirs, files in os.walk(REPO):
        dirs[:] = [d for d in dirs if d not in (".git", "node_modules", "bin", "obj")]
        for name in files:
            if not name.endswith(".sh"):
                continue
            path = os.path.join(root, name)
            if should_skip(path):
                continue
            with open(path, encoding="utf-8", errors="replace") as f:
                content = f.read()
            new_content = insert_block(content)
            if new_content == content:
                continue
            with open(path, "w", encoding="utf-8", newline="\n") as f:
                f.write(new_content)
            print("Updated:", os.path.relpath(path, REPO))
            n += 1
    print("Done,", n, "files.")


if __name__ == "__main__":
    main()

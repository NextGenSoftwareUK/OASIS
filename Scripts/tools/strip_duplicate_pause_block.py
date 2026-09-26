#!/usr/bin/env python3
import re
import os

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
BLOCK_RE = re.compile(
    r"\n# OASIS: pause before exit when run from GUI \(CI: OASIS_SCRIPT_NO_PAUSE=1\)\n"
    r"if \[\[ \"\$\{OASIS_SCRIPT_NO_PAUSE:-\}\" != \"1\" \]\]; then\n"
    r"  _OASIS_TD=\"\$\(cd \"\$\(dirname \"\$\{BASH_SOURCE\[0\]\}\"\)\" && pwd\)\"\n"
    r"  while \[\[ \"\$_OASIS_TD\" != \"/\" \]\]; do\n"
    r"    if \[\[ -f \"\$_OASIS_TD/Scripts/include/pause_on_exit\.inc\.sh\" \]\]; then\n"
    r"      # shellcheck disable=SC1091\n"
    r"      source \"\$_OASIS_TD/Scripts/include/pause_on_exit\.inc\.sh\"\n"
    r"      break\n"
    r"    fi\n"
    r"    _OASIS_TD=\"\$\(dirname \"\$_OASIS_TD\"\)\"\n"
    r"  done\n"
    r"fi\n",
    re.MULTILINE,
)


def main():
    scripts_root = os.path.join(REPO, "Scripts")
    for root, _, files in os.walk(scripts_root):
        for name in files:
            if not name.endswith(".sh") or name == "pause_on_exit.inc.sh":
                continue
            path = os.path.join(root, name)
            with open(path, encoding="utf-8") as f:
                c = f.read()
            if "source \"$SCRIPT_DIR" not in c and "source \"${SCRIPT_DIR" not in c:
                continue
            if "pause_on_exit.inc.sh" not in c:
                continue
            new_c, n = BLOCK_RE.subn("\n", c, count=1)
            if n:
                with open(path, "w", encoding="utf-8", newline="\n") as f:
                    f.write(new_c)
                print("Stripped duplicate:", os.path.relpath(path, REPO))


if __name__ == "__main__":
    main()

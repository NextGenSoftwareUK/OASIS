"""Check published local-demo launcher links against the selected checkout.

Exit 1 means published launchers are missing. This is a source-availability
check, not native execution or hosted-provider acceptance.
"""
import argparse
import json
from pathlib import Path
import re


def audit(html, checkout):
    checkout = Path(checkout).resolve(strict=True)
    paths = sorted(set(re.findall(
        r"Scripts/TestHosts/[A-Za-z0-9_.-]+\.ps1", html)))
    missing = []
    for relative in paths:
        target = (checkout / relative).resolve()
        if not target.is_relative_to(checkout):
            raise ValueError("Launcher resolves outside the selected checkout.")
        if not target.is_file():
            missing.append(relative)
    return {"published": len(paths), "present": len(paths) - len(missing),
            "missingCount": len(missing), "missing": missing,
            "nativeExecuted": False, "hostedAcceptance": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--html", type=Path, required=True)
    parser.add_argument("--checkout", type=Path,
                        default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    checkout = args.checkout.resolve(strict=True)
    output = args.output.resolve()
    if not output.is_relative_to(checkout / "TestResults"):
        raise ValueError("Write regenerable reports under this checkout's TestResults.")
    report = audit(args.html.read_text(encoding="utf-8"), checkout)
    if report["published"] == 0:
        raise ValueError("No published launchers found; refusing a vacuous pass.")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report))
    return 1 if report["missingCount"] else 0


if __name__ == "__main__":
    raise SystemExit(main())

"""Keep the dev watch exclusion narrow and reject solution-based API builds."""
import json
from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[1]


def validate_dockerfile(text, label):
    commands = "\n".join(line for line in text.splitlines() if not line.lstrip().startswith("#"))
    if re.search(r"\.sln(?:x)?\b", commands, re.IGNORECASE):
        raise ValueError(f"{label}: solution input requires reviewing the watch exclusion")
    if not re.search(r'dotnet\s+publish\s+"[^"\n]+\.csproj"', commands):
        raise ValueError(f"{label}: expected a direct project publication")


def validate(root=ROOT):
    policy = json.loads((root / "Scripts/railway_dev_watch_policy.json").read_text(encoding="utf-8"))
    if policy.get("schema") != 1 or policy.get("environmentName") != "dev":
        raise ValueError("Only the declared dev policy is supported")
    if policy.get("apis") != list(range(4, 11)):
        raise ValueError("The WEB4-WEB10 inventory must be explicit and complete")
    if policy.get("watchPatterns") != ["**", "!/The OASIS.sln"]:
        raise ValueError("Every path except the root solution must remain watched")
    for api in policy["apis"]:
        file = root / f"Docker/Dockerfile.web{api}"
        validate_dockerfile(file.read_text(encoding="utf-8"), file.name)
    return policy


if __name__ == "__main__":
    validate()
    print("PASS: all seven APIs publish projects directly; only the root solution is excluded")

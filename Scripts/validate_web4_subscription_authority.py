#!/usr/bin/env python3
"""Structural guard for the single WEB4 usage protocol; runtime suites test semantics."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
MIDDLEWARE = {
    "WEB5": ROOT / "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Middleware/SubscriptionMiddleware.cs",
    "WEB6": ROOT / "WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Middleware/SubscriptionMiddleware.cs",
    "WEB7": ROOT / "WEB7/NextGenSoftware.OASIS.Web7.WebAPI/Middleware/SubscriptionMiddleware.cs",
    "WEB8": ROOT / "WEB8/NextGenSoftware.OASIS.Web8.WebAPI/Middleware/SubscriptionMiddleware.cs",
    "WEB9": ROOT / "WEB9/NextGenSoftware.OASIS.Web9.WebAPI/Middleware/SubscriptionMiddleware.cs",
    "WEB10": ROOT / "WEB10/NextGenSoftware.OASIS.Web10.WebAPI/Middleware/SubscriptionMiddleware.cs",
}

errors: list[str] = []
for service, path in MIDDLEWARE.items():
    if not path.exists():
        errors.append(f"{service}: missing {path.relative_to(ROOT)}")
        continue
    source = path.read_text(encoding="utf-8")
    for required in ("SubscriptionPolicy", "UsageEndpointPolicy"):
        if required not in source:
            errors.append(f"{service}: missing contract token {required}")
    project = path.parent.parent
    hosting = "\n".join(p.read_text(encoding="utf-8") for p in (project / "Program.cs", project / "Startup.cs") if p.exists())
    for required in ("AddWeb4UsageLedger", "Web4UsageLedgerMiddleware", f'"{service}"'):
        if required not in hosting:
            errors.append(f"{service}: hosting does not register {required}")
    for p in project.rglob("*.cs"):
        if any(part in {"obj", "bin"} for part in p.parts):
            continue
        active = p.read_text(encoding="utf-8")
        if "AuthorizeRequestAsync(" in active:
            errors.append(f"{service}: obsolete authorize-request call in {p.relative_to(ROOT)}")

legacy_dir = ROOT / "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Services/Subscription"
if legacy_dir.exists() and any(legacy_dir.glob("*.cs")):
    errors.append("WEB5: legacy local subscription service still exists")

if errors:
    print("WEB4 subscription authority validation FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print("WEB4 shared usage protocol structural validation passed for WEB5-WEB10; run runtime tests separately.")

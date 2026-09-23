#!/usr/bin/env python3
"""Fail CI if a consuming service can activate the ledger without the Railway rollout gate."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
PROGRAMS = {
    "WEB5": ROOT / "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Program.cs",
    "WEB6": ROOT / "WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Program.cs",
    "WEB7": ROOT / "WEB7/NextGenSoftware.OASIS.Web7.WebAPI/Program.cs",
    "WEB8": ROOT / "WEB8/NextGenSoftware.OASIS.Web8.WebAPI/Program.cs",
    "WEB9": ROOT / "WEB9/NextGenSoftware.OASIS.Web9.WebAPI/Program.cs",
    "WEB10": ROOT / "WEB10/NextGenSoftware.OASIS.Web10.WebAPI/Program.cs",
}

errors = []
for service, path in PROGRAMS.items():
    source = path.read_text(encoding="utf-8")
    if 'builder.Configuration["SUBSCRIPTION_USAGE_ENABLED"]' not in source:
        errors.append(f"{service}: missing SUBSCRIPTION_USAGE_ENABLED configuration gate")
    registration = rf"if \(subscriptionUsageEnabled\)\s*builder\.Services\.AddWeb4UsageLedger\(builder\.Configuration, \"{service}\""
    middleware = r"if \(subscriptionUsageEnabled\)\s*app\.UseMiddleware<Web4UsageLedgerMiddleware>\(\);"
    if not re.search(registration, source):
        errors.append(f"{service}: ledger registration is not guarded")
    if not re.search(middleware, source):
        errors.append(f"{service}: ledger middleware is not guarded")

if errors:
    raise SystemExit("Subscription rollout gate validation failed:\n- " + "\n- ".join(errors))
print("Subscription rollout gate validation passed for WEB5-WEB10.")

web4 = (ROOT / "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Startup.cs").read_text(encoding="utf-8")
if not re.search(r'if \(subscriptionUsageEnabled\)\s*services\.AddHostedService<Services\.Subscription\.SubscriptionUsageExpiryWorker>\(\);', web4):
    raise SystemExit("WEB4 expiry worker is not guarded by SUBSCRIPTION_USAGE_ENABLED.")
if "AddUsageReconciliation(services, subscriptionUsageEnabled)" not in web4:
    raise SystemExit("WEB4 reconciliation worker is not guarded by SUBSCRIPTION_USAGE_ENABLED.")
print("Subscription rollout gate validation passed for WEB4 background workers.")

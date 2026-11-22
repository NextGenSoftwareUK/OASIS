#!/usr/bin/env python3
"""Fail CI if a consuming service can activate the ledger without the Railway rollout gate."""
from pathlib import Path
import re
from subscription_source_tree import source_tree_from_args

ROOT = Path(__file__).resolve().parents[1]
PROGRAMS = {
    "WEB5": ROOT / "STAR ODK/NextGenSoftware.OASIS.STAR.WebAPI/Program.cs",
    "WEB6": ROOT / "WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Program.cs",
    "WEB7": ROOT / "WEB7/NextGenSoftware.OASIS.Web7.WebAPI/Program.cs",
    "WEB8": ROOT / "WEB8/NextGenSoftware.OASIS.Web8.WebAPI/Program.cs",
    "WEB9": ROOT / "WEB9/NextGenSoftware.OASIS.Web9.WebAPI/Program.cs",
    "WEB10": ROOT / "WEB10/NextGenSoftware.OASIS.Web10.WebAPI/Program.cs",
}


def web4_worker_gate_errors(source):
    """Require both rollout and hosted-maintenance controls in the shipping registration."""
    errors = []
    reconciliation = (
        r"AddUsageReconciliation\(\s*services\s*,\s*subscriptionUsageEnabled\s*&&\s*"
        r"Services\.Operations\.HostedMongoWorkerControl\.ReconciliationMayStart\(Configuration\)\s*\)"
    )
    if not re.search(reconciliation, source):
        errors.append("reconciliation must require subscription usage AND hosted reconciliation permission")
    enabled = re.search(r"if\s*\(subscriptionUsageEnabled\)\s*\{(?P<body>.*?)\}", source, re.DOTALL)
    if not enabled:
        return errors + ["missing subscription usage registration block"]
    block = enabled.group("body")
    for worker in ("SubscriptionUsageExpiryWorker", "PaidOverageBillingWorker"):
        registration = (
            r"Services\.Operations\.HostedMongoWorkerControl\.AddMaintenanceWorker"
            r"<Services\.Subscription\." + worker + r">\(services,\s*Configuration\)"
        )
        if not re.search(registration, block):
            errors.append(worker + " must require subscription usage and the hosted maintenance control")
    if 'AddSubscriptionUsageTelemetry(services, Configuration, "WEB4")' not in block:
        errors.append("WEB4 telemetry must require subscription usage")
    return errors


def main():
    errors = []
    tree = source_tree_from_args()
    for service, path in PROGRAMS.items():
        if not tree.exists(path):
            errors.append(f"{service}: missing {path.relative_to(ROOT)}")
            continue
        source = tree.read(path)
        if 'builder.Configuration["SUBSCRIPTION_USAGE_ENABLED"]' not in source:
            errors.append(f"{service}: missing SUBSCRIPTION_USAGE_ENABLED configuration gate")
        # The guarded statement may be the bare body of the if or the first statement of its braced block.
        registration = rf"if \(subscriptionUsageEnabled\)\s*\{{?\s*(//[^\n]*\n\s*)*builder\.Services\.AddWeb4UsageLedger\(builder\.Configuration, \"{service}\""
        middleware = r"if \(subscriptionUsageEnabled\)\s*\{?\s*(//[^\n]*\n\s*)*app\.UseMiddleware<Web4UsageLedgerMiddleware>\(\);"
        if not re.search(registration, source):
            errors.append(f"{service}: ledger registration is not guarded")
        if not re.search(middleware, source):
            errors.append(f"{service}: ledger middleware is not guarded")

    if errors:
        raise SystemExit("Subscription rollout gate validation failed:\n- " + "\n- ".join(errors))
    print("Subscription rollout gate validation passed for WEB5-WEB10.")

    web4 = tree.read(ROOT / "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Startup.cs")
    worker_errors = web4_worker_gate_errors(web4)
    if worker_errors:
        raise SystemExit("WEB4 worker rollout gate validation failed:\n- " + "\n- ".join(worker_errors))
    print("Subscription rollout gate validation passed for WEB4 background workers.")


if __name__ == "__main__":
    main()

"""Prove an incident image contains only the accepted pause on its serving base.

This gate is for the two older serving snapshots, which predate the current SDK
publication verifier. Their complete original Docker publish still runs in CI.
No dependency pin, DNA, authorization or foreground subscription change is allowed.
The accepted clone transport explicitly fetches each immutable pin before checkout.
"""

import argparse
import re
import subprocess


BASES = {
    "staging": "28a2eb25abd2394a910b10da1a6572ba576ede0c",
    "production": "cf47b47e595b6f744ecdd40c076ca046ab531be7",
}
STARTUP = "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Startup.cs"
HELPER = "ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/Operations/HostedMongoWorkerControl.cs"
ACCEPTED_HELPER_BLOB = "7b9edae94e073f639eb3352e5796073b72a1d54e"
CLONE_SCRIPT = "Docker/clone-pinned-oasis-dependencies.sh"
ACCEPTED_CLONE_BLOB = "daa4b566bc8e6bad49dec1fedb6cda5382a6ee40"
MAINTENANCE_BASES = {
    "staging-maintenance": "7536f6d4767750137ff76f1d5284a3ccd888dc00",
    "production-maintenance": "73d662740c469f1ded59c2e02bcf4bbc4fb7dac7",
}
# Qualified on b92ca4289a31; a blob pin works with the image job's shallow checkout.
MAINTENANCE_ACCEPTED_HELPER_BLOB = "c36509a4890d01ab8b44710860d208ce3a4d9165"


def verify_maintenance(environment, source):
    base = MAINTENANCE_BASES[environment]
    if git("show", "-s", "--format=%P", source).decode().split() != [base]:
        raise ValueError("Maintenance must directly extend the inspected serving source.")
    if set(git("diff", "--name-only", base, source).decode().splitlines()) != {STARTUP, HELPER}:
        raise ValueError("Only maintenance registration gates and the accepted helper may change.")
    if git("rev-parse", source + ":" + HELPER).decode().strip() != MAINTENANCE_ACCEPTED_HELPER_BLOB:
        raise ValueError("Maintenance helper differs from the five-case qualified implementation.")
    expected = git("show", base + ":" + STARTUP)
    workers = [("Subscription", "SubscriptionUsageExpiryWorker"),
               ("Subscription", "PaidOverageBillingWorker")]
    if environment == "production-maintenance":
        workers.append(("Sandbox", "SandboxCleanupHostedService"))
    for namespace, name in workers:
        before = ("services.AddHostedService<Services." + namespace + "." + name + ">();").encode()
        after = ("Services.Operations.HostedMongoWorkerControl.AddMaintenanceWorker<Services."
                 + namespace + "." + name + ">(services, Configuration);").encode()
        if expected.count(before) != 1:
            raise ValueError("Unexpected serving maintenance registration: " + name)
        expected = expected.replace(before, after)
    if expected != git("show", source + ":" + STARTUP):
        raise ValueError("Startup differs beyond the exact maintenance registration gates.")
    print(environment + ": exact maintenance gates; dependencies/DNA/foreground/security preserved")


def git(*args):
    return subprocess.check_output(["git", *args])


def verify(environment, source):
    if not re.fullmatch(r"[0-9a-f]{40}", source):
        raise ValueError("Use an immutable full source SHA.")
    if environment in MAINTENANCE_BASES:
        return verify_maintenance(environment, source)
    base = BASES[environment]
    parents = git("show", "-s", "--format=%P", source).decode().split()
    if parents != [base]:
        raise ValueError("The pause must be a direct, single-parent serving-source patch.")
    changed = set(git("diff", "--name-only", base, source).decode().splitlines())
    if changed != {STARTUP, HELPER, CLONE_SCRIPT}:
        raise ValueError("Only the registration gates, helper and pinned-fetch transport may change.")
    helper_blob = git("rev-parse", source + ":" + HELPER).decode().strip()
    if helper_blob != ACCEPTED_HELPER_BLOB:
        raise ValueError("The helper differs from the six-case accepted dev implementation.")
    clone_blob = git("rev-parse", source + ":" + CLONE_SCRIPT).decode().strip()
    if clone_blob != ACCEPTED_CLONE_BLOB:
        raise ValueError("The dependency transport differs from the accepted exact-pin fetch implementation.")
    expected = git("show", base + ":" + STARTUP)
    names = ["HyperDriveCommandHostedService", "HyperDriveFanOutHostedService",
             "HyperDriveDomainChangeCaptureHostedService", "HyperDriveSyncCompactionHostedService"]
    if environment == "production":
        names.append("HolonReplicationDeliveryHostedService")
    for name in names:
        before = ("services.AddHostedService<Services.HyperDrive." + name + ">();").encode()
        after = ("Services.Operations.HostedMongoWorkerControl.AddHyperDriveWorker<Services.HyperDrive."
                 + name + ">(services, Configuration);").encode()
        if expected.count(before) != 1:
            raise ValueError("Serving worker registration is not the inspected contract: " + name)
        expected = expected.replace(before, after)
    before = b"Services.SubscriptionReconciliation.UsageReconciliationRegistration.AddUsageReconciliation(services, subscriptionUsageEnabled);"
    after = b"Services.SubscriptionReconciliation.UsageReconciliationRegistration.AddUsageReconciliation(services, subscriptionUsageEnabled && Services.Operations.HostedMongoWorkerControl.ReconciliationMayStart(Configuration));"
    if expected.count(before) != 1:
        raise ValueError("Serving reconciliation registration is not the inspected contract.")
    expected = expected.replace(before, after)
    if expected != git("show", source + ":" + STARTUP):
        raise ValueError("Startup includes changes beyond the exact pause registration edits.")
    print(environment + ": exact pause and pinned-fetch transport; dependency pins/DNA/foreground code preserved")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--environment", required=True, choices=[*BASES, *MAINTENANCE_BASES])
    parser.add_argument("--source", required=True)
    args = parser.parse_args()
    verify(args.environment, args.source)

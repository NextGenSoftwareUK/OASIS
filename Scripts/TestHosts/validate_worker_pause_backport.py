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


def git(*args):
    return subprocess.check_output(["git", *args])


def verify(environment, source):
    if not re.fullmatch(r"[0-9a-f]{40}", source):
        raise ValueError("Use an immutable full source SHA.")
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
    parser.add_argument("--environment", required=True, choices=BASES)
    parser.add_argument("--source", required=True)
    args = parser.parse_args()
    verify(args.environment, args.source)

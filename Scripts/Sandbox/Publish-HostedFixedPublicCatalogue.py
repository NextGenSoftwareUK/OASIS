"""Verify and explicitly apply a prepared fixed public CQL catalogue."""

import argparse
import re

from pathlib import Path
from runpy import run_path
from uuid import UUID


def publish(preparation, guard, providers, prefix, release_ref, deployment_id, *, apply):
    config = preparation["configuration"]
    verifier = preparation["verifier"]
    if (not providers or len(providers) != len(set(providers))
            or any(provider not in verifier.FIXED_PUBLIC_CATALOGUES for provider in providers)):
        raise ValueError("Only fixed reviewed public catalogues are supported")
    if not re.fullmatch(r"[a-z][a-z0-9-]{0,63}", prefix):
        raise ValueError("A canonical configuration snapshot prefix is required")
    if not release_ref or str(UUID(deployment_id)) != deployment_id:
        raise ValueError("An explicit reviewed ref and deployment ID are required")

    before = config.protected_read(f"{prefix}-before-variables.dpapi")
    after = config.protected_read(f"{prefix}-candidate-variables.dpapi")
    expected = before
    source_keys = []
    preset_keys = []
    for provider in providers:
        descriptor = f"{prefix}-descriptor.json" if len(providers) == 1 else f"{prefix}-{provider}-descriptor.json"
        review = verifier.reviewed_exact_key_source(provider, config.STATE / descriptor)
        source_binding = review["sourceBinding"]
        if len(source_binding) != 1 or any(key in expected for key in source_binding):
            raise RuntimeError("Existing catalogue enrollment requires reconciliation")
        expected = preparation["candidate"](expected | source_binding, provider, review)
        source_keys.extend(source_binding)
        preset_keys.append("CQL_HOSTED_QUERY_EXAMPLES__" + provider)

    keys = (*source_keys, preparation["ADMISSION"], preparation["GRANTS"], *preset_keys)
    changed = {key for key in before.keys() | after.keys() if before.get(key) != after.get(key)}
    if changed != set(keys) or after != expected:
        raise RuntimeError("Prepared settings do not match the exact reviewed catalogue")

    def require_deployment():
        if guard(config, release_ref=release_ref) != deployment_id:
            raise RuntimeError("The exact reviewed code deployment must be current")

    require_deployment()
    if config.variables() != before:
        raise RuntimeError("Live settings differ from the reviewed baseline")
    if apply:
        config.apply_values(prefix, keys, reviewed_snapshots=(before, after), before_mutation=require_deployment)
    return len(keys)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("providers", nargs="+")
    parser.add_argument("--prefix", required=True)
    parser.add_argument("--release-ref", required=True)
    parser.add_argument("--deployment-id", required=True)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    scripts = Path(__file__).resolve().parent
    prepare = run_path(str(scripts / "Prepare-HostedExactKeyConfiguration.py"))
    guard = run_path(str(scripts / "Prepare-TenSourcePublicBatch.py"))["require_matching_release"]
    count = publish(prepare, guard, args.providers, args.prefix, args.release_ref, args.deployment_id, apply=args.apply)
    print(f"Verified {count} reviewed settings" + (" and applied them without redeploying." if args.apply else "."))


if __name__ == "__main__":
    main()

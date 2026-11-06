"""Prepare a protected WEB4 settings candidate from a reviewed protected baseline."""
import argparse
import importlib.util
import json
from pathlib import Path


def load(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + ".py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


configuration = load("Publish-HostedSolidConfiguration")
verifier = load("Verify-HostedQuerySource")
ADMISSION = "PREPARED_STORAGE_ADMISSION_JSON"
GRANTS = "CQL_PUBLIC_READ_CATALOGUES_JSON"
EXAMPLES = "CQL_HOSTED_QUERY_EXAMPLES_JSON"


def candidate(before, provider, review):
    if provider not in verifier.REVIEWED_EXACT_KEY_PROVIDERS:
        raise ValueError("An explicitly prepared provider is required.")
    if json.loads(before["OASIS_DNA_JSON"])["OASIS"]["Sandbox"]["Enabled"] is not True:
        raise ValueError("An enabled WEB4 sandbox baseline is required.")
    if any(before.get(key) != value for key, value in review.get("sourceBinding", {}).items()):
        raise ValueError("Protected source settings do not match the reviewed teaching catalogue.")
    admission = json.loads(before[ADMISSION])
    grants = json.loads(before.get(GRANTS, "{}"))
    partition = "CQL_HOSTED_QUERY_EXAMPLES__" + provider
    if (provider in admission["queryOnlyProviders"] or provider in admission["ordinaryProviders"]
            or any(row["provider"] == provider for row in admission["nativeProviders"] + admission["queryTariffs"])
            or provider in grants or partition in before or provider in json.loads(before.get(EXAMPLES, "{}"))):
        raise ValueError("Existing provider settings require explicit reconciliation.")

    free = [row for row in admission["queryTariffs"]
            if row["provider"] == "IPFSOASIS" and type(row["costUsd"]) in (int, float) and row["costUsd"] == 0]
    if len(free) != 1:
        raise ValueError("One reviewed zero-cost sandbox tariff template is required.")
    presets = review["presets"]
    full = next(row for row in presets if row["id"] == "public-records")
    admission["queryOnlyProviders"].append(provider)
    admission["queryTariffs"].append(dict(free[0], provider=provider))
    grants[provider] = full["request"]["filters"][0]["value"]
    updates = {ADMISSION: configuration.compact(admission), GRANTS: configuration.compact(grants),
               partition: configuration.compact(presets)}
    if any(len(value.encode()) > 32768 for value in updates.values()):
        raise ValueError("Candidate exceeds the hosted variable byte budget.")
    return before | updates


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("provider", choices=verifier.REVIEWED_EXACT_KEY_PROVIDERS)
    parser.add_argument("--catalogue", required=True, type=Path)
    parser.add_argument("--baseline", required=True, type=Path, help="Reviewed DPAPI settings snapshot.")
    parser.add_argument("--output", required=True, type=Path, help="New DPAPI candidate path.")
    args = parser.parse_args()
    if args.baseline.suffix.lower() != ".dpapi" or args.output.suffix.lower() != ".dpapi":
        parser.error("Use protected .dpapi settings files.")
    review = verifier.reviewed_exact_key_source(args.provider, args.catalogue)
    protect = configuration.verification.protected_token
    baseline = json.loads(protect(args.baseline.read_bytes(), decrypt=True))
    prepared = candidate(baseline, args.provider, review)
    configuration.write_new(args.output, protect(configuration.compact(prepared).encode()))
    print(f"Prepared protected {args.provider} candidate with three settings changes.")


if __name__ == "__main__":
    main()

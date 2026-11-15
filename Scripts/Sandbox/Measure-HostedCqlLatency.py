"""Measure live Development CQL without creating fixtures or accounts."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import importlib.util
import json
from pathlib import Path
from statistics import median
from time import perf_counter


def measure(query, token, request, route, provider, samples, concurrency):
    def read(_):
        started = perf_counter()
        rows = query.call(route, token, request)["result"]["rows"]
        elapsed = round((perf_counter() - started) * 1000)
        if route.endswith("/federation"):
            if any(row["selected"]["provider"] != provider for row in rows):
                raise RuntimeError("Federated sample changed provider provenance.")
            identities = {row["selected"]["row"]["id"] for row in rows}
        else:
            identities = {row["id"] for row in rows}
        if not identities or len(identities) != len(rows):
            raise RuntimeError("Sample did not return unique, nonempty results.")
        return identities, elapsed

    expected, first = read(0)
    warm = []
    with ThreadPoolExecutor(max_workers=concurrency) as workers:
        for identities, elapsed in workers.map(read, range(samples)):
            if expected != identities:
                raise RuntimeError("The result set changed between samples.")
            warm.append(elapsed)
    return expected, {
        "firstObservedMs": first,
        "warmSamples": samples,
        "warmMedianMs": median(warm),
        "warmMaxMs": max(warm),
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("provider")
    parser.add_argument("state", type=Path, help="Existing Development visitor state directory.")
    parser.add_argument("--example", default="public-subset")
    parser.add_argument("--samples", type=int, choices=range(2, 17), default=8)
    parser.add_argument("--concurrency", type=int, choices=range(1, 5), default=1)
    args = parser.parse_args()
    if args.concurrency > args.samples:
        parser.error("--concurrency cannot exceed --samples")

    spec = importlib.util.spec_from_file_location("hosted_query", Path(__file__).with_name("Verify-HostedQuerySource.py"))
    query = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(query)
    _, token = query.session(args.state, "visitor", allow_create=False)
    sources = query.call("data/query-cql/providers?examplesVersion=2", token, method="GET")["result"]
    source = next(item for item in sources if item["provider"] == args.provider)
    if not source["available"] or not source["federationAvailable"]:
        raise RuntimeError("Provider must be ready in both catalogues.")
    requests = {example["id"]: example["request"] for example in source["examples"]}
    direct_ids, direct = measure(query, token, requests[args.example], "data/query-cql", args.provider, args.samples, args.concurrency)
    pair_ids, paired = measure(query, token, requests["mongo-" + args.example],
                               "data/query-cql/federation", args.provider, args.samples, args.concurrency)
    if direct_ids != pair_ids:
        raise RuntimeError("Native and federated samples returned different records.")
    print(json.dumps({"provider": args.provider, "example": args.example, "rows": len(direct_ids),
                      "concurrency": args.concurrency, "native": direct, "federated": paired}, indent=2))


if __name__ == "__main__":
    main()

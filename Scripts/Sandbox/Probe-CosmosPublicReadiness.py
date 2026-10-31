"""Bounded, read-only Cosmos Hub REST feasibility probe; not CQL acceptance."""
import hashlib
import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from urllib.request import Request, build_opener, HTTPRedirectHandler


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise ValueError("Endpoint redirects are not accepted")


BASE = "https://cosmos-rest.publicnode.com"
LIMIT = 256 * 1024


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--endpoint", choices=[BASE, "https://rest.cosmoshub-main.ccvalidators.com"], default=BASE)
    args = parser.parse_args()
    endpoint = args.endpoint
    opener = build_opener(NoRedirect())
    evidence = []

    def get(path, height=None):
        headers = {"Accept": "application/json"}
        if height:
            headers["x-cosmos-block-height"] = height
        with opener.open(Request(endpoint + path, headers=headers), timeout=20) as response:
            raw = response.read(LIMIT + 1)
            if len(raw) > LIMIT:
                raise ValueError("Response exceeds 256 KiB")
            observed = response.headers.get("x-cosmos-block-height")
            evidence.append({"path": path, "bytes": len(raw),
                             "sha256": hashlib.sha256(raw).hexdigest(),
                             "requestedHeight": height, "responseHeight": observed})
            if height and observed != height:
                raise ValueError("Requested state height was not acknowledged")
            return json.loads(raw)

    report = {"endpoint": endpoint, "utc": datetime.now(timezone.utc).isoformat(),
              "scope": "readiness only, not CQL or cryptographic consensus proof",
              "requests": evidence}
    try:
        block = get("/cosmos/base/tendermint/v1beta1/blocks/latest")
        header = block["block"]["header"]
        if header["chain_id"] != "cosmoshub-4":
            raise ValueError("Unexpected chain identity")
        height = header["height"]
        if not isinstance(height, str) or not height.isascii() or not height.isdecimal():
            raise ValueError("Invalid height")
        report.update(blockHeight=height, blockHash=block["block_id"]["hash"])
        # Latest block RPC may lead the committed application query context by one.
        # Probe its explicitly advertised state height, never silently retry latest.
        state_height = evidence[-1]["responseHeight"]
        if state_height not in (height, str(int(height) - 1)):
            raise ValueError("Unexpected application state height")
        height = state_height
        report["stateHeight"] = height
        # Discover only three addresses, then test individual height-pinned reads.
        page = get("/cosmos/staking/v1beta1/validators?pagination.limit=3", height)
        addresses = [v["operator_address"] for v in page["validators"]]
        if len(addresses) != 3 or len(set(addresses)) != 3:
            raise ValueError("Expected three distinct validators")
        for address in addresses:
            if not address.startswith("cosmosvaloper1") or not address.isalnum():
                raise ValueError("Unexpected validator address")
            item = get("/cosmos/staking/v1beta1/validators/" + address, height)["validator"]
            if item["operator_address"] != address:
                raise ValueError("Validator identity mismatch")
        report.update(passed=True, addresses=addresses)
    except Exception as error:
        report.update(passed=False, errorType=type(error).__name__, error=str(error))
    filename = "readiness-publicnode.json" if endpoint == BASE else "readiness-cryptocrew.json"
    destination = Path(__file__).resolve().parents[2] / "TestResults/CosmosPublicSourceCandidateCurrent" / filename
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())

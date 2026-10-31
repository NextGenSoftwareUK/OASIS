#!/usr/bin/env python3
"""Publish WEB6 pass-through unit prices (sandbox_ms, audio_ms) as a new catalogue version.

Fetches the active catalogue, replaces the unitPrices listed below (others are kept), bumps the
version and publishes it. Requires a JWT for an OASIS Wizard avatar (or approved subscription admin).

Usage:
    WEB6_ADMIN_JWT=<jwt> python3 Scripts/web6_publish_unit_prices.py [--base https://dev.api.web6.oasisomniverse.one] [--dry-run]

Prices are provider list cost per unit with no markup (see Docs/Devs/API Documentation/WEB6/WEB6_vs_OpenRouter_vs_Hermes.md).
"""
import argparse
import datetime
import json
import os
import sys
import urllib.error
import urllib.request

# provider, model, unit, USD per unit, max units per call
UNIT_PRICES = [
    ("E2B", "sandbox", "sandbox_ms", "0.000000028", 300_000),      # ~$0.000028/s, 2 vCPU sandbox
    ("Daytona", "sandbox", "sandbox_ms", "0.000000028", 300_000),  # comparable per-vCPU-second rate
    ("Judge0", "sandbox", "sandbox_ms", "0.000000005", 300_000),   # self-hosted; covers hosting only
    ("AssemblyAI", "nano", "audio_ms", "0.000000033", 3_600_000),  # ~$0.12/hour
    ("Deepgram", "nova-3", "audio_ms", "0.000000072", 3_600_000),  # ~$0.0043/minute
]


def call(method, url, jwt, body=None):
    # Decimal prices travel as fixed-point JSON numbers (not 2.8e-08 floats) for the server's decimal parser.
    data = json.dumps(body).replace('"@dec:', '').replace(':dec@"', '').encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method, headers={
        "Authorization": f"Bearer {jwt}", "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            return json.loads(resp.read() or b"{}")
    except urllib.error.HTTPError as e:
        sys.exit(f"{method} {url} failed: {e.code} {e.read().decode(errors='replace')}")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", default="https://dev.api.web6.oasisomniverse.one")
    parser.add_argument("--dry-run", action="store_true", help="validate only; do not publish")
    args = parser.parse_args()
    jwt = os.environ.get("WEB6_ADMIN_JWT")
    if not jwt:
        sys.exit("Set WEB6_ADMIN_JWT to a Wizard avatar's JWT.")

    catalogue = call("GET", f"{args.base}/v1/admin/config/catalogue", jwt)
    replaced = {(p, m, u) for p, m, u, _, _ in UNIT_PRICES}
    kept = [u for u in catalogue.get("unitPrices") or []
            if (u["provider"], u["model"], u["unit"]) not in replaced]
    catalogue["unitPrices"] = kept + [
        {"provider": p, "model": m, "unit": u, "usdPerUnit": f"@dec:{price}:dec@", "maxUnitsPerCall": cap}
        for p, m, u, price, cap in UNIT_PRICES]
    now = datetime.datetime.now(datetime.timezone.utc)
    catalogue["version"] = now.strftime("%Y.%m.%d-units-%H%M%S")
    catalogue["createdAtUtc"] = now.isoformat()
    catalogue["reason"] = "Pass-through sandbox_ms and audio_ms unit prices (no markup)"

    print(json.dumps(call("POST", f"{args.base}/v1/admin/config/catalogue/validate", jwt, catalogue)))
    if args.dry_run:
        return
    print(json.dumps(call("POST", f"{args.base}/v1/admin/config/catalogue/publish", jwt,
                          {"reason": catalogue["reason"], "catalogue": catalogue})))


if __name__ == "__main__":
    main()

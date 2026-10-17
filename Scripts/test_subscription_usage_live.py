#!/usr/bin/env python3
"""Credential-aware WEB4-WEB10 live contract matrix. Uses isolated test subscribers only.

No credentials are written to disk or included in output. Exit 2 means missing prerequisites,
exit 1 means a failed assertion. See Docs/Devs/WEB4_SUBSCRIPTION_USAGE_OPERATIONS.md.
"""
import argparse
from datetime import datetime, timezone
from concurrent.futures import ThreadPoolExecutor
import hashlib
import ipaddress
import json
import os
from pathlib import Path
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid

SERVICES = tuple(f"WEB{i}" for i in range(5, 11))


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, new_url):
        # Account mutations must reach the configured endpoint once. urllib's default
        # redirect behavior also forwards bearer/service secrets to another origin.
        raise ValueError("HTTP redirects are forbidden for authenticated usage requests")


HTTP = urllib.request.build_opener(NoRedirect())


def validate_base_url(base):
    if not isinstance(base, str) or not base or any(c.isspace() or ord(c) < 32 for c in base) or "\\" in base:
        raise ValueError("A valid HTTPS base URL is required")
    try:
        parsed = urllib.parse.urlsplit(base)
        host, port = parsed.hostname, parsed.port
    except ValueError:
        raise ValueError("A valid HTTPS base URL is required") from None
    if not host or parsed.scheme not in ("http", "https") or parsed.username is not None or parsed.password is not None or parsed.query or parsed.fragment:
        raise ValueError("Base URLs must use HTTP(S), without credentials, a query or a fragment")
    try:
        loopback = ipaddress.ip_address(host).is_loopback
    except ValueError:
        loopback = host.lower() == "localhost"
    if parsed.scheme != "https" and not loopback:
        raise ValueError("HTTPS is required for non-loopback environments")
    return base.rstrip("/")


def request_url(base, path):
    base = validate_base_url(base)
    if not isinstance(path, str) or not path.startswith("/") or path.startswith("//") or "\\" in path or any(c.isspace() or ord(c) < 32 for c in path):
        raise ValueError("Request paths must be absolute paths on the configured service")
    parsed = urllib.parse.urlsplit(path)
    if parsed.scheme or parsed.netloc or parsed.fragment:
        raise ValueError("Request paths cannot change origin or include a fragment")
    return base + path


def call(base, path, headers, body=None, expected=200, method=None):
    request = urllib.request.Request(request_url(base, path),
        data=None if body is None else json.dumps(body).encode(), headers={"Content-Type": "application/json", **headers},
        method=method or ("POST" if body is not None else "GET"))
    try:
        with HTTP.open(request, timeout=45) as response:
            status, content, response_headers = response.status, response.read(), response.headers
    except urllib.error.HTTPError as error:
        status, content, response_headers = error.code, error.read(), error.headers
    if status != expected:
        # Do not print arbitrary server error bodies: they may contain credentials or private provider data.
        raise AssertionError(f"{request.method} {path}: expected HTTP {expected}, got {status}")
    result = json.loads(content) if content and response_headers.get_content_type() == "application/json" else None
    return result, dict(response_headers)


def authorization(service):
    operation = str(uuid.uuid4())
    return {"operationId": operation, "consumingService": service, "endpoint": "POST /subscription-live-test",
        "meterCategory": "api.request", "requestedUnits": 1, "estimatedCostUsd": 0,
        "requestFingerprint": hashlib.sha256(operation.encode()).hexdigest()}


def finish(base, service_headers, decision, service, outcome="succeeded"):
    identity = {"operationId": decision["operationId"], "userId": decision["userId"], "consumingService": service}
    call(base, "/api/subscription/usage/start", service_headers, identity)
    receipt = {"provider": service, "model": None, "providerRequestId": decision["operationId"],
        "promptTokens": 0, "completionTokens": 0, "units": 1 if outcome == "succeeded" else 0,
        "actualCostUsd": 0, "costSource": "service-catalogue", "pricingCatalogueVersion": "live-included-request-v1"}
    settlement = {**identity, **receipt, "outcome": outcome, "estimatedCostUsd": 0, "providerReceipts": [receipt]}
    result, _ = call(base, "/api/subscription/usage/settle", service_headers, settlement)
    assert result["settled"], "Settlement was not acknowledged"
    replay, _ = call(base, "/api/subscription/usage/settle", service_headers, settlement)
    assert replay["alreadySettled"], "Settlement replay was not idempotent"
    conflict = {**settlement, "outcome": "cancelled" if outcome != "cancelled" else "failed"}
    call(base, "/api/subscription/usage/settle", service_headers, conflict, expected=409)


def run(args):
    required = ["WEB4_TEST_BASE_URL", "WEB4_TEST_BEARER", "WEB4_SECOND_TEST_BEARER"] + [f"SUBSCRIPTION_SERVICE_KEY_{s}" for s in SERVICES]
    missing = [key for key in required if not os.environ.get(key)]
    if missing:
        print("Missing required environment variables: " + ", ".join(missing), file=sys.stderr)
        return 2
    base = validate_base_url(os.environ["WEB4_TEST_BASE_URL"])
    bearer = {"Authorization": "Bearer " + os.environ["WEB4_TEST_BEARER"]}
    outcomes = []
    for service in SERVICES:
        service_headers = {"X-OASIS-Service": service, "X-OASIS-Service-Key": os.environ[f"SUBSCRIPTION_SERVICE_KEY_{service}"]}
        authenticated = {**service_headers, **bearer}
        payload = authorization(service)
        call(base, "/api/subscription/usage/authorize", service_headers, payload, expected=401)
        call(base, "/api/subscription/usage/authorize", bearer, payload, expected=401)
        first, _ = call(base, "/api/subscription/usage/authorize", authenticated, payload)
        replay, _ = call(base, "/api/subscription/usage/authorize", authenticated, payload)
        assert first["allowed"] and replay["alreadyAuthorized"] and replay["operationId"] == first["operationId"]
        call(base, "/api/subscription/usage/authorize", authenticated, {**payload, "requestFingerprint": "f" * 64}, expected=409)
        call(base, "/api/subscription/usage/authorize", {**service_headers, "Authorization": "Bearer " + os.environ["WEB4_SECOND_TEST_BEARER"]}, payload, expected=409)
        wrong_service = "WEB5" if service != "WEB5" else "WEB6"
        call(base, "/api/subscription/usage/settle", {"X-OASIS-Service": wrong_service, "X-OASIS-Service-Key": os.environ[f"SUBSCRIPTION_SERVICE_KEY_{wrong_service}"]},
             {"operationId": first["operationId"], "userId": first["userId"], "consumingService": service}, expected=401)
        finish(base, service_headers, first, service)
        for outcome in ("failed", "cancelled"):
            decision, _ = call(base, "/api/subscription/usage/authorize", authenticated, authorization(service))
            finish(base, service_headers, decision, service, outcome)
        with ThreadPoolExecutor(max_workers=args.concurrency) as pool:
            decisions = list(pool.map(lambda _: call(base, "/api/subscription/usage/authorize", authenticated, authorization(service))[0], range(args.concurrency)))
        assert len({d["operationId"] for d in decisions}) == args.concurrency
        # Settle every authorized test operation; do not leave quota exposure behind after a passing run.
        for decision in decisions:
            finish(base, service_headers, decision, service)
        outcomes.append({"service": service, "protocol": "passed", "concurrency": args.concurrency})
        print(service + ": identity, replay, conflicts, three outcomes, concurrent reservation and settlement passed")

    call(base, "/api/subscription/usage/admin/reconciliation/reports?userId=" + first["userId"] + "&month=" + datetime.now(timezone.utc).strftime("%Y-%m"), bearer, expected=403)
    if args.service_cases:
        cases = json.loads(Path(args.service_cases).read_text(encoding="utf-8"))
        represented = {case["service"] for case in cases}
        if represented != set(SERVICES):
            raise ValueError("Service case manifest must include WEB5 through WEB10 exactly as service names")
        for case in cases:
            service = case["service"]
            service_base = os.environ[case["baseUrlEnvironment"]]
            headers = {**bearer, "Idempotency-Key": str(uuid.uuid4())}
            _, response_headers = call(service_base, case["path"], headers, case.get("body"), expected=case.get("expectedStatus", 200), method=case["method"])
            op = next((v for k, v in response_headers.items() if k.lower() == "x-oasis-operation-id"), None)
            assert op, f"{service}: response lacks operation identity"
            call(service_base, case["path"], headers, case.get("body"), expected=409, method=case["method"])
            print(service + ": configured deployed endpoint and duplicate-provider-execution rejection passed")
    else:
        print("Service endpoint/provider execution matrix NOT RUN: provide --service-cases; protocol-only checks are not a deployment release gate.")
    if args.report:
        Path(args.report).write_text(json.dumps({"protocol": outcomes, "serviceEndpointsRun": bool(args.service_cases)}, indent=2), encoding="utf-8")
    return 0


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--concurrency", type=int, default=4, choices=range(2, 33))
    parser.add_argument("--service-cases", help="JSON manifest of approved test endpoint requests; see operations guide")
    parser.add_argument("--report", help="Write non-secret result JSON")
    arguments = parser.parse_args()
    try:
        sys.exit(run(arguments))
    except (AssertionError, ValueError, KeyError, urllib.error.URLError) as error:
        print(f"LIVE MATRIX FAILED: {type(error).__name__}: {error}", file=sys.stderr)
        sys.exit(1)

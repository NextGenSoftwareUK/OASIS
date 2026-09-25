#!/usr/bin/env python3
"""Generate the checked-in protocol examples without embedding any credentials."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Docs/Devs/API Documentation/WEB4 OASIS API/WEB4-Subscription-Usage.postman_collection.json"


def item(name, method, path, body=None, service=None, bearer=False, scripts=None):
    headers = [{"key": "Content-Type", "value": "application/json"}]
    if bearer:
        headers.append({"key": "Authorization", "value": "Bearer {{bearerToken}}"})
    if service:
        headers += [{"key": "X-OASIS-Service", "value": service}, {"key": "X-OASIS-Service-Key", "value": "{{serviceKey" + service + "}}"}]
    request = {"method": method, "header": headers, "url": "{{web4BaseUrl}}" + path}
    if body is not None:
        request["body"] = {"mode": "raw", "raw": json.dumps(body, indent=2), "options": {"raw": {"language": "json"}}}
    result = {"name": name, "request": request}
    if scripts:
        result["event"] = [{"listen": key, "script": {"type": "text/javascript", "exec": value}} for key, value in scripts.items()]
    return result


def collection():
    folders = []
    for number in range(5, 11):
        service = f"WEB{number}"
        identity = {"operationId": "{{operationId}}", "userId": "{{userId}}", "consumingService": service}
        authorization = {"operationId": "{{operationId}}", "consumingService": service, "endpoint": "POST /subscription-postman-test",
            "meterCategory": "api.request", "requestedUnits": 1, "estimatedCostUsd": 0, "requestFingerprint": "0" * 64}
        receipt = {"provider": service, "model": None, "providerRequestId": "{{operationId}}", "promptTokens": 0,
            "completionTokens": 0, "units": 1, "actualCostUsd": 0, "costSource": "service-catalogue", "pricingCatalogueVersion": "postman-included-request-v1"}
        settlement = {**identity, **receipt, "outcome": "succeeded", "estimatedCostUsd": 0, "providerReceipts": [receipt]}
        folders.append({"name": service + " reserve → start → settle", "item": [
            item("Authorize", "POST", "/api/subscription/usage/authorize", authorization, service, True, {
                "prerequest": ["pm.collectionVariables.set('operationId', pm.variables.replaceIn('{{$guid}}'));"],
                "test": ["pm.test('Authorized', () => pm.response.to.have.status(200));", "const decision = pm.response.json();", "pm.expect(decision.allowed).to.equal(true);", "pm.collectionVariables.set('userId', decision.userId);"]}),
            item("Replay authorization", "POST", "/api/subscription/usage/authorize", authorization, service, True, {
                "test": ["pm.test('Same reservation', () => pm.expect(pm.response.json().alreadyAuthorized).to.equal(true));"]}),
            item("Acknowledge durable execution start", "POST", "/api/subscription/usage/start", identity, service, scripts={
                "test": ["pm.test('Started', () => pm.expect(pm.response.json().started).to.equal(true));"]}),
            item("Settle included request", "POST", "/api/subscription/usage/settle", settlement, service, scripts={
                "test": ["pm.test('Settled', () => pm.expect(pm.response.json().settled).to.equal(true));"]}),
            item("Replay identical settlement", "POST", "/api/subscription/usage/settle", settlement, service, scripts={
                "test": ["pm.test('No duplicate charge', () => pm.expect(pm.response.json().alreadySettled).to.equal(true));"]})
        ]})
    folders.append({"name": "Authenticated subscriber reads", "item": [
        item("Current quotas", "GET", "/api/subscription/usage/current", bearer=True),
        item("Operation projections", "GET", "/api/subscription/usage/events?limit=100", bearer=True),
        item("Immutable audit", "GET", "/api/subscription/usage/audit?limit=100", bearer=True)
    ]})
    return {
        "info": {"name": "WEB4–WEB10 Subscription Usage Protocol", "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
            "description": "Run only with isolated staging subscribers. Configure base URL, user bearer and six separate service keys locally; never export secrets. Each folder performs included-request reserve/start/settle and replay checks. Actual provider prices/receipts and outage tests require the operations runbook. The retired authorize-request counter is not used."},
        "variable": [{"key": key, "value": ""} for key in ("web4BaseUrl", "bearerToken", "operationId", "userId", *(f"serviceKeyWEB{i}" for i in range(5, 11)))],
        "item": folders
    }


if __name__ == "__main__":
    TARGET.write_text(json.dumps(collection(), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

#!/usr/bin/env python3
"""Generate the checked-in protocol examples without embedding any credentials."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Docs/Devs/API Documentation/WEB4 OASIS API/WEB4-Subscription-Usage.postman_collection.json"


def item(name, method, path, body=None, service=None, bearer=False, scripts=None, token="bearerToken", extra_headers=None, description=None):
    headers = [{"key": "Content-Type", "value": "application/json"}]
    if bearer:
        headers.append({"key": "Authorization", "value": "Bearer {{" + token + "}}"})
    headers += extra_headers or []
    if service:
        headers += [{"key": "X-OASIS-Service", "value": service}, {"key": "X-OASIS-Service-Key", "value": "{{serviceKey" + service + "}}"}]
    request = {"method": method, "header": headers, "url": "{{web4BaseUrl}}" + path}
    if body is not None:
        request["body"] = {"mode": "raw", "raw": json.dumps(body, indent=2), "options": {"raw": {"language": "json"}}}
    if description:
        request["description"] = description
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
    folders.append({"name": "Administrator: plan grants and app accounts",
        "description": "Administrator (Wizard) JWT required: set {{adminToken}}. Every change is audited with the administrator and reason. Keys are returned once, when an app account is created or its key rotated; store them as secrets.",
        "item": [
            item("Grant a plan to an avatar (no Stripe)", "POST", "/api/subscription/admin/plan-grant",
                {"userId": "{{userId}}", "planId": "enterprise", "reason": "Internal testing account", "expiresAtUtc": None}, bearer=True, token="adminToken",
                description="planId is free, bronze, silver, gold or enterprise. 409 if the avatar has an active Stripe subscription. Granting free reverts the avatar. expiresAtUtc is optional (at most 10 years)."),
            item("List app accounts", "GET", "/api/subscription/admin/app-accounts", bearer=True, token="adminToken"),
            item("Create an app account", "POST", "/api/subscription/admin/app-accounts",
                {"appId": "{{appId}}", "name": "PIPER", "planId": "enterprise", "reason": "PIPER production app"}, bearer=True, token="adminToken",
                description="The response contains apiKey exactly once. appId is 3 to 40 lowercase letters, digits or hyphens."),
            item("Rotate an app account key", "POST", "/api/subscription/admin/app-accounts/{{appId}}/rotate-key",
                {"reason": "Scheduled key rotation"}, bearer=True, token="adminToken",
                description="Issues a new key and invalidates the old one at once."),
            item("Change an app account plan or disable it", "POST", "/api/subscription/admin/app-accounts/{{appId}}",
                {"planId": "enterprise", "status": "active", "reason": "Plan review"}, bearer=True, token="adminToken",
                description="Provide any of planId, status (active or disabled), pooledQuota, perUserPlanId. A disabled app's credentials stop working at once."),
            item("Create a POOLED app account (finite plan, shared by all users)", "POST", "/api/subscription/admin/app-accounts",
                {"appId": "demo-app", "name": "Demo app", "planId": "gold", "pooledQuota": True, "perUserPlanId": "silver", "reason": "Pooled quota pilot"}, bearer=True, token="adminToken",
                description="Needs the server setting OASIS_POOLED_APP_QUOTAS=true; otherwise a finite plan is refused (400). The plan is ONE pool shared by every user of the app; each user keeps personal limits at perUserPlanId (default silver, never enterprise) or their own plan, whichever is higher. When the pool is exhausted requests are denied with APP_-prefixed codes such as APP_MONTHLY_REQUEST_LIMIT_EXCEEDED."),
            item("Read an app pool's usage", "GET", "/api/subscription/admin/app-accounts/{{appId}}/usage", bearer=True, token="adminToken",
                description="This month's and today's pool usage against the plan limits, with the fraction used. poolActive is false when the app is not pooled or the feature flag is off."),
            item("Read quotas as a user through an app", "GET", "/api/subscription/usage/current", bearer=True,
                extra_headers=[{"key": "X-OASIS-App-Id", "value": "{{appId}}"}, {"key": "X-OASIS-App-Key", "value": "{{appKey}}"}],
                description="App credentials are optional. When present and valid, WEB4 evaluates the request against the more generous of the app's plan and the user's own plan; invalid credentials are rejected (401). Usage is still recorded against the signed-in avatar."),
        ]})
    return {
        "info": {"name": "WEB4–WEB10 Subscription Usage Protocol", "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
            "description": "Run only with isolated staging subscribers. Configure base URL, user bearer and six separate service keys locally; never export secrets. Each folder performs included-request reserve/start/settle and replay checks. Actual provider prices/receipts and outage tests require the operations runbook. The retired authorize-request counter is not used."},
        "variable": [{"key": key, "value": ""} for key in ("web4BaseUrl", "bearerToken", "operationId", "userId", *(f"serviceKeyWEB{i}" for i in range(5, 11)), "adminToken", "appId", "appKey")],
        "item": folders
    }


if __name__ == "__main__":
    TARGET.write_text(json.dumps(collection(), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

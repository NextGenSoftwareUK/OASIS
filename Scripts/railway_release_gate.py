"""Fail-closed, exact-source Railway staging/approval/production orchestration.

No credential values, request headers, response bodies or provider operations are logged.
Deployment mutations are never retried: an ambiguous response requires reconciliation.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import time
import urllib.error
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
CONFIG_PATH = ROOT / "Scripts/railway_release_gate.json"
CONFIG = json.loads(CONFIG_PATH.read_text())
FIELDS = "id status serviceId environmentId meta"


def require(condition, message):
    if not condition:
        raise ValueError(message)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def git(*args):
    return subprocess.check_output(["git", "-C", str(ROOT), *args]).decode().strip()


def api(query, variables, token):
    require(bool(token), "Missing environment-scoped Railway credential")
    request = urllib.request.Request("https://backboard.railway.com/graphql/v2",
        data=json.dumps({"query": query, "variables": variables}).encode(),
        headers={"Content-Type": "application/json", "Project-Access-Token": token})
    with urllib.request.urlopen(request, timeout=45) as response:
        data = json.load(response)
    require(not data.get("errors") and data.get("data"), "Railway operation failed; reconcile before retrying deployment")
    return data["data"]


def write(path, data):
    target = Path(path)
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(data, indent=2) + "\n")


def freeze(sha):
    require(bool(re.fullmatch(r"[0-9a-f]{40}", sha)), "Candidate must be a full lowercase commit SHA")
    subprocess.run(["git", "merge-base", "--is-ancestor", sha, "origin/master"], cwd=ROOT, check=True)
    # Read data from the candidate, never execute candidate-provided scripts.
    manifest = git("show", f"{sha}:Docker/oasis-dependency-versions.env")
    tree = git("rev-parse", f"{sha}^{{tree}}")
    output = subprocess.check_output(["gh", "api", f"repos/{CONFIG['repository']}/actions/workflows/ci-cd.yml/runs?head_sha={sha}&per_page=100"])
    runs = json.loads(output)["workflow_runs"]
    runs = [r for r in runs if r["head_sha"] == sha and r["head_branch"] == "master" and r["event"] == "push"]
    require(runs and runs[0]["status"] == "completed" and runs[0]["conclusion"] == "success",
            "Candidate lacks successful exact-master push CI")
    return {"schema": 1, "candidate": sha, "tree": tree, "manifestSha256": digest(manifest.encode()),
            "configSha256": digest(CONFIG_PATH.read_bytes()), "services": CONFIG["services"],
            "workflowRun": os.environ.get("GITHUB_RUN_ID"), "createdAt": int(time.time())}


def validate_candidate(candidate):
    require(candidate.get("schema") == 1, "Unknown candidate schema")
    require(bool(re.fullmatch(r"[0-9a-f]{40}", candidate.get("candidate", ""))), "Invalid candidate identity")
    require(candidate["services"] == CONFIG["services"], "Candidate service inventory mismatch")
    require(candidate["configSha256"] == digest(CONFIG_PATH.read_bytes()), "Deployment policy changed after freeze")
    require(candidate["tree"] == git("rev-parse", f"{candidate['candidate']}^{{tree}}"), "Candidate tree mismatch")
    manifest = git("show", f"{candidate['candidate']}:Docker/oasis-dependency-versions.env")
    require(candidate["manifestSha256"] == digest(manifest.encode()), "Dependency graph mismatch")
    require(candidate.get("workflowRun") == os.environ.get("GITHUB_RUN_ID"), "Evidence is from another workflow run")


def preflight(environment, token):
    scope = api("query { projectToken { projectId environmentId } }", {}, token)["projectToken"]
    require(scope == {"projectId": CONFIG["projectId"], "environmentId": CONFIG["environments"][environment]},
            "Railway credential is not scoped to the intended project/environment")
    for service in CONFIG["services"].values():
        variables = {"project": CONFIG["projectId"], "environment": scope["environmentId"], "service": service}
        result = api("query($project:String!,$environment:String!,$service:String!){ serviceInstanceAutoDeployStatus(projectId:$project,environmentId:$environment,serviceId:$service){enabled} }", variables, token)
        require(result["serviceInstanceAutoDeployStatus"]["enabled"] is False, "Push autodeploy bypass is enabled")
    if environment == "staging":
        expected_db = CONFIG["stagingIsolation"]["mongoDbName"]
        require(bool(expected_db), "Staging isolation is not configured/approved; deployment blocked")
        expected_user = CONFIG["stagingIsolation"]["mongoUsername"]
        require(bool(expected_user), "Staging-only database credential policy is not configured; deployment blocked")
        for service in CONFIG["services"].values():
            values = service_variables(environment, service, token)
            validate_staging_variables(values, expected_db, CONFIG["stagingIsolation"]["accountingDbName"] if service == CONFIG["services"]["4"] else None, expected_user)


def service_variables(environment, service, token):
    variables = {"project": CONFIG["projectId"], "environment": CONFIG["environments"][environment], "service": service}
    return api("query($project:String!,$environment:String!,$service:String!){variables(projectId:$project,environmentId:$environment,serviceId:$service)}", variables, token)["variables"]


def mongo_database(values):
    dna = json.loads(values.get("OASIS_DNA_JSON", "{}"))
    return dna.get("OASIS", {}).get("StorageProviders", {}).get("MongoDBOASIS", {}).get("DBName")


def mongo_username(values):
    dna = json.loads(values.get("OASIS_DNA_JSON", "{}"))
    connection = dna.get("OASIS", {}).get("StorageProviders", {}).get("MongoDBOASIS", {}).get("ConnectionString", "")
    return urllib.parse.unquote(urllib.parse.urlsplit(connection).username or "")


def validate_staging_variables(values, expected_db, expected_accounting=None, expected_user=None):
    require(bool(expected_db) and mongo_database(values) == expected_db, "Staging Mongo database does not match approved isolation policy")
    if expected_user:
        require(mongo_username(values) == expected_user, "Staging database credential does not match reviewed staging-only identity")
    if expected_accounting:
        require(values.get("SUBSCRIPTION_MONGODB_DATABASE") == expected_accounting,
                "Staging subscription accounting database does not match approved isolation policy")
        accounting_uri = values.get("SUBSCRIPTION_MONGODB_CONNECTION_STRING")
        if accounting_uri and expected_user:
            require(urllib.parse.unquote(urllib.parse.urlsplit(accounting_uri).username or "") == expected_user,
                    "Staging accounting credential does not match reviewed staging-only identity")
    dna = json.loads(values.get("OASIS_DNA_JSON", "{}"))
    embedded_stripe = dna.get("OASIS", {}).get("SubscriptionConfig", {}).get("Stripe", {})
    for stripe_key in (values.get("STRIPE_SECRET_KEY", ""), embedded_stripe.get("SecretKey", "")):
        require(not stripe_key or stripe_key.startswith(("sk_test_", "rk_test_")), "Staging Stripe credentials are not test-mode")


def stripe_endpoint(key, identity, enable=False):
    require(key.startswith(("sk_test_", "rk_test_")), "Only test-mode Stripe access is allowed")
    require(bool(re.fullmatch(r"we_[A-Za-z0-9]+", identity)), "Invalid reviewed webhook identity")
    request = urllib.request.Request(f"https://api.stripe.com/v1/webhook_endpoints/{identity}",
        data=urllib.parse.urlencode({"disabled": "false"}).encode() if enable else None,
        headers={"Authorization": f"Bearer {key}", "Content-Type": "application/x-www-form-urlencoded"})
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def activate_staging_webhook(report, token):
    require(report.get("environment") == "staging" and report.get("accepted") is True and len(report.get("checks", [])) == 10,
            "Webhook activation requires complete staging acceptance")
    return checked_staging_webhook(token, activate=True)


def checked_staging_webhook(token, activate=False):
    values = service_variables("staging", CONFIG["services"]["4"], token)
    isolation = CONFIG["stagingIsolation"]
    require(bool(isolation["mongoUsername"]), "Reviewed staging database identity is required before webhook activation")
    validate_staging_variables(values, isolation["mongoDbName"], isolation["accountingDbName"], isolation["mongoUsername"])
    expected = CONFIG["stagingStripeWebhook"]
    key = values.get("STRIPE_SECRET_KEY", "")
    endpoint = stripe_endpoint(key, expected["id"])
    require(endpoint.get("id") == expected["id"] and endpoint.get("url") == expected["url"] and
            endpoint.get("livemode") is False and endpoint.get("api_version") == expected["apiVersion"],
            "Stripe webhook differs from reviewed staging test endpoint")
    result = stripe_endpoint(key, expected["id"], enable=True) if activate else endpoint
    require(result.get("status") == "enabled" and result.get("livemode") is False and
            result.get("id") == expected["id"] and result.get("url") == expected["url"] and
            result.get("api_version") == expected["apiVersion"], "Staging test webhook activation failed")
    return {"id": expected["id"], "livemode": False, "status": "enabled"}


def deployment(identity, token):
    item = api(f"query($id:String!){{deployment(id:$id){{{FIELDS}}}}}", {"id": identity}, token)["deployment"]
    # Railway metadata can contain deployment configuration. Evidence retains only
    # identity/digest, never arbitrary configuration or registry credential fields.
    meta = item.get("meta") or {}
    item["meta"] = {key: meta.get(key) for key in ("commitHash", "imageDigest")}
    return item


def validate_deployment(item, candidate, environment, service):
    require(item["serviceId"] == service and item["environmentId"] == CONFIG["environments"][environment],
            "Deployment belongs to a different service/environment")
    require((item.get("meta") or {}).get("commitHash") == candidate["candidate"], "Deployment source SHA mismatch")
    require(item["status"] in ("SUCCESS", "SLEEPING"), "Deployment has not reached successful terminal state")


def assert_current(item, token):
    variables = {"environment": item["environmentId"], "service": item["serviceId"]}
    result = api("query($environment:String!,$service:String!){serviceInstance(environmentId:$environment,serviceId:$service){latestDeployment{id} activeDeployments{id}}}", variables, token)["serviceInstance"]
    require(result["latestDeployment"]["id"] == item["id"], "Candidate was superseded by another deployment")
    require(item["id"] in [d["id"] for d in result["activeDeployments"]], "Candidate is not active")


def deploy(candidate, environment, token, report_path):
    preflight(environment, token)
    report = {"candidate": candidate, "environment": environment, "deployments": {}}
    write(report_path, report)
    # Record each returned deployment ID before starting another service.
    for number, service in CONFIG["services"].items():
        variables = {"environment": CONFIG["environments"][environment], "service": service, "sha": candidate["candidate"]}
        result = api("mutation($environment:String!,$service:String!,$sha:String!){serviceInstanceDeployV2(environmentId:$environment,serviceId:$service,commitSha:$sha)}", variables, token)
        report["deployments"][number] = result["serviceInstanceDeployV2"]
        write(report_path, report)
    deadline = time.monotonic() + 45 * 60
    while time.monotonic() < deadline:
        states = {n: deployment(identity, token) for n, identity in report["deployments"].items()}
        require(all(d["status"] not in ("FAILED", "CRASHED", "REMOVED", "SKIPPED") for d in states.values()),
                "Deployment failed; stop and inspect recorded IDs, do not automatically roll back data")
        print(json.dumps({"environment": environment, "states": {n: d["status"] for n, d in states.items()}}), flush=True)
        if all(d["status"] in ("SUCCESS", "SLEEPING") for d in states.values()):
            for number, item in states.items():
                validate_deployment(item, candidate, environment, CONFIG["services"][number])
                assert_current(item, token)
            report["verifiedDeployments"] = states
            write(report_path, report)
            return report
        # Bounded polling of the documented asynchronous deployment lifecycle.
        time.sleep(30)
    raise TimeoutError("Deployment deadline exceeded; reconcile IDs before any new mutation")


def probe_json(url):
    request = urllib.request.Request(url, headers={"User-Agent": "OASIS-release-acceptance/1"})
    with urllib.request.urlopen(request, timeout=45) as response:
        require(response.status == 200 and response.url.startswith("https://"), "Acceptance requires valid HTTPS200")
        require(urllib.parse.urlsplit(response.url).netloc == urllib.parse.urlsplit(url).netloc, "Unexpected redirect host")
        data = json.load(response)
    require(isinstance(data, dict) and data, "Acceptance response is not a nonempty JSON object")
    if "/swagger/" in url:
        require(data.get("openapi") and data.get("paths"), "Invalid OpenAPI contract")
    return {"url": url, "status": 200, "jsonObject": True}


def acceptance(report, token):
    candidate, environment = report["candidate"], report["environment"]
    require(set(report["deployments"]) == set(CONFIG["services"]), "Acceptance service coverage mismatch")
    checks = []
    for number, identity in report["deployments"].items():
        item = deployment(identity, token)
        validate_deployment(item, candidate, environment, CONFIG["services"][number])
        assert_current(item, token)
        prefix = "staging.api" if environment == "staging" else "api"
        host = "api.starnet.oasisomniverse.one" if environment == "production" and number == "5" else f"{prefix}.web{number}.oasisomniverse.one"
        version = "2" if number == "6" else "1"
        checks.append(probe_json(f"https://{host}/swagger/v{version}/swagger.json"))
        if number == "6":
            for path in ("/v1/health", "/.well-known/mcp.json", "/.well-known/agent.json"):
                checks.append(probe_json(f"https://{host}{path}"))
    require(len(checks) == 10, "Acceptance endpoint coverage mismatch")
    report.update({"checks": checks, "acceptedAt": int(time.time()), "accepted": True})
    return report


def validate_staging_evidence(report, candidate, now=None):
    require(report.get("environment") == "staging" and report.get("candidate") == candidate, "Staging candidate mismatch")
    require(report.get("accepted") is True and len(report.get("checks", [])) == 10, "Missing complete staging acceptance")
    require(set(report.get("deployments", {})) == set(CONFIG["services"]), "Missing staged services")
    require(report.get("stripeWebhook") == {"id": CONFIG["stagingStripeWebhook"]["id"], "livemode": False, "status": "enabled"},
            "Missing accepted staging test webhook activation")
    require(0 <= (now or int(time.time())) - report.get("acceptedAt", 0) <= 6 * 3600, "Staging evidence expired; run a fresh candidate")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("operation", choices=["freeze", "staging", "production"])
    parser.add_argument("--candidate")
    parser.add_argument("--evidence")
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    if args.operation == "freeze":
        write(args.output, freeze(args.candidate))
        return
    candidate = json.loads(Path(args.candidate).read_text())
    validate_candidate(candidate)
    token = os.environ.get("RAILWAY_TOKEN")
    if args.operation == "production":
        report = json.loads(Path(args.evidence).read_text())
        validate_staging_evidence(report, candidate)
        stage_token = os.environ.get("RAILWAY_STAGING_TOKEN")
        preflight("staging", stage_token)
        # Confirm isolated database selection against the actual production configuration.
        # Values remain in memory and are never included in evidence/logs.
        for service in CONFIG["services"].values():
            stage_variables = service_variables("staging", service, stage_token)
            prod_variables = service_variables("production", service, token)
            stage_db = mongo_database(stage_variables)
            prod_db = mongo_database(prod_variables)
            require(stage_db and prod_db and stage_db != prod_db, "Staging and production database names are not isolated")
            require(mongo_username(stage_variables) and mongo_username(stage_variables) != mongo_username(prod_variables),
                    "Staging and production must not share a database credential identity")
            if service == CONFIG["services"]["4"]:
                require(stage_variables.get("SUBSCRIPTION_MONGODB_DATABASE") != prod_variables.get("SUBSCRIPTION_MONGODB_DATABASE", "oasis_subscription_accounting"),
                        "Staging and production accounting databases are not isolated")
        # Recheck staged identity and contracts after human approval, before any production write.
        acceptance(report, stage_token)
        require(checked_staging_webhook(stage_token) == report["stripeWebhook"], "Staging test webhook drifted after acceptance")
    report = acceptance(deploy(candidate, args.operation, token, args.output), token)
    if args.operation == "staging":
        report["stripeWebhook"] = activate_staging_webhook(report, token)
    write(args.output, report)


if __name__ == "__main__":
    main()

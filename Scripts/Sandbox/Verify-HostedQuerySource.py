"""Explicit normal-visitor HTTP acceptance. No uncertain mutation is retried.

Creates at most two journalled demo sessions for this provider cohort, protects
their bearers with existing Windows DPAPI, and reuses acknowledged sessions.
"""
from pathlib import Path
import argparse
import base64
import datetime
import hashlib
import importlib.util
import json
import os
import re
import subprocess
import urllib.error
import urllib.request
import uuid

import HostedSessionCredentials as session_credentials

ROOT = Path(__file__).resolve().parents[2]
BASE = "https://dev.api.web4.oasisomniverse.one/api/"
BROWSER_ORIGIN = "https://web4.oasisomniverse.one"
REVIEWED_EXACT_KEY_PROVIDERS = (
    "MeilisearchOASIS", "SolrOASIS", "ElasticsearchOASIS", "OpenSearchOASIS",
    "ArcadeDBOASIS", "SurrealDBOASIS", "QdrantOASIS", "RedisOASIS", "ValKeyOASIS",
    "DynamoDBOASIS", "TypesenseOASIS", "MinIOOASIS", "PocketBaseOASIS", "SupabaseOASIS", "FirebaseOASIS",
    "WeaviateOASIS", "TigrisOASIS", "GarnetOASIS", "KeyDBOASIS", "DragonflyOASIS", "UpstashOASIS", "InfluxDBOASIS", "CouchbaseOASIS", "ArangoDBOASIS", "CockroachDBOASIS", "TimescaleDBOASIS", "CassandraOASIS", "RavenDBOASIS",
)
spec = importlib.util.spec_from_file_location("verification", ROOT / "Scripts/Sandbox/Verify-HostedCqlDemo.py")
verification = importlib.util.module_from_spec(spec)
spec.loader.exec_module(verification)


def write_new(path, data):
    with path.open("xb") as stream:
        stream.write(data)
        stream.flush()
        os.fsync(stream.fileno())


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, url):
        return None


def capture_query_response(path, route, operation, status, raw):
    """Preserve query outcomes before JSON/error interpretation; never auth bodies."""
    path = Path(path).resolve()
    if route != "data/query-cql" or not path.is_relative_to((ROOT / "TestResults").resolve()):
        raise ValueError("Query-only evidence must stay in this worktree's TestResults.")
    write_new(path, (json.dumps({"route": route, "operationId": operation, "status": status,
        "bodyBase64": base64.b64encode(raw).decode(), "bodyTruncated": len(raw) > 2 * 1024 * 1024}, indent=2) + "\n").encode())


def call(route, token=None, body=None, method="POST", operation=None, *, cookie_header=None, capture_auth=None, evidence_path=None):
    if evidence_path is not None and route != "data/query-cql":
        raise ValueError("Only query response evidence may be captured here.")
    if evidence_path is not None:
        target = Path(evidence_path).resolve()
        if not target.is_relative_to((ROOT / "TestResults").resolve()):
            raise ValueError("Query evidence must stay in this worktree's TestResults.")
        if target.exists():
            raise FileExistsError("Original query outcome exists; reconcile before another dispatch.")
    headers = {"Accept": "application/json", "Origin": BROWSER_ORIGIN, "Idempotency-Key": operation or str(uuid.uuid4())}
    if token:
        headers["Authorization"] = "Bearer " + token
    if cookie_header:
        headers["Cookie"] = cookie_header
    if body is not None:
        headers["Content-Type"] = "application/json"
    request = urllib.request.Request(BASE + route, method=method, headers=headers,
                                    data=json.dumps(body).encode() if body is not None else None)
    try:
        response = urllib.request.build_opener(NoRedirect()).open(request, timeout=45)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        raw = response.read(2 * 1024 * 1024 + 1)
        if capture_auth is not None:
            capture_auth(response.status, raw, response.headers.get_all("Set-Cookie") or [])
        if evidence_path is not None:
            capture_query_response(evidence_path, route, headers["Idempotency-Key"], response.status, raw)
        if len(raw) > 2 * 1024 * 1024:
            raise RuntimeError("HTTP proof exceeds its response budget.")
        if (response.headers.get("Access-Control-Allow-Origin") != BROWSER_ORIGIN
                or response.headers.get("Access-Control-Allow-Credentials") != "true"):
            raise RuntimeError("The exact hosted browser credential origin was not acknowledged.")
        if not raw:
            raise RuntimeError("HTTP acceptance failed with an empty response: " + str(response.status))
        data = json.loads(raw)
        if isinstance(data, dict):
            data = verification.unwrap(data)
        if response.status != 200 or isinstance(data, dict) and data.get("isError"):
            code = data.get("errorCode") or data.get("code") if isinstance(data, dict) else None
            raise RuntimeError("HTTP acceptance failed: " + str(response.status) + "/" + str(code))
        return data


def session(state, label, *, allow_create=True):
    ack = state / (label + ".acknowledged.json")
    token_file = state / (label + ".dpapi")
    intent = state / (label + ".intent.json")
    session_file = state / (label + ".session.dpapi")
    if ack.exists():
        acknowledged = json.loads(ack.read_text())
        if acknowledged.get("credentialsSchema") == 1:
            session_credentials.no_pending_rotation(state, label)
            credential = session_credentials.validate(json.loads(verification.protected_token(
                session_file.read_bytes(), decrypt=True)), acknowledged["id"])
            if session_credentials.needs_renewal(credential):
                credential = session_credentials.rotate(state, label, credential, call, verification.protected_token, write_new)
            return acknowledged, credential["jwtToken"]
        # Retained unversioned cohorts own only their original JWT. Never guess a
        # refresh format or create a replacement identity for an expired legacy fixture.
        token = verification.protected_token(token_file.read_bytes(), decrypt=True).decode()
        claims = session_credentials.claims(token)
        if claims["exp"] <= datetime.datetime.now(datetime.timezone.utc).timestamp():
            raise RuntimeError("Acknowledged legacy session expired without retained cookie credentials; reconcile the same cohort explicitly.")
        return acknowledged, token
    if not allow_create:
        raise RuntimeError("Selected read-only cohort has no acknowledged session; no replacement created.")
    if intent.exists() or token_file.exists() or session_file.exists():
        raise RuntimeError("Unacknowledged session creation; reconcile, never create another avatar.")
    operation = str(uuid.uuid4())
    write_new(intent, json.dumps({"route": "sandbox/session", "operationId": operation, "label": label}).encode())
    captured = []
    def capture(status, raw, cookies):
        write_new(state / (label + ".auth-response.dpapi"), verification.protected_token(json.dumps({
            "status": status, "bodyBase64": base64.b64encode(raw).decode(), "cookies": cookies}).encode()))
        captured.append((status, cookies))
    requested_at = session_credentials.time.time()
    result = call("sandbox/session", operation=operation, capture_auth=capture)
    identity = verification.acknowledged_identity("sandbox/session", result)
    avatar = result["result"]
    if len(captured) != 1 or captured[0][0] != 200:
        raise RuntimeError("Session cookie issuance outcome is incomplete; no replacement avatar.")
    credential = session_credentials.bundle(identity, avatar["jwtToken"], captured[0][1], requested_at=requested_at)
    write_new(session_file, verification.protected_token(json.dumps(credential).encode()))
    recorded = {"id": identity, "username": avatar["username"], "label": label, "operationId": operation, "credentialsSchema": 1}
    write_new(ack, json.dumps(recorded).encode())
    return recorded, avatar["jwtToken"]


def ready_source(provider, token, require_pair):
    discovery = call("data/query-cql/providers?examplesVersion=2", token, method="GET")
    matches = [row for row in discovery["result"] if row["provider"] == provider]
    if len(matches) > 1:
        raise RuntimeError(f"{provider} appeared more than once in hosted discovery.")
    source = matches[0] if matches else None
    if source is None or source.get("available") is not True or (require_pair and source.get("federationAvailable") is not True):
        raise RuntimeError(f"{provider} is absent or unavailable in hosted discovery.")
    required = {"public-records", "public-subset"}
    if require_pair:
        required.update(("mongo-public-records", "mongo-public-subset"))
    examples = source["examples"]
    if len({row["id"] for row in examples}) != len(examples):
        raise RuntimeError(f"{provider} returned duplicate hosted preset IDs.")
    if any("available" in row and type(row["available"]) is not bool for row in examples):
        raise RuntimeError(f"{provider} returned invalid hosted preset availability.")
    available = {row["id"] for row in examples if row.get("available", True)}
    if not required.issubset(available):
        raise RuntimeError(f"{provider} is missing required hosted query presets.")
    return source

def require_native_rows(response, example, count):
    rows = response["result"]["rows"]
    if len(rows) != count:
        raise RuntimeError("Native source selection count differs.")
    filters = example["request"].get("filters", [])
    if len(filters) == 1:
        predicate = filters[0]
        if (predicate.get("field"), predicate.get("scalar"), predicate.get("operator")) == ("Id", "Guid", "In"):
            expected = {value.lower() for value in predicate["value"]}
            actual = {row.get("id", "").lower() for row in rows}
            if len(expected) != count or actual != expected:
                raise RuntimeError("Native source selection IDs differ from the reviewed preset.")
        elif (example["request"].get("includeMetadata") is True
              and len(predicate.get("metadataPath") or []) == 1
              and (predicate.get("scalar"), predicate.get("operator")) == ("String", "In")):
            key = predicate["metadataPath"][0]
            expected = set(predicate["value"])
            actual = {(row.get("metadata") or {}).get(key) for row in rows}
            if len(expected) != count or actual != expected:
                raise RuntimeError("Native source metadata keys differ from the reviewed preset.")


def normalized_preset_request(request, paired):
    # Discovery serializes the typed request and materializes its defaults.
    selection_defaults = {
        "provider": None, "filters": [], "anyOf": [], "anyChildren": [], "anyDescendants": [],
        "orderBy": "Name", "descending": False, "skip": 0, "take": 25,
        "maximumDurationMilliseconds": 5000, "includeMetadata": False, "graph": None,
    }

    def selection(value):
        normalized = selection_defaults | value
        normalized["filters"] = [{"metadataPath": None} | row for row in normalized["filters"]]
        return normalized

    if not paired:
        return selection(request)
    federation_defaults = {
        "scope": "CurrentDefault", "providers": [], "identityMode": "PreserveCopies", "providerPriority": [],
        "skip": 0, "take": 25, "maximumInputRows": 300, "maximumRowsPerProvider": 100,
        "maximumInputBytes": 4194304, "maximumOutputBytes": 1048576, "maximumWork": 100000,
        "maximumProviderAttempts": 3, "maximumDurationMilliseconds": 5000,
        "enableContinuation": False, "cursor": None,
    }
    return federation_defaults | request | {"source": selection(request.get("source", {}))}


def require_reviewed_presets(provider, source, review):
    expected = {row["id"]: row for row in review["presets"]}
    actual = {row["id"]: row for row in source["examples"]}
    if len(actual) != len(source["examples"]) or len(expected) != len(review["presets"]):
        raise RuntimeError(f"Hosted {provider} preset IDs must be unique.")
    if provider in REVIEWED_EXACT_KEY_PROVIDERS and actual.keys() != expected.keys():
        raise RuntimeError(f"Hosted {provider} preset set differs from the reviewed rollout.")
    for key in ("public-records", "public-subset", "mongo-public-records", "mongo-public-subset"):
        observed = actual.get(key, {}).get("request", {})
        reviewed = expected.get(key, {}).get("request", {})
        paired = key.startswith("mongo-")
        observed_selection = observed.get("source", {}) if paired else observed
        reviewed_selection = reviewed.get("source", {}) if paired else reviewed
        filters = observed_selection.get("filters", [])
        reviewed_filters = reviewed_selection.get("filters", [])
        if (actual.get(key, {}).get("operation", "Rows") != expected.get(key, {}).get("operation", "Rows")
                or ((provider == "LocalFile" or provider in REVIEWED_EXACT_KEY_PROVIDERS)
                    and json.dumps(normalized_preset_request(observed, paired), sort_keys=True)
                    != json.dumps(normalized_preset_request(reviewed, paired), sort_keys=True))
                or (paired and any(observed.get(field) != reviewed.get(field)
                                   for field in ("scope", "identityMode")))
                or len(filters) != 1 or len(reviewed_filters) != 1
                or any(filters[0].get(field) != reviewed_filters[0].get(field)
                       for field in ("field", "scalar", "operator", "value", "metadataPath"))
                or observed.get("providers" if paired else "provider")
                != reviewed.get("providers" if paired else "provider")):
            raise RuntimeError(f"Hosted {provider} preset {key} differs from the reviewed rollout.")


def localfile_review(*, require_mount=False):
    path = ROOT / "TestResults/HostedLocalFileSourceCurrent/prepared-review.json"
    if not path.is_file():
        raise RuntimeError("Prepare and review the LocalFile rollout before verifying its advertised catalogue.")
    review = json.loads(path.read_bytes())
    if require_mount:
        publisher_path = Path(__file__).with_name("Publish-HostedLocalFileConfiguration.py")
        publisher_spec = importlib.util.spec_from_file_location("localfile_publisher", publisher_path)
        publisher = importlib.util.module_from_spec(publisher_spec)
        publisher_spec.loader.exec_module(publisher)
        review["mountedReceipt"] = publisher.require_mounted_record_receipt(review["source"])
    return review


def localfile_deployment_visitor():
    review = localfile_review(require_mount=True)
    return deployment_visitor("LocalFile", review["mountedReceipt"]["deployment"]), review


def deployment_visitor(provider, deployment):
    if str(uuid.UUID(deployment)) != deployment:
        raise RuntimeError(f"Canonical {provider} settings deployment ID required.")
    return ROOT / "LocalState/COSMIC" / f"Hosted{provider}Readiness-{deployment}"


def acknowledge_localfile_http(state, owner, foreign, reviewed_mount, evidence):
    if owner == foreign:
        raise RuntimeError("LocalFile privacy proof requires two distinct visitor identities.")
    if localfile_review(require_mount=True)["mountedReceipt"] != reviewed_mount:
        raise RuntimeError("The reviewed LocalFile deployment changed during acceptance.")
    receipt = {"schema": 1, "provider": "LocalFileOASIS", "ownerId": owner, "foreignId": foreign,
               "deployment": reviewed_mount["deployment"], "imageDigest": reviewed_mount["imageDigest"],
               "nativeCounts": [3, 2], "federationCounts": [5, 4], "browserAcceptance": False,
               "evidenceSha256": evidence}
    write_new(state / "cql-http-acceptance.acknowledged.json", json.dumps(receipt, sort_keys=True).encode())


def recover_localfile_http(state, native, paired, isolation):
    if not all(path.is_file() for path in (native, paired, isolation)):
        raise RuntimeError("All three original LocalFile HTTP reports are required for receipt recovery.")
    isolation_bytes = isolation.read_bytes()
    proof = json.loads(isolation_bytes)
    binding = proof.get("localFileAcceptance")
    if not isinstance(binding, dict) or set(binding) != {"ownerId", "mount", "priorEvidenceSha256"}:
        raise RuntimeError("Original LocalFile isolation report has no deployment-bound acceptance evidence.")
    prior = {path.name: hashlib.sha256(path.read_bytes()).hexdigest() for path in (native, paired)}
    if prior != binding["priorEvidenceSha256"]:
        raise RuntimeError("Original LocalFile native or paired report differs from the isolation evidence.")
    owner = json.loads((state / "owner.acknowledged.json").read_bytes())["id"]
    foreign = json.loads((state / "foreign.acknowledged.json").read_bytes())["id"]
    if owner != binding["ownerId"] or foreign != proof.get("avatar", {}).get("id"):
        raise RuntimeError("Original LocalFile visitor acknowledgements differ from the isolation evidence.")
    evidence = prior | {isolation.name: hashlib.sha256(isolation_bytes).hexdigest()}
    acknowledge_localfile_http(state, owner, foreign, binding["mount"], evidence)


def pouchdb_review(path, *, require_deployment=False):
    if path is None or not path.is_file() or not 0 < path.stat().st_size <= 256 * 1024:
        raise RuntimeError("An explicit reviewed PouchDB settings patch is required.")
    settings = json.loads(path.read_bytes())
    proof_path = ROOT / "LocalState/COSMIC/RecoveryLaptopPouchDBCql/hosted-preparation/native-source-qualified.json"
    proof = json.loads(proof_path.read_bytes())
    digest = proof["sourceBundleSha256"].lower()
    bundle = settings["OASIS_SANDBOX_POUCHDB_BOOTSTRAP_JSON"].encode()
    if (settings["OASIS_SANDBOX_POUCHDB"] != "true" or settings["CQL_POUCHDB_QUERY_SOURCE_DIGEST"] != digest
            or hashlib.sha256(bundle).hexdigest() != digest):
        raise RuntimeError("Reviewed PouchDB settings differ from the qualified source.")
    ids = [row["id"] for row in json.loads(bundle)["records"]]
    presets = json.loads(settings["CQL_HOSTED_QUERY_EXAMPLES__PouchDBOASIS"])
    expected = {}
    for suffix, count in (("records", 3), ("subset", 2)):
        selection = {"filters": [{"field": "Id", "scalar": "Guid", "operator": "In", "value": ids[:count]}]}
        expected["public-" + suffix] = {"id": "public-" + suffix, "request": dict(selection, provider="PouchDBOASIS")}
        key = "mongo-public-" + suffix
        expected[key] = {"id": key, "operation": "Federation", "request": {
            "scope": "Named", "identityMode": "PreserveCopies", "providers": ["MongoDBOASIS", "PouchDBOASIS"],
            "source": selection}}
    if len(presets) != len(expected):
        raise RuntimeError("Four reviewed PouchDB presets are required.")
    require_reviewed_presets("PouchDBOASIS", {"examples": presets}, {"presets": list(expected.values())})
    review = {"presets": presets}
    if require_deployment:
        publisher_path = Path(__file__).with_name("Publish-HostedPouchDbConfiguration.py")
        publisher_spec = importlib.util.spec_from_file_location("pouchdb_publisher", publisher_path)
        publisher = importlib.util.module_from_spec(publisher_spec)
        publisher_spec.loader.exec_module(publisher)
        review["deployment"] = publisher.verify_settings(settings)
    return review


def reviewed_exact_key_source(provider, path):
    if path is None or not path.is_file() or not 0 < path.stat().st_size <= 16384:
        raise RuntimeError("An explicit reviewed native teaching catalogue is required.")
    catalogue = json.loads(path.read_bytes())
    try:
        fields = {"Actor", "LiveIds", "DeletedId", "MissingId"}
        if provider in ("MinIOOASIS", "TigrisOASIS"):
            fields |= {"Endpoint", "Bucket", "Originals"}
        if provider == "MinIOOASIS":
            fields.add("Region")
        if provider == "CassandraOASIS":
            fields.add("BundleSha256")
            if not re.fullmatch(r"[0-9a-f]{64}", catalogue["BundleSha256"]):
                raise ValueError("Unexpected Cassandra secure connection bundle digest.")
        if provider == "RavenDBOASIS":
            fields |= {"Origin", "CertificateSha256"}
            origin = catalogue["Origin"]
            if (len(origin) > 261
                    or not re.fullmatch(r"https://(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+ravendb\.cloud", origin)
                    or not re.fullmatch(r"[0-9a-f]{64}", catalogue["CertificateSha256"])):
                raise ValueError("Unexpected RavenDB Cloud origin or certificate digest.")
        if provider in ("CockroachDBOASIS", "TimescaleDBOASIS"):
            fields.add("Host")
            host = catalogue["Host"]
            domains = (".cockroachlabs.cloud",) if provider == "CockroachDBOASIS" else (
                ".tsdb.cloud.timescale.com", ".tsdb.cloud.tigerdata.com")
            if (not isinstance(host, str) or len(host) > 253 or not host.endswith(domains)
                    or any(not re.fullmatch(r"[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?", label)
                           for label in host.split("."))):
                raise ValueError("Unexpected managed database host.")
            if provider == "TimescaleDBOASIS":
                fields.add("Port")
                port = catalogue["Port"]
                if type(port) is not int or not 1 <= port <= 65535:
                    raise ValueError("Unexpected managed database port.")
        if provider == "TigrisOASIS" and (
                catalogue["Endpoint"] not in ("https://t3.storage.dev", "https://fly.storage.tigris.dev")
                or not re.fullmatch(r"[a-z0-9][a-z0-9-]{1,61}[a-z0-9]", catalogue["Bucket"])):
            raise ValueError("Unexpected Tigris source address.")
        if (set(catalogue) != fields
                or not isinstance(catalogue["LiveIds"], list) or len(catalogue["LiveIds"]) != 3):
            raise ValueError("Unexpected teaching catalogue shape.")
        ids = catalogue["LiveIds"]
        record_ids = [*ids, catalogue["DeletedId"], catalogue["MissingId"]]
        if any(str(uuid.UUID(value)) != value or uuid.UUID(value).int == 0
               for value in [catalogue["Actor"], *record_ids]) or len(set(record_ids)) != 5:
            raise ValueError("Invalid teaching identities.")
        if provider in ("MinIOOASIS", "TigrisOASIS"):
            originals = catalogue["Originals"]
            if (not isinstance(originals, list) or len(originals) != 4
                    or any(set(row) != {"Id", "Sha256", "ETag"}
                           or not re.fullmatch(r"[0-9a-fA-F]{64}", row["Sha256"])
                           or not isinstance(row["ETag"], str) or not row["ETag"].strip()
                           for row in originals)
                    or {row["Id"] for row in originals} != set(record_ids[:-1])):
                raise ValueError("Object-store descriptor requires exact original object receipts.")
    except (AttributeError, KeyError, TypeError, ValueError) as error:
        raise RuntimeError("The reviewed teaching catalogue requires canonical, distinct record identities"
                           " and any provider-specific original receipts.") from error
    presets = []
    for suffix, selected in (("records", ids), ("subset", ids[:2])):
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True,
                     "filters": [{"field": "Id", "scalar": "Guid", "operator": "In", "value": selected}]}
        presets.extend([
            {"id": "public-" + suffix, "label": f"{provider} public teaching records ({len(selected)})",
             "request": dict(selection, provider=provider, maximumDurationMilliseconds=30000)},
            {"id": "mongo-public-" + suffix, "label": f"MongoDB + {provider} ({len(selected)} source records)",
             "operation": "Federation", "request": {
                "scope": "Named", "providers": ["MongoDBOASIS", provider], "source": selection,
                "identityMode": "PreserveCopies", "maximumProviderAttempts": 2,
                "maximumDurationMilliseconds": 30000, "take": 25}},
        ])
    review = {"presets": presets}
    if provider == "TigrisOASIS":
        review["sourceBinding"] = {"CQL_TIGRIS_QUERY_ORIGIN": catalogue["Endpoint"],
                                   "CQL_TIGRIS_QUERY_BUCKET": catalogue["Bucket"],
                                   "CQL_TIGRIS_QUERY_PROBE_ID": ids[0]}
    elif provider == "CockroachDBOASIS":
        review["sourceBinding"] = {"CQL_COCKROACHDB_QUERY_HOST": catalogue["Host"],
                                   "CQL_COCKROACHDB_QUERY_PROBE_ID": ids[0]}
    elif provider == "TimescaleDBOASIS":
        review["sourceBinding"] = {"CQL_TIMESCALEDB_QUERY_HOST": catalogue["Host"],
                                   "CQL_TIMESCALEDB_QUERY_PORT": str(catalogue["Port"]),
                                   "CQL_TIMESCALEDB_QUERY_PROBE_ID": ids[0]}
    elif provider == "CassandraOASIS":
        review["sourceBinding"] = {"CQL_CASSANDRA_QUERY_BUNDLE_SHA256": catalogue["BundleSha256"],
                                   "CQL_CASSANDRA_QUERY_PROBE_ID": ids[0]}
    elif provider == "RavenDBOASIS":
        review["sourceBinding"] = {"CQL_RAVENDB_QUERY_ORIGIN": catalogue["Origin"],
                                   "CQL_RAVENDB_QUERY_CERTIFICATE_SHA256": catalogue["CertificateSha256"],
                                   "CQL_RAVENDB_QUERY_PROBE_ID": ids[0]}
    elif provider in ("WeaviateOASIS", "GarnetOASIS", "KeyDBOASIS", "DragonflyOASIS", "UpstashOASIS", "InfluxDBOASIS", "CouchbaseOASIS", "ArangoDBOASIS"):
        product = provider.removesuffix("OASIS").upper()
        review["sourceBinding"] = {f"CQL_{product}_QUERY_PROBE_ID": ids[0]}
    return review


def require_native_selection(responses, examples):
    if len(responses) != 2 or len(examples) != 2:
        raise RuntimeError("Two native source selections are required.")
    for response, example, count in zip(responses, examples, (3, 2)):
        require_native_rows(response, example, count)


def verify_private_visibility(token, ids, providers):
    if len(ids) != 2 or len(set(ids)) != 2:
        raise RuntimeError("Visibility verification requires two distinct acknowledged snapshot IDs.")
    observations = {}
    for provider in providers:
        response = call("data/query-cql", token, {"provider": provider, "orderBy": "Id", "take": 2,
                        "filters": [{"field": "Id", "scalar": "Guid", "operator": "In", "value": ids}]})
        if response["result"]["rows"]:
            raise RuntimeError(f"Independent visitor can see private snapshot IDs on {provider}.")
        observations[provider] = response
    return observations


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("provider", choices=["ThreeFoldOASIS", "ClickHouseOASIS", "QuestDBOASIS", "SQLServerDBOASIS", "DuckDBOASIS", "CouchDBOASIS", "PouchDBOASIS", "ChromaOASIS", "CloudflareD1OASIS", "NATSJetStreamOASIS", "ZcashOASIS", "BittensorOASIS", "AkashOASIS", "EclipseOASIS", "ENSOASIS", "BlockscoutOASIS", "CelestiaOASIS", "ChainflipOASIS", "LocalFileOASIS", "HyperlaneOASIS", "LayerZeroV2OASIS", "StargateOASIS", "deBridgeOASIS", "SynapseOASIS", "ConnextOASIS", "WormholeOASIS", "BlockStackOASIS", "MatrixOASIS", "NostrOASIS", "BlueSkyOASIS", "ChainLinkOASIS", "EOSIOOASIS", "TelosOASIS", "FilecoinOASIS", "FantomOASIS", "AzureStorageOASIS", "SOLIDOASIS", "AWSS3OASIS", "SolanaOASIS", "GoogleCloudOASIS", "EthereumOASIS", "PolygonOASIS", "ArbitrumOASIS", "CosmosBlockChainOASIS", "PolkadotOASIS", "NEAROASIS", "BaseOASIS", "AvalancheOASIS", "BNBChainOASIS", "OptimismOASIS", "LineaOASIS", "ScrollOASIS", "ZkSyncOASIS", "AbstractOASIS", "MonadOASIS", "SeiOASIS", "BerachainOASIS", "BitcoinOASIS", "XRPLOASIS", "StellarOASIS", "AlgorandOASIS", "AptosOASIS", "HashgraphOASIS", "SuiOASIS", "ElrondOASIS", "RadixOASIS", "StarknetOASIS", "CardanoOASIS", "ArweaveOASIS", "TONOASIS", *REVIEWED_EXACT_KEY_PROVIDERS])
    parser.add_argument("--prepare", action="store_true")
    parser.add_argument("--readiness", action="store_true", help="Check discovery using the retained rehearsal visitor; create no cohort.")
    parser.add_argument("--localfile-deployment-visitor", action="store_true",
                        help="Use a separate visitor for the verified LocalFile settings deployment.")
    parser.add_argument("--pouchdb-deployment-visitor", action="store_true",
                        help="Use a separate visitor for the verified PouchDB settings deployment.")
    parser.add_argument("--recover-localfile-receipt", action="store_true",
                        help="Finalize the original completed LocalFile reports without sessions, queries or saves.")
    parser.add_argument("--reviewed-exact-key-source", type=Path, help="Reviewed native teaching catalogue for the prepared exact-key providers.")
    parser.add_argument("--prepare-exact-key-presets", type=Path, metavar="OUTPUT",
                        help="Write one reviewed provider's preset settings fragment without contacting WEB4.")
    parser.add_argument("--pouchdb-reviewed-settings", type=Path, help="Explicit reviewed settings patch for the qualified PouchDB source.")
    parser.add_argument("--resume-unstarted-native", action="store_true",
                        help="One explicit same-owner continuation before any client preparation/save; original failure must be recorded.")
    parser.add_argument("--cohort", choices=["current", "fixed-client", "post-startup-fix"], default="current",
                        help="An explicitly planned fresh test identity after retaining a failed/expired cohort; never automatic.")
    parser.add_argument("--continue-settled-read", action="store_true",
                        help="Explicitly finish the original qualified cohort; preserve prior proofs and forbid saves.")
    parser.add_argument("--continue-native-read-failure", action="store_true",
                        help="Explicit original-cohort completion after a preserved native read failure; saves remain forbidden.")
    parser.add_argument("--continue-snapshot-observation-change", action="store_true",
                        help="Finish original acknowledged snapshots after a proved changing-observation reuse defect; no writes.")
    options = parser.parse_args()
    if options.localfile_deployment_visitor and (options.provider != "LocalFileOASIS"
            or not (options.readiness or options.prepare)):
        parser.error("--localfile-deployment-visitor requires LocalFileOASIS with --readiness or --prepare.")
    if options.pouchdb_deployment_visitor and (options.provider != "PouchDBOASIS"
            or not (options.readiness or options.prepare)):
        parser.error("--pouchdb-deployment-visitor requires PouchDBOASIS with --readiness or --prepare.")
    if options.recover_localfile_receipt:
        if (options.provider != "LocalFileOASIS" or options.prepare or options.readiness
                or options.resume_unstarted_native or options.continue_settled_read
                or options.continue_native_read_failure or options.continue_snapshot_observation_change
                or options.pouchdb_reviewed_settings is not None or options.reviewed_exact_key_source is not None
                or options.prepare_exact_key_presets is not None):
            parser.error("--recover-localfile-receipt is a standalone LocalFile operation.")
        suffix = options.provider + ("" if options.cohort == "current" else "-" + options.cohort)
        state = ROOT / "LocalState/COSMIC" / ("HostedQueryAcceptance-" + suffix)
        results = ROOT / "TestResults/HostedCqlIntegrationCurrent"
        recover_localfile_http(state, *(results / (suffix + ending) for ending in
            ("-native-http.json", "-client-http-flow.json", "-isolation-http.json")))
        print("Recovered the original LocalFile HTTP acceptance receipt.")
        return
    is_pouchdb = options.provider == "PouchDBOASIS"
    if options.pouchdb_reviewed_settings is not None and not is_pouchdb:
        parser.error("--pouchdb-reviewed-settings applies only to PouchDBOASIS.")
    reviewed_pouchdb = pouchdb_review(options.pouchdb_reviewed_settings, require_deployment=True) if is_pouchdb else None
    rehearsal = ROOT / "LocalState/COSMIC/HostedDemoRehearsalDiscovery"
    if options.pouchdb_deployment_visitor:
        rehearsal = deployment_visitor("PouchDB", reviewed_pouchdb["deployment"])
    is_reviewed_exact_key = options.provider in REVIEWED_EXACT_KEY_PROVIDERS
    if options.reviewed_exact_key_source is not None and not is_reviewed_exact_key:
        parser.error("--reviewed-exact-key-source applies only to the prepared exact-key providers.")
    reviewed_source = (reviewed_exact_key_source(options.provider, options.reviewed_exact_key_source)
                       if is_reviewed_exact_key else reviewed_pouchdb)
    continuing = options.continue_settled_read or options.continue_native_read_failure or options.continue_snapshot_observation_change
    if options.prepare_exact_key_presets is not None:
        if (not is_reviewed_exact_key or options.prepare or options.readiness or continuing
                or options.resume_unstarted_native or options.cohort != "current"):
            parser.error("--prepare-exact-key-presets is a standalone operation for prepared exact-key providers.")
        key = "CQL_HOSTED_QUERY_EXAMPLES__" + options.provider
        fragment = {key: json.dumps(reviewed_source["presets"], separators=(",", ":"))}
        write_new(options.prepare_exact_key_presets, (json.dumps(fragment, indent=2) + "\n").encode())
        print(f"Prepared one reviewed {options.provider} preset partition.")
        return
    continuation_folder = "snapshot-reuse-completion" if options.continue_snapshot_observation_change else "native-read-completion" if options.continue_native_read_failure else "settlement-continuation"
    if sum((options.continue_settled_read, options.continue_native_read_failure, options.continue_snapshot_observation_change)) > 1:
        parser.error("Choose one explicit continuation kind.")
    if continuing and not options.prepare:
        parser.error("--continue-settled-read requires --prepare for the original client-flow gate")
    if options.resume_unstarted_native and (not options.prepare or continuing):
        parser.error("Unstarted-native continuation requires --prepare and cannot resume settled writes.")
    if options.readiness:
        if options.prepare or continuing or options.resume_unstarted_native or options.cohort != "current":
            parser.error("--readiness cannot be combined with cohort execution.")
        reviewed_localfile = None
        if options.localfile_deployment_visitor:
            rehearsal, reviewed_localfile = localfile_deployment_visitor()
        create_readiness_visitor = options.localfile_deployment_visitor or options.pouchdb_deployment_visitor
        if create_readiness_visitor:
            rehearsal.mkdir(exist_ok=True)
        _, token = session(rehearsal, "visitor", allow_create=create_readiness_visitor)
        try:
            source = ready_source(options.provider, token, require_pair=True)
            if options.provider == "LocalFileOASIS":
                require_reviewed_presets("LocalFile", source, reviewed_localfile or localfile_review())
            if reviewed_source is not None:
                require_reviewed_presets(options.provider, source, reviewed_source)
        except RuntimeError as error:
            raise SystemExit(str(error)) from None
        print(f"{options.provider} advertises native and paired presets; no acceptance cohort created.")
        return
    suffix = options.provider + ("" if options.cohort == "current" else "-" + options.cohort)
    if options.prepare:
        frontend = ROOT.parent / "Web4Site"
        if not all((frontend / name).is_file() for name in ("cql-demo-preparation.js", "cql-playground.js")):
            raise RuntimeError("Existing published frontend modules are required before creating any test session.")
    state = ROOT / "LocalState/COSMIC" / ("HostedQueryAcceptance-" + suffix)
    if continuing and not (state / continuation_folder / "continuation-source.json").is_file():
        raise RuntimeError("Original durable continuation manifest required; no session or replacement created.")
    if options.continue_native_read_failure or options.continue_snapshot_observation_change:
        manifest = json.loads((state / continuation_folder / "continuation-source.json").read_text(encoding="utf-8"))
        if (manifest.get("schema") != (3 if options.continue_snapshot_observation_change else 2) or manifest.get("failureKind") != ("snapshot_observation_change" if options.continue_snapshot_observation_change else "native_read_failure")
                or manifest.get("provider") != options.provider or manifest.get("writesPermitted") is not False):
            raise RuntimeError("Exact read-only native-failure continuation required.")
        write_new(state / continuation_folder / "execution.intent.json", (json.dumps({
            "ownerId": manifest["ownerId"], "failedOperationId": manifest["failedOperationId"],
            "additionalSaves": 0, "replayAllowed": False}) + "\n").encode())
    if (state / "cql-http-acceptance.acknowledged.json").exists():
        raise RuntimeError("This cohort already completed acceptance; retain its acknowledged outcome.")
    results = ROOT / "TestResults/HostedCqlIntegrationCurrent"
    if (options.provider == "LocalFileOASIS" or is_reviewed_exact_key) and (results / (suffix + "-client-http-flow.json")).exists():
        raise RuntimeError("Original client proof exists; reconcile it without another query or save.")
    if not continuing and (ROOT / "TestResults/HostedCqlIntegrationCurrent" / (suffix + "-client-http-flow.json.progress.json")).exists():
        raise RuntimeError("Original partial client flow exists; reconcile explicitly before another execution.")
    allow_create = not (continuing or options.resume_unstarted_native)
    reviewed_localfile = None
    if (options.provider == "LocalFileOASIS" or reviewed_source is not None) and allow_create and not (state / "owner.acknowledged.json").exists():
        if options.localfile_deployment_visitor:
            rehearsal, reviewed_localfile = localfile_deployment_visitor()
        _, visitor_token = session(rehearsal, "visitor", allow_create=False)
        source = ready_source(options.provider, visitor_token, require_pair=True)
        if reviewed_source is not None:
            require_reviewed_presets(options.provider, source, reviewed_source)
        else:
            if reviewed_localfile is None:
                reviewed_localfile = localfile_review(require_mount=True)
            require_reviewed_presets("LocalFile", source, reviewed_localfile)
    elif options.provider == "LocalFileOASIS":
        reviewed_localfile = localfile_review(require_mount=True)
    state.mkdir(exist_ok=True)
    if options.resume_unstarted_native:
        if (not (state / "owner.acknowledged.json").is_file()
                or not (state / "native-failure.observed.json").is_file()
                or any((state / name).exists() for name in ("foreign.acknowledged.json", "preparation-intents.json", "save-acknowledgements.jsonl"))
                or (results / (suffix + "-native-http.json")).exists()):
            raise RuntimeError("Original failed native-only cohort required; never replace or resave it.")
        failure = json.loads((state / "native-failure.observed.json").read_text(encoding="utf-8"))
        owner = json.loads((state / "owner.acknowledged.json").read_text(encoding="utf-8"))
        if failure.get("ownerId") != owner.get("id") or failure.get("privateSavesStarted") is not False:
            raise RuntimeError("Original owner/no-save failure proof differs.")
        write_new(state / "native-read-continuation.intent.json", (json.dumps({
            "ownerId": owner["id"], "provider": options.provider, "operation": "explicit original native-only continuation",
            "replacementOwner": False, "replayAllowed": False}) + "\n").encode())
    avatar, token = session(state, "owner", allow_create=allow_create)
    source = ready_source(options.provider, token, require_pair=options.prepare or reviewed_source is not None or reviewed_localfile is not None)
    if reviewed_source is not None:
        require_reviewed_presets(options.provider, source, reviewed_source)
    if reviewed_localfile is not None:
        require_reviewed_presets("LocalFile", source, reviewed_localfile)
    examples = source["examples"]
    leaf = [next(row for row in examples if row["id"] == key) for key in ("public-records", "public-subset")]
    if continuing:
        original = json.loads((state / continuation_folder / "original-native-http.json").read_text(encoding="utf-8-sig"))
        if original["avatar"]["id"] != avatar["id"] or original["source"]["provider"] != options.provider:
            raise RuntimeError("Original native proof belongs to a different cohort.")
        require_native_selection(original["native"], leaf)
    else:
        native = []
        for row in leaf:
            evidence = results / (suffix + "-" + row["id"] + "-native-observation.json")
            native.append(call("data/query-cql", token, row["request"], evidence_path=evidence))
        require_native_selection(native, leaf)
        verification.write(results / (suffix + "-native-http.json"),
                           {"avatar": avatar, "source": source, "native": native, "browserAcceptance": False})
    if options.prepare:
        report = results / (suffix + "-client-http-flow.json")
        if report.exists():
            raise RuntimeError("Client-flow proof already exists; review the original outcome rather than resaving.")
        payload = {"base": BASE, "token": token, "ownerId": avatar["id"], "state": str(state), "report": str(report), "source": source,
                   "preparationModule": str(ROOT.parent / "Web4Site/cql-demo-preparation.js"),
                   "pairedThree": next(row for row in examples if row["id"] == "mongo-public-records"),
                   "pairedTwo": next(row for row in examples if row["id"] == "mongo-public-subset")}
        if continuing:
            payload["resumeManifest"] = str(state / continuation_folder / "continuation-source.json")
            if options.continue_native_read_failure:
                payload["resumeKind"] = "native_read_failure"
            elif options.continue_snapshot_observation_change:
                payload["resumeKind"] = "snapshot_observation_change"
        elif Path(str(report) + ".progress.json").exists():
            raise RuntimeError("An original partial flow exists; reconcile explicitly instead of rerunning preparation.")
        run = subprocess.run(["node", str(ROOT / "Scripts/Sandbox/Verify-HostedSourceFlow.cjs")],
                             input=json.dumps(payload).encode(), capture_output=True)
        if run.returncode:
            raise RuntimeError("Client-flow failed; inspect preserved preparation intents. " + run.stderr.decode()[:500])
        print(run.stdout.decode().strip())
        proof = json.loads(report.read_text())
        foreign, foreign_token = session(state, "foreign")
        if reviewed_localfile is not None and foreign["id"] == avatar["id"]:
            raise RuntimeError("LocalFile privacy proof requires two distinct visitor identities.")
        public = call("data/query-cql", foreign_token, leaf[0]["request"])
        require_native_rows(public, leaf[0], 3)
        private = verify_private_visibility(foreign_token, proof["ids"],
                                            ["MongoDBOASIS", *proof["replicas"]])
        isolation = results / (suffix + "-isolation-http.json")
        isolation_proof = {"avatar": foreign, "public": public, "foreignPrivateIds": private["MongoDBOASIS"],
                           "foreignPrivateReplicaIds": {key: private[key] for key in proof["replicas"]},
                           "browserAcceptance": False}
        if reviewed_localfile is not None:
            mount = reviewed_localfile["mountedReceipt"]
            native_report = results / (suffix + "-native-http.json")
            isolation_proof["localFileAcceptance"] = {"ownerId": avatar["id"], "mount": mount,
                "priorEvidenceSha256": {path.name: hashlib.sha256(path.read_bytes()).hexdigest()
                                        for path in (native_report, report)}}
            write_new(isolation, json.dumps(isolation_proof, sort_keys=True).encode())
            recover_localfile_http(state, native_report, report, isolation)
        else:
            verification.write(isolation, isolation_proof)
    print(json.dumps({"provider": options.provider, "nativeRows": [3, 2], "nativeProofReused": continuing, "browserAcceptance": False}))


if __name__ == "__main__":
    main()

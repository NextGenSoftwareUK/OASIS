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
import ipaddress
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
    "WeaviateOASIS", "TigrisOASIS", "GarnetOASIS", "KeyDBOASIS", "DragonflyOASIS", "UpstashOASIS", "InfluxDBOASIS", "CouchbaseOASIS", "ArangoDBOASIS", "CockroachDBOASIS", "TimescaleDBOASIS", "CassandraOASIS", "RavenDBOASIS", "ScyllaDBOASIS", "LitestreamOASIS", "PineconeOASIS", "LanceDBOASIS", "NeonOASIS", "AlgoliaOASIS", "NileOASIS", "NetlifyBlobsOASIS", "FastlyOASIS", "DaprOASIS", "CloudflareOASIS", "VercelKVOASIS", "DenoDeployOASIS", "AppwriteOASIS", "ConvexOASIS", "TursoOASIS", "ENSOffchainOASIS", "OrbitDBOASIS", "GUNOASIS", "MilvusOASIS", "CloudinaryOASIS", "PgVectorOASIS", "ProofOfHumanityOASIS", "OracleObjectStorageOASIS", "SafeOASIS", "DruidOASIS", "PinotOASIS", "TemporalOASIS", "DiscordOASIS", "LivepeerOASIS", "TenderlyOASIS", "QuickNodeOASIS", "PinataOASIS", "PushProtocolOASIS", "LayerZeroOASIS",
    "WorldIDOASIS", "ReclaimProtocolOASIS", "DecentralandOASIS", "TheGraphOASIS", "SubqueryOASIS", "SelfProtocolOASIS", "HolonymOASIS", "CivicOASIS", "MapLibreOASIS", "TheSandboxOASIS", "WakuOASIS", "DuckDBOASIS", "NATSJetStreamOASIS",
)
FIXED_PUBLIC_CATALOGUES = {
    "TheSandboxOASIS": (
        "CQL_SANDBOX_CONTRACT_QUERY_SOURCE_OWNER", "sandbox/polygon/contracts", ("land", "sand", "asset"),
    ),
    "MapLibreOASIS": (
        "CQL_MAPLIBRE_STYLE_QUERY_SOURCE_OWNER", "maplibre/demo-styles", ("world", "bright", "terrain"),
    ),
    "CivicOASIS": (
        "CQL_CIVIC_NETWORK_QUERY_SOURCE_OWNER", "civic/polygon/network", ("1", "4", "10"),
    ),
    "HolonymOASIS": (
        "CQL_HOLONYM_ATTESTATION_QUERY_SOURCE_OWNER", "human-id/sign/optimism/schema-1",
        ("onchain_evm_10_0x3248d", "onchain_evm_10_0x3248c", "onchain_evm_10_0x3248a"),
    ),
    "SelfProtocolOASIS": (
        "CQL_SELF_PROTOCOL_REGISTRY_QUERY_SOURCE_OWNER",
        "self-protocol/celo/registry-root/0xe8e3a373b127a614a3df09c20e23085c2881804d05aea7f3122039f08f7586ec",
        ("1", "3", "4"),
    ),
    "SubqueryOASIS": (
        "CQL_SUBQUERY_DICTIONARY_QUERY_SOURCE_OWNER", "subquery/arbitrum/transfer-block",
        ("400000000", "400000002", "400000003"),
    ),
    "TheGraphOASIS": (
        "CQL_THE_GRAPH_STUDIO_QUERY_SOURCE_OWNER", "the-graph-studio-index-status",
        ("adexto-arbitrum", "adexto-base", "klerosboard-gnosis"),
    ),
    "DiscordOASIS": (
        "CQL_DISCORD_PUBLIC_GUILD_QUERY_SOURCE_OWNER", "discord-public-guild",
        ("267624335836053506", "1235157165589794909", "212250894228652034"),
    ),
    "LivepeerOASIS": (
        "CQL_LIVEPEER_ORCHESTRATOR_QUERY_SOURCE_OWNER", "livepeer-bonding-manager/arbitrum-one",
        ("0x525419ff5707190389bfb5c87c375d710f5fcb0e", "0xac2e50c8f7ac0f82923a7df9d9903f6ec4741919",
         "0x4f4758f7167b18e1f5b3c1a7575e3eb584894dbc"),
    ),
    "PinataOASIS": (
        "CQL_PINATA_PUBLIC_BLOCK_QUERY_SOURCE_OWNER", "pinata-public-ipfs-block",
        ("QmP4eZj61tDxCmtb6aqSwQsrxRQAN2gwxTEXkTed9TujHh", "QmeBt6f2Dem8QnZuZ8PDRT1iuWpphmza3gP17en2ZoReYR",
         "Qmb733sded7TRm8U8bM4FHif89jUVKd8yQiurKCKgCLyQk"),
    ),
    "PushProtocolOASIS": (
        "CQL_PUSH_PUBLIC_CHANNEL_QUERY_SOURCE_OWNER", "push-protocol-public-channel/eip155:1",
        ("0xb88460bb2696cab9d66013a05dff29a28330689d", "0x90a48d5cf7343b08da12e067680b4c6dbfe551be",
         "0xdbc5936e4daae94f415c39d284f6a69c4d553f2f"),
    ),
    "LayerZeroOASIS": (
        "CQL_LAYERZERO_V1_ENDPOINT_QUERY_SOURCE_OWNER", "layerzero-v1-endpoint",
        ("ethereum", "polygon", "arbitrum"),
    ),
    "WorldIDOASIS": (
        "CQL_WORLD_ID_PROTOCOL_QUERY_SOURCE_OWNER", "world-id-v4-protocol/world-chain",
        ("registry", "verifier", "issuer-registry"),
    ),
    "ReclaimProtocolOASIS": (
        "CQL_RECLAIM_VERIFIER_QUERY_SOURCE_OWNER", "reclaim-verifier",
        ("base", "arbitrum", "polygon"),
    ),
    "DecentralandOASIS": (
        "CQL_DECENTRALAND_SCENE_QUERY_SOURCE_OWNER", "decentraland-genesis-city-parcel",
        ("0,0", "-53,71", "-100,-50"),
    ),
}
REVIEWED_PUBLIC_CATALOGUE_PROVIDERS = frozenset((*FIXED_PUBLIC_CATALOGUES,
    "CloudinaryOASIS", "OracleObjectStorageOASIS", "ProofOfHumanityOASIS",
    "GUNOASIS", "OrbitDBOASIS", "WakuOASIS", "DuckDBOASIS", "NATSJetStreamOASIS"))

POLYGON_GATEWAY_CATALOGUES = {
    "TenderlyOASIS": ("CQL_TENDERLY_POLYGON_QUERY_SOURCE_OWNER", "tenderlypolygon-public-accounts:0x89",
                     "TenderlyPolygonAccountAddress"),
    "QuickNodeOASIS": ("CQL_QUICKNODE_POLYGON_QUERY_SOURCE_OWNER", "quicknodepolygon-public-accounts:0x89",
                       "QuickNodePolygonAccountAddress"),
}
POLYGON_GATEWAY_KEYS = (
    "0x0000000000000000000000000000000000000000",
    "0x000000000000000000000000000000000000dead",
    "0x0000000000000000000000000000000000001010",
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
        if not target.parent.is_dir():
            raise FileNotFoundError("Query evidence directory must exist before dispatch.")
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

def require_native_rows(response, example, count, public_ids=None):
    rows = response["result"]["rows"]
    if len(rows) != count:
        raise RuntimeError("Native source selection count differs.")
    if public_ids is not None and {row.get("id") for row in rows} != set(public_ids[:count]):
        raise RuntimeError("Native source row IDs differ from the reviewed public authority.")
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
            if public_ids is not None:
                expected_ids = dict(zip(predicate["value"], public_ids[:count]))
                if any(row.get("id") != expected_ids[(row.get("metadata") or {}).get(key)] for row in rows):
                    raise RuntimeError("Native source row identities differ from their reviewed metadata keys.")


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


def decentraland_deployment_visitor():
    return reviewed_deployment_visitor("DecentralandOASIS")


def reviewed_deployment_visitor(provider):
    if provider not in REVIEWED_PUBLIC_CATALOGUE_PROVIDERS:
        raise RuntimeError("A reviewed public catalogue is required.")
    publisher = ROOT / "Scripts/Sandbox" / {
        "WakuOASIS": "Publish-HostedWakuConfiguration.py",
        "DuckDBOASIS": "Publish-HostedDuckDbConfiguration.py",
        "NATSJetStreamOASIS": "Publish-HostedOwnedNatsConfiguration.py",
    }.get(provider, "Publish-HostedLocalFileConfiguration.py")
    spec = importlib.util.spec_from_file_location("reviewed_release", publisher)
    release = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(release)
    deployment = os.environ.get("COSMIC_CQL_SETTINGS_DEPLOYMENT_ID", "")
    if not deployment or (release.verify_settings() if provider in ("WakuOASIS", "DuckDBOASIS", "NATSJetStreamOASIS") else release.verify_release()) != deployment:
        raise RuntimeError(f"The reviewed {provider} settings deployment must be current.")
    return deployment_visitor(provider.removesuffix("OASIS"), deployment)


def deployment_visitor(provider, deployment):
    if str(uuid.UUID(deployment)) != deployment:
        raise RuntimeError(f"Canonical {provider} settings deployment ID required.")
    return ROOT / "LocalState/COSMIC" / f"Hosted{provider}Readiness-{deployment}"


def require_readiness_release():
    spec = importlib.util.spec_from_file_location("readiness_release_guard", ROOT / "Scripts/Sandbox/Prepare-TenSourcePublicBatch.py")
    guard = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(guard)
    host_spec = importlib.util.spec_from_file_location("readiness_configuration", ROOT / "Scripts/Sandbox/Publish-HostedSolidConfiguration.py")
    configuration = importlib.util.module_from_spec(host_spec)
    host_spec.loader.exec_module(configuration)
    return guard.require_matching_release(configuration,
        release_ref=os.environ.get("COSMIC_CQL_REVIEWED_RELEASE_REF", "HEAD"))


def acknowledged_readiness_visitor(directory):
    path = directory.resolve(strict=True)
    if (path.parent != (ROOT / "LocalState/COSMIC").resolve(strict=True)
            or not path.name.startswith("Hosted") or "Readiness-" not in path.name
            or not (path / "visitor.acknowledged.json").is_file()
            or not (path / "visitor.session.dpapi").is_file()):
        raise RuntimeError("An existing acknowledged readiness visitor in this checkout is required.")
    return path


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


def ens_offchain_logical_id(actor, name):
    return native_logical_id(actor, "ENSOffchainOASIS", "web3.bio/ens", name)


def native_logical_id(actor, provider, source, key):
    # Match NativeRecordHolonMapper's version-eight .NET Guid byte layout.
    digest = bytearray(hashlib.sha256(f"oasis-native-import\n{actor}\n{provider}\n{source}\n{key}".encode()).digest()[:16])
    digest[7] = (digest[7] & 0x0f) | 0x80
    digest[8] = (digest[8] & 0x3f) | 0x80
    return str(uuid.UUID(bytes_le=bytes(digest)))


def reviewed_exact_key_source(provider, path):
    if path is None or not path.is_file() or not 0 < path.stat().st_size <= 16384:
        raise RuntimeError("An explicit reviewed native teaching catalogue is required.")
    catalogue = json.loads(path.read_bytes())
    try:
        names_only = provider == "ENSOffchainOASIS"
        fixed_public = FIXED_PUBLIC_CATALOGUES.get(provider)
        polygon_gateway = POLYGON_GATEWAY_CATALOGUES.get(provider)
        fields = ({"Actor", "LiveIds"} if provider == "NATSJetStreamOASIS" else
                  {"Actor", "Names"} if names_only else {"Actor", "LiveIds", "DeletedId", "MissingId"})
        public_keys = {
            "WakuOASIS": ("MessageHashes", "CQL_WAKU_STORE_QUERY_SOURCE_JSON",
                          "waku-store-v3/oasis-cql/1/store-fixture/proto", r"0x[0-9a-f]{64}"),
            "SafeOASIS": ("Networks", "CQL_SAFE_INDEXING_QUERY_SOURCE_JSON",
                          "safe-transaction-service-indexing", r"(?:eth|base|arb1)"),
            "ProofOfHumanityOASIS": ("Addresses", "CQL_PROOF_OF_HUMANITY_QUERY_SOURCE_JSON",
                                    "proof-of-humanity-v2/cross-chain", r"0x[0-9a-f]{40}"),
            "OracleObjectStorageOASIS": ("Keys", "CQL_ORACLE_OBJECT_STORAGE_QUERY_SOURCE_JSON",
                                         "objectstorage.us-phoenix-1.oraclecloud.com/n/oraclebigdatadb/b/workshop-data/o",
                                         r"bds-livelabs/[a-z0-9][a-z0-9-]{0,79}\.sh"),
        }.get(provider)
        if fixed_public or polygon_gateway:
            fields = {"Actor"}
        elif public_keys:
            fields = {"Actor", public_keys[0]}
        if provider == "CloudinaryOASIS":
            fields = {"Actor", "Assets"}
        if provider == "TemporalOASIS":
            fields = {"Actor", "Namespace", "Runs"}
        if provider in ("DruidOASIS", "PinotOASIS"):
            fields |= {"Origin", "ReaderUsername"}
            origin = catalogue["Origin"]
            if (len(origin) > 261 or not re.fullmatch(
                    r"https://(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z](?:[a-z0-9-]{0,61}[a-z0-9])?", origin)
                    or not re.fullmatch(r"[a-z][a-z0-9_-]{0,62}", catalogue["ReaderUsername"])):
                raise ValueError("Unexpected protected SQL HTTPS origin or restricted reader identity.")
        if provider == "PgVectorOASIS":
            fields |= {"Host", "Database", "ReaderUsername"}
            host = catalogue["Host"]
            if (len(host) > 253 or not re.fullmatch(
                    r"(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z](?:[a-z0-9-]{0,61}[a-z0-9])?", host)
                    or any(not re.fullmatch(r"[a-z][a-z0-9_]{0,62}", catalogue[key])
                           for key in ("Database", "ReaderUsername"))):
                raise ValueError("Unexpected pgvector database address or reader identity.")
        if provider in ("MinIOOASIS", "TigrisOASIS"):
            fields |= {"Endpoint", "Bucket", "Originals"}
        if provider == "MinIOOASIS":
            fields.add("Region")
        if provider == "CassandraOASIS":
            fields.add("BundleSha256")
            if not re.fullmatch(r"[0-9a-f]{64}", catalogue["BundleSha256"]):
                raise ValueError("Unexpected Cassandra secure connection bundle digest.")
        if provider == "LitestreamOASIS":
            fields.add("RestoreSha256")
            if not re.fullmatch(r"[0-9a-f]{64}", catalogue["RestoreSha256"]):
                raise ValueError("Unexpected Litestream restore digest.")
        if provider == "PineconeOASIS":
            fields.add("Origin")
            origin = catalogue["Origin"]
            if (len(origin) > 261 or ".svc." not in origin
                    or not re.fullmatch(r"https://(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+pinecone\.io", origin)):
                raise ValueError("Unexpected Pinecone index origin.")
        if provider == "LanceDBOASIS":
            fields |= {"Origin", "Database"}
            origin = catalogue["Origin"]
            if (len(origin) > 261
                    or not re.fullmatch(r"https://(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+api\.lancedb\.com", origin)
                    or not re.fullmatch(r"[a-z][a-z0-9_-]{0,62}", catalogue["Database"])):
                raise ValueError("Unexpected LanceDB origin or database.")
        if provider == "MilvusOASIS":
            fields.add("Origin")
            origin = catalogue["Origin"]
            if (len(origin) > 261 or not re.fullmatch(
                    r"https://(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z](?:[a-z0-9-]{0,61}[a-z0-9])?",
                    origin)):
                raise ValueError("Unexpected Milvus HTTPS origin.")
        if provider == "NeonOASIS":
            fields.add("Host")
            host = catalogue["Host"]
            if (len(host) > 253 or not host.startswith("ep-") or host.split(".")[0].endswith("-pooler")
                    or not re.fullmatch(r"(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+neon\.tech", host)):
                raise ValueError("Unexpected direct Neon compute host.")
        if provider == "AlgoliaOASIS":
            fields.add("AppId")
            if not re.fullmatch(r"[A-Za-z0-9]{1,50}", catalogue["AppId"]):
                raise ValueError("Unexpected Algolia application ID.")
        if provider == "NileOASIS":
            fields |= {"Host", "Database"}
            if (not re.fullmatch(r"(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)?db\.thenile\.dev", catalogue["Host"])
                    or not re.fullmatch(r"[a-z0-9][a-z0-9_-]{0,62}", catalogue["Database"])):
                raise ValueError("Unexpected Nile database address.")
        if provider == "NetlifyBlobsOASIS":
            fields |= {"SiteId", "Store", "ObjectOrigin"}
            site = uuid.UUID(catalogue["SiteId"])
            origin = catalogue["ObjectOrigin"]
            if (site.int == 0 or str(site) != catalogue["SiteId"]
                    or not re.fullmatch(r"[A-Za-z0-9_-]{1,64}", catalogue["Store"])
                    or len(origin) > 261
                    or not re.fullmatch(r"https://(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z][a-z0-9-]{0,61}[a-z0-9]", origin)):
                raise ValueError("Unexpected Netlify site, store or object origin.")
        if provider == "FastlyOASIS":
            fields.add("StoreId")
            if not re.fullmatch(r"[A-Za-z0-9_-]{1,64}", catalogue["StoreId"]):
                raise ValueError("Unexpected Fastly KV store ID.")
        if provider == "DaprOASIS":
            fields |= {"Store", "GrpcEndpoint"}
            port = re.fullmatch(r"http://127\.0\.0\.1:([1-9][0-9]{0,4})/", catalogue["GrpcEndpoint"])
            if (not re.fullmatch(r"[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?", catalogue["Store"])
                    or not port or int(port[1]) == 80 or int(port[1]) > 65535):
                raise ValueError("Unexpected Dapr component or loopback endpoint.")
        if provider in ("OrbitDBOASIS", "GUNOASIS"):
            fields.add("Sidecar")
            port = re.fullmatch(r"http://127\.0\.0\.1:([1-9][0-9]{3,4})/", catalogue["Sidecar"])
            if not port or not 1024 <= int(port[1]) <= 65535:
                raise ValueError("Unexpected co-located teaching sidecar.")
        if provider == "CloudflareOASIS":
            fields |= {"AccountId", "NamespaceId"}
            if any(not re.fullmatch(r"[0-9a-f]{32}", catalogue[field]) for field in ("AccountId", "NamespaceId")):
                raise ValueError("Unexpected Workers KV account or namespace ID.")
        if provider == "VercelKVOASIS":
            fields.add("Endpoint")
            endpoint = catalogue["Endpoint"]
            if (len(endpoint) > 262 or
                    not re.fullmatch(r"https://(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+upstash\.io/", endpoint)):
                raise ValueError("Unexpected migrated Vercel KV HTTPS endpoint.")
        if provider == "DenoDeployOASIS":
            fields |= {"DatabaseId", "DataEndpoint"}
            identity = uuid.UUID(catalogue["DatabaseId"])
            endpoint = catalogue["DataEndpoint"]
            if (identity.int == 0 or str(identity) != catalogue["DatabaseId"] or len(endpoint) > 1024
                    or not re.fullmatch(r"https://(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z][a-z0-9-]{0,62}/[A-Za-z0-9._~/-]*", endpoint)
                    or any(part in (".", "..") for part in endpoint.split("/"))):
                raise ValueError("Unexpected Deno KV database identity or HTTPS data endpoint.")
        if provider == "AppwriteOASIS":
            fields |= {"Endpoint", "ProjectId", "DatabaseId"}
            origin = catalogue["Endpoint"]
            if (len(origin) > 264
                    or not re.fullmatch(r"https://(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z][a-z0-9-]{0,62}/v1", origin)
                    or any(not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]{0,35}", catalogue[field])
                           for field in ("ProjectId", "DatabaseId"))):
                raise ValueError("Unexpected Appwrite endpoint, project or database.")
        if provider == "ConvexOASIS":
            fields.add("Site")
            site = catalogue["Site"]
            if (len(site) > 262
                    or not re.fullmatch(r"https://(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z][a-z0-9-]{0,62}/", site)):
                raise ValueError("Unexpected Convex teaching site.")
        if provider == "TursoOASIS":
            fields.add("Origin")
            origin = catalogue["Origin"]
            if (len(origin) > 262
                    or not re.fullmatch(r"https://(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+turso\.io/", origin)):
                raise ValueError("Unexpected Turso teaching database origin.")
        if provider == "ScyllaDBOASIS":
            fields |= {"Ip", "Host", "CaSha256"}
            address = ipaddress.ip_address(catalogue["Ip"])
            host = catalogue["Host"]
            if (str(address) != catalogue["Ip"] or not address.is_global or address.is_multicast
                    or len(host) > 253
                    or not re.fullmatch(r"(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+clusters\.scylla\.cloud", host)
                    or not re.fullmatch(r"[0-9a-f]{64}", catalogue["CaSha256"])):
                raise ValueError("Unexpected ScyllaDB Cloud node or CA digest.")
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
        if set(catalogue) != fields:
            raise ValueError("Unexpected teaching catalogue shape.")
        if polygon_gateway:
            _, namespace, _ = polygon_gateway
            ids = [native_logical_id(catalogue["Actor"], provider, namespace, key) for key in POLYGON_GATEWAY_KEYS]
            record_ids = ids
        elif fixed_public:
            _, namespace, keys = fixed_public
            ids = [native_logical_id(catalogue["Actor"], provider, namespace, key) for key in keys]
            record_ids = ids
        elif names_only:
            names = catalogue["Names"]
            if (not isinstance(names, list) or len(names) != 3
                    or any(not isinstance(name, str) or len(name) > 253
                           or not re.fullmatch(r"(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+eth", name)
                           for name in names)):
                raise ValueError("Three canonical ASCII ENS names are required.")
            ids = [ens_offchain_logical_id(catalogue["Actor"], name) for name in names]
            record_ids = ids
        elif provider == "TemporalOASIS":
            namespace = catalogue["Namespace"]
            runs = catalogue["Runs"]
            label = r"[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?"
            if (len(namespace) > 64 or not re.fullmatch(label + r"\." + label, namespace)
                    or not isinstance(runs, list) or len(runs) != 3
                    or any(not isinstance(run, dict) or set(run) != {"WorkflowId", "RunId"}
                           or not re.fullmatch(r"oasis-teaching-[a-z0-9-]{1,40}", run["WorkflowId"])
                           or str(uuid.UUID(run["RunId"])) != run["RunId"] or uuid.UUID(run["RunId"]).int == 0
                           for run in runs)
                    or len({run["RunId"] for run in runs}) != 3):
                raise ValueError("Three exact Temporal teaching runs in the reviewed cloud namespace are required.")
            ids = [native_logical_id(catalogue["Actor"], provider, "temporal/workflow-description",
                                     f"{namespace}/{run['WorkflowId']}/{run['RunId']}") for run in runs]
            record_ids = ids
        elif provider == "CloudinaryOASIS":
            assets = catalogue["Assets"]
            if (not isinstance(assets, list) or len(assets) != 3
                    or any(not isinstance(asset, str) or not re.fullmatch(r"[a-z0-9][a-z0-9_-]{0,79}\.jpg", asset)
                           for asset in assets)):
                raise ValueError("Three canonical Cloudinary demo JPEG assets are required.")
            ids = [native_logical_id(catalogue["Actor"], provider, "res.cloudinary.com/demo/image/upload", asset)
                   for asset in assets]
            record_ids = ids
        elif public_keys:
            key_field, _, source_name, pattern = public_keys
            logical_keys = catalogue[key_field]
            if (not isinstance(logical_keys, list) or len(logical_keys) != 3
                    or any(not isinstance(key, str) or not re.fullmatch(pattern, key) for key in logical_keys)):
                raise ValueError("Three canonical public source keys are required.")
            ids = [native_logical_id(catalogue["Actor"], provider, source_name, key) for key in logical_keys]
            record_ids = ids
        else:
            ids = catalogue["LiveIds"]
            if not isinstance(ids, list) or len(ids) != 3:
                raise ValueError("Three teaching identities are required.")
            record_ids = ids if provider == "NATSJetStreamOASIS" else [*ids, catalogue["DeletedId"], catalogue["MissingId"]]
        if any(str(uuid.UUID(value)) != value or uuid.UUID(value).int == 0
               for value in [catalogue["Actor"], *record_ids]) or len(set(record_ids)) != len(record_ids):
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
    for suffix, count in (("records", 3), ("subset", 2)):
        if polygon_gateway:
            predicate = {"metadataPath": [polygon_gateway[2]], "scalar": "String", "operator": "In",
                         "value": list(POLYGON_GATEWAY_KEYS[:count])}
        else:
            predicate = {"field": "Id", "scalar": "Guid", "operator": "In", "value": ids[:count]}
        selection = {"orderBy": "None", "take": 25, "includeMetadata": True, "filters": [predicate]}
        presets.extend([
            {"id": "public-" + suffix, "label": f"{provider} public teaching records ({count})",
             "request": dict(selection, provider=provider, maximumDurationMilliseconds=30000)},
            {"id": "mongo-public-" + suffix, "label": f"MongoDB + {provider} ({count} source records)",
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
    elif provider == "ScyllaDBOASIS":
        review["sourceBinding"] = {"CQL_SCYLLADB_QUERY_IP": catalogue["Ip"],
                                   "CQL_SCYLLADB_QUERY_HOST": catalogue["Host"],
                                   "CQL_SCYLLADB_QUERY_CA_SHA256": catalogue["CaSha256"],
                                   "CQL_SCYLLADB_QUERY_PROBE_ID": ids[0]}
    elif provider == "LitestreamOASIS":
        review["sourceBinding"] = {"CQL_LITESTREAM_QUERY_RESTORE_SHA256": catalogue["RestoreSha256"],
                                   "CQL_LITESTREAM_QUERY_PROBE_ID": ids[0]}
    elif provider == "PineconeOASIS":
        review["sourceBinding"] = {"CQL_PINECONE_QUERY_ORIGIN": catalogue["Origin"],
                                   "CQL_PINECONE_QUERY_PROBE_ID": ids[0]}
    elif provider == "LanceDBOASIS":
        review["sourceBinding"] = {"CQL_LANCEDB_QUERY_ORIGIN": catalogue["Origin"],
                                   "CQL_LANCEDB_QUERY_DATABASE": catalogue["Database"],
                                   "CQL_LANCEDB_QUERY_PROBE_ID": ids[0]}
    elif provider == "NeonOASIS":
        review["sourceBinding"] = {"CQL_NEON_QUERY_HOST": catalogue["Host"],
                                   "CQL_NEON_QUERY_PROBE_ID": ids[0]}
    elif provider == "AlgoliaOASIS":
        review["sourceBinding"] = {"CQL_ALGOLIA_QUERY_APP_ID": catalogue["AppId"],
                                   "CQL_ALGOLIA_QUERY_PROBE_ID": ids[0]}
    elif provider == "NileOASIS":
        review["sourceBinding"] = {"CQL_NILE_QUERY_HOST": catalogue["Host"],
                                   "CQL_NILE_QUERY_DATABASE": catalogue["Database"],
                                   "CQL_NILE_QUERY_PROBE_ID": ids[0]}
    elif provider == "NetlifyBlobsOASIS":
        review["sourceBinding"] = {"CQL_NETLIFY_BLOBS_QUERY_SITE_ID": catalogue["SiteId"],
                                   "CQL_NETLIFY_BLOBS_QUERY_STORE": catalogue["Store"],
                                   "CQL_NETLIFY_BLOBS_QUERY_OBJECT_ORIGIN": catalogue["ObjectOrigin"],
                                   "CQL_NETLIFY_BLOBS_QUERY_PROBE_ID": ids[0]}
    elif provider == "FastlyOASIS":
        review["sourceBinding"] = {"CQL_FASTLY_QUERY_STORE_ID": catalogue["StoreId"],
                                   "CQL_FASTLY_QUERY_PROBE_ID": ids[0]}
    elif provider == "DaprOASIS":
        review["sourceBinding"] = {"CQL_DAPR_QUERY_STORE": catalogue["Store"],
                                   "CQL_DAPR_QUERY_GRPC_ENDPOINT": catalogue["GrpcEndpoint"],
                                   "CQL_DAPR_QUERY_PROBE_ID": ids[0]}
    elif provider == "CloudflareOASIS":
        review["sourceBinding"] = {"CQL_CLOUDFLARE_KV_QUERY_ACCOUNT_ID": catalogue["AccountId"],
                                   "CQL_CLOUDFLARE_KV_QUERY_NAMESPACE_ID": catalogue["NamespaceId"],
                                   "CQL_CLOUDFLARE_KV_QUERY_PROBE_ID": ids[0]}
    elif provider == "VercelKVOASIS":
        review["sourceBinding"] = {"CQL_VERCEL_KV_QUERY_ENDPOINT": catalogue["Endpoint"],
                                   "CQL_VERCEL_KV_QUERY_PROBE_ID": ids[0]}
    elif provider == "DenoDeployOASIS":
        review["sourceBinding"] = {"CQL_DENO_KV_QUERY_DATABASE_ID": catalogue["DatabaseId"],
                                   "CQL_DENO_KV_QUERY_DATA_ENDPOINT": catalogue["DataEndpoint"],
                                   "CQL_DENO_KV_QUERY_PROBE_ID": ids[0]}
    elif provider == "AppwriteOASIS":
        review["sourceBinding"] = {"CQL_APPWRITE_QUERY_ENDPOINT": catalogue["Endpoint"],
                                   "CQL_APPWRITE_QUERY_PROJECT_ID": catalogue["ProjectId"],
                                   "CQL_APPWRITE_QUERY_DATABASE_ID": catalogue["DatabaseId"],
                                   "CQL_APPWRITE_QUERY_PROBE_ID": ids[0]}
    elif provider == "ConvexOASIS":
        review["sourceBinding"] = {"CQL_CONVEX_QUERY_SITE": catalogue["Site"],
                                   "CQL_CONVEX_QUERY_PROBE_ID": ids[0]}
    elif provider == "NATSJetStreamOASIS":
        review["publicIds"] = ids
        review["sourceBinding"] = {"OASIS_SANDBOX_NATS": "true",
            "CQL_NATS_PUBLIC_SOURCE_USERNAME": "cql_reader",
            "CQL_NATS_PUBLIC_SOURCE_JSON": json.dumps({"schema": 1,
                "endpoint": "tls://127.0.0.1:24222", "bucket": "cosmic_cql_public",
                "sourceOwner": catalogue["Actor"], "ids": ids}, separators=(",", ":"))}
    elif provider == "DuckDBOASIS":
        review["sourceBinding"] = {"CQL_DUCKDB_QUERY_CONNECTION_STRING":
            "Data Source=/data/COSMIC/SandboxDuckDB/source.duckdb;access_mode=READ_ONLY"}
    elif provider == "TursoOASIS":
        review["sourceBinding"] = {"CQL_TURSO_QUERY_ORIGIN": catalogue["Origin"],
                                   "CQL_TURSO_QUERY_PROBE_ID": ids[0]}
    elif fixed_public or polygon_gateway:
        setting, _, _ = fixed_public or polygon_gateway
        review["sourceBinding"] = {setting: catalogue["Actor"]}
        if polygon_gateway:
            review["publicIds"] = ids
            review["requiredQueryableMetadataKey"] = polygon_gateway[2]
    elif provider == "TemporalOASIS":
        source = {"owner": catalogue["Actor"], "namespace": namespace,
                  "runs": [{"workflowId": run["WorkflowId"], "runId": run["RunId"]} for run in runs],
                  "probeRunId": runs[0]["RunId"]}
        review["sourceBinding"] = {"CQL_TEMPORAL_QUERY_SOURCE_JSON": json.dumps(source, separators=(",", ":"))}
    elif provider == "ENSOffchainOASIS":
        source = {"owner": catalogue["Actor"], "names": names, "probe": names[0]}
        review["sourceBinding"] = {"CQL_ENS_OFFCHAIN_QUERY_SOURCE_JSON": json.dumps(source, separators=(",", ":"))}
    elif provider in ("OrbitDBOASIS", "GUNOASIS"):
        prefix = "CQL_" + provider.removesuffix("OASIS").upper() + "_QUERY_"
        review["sourceBinding"] = {prefix + "SIDECAR": catalogue["Sidecar"], prefix + "PROBE_ID": ids[0]}
    elif provider == "MilvusOASIS":
        review["sourceBinding"] = {"CQL_MILVUS_QUERY_ORIGIN": catalogue["Origin"],
                                   "CQL_MILVUS_QUERY_PROBE_ID": ids[0]}
    elif provider == "CloudinaryOASIS":
        source = {"owner": catalogue["Actor"], "assets": assets, "probe": assets[0]}
        review["sourceBinding"] = {"CQL_CLOUDINARY_QUERY_SOURCE_JSON": json.dumps(source, separators=(",", ":"))}
    elif provider in ("DruidOASIS", "PinotOASIS"):
        prefix = "CQL_" + provider.removesuffix("OASIS").upper() + "_QUERY_"
        review["sourceBinding"] = {prefix + "ORIGIN": catalogue["Origin"],
                                   prefix + "READER_USERNAME": catalogue["ReaderUsername"],
                                   prefix + "PROBE_ID": ids[0]}
    elif provider == "PgVectorOASIS":
        review["sourceBinding"] = {"CQL_PGVECTOR_QUERY_HOST": catalogue["Host"],
                                   "CQL_PGVECTOR_QUERY_DATABASE": catalogue["Database"],
                                   "CQL_PGVECTOR_QUERY_READER_USERNAME": catalogue["ReaderUsername"],
                                   "CQL_PGVECTOR_QUERY_PROBE_ID": ids[0]}
    elif public_keys:
        key_field, setting, _, _ = public_keys
        source = ({"sourceOwner": catalogue["Actor"], "messageHashes": logical_keys} if provider == "WakuOASIS"
                  else {"owner": catalogue["Actor"], key_field.lower(): logical_keys, "probe": logical_keys[0]})
        review["sourceBinding"] = {setting: json.dumps(source, separators=(",", ":"))}
    elif provider in ("WeaviateOASIS", "GarnetOASIS", "KeyDBOASIS", "DragonflyOASIS", "UpstashOASIS", "InfluxDBOASIS", "CouchbaseOASIS", "ArangoDBOASIS"):
        product = provider.removesuffix("OASIS").upper()
        review["sourceBinding"] = {f"CQL_{product}_QUERY_PROBE_ID": ids[0]}
    return review


def require_native_selection(responses, examples, public_ids=None):
    if len(responses) != 2 or len(examples) != 2:
        raise RuntimeError("Two native source selections are required.")
    for response, example, count in zip(responses, examples, (3, 2)):
        require_native_rows(response, example, count, public_ids)


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
    parser.add_argument("provider", choices=["ThreeFoldOASIS", "ClickHouseOASIS", "QuestDBOASIS", "SQLServerDBOASIS", "CouchDBOASIS", "PouchDBOASIS", "ChromaOASIS", "CloudflareD1OASIS", "NATSJetStreamOASIS", "ZcashOASIS", "BittensorOASIS", "AkashOASIS", "EclipseOASIS", "ENSOASIS", "BlockscoutOASIS", "CelestiaOASIS", "ChainflipOASIS", "LocalFileOASIS", "HyperlaneOASIS", "LayerZeroV2OASIS", "StargateOASIS", "deBridgeOASIS", "SynapseOASIS", "ConnextOASIS", "MidenOASIS", "EspressoSystemsOASIS", "EigenLayerOASIS", "FhenixOASIS", "AxelarOASIS", "RitualOASIS", "WormholeOASIS", "BlockStackOASIS", "MatrixOASIS", "NostrOASIS", "BlueSkyOASIS", "ChainLinkOASIS", "EOSIOOASIS", "TelosOASIS", "FilecoinOASIS", "FantomOASIS", "AzureStorageOASIS", "SOLIDOASIS", "AWSS3OASIS", "SolanaOASIS", "GoogleCloudOASIS", "EthereumOASIS", "PolygonOASIS", "ArbitrumOASIS", "CosmosBlockChainOASIS", "PolkadotOASIS", "NEAROASIS", "BaseOASIS", "AvalancheOASIS", "BNBChainOASIS", "OptimismOASIS", "LineaOASIS", "ScrollOASIS", "ZkSyncOASIS", "AbstractOASIS", "MonadOASIS", "SeiOASIS", "BerachainOASIS", "BitcoinOASIS", "XRPLOASIS", "StellarOASIS", "AlgorandOASIS", "AptosOASIS", "HashgraphOASIS", "SuiOASIS", "ElrondOASIS", "RadixOASIS", "StarknetOASIS", "CardanoOASIS", "ArweaveOASIS", "TONOASIS", *REVIEWED_EXACT_KEY_PROVIDERS])
    parser.add_argument("--prepare", action="store_true")
    parser.add_argument("--readiness", action="store_true", help="Check discovery using the retained rehearsal visitor; create no cohort.")
    parser.add_argument("--localfile-deployment-visitor", action="store_true",
                        help="Use a separate visitor for the verified LocalFile settings deployment.")
    parser.add_argument("--pouchdb-deployment-visitor", action="store_true",
                        help="Use a separate visitor for the verified PouchDB settings deployment.")
    parser.add_argument("--decentraland-deployment-visitor", action="store_true",
                        help="Use a separate visitor for the verified Decentraland settings deployment.")
    parser.add_argument("--reviewed-deployment-visitor", action="store_true",
                        help="Use a separate visitor for a verified reviewed public catalogue settings deployment.")
    parser.add_argument("--reuse-acknowledged-readiness", type=Path,
                        help="Explicitly reuse an existing acknowledged discovery visitor after verifying the exact deployment; never create a replacement.")
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
    if options.reuse_acknowledged_readiness is not None and (not (options.readiness or options.prepare)
            or (options.provider in ("WakuOASIS", "DuckDBOASIS", "NATSJetStreamOASIS") and not options.reviewed_deployment_visitor)):
        parser.error("Readiness visitor reuse requires readiness or preparation and the retained-source deployment review where applicable.")
    if options.decentraland_deployment_visitor and (options.provider != "DecentralandOASIS"
            or not (options.readiness or options.prepare)):
        parser.error("--decentraland-deployment-visitor requires DecentralandOASIS with --readiness or --prepare.")
    if options.reviewed_deployment_visitor and (options.provider not in REVIEWED_PUBLIC_CATALOGUE_PROVIDERS
            or not (options.readiness or options.prepare) or options.decentraland_deployment_visitor):
        parser.error("--reviewed-deployment-visitor requires a reviewed public catalogue with --readiness or --prepare.")
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
    if options.decentraland_deployment_visitor:
        rehearsal = decentraland_deployment_visitor()
    if options.reviewed_deployment_visitor:
        rehearsal = reviewed_deployment_visitor(options.provider)
    if options.reuse_acknowledged_readiness is not None:
        if not options.reviewed_deployment_visitor:
            require_readiness_release()
        rehearsal = acknowledged_readiness_visitor(options.reuse_acknowledged_readiness)
    if options.provider in ("WakuOASIS", "DuckDBOASIS", "NATSJetStreamOASIS") and not options.prepare_exact_key_presets and not options.reviewed_deployment_visitor:
        parser.error("Retained-source acceptance requires its verified deployment, mounted archive and source receipt.")
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
        create_readiness_visitor = (options.localfile_deployment_visitor or options.pouchdb_deployment_visitor
                                    or options.decentraland_deployment_visitor or options.reviewed_deployment_visitor) and options.reuse_acknowledged_readiness is None
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
    if not (continuing or options.resume_unstarted_native) and any(
            (state / name).exists() for name in ("owner.intent.json", "owner.acknowledged.json",
                "owner.session.dpapi", "owner.auth-response.dpapi", "preparation-intents.json",
                "save-acknowledgements.jsonl")):
        raise RuntimeError("Original acceptance cohort already exists. Reconcile its durable journals;"
                           " cleaned TestResults cannot authorize another session, query or save.")
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
    results.mkdir(parents=True, exist_ok=True)
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
        require_native_selection(original["native"], leaf, (reviewed_source or {}).get("publicIds"))
    else:
        native = []
        for row in leaf:
            evidence = results / (suffix + "-" + row["id"] + "-native-observation.json")
            native.append(call("data/query-cql", token, row["request"], evidence_path=evidence))
        require_native_selection(native, leaf, (reviewed_source or {}).get("publicIds"))
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
        require_native_rows(public, leaf[0], 3, (reviewed_source or {}).get("publicIds"))
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

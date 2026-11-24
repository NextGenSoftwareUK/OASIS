"""Prepare a protected WEB4 settings candidate from a reviewed protected baseline."""
import argparse
import base64
import hashlib
import importlib.util
import json
import re
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
CONNECTION_DIGEST_SETTINGS = {
    "SQLServerDBOASIS": "CQL_SQLSERVER_QUERY_CONNECTION_STRING",
    "PlanetScaleOASIS": "CQL_PLANETSCALE_QUERY_CONNECTION_STRING",
    "FlyIOOASIS": "CQL_FLYIO_QUERY_CONNECTION_STRING",
    "XataOASIS": "CQL_XATA_QUERY_CONNECTION_STRING",
    "RedshiftOASIS": "CQL_REDSHIFT_QUERY_CONNECTION_STRING",
    "SnowflakeOASIS": "CQL_SNOWFLAKE_QUERY_CONNECTION_STRING",
}
REQUIRED_SOURCE_TOKENS = {
    "DaprOASIS": "CQL_DAPR_QUERY_SIDECAR_TOKEN",
    "CloudflareOASIS": "CQL_CLOUDFLARE_KV_QUERY_READER_TOKEN",
    "VercelKVOASIS": "CQL_VERCEL_KV_QUERY_READER_TOKEN",
    "DenoDeployOASIS": "CQL_DENO_KV_QUERY_READER_TOKEN",
    "AppwriteOASIS": "CQL_APPWRITE_QUERY_READER_TOKEN",
    "ConvexOASIS": "CQL_CONVEX_QUERY_READER_TOKEN",
    "TursoOASIS": "CQL_TURSO_QUERY_READER_TOKEN",
    "ENSOffchainOASIS": "CQL_ENS_OFFCHAIN_QUERY_API_KEY",
    "SafeOASIS": "CQL_SAFE_INDEXING_QUERY_API_KEY",
    "DruidOASIS": "CQL_DRUID_QUERY_READER_PASSWORD",
    "PinotOASIS": "CQL_PINOT_QUERY_READER_PASSWORD",
    "TemporalOASIS": "CQL_TEMPORAL_QUERY_API_KEY",
    "OrbitDBOASIS": "CQL_ORBITDB_QUERY_READER_TOKEN",
    "GUNOASIS": "CQL_GUN_QUERY_READER_TOKEN",
    "MilvusOASIS": "CQL_MILVUS_QUERY_READER_TOKEN",
    "ZillizOASIS": "CQL_ZILLIZ_QUERY_READER_TOKEN",
    "PgVectorOASIS": "CQL_PGVECTOR_QUERY_READER_PASSWORD",
}


def reject_json_constant(value):
    raise ValueError(f"Invalid JSON constant: {value}")


def jwt_claim_pairs(token):
    if not isinstance(token, str) or len(token) > 8192 or not re.fullmatch(r"[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", token):
        return ()
    payload = token.split(".")[1]
    try:
        decoded = base64.urlsafe_b64decode(payload + "=" * (-len(payload) % 4)).decode("utf-8")
        pairs = json.loads(decoded, object_pairs_hook=tuple, parse_constant=reject_json_constant)
    except (ValueError, UnicodeError):
        return ()
    return pairs if isinstance(pairs, tuple) else ()


def has_jwt_role(token, expected):
    return [value for key, value in jwt_claim_pairs(token) if key == "role"] == [expected]


def reviewed_chroma_source(path, receipt_path):
    probe = load("Probe-ChromaHostedReadiness")
    raw = path.read_bytes()
    source = probe.validate_source(json.loads(raw, object_pairs_hook=probe.unique))
    receipt = json.loads(receipt_path.read_bytes(), object_pairs_hook=probe.unique)
    if (not isinstance(receipt, dict) or receipt_path.with_name("qualification.failed.json").exists()
            or receipt.get("sourceSha256") != hashlib.sha256(raw).hexdigest()
            or receipt.get("nativeCounts") != [3, 2] or receipt.get("requests") != 3
            or type(receipt.get("writes")) is not int or receipt["writes"] != 0):
        raise ValueError("Chroma requires the matching native-read qualification receipt.")
    binding = {"CQL_CHROMA_QUERY_" + setting: source[field] for setting, field in
               (("ORIGIN", "origin"), ("TENANT", "tenant"), ("DATABASE", "database"),
                ("COLLECTION_ID", "collectionId"))}
    return {"sourceBinding": binding, "presets": verifier.reviewed_record_presets("ChromaOASIS", source["ids"])}


def candidate(before, provider, review):
    if provider not in (*verifier.REVIEWED_EXACT_KEY_PROVIDERS, "ChromaOASIS"):
        raise ValueError("An explicitly prepared provider is required.")
    dna = json.loads(before["OASIS_DNA_JSON"], object_pairs_hook=verifier.unique_catalogue_properties)["OASIS"]
    if dna["Sandbox"]["Enabled"] is not True:
        raise ValueError("An enabled WEB4 sandbox baseline is required.")
    metadata_key = review.get("requiredQueryableMetadataKey")
    encryption = (dna.get("Security") or {}).get("HolonDataEncryption") or {}
    queryable_keys = encryption.get("AdditionalQueryableKeys")
    if (metadata_key is not None
            and (encryption.get("Rijndael256EncryptionEnabled") is True or encryption.get("QuantumEncryptionEnabled") is True)
            and (not isinstance(queryable_keys, list) or metadata_key not in queryable_keys)):
        raise ValueError("The encrypted Mongo metadata key must be explicitly queryable.")
    bindings = review.get("sourceBinding", {})
    new_source = {}
    source_setting = {"SignProtocolOASIS": "CQL_SIGN_PUBLIC_SOURCE_JSON",
                      "EASOASIS": "CQL_EAS_PUBLIC_SOURCE_JSON", "FlowOASIS": "CQL_FLOW_PUBLIC_SOURCE_JSON",
                      "RoninOASIS": "CQL_RONIN_PUBLIC_SOURCE_JSON",
                      "ImmutableZkEVMOASIS": "CQL_IMMUTABLE_ZKEVM_PUBLIC_SOURCE_JSON"}.get(provider)
    if source_setting and source_setting not in before:
        new_source = {source_setting: bindings[source_setting]}
    managed_holo = provider == "HoloOASIS" and review.get("managedLocalHoloOasis") is True
    if managed_holo:
        if any(key.startswith("CQL_HOLOOASIS_QUERY__") for key in before):
            raise ValueError("Managed HoloOASIS requires an empty external reader configuration.")
        new_source = {key: value for key, value in bindings.items() if key not in before}
    if any((before | new_source).get(key) != value for key, value in bindings.items()):
        raise ValueError("Protected source settings do not match the reviewed teaching catalogue.")
    if provider in REQUIRED_SOURCE_TOKENS:
        token = before.get(REQUIRED_SOURCE_TOKENS[provider])
        maximum_length = {"DenoDeployOASIS": 8192, "TursoOASIS": 8192, "SafeOASIS": 4096, "TemporalOASIS": 4096}.get(provider, 1024)
        if (not isinstance(token, str) or not 1 <= len(token) <= maximum_length
                or any(not 33 <= ord(character) <= 126 for character in token)):
            raise ValueError("A valid protected source token is required.")
        if provider in ("ENSOffchainOASIS", "TemporalOASIS") and "," in token:
            raise ValueError("A valid protected source token is required.")
        if provider == "SafeOASIS" and not re.fullmatch(r"[A-Za-z0-9._-]{1,4096}", token):
            raise ValueError("A valid protected source token is required.")
        if provider in ("ConvexOASIS", "OrbitDBOASIS", "GUNOASIS") and not re.fullmatch(r"[A-Za-z0-9_-]{32,256}", token):
            raise ValueError("A valid protected source token is required.")
    admission = json.loads(before[ADMISSION], object_pairs_hook=verifier.unique_catalogue_properties)
    grants = json.loads(before.get(GRANTS, "{}"), object_pairs_hook=verifier.unique_catalogue_properties)
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
    grants[provider] = review["publicIds"] if metadata_key is not None else full["request"]["filters"][0]["value"]
    updates = {**new_source, ADMISSION: configuration.compact(admission), GRANTS: configuration.compact(grants),
               partition: configuration.compact(presets)}
    if any(len(value.encode()) > 32768 for value in updates.values()):
        raise ValueError("Candidate exceeds the hosted variable byte budget.")
    return before | updates


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("provider", choices=(*verifier.REVIEWED_EXACT_KEY_PROVIDERS, "ChromaOASIS"))
    parser.add_argument("--catalogue", required=True, type=Path)
    parser.add_argument("--also", action="append", nargs=2, default=[], metavar=("PROVIDER", "CATALOGUE"),
                        help="Add another reviewed exact-key provider to the same protected candidate.")
    parser.add_argument("--chroma-qualification", type=Path, help="Chroma native read acknowledgement.")
    parser.add_argument("--baseline", required=True, type=Path, help="Reviewed DPAPI settings snapshot.")
    parser.add_argument("--output", required=True, type=Path, help="New DPAPI candidate path.")
    args = parser.parse_args()
    if args.baseline.suffix.lower() != ".dpapi" or args.output.suffix.lower() != ".dpapi":
        parser.error("Use protected .dpapi settings files.")
    if (args.provider == "ChromaOASIS") != (args.chroma_qualification is not None):
        parser.error("--chroma-qualification is required for ChromaOASIS only.")
    review = (reviewed_chroma_source(args.catalogue, args.chroma_qualification) if args.chroma_qualification
              else verifier.reviewed_exact_key_source(args.provider, args.catalogue))
    protect = configuration.verification.protected_token
    baseline = json.loads(protect(args.baseline.read_bytes(), decrypt=True), object_pairs_hook=verifier.unique_catalogue_properties)
    prepared = candidate(baseline, args.provider, review)
    providers = [args.provider]
    for provider, catalogue in args.also:
        if provider not in verifier.REVIEWED_EXACT_KEY_PROVIDERS or provider in providers:
            parser.error("--also requires a distinct reviewed exact-key provider.")
        prepared = candidate(prepared, provider, verifier.reviewed_exact_key_source(provider, Path(catalogue)))
        providers.append(provider)
    configuration.write_new(args.output, protect(configuration.compact(prepared).encode()))
    changed = sum(baseline.get(key) != value for key, value in prepared.items())
    print(f"Prepared protected {', '.join(providers)} candidate with {changed} settings changes.")


if __name__ == "__main__":
    main()

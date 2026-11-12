import base64
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import unittest
import uuid
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("exact_key_configuration",
    Path(__file__).with_name("Prepare-HostedExactKeyConfiguration.py"))
configuration = importlib.util.module_from_spec(spec)
spec.loader.exec_module(configuration)


def exact_key_catalogue(provider):
    actor, first, second, third, deleted, missing = [str(uuid.UUID(int=i)) for i in range(1, 7)]
    if provider == "NATSJetStreamOASIS":
        catalogue = {"Actor": actor, "LiveIds": [first, second, third]}
    elif provider == "WakuOASIS":
        actor = "a5808c3e-7031-47e7-92de-48a659568f1b"
        first, second, third = ("8ff38484-cb3f-834b-a77e-8e7321130e7c", "5c75110a-020a-8e6a-bdae-374cb85b5d39",
                                "38f0dce7-ca39-82ae-90ae-a1409202a581")
        catalogue = {"Actor": actor, "MessageHashes": ["0x" + value * 64 for value in "123"]}
    elif provider == "ENSOffchainOASIS":
        actor = "a5808c3e-7031-47e7-92de-48a659568f1b"
        first, second, third = ("2d8b5e78-3137-8c0d-ba70-bdaa880dcdad", "e29d23d8-4c2c-8e0a-9fde-5b92d31135c0",
                                "ffab761f-bf16-8e02-88e4-1e8a7b6cccd9")
        catalogue = {"Actor": actor, "Names": ["vitalik.eth", "nick.eth", "luc.eth"]}
    elif provider in ("DiscordOASIS", "LivepeerOASIS", "PinataOASIS", "PushProtocolOASIS", "LayerZeroOASIS", "WorldIDOASIS", "ReclaimProtocolOASIS", "DecentralandOASIS", "TheGraphOASIS", "SubqueryOASIS", "SelfProtocolOASIS", "HolonymOASIS", "CivicOASIS", "MapLibreOASIS", "GOMapOASIS", "TheSandboxOASIS", "PolygonIDOASIS", "SEEDSOASIS", "IntelOpenVINOOASIS"):
        actor = "a5808c3e-7031-47e7-92de-48a659568f1b"
        fixed_ids = {
            "GOMapOASIS": ("f2c0c826-066e-8a9c-aaec-edafe1a16fdf", "ba0c3bc1-920a-8f73-8c34-51a1cfc9d6e3", "d25cf63e-14cb-8a85-aa80-c1ee36b1f1b2"),
            "IntelOpenVINOOASIS": ('76dc34ff-dbbc-8541-9fd4-6708c14086a9', '9a25a55f-5a66-85d0-9700-891eacd87dd6', 'acd7cd30-fffe-844a-a048-a20e67e1afed'),
            "SEEDSOASIS": ('999101fa-bdd3-8b42-969e-eafeaaad3e64', 'e8b73bc1-15f5-8dad-b92d-47b8750638eb', '743062bf-03df-81d3-a29b-6eb015a80858'),
            "PolygonIDOASIS": ('b33f9708-a3da-805c-8d7f-73fc6c7980ea', '395a8ace-db5c-81d0-a31b-2b4b83f27d40', 'c972e7a0-677c-8b43-b2bb-fe90da46f9d1'),
            "TheSandboxOASIS": ('84cbd2a6-dde5-8895-ac30-17aa35d25972', 'b019be67-c823-85fd-80a7-9d33925b9990', 'f5412ace-a25a-8f42-bb73-64c3ac55616a'),
            "MapLibreOASIS": ('b0f5d38d-cc44-8e60-93b2-e201be79f2b9', '025230b9-0731-8dca-8443-af589d04f4cd', '2f4cf1cc-ed1b-85e2-88fc-75930d4b582a'),
            "CivicOASIS": ('fb19f71f-ed17-8a0c-b2dc-b0f5074a6888', '863752f8-cc68-8ebe-bf88-4b95f8921590', '3e9729d9-0057-82ea-8335-a83ad0fa8f8d'),
            "HolonymOASIS": ('47ea43fd-6800-8894-a3a1-fb4cdbe5060a', '2cdd8175-b963-8977-8fc7-2e43b22096bc', '4996e5c3-15a9-844e-9434-ef3fb9345470'),
            "SelfProtocolOASIS": ('afc6163f-f72e-8c3c-a270-33c1cb3a4d69', 'b69b4b29-3147-8a0e-8042-45e96f96d883', '7995517f-88ba-8834-8c5d-1063dba989f0'),
            "SubqueryOASIS": ("03d65d4b-1b61-8192-b4e0-3b74a698eb8d", "ea697e06-ad86-80f4-a0cc-3ba141d9c458",
                              "1b6bec6e-d75a-8f47-8815-3c00fbb271c9"),
            "TheGraphOASIS": ("d34efac2-6513-855c-ac40-35bb83ea952e", "d76a1024-c71f-8527-96e5-4def4df3633b",
                              "f3d72db4-5b1f-8daf-a8bb-e9658a8b6b8b"),
            "DiscordOASIS": ("ca36c2c9-350d-8603-ab00-1c8e1f2a9d4f", "d5c8ecfd-cc3e-8ec7-9697-0e2ea2c96ab2",
                             "9b9fe5bf-0664-8175-a1d7-4da37799db4f"),
            "LivepeerOASIS": ("083bc10d-dcca-860f-ae09-71c4d986966c", "64a23e52-84b0-8115-a142-3ba828e40a40",
                              "292de706-63f2-8d28-b209-cf7e0fb55534"),
            "PinataOASIS": ("222a3496-a5a9-8781-8106-8e45fa4b9139", "54d558da-603a-8928-abe8-274107216614",
                            "08782187-82e4-8387-9b33-7504c5c31fcf"),
            "PushProtocolOASIS": ("f651c8a7-7507-8700-8b31-afec251f38fe", "a07e52f6-c0a0-8b28-841e-7e417cb7fde2",
                                  "9d4be1b3-f70c-8592-b6a6-430b8db24cc5"),
            "LayerZeroOASIS": ("47781bed-c8a3-87e6-bf48-55748f3394fd", "171ea793-8d0e-8d1b-b01b-b7d4ed1a322d",
                              "8a3da627-e33b-8baf-9c62-9e254b9ba743"),
            "WorldIDOASIS": ("442492b5-6f60-8889-a9b5-b50c92699abd", "7e46076e-5290-81eb-a5b8-6769d51ed6bc",
                             "f280265a-092b-873a-8a08-cb0b310a78ee"),
            "ReclaimProtocolOASIS": ("fce17b97-a4ac-89fb-9aef-3f142df2e283", "69b8526d-4393-8806-8eb3-ca6db9216b61",
                                     "b3e84f9d-d919-84ea-9c26-7bf7e7dbd880"),
            "DecentralandOASIS": ("98143b52-310f-8ab0-80f1-374220d2d3b6", "39df5acf-2867-8bdf-9f4a-f543417cee2b",
                                   "ba775270-3c2f-8f43-9f40-b16fe76a393e"),
        }
        first, second, third = fixed_ids[provider]
        catalogue = {"Actor": actor}
        if provider == "GOMapOASIS":
            catalogue["Origin"] = "https://routing.example"
    elif provider in ("TenderlyOASIS", "QuickNodeOASIS"):
        first, second, third = ("0x0000000000000000000000000000000000000000",
                                "0x000000000000000000000000000000000000dead",
                                "0x0000000000000000000000000000000000001010")
        catalogue = {"Actor": actor}
    elif provider == "TemporalOASIS":
        actor = "a5808c3e-7031-47e7-92de-48a659568f1b"
        first, second, third = ("6bef46ce-5f1e-8506-93fe-40f019780879", "2ac8bd1a-cd32-847f-acd4-b34a7ac373e9",
                                "2a70632f-2884-8b80-baad-0e94175be318")
        catalogue = {"Actor": actor, "Namespace": "oasis-cql.exampleaccount", "Runs": [
            {"WorkflowId": "oasis-teaching-" + name, "RunId": f"30000000-0000-4000-8000-{position:012d}"}
            for position, name in enumerate(("first", "second", "third"), 1)
        ]}
    elif provider == "SafeOASIS":
        actor = "a5808c3e-7031-47e7-92de-48a659568f1b"
        first, second, third = ("64ed5973-5b49-88b2-81ae-159cb2d12859", "ce0eac74-96b1-8d63-893b-c8bb9adfa0a5",
                                "54614170-6ac0-86aa-a522-d4b1ecc08359")
        catalogue = {"Actor": actor, "Networks": ["eth", "base", "arb1"]}
    elif provider == "CloudinaryOASIS":
        actor = "a5808c3e-7031-47e7-92de-48a659568f1b"
        first, second, third = ("8aee1458-13a7-8822-bfc3-670a6383d512", "12043da4-2792-8476-9786-7089dce330b8",
                                "b2b331e8-2252-8910-b5bd-f56954d50e04")
        catalogue = {"Actor": actor, "Assets": ["sample.jpg", "lady.jpg", "woman.jpg"]}
    elif provider == "ProofOfHumanityOASIS":
        actor = "a5808c3e-7031-47e7-92de-48a659568f1b"
        first, second, third = ("34f0383f-8729-8509-b64b-59a39fa08dbd", "1c7b7bd9-cfc1-8266-938c-e74b622523b1",
                                "e74b484b-7e05-8e32-98e8-5fb0c649f389")
        catalogue = {"Actor": actor, "Addresses": ["0x16044e1063c08670f8653055a786b7cc2034d2b0",
                                                   "0xa478095886659168e8812154fb0de39f103e74b2",
                                                   "0xc5e9ddebb09cd64dfacab4011a0d5cedaf7c9bdb"]}
    elif provider == "OracleObjectStorageOASIS":
        actor = "a5808c3e-7031-47e7-92de-48a659568f1b"
        first, second, third = ("912fbb3f-1022-85df-bb65-8dde05c94d6d", "f7ab669f-496d-851b-a14b-5166a97011cf",
                                "4d68cf50-9505-8de5-91bc-d149d4404145")
        catalogue = {"Actor": actor, "Keys": ["bds-livelabs/env.sh", "bds-livelabs/download-all-hdfs-data.sh",
                                            "bds-livelabs/download-all-objstore.sh"]}
    else:
        catalogue = {"Actor": actor, "LiveIds": [first, second, third], "DeletedId": deleted, "MissingId": missing}
    fields = {
        "DruidOASIS": {"Origin": "https://druid.example.com", "ReaderUsername": "cql_reader"},
        "PinotOASIS": {"Origin": "https://pinot.example.com", "ReaderUsername": "cql_reader"},
        "FastlyOASIS": {"StoreId": "teachingStore"},
        "DaprOASIS": {"Store": "cosmic-teaching", "GrpcEndpoint": "http://127.0.0.1:54862/"},
        "CloudflareOASIS": {"AccountId": "a" * 32, "NamespaceId": "b" * 32},
        "VercelKVOASIS": {"Endpoint": "https://teaching.upstash.io/"},
        "DenoDeployOASIS": {"DatabaseId": str(uuid.UUID(int=7)), "DataEndpoint": "https://api.deno.com/v2/teaching"},
        "AppwriteOASIS": {"Endpoint": "https://appwrite.example/v1", "ProjectId": "teaching", "DatabaseId": "records"},
        "ConvexOASIS": {"Site": "https://teaching.convex.site/"},
        "TursoOASIS": {"Origin": "https://teaching.turso.io/"},
        "OrbitDBOASIS": {"Sidecar": "http://127.0.0.1:54869/"},
        "GUNOASIS": {"Sidecar": "http://127.0.0.1:54870/"},
        "MilvusOASIS": {"Origin": "https://milvus.example.com"},
        "PgVectorOASIS": {"Host": "teaching.pgvector.example", "Database": "oasis_teaching",
                          "ReaderUsername": "cosmic_reader"},
        "NetlifyBlobsOASIS": {"SiteId": str(uuid.UUID(int=7)), "Store": "oasis", "ObjectOrigin": "https://objects.example"},
        "NileOASIS": {"Host": "us-west-2.db.thenile.dev", "Database": "oasis_teaching"},
        "AlgoliaOASIS": {"AppId": "Teaching123"},
        "NeonOASIS": {"Host": "ep-demo.us-east-2.aws.neon.tech"},
        "LanceDBOASIS": {"Origin": "https://teaching.api.lancedb.com", "Database": "oasis"},
        "PineconeOASIS": {"Origin": "https://teaching.svc.pinecone.io"},
        "LitestreamOASIS": {"RestoreSha256": "a" * 64},
        "ScyllaDBOASIS": {"Ip": "8.8.8.8", "Host": "node.teaching.clusters.scylla.cloud", "CaSha256": "a" * 64},
        "RavenDBOASIS": {"Origin": "https://a.free.teaching.ravendb.cloud", "CertificateSha256": "a" * 64},
        "CassandraOASIS": {"BundleSha256": "a" * 64},
        "TimescaleDBOASIS": {"Host": "service.tsdb.cloud.tigerdata.com", "Port": 32625},
        "CockroachDBOASIS": {"Host": "oasis-demo.aws-us-east-1.cockroachlabs.cloud"},
        "TigrisOASIS": {"Endpoint": "https://t3.storage.dev", "Bucket": "teaching"},
        "DynamoDBOASIS": {"Region": "us-east-1"},
        "MinIOOASIS": {"Endpoint": "https://minio.example.test", "Bucket": "teaching", "Region": "us-east-1"},
    }
    catalogue.update(fields.get(provider, {}))
    if provider in ("TigrisOASIS", "MinIOOASIS"):
        catalogue["Originals"] = [{"Id": key, "Sha256": "a" * 64, "ETag": "controlled-etag"}
                                  for key in (first, second, third, deleted)]
    return catalogue, (actor, first, second, third, deleted, missing)


class ExactKeyConfigurationTests(unittest.TestCase):
    def test_holooasis_binds_reviewed_cell_and_keeps_signing_credentials_protected(self):
        provider = "HoloOASIS"
        prefix = "CQL_HOLOOASIS_QUERY__"
        catalogue, (_, first, second, third, _, _) = exact_key_catalogue(provider)
        catalogue.update({
            "Endpoint": "wss://conductor.example.test/app", "InstalledAppId": "oasis-demo",
            "DnaHash": base64.b64encode(b"d" * 39).decode(),
            "AgentKey": base64.b64encode(b"a" * 39).decode(),
        })
        with tempfile.TemporaryDirectory() as directory:
            descriptor = Path(directory) / "catalogue.json"
            descriptor.write_text(json.dumps(catalogue | {"Endpoint": "ws://conductor.example.test/app"}))
            with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            descriptor.write_text(json.dumps(catalogue))
            review = configuration.verifier.reviewed_exact_key_source(provider, descriptor)

        credentials = {prefix + key: base64.b64encode(key.encode()[:1] * length).decode()
                       for key, length in (("AppToken", 32), ("CapabilitySecret", 64),
                                           ("SigningKey", 39), ("SigningPrivateKey", 64))}
        baseline = {
            "OASIS_DNA_JSON": '{"OASIS":{"Sandbox":{"Enabled":true}}}',
            configuration.ADMISSION: json.dumps({
                "ordinaryProviders": ["MongoDBOASIS"], "nativeProviders": [],
                "queryOnlyProviders": ["IPFSOASIS"],
                "queryTariffs": [{"provider": "IPFSOASIS", "costUsd": 0}],
            }),
        } | review["sourceBinding"] | credentials
        self.assertEqual(first, review["sourceBinding"][prefix + "ProbeId"])
        with self.assertRaisesRegex(ValueError, "do not match"):
            configuration.candidate(baseline | {prefix + "AgentKey": catalogue["DnaHash"]}, provider, review)
        with self.assertRaisesRegex(ValueError, "protected SigningPrivateKey"):
            configuration.candidate(baseline | {prefix + "SigningPrivateKey": "invalid"}, provider, review)
        result = configuration.candidate(baseline, provider, review)
        self.assertEqual([first, second, third], json.loads(result[configuration.GRANTS])[provider])
        self.assertEqual(credentials, {key: result[key] for key in credentials})
        presets = result["CQL_HOSTED_QUERY_EXAMPLES__" + provider]
        self.assertEqual(4, len(json.loads(presets)))
        for secret in credentials.values():
            self.assertNotIn(secret, presets)

    def test_holooasis_managed_catalogue_generates_admission_without_external_credentials(self):
        provider = "HoloOASIS"
        catalogue, (_, first, second, third, _, _) = exact_key_catalogue(provider)
        with tempfile.TemporaryDirectory() as directory:
            descriptor = Path(directory) / "catalogue.json"
            descriptor.write_text(json.dumps(catalogue | {"Mode": "ManagedLocal"}))
            review = configuration.verifier.reviewed_exact_key_source(provider, descriptor)

        baseline = {
            "OASIS_DNA_JSON": '{"OASIS":{"Sandbox":{"Enabled":true}}}',
            configuration.ADMISSION: json.dumps({
                "ordinaryProviders": ["MongoDBOASIS"], "nativeProviders": [],
                "queryOnlyProviders": ["IPFSOASIS"],
                "queryTariffs": [{"provider": "IPFSOASIS", "costUsd": 0}],
            }),
        }
        result = configuration.candidate(baseline, provider, review)
        self.assertEqual("true", result["CQL_HOLOOASIS_LOCAL_DEMO__Enabled"])
        self.assertEqual("/data/COSMIC/HoloOasisCql/tools", result["HOLONET_CONDUCTOR_FOLDER"])
        self.assertEqual([first, second, third], json.loads(result[configuration.GRANTS])[provider])
        self.assertFalse(any(key.startswith("CQL_HOLOOASIS_QUERY__") for key in result))
        with self.assertRaisesRegex(ValueError, "empty external reader configuration"):
            configuration.candidate(baseline | {"CQL_HOLOOASIS_QUERY__Endpoint": "wss://external.example/"}, provider, review)

    def test_candidate_preserves_existing_providers_and_refuses_replacement(self):
        self.check_candidate("QdrantOASIS")

    def test_dynamodb_candidate_binds_region_probe_and_protected_reader(self):
        self.check_candidate("DynamoDBOASIS")

    def test_weaviate_candidate_preserves_existing_providers(self):
        self.check_candidate("WeaviateOASIS")

    def test_tigris_candidate_preserves_existing_providers(self):
        self.check_candidate("TigrisOASIS")

    def test_redis_family_candidates_preserve_existing_providers(self):
        for provider in ("GarnetOASIS", "KeyDBOASIS", "DragonflyOASIS"):
            with self.subTest(provider=provider):
                self.check_candidate(provider)

    def test_upstash_candidate_preserves_existing_providers(self):
        self.check_candidate("UpstashOASIS")

    def test_influxdb_candidate_preserves_existing_providers(self):
        self.check_candidate("InfluxDBOASIS")

    def test_couchbase_candidate_preserves_existing_providers(self):
        self.check_candidate("CouchbaseOASIS")

    def test_arangodb_candidate_preserves_existing_providers(self):
        self.check_candidate("ArangoDBOASIS")

    def test_cockroachdb_candidate_preserves_existing_providers(self):
        self.check_candidate("CockroachDBOASIS")

    def test_timescaledb_candidate_preserves_existing_providers(self):
        self.check_candidate("TimescaleDBOASIS")

    def test_cassandra_candidate_preserves_existing_providers(self):
        self.check_candidate("CassandraOASIS")

    def test_ravendb_candidate_preserves_existing_providers(self):
        self.check_candidate("RavenDBOASIS")

    def test_scylladb_candidate_preserves_existing_providers(self):
        self.check_candidate("ScyllaDBOASIS")

    def test_litestream_candidate_preserves_existing_providers(self):
        self.check_candidate("LitestreamOASIS")

    def test_pinecone_candidate_preserves_existing_providers(self):
        self.check_candidate("PineconeOASIS")

    def test_lancedb_candidate_preserves_existing_providers(self):
        self.check_candidate("LanceDBOASIS")

    def test_neon_candidate_preserves_existing_providers(self):
        self.check_candidate("NeonOASIS")

    def test_algolia_candidate_preserves_existing_providers(self):
        self.check_candidate("AlgoliaOASIS")

    def test_nile_candidate_preserves_existing_providers(self):
        self.check_candidate("NileOASIS")

    def test_netlify_blobs_candidate_preserves_existing_providers(self):
        self.check_candidate("NetlifyBlobsOASIS")

    def test_fastly_candidate_preserves_existing_providers(self):
        self.check_candidate("FastlyOASIS")

    def test_dapr_candidate_preserves_existing_providers(self):
        self.check_candidate("DaprOASIS")

    def test_cloudflare_kv_candidate_preserves_existing_providers(self):
        self.check_candidate("CloudflareOASIS")

    def test_migrated_vercel_kv_candidate_preserves_existing_providers(self):
        self.check_candidate("VercelKVOASIS")

    def test_deno_kv_candidate_preserves_existing_providers(self):
        self.check_candidate("DenoDeployOASIS")

    def test_appwrite_candidate_preserves_existing_providers(self):
        self.check_candidate("AppwriteOASIS")

    def test_convex_candidate_preserves_existing_providers(self):
        self.check_candidate("ConvexOASIS")

    def test_turso_candidate_preserves_existing_providers(self):
        self.check_candidate("TursoOASIS")

    def test_ens_offchain_candidate_binds_the_native_name_identities(self):
        self.check_candidate("ENSOffchainOASIS")

    def test_protected_sql_candidates_bind_dedicated_https_origin_and_reader(self):
        for provider in ("DruidOASIS", "PinotOASIS"):
            with self.subTest(provider=provider):
                self.check_candidate(provider)

    def test_temporal_candidate_binds_exact_workflow_runs_and_protected_key(self):
        self.check_candidate("TemporalOASIS")

    def test_fixed_public_network_candidates_bind_only_the_reviewed_owner(self):
        for provider in ("DiscordOASIS", "LivepeerOASIS", "PinataOASIS", "PushProtocolOASIS", "LayerZeroOASIS", "WorldIDOASIS", "ReclaimProtocolOASIS", "DecentralandOASIS", "TheGraphOASIS", "SubqueryOASIS", "SelfProtocolOASIS", "HolonymOASIS", "CivicOASIS", "MapLibreOASIS", "GOMapOASIS", "TheSandboxOASIS", "PolygonIDOASIS", "SEEDSOASIS", "IntelOpenVINOOASIS"):
            with self.subTest(provider=provider):
                self.check_candidate(provider)
                catalogue, _ = exact_key_catalogue(provider)
                with tempfile.TemporaryDirectory() as directory:
                    descriptor = Path(directory) / "catalogue.json"
                    descriptor.write_text(json.dumps(catalogue | {"Keys": ["foreign"]}))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)

    def test_gomap_refuses_unreviewed_or_malformed_origins(self):
        catalogue, _ = exact_key_catalogue("GOMapOASIS")
        cases = ({}, {"Origin": None}, {"Origin": "http://routing.example"},
                 {"Origin": "https://router.project-osrm.org"},
                 {"Origin": "https://routing.openstreetmap.de."},
                 {"Origin": "https://routing.example:65536"},
                 {"Origin": "https://routing.example/route"},
                 {"Origin": "https://."},
                 {"Origin": "https://routing..example"},
                 {"Origin": "https://-routing.example"},
                 {"Origin": "https://user@routing.example"})
        with tempfile.TemporaryDirectory() as directory:
            descriptor = Path(directory) / "catalogue.json"
            for fields in cases:
                with self.subTest(fields=fields):
                    descriptor.write_text(json.dumps({"Actor": catalogue["Actor"]} | fields))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source("GOMapOASIS", descriptor)

    def test_infura_catalogue_keeps_credentials_out_of_reviewed_presets(self):
        actor = "a5808c3e-7031-47e7-92de-48a659568f1b"
        with tempfile.TemporaryDirectory() as directory:
            descriptor = Path(directory) / "catalogue.json"
            descriptor.write_text(json.dumps({"Actor": actor}))
            review = configuration.verifier.reviewed_exact_key_source("InfuraOASIS", descriptor)
            self.assertEqual({"CQL_INFURA_QUERY_SOURCE_OWNER": actor}, review["sourceBinding"])
            self.assertEqual("InfuraEthereumAccountAddress", review["requiredQueryableMetadataKey"])
            keys = review["presets"][0]["request"]["filters"][0]["value"]
            self.assertEqual("0x00000000219ab540356cbb839cbe05303d7705fa", keys[2])
            self.assertEqual([
                "ef429e7f-cebb-8538-87b2-5a57b6884bf2",
                "8c2689e4-73e9-8faa-9d18-b70d4e4486fa",
                "a4669a44-3d6d-826b-91aa-1dae1e62fc03",
            ], review["publicIds"])
            descriptor.write_text(json.dumps({"Actor": actor, "ApiKey": "controlled"}))
            with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                configuration.verifier.reviewed_exact_key_source("InfuraOASIS", descriptor)

    def test_polygon_gateway_presets_use_addresses_and_separate_logical_authority(self):
        actor = "a5808c3e-7031-47e7-92de-48a659568f1b"
        gateways = {
            "TenderlyOASIS": ("CQL_TENDERLY_POLYGON_QUERY_SOURCE_OWNER", "TenderlyPolygonAccountAddress",
                             ["c732827d-bd7d-8bca-b4ae-fcdd85e5fd1f", "7894d368-40af-8bd4-bb06-dc7ad2221ac2",
                              "c24a2017-5ab6-8c17-8237-6fd566f83df7"]),
            "QuickNodeOASIS": ("CQL_QUICKNODE_POLYGON_QUERY_SOURCE_OWNER", "QuickNodePolygonAccountAddress",
                               ["bc0eda6d-cd8b-82eb-9f6f-d0b2462b1696", "afdf8c23-f1cc-80b6-9249-50a32c36c9cc",
                                "9aff6b7e-66b8-8b21-82bf-ea37e8400bcf"]),
        }
        with tempfile.TemporaryDirectory() as directory:
            descriptor = Path(directory) / "gateway.json"
            descriptor.write_text(json.dumps({"Actor": actor}))
            for provider, (setting, metadata, ids) in gateways.items():
                with self.subTest(provider=provider):
                    review = configuration.verifier.reviewed_exact_key_source(provider, descriptor)
                    self.assertEqual({setting: actor}, review["sourceBinding"])
                    self.assertEqual(ids, review["publicIds"])
                    self.assertEqual(metadata, review["requiredQueryableMetadataKey"])
                    for row in review["presets"]:
                        selection = row["request"].get("source", row["request"])
                        count = 2 if row["id"].endswith("-subset") else 3
                        self.assertEqual([{"metadataPath": [metadata], "scalar": "String", "operator": "In",
                                           "value": list(configuration.verifier.POLYGON_GATEWAY_KEYS[:count])}],
                                         selection["filters"])
                    native = {"result": {"rows": [{"id": logical_id, "metadata": {metadata: key}}
                              for logical_id, key in zip(ids, configuration.verifier.POLYGON_GATEWAY_KEYS)]}}
                    configuration.verifier.require_native_rows(native, review["presets"][0], 3, ids)
                    rows = native["result"]["rows"]
                    rows[0]["id"], rows[1]["id"] = rows[1]["id"], rows[0]["id"]
                    with self.assertRaisesRegex(RuntimeError, "reviewed metadata keys"):
                        configuration.verifier.require_native_rows(native, review["presets"][0], 3, ids)
                    rows[0]["id"] = str(uuid.UUID(int=2))
                    with self.assertRaisesRegex(RuntimeError, "reviewed public authority"):
                        configuration.verifier.require_native_rows(native, review["presets"][0], 3, ids)
                    encryption = {"Rijndael256EncryptionEnabled": True}
                    dna = {"OASIS": {"Sandbox": {"Enabled": True}, "Security": {"HolonDataEncryption": encryption}}}
                    baseline = {setting: actor, "OASIS_DNA_JSON": json.dumps(dna), configuration.ADMISSION: json.dumps({
                        "queryOnlyProviders": ["IPFSOASIS"], "ordinaryProviders": ["MongoDBOASIS"],
                        "nativeProviders": [], "queryTariffs": [{"provider": "IPFSOASIS", "costUsd": 0}]
                    })}
                    with self.assertRaisesRegex(ValueError, "explicitly queryable"):
                        configuration.candidate(baseline, provider, review)
                    encryption["AdditionalQueryableKeys"] = [metadata]
                    baseline["OASIS_DNA_JSON"] = json.dumps(dna)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {setting: str(uuid.UUID(int=2))}, provider, review)
                    prepared = configuration.candidate(baseline, provider, review)
                    self.assertEqual(ids, json.loads(prepared[configuration.GRANTS])[provider])
                    self.assertEqual(baseline["OASIS_DNA_JSON"], prepared["OASIS_DNA_JSON"])

    def test_safe_indexing_candidate_binds_reviewed_networks_and_protected_key(self):
        self.check_candidate("SafeOASIS")

    def test_loopback_teaching_candidates_bind_each_sidecar(self):
        for provider in ("OrbitDBOASIS", "GUNOASIS"):
            with self.subTest(provider=provider):
                self.check_candidate(provider)

    def test_milvus_candidate_binds_the_https_origin_and_probe(self):
        self.check_candidate("MilvusOASIS")
        with tempfile.TemporaryDirectory() as directory:
            descriptor = Path(directory) / "catalogue.json"
            catalogue, _ = exact_key_catalogue("MilvusOASIS")
            for origin in ("https://127.0.0.1", "http://milvus.example.com", "https://milvus.example.com/"):
                with self.subTest(origin=origin):
                    descriptor.write_text(json.dumps(catalogue | {"Origin": origin}))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source("MilvusOASIS", descriptor)

    def test_cloudinary_candidate_binds_the_native_image_identities(self):
        self.check_candidate("CloudinaryOASIS")
        with tempfile.TemporaryDirectory() as directory:
            descriptor = Path(directory) / "catalogue.json"
            catalogue, _ = exact_key_catalogue("CloudinaryOASIS")
            for assets in (["sample.jpg", "sample.jpg", "woman.jpg"], ["../sample.jpg", "lady.jpg", "woman.jpg"]):
                with self.subTest(assets=assets):
                    descriptor.write_text(json.dumps(catalogue | {"Assets": assets}))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source("CloudinaryOASIS", descriptor)

    def test_pgvector_candidate_binds_host_database_and_reader(self):
        self.check_candidate("PgVectorOASIS")

    def test_public_registry_and_object_candidates_bind_stable_identities(self):
        for provider, field, invalid in (("ProofOfHumanityOASIS", "Addresses", "0xA" + "0" * 39),
                                         ("OracleObjectStorageOASIS", "Keys", "bds-livelabs/../env.sh")):
            with self.subTest(provider=provider):
                self.check_candidate(provider)
                with tempfile.TemporaryDirectory() as directory:
                    descriptor = Path(directory) / "catalogue.json"
                    catalogue, _ = exact_key_catalogue(provider)
                    catalogue[field][0] = invalid
                    descriptor.write_text(json.dumps(catalogue))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)

    def check_candidate(self, provider):
        partition = "CQL_HOSTED_QUERY_EXAMPLES__" + provider
        baseline = {
            "OASIS_DNA_JSON": '{"OASIS":{"Sandbox":{"Enabled":true}}}',
            configuration.ADMISSION: json.dumps({
                "ordinaryProviders": ["MongoDBOASIS"], "nativeProviders": [],
                "queryOnlyProviders": ["IPFSOASIS"],
                "queryTariffs": [{"provider": "IPFSOASIS", "costUsd": 0, "tariffVersion": "demo-read-v1"}],
            }),
            configuration.GRANTS: json.dumps({"IPFSOASIS": ["retained"]}),
            configuration.EXAMPLES: json.dumps({"IPFSOASIS": ["retained"]}),
            "UNRELATED_SETTING": "retained",
        }
        with tempfile.TemporaryDirectory() as directory:
            descriptor = Path(directory) / "catalogue.json"
            catalogue, (actor, first, second, third, deleted, missing) = exact_key_catalogue(provider)
            if provider == "ENSOffchainOASIS":
                for names in (["VITALIK.eth", "nick.eth", "luc.eth"], ["vitalik.eth", "nick.eth", "nick.eth"],
                              ["vitalik.eth", "nick.eth", "other/foreign.eth"], ["vitalik.eth"]):
                    descriptor.write_text(json.dumps(catalogue | {"Names": names}))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "FastlyOASIS":
                for store in ("", "../foreign", "foreign?key=1", "s" * 65):
                    descriptor.write_text(json.dumps(catalogue | {"StoreId": store}))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "DaprOASIS":
                for invalid in ({"Store": "../foreign"}, {"GrpcEndpoint": "http://localhost:54862/"},
                                {"GrpcEndpoint": "http://127.0.0.1:65536/"}, {"GrpcEndpoint": "http://127.0.0.1:80/"}):
                    descriptor.write_text(json.dumps(catalogue | invalid))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "CloudflareOASIS":
                for invalid in ({"AccountId": "A" * 32}, {"NamespaceId": "b" * 31}, {"NamespaceId": "../foreign"}):
                    descriptor.write_text(json.dumps(catalogue | invalid))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "VercelKVOASIS":
                for endpoint in ("http://teaching.upstash.io/", "https://upstash.io/", "https://teaching.upstash.io.example/",
                                 "https://teaching.upstash.io/path", "https://user@teaching.upstash.io/"):
                    descriptor.write_text(json.dumps(catalogue | {"Endpoint": endpoint}))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "DenoDeployOASIS":
                for invalid in ({"DatabaseId": str(uuid.UUID(int=0))}, {"DataEndpoint": "http://api.deno.com/v2/teaching"},
                                {"DataEndpoint": "https://127.0.0.1/v2"}, {"DataEndpoint": "https://user@api.deno.com/v2"},
                                {"DataEndpoint": "https://api.deno.com/v2/../v3"}, {"DataEndpoint": "https://api.deno.com/v2?secret=bad"}):
                    descriptor.write_text(json.dumps(catalogue | invalid))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "AppwriteOASIS":
                for invalid in ({"Endpoint": "http://appwrite.example/v1"}, {"Endpoint": "https://user@appwrite.example/v1"},
                                {"Endpoint": "https://appwrite.example/v1/"}, {"Endpoint": "https://127.0.0.1/v1"},
                                {"ProjectId": "../foreign"}, {"DatabaseId": "a" * 37}):
                    descriptor.write_text(json.dumps(catalogue | invalid))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "ConvexOASIS":
                for site in ("http://teaching.convex.site/", "https://user@teaching.convex.site/",
                             "https://teaching.convex.site/foreign", "https://127.0.0.1/"):
                    descriptor.write_text(json.dumps(catalogue | {"Site": site}))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "TursoOASIS":
                for origin in ("http://teaching.turso.io/", "https://user@teaching.turso.io/",
                               "https://teaching.turso.io/foreign", "https://teaching.turso.io.example/"):
                    descriptor.write_text(json.dumps(catalogue | {"Origin": origin}))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider in ("OrbitDBOASIS", "GUNOASIS"):
                for address in ("http://localhost:54869/", "http://127.0.0.1:65536/",
                                "http://127.0.0.1:1023/", "http://127.0.0.1:54869/foreign"):
                    descriptor.write_text(json.dumps(catalogue | {"Sidecar": address}))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "NetlifyBlobsOASIS":
                for invalid in ({"SiteId": str(uuid.UUID(int=0))}, {"Store": "foreign/store"},
                                {"ObjectOrigin": "https://objects.example/foreign"}, {"ObjectOrigin": "https://127.0.0.1"}):
                    descriptor.write_text(json.dumps(catalogue | invalid))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "NileOASIS":
                for invalid in ({"Host": "api.thenile.dev"}, {"Database": "foreign/db"}):
                    descriptor.write_text(json.dumps(catalogue | invalid))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "AlgoliaOASIS":
                for application in ("teaching/foreign", "teaching.example", "a" * 51):
                    descriptor.write_text(json.dumps(catalogue | {"AppId": application}))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "NeonOASIS":
                for host in ("ep-demo-pooler.us-east-2.aws.neon.tech", "pg.neon.tech", "ep-demo.neon.tech.invalid"):
                    descriptor.write_text(json.dumps(catalogue | {"Host": host}))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "LanceDBOASIS":
                for invalid in ({"Origin": "https://api.lancedb.com"}, {"Database": "oasis/foreign"}):
                    descriptor.write_text(json.dumps(catalogue | invalid))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "PineconeOASIS":
                for origin in ("https://api.pinecone.io", catalogue["Origin"] + ".invalid"):
                    descriptor.write_text(json.dumps(catalogue | {"Origin": origin}))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "LitestreamOASIS":
                descriptor.write_text(json.dumps(catalogue | {"RestoreSha256": "A" * 64}))
                with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                    configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "ScyllaDBOASIS":
                for invalid in ({"Ip": "169.254.169.254"}, {"Host": "other.invalid"}, {"CaSha256": "invalid"}):
                    descriptor.write_text(json.dumps(catalogue | invalid))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "RavenDBOASIS":
                for invalid in ({"Origin": catalogue["Origin"] + ":443"}, {"CertificateSha256": "invalid"}):
                    descriptor.write_text(json.dumps(catalogue | invalid))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "CassandraOASIS":
                descriptor.write_text(json.dumps(catalogue | {"BundleSha256": "A" * 64}))
                with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                    configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "TimescaleDBOASIS":
                for invalid in ({"Port": True}, {"Port": "32625"}, {"Port": 65536}, {"Host": "localhost"}):
                    descriptor.write_text(json.dumps(catalogue | invalid))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "CockroachDBOASIS":
                descriptor.write_text(json.dumps(catalogue | {"Host": "other.cockroachlabs.cloud.invalid"}))
                with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                    configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider == "TigrisOASIS":
                for invalid in (catalogue | {"Originals": catalogue["Originals"][:-1]},
                                catalogue | {"Endpoint": "https://t3.storage.dev.other.example"}):
                    descriptor.write_text(json.dumps(invalid))
                    with self.assertRaisesRegex(RuntimeError, "reviewed teaching catalogue"):
                        configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            descriptor.write_text(json.dumps(catalogue))
            review = configuration.verifier.reviewed_exact_key_source(provider, descriptor)
            if provider in ("TigrisOASIS", "WeaviateOASIS", "GarnetOASIS", "KeyDBOASIS", "DragonflyOASIS", "UpstashOASIS", "InfluxDBOASIS", "CouchbaseOASIS", "ArangoDBOASIS", "CockroachDBOASIS", "TimescaleDBOASIS", "CassandraOASIS", "RavenDBOASIS", "ScyllaDBOASIS", "LitestreamOASIS", "PineconeOASIS", "LanceDBOASIS", "NeonOASIS", "AlgoliaOASIS", "NileOASIS", "NetlifyBlobsOASIS", "FastlyOASIS", "DaprOASIS", "CloudflareOASIS", "VercelKVOASIS", "DenoDeployOASIS", "AppwriteOASIS", "ConvexOASIS", "TursoOASIS", "ENSOffchainOASIS", "OrbitDBOASIS", "GUNOASIS", "MilvusOASIS", "CloudinaryOASIS", "PgVectorOASIS", "ProofOfHumanityOASIS", "OracleObjectStorageOASIS", "SafeOASIS", "DruidOASIS", "PinotOASIS", "TemporalOASIS", "DiscordOASIS", "LivepeerOASIS", "PinataOASIS", "PushProtocolOASIS", "LayerZeroOASIS", "WorldIDOASIS", "ReclaimProtocolOASIS", "DecentralandOASIS", "TheGraphOASIS", "SubqueryOASIS", "SelfProtocolOASIS", "HolonymOASIS", "CivicOASIS", "MapLibreOASIS", "GOMapOASIS", "TheSandboxOASIS", "PolygonIDOASIS", "SEEDSOASIS", "IntelOpenVINOOASIS"):
                with self.assertRaisesRegex(ValueError, "do not match"):
                    configuration.candidate(baseline, provider, review)
                if provider == "TigrisOASIS":
                    baseline.update(CQL_TIGRIS_QUERY_ORIGIN="https://t3.storage.dev",
                                    CQL_TIGRIS_QUERY_BUCKET="teaching", CQL_TIGRIS_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_TIGRIS_QUERY_BUCKET": "foreign"}, provider, review)
                elif provider == "CockroachDBOASIS":
                    baseline.update(CQL_COCKROACHDB_QUERY_HOST=catalogue["Host"], CQL_COCKROACHDB_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_COCKROACHDB_QUERY_HOST": "other.cockroachlabs.cloud"}, provider, review)
                elif provider == "TimescaleDBOASIS":
                    baseline.update(CQL_TIMESCALEDB_QUERY_HOST=catalogue["Host"],
                                    CQL_TIMESCALEDB_QUERY_PORT=str(catalogue["Port"]), CQL_TIMESCALEDB_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_TIMESCALEDB_QUERY_PORT": "5432"}, provider, review)
                elif provider == "CassandraOASIS":
                    baseline.update(CQL_CASSANDRA_QUERY_BUNDLE_SHA256=catalogue["BundleSha256"],
                                    CQL_CASSANDRA_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_CASSANDRA_QUERY_BUNDLE_SHA256": "b" * 64}, provider, review)
                elif provider == "RavenDBOASIS":
                    baseline.update(CQL_RAVENDB_QUERY_ORIGIN=catalogue["Origin"],
                                    CQL_RAVENDB_QUERY_CERTIFICATE_SHA256=catalogue["CertificateSha256"], CQL_RAVENDB_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_RAVENDB_QUERY_ORIGIN": "https://other.ravendb.cloud"}, provider, review)
                elif provider == "ScyllaDBOASIS":
                    baseline.update(CQL_SCYLLADB_QUERY_IP=catalogue["Ip"], CQL_SCYLLADB_QUERY_HOST=catalogue["Host"],
                                    CQL_SCYLLADB_QUERY_CA_SHA256=catalogue["CaSha256"], CQL_SCYLLADB_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_SCYLLADB_QUERY_IP": "1.1.1.1"}, provider, review)
                elif provider == "LitestreamOASIS":
                    baseline.update(CQL_LITESTREAM_QUERY_RESTORE_SHA256=catalogue["RestoreSha256"], CQL_LITESTREAM_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_LITESTREAM_QUERY_RESTORE_SHA256": "b" * 64}, provider, review)
                elif provider == "PineconeOASIS":
                    baseline.update(CQL_PINECONE_QUERY_ORIGIN=catalogue["Origin"], CQL_PINECONE_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_PINECONE_QUERY_ORIGIN": "https://other.svc.pinecone.io"}, provider, review)
                elif provider == "LanceDBOASIS":
                    baseline.update(CQL_LANCEDB_QUERY_ORIGIN=catalogue["Origin"],
                                    CQL_LANCEDB_QUERY_DATABASE=catalogue["Database"], CQL_LANCEDB_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_LANCEDB_QUERY_DATABASE": "foreign"}, provider, review)
                elif provider == "NeonOASIS":
                    baseline.update(CQL_NEON_QUERY_HOST=catalogue["Host"], CQL_NEON_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_NEON_QUERY_HOST": "ep-foreign.neon.tech"}, provider, review)
                elif provider == "AlgoliaOASIS":
                    baseline.update(CQL_ALGOLIA_QUERY_APP_ID=catalogue["AppId"], CQL_ALGOLIA_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_ALGOLIA_QUERY_APP_ID": "Foreign123"}, provider, review)
                elif provider == "NileOASIS":
                    baseline.update(CQL_NILE_QUERY_HOST=catalogue["Host"], CQL_NILE_QUERY_DATABASE=catalogue["Database"],
                                    CQL_NILE_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_NILE_QUERY_DATABASE": "foreign"}, provider, review)
                elif provider == "NetlifyBlobsOASIS":
                    baseline.update(CQL_NETLIFY_BLOBS_QUERY_SITE_ID=catalogue["SiteId"],
                                    CQL_NETLIFY_BLOBS_QUERY_STORE=catalogue["Store"],
                                    CQL_NETLIFY_BLOBS_QUERY_OBJECT_ORIGIN=catalogue["ObjectOrigin"],
                                    CQL_NETLIFY_BLOBS_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_NETLIFY_BLOBS_QUERY_OBJECT_ORIGIN": "https://foreign.example"}, provider, review)
                elif provider == "FastlyOASIS":
                    baseline.update(CQL_FASTLY_QUERY_STORE_ID=catalogue["StoreId"], CQL_FASTLY_QUERY_PROBE_ID=first)
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {"CQL_FASTLY_QUERY_STORE_ID": "foreignStore"}, provider, review)
                elif provider == "DaprOASIS":
                    baseline.update(CQL_DAPR_QUERY_STORE=catalogue["Store"],
                                    CQL_DAPR_QUERY_GRPC_ENDPOINT=catalogue["GrpcEndpoint"], CQL_DAPR_QUERY_PROBE_ID=first)
                    for changed in ({"CQL_DAPR_QUERY_STORE": "foreign"}, {"CQL_DAPR_QUERY_GRPC_ENDPOINT": "http://127.0.0.1:54863/"}):
                        with self.assertRaisesRegex(ValueError, "do not match"):
                            configuration.candidate(baseline | changed, provider, review)
                    for token in (None, "", "invalid token", "x" * 1025):
                        with self.assertRaisesRegex(ValueError, "source token"):
                            configuration.candidate(baseline | {"CQL_DAPR_QUERY_SIDECAR_TOKEN": token}, provider, review)
                    baseline["CQL_DAPR_QUERY_SIDECAR_TOKEN"] = "fixture-token"
                elif provider == "ENSOffchainOASIS":
                    key, value = next(iter(review["sourceBinding"].items()))
                    self.assertEqual({"owner": actor, "names": catalogue["Names"], "probe": "vitalik.eth"}, json.loads(value))
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {key: "{}"}, provider, review)
                    baseline[key] = value
                    secret = "CQL_ENS_OFFCHAIN_QUERY_API_KEY"
                    for invalid in (None, "", "invalid key", "invalid,key", "x" * 1025):
                        with self.assertRaisesRegex(ValueError, "source token"):
                            configuration.candidate(baseline | {secret: invalid}, provider, review)
                    baseline[secret] = "fixture-web3bio-key"
                elif provider == "CloudinaryOASIS":
                    key, value = next(iter(review["sourceBinding"].items()))
                    self.assertEqual({"owner": actor, "assets": catalogue["Assets"], "probe": "sample.jpg"}, json.loads(value))
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {key: "{}"}, provider, review)
                    baseline[key] = value
                elif provider in ("DiscordOASIS", "LivepeerOASIS", "PinataOASIS", "PushProtocolOASIS", "LayerZeroOASIS", "WorldIDOASIS", "ReclaimProtocolOASIS", "DecentralandOASIS", "TheGraphOASIS", "SubqueryOASIS", "SelfProtocolOASIS", "HolonymOASIS", "CivicOASIS", "MapLibreOASIS", "GOMapOASIS", "TheSandboxOASIS", "PolygonIDOASIS", "SEEDSOASIS", "IntelOpenVINOOASIS"):
                    setting = {"DiscordOASIS": "CQL_DISCORD_PUBLIC_GUILD_QUERY_SOURCE_OWNER",
                               "LivepeerOASIS": "CQL_LIVEPEER_ORCHESTRATOR_QUERY_SOURCE_OWNER",
                               "PinataOASIS": "CQL_PINATA_PUBLIC_BLOCK_QUERY_SOURCE_OWNER",
                               "PushProtocolOASIS": "CQL_PUSH_PUBLIC_CHANNEL_QUERY_SOURCE_OWNER",
                               "LayerZeroOASIS": "CQL_LAYERZERO_V1_ENDPOINT_QUERY_SOURCE_OWNER",
                               "WorldIDOASIS": "CQL_WORLD_ID_PROTOCOL_QUERY_SOURCE_OWNER",
                               "ReclaimProtocolOASIS": "CQL_RECLAIM_VERIFIER_QUERY_SOURCE_OWNER",
                               "DecentralandOASIS": "CQL_DECENTRALAND_SCENE_QUERY_SOURCE_OWNER",
                               "TheGraphOASIS": "CQL_THE_GRAPH_STUDIO_QUERY_SOURCE_OWNER",
                               "SubqueryOASIS": "CQL_SUBQUERY_DICTIONARY_QUERY_SOURCE_OWNER",
                               "SelfProtocolOASIS": "CQL_SELF_PROTOCOL_REGISTRY_QUERY_SOURCE_OWNER",
                               "HolonymOASIS": "CQL_HOLONYM_ATTESTATION_QUERY_SOURCE_OWNER",
                               "CivicOASIS": "CQL_CIVIC_NETWORK_QUERY_SOURCE_OWNER",
                               "MapLibreOASIS": "CQL_MAPLIBRE_STYLE_QUERY_SOURCE_OWNER",
                               "GOMapOASIS": "CQL_GOMAP_ROUTE_QUERY_SOURCE_OWNER",
                               "TheSandboxOASIS": "CQL_SANDBOX_CONTRACT_QUERY_SOURCE_OWNER",
                               "PolygonIDOASIS": "CQL_POLYGON_ID_SCHEMA_QUERY_SOURCE_OWNER",
                               "SEEDSOASIS": "CQL_SEEDS_SETTINGS_QUERY_SOURCE_OWNER",
                               "IntelOpenVINOOASIS": "CQL_OPENVINO_MODEL_QUERY_SOURCE_OWNER"}[provider]
                    binding = {setting: actor}
                    if provider == "GOMapOASIS":
                        binding["CQL_GOMAP_ROUTE_QUERY_SOURCE_ORIGIN"] = catalogue["Origin"]
                    self.assertEqual(binding, review["sourceBinding"])
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | binding | {setting: str(uuid.UUID(int=2))}, provider, review)
                    baseline.update(binding)
                elif provider == "TemporalOASIS":
                    key, value = next(iter(review["sourceBinding"].items()))
                    runs = catalogue["Runs"]
                    self.assertEqual({"owner": actor, "namespace": catalogue["Namespace"],
                                      "runs": [{"workflowId": run["WorkflowId"], "runId": run["RunId"]} for run in runs],
                                      "probeRunId": runs[0]["RunId"]}, json.loads(value))
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {key: "{}"}, provider, review)
                    baseline[key] = value
                    secret = "CQL_TEMPORAL_QUERY_API_KEY"
                    for invalid in (None, "", "invalid key", "invalid,key", "x" * 4097):
                        with self.assertRaisesRegex(ValueError, "source token"):
                            configuration.candidate(baseline | {secret: invalid}, provider, review)
                    baseline[secret] = "fixture-temporal-key"
                elif provider == "SafeOASIS":
                    key, value = next(iter(review["sourceBinding"].items()))
                    self.assertEqual({"owner": actor, "networks": ["eth", "base", "arb1"], "probe": "eth"}, json.loads(value))
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {key: "{}"}, provider, review)
                    baseline[key] = value
                    secret = "CQL_SAFE_INDEXING_QUERY_API_KEY"
                    for invalid in (None, "", "invalid key", "invalid/key", "x" * 4097):
                        with self.assertRaisesRegex(ValueError, "source token"):
                            configuration.candidate(baseline | {secret: invalid}, provider, review)
                    baseline[secret] = "fixture.safe-key"
                elif provider in ("ProofOfHumanityOASIS", "OracleObjectStorageOASIS"):
                    field = "Addresses" if provider == "ProofOfHumanityOASIS" else "Keys"
                    key, value = next(iter(review["sourceBinding"].items()))
                    self.assertEqual({"owner": actor, field.lower(): catalogue[field], "probe": catalogue[field][0]},
                                     json.loads(value))
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {key: "{}"}, provider, review)
                    baseline[key] = value
                elif provider in ("CloudflareOASIS", "VercelKVOASIS", "DenoDeployOASIS", "AppwriteOASIS", "ConvexOASIS", "TursoOASIS", "OrbitDBOASIS", "GUNOASIS", "MilvusOASIS", "PgVectorOASIS", "DruidOASIS", "PinotOASIS"):
                    baseline.update(review["sourceBinding"])
                    token_key = configuration.REQUIRED_SOURCE_TOKENS[provider]
                    with self.assertRaisesRegex(ValueError, "source token"):
                        configuration.candidate(baseline, provider, review)
                    if provider in ("ConvexOASIS", "OrbitDBOASIS", "GUNOASIS"):
                        for token in ("fixture-token", "a" * 257, "a" * 32 + "/"):
                            with self.assertRaisesRegex(ValueError, "source token"):
                                configuration.candidate(baseline | {token_key: token}, provider, review)
                    baseline[token_key] = "fixture-token" * 3 if provider in ("ConvexOASIS", "OrbitDBOASIS", "GUNOASIS") else "fixture-token"
                    for key in review["sourceBinding"]:
                        with self.assertRaisesRegex(ValueError, "do not match"):
                            configuration.candidate(baseline | {key: "foreign"}, provider, review)
                else:
                    product = provider.removesuffix("OASIS").upper()
                    baseline[f"CQL_{product}_QUERY_PROBE_ID"] = first
            if provider == "DynamoDBOASIS":
                prefix = "CQL_DYNAMODB_QUERY_"
                binding = {prefix + "REGION": "us-east-1", prefix + "PROBE_ID": first}
                self.assertEqual(binding, review["sourceBinding"])
                baseline.update(binding)
                with self.assertRaisesRegex(ValueError, "protected DynamoDB"):
                    configuration.candidate(baseline, provider, review)
                baseline[prefix + "ACCESS_KEY_ID"] = "fixture-access"
                baseline[prefix + "SECRET_ACCESS_KEY"] = "fixture-secret"
                for key, value in ((prefix + "REGION", "us-west-2"), (prefix + "PROBE_ID", second)):
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {key: value}, provider, review)
                with self.assertRaisesRegex(ValueError, "protected DynamoDB"):
                    configuration.candidate(baseline | {prefix + "SESSION_TOKEN": ""}, provider, review)
            after = configuration.candidate(baseline, provider, review)
            self.assertEqual({configuration.ADMISSION, configuration.GRANTS, partition},
                             {key for key, value in after.items() if baseline.get(key) != value})
            admission = json.loads(after[configuration.ADMISSION])
            self.assertEqual(["IPFSOASIS", provider], admission["queryOnlyProviders"])
            self.assertEqual(dict(admission["queryTariffs"][0], provider=provider), admission["queryTariffs"][1])
            self.assertEqual({"IPFSOASIS": ["retained"], provider: [first, second, third]},
                             json.loads(after[configuration.GRANTS]))
            self.assertEqual(review["presets"], json.loads(after[partition]))
            for changed in (after, baseline | {partition: "[]"},
                            baseline | {configuration.EXAMPLES: json.dumps({provider: []})}):
                with self.assertRaisesRegex(ValueError, "reconciliation"):
                    configuration.candidate(changed, provider, review)
            priced = json.loads(baseline[configuration.ADMISSION])
            priced["queryTariffs"][0]["costUsd"] = 1
            with self.assertRaisesRegex(ValueError, "zero-cost"):
                configuration.candidate(baseline | {configuration.ADMISSION: json.dumps(priced)}, provider, review)
            if os.name == "nt":
                protect = configuration.configuration.verification.protected_token
                original = Path(directory) / "baseline.dpapi"
                output = Path(directory) / "candidate.dpapi"
                original.write_bytes(protect(json.dumps(baseline).encode()))
                with patch("sys.argv", ["prepare", provider, "--catalogue", str(descriptor),
                                        "--baseline", str(original), "--output", str(output)]):
                    configuration.main()
                    with self.assertRaises(FileExistsError):
                        configuration.main()
                self.assertEqual(after, json.loads(protect(output.read_bytes(), decrypt=True)))

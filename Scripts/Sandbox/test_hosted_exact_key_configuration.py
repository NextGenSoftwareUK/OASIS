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
    if provider == "ENSOffchainOASIS":
        actor = "a5808c3e-7031-47e7-92de-48a659568f1b"
        first, second, third = ("2d8b5e78-3137-8c0d-ba70-bdaa880dcdad", "e29d23d8-4c2c-8e0a-9fde-5b92d31135c0",
                                "ffab761f-bf16-8e02-88e4-1e8a7b6cccd9")
        catalogue = {"Actor": actor, "Names": ["vitalik.eth", "nick.eth", "luc.eth"]}
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
        "MinIOOASIS": {"Endpoint": "https://minio.example.test", "Bucket": "teaching", "Region": "us-east-1"},
    }
    catalogue.update(fields.get(provider, {}))
    if provider in ("TigrisOASIS", "MinIOOASIS"):
        catalogue["Originals"] = [{"Id": key, "Sha256": "a" * 64, "ETag": "controlled-etag"}
                                  for key in (first, second, third, deleted)]
    return catalogue, (actor, first, second, third, deleted, missing)


class ExactKeyConfigurationTests(unittest.TestCase):
    def test_candidate_preserves_existing_providers_and_refuses_replacement(self):
        self.check_candidate("QdrantOASIS")

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
            if provider in ("TigrisOASIS", "WeaviateOASIS", "GarnetOASIS", "KeyDBOASIS", "DragonflyOASIS", "UpstashOASIS", "InfluxDBOASIS", "CouchbaseOASIS", "ArangoDBOASIS", "CockroachDBOASIS", "TimescaleDBOASIS", "CassandraOASIS", "RavenDBOASIS", "ScyllaDBOASIS", "LitestreamOASIS", "PineconeOASIS", "LanceDBOASIS", "NeonOASIS", "AlgoliaOASIS", "NileOASIS", "NetlifyBlobsOASIS", "FastlyOASIS", "DaprOASIS", "CloudflareOASIS", "VercelKVOASIS", "DenoDeployOASIS", "AppwriteOASIS", "ConvexOASIS", "TursoOASIS", "ENSOffchainOASIS", "OrbitDBOASIS", "GUNOASIS", "MilvusOASIS", "CloudinaryOASIS", "PgVectorOASIS", "ProofOfHumanityOASIS", "OracleObjectStorageOASIS"):
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
                elif provider == "CloudinaryOASIS":
                    key, value = next(iter(review["sourceBinding"].items()))
                    self.assertEqual({"owner": actor, "assets": catalogue["Assets"], "probe": "sample.jpg"}, json.loads(value))
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {key: "{}"}, provider, review)
                    baseline[key] = value
                elif provider in ("ProofOfHumanityOASIS", "OracleObjectStorageOASIS"):
                    field = "Addresses" if provider == "ProofOfHumanityOASIS" else "Keys"
                    key, value = next(iter(review["sourceBinding"].items()))
                    self.assertEqual({"owner": actor, field.lower(): catalogue[field], "probe": catalogue[field][0]},
                                     json.loads(value))
                    with self.assertRaisesRegex(ValueError, "do not match"):
                        configuration.candidate(baseline | {key: "{}"}, provider, review)
                    baseline[key] = value
                elif provider in ("CloudflareOASIS", "VercelKVOASIS", "DenoDeployOASIS", "AppwriteOASIS", "ConvexOASIS", "TursoOASIS", "OrbitDBOASIS", "GUNOASIS", "MilvusOASIS", "PgVectorOASIS"):
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

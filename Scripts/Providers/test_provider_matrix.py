"""Offline inventory regression checks; no SDK install or service needed."""
import sys
sys.dont_write_bytecode=True
import unittest
from unittest.mock import patch
from pathlib import Path
import update_provider_matrix as m

class ProviderMatrixTests(unittest.TestCase):
    def test_holonet_and_eoscore_require_active_sdk_calls(self):
        holo='using NextGenSoftware.Holochain.HoloNET.Client.Interfaces; await client.CallZomeFunctionAsync(zome, function, payload);'
        eos='using EOSNewYork.EOSCore.Response.API; await provider.ChainAPI.GetTableRowsAsync(args);'
        self.assertIn('HoloNET client',m.detect_cs_sdk(holo))
        self.assertIn('EOSCore chain API',m.detect_cs_sdk(eos))
        for text in ['// '+holo,'/* '+eos+' */','using EOSNewYork.EOSCore.Response.API;', 'client.CallZomeFunctionAsync(args);']:
            self.assertNotIn('HoloNET client',m.detect_cs_sdk(text))
            self.assertNotIn('EOSCore chain API',m.detect_cs_sdk(text))
        for name,group,expected in [('HoloOASIS.Edge','Network','HoloNET client'),('SEEDSOASIS','Other','EOSCore chain API')]:
            project=m.ROOT/'Providers'/group/('NextGenSoftware.OASIS.API.Providers.'+name)/('NextGenSoftware.OASIS.API.Providers.'+name+'.csproj')
            self.assertIn(expected,m.sdk_evidence(project))

    def test_requested_document_sql_and_object_roles(self):
        for family,names in {
            'Document':['FirebaseOASIS','AppwriteOASIS','FaunaOASIS','ConvexOASIS','PocketBaseOASIS'],
            'SQL':['CloudflareD1OASIS','TursoOASIS','TablelandOASIS','FlyIOOASIS'],
            'Key-value/object':['UpstashOASIS','AzureStorageOASIS','OracleObjectStorageOASIS']}.items():
            for name in names:
                with self.subTest(provider=name):self.assertEqual(family,m.family(name,'',Path('Providers/Cloud/provider.csproj')))

    def test_nonstorage_product_roles_override_old_template_storage_and_evm_imports(self):
        for name in ['MapboxOASIS','GoogleMapsOASIS','BittensorOASIS','DuneAnalyticsOASIS','CovalentOASIS','CivicOASIS','ActivityPubOASIS']:
            with self.subTest(provider=name):self.assertEqual('Non-storage',m.family(name,'Nethereum Web3CoreOASISBaseProvider',Path('Providers/Blockchain/provider.csproj')))
        self.assertEqual('Non-EVM chain',m.family('EOSIOOASIS','using Nethereum;',Path('Providers/Blockchain/provider.csproj')))
        self.assertEqual('Non-EVM chain',m.family('CosmosBlockChainOASIS','KeyValueStorageProviderBase',Path('Providers/Blockchain/provider.csproj')))

    def test_client_usage_and_injected_official_sdk_are_recognized(self):
        self.assertIn('Nethereum',m.detect_cs_sdk('using Nethereum.Web3; Web3Client = CreateWeb3WithAccount(account, endpoint);'))
        self.assertIn('AWS S3 SDK',m.detect_cs_sdk('using Amazon.S3; IAmazonS3 client; await client.PutObjectAsync(request);'))
        self.assertIn('AWS DynamoDB SDK',m.detect_cs_sdk('using Amazon.DynamoDBv2; await client.GetItemAsync(request);'))
        self.assertIn('Solnet',m.detect_cs_sdk('using Solnet.Rpc; ClientFactory.GetClient(endpoint);'))
        self.assertIn('HTTP API client',m.detect_cs_sdk('using System.Net.Http; await client.GetAsync(endpoint);'))

    def test_priority_injected_oci_and_native_sql_redis_evidence(self):
        self.assertIn('OCI Object Storage SDK',m.detect_cs_sdk('using Oci.ObjectstorageService; ObjectStorageClient client; await client.GetObject(request);'))
        self.assertIn('StackExchange.Redis',m.detect_cs_sdk('using StackExchange.Redis; await ConnectionMultiplexer.ConnectAsync(options);'))
        self.assertIn('Microsoft.Data.SqlClient',m.detect_cs_sdk('using Microsoft.Data.SqlClient; var client=new SqlConnection(settings);'))
        self.assertNotIn('OCI Object Storage SDK',m.detect_cs_sdk('await unrelated.GetObject(request);'))
        self.assertNotIn('StackExchange.Redis',m.detect_cs_sdk('// using StackExchange.Redis; ConnectionMultiplexer.ConnectAsync(options);'))
        self.assertNotIn('Microsoft.Data.SqlClient',m.detect_cs_sdk('using Microsoft.Data.SqlClient; /* new SqlConnection(settings); */'))

    def test_native_family_sdk_guards_and_real_client_calls(self):
        rows=[('DuckDB.NET', 'DuckDB.NET.Data', 'var client=new DuckDBConnection(settings);'), ('MySqlConnector', 'MySqlConnector', 'var client=new MySqlConnection(settings);'), ('Snowflake.Data', 'Snowflake.Data.Client', 'var client=new SnowflakeDbConnection();'), ('Cassandra C# driver', 'Cassandra', 'var client=Cluster.Builder();'), ('Couchbase SDK', 'Couchbase', 'await Cluster.ConnectAsync(settings);'), ('RavenDB client', 'Raven.Client.Documents', 'var client=new DocumentStore();'), ('NATS.Net', 'NATS.Client.Core', 'var client=new NatsConnection(options);'), ('Dapr SDK', 'Dapr.Client', 'var client=new DaprClientBuilder();'), ('Temporal SDK', 'Temporalio.Client', 'await TemporalClient.ConnectAsync(options);'), ('ClickHouse.Client', 'ClickHouse.Client.ADO', 'var client=new ClickHouseConnection(settings);'), ('InfluxDB.Client', 'InfluxDB.Client', 'var client=InfluxDBClientFactory.Create(settings);'), ('Enyim Memcached', 'Enyim.Caching', 'var client=new MemcachedClient(options);'), ('SurrealDb.Net', 'SurrealDb.Net', 'var client=new SurrealDbClient(endpoint);')]
        for name,namespace,code in rows:
            with self.subTest(sdk=name):
                self.assertIn(name,m.detect_cs_sdk('using '+namespace+'; '+code))
                self.assertNotIn(name,m.detect_cs_sdk('using '+namespace+';'))
                self.assertNotIn(name,m.detect_cs_sdk(code))
                self.assertNotIn(name,m.detect_cs_sdk('// using '+namespace+'; '+code))

    def test_package_presence_and_unrelated_client_factory_do_not_prove_an_sdk(self):
        self.assertEqual([],m.detect_cs_sdk('<PackageReference Include="Nethereum.Web3" />'))
        self.assertEqual([],m.detect_cs_sdk('// using Nethereum.Web3; CreateWeb3WithAccount(account, endpoint);'))
        self.assertEqual([],m.detect_cs_sdk('/* using Amazon.S3; client.PutObjectAsync(request); */'))
        self.assertNotIn('Solnet',m.detect_cs_sdk('JsonRpcClientFactory.GetClient(endpoint);'))
        self.assertNotIn('Solnet',m.detect_cs_sdk('using Microsoft.Azure.Cosmos; JsonRpcClientFactory.GetClient(endpoint);'))

    def test_existing_pinned_node_bridge_import_is_recognized_without_installing(self):
        directory=m.ROOT/'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS'
        self.assertTrue(any(x.startswith('near-api-js 7.3.1') for x in m.node_sdk_evidence(directory)))

    def test_real_relative_provider_inheritance_and_arweave_bridge(self):
        project=m.ROOT/'Providers/Storage/NextGenSoftware.OASIS.API.Providers.FlyIOOASIS/NextGenSoftware.OASIS.API.Providers.FlyIOOASIS.csproj'
        self.assertIn('Npgsql',m.sdk_evidence(project))
        directory=m.ROOT/'Providers/Storage/NextGenSoftware.OASIS.API.Providers.ArweaveOASIS'
        self.assertIn('arweave 2.1.0 (bridge import)',m.node_sdk_evidence(directory))

    def test_content_addressed_and_redis_facades_use_their_actual_family_owner(self):
        self.assertEqual('IPFS/content-addressed',m.family('FilecoinOASIS','HttpClient',Path('Providers/Blockchain/provider.csproj')))
        for name in ['KeyDBOASIS','ValKeyOASIS','DragonflyOASIS','GarnetOASIS']:
            project=m.ROOT/'Providers/Storage'/('NextGenSoftware.OASIS.API.Providers.'+name)/('NextGenSoftware.OASIS.API.Providers.'+name+'.csproj')
            with self.subTest(provider=name):self.assertIn('StackExchange.Redis',m.sdk_evidence(project))
        project=m.ROOT/'Providers/Network/NextGenSoftware.OASIS.API.Providers.IPFSOASIS/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.csproj'
        self.assertIn('IPFS client',m.sdk_evidence(project))

    def test_unused_provider_reference_cannot_lend_its_sdk_to_an_empty_project(self):
        p=m.ROOT/'Providers/Other/NextGenSoftware.OASIS.API.Providers.CargoOASIS/NextGenSoftware.OASIS.API.Providers.CargoOASIS.csproj'
        with patch.object(m,'source_text',return_value=''),patch.object(m,'node_sdk_evidence',return_value=[]):
            self.assertEqual([],m.sdk_evidence(p))

    def test_complete_inventory_has_no_unclassified_provider_or_template(self):
        text=m.build();rows=[r for r in text.splitlines() if r.startswith('| [')]
        self.assertEqual(229,len(rows));self.assertFalse(any('| Other / capability review |' in r for r in rows))
        self.assertNotIn('[ProviderNameOASIS]',text)
        self.assertIn('capability-v1',text)

    def test_negative_sdk_review_requires_unchanged_complete_source(self):
        for name,group in [('PolygonIDOASIS','Identity'),('zkPassOASIS','Identity'),('HoloOASIS.Desktop','Network'),('Neo4jOASIS2','Storage')]:
            project=m.ROOT/'Providers'/group/('NextGenSoftware.OASIS.API.Providers.'+name)/('NextGenSoftware.OASIS.API.Providers.'+name+'.csproj')
            self.assertIsNotNone(m.reviewed_sdk_gap(name,project))
            self.assertEqual([],m.sdk_evidence(project))
        project=m.ROOT/'Providers/Maps/NextGenSoftware.OASIS.API.Providers.GoogleMapsOASIS/NextGenSoftware.OASIS.API.Providers.GoogleMapsOASIS.csproj'
        self.assertIsNotNone(m.reviewed_sdk_gap('GoogleMapsOASIS',project))
        with patch.object(m,'source_files',return_value=iter([])):
            self.assertIsNone(m.reviewed_sdk_gap('GoogleMapsOASIS',project))
        class ChangedHash:
            def hexdigest(self):return 'changed'
        with patch.object(m.hashlib,'sha256',return_value=ChangedHash()):
            self.assertIsNone(m.reviewed_sdk_gap('GoogleMapsOASIS',project))

if __name__=='__main__':unittest.main()

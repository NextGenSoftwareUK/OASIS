"""Offline inventory regression checks; no SDK install or service needed."""
import sys
sys.dont_write_bytecode=True
import unittest
from unittest.mock import patch
from pathlib import Path
import update_provider_matrix as m

class ProviderMatrixTests(unittest.TestCase):
    def test_requested_document_sql_and_object_roles(self):
        for family,names in {
            'Document':['FirebaseOASIS','AppwriteOASIS','FaunaOASIS','ConvexOASIS','PocketBaseOASIS'],
            'SQL':['CloudflareD1OASIS','TursoOASIS','TablelandOASIS'],
            'Key-value/object':['UpstashOASIS','AzureStorageOASIS']}.items():
            for name in names:
                with self.subTest(provider=name):self.assertEqual(family,m.family(name,'',Path('Providers/Cloud/provider.csproj')))

    def test_nonstorage_product_roles_override_old_template_storage_and_evm_imports(self):
        for name in ['MapboxOASIS','GoogleMapsOASIS','BittensorOASIS','DuneAnalyticsOASIS','CovalentOASIS','CivicOASIS']:
            with self.subTest(provider=name):self.assertEqual('Non-storage',m.family(name,'Nethereum Web3CoreOASISBaseProvider',Path('Providers/Blockchain/provider.csproj')))
        self.assertEqual('Non-EVM chain',m.family('EOSIOOASIS','using Nethereum;',Path('Providers/Blockchain/provider.csproj')))
        self.assertEqual('Non-EVM chain',m.family('CosmosBlockChainOASIS','KeyValueStorageProviderBase',Path('Providers/Blockchain/provider.csproj')))

    def test_client_usage_and_injected_official_sdk_are_recognized(self):
        self.assertIn('Nethereum',m.detect_cs_sdk('using Nethereum.Web3; Web3Client = CreateWeb3WithAccount(account, endpoint);'))
        self.assertIn('AWS S3 SDK',m.detect_cs_sdk('using Amazon.S3; IAmazonS3 client; await client.PutObjectAsync(request);'))
        self.assertIn('AWS DynamoDB SDK',m.detect_cs_sdk('using Amazon.DynamoDBv2; await client.GetItemAsync(request);'))
        self.assertIn('Solnet',m.detect_cs_sdk('using Solnet.Rpc; ClientFactory.GetClient(endpoint);'))
        self.assertIn('HTTP API client',m.detect_cs_sdk('using System.Net.Http; await client.GetAsync(endpoint);'))

    def test_package_presence_and_unrelated_client_factory_do_not_prove_an_sdk(self):
        self.assertEqual([],m.detect_cs_sdk('<PackageReference Include="Nethereum.Web3" />'))
        self.assertEqual([],m.detect_cs_sdk('// using Nethereum.Web3; CreateWeb3WithAccount(account, endpoint);'))
        self.assertEqual([],m.detect_cs_sdk('/* using Amazon.S3; client.PutObjectAsync(request); */'))
        self.assertNotIn('Solnet',m.detect_cs_sdk('JsonRpcClientFactory.GetClient(endpoint);'))
        self.assertNotIn('Solnet',m.detect_cs_sdk('using Microsoft.Azure.Cosmos; JsonRpcClientFactory.GetClient(endpoint);'))

    def test_existing_pinned_node_bridge_import_is_recognized_without_installing(self):
        directory=m.ROOT/'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS'
        self.assertTrue(any(x.startswith('near-api-js 7.3.1') for x in m.node_sdk_evidence(directory)))

    def test_unused_provider_reference_cannot_lend_its_sdk_to_an_empty_project(self):
        p=m.ROOT/'Providers/Other/NextGenSoftware.OASIS.API.Providers.CargoOASIS/NextGenSoftware.OASIS.API.Providers.CargoOASIS.csproj'
        with patch.object(m,'source_text',return_value=''),patch.object(m,'node_sdk_evidence',return_value=[]):
            self.assertEqual([],m.sdk_evidence(p))

    def test_complete_inventory_has_no_unclassified_provider_or_template(self):
        text=m.build();rows=[r for r in text.splitlines() if r.startswith('| [')]
        self.assertEqual(229,len(rows));self.assertFalse(any('| Other / capability review |' in r for r in rows))
        self.assertNotIn('[ProviderNameOASIS]',text)
        self.assertIn('capability-v1',text)

if __name__=='__main__':unittest.main()

"""Offline composition checks, never observing/changing a hosted service."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

def load(name, filename):
    spec=importlib.util.spec_from_file_location(name,Path(__file__).with_name(filename))
    value=importlib.util.module_from_spec(spec);spec.loader.exec_module(value);return value

publisher=load("combined", "Publish-HostedGoogleEthereumConfiguration.py")
google_fixture=load("google_fixture", "test_google_public_configuration.py")
ethereum_fixture=load("ethereum_fixture", "test_ethereum_public_configuration.py")

class CombinedConfigurationTests(unittest.TestCase):
    def fixture(self):
        before,google=google_fixture.GoogleConfigurationTests().fixture()
        _,ethereum=ethereum_fixture.EthereumConfigurationTests().fixture()
        return before,google,ethereum

    def test_one_candidate_preserves_both_sources_and_all_eight_presets(self):
        before,google,ethereum=self.fixture();original=copy.deepcopy(before)
        after=publisher.candidate(before,google,ethereum)
        self.assertEqual(before,original)
        self.assertEqual(after["OASIS_DNA_JSON"],before["OASIS_DNA_JSON"])
        admission=json.loads(after["PREPARED_STORAGE_ADMISSION_JSON"])
        self.assertEqual(admission["queryOnlyProviders"],["IPFSOASIS","GoogleCloudOASIS","EthereumOASIS"])
        self.assertEqual([row["provider"] for row in admission["queryTariffs"]],admission["queryOnlyProviders"])
        examples=json.loads(after["CQL_HOSTED_QUERY_EXAMPLES_JSON"])
        for provider in ("GoogleCloudOASIS","EthereumOASIS"):
            self.assertEqual(len(examples[provider]),4)
        self.assertEqual(set(publisher.KEYS),{key for key in after if before.get(key)!=after[key]})

    def test_existing_enrollment_rejects_instead_of_replacing_either_source(self):
        for provider in ("GoogleCloudOASIS","EthereumOASIS"):
            before,google,ethereum=self.fixture()
            before["CQL_HOSTED_QUERY_EXAMPLES_JSON"]=json.dumps({provider:[]})
            with self.assertRaises(ValueError):publisher.candidate(before,google,ethereum)

    def test_encryption_requires_both_query_keys_without_mutating_dna(self):
        before,google,ethereum=self.fixture();dna=json.loads(before["OASIS_DNA_JSON"])
        policy={"Rijndael256EncryptionEnabled":True,"AdditionalQueryableKeys":["GCSObjectKey"]}
        dna["OASIS"]["Security"]={"HolonDataEncryption":policy};before["OASIS_DNA_JSON"]=json.dumps(dna)
        with self.assertRaises(ValueError):publisher.candidate(before,google,ethereum)
        policy["AdditionalQueryableKeys"].append("EthereumAccountAddress");before["OASIS_DNA_JSON"]=json.dumps(dna)
        self.assertEqual(publisher.candidate(before,google,ethereum)["OASIS_DNA_JSON"],before["OASIS_DNA_JSON"])

if __name__=="__main__":unittest.main()

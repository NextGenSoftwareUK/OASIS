"""Compose the new three profiles without overwriting the earlier seven."""
import copy
import importlib.util
import json
from pathlib import Path
import unittest

def load(name):
    spec=importlib.util.spec_from_file_location(name,Path(__file__).with_name("test_"+name+"_public_configuration.py"))
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module

class EvmBreadthCompositionTests(unittest.TestCase):
    def test_preserves_existing_seven_and_enrolls_twelve_new_presets(self):
        base=load("base");avalanche=load("avalanche");bnb=load("bnb")
        before,first=base.BaseConfigurationTests().fixture()
        earlier={name:[{"id":"preserved-"+str(index)}] for index,name in enumerate(
            ["GoogleCloudOASIS","EthereumOASIS","PolygonOASIS","ArbitrumOASIS","CosmosBlockChainOASIS","PolkadotOASIS","NEAROASIS"])}
        before["CQL_HOSTED_QUERY_EXAMPLES_JSON"]=json.dumps(earlier)
        original=copy.deepcopy(before)
        after=base.publisher.candidate(before,first)
        after=avalanche.publisher.candidate(after,avalanche.AvalancheConfigurationTests().fixture()[1])
        after=bnb.publisher.candidate(after,bnb.BnbConfigurationTests().fixture()[1])
        self.assertEqual(before,original)
        self.assertEqual(after["OASIS_DNA_JSON"],before["OASIS_DNA_JSON"])
        examples=json.loads(after["CQL_HOSTED_QUERY_EXAMPLES_JSON"])
        for key,value in earlier.items():self.assertEqual(examples[key],value)
        for key in ["BaseOASIS","AvalancheOASIS","BNBChainOASIS"]:
            self.assertEqual(len(examples[key]),4)
            self.assertEqual(sum(row.get("operation")=="Federation" for row in examples[key]),2)
        self.assertEqual(len([key for key in after if before.get(key)!=after[key]]),5)

if __name__=="__main__":unittest.main()

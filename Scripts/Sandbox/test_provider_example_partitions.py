import importlib.util,json
from pathlib import Path
import unittest
spec=importlib.util.spec_from_file_location('partitions',Path(__file__).with_name('Prepare-ProviderExamplePartitions.py'))
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
class PartitionCandidateTests(unittest.TestCase):
    def fixture(self):
        providers=['Provider'+str(i) for i in range(17)]
        old={'Existing':[{'id':'original'}]}
        partial={'DNA':'unchanged',m.KEY:json.dumps(old),'admission':'already-reviewed'}
        complete={**partial,m.KEY:json.dumps({**old,**{p:[{'id':'a'},{'id':'b'},{'id':'c'},{'id':'d'}] for p in providers}})}
        return providers,partial,complete
    def test_preserves_existing_values_and_all_four_examples_per_provider(self):
        providers,partial,complete=self.fixture();after=m.partition_candidate(partial,complete,providers)
        self.assertEqual({k:after[k] for k in partial},partial)
        self.assertEqual(len(after)-len(partial),17)
        self.assertEqual(sum(len(json.loads(after[m.SECTION+p])) for p in providers),68)
    def test_changed_existing_catalogue_and_existing_partition_are_rejected(self):
        providers,partial,complete=self.fixture();bad=json.loads(complete[m.KEY]);bad['Existing']=[]
        with self.assertRaises(ValueError):m.partition_candidate(partial,{**complete,m.KEY:json.dumps(bad)},providers)
        with self.assertRaises(ValueError):m.partition_candidate({**partial,m.SECTION+providers[0]:'[]'},complete,providers)
    def test_incomplete_provider_set_and_oversize_partition_are_rejected(self):
        providers,partial,complete=self.fixture()
        with self.assertRaises(ValueError):m.partition_candidate(partial,complete,providers[:-1])
        bad=json.loads(complete[m.KEY]);bad[providers[0]]=['x'*32769]
        with self.assertRaises(ValueError):m.partition_candidate(partial,{**complete,m.KEY:json.dumps(bad)},providers)
if __name__=='__main__':unittest.main()

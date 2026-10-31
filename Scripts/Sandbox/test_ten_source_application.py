import importlib.util
from pathlib import Path
import unittest
spec=importlib.util.spec_from_file_location("batch",Path(__file__).with_name("Prepare-TenSourcePublicBatch.py"))
batch=importlib.util.module_from_spec(spec);spec.loader.exec_module(batch)
class ApplicationScopeTests(unittest.TestCase):
    def fixture(self):
        before={"DNA":"unchanged","PREPARED_STORAGE_ADMISSION_JSON":"old","CQL_HOSTED_QUERY_EXAMPLES_JSON":"old"}
        after={**before,**{key:"new" for key in batch.APPLY_KEYS}}
        return before,after
    def test_sources_precede_shared_admission_and_examples(self):
        before,after=self.fixture()
        self.assertEqual(batch.application_keys(before,after),batch.APPLY_KEYS)
        self.assertEqual(len(batch.APPLY_KEYS),12)
        self.assertEqual(batch.APPLY_KEYS[-2:],("PREPARED_STORAGE_ADMISSION_JSON","CQL_HOSTED_QUERY_EXAMPLES_JSON"))
    def test_optional_optimism_is_explicit_and_keeps_ten_source_scope(self):
        before,after=self.fixture()
        after["CQL_OPTIMISM_PUBLIC_SOURCE_JSON"]="new"
        with self.assertRaises(ValueError):batch.application_keys(before,after)
        keys=batch.application_keys(before,after,True)
        self.assertEqual(len(keys),13)
        self.assertEqual(keys[-3:],("CQL_OPTIMISM_PUBLIC_SOURCE_JSON",)+batch.APPLY_KEYS[-2:])
        del after["CQL_OPTIMISM_PUBLIC_SOURCE_JSON"]
        with self.assertRaises(ValueError):batch.application_keys(before,after,True)

    def test_refuses_extra_missing_removed_and_existing_source_settings(self):
        for fault in ("extra","missing","removed","existing"):
            with self.subTest(fault=fault):
                before,after=self.fixture()
                if fault=="extra":after["DNA"]="changed"
                if fault=="missing":del after[batch.SOURCE_KEYS[0]]
                if fault=="removed":del after["DNA"]
                if fault=="existing":before[batch.SOURCE_KEYS[0]]="old-profile"
                with self.assertRaises(ValueError):batch.application_keys(before,after)
if __name__=="__main__":unittest.main()

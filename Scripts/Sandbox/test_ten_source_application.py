import importlib.util
from pathlib import Path
import unittest
import json
from types import SimpleNamespace
from unittest.mock import patch
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

    def test_linea_scroll_require_complete_explicit_thirteen_source_scope(self):
        before,after=self.fixture()
        for name in ("OPTIMISM","LINEA","SCROLL"):after["CQL_"+name+"_PUBLIC_SOURCE_JSON"]="new"
        self.assertEqual(len(batch.application_keys(before,after,True,True)),15)
        with self.assertRaises(ValueError):batch.application_keys(before,after,True)
        with self.assertRaises(ValueError):batch.application_keys(before,after,False,True)
        del after["CQL_SCROLL_PUBLIC_SOURCE_JSON"]
        with self.assertRaises(ValueError):batch.application_keys(before,after,True,True)

    def test_zksync_abstract_require_complete_explicit_fifteen_source_scope(self):
        before,after=self.fixture()
        for name in ("OPTIMISM","LINEA","SCROLL","ZKSYNC","ABSTRACT"):after["CQL_"+name+"_PUBLIC_SOURCE_JSON"]="new"
        self.assertEqual(len(batch.application_keys(before,after,True,True,True)),17)
        with self.assertRaises(ValueError):batch.application_keys(before,after,True,True)
        with self.assertRaises(ValueError):batch.application_keys(before,after,True,False,True)
        del after["CQL_ABSTRACT_PUBLIC_SOURCE_JSON"]
        with self.assertRaises(ValueError):batch.application_keys(before,after,True,True,True)

    def test_monad_requires_complete_explicit_sixteen_source_scope(self):
        before,after=self.fixture()
        for name in ("OPTIMISM","LINEA","SCROLL","ZKSYNC","ABSTRACT","MONAD"):after["CQL_"+name+"_PUBLIC_SOURCE_JSON"]="new"
        self.assertEqual(len(batch.application_keys(before,after,True,True,True,True)),18)
        with self.assertRaises(ValueError):batch.application_keys(before,after,True,True,True)
        with self.assertRaises(ValueError):batch.application_keys(before,after,True,True,False,True)
        del after["CQL_MONAD_PUBLIC_SOURCE_JSON"]
        with self.assertRaises(ValueError):batch.application_keys(before,after,True,True,True,True)

    def test_sei_requires_complete_explicit_seventeen_source_scope(self):
        before,after=self.fixture()
        for name in ("OPTIMISM","LINEA","SCROLL","ZKSYNC","ABSTRACT","MONAD","SEI"):after["CQL_"+name+"_PUBLIC_SOURCE_JSON"]="new"
        self.assertEqual(len(batch.application_keys(before,after,True,True,True,True,True)),19)
        with self.assertRaises(ValueError):batch.application_keys(before,after,True,True,True,True)
        with self.assertRaises(ValueError):batch.application_keys(before,after,True,True,True,False,True)
        del after["CQL_SEI_PUBLIC_SOURCE_JSON"]
        with self.assertRaises(ValueError):batch.application_keys(before,after,True,True,True,True,True)

    def test_refuses_extra_missing_removed_and_existing_source_settings(self):
        for fault in ("extra","missing","removed","existing"):
            with self.subTest(fault=fault):
                before,after=self.fixture()
                if fault=="extra":after["DNA"]="changed"
                if fault=="missing":del after[batch.SOURCE_KEYS[0]]
                if fault=="removed":del after["DNA"]
                if fault=="existing":before[batch.SOURCE_KEYS[0]]="old-profile"
                with self.assertRaises(ValueError):batch.application_keys(before,after)
class DeploymentGuardTests(unittest.TestCase):
    def configuration(self):
        return SimpleNamespace(CLI="existing-railway",COMMON=["-e","dev"])
    def test_missing_reviewed_sha_never_observes_or_mutates_host(self):
        with patch.dict(batch.os.environ,{},clear=True),patch.object(batch.subprocess,"run") as run:
            with self.assertRaises(RuntimeError):batch.require_matching_release(self.configuration())
            run.assert_not_called()
    def test_different_checkout_stops_before_host_observation(self):
        with patch.dict(batch.os.environ,{"COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT":"a"*40}),patch.object(batch.subprocess,"run",return_value=SimpleNamespace(returncode=0,stdout="b"*40)) as run:
            with self.assertRaises(RuntimeError):batch.require_matching_release(self.configuration())
            self.assertEqual(1,run.call_count)
    def test_apply_never_calls_mutation_owner_when_release_guard_fails(self):
        before,after=ApplicationScopeTests().fixture()
        from unittest.mock import Mock
        configuration=SimpleNamespace(protected_read=Mock(side_effect=[before,after]),apply_values=Mock())
        with patch.object(batch,"load",return_value=configuration),patch.object(batch,"require_matching_release",side_effect=RuntimeError("not deployed")):
            with self.assertRaises(RuntimeError):batch.apply()
        configuration.apply_values.assert_not_called()

    def test_only_exact_successful_deployment_passes(self):
        for status,commit,accepted in [("SUCCESS","a"*40,True),("BUILDING","a"*40,False),("SUCCESS","b"*40,False)]:
            with self.subTest(status=status,commit=commit),patch.dict(batch.os.environ,{"COSMIC_CQL_EXPECTED_DEPLOYMENT_COMMIT":"a"*40}),patch.object(batch.subprocess,"run",side_effect=[SimpleNamespace(returncode=0,stdout="a"*40),SimpleNamespace(returncode=0,stdout=json.dumps([{"status":status,"meta":{"commitHash":commit}}]))]):
                if accepted:batch.require_matching_release(self.configuration())
                else:
                    with self.assertRaises(RuntimeError):batch.require_matching_release(self.configuration())

if __name__=="__main__":unittest.main()

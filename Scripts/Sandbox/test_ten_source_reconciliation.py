import importlib.util
from pathlib import Path
import unittest
spec=importlib.util.spec_from_file_location("reconcile",Path(__file__).with_name("Reconcile-TenSourceHostedConfiguration.py"))
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
class ReconciliationTests(unittest.TestCase):
    def test_unchanged_and_both_kinds_of_drift_are_distinguished_without_values(self):
        before={"shared":"secret-old","unrelated":"secret"};after={**before,"shared":"secret-new","source":"secret-source"}
        same=module.compare(before,dict(before),after)
        self.assertTrue(same["matchesPreservedSnapshot"])
        result=module.compare(before,{"shared":"changed","unrelated":"changed","added":"private"},after)
        self.assertFalse(result["matchesPreservedSnapshot"])
        self.assertEqual(result["overlappingChangedKeyNames"],["shared"])
        self.assertEqual(result["changedKeyNames"],["added","shared","unrelated"])
        self.assertNotIn("secret",str(result))
        self.assertEqual(before["shared"],"secret-old")
if __name__=="__main__":unittest.main()

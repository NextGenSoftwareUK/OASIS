import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location(
    "launcher_audit", Path(__file__).with_name("Audit-LocalDemoLaunchers.py"))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class LauncherAuditTests(unittest.TestCase):
    def test_duplicate_links_count_once_and_missing_is_explicit(self):
        with tempfile.TemporaryDirectory() as root:
            report = module.audit(
                'Scripts/TestHosts/Demo.ps1 Scripts/TestHosts/Demo.ps1', root)
            self.assertEqual(1, report["published"])
            self.assertEqual(["Scripts/TestHosts/Demo.ps1"], report["missing"])
            self.assertFalse(report["nativeExecuted"])

    def test_present_file_is_source_proof_only(self):
        with tempfile.TemporaryDirectory() as root:
            target = Path(root) / "Scripts/TestHosts/Demo.ps1"
            target.parent.mkdir(parents=True)
            target.write_text("throw 'must not execute'", encoding="utf-8")
            report = module.audit("Scripts/TestHosts/Demo.ps1", root)
            self.assertEqual(1, report["present"])
            self.assertEqual(0, report["missingCount"])
            self.assertFalse(report["hostedAcceptance"])

    def test_directory_does_not_satisfy_launcher(self):
        with tempfile.TemporaryDirectory() as root:
            (Path(root) / "Scripts/TestHosts/Demo.ps1").mkdir(parents=True)
            self.assertEqual(1, module.audit(
                "Scripts/TestHosts/Demo.ps1", root)["missingCount"])


if __name__ == "__main__":
    unittest.main()

"""Ownership isolation for the reusable cloud-only WEB4 Mongo fixture."""
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("owned_mongo", Path(__file__).with_name("gomap_web4_mongo.py"))
fixture = importlib.util.module_from_spec(spec)
spec.loader.exec_module(fixture)


class OwnershipTests(unittest.TestCase):
    def verify_cleanup(self, profile, foreign):
        calls = []
        removed = False
        name = profile + "-cql-mongo"
        label = "oasis." + profile + ".web4.owner"

        def docker(args, **kwargs):
            nonlocal removed
            calls.append(args)
            if args[1:3] == ["container", "inspect"]:
                if removed:
                    return subprocess.CompletedProcess(args, 1, "", "Error: No such container: " + name)
                state = {"Config": {"Labels": {label: "foreign" if foreign else profile + "-web4-123-1"}},
                         "State": {"Running": True}, "Mounts": [{"Type": "volume", "Name": "owned-volume"}]}
                return subprocess.CompletedProcess(args, 0, json.dumps([state]), "")
            if args[1] == "rm":
                removed = True
            if args[1:3] == ["volume", "inspect"]:
                return subprocess.CompletedProcess(args, 1, "", "Error response from daemon: get owned-volume: no such volume")
            return subprocess.CompletedProcess(args, 0, "", "")

        with tempfile.TemporaryDirectory(prefix="cosmic-owner-check-") as temporary:
            script = Path(temporary) / "Scripts/TestHosts/helper.py"
            script.parent.mkdir(parents=True)
            script.touch()
            with patch.object(fixture, "__file__", str(script)), patch.object(fixture.subprocess, "run", side_effect=docker), \
                    patch.dict(fixture.os.environ, {"GITHUB_ACTIONS": "true", "GITHUB_RUN_ID": "123", "GITHUB_RUN_ATTEMPT": "1"}), \
                    patch.object(sys, "argv", ["helper", "--operation", "stop", "--profile", profile]):
                if foreign:
                    with self.assertRaises(AssertionError):
                        fixture.main()
                    self.assertTrue(all(call[1:3] == ["container", "inspect"] for call in calls))
                    self.assertFalse(removed)
                else:
                    fixture.main()
                    self.assertIn(["docker", "stop", "--time", "20", name], calls)
                    self.assertIn(["docker", "rm", "--volumes", name], calls)
                    folder = {"gomap": "GOMapWeb4Current", "tron": "TronHeaderWeb4Current"}[profile]
                    proof = json.loads((Path(temporary) / "TestResults" / folder / "mongo-cleanup.json").read_text(encoding="utf-8"))
                    self.assertTrue(proof["ownedContainerAbsent"])
                    self.assertTrue(proof["anonymousVolumesAbsent"])

    def test_gomap_rejects_foreign_owner(self):
        self.verify_cleanup("gomap", True)

    def test_tron_rejects_foreign_owner(self):
        self.verify_cleanup("tron", True)

    def test_gomap_owned_cleanup(self):
        self.verify_cleanup("gomap", False)

    def test_tron_owned_cleanup(self):
        self.verify_cleanup("tron", False)


if __name__ == "__main__":
    unittest.main()

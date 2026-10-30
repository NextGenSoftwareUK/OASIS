import json
from pathlib import Path
import unittest
from garnet_fixture_lifecycle import stop_owned_garnet

ROOT = Path("/mnt/c/Source/OASIS-cosmic/TestResults/GarnetCqlCurrent/lifecycle-controls")

class Process:
    returncode = None
    terminated = False
    def poll(self): return self.returncode
    def terminate(self): self.terminated = True
    def wait(self, timeout): self.returncode = -15

class TeardownControls(unittest.TestCase):
    def setUp(self):
        ROOT.mkdir(exist_ok=True)
        self.assertEqual([], list(ROOT.iterdir()), "Never adopt existing test artifacts")
        self.process = Process()
    def tearDown(self):
        self.assertEqual(ROOT.resolve().parent.name, "GarnetCqlCurrent")
        for path in ROOT.iterdir():
            self.assertTrue(path.is_file() and not path.is_symlink())
            path.unlink()
        ROOT.rmdir()
    def test_pending_source_stays_live(self):
        (ROOT / "demo-seed.pending.json").write_text("{}")
        with self.assertRaisesRegex(RuntimeError, "still live"):
            stop_owned_garnet(ROOT, self.process)
        self.assertFalse(self.process.terminated)
    def test_acknowledged_uncheckpointed_source_stays_live(self):
        (ROOT / "source-write-0.ack.json").write_text("{}")
        with self.assertRaisesRegex(RuntimeError, "still live"):
            stop_owned_garnet(ROOT, self.process)
        self.assertFalse(self.process.terminated)
    def test_unknown_checkpoint_keeps_source_live_even_with_old_ack(self):
        (ROOT / "demo-seed.json").write_text("{}")
        (ROOT / "checkpoint.json").write_text(json.dumps(dict(acknowledged=True)))
        (ROOT / "checkpoint.pending.json").write_text("{}")
        with self.assertRaisesRegex(RuntimeError, "still live"):
            stop_owned_garnet(ROOT, self.process)
        self.assertFalse(self.process.terminated)
    def test_durable_checkpoint_allows_owned_termination(self):
        (ROOT / "demo-seed.json").write_text("{}")
        (ROOT / "checkpoint.json").write_text(json.dumps(dict(acknowledged=True)))
        stop_owned_garnet(ROOT, self.process)
        self.assertTrue(self.process.terminated)
    def test_no_source_allows_startup_failure_cleanup(self):
        stop_owned_garnet(ROOT, self.process)
        self.assertTrue(self.process.terminated)
    def test_corrupt_checkpoint_cannot_kill_live_source(self):
        (ROOT / "demo-seed.json").write_text("{}")
        (ROOT / "checkpoint.json").write_text("not-json")
        with self.assertRaises(json.JSONDecodeError):
            stop_owned_garnet(ROOT, self.process)
        self.assertFalse(self.process.terminated)

if __name__ == "__main__": unittest.main()

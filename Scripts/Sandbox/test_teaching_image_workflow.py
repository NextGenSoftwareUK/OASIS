"""Execute the actual image-name step to protect native runtime artifact identity."""
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

import yaml

ROOT = Path(__file__).resolve().parents[2]


class TeachingImageWorkflowTests(unittest.TestCase):
    def run_names(self, selection):
        workflow = yaml.load((ROOT / ".github/workflows/build-web4-image.yml").read_text(), Loader=yaml.BaseLoader)
        selected = workflow["on"]["workflow_dispatch"]["inputs"]["teaching_sidecars"]
        self.assertEqual((selected["type"], selected["default"]), ("boolean", "false"))
        step = next(step for step in workflow["jobs"]["image"]["steps"] if step.get("id") == "names")
        bash = Path("C:/Program Files/Git/bin/bash.exe") if os.name == "nt" else shutil.which("bash")
        if not bash or not Path(bash).is_file():
            self.fail("Git Bash or bash is required to execute the shipping image-name step")
        results = ROOT / "TestResults/TeachingImageWorkflowCurrent"
        results.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(dir=results) as directory:
            output = Path(directory) / "outputs"
            environment = os.environ | {"REPOSITORY_OWNER": "NextGenSoftwareUK", "EXTRA_IMAGE_TAG": "",
                "TEACHING_SIDECARS": selection, "GITHUB_OUTPUT": output.as_posix()}
            process = subprocess.run([str(bash), "--noprofile", "--norc", "-c", step["run"]],
                cwd=ROOT, env=environment, capture_output=True, text=True, timeout=15)
            return process, output.read_text().splitlines()

    def test_default_profile_keeps_its_commit_tag(self):
        process, lines = self.run_names("false")
        self.assertEqual(process.returncode, 0, process.stderr)
        commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
        self.assertIn("ghcr.io/nextgensoftwareuk/oasis-web4:" + commit, lines)

    def test_native_profile_has_a_distinct_commit_tag(self):
        process, lines = self.run_names("true")
        self.assertEqual(process.returncode, 0, process.stderr)
        commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
        self.assertIn("ghcr.io/nextgensoftwareuk/oasis-web4:" + commit + "-teaching", lines)
        self.assertNotIn("ghcr.io/nextgensoftwareuk/oasis-web4:" + commit, lines)

    def test_invalid_profile_produces_no_image_tags(self):
        process, lines = self.run_names("unexpected")
        self.assertNotEqual(process.returncode, 0)
        self.assertNotIn("tags<<TAGS", lines)


if __name__ == "__main__":
    unittest.main()

import base64
import json
import os
from pathlib import Path
from runpy import run_path
import subprocess
import unittest

TOOL = run_path(str(Path(__file__).with_name("Provision-HostedTeachingSource.py")))
HASH = "a" * 64
ADDRESS = "/orbitdb/z" + "a" * 45


def output(value):
    return TOOL["MARKER"] + json.dumps(value).encode() + b"\n"


class TeachingProvisioningTests(unittest.TestCase):
    def test_native_receipts_bind_the_exact_plan_and_provider_identity(self):
        create = {"schema": 1, "planSha256": HASH, "address": None}
        self.assertEqual(TOOL["receipt"](output(create), "create", HASH, "GUNOASIS"), create)
        proof = create | {"address": ADDRESS, "publicRecords": 3,
            "privateRecords": 1, "deletedRecords": 1, "missingRecords": 1}
        self.assertEqual(TOOL["receipt"](output(proof), "verify", HASH, "OrbitDBOASIS"), proof)
        for changed in (proof | {"planSha256": "b" * 64}, proof | {"privateRecords": 0},
                        proof | {"address": "unreviewed"}, proof | {"publicRecords": True}):
            with self.subTest(changed=changed), self.assertRaises(RuntimeError):
                TOOL["receipt"](output(changed), "verify", HASH, "OrbitDBOASIS")

    def test_uncertain_or_ambiguous_output_is_not_an_acknowledgement(self):
        proof = {"schema": 1, "planSha256": HASH, "address": None}
        for raw in (b"", b"SDK started\n", output(proof) + output(proof)):
            with self.subTest(raw=raw), self.assertRaises(RuntimeError):
                TOOL["receipt"](raw, "create", HASH, "GUNOASIS")

    def test_remote_guard_rejects_other_deployments_and_active_native_readers_before_evaluation(self):
        for environment, reason in (({"RAILWAY_DEPLOYMENT_ID": "other", "RAILWAY_REPLICA_ID": "replica"},
                                     b"Reviewed deployment/replica changed"),
                                    ({"RAILWAY_DEPLOYMENT_ID": "deployment", "RAILWAY_REPLICA_ID": "replica",
                                      "OASIS_SANDBOX_GUN": "true"}, b"Native readers must remain off")):
            with self.subTest(environment=environment):
                result = subprocess.run(["node", "--eval", TOOL["REMOTE"], "invalid-helper",
                    "invalid-plan", "createSource", "deployment", "replica"],
                    env=os.environ | environment, capture_output=True, timeout=10)
                self.assertNotEqual(result.returncode, 0)
                self.assertNotIn(TOOL["MARKER"], result.stdout)
                self.assertIn(reason, result.stderr)

    def test_remote_command_preserves_the_structured_plan_and_returns_one_receipt(self):
        proof = {"schema": 1, "planSha256": HASH, "address": None}
        helper = ("module.exports.createSource=async plan=>{if(plan.provider!=='GUNOASIS')"
                  "throw Error('wrong plan');return " + json.dumps(proof) + ";};").encode()
        result = subprocess.run(["node", "--eval", TOOL["REMOTE"], base64.b64encode(helper).decode(),
            base64.b64encode(b'{"provider":"GUNOASIS"}').decode(), "createSource", "deployment", "replica"],
            env=os.environ | {"RAILWAY_DEPLOYMENT_ID": "deployment", "RAILWAY_REPLICA_ID": "replica",
                              "OASIS_SANDBOX_GUN": "false", "OASIS_SANDBOX_ORBITDB": "false"},
            capture_output=True, timeout=10)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(TOOL["receipt"](result.stdout, "create", HASH, "GUNOASIS"), proof)


if __name__ == "__main__":
    unittest.main()

import copy
import unittest
from unittest.mock import patch
import railway_release_gate as gate


class ReleaseGateContracts(unittest.TestCase):
    def candidate(self):
        return {"candidate": "a" * 40}

    def report(self):
        return {"candidate": self.candidate(), "environment": "staging", "accepted": True,
                "acceptedAt": 1000, "checks": [{}] * 10,
                "deployments": {n: "deployment-" + n for n in gate.CONFIG["services"]}}

    def test_complete_staging_evidence_accepted(self):
        gate.validate_staging_evidence(self.report(), self.candidate(), now=1001)

    def test_different_candidate_rejected(self):
        with self.assertRaisesRegex(ValueError, "candidate mismatch"):
            gate.validate_staging_evidence(self.report(), {"candidate": "b" * 40}, now=1001)

    def test_failed_or_partial_acceptance_rejected(self):
        for field, value in (("accepted", False), ("checks", [{}] * 9), ("deployments", {})):
            report = self.report()
            report[field] = value
            with self.assertRaises(ValueError):
                gate.validate_staging_evidence(report, self.candidate(), now=1001)

    def test_expired_and_future_evidence_rejected(self):
        for now in (999, 1000 + 6 * 3600 + 1):
            with self.assertRaisesRegex(ValueError, "expired"):
                gate.validate_staging_evidence(self.report(), self.candidate(), now=now)

    def test_wrong_sha_environment_service_and_failure_rejected(self):
        item = {"serviceId": "service", "environmentId": gate.CONFIG["environments"]["staging"],
                "status": "SUCCESS", "meta": {"commitHash": "a" * 40}}
        gate.validate_deployment(item, self.candidate(), "staging", "service")
        for field, value in (("serviceId", "foreign"), ("environmentId", "foreign"),
                             ("status", "FAILED"), ("meta", {"commitHash": "b" * 40})):
            changed = copy.deepcopy(item)
            changed[field] = value
            with self.assertRaises(ValueError):
                gate.validate_deployment(changed, self.candidate(), "staging", "service")

    def test_autodeploy_bypass_rejected(self):
        scope = {"projectId": gate.CONFIG["projectId"], "environmentId": gate.CONFIG["environments"]["staging"]}
        with patch.object(gate, "api", side_effect=[{"projectToken": scope}, {"serviceInstanceAutoDeployStatus": {"enabled": True}}]):
            with self.assertRaisesRegex(ValueError, "bypass"):
                gate.preflight("staging", "not-a-real-token")

    def test_wrong_environment_token_rejected_before_deploy(self):
        scope = {"projectId": gate.CONFIG["projectId"], "environmentId": gate.CONFIG["environments"]["production"]}
        with patch.object(gate, "api", return_value={"projectToken": scope}):
            with self.assertRaisesRegex(ValueError, "scoped"):
                gate.preflight("staging", "not-a-real-token")

    def test_staging_live_stripe_and_wrong_database_rejected(self):
        import json
        values = {"OASIS_DNA_JSON": json.dumps({"OASIS": {"StorageProviders": {"MongoDBOASIS": {"DBName": "staging"}}}}),
                  "STRIPE_SECRET_KEY": "sk_test_not_a_real_key"}
        gate.validate_staging_variables(values, "staging")
        for changed, expected in ((dict(values, STRIPE_SECRET_KEY="sk_live_not_a_real_key"), "staging"), (values, "other"), (values, None)):
            with self.assertRaises(ValueError):
                gate.validate_staging_variables(changed, expected)

    def test_superseded_or_inactive_candidate_rejected(self):
        item = {"id": "candidate", "environmentId": "env", "serviceId": "service"}
        for latest, active in (("other", ["candidate"]), ("candidate", ["other"])):
            response = {"serviceInstance": {"latestDeployment": {"id": latest}, "activeDeployments": [{"id": x} for x in active]}}
            with patch.object(gate, "api", return_value=response):
                with self.assertRaises(ValueError):
                    gate.assert_current(item, "not-a-real-token")

    def test_production_does_not_mutate_on_failed_staging(self):
        with patch.object(gate, "validate_candidate"), patch.object(gate, "deploy") as deploy, \
             patch.object(gate.Path, "read_text", side_effect=[__import__('json').dumps(self.candidate()), __import__('json').dumps(dict(self.report(), accepted=False))]), \
             patch('sys.argv', ['gate', 'production', '--candidate', 'candidate.json', '--evidence', 'staging.json', '--output', 'out.json']):
            with self.assertRaises(ValueError):
                gate.main()
            deploy.assert_not_called()

    def test_workflow_keeps_native_approval_and_serialization(self):
        workflow = (gate.ROOT / '.github/workflows/railway-staged-release.yml').read_text()
        self.assertIn('environment: oasis-production-gated', workflow)
        self.assertIn('needs: [freeze, staging]', workflow)
        self.assertIn('cancel-in-progress: false', workflow)
        self.assertNotIn('pull_request_target', workflow)

    def test_every_deployment_mutation_uses_frozen_sha(self):
        candidate = self.candidate()
        services = list(gate.CONFIG['services'].items())
        responses = [{'serviceInstanceDeployV2': 'deployment-' + n} for n, _ in services]
        def deployment(identity, token):
            number = identity.removeprefix('deployment-')
            return {'id': identity, 'status': 'SUCCESS', 'serviceId': gate.CONFIG['services'][number],
                    'environmentId': gate.CONFIG['environments']['staging'], 'meta': {'commitHash': candidate['candidate']}}
        with patch.object(gate, 'preflight'), patch.object(gate, 'write'), \
             patch.object(gate, 'api', side_effect=responses) as api, \
             patch.object(gate, 'deployment', side_effect=deployment), patch.object(gate, 'assert_current'):
            gate.deploy(candidate, 'staging', 'not-a-real-token', 'unused.json')
        self.assertEqual(7, api.call_count)
        for call in api.call_args_list:
            self.assertEqual(candidate['candidate'], call.args[1]['sha'])
            self.assertIn('commitSha:$sha', call.args[0])
            self.assertNotIn('latestCommit', call.args[0])


if __name__ == '__main__':
    unittest.main()

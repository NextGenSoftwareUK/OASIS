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
                "stripeWebhook": {"id": gate.CONFIG['stagingStripeWebhook']['id'], "livemode": False, "status": "enabled"},
                "deployments": {n: "deployment-" + n for n in gate.CONFIG["services"]}}

    def test_complete_staging_evidence_accepted(self):
        gate.validate_staging_evidence(self.report(), self.candidate(), now=1001)

    def test_different_candidate_rejected(self):
        with self.assertRaisesRegex(ValueError, "candidate mismatch"):
            gate.validate_staging_evidence(self.report(), {"candidate": "b" * 40}, now=1001)

    def test_failed_or_partial_acceptance_rejected(self):
        for field, value in (("accepted", False), ("checks", [{}] * 9), ("deployments", {}), ("stripeWebhook", {})):
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

    def test_accounting_cannot_silently_use_production_default(self):
        import json
        values = {'OASIS_DNA_JSON': json.dumps({'OASIS': {'StorageProviders': {'MongoDBOASIS': {'DBName': 'staging'}}}})}
        with self.assertRaisesRegex(ValueError, 'accounting'):
            gate.validate_staging_variables(values, 'staging', 'staging_accounting')
        values['SUBSCRIPTION_MONGODB_DATABASE'] = 'staging_accounting'
        gate.validate_staging_variables(values, 'staging', 'staging_accounting')

    def test_database_identity_must_match_reviewed_staging_user(self):
        import json
        values = {'OASIS_DNA_JSON': json.dumps({'OASIS': {'StorageProviders': {'MongoDBOASIS': {
            'DBName': 'staging', 'ConnectionString': 'mongodb://stage_user:fictional@localhost/staging'}}}})}
        gate.validate_staging_variables(values, 'staging', expected_user='stage_user')
        with self.assertRaisesRegex(ValueError, 'credential'):
            gate.validate_staging_variables(values, 'staging', expected_user='different_user')

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

    def test_webhook_cannot_activate_before_staging_acceptance(self):
        with patch.object(gate, 'stripe_endpoint') as stripe:
            with self.assertRaisesRegex(ValueError, 'acceptance'):
                gate.activate_staging_webhook({'environment': 'staging', 'accepted': False}, 'not-a-token')
            stripe.assert_not_called()

    def test_live_embedded_stripe_key_is_rejected_even_with_test_override(self):
        import json
        values = {'OASIS_DNA_JSON': json.dumps({'OASIS': {'StorageProviders': {'MongoDBOASIS': {'DBName': 'staging'}},
                    'SubscriptionConfig': {'Stripe': {'SecretKey': 'sk_live_fictional'}}}}), 'STRIPE_SECRET_KEY': 'sk_test_fictional'}
        with self.assertRaisesRegex(ValueError, 'test-mode'):
            gate.validate_staging_variables(values, 'staging')

    def test_webhook_foreign_url_or_live_mode_prevents_activation(self):
        import json
        config = copy.deepcopy(gate.CONFIG)
        config['stagingIsolation']['mongoUsername'] = 'stage_user'
        values = {'OASIS_DNA_JSON': json.dumps({'OASIS': {'StorageProviders': {'MongoDBOASIS': {
            'DBName': config['stagingIsolation']['mongoDbName'], 'ConnectionString': 'mongodb://stage_user:fictional@localhost/staging'}}}}),
            'STRIPE_SECRET_KEY': 'sk_test_fictional', 'SUBSCRIPTION_MONGODB_DATABASE': config['stagingIsolation']['accountingDbName']}
        endpoint = {'id': config['stagingStripeWebhook']['id'], 'url': config['stagingStripeWebhook']['url'],
                    'api_version': config['stagingStripeWebhook']['apiVersion'], 'livemode': False, 'status': 'disabled'}
        for change in ({'url': 'https://foreign.invalid'}, {'livemode': True}):
            with patch.object(gate, 'CONFIG', config), patch.object(gate, 'service_variables', return_value=values), \
                 patch.object(gate, 'stripe_endpoint', return_value=dict(endpoint, **change)) as stripe:
                with self.assertRaisesRegex(ValueError, 'differs'):
                    gate.checked_staging_webhook('not-a-token', activate=True)
                self.assertEqual(1, stripe.call_count)

    def test_postapproval_read_cannot_silently_reactivate_disabled_webhook(self):
        with patch.object(gate, 'service_variables', return_value={}), patch.object(gate, 'validate_staging_variables'), \
             patch.dict(gate.CONFIG['stagingIsolation'], {'mongoUsername': 'stage_user'}), \
             patch.object(gate, 'stripe_endpoint', return_value=dict(id=gate.CONFIG['stagingStripeWebhook']['id'],
                url=gate.CONFIG['stagingStripeWebhook']['url'], api_version=gate.CONFIG['stagingStripeWebhook']['apiVersion'],
                livemode=False, status='disabled')) as stripe:
            with self.assertRaisesRegex(ValueError, 'activation'):
                gate.checked_staging_webhook('not-a-token')
            self.assertEqual(1, stripe.call_count)


if __name__ == '__main__':
    unittest.main()

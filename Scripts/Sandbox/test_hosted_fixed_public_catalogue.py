from pathlib import Path
from runpy import run_path
import unittest
from unittest.mock import Mock, call


publish = run_path(str(Path(__file__).with_name("Publish-HostedFixedPublicCatalogue.py")))["publish"]


class FixedPublicCatalogueTests(unittest.TestCase):
    provider = "PushProtocolOASIS"
    deployment = "00000000-0000-0000-0000-000000000001"

    def setUp(self):
        self.before = {"preserved": "current"}
        self.binding = {"source-owner": "reviewed"}
        self.after = self.before | self.binding | {
            "admission": "scoped", "grants": "reviewed", "CQL_HOSTED_QUERY_EXAMPLES__PushProtocolOASIS": "four"
        }
        self.config = Mock(STATE=Path("protected-state"))
        self.config.protected_read.side_effect = [self.before, self.after]
        self.config.variables.return_value = self.before
        self.verifier = Mock(REVIEWED_PUBLIC_CATALOGUE_PROVIDERS={self.provider})
        self.verifier.reviewed_exact_key_source.return_value = {"sourceBinding": self.binding}
        self.preparation = {
            "configuration": self.config, "verifier": self.verifier,
            "candidate": Mock(return_value=self.after.copy()), "ADMISSION": "admission", "GRANTS": "grants"
        }
        self.guard = Mock(return_value=self.deployment)

    def verify(self, *, apply=False):
        return publish(self.preparation, self.guard, (self.provider,), "push-protocol-public",
                       "reviewed-release", self.deployment, apply=apply)

    def test_preflight_has_no_mutation(self):
        self.assertEqual(4, self.verify())
        self.config.apply_values.assert_not_called()

    def test_stale_deployment_never_mutates(self):
        self.guard.return_value = "00000000-0000-0000-0000-000000000002"
        with self.assertRaisesRegex(RuntimeError, "deployment must be current"):
            self.verify(apply=True)
        self.config.apply_values.assert_not_called()

    def test_different_live_baseline_never_mutates(self):
        self.config.variables.return_value = {"preserved": "changed"}
        with self.assertRaisesRegex(RuntimeError, "differ from the reviewed baseline"):
            self.verify(apply=True)
        self.config.apply_values.assert_not_called()

    def test_apply_delegates_only_reviewed_settings_with_live_guard(self):
        self.assertEqual(4, self.verify(apply=True))
        args, kwargs = self.config.apply_values.call_args
        self.assertEqual(("push-protocol-public", ("source-owner", "admission", "grants",
                          "CQL_HOSTED_QUERY_EXAMPLES__PushProtocolOASIS")), args)
        self.assertEqual((self.before, self.after), kwargs["reviewed_snapshots"])
        kwargs["before_mutation"]()
        self.assertEqual([call(self.config, release_ref="reviewed-release")] * 2, self.guard.call_args_list)

    def test_batch_composes_shared_settings_before_any_write(self):
        second = "ReclaimProtocolOASIS"
        extra_binding = {"second-owner": "reviewed"}
        after = self.after | extra_binding | {"admission": "both", "grants": "both",
                                               "CQL_HOSTED_QUERY_EXAMPLES__ReclaimProtocolOASIS": "four"}
        self.config.protected_read.side_effect = [self.before, after]
        self.verifier.REVIEWED_PUBLIC_CATALOGUE_PROVIDERS.add(second)
        self.verifier.reviewed_exact_key_source.side_effect = [
            {"sourceBinding": self.binding}, {"sourceBinding": extra_binding}
        ]
        self.preparation["candidate"].side_effect = [self.after, after]
        self.assertEqual(6, publish(self.preparation, self.guard, (self.provider, second), "batch",
                                   "reviewed-release", self.deployment, apply=True))
        self.preparation["candidate"].assert_has_calls([
            call(self.before | self.binding, self.provider, {"sourceBinding": self.binding}),
            call(self.after | extra_binding, second, {"sourceBinding": extra_binding})
        ])
        self.assertEqual(("source-owner", "second-owner", "admission", "grants",
                          "CQL_HOSTED_QUERY_EXAMPLES__PushProtocolOASIS",
                          "CQL_HOSTED_QUERY_EXAMPLES__ReclaimProtocolOASIS"), self.config.apply_values.call_args.args[1])

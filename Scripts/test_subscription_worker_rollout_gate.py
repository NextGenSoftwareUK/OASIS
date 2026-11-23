"""Regression controls for the shipping WEB4 worker rollout gate; no database or runtime."""
import unittest
from validate_subscription_rollout_gate import web4_worker_gate_errors

VALID = """
if (subscriptionUsageEnabled)
{
    Services.Operations.HostedMongoWorkerControl.AddMaintenanceWorker<Services.Subscription.SubscriptionUsageExpiryWorker>(services, Configuration);
    Services.Operations.HostedMongoWorkerControl.AddMaintenanceWorker<Services.Subscription.PaidOverageBillingWorker>(services, Configuration);
    NextGenSoftware.OASIS.API.Core.Services.Subscriptions.SubscriptionTelemetryRegistration.AddSubscriptionUsageTelemetry(services, Configuration, "WEB4");
}
Services.SubscriptionReconciliation.UsageReconciliationRegistration.AddUsageReconciliation(services,
    subscriptionUsageEnabled && Services.Operations.HostedMongoWorkerControl.ReconciliationMayStart(Configuration));
"""

class Web4WorkerGateTests(unittest.TestCase):
    def test_current_dual_gates_are_accepted(self):
        self.assertEqual([], web4_worker_gate_errors(VALID))

    def test_reconciliation_cannot_drop_either_gate(self):
        for replacement in ("subscriptionUsageEnabled", "Services.Operations.HostedMongoWorkerControl.ReconciliationMayStart(Configuration)", "true"):
            with self.subTest(replacement=replacement):
                source = VALID.replace("subscriptionUsageEnabled && Services.Operations.HostedMongoWorkerControl.ReconciliationMayStart(Configuration)", replacement)
                self.assertTrue(web4_worker_gate_errors(source))

    def test_reconciliation_cannot_use_or(self):
        self.assertTrue(web4_worker_gate_errors(VALID.replace(" && ", " || ")))

    def test_every_maintenance_worker_requires_host_control(self):
        for worker in ("SubscriptionUsageExpiryWorker", "PaidOverageBillingWorker"):
            with self.subTest(worker=worker):
                source = VALID.replace("Services.Operations.HostedMongoWorkerControl.AddMaintenanceWorker<Services.Subscription." + worker + ">(services, Configuration)", "services.AddHostedService<Services.Subscription." + worker + ">()")
                self.assertTrue(web4_worker_gate_errors(source))

    def test_subscription_block_cannot_be_unconditional(self):
        self.assertTrue(web4_worker_gate_errors(VALID.replace("if (subscriptionUsageEnabled)", "if (true)")))

    def test_telemetry_must_stay_inside_gate(self):
        line = 'NextGenSoftware.OASIS.API.Core.Services.Subscriptions.SubscriptionTelemetryRegistration.AddSubscriptionUsageTelemetry(services, Configuration, "WEB4");'
        self.assertTrue(web4_worker_gate_errors(VALID.replace(line, "") + line))

if __name__ == "__main__":
    unittest.main()

"""Reject worker startup when either rollout or operator-pause protection is lost."""
import unittest
from subscription_worker_gate import validate_web4_workers

STARTUP = '''
bool subscriptionUsageEnabled = string.Equals(Configuration["SUBSCRIPTION_USAGE_ENABLED"], "true", StringComparison.OrdinalIgnoreCase);
if (subscriptionUsageEnabled)
{
    Services.Operations.HostedMongoWorkerControl.AddMaintenanceWorker<Services.Subscription.SubscriptionUsageExpiryWorker>(services, Configuration);
    Registration.AddSubscriptionUsageTelemetry(services, Configuration, "WEB4");
}
Services.SubscriptionReconciliation.UsageReconciliationRegistration.AddUsageReconciliation(services,
    subscriptionUsageEnabled && Services.Operations.HostedMongoWorkerControl.ReconciliationMayStart(Configuration));
'''
CONTROL = '''
public const string PauseReconciliationKey = "OASIS_PAUSE_USAGE_RECONCILIATION";
public const string PauseMaintenanceKey = "OASIS_PAUSE_APPLICATION_MAINTENANCE";
public static bool ReconciliationMayStart(IConfiguration configuration)
    => !configuration.GetValue<bool>(PauseReconciliationKey);
public static void AddMaintenanceWorker<T>(IServiceCollection services, IConfiguration configuration)
    where T : class, IHostedService
{
    if (!configuration.GetValue<bool>(PauseMaintenanceKey))
        services.AddHostedService<T>();
}
'''
class WorkerGateTests(unittest.TestCase):
    def test_current_guarded_contract(self):
        self.assertEqual([], validate_web4_workers(STARTUP, CONTROL))

    def test_whitespace_between_reconciliation_arguments(self):
        self.assertEqual([], validate_web4_workers(STARTUP.replace("services,\n    subscriptionUsageEnabled", "services, subscriptionUsageEnabled"), CONTROL))

    def test_reject_either_gate_removed_or_weakened(self):
        for before, after in [
            ("subscriptionUsageEnabled && ", ""),
            (" && Services.Operations.HostedMongoWorkerControl.ReconciliationMayStart(Configuration)", ""),
            ("subscriptionUsageEnabled &&", "subscriptionUsageEnabled ||"),
            ('Configuration["SUBSCRIPTION_USAGE_ENABLED"]', 'Configuration["OTHER_SETTING"]'),
            ("if (subscriptionUsageEnabled)", "if (true)"),
        ]:
            with self.subTest(after=after):
                self.assertTrue(validate_web4_workers(STARTUP.replace(before, after), CONTROL))

    def test_reject_unconditional_duplicate_reconciliation(self):
        self.assertTrue(validate_web4_workers(STARTUP + "\nRegistration.AddUsageReconciliation(services, true);", CONTROL))

    def test_reject_unconditional_duplicate_expiry(self):
        duplicate = "\nServices.Operations.HostedMongoWorkerControl.AddMaintenanceWorker<Services.Subscription.SubscriptionUsageExpiryWorker>(services, Configuration);"
        self.assertTrue(validate_web4_workers(STARTUP + duplicate, CONTROL))

    def test_reject_direct_expiry_registration(self):
        self.assertTrue(validate_web4_workers(STARTUP + "\nservices.AddHostedService<Services.Subscription.SubscriptionUsageExpiryWorker>();", CONTROL))

    def test_reject_operator_pause_helpers_weakened(self):
        for before, after in [
            ("=> !configuration", "=> configuration"),
            ("if (!configuration", "if (configuration"),
            ("OASIS_PAUSE_USAGE_RECONCILIATION", "OTHER_RECONCILIATION_KEY"),
            ("OASIS_PAUSE_APPLICATION_MAINTENANCE", "OTHER_MAINTENANCE_KEY"),
        ]:
            with self.subTest(after=after):
                self.assertTrue(validate_web4_workers(STARTUP, CONTROL.replace(before, after)))

    def test_reject_telemetry_outside_rollout_block(self):
        inside = '    Registration.AddSubscriptionUsageTelemetry(services, Configuration, "WEB4");'
        self.assertTrue(validate_web4_workers(STARTUP.replace(inside, "") + "\n" + inside, CONTROL))

if __name__ == "__main__":
    unittest.main()

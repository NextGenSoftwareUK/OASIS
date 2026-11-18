"""Structural WEB4 worker rollout contract; no runtime or database access."""
import re

def validate_web4_workers(startup, control):
    errors = []
    declaration = r'bool\s+subscriptionUsageEnabled\s*=\s*string\.Equals\(Configuration\["SUBSCRIPTION_USAGE_ENABLED"\],\s*"true",\s*StringComparison\.OrdinalIgnoreCase\);'
    if not re.search(declaration, startup):
        errors.append("WEB4 usage gate must read the explicit enabled configuration.")
    reconciliation = r'UsageReconciliationRegistration\.AddUsageReconciliation\(services,\s*subscriptionUsageEnabled\s*&&\s*Services\.Operations\.HostedMongoWorkerControl\.ReconciliationMayStart\(Configuration\)\);'
    if not re.search(reconciliation, startup) or len(re.findall(r'\.AddUsageReconciliation\(', startup)) != 1:
        errors.append("WEB4 reconciliation requires both rollout enablement and the operator pause guard.")
    enabled = re.search(r'if\s*\(subscriptionUsageEnabled\)\s*\{(?P<body>[^{}]*)\}', startup)
    body = enabled.group("body") if enabled else ""
    expiry = "HostedMongoWorkerControl.AddMaintenanceWorker<Services.Subscription.SubscriptionUsageExpiryWorker>(services, Configuration);"
    telemetry = 'AddSubscriptionUsageTelemetry(services, Configuration, "WEB4")'
    if expiry not in body or startup.count(expiry) != 1 or re.search(r'AddHostedService\s*<\s*Services\.Subscription\.SubscriptionUsageExpiryWorker\s*>', startup):
        errors.append("WEB4 expiry worker requires rollout enablement and the maintenance pause guard.")
    if telemetry not in body:
        errors.append("WEB4 usage telemetry must remain rollout-gated.")
    reconciliation_guard = r'public\s+static\s+bool\s+ReconciliationMayStart\(IConfiguration\s+configuration\)\s*=>\s*!configuration\.GetValue<bool>\(PauseReconciliationKey\);'
    maintenance_guard = r'public\s+static\s+void\s+AddMaintenanceWorker<T>\(IServiceCollection\s+services,\s*IConfiguration\s+configuration\)\s*where\s+T\s*:\s*class,\s*IHostedService\s*\{\s*if\s*\(!configuration\.GetValue<bool>\(PauseMaintenanceKey\)\)\s*services\.AddHostedService<T>\(\);\s*\}'
    if not re.search(reconciliation_guard, control) or 'PauseReconciliationKey = "OASIS_PAUSE_USAGE_RECONCILIATION"' not in control:
        errors.append("WEB4 reconciliation pause helper must preserve its configured negative guard.")
    if not re.search(maintenance_guard, control) or 'PauseMaintenanceKey = "OASIS_PAUSE_APPLICATION_MAINTENANCE"' not in control:
        errors.append("WEB4 maintenance registration helper must preserve its configured negative guard.")
    return errors

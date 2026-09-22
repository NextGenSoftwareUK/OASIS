using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.SubscriptionReconciliation;

/// <summary>Scans distinct account-month keys from audit, projections and external evidence. Never changes accounting totals.</summary>
public sealed class UsageReconciliationWorker : BackgroundService
{
    private readonly MongoUsageReconciler _reconciler;
    private readonly StripeUsageEvidenceReader _stripe;
    private readonly ISubscriptionService _subscriptions;
    private readonly ILogger<UsageReconciliationWorker> _logger;
    public UsageReconciliationWorker(MongoUsageReconciler reconciler, StripeUsageEvidenceReader stripe,
        ISubscriptionService subscriptions, ILogger<UsageReconciliationWorker> logger)
    { _reconciler = reconciler; _stripe = stripe; _subscriptions = subscriptions; _logger = logger; }

    public override async Task StartAsync(CancellationToken ct)
    {
        await _reconciler.InitializeAsync(ct);
        await base.StartAsync(ct);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string cursor = "";
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var work = await _reconciler.GetWorkAsync(cursor, 25, stoppingToken);
                foreach (var account in work)
                {
                    try
                    {
                        var subscription = await _subscriptions.GetSubscriptionAsync(account.UserId);
                        if (subscription == null) throw new InvalidOperationException("Ledger account has no subscription: " + account.UserId);
                        await _stripe.CollectAsync(subscription.StripeCustomerId, account.UserId, account.Month, stoppingToken);
                        var report = await _reconciler.ReconcileAsync(account.UserId, account.Month, stoppingToken);
                        if (report.ActionRequired)
                            _logger.LogError("USAGE_RECONCILIATION_ACTION_REQUIRED report={ReportId} findings={Findings} unsettled={Unsettled}",
                                report.Id, report.Findings.Count, report.UnsettledOperations);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        // Persist failure before advancing; one invalid account must not starve later accounts.
                        await _reconciler.RecordFailedCheckAsync(account.UserId, account.Month, ex.GetType().Name, stoppingToken);
                        _logger.LogCritical(ex, "USAGE_ACCOUNT_RECONCILIATION_FAILED account={Account}; recheck on the next complete scan", account.Id);
                    }
                    cursor = account.Id;
                }
                if (work.Count < 25) cursor = "";
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                // A failed cycle is visible and not represented as a balanced report. Retry the same cursor next cycle.
                _logger.LogCritical(ex, "USAGE_RECONCILIATION_FAILED cursor={Cursor}; retrying after 60 seconds", cursor);
            }
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}

public static class UsageReconciliationRegistration
{
    public static IServiceCollection AddUsageReconciliation(this IServiceCollection services)
    {
        services.AddSingleton<MongoUsageReconciler>();
        services.AddHttpClient<StripeUsageEvidenceReader>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddHostedService<UsageReconciliationWorker>();
        return services;
    }
}

using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.TestSupport;

// Strict controlled ledger reused by controlled-RPC and native-source HTTP gates.
// Real authority authentication and Mongo durability remain separate acceptance.
public class IndexedHttpLedger : DispatchProxy
{
    public Guid Actor;
    public readonly Dictionary<string, UsageOutboxOperation> Operations = new();
    public readonly List<UsageAuthorizationRequest> Authorizations = new();
    public readonly List<UsageSettlementRequest> Settlements = new();

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        switch (method!.Name)
        {
            case nameof(IUsageReservationAuthority.AuthorizeUsageAsync):
                var authorization = (UsageAuthorizationRequest)args![1]!;
                Authorizations.Add(authorization);
                return Task.FromResult(new SubscriptionAuthorizationResult { Allowed = true, UserId = Actor.ToString("D"),
                    OperationId = authorization.OperationId, Status = "authorized", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) });
            case nameof(IUsageReservationAuthority.StartUsageAsync):
                return Task.FromResult(new UsageStartResult { Started = true, OperationId = ((UsageStartRequest)args![0]!).OperationId });
            case nameof(IUsageReservationAuthority.SettleUsageAsync):
                var settlement = (UsageSettlementRequest)args![0]!;
                Settlements.Add(settlement);
                return Task.FromResult(new UsageSettlementResult { Settled = true, OperationId = settlement.OperationId });
            case nameof(IUsageOutboxStore.TryBeginAsync):
                var operation = (UsageOutboxOperation)args![0]!;
                return Task.FromResult(Operations.TryAdd(operation.Id, operation));
            case nameof(IUsageOutboxStore.GetAsync):
                return Task.FromResult(Operations.GetValueOrDefault((string)args![0]!));
        }
        var op = Operations[(string)args![0]!];
        switch (method.Name)
        {
            case nameof(IUsageOutboxStore.BeginProviderAttemptAsync):
                Assert.AreEqual(op.ExecutionOwner, args[1]);
                var attempt = (UsageProviderAttempt)args[2]!;
                Assert.IsFalse(op.ProviderAttempts.Any(p => p.ClientRequestId == attempt.ClientRequestId));
                op.ProviderAttempts.Add(attempt); op.EvidenceRevision++;
                return Task.CompletedTask;
            case nameof(IUsageOutboxStore.AppendReceiptAsync):
                Assert.AreEqual(op.ExecutionOwner, args[1]);
                var receipt = (UsageProviderReceipt)args[2]!;
                receipt.Validate();
                Assert.IsTrue(op.ProviderAttempts.Any(p => p.ClientRequestId == receipt.ClientRequestId && p.Provider == receipt.Provider));
                op.Receipts.Add(UsageReceiptAccounting.Snapshot(receipt)); op.EvidenceRevision++;
                return Task.CompletedTask;
            case nameof(IUsageOutboxStore.RecordObservedOutcomeAsync):
                op.ObservedOutcome = (string)args[2]!;
                return Task.FromResult(op);
            case nameof(IUsageOutboxStore.EnqueueAsync):
                Assert.AreEqual(op.EvidenceRevision, (long)args[3]!);
                op.Settlement = (UsageSettlementRequest)args[2]!; op.State = "pending";
                return Task.CompletedTask;
            case nameof(IUsageOutboxStore.ClaimOperationAsync): return Task.FromResult(op);
            case nameof(IUsageOutboxStore.AcknowledgeAsync): op.State = "settled"; return Task.CompletedTask;
            default: throw new AssertFailedException("Unexpected ledger call: " + method.Name);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed record PendingSubscriptionCheckout(string OperationId, string PlanId, DateTime RequestedAtUtc, string StripeSessionId);

public interface ISubscriptionCheckoutJournal
{
    Task<IReadOnlyList<PendingSubscriptionCheckout>> GetPendingCheckoutsAsync(string avatarId, CancellationToken token);
    Task<string> BeginCheckoutAsync(string avatarId, string planId, CancellationToken token);
    Task RecordCheckoutSessionAsync(string avatarId, string operationId, string stripeSessionId, CancellationToken token);
    Task CloseCheckoutAsync(string avatarId, string operationId, string stripeSessionId, CancellationToken token);
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

internal sealed record AccountClosurePrerequisites(bool SubscriptionRequiresCancellation,
    bool HasOpenStripeInvoices, bool HasOwnedApplications, bool HasUnsettledUsage,
    bool HasUnbilledCharges, bool HasUnresolvedStatements);

internal sealed class AccountClosurePrerequisiteReader(MongoSubscriptionUsageRepository repository, IConfiguration configuration)
{
    // This does not cancel, charge, delete or authorize a closure. The caller must
    // perform those operations through their acknowledged state transitions.
    internal async Task<AccountClosurePrerequisites> ReadAsync(Guid avatarId, CancellationToken token)
    {
        var snapshot = await repository.ReadAccountClosureLedgerAsync(avatarId, token);
        var subscription = snapshot.Subscription;
        var cancel = false;
        var openInvoices = false;
        if (!string.IsNullOrWhiteSpace(subscription?.StripeSubscriptionId) && string.IsNullOrWhiteSpace(subscription.StripeCustomerId))
            throw new InvalidOperationException("The stored billing identity is incomplete; account closure cannot be assessed.");
        if (!string.IsNullOrWhiteSpace(subscription?.StripeCustomerId))
        {
            var client = new StripeClient(SubscriptionStripeConfiguration.SecretKey(configuration));
            if (!string.IsNullOrWhiteSpace(subscription.StripeSubscriptionId))
            {
                var canonical = await new Stripe.SubscriptionService(client).GetAsync(subscription.StripeSubscriptionId, cancellationToken: token);
                if (canonical.CustomerId != subscription.StripeCustomerId ||
                    canonical.Metadata?.GetValueOrDefault("avatar_id") is string owner &&
                        !string.IsNullOrEmpty(owner) && owner != avatarId.ToString("D"))
                    throw new InvalidOperationException("The canonical billing account does not match the authenticated avatar.");
                cancel = canonical.Status is not ("canceled" or "incomplete_expired");
            }
            var invoices = await new Stripe.InvoiceService(client).ListAsync(new InvoiceListOptions
                { Customer = subscription.StripeCustomerId, Status = "open", Limit = 1 }, cancellationToken: token);
            openInvoices = invoices.Data.Count != 0;
        }
        return new AccountClosurePrerequisites(cancel, openInvoices, snapshot.HasOwnedApplications,
            snapshot.HasUnsettledUsage, snapshot.HasUnbilledCharges, snapshot.HasUnresolvedStatements);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed partial class MongoSubscriptionUsageRepository
{
    // Called by the closure service after canonical external prerequisite checks.
    // This closes new billing admission; it does not delete an avatar or its stores.
    internal Task<bool> CloseBillingAdmissionAsync(Guid avatarId, Guid operationId,
        AccountClosureLedgerSnapshot verified, CancellationToken token)
    {
        if (avatarId == Guid.Empty || operationId == Guid.Empty || verified == null)
            throw new ArgumentException("Verified account and closure identities are required.");
        var actor = avatarId.ToString("D");
        var operation = operationId.ToString("D");
        if (verified.Subscription != null && verified.Subscription.UserId != actor)
            throw new ArgumentException("The verified billing snapshot belongs to another account.");
        return TransactAsync(async (session, ct) =>
        {
            await EnsureOpeningBalanceAsync(session, ct);
            var snapshot = await ReadAccountClosureLedgerInSessionAsync(session, actor, ct);
            var account = snapshot.Subscription;
            if (account?.AccountClosureOperationId == operation && account.AccountClosureStartedAtUtc.HasValue)
                return true;
            if (account?.IsBillingAdmissionOpen == false)
                throw new SubscriptionUsageConflictException("Another account closure already owns billing admission.");
            var prior = verified.Subscription;
            if ((account == null) != (prior == null) || account != null &&
                (account.UserId != actor || account.AuthorizationVersion != prior.AuthorizationVersion ||
                 account.PaidOveragePreferenceVersion != prior.PaidOveragePreferenceVersion || account.PlanId != prior.PlanId ||
                 account.Status != prior.Status || account.StripeCustomerId != prior.StripeCustomerId ||
                 account.StripeSubscriptionId != prior.StripeSubscriptionId || account.CurrentPeriodEnd != prior.CurrentPeriodEnd))
                throw new SubscriptionUsageConflictException("Billing changed during closure review. Refresh the account dependencies.");
            if (snapshot.HasOwnedApplications || snapshot.HasUnsettledUsage || snapshot.HasUnbilledCharges ||
                snapshot.HasUnresolvedStatements || snapshot.HasUnresolvedCheckouts)
                throw new SubscriptionUsageConflictException("Resolve owned applications, outstanding usage, charges and checkouts before closing the account.");
            if (account?.StripeSubscriptionId != null && account.Status is not ("canceled" or "incomplete_expired"))
                throw new SubscriptionUsageConflictException("The stored subscription has not confirmed cancellation.");
            var slotId = "checkout-slot:" + actor;
            var slot = await BillingAudit.Find(session, new BsonDocument("_id", slotId)).SingleOrDefaultAsync(ct);
            if (slot != null && (slot["UserId"].AsString != actor ||
                !slot.GetValue("ActiveOperationId", BsonNull.Value).IsBsonNull ||
                !slot.GetValue("AccountClosureOperationId", BsonNull.Value).IsBsonNull ||
                !slot.GetValue("AccountClosureStartedAtUtc", BsonNull.Value).IsBsonNull))
                throw new SubscriptionUsageConflictException("Checkout admission is reserved or its closure state conflicts.");
            var now = _time.GetUtcNow().UtcDateTime;
            account ??= new SubscriptionRecord { UserId = actor, PlanId = "free", Status = "closed", CreatedAt = now };
            account.AccountClosureOperationId = operation;
            account.AccountClosureStartedAtUtc = now;
            account.AuthorizationVersion = checked(account.AuthorizationVersion + 1);
            account.PaidOveragePreferenceVersion = checked(account.PaidOveragePreferenceVersion + 1);
            account.PayAsYouGoEnabled = false;
            account.UpdatedAt = now;
            var saved = await Subscriptions.ReplaceOneAsync(session, row => row.UserId == actor, account,
                new ReplaceOptions { IsUpsert = true }, ct);
            if (!saved.IsAcknowledged || saved.MatchedCount != 1 && saved.UpsertedId == null)
                throw new InvalidOperationException("Billing admission closure was not acknowledged.");
            var closed = await BillingAudit.UpdateOneAsync(session, new BsonDocument("_id", slotId),
                Builders<BsonDocument>.Update.SetOnInsert("UserId", actor).SetOnInsert("Kind", "checkout-reservation")
                    .Set("AccountClosureOperationId", operation).Set("AccountClosureStartedAtUtc", now).Set("OccurredAtUtc", now),
                new UpdateOptions { IsUpsert = true }, ct);
            if (!closed.IsAcknowledged || closed.MatchedCount != 1 && closed.UpsertedId == null)
                throw new InvalidOperationException("Checkout admission closure was not acknowledged.");
            await BillingAudit.InsertOneAsync(session, new BsonDocument {
                { "_id", "account-closure:" + operation }, { "Kind", "account-billing-admission-closed" },
                { "UserId", actor }, { "OperationId", operation }, { "OccurredAtUtc", now }
            }, cancellationToken: ct);
            return true;
        }, token);
    }
}

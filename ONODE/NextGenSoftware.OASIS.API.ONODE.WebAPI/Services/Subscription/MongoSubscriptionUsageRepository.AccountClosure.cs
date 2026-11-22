using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

internal sealed record AccountClosureLedgerSnapshot(SubscriptionRecord Subscription,
    bool HasOwnedApplications, bool HasUnsettledUsage, bool HasUnbilledCharges, bool HasUnresolvedStatements, bool HasUnresolvedCheckouts);

public sealed partial class MongoSubscriptionUsageRepository
{
    // A read snapshot, not permission to delete. Closure must linearize admission and
    // revalidate these conditions before committing any irreversible account changes.
    internal async Task<AccountClosureLedgerSnapshot> ReadAccountClosureLedgerAsync(Guid avatarId, CancellationToken token)
    {
        if (avatarId == Guid.Empty) throw new ArgumentException("An authenticated avatar is required.", nameof(avatarId));
        var actor = avatarId.ToString("D");
        using var session = await _client.StartSessionAsync(cancellationToken: token);
        session.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot, readPreference: ReadPreference.Primary));
        var subscription = await Subscriptions.Find(session, row => row.UserId == actor).SingleOrDefaultAsync(token);
        var ownedApps = await AppAccounts.Find(session, row => row.OwnerAvatarIds.Contains(actor)).AnyAsync(token);
        var unsettled = await _events.Find(session, row => row.UserId == actor && row.Status != "settled" && row.Status != "expired").AnyAsync(token);
        var unbilled = await _events.Find(session, row => row.UserId == actor && row.Status == "settled" &&
            row.PaidOveragePricingVersion != null && row.BillingStatementId == null &&
            (row.SettledRequestOverageUsd > 0 || row.SettledAiOverageUsd > 0 || row.PersonalAiCorrectionUsd > 0)).AnyAsync(token);
        var statements = await OverageStatements.Find(session, row => row.UserId == actor && row.Scope == OverageScope &&
            row.State != "paid" && row.State != "void" && row.State != "uncollectible").AnyAsync(token);
        var unresolvedCheckout = await BillingAudit.Aggregate(session)
            .Match(new BsonDocument { { "UserId", actor }, { "Kind", "checkout-requested" } })
            .AppendStage<BsonDocument>(new BsonDocument("$lookup", new BsonDocument {
                { "from", BillingAudit.CollectionNamespace.CollectionName }, { "localField", "ClosedEventId" },
                { "foreignField", "_id" }, { "as", "closureAcknowledgement" } }))
            .Match(new BsonDocument("closureAcknowledgement.0", new BsonDocument("$exists", false)))
            .Limit(1).FirstOrDefaultAsync(token);
        await session.CommitTransactionAsync(token);
        return new AccountClosureLedgerSnapshot(subscription, ownedApps, unsettled, unbilled, statements, unresolvedCheckout != null);
    }
}

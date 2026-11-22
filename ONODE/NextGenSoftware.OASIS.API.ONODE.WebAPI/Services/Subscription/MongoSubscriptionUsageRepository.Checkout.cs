using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed partial class MongoSubscriptionUsageRepository : ISubscriptionCheckoutJournal
{
    private static string CheckoutEventId(string operation, string kind) => "checkout:" + operation + ":" + kind;

    private static void ValidateCheckoutIdentity(string avatar, string operation)
    {
        if (!Guid.TryParseExact(avatar, "D", out var actor) || actor == Guid.Empty ||
            !Guid.TryParseExact(operation, "D", out var id) || id == Guid.Empty)
            throw new ArgumentException("Canonical account and checkout operation identities are required.");
    }

    public async Task<string> BeginCheckoutAsync(string avatarId, string planId, CancellationToken token)
    {
        var operation = Guid.NewGuid().ToString("D");
        ValidateCheckoutIdentity(avatarId, operation);
        if (string.IsNullOrWhiteSpace(planId)) throw new ArgumentException("A checkout plan is required.", nameof(planId));
        try
        {
            using var session = await _client.StartSessionAsync(cancellationToken: token);
            session.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot,
                readPreference: ReadPreference.Primary, writeConcern: WriteConcern.WMajority));
            var slotId = "checkout-slot:" + avatarId;
            var slot = await BillingAudit.Find(session, new BsonDocument("_id", slotId)).SingleOrDefaultAsync(token);
            if (slot != null && slot["UserId"].AsString != avatarId)
                throw new InvalidOperationException("The checkout reservation has conflicting account identity.");
            if (slot != null && !slot.GetValue("ActiveOperationId", BsonNull.Value).IsBsonNull)
                throw new CheckoutReservationException("CHECKOUT_ALREADY_PENDING",
                    "Review your unfinished checkout before starting another payment.");
            // One mutable reservation projection per account serializes creation.
            // Requested/created/closed event rows remain immutable audit evidence.
            var reservation = await BillingAudit.ReplaceOneAsync(session, new BsonDocument("_id", slotId), new BsonDocument {
                { "_id", slotId }, { "Kind", "checkout-reservation" }, { "UserId", avatarId },
                { "ActiveOperationId", operation }, { "OccurredAtUtc", _time.GetUtcNow().UtcDateTime }
            }, new ReplaceOptions { IsUpsert = true }, token);
            if (!reservation.IsAcknowledged || reservation.MatchedCount != 1 && reservation.UpsertedId == null)
                throw new InvalidOperationException("The checkout reservation was not acknowledged.");
            await BillingAudit.InsertOneAsync(session, new BsonDocument {
                { "_id", CheckoutEventId(operation, "requested") }, { "Kind", "checkout-requested" },
                { "UserId", avatarId }, { "CheckoutOperationId", operation }, { "PlanId", planId },
                { "CreatedEventId", CheckoutEventId(operation, "created") },
                { "ClosedEventId", CheckoutEventId(operation, "closed") }, { "OccurredAtUtc", _time.GetUtcNow().UtcDateTime }
            }, cancellationToken: token);
            await session.CommitTransactionAsync(token);
        }
        catch (MongoWriteException cause) when (cause.WriteError?.Code is 11000 or 112)
        {
            throw new CheckoutReservationException("CHECKOUT_RESERVATION_CONFLICT",
                "A concurrent checkout reservation conflicted. Review unfinished checkouts before trying again.", cause);
        }
        catch (MongoCommandException cause) when (cause.Code is 11000 or 112)
        {
            throw new CheckoutReservationException("CHECKOUT_RESERVATION_CONFLICT",
                "A concurrent checkout reservation conflicted. Review unfinished checkouts before trying again.", cause);
        }
        return operation;
    }

    public async Task<IReadOnlyList<PendingSubscriptionCheckout>> GetPendingCheckoutsAsync(string avatarId, CancellationToken token)
    {
        if (!Guid.TryParseExact(avatarId, "D", out var actor) || actor == Guid.Empty)
            throw new ArgumentException("An authenticated account is required.", nameof(avatarId));
        var rows = await BillingAudit.Aggregate()
            .Match(new BsonDocument { { "UserId", avatarId }, { "Kind", "checkout-requested" } })
            .AppendStage<BsonDocument>(new BsonDocument("$lookup", new BsonDocument {
                { "from", BillingAudit.CollectionNamespace.CollectionName }, { "localField", "ClosedEventId" },
                { "foreignField", "_id" }, { "as", "terminal" } }))
            .Match(new BsonDocument("terminal.0", new BsonDocument("$exists", false)))
            .AppendStage<BsonDocument>(new BsonDocument("$lookup", new BsonDocument {
                { "from", BillingAudit.CollectionNamespace.CollectionName }, { "localField", "CreatedEventId" },
                { "foreignField", "_id" }, { "as", "acknowledgement" } }))
            .Project(new BsonDocument { { "CheckoutOperationId", 1 }, { "PlanId", 1 }, { "OccurredAtUtc", 1 },
                { "acknowledgement.StripeSessionId", 1 } }).ToListAsync(token);
        return rows.Select(row => new PendingSubscriptionCheckout(row["CheckoutOperationId"].AsString,
            row["PlanId"].AsString, row["OccurredAtUtc"].ToUniversalTime(),
            row["acknowledgement"].AsBsonArray.Count == 0 ? null : row["acknowledgement"][0]["StripeSessionId"].AsString)).ToArray();
    }

    public Task RecordCheckoutSessionAsync(string avatarId, string operationId, string stripeSessionId, CancellationToken token)
        => AppendCheckoutAcknowledgementAsync(avatarId, operationId, stripeSessionId, false, token);

    // Call only after canonical Stripe expiry or confirmed subscription persistence.
    public Task CloseCheckoutAsync(string avatarId, string operationId, string stripeSessionId, CancellationToken token)
        => AppendCheckoutAcknowledgementAsync(avatarId, operationId, stripeSessionId, true, token);

    private async Task AppendCheckoutAcknowledgementAsync(string avatar, string operation, string providerSession,
        bool close, CancellationToken token)
    {
        ValidateCheckoutIdentity(avatar, operation);
        if (string.IsNullOrWhiteSpace(providerSession) || !providerSession.StartsWith("cs_", StringComparison.Ordinal))
            throw new ArgumentException("A canonical Stripe session identity is required.", nameof(providerSession));
        using var session = await _client.StartSessionAsync(cancellationToken: token);
        session.StartTransaction(new TransactionOptions(readConcern: ReadConcern.Snapshot,
            readPreference: ReadPreference.Primary, writeConcern: WriteConcern.WMajority));
        var requested = await BillingAudit.Find(session, new BsonDocument("_id", CheckoutEventId(operation, "requested")))
            .SingleOrDefaultAsync(token);
        if (requested == null || requested["UserId"].AsString != avatar)
            throw new InvalidOperationException("The checkout request does not belong to this account.");
        var slot = await BillingAudit.Find(session, new BsonDocument("_id", "checkout-slot:" + avatar)).SingleOrDefaultAsync(token);
        var activeOperation = slot?.GetValue("ActiveOperationId", BsonNull.Value);
        var ownsReservation = slot != null && slot["UserId"].AsString == avatar &&
            activeOperation != null && !activeOperation.IsBsonNull && activeOperation.AsString == operation;
        // Signed webhook reconciliation may supply the acknowledged session even if
        // the original HTTP response was lost. It uses the same immutable identity.
        var closedAlreadyRecorded = false;
        foreach (var kind in close ? new[] { "created", "closed" } : new[] { "created" })
        {
            var id = CheckoutEventId(operation, kind);
            var existing = await BillingAudit.Find(session, new BsonDocument("_id", id)).SingleOrDefaultAsync(token);
            if (existing != null)
            {
                if (existing["UserId"].AsString != avatar || existing["StripeSessionId"].AsString != providerSession)
                    throw new InvalidOperationException("The checkout operation has conflicting provider acknowledgement.");
                if (kind == "closed") closedAlreadyRecorded = true;
                continue;
            }
            if (!ownsReservation)
                throw new InvalidOperationException("The checkout acknowledgement does not own the active reservation.");
            await BillingAudit.InsertOneAsync(session, new BsonDocument {
                { "_id", id }, { "Kind", "checkout-" + kind }, { "UserId", avatar },
                { "CheckoutOperationId", operation }, { "StripeSessionId", providerSession },
                { "OccurredAtUtc", _time.GetUtcNow().UtcDateTime }
            }, cancellationToken: token);
        }
        if (close && ownsReservation)
        {
            if (closedAlreadyRecorded)
                throw new InvalidOperationException("A closed checkout still owns an active reservation.");
            var released = await BillingAudit.UpdateOneAsync(session,
                new BsonDocument { { "_id", "checkout-slot:" + avatar }, { "ActiveOperationId", operation } },
                Builders<BsonDocument>.Update.Unset("ActiveOperationId").Set("OccurredAtUtc", _time.GetUtcNow().UtcDateTime),
                cancellationToken: token);
            if (!released.IsAcknowledged || released.MatchedCount != 1)
                throw new InvalidOperationException("Checkout closure could not release its reservation.");
        }
        // Replaying an already-closed operation must never release a newer reservation.
        await session.CommitTransactionAsync(token);
    }
}

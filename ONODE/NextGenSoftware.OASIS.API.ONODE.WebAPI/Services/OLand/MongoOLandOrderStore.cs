using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.OLand;

/// <summary>Unique metre-parcel claims and paid ownership are committed atomically on Mongo replica sets.
/// No TTL index deletes a paid/unresolved payment reservation.</summary>
public sealed class MongoOLandOrderStore : IOLandOrderStore
{
    private readonly IMongoClient client;
    private readonly IMongoCollection<OLandOrder> orders;
    private readonly IMongoCollection<OLandParcelClaim> claims;
    private readonly IMongoCollection<BsonDocument> receipts;
    public MongoOLandOrderStore(IMongoClient client, string database)
    {
        this.client = client;
        var db = client.GetDatabase(database);
        orders = db.GetCollection<OLandOrder>("oland_orders");
        claims = db.GetCollection<OLandParcelClaim>("oland_parcel_claims");
        receipts = db.GetCollection<BsonDocument>("oland_payment_receipts");
    }
    private readonly SemaphoreSlim indexGate = new(1, 1);
    private volatile bool indexed;
    private async Task EnsureIndexes(CancellationToken token)
    {
        if (indexed) return;
        await indexGate.WaitAsync(token);
        try
        {
            if (indexed) return;
        orders.Indexes.CreateOne(new CreateIndexModel<OLandOrder>(Builders<OLandOrder>.IndexKeys.Ascending(x => x.AvatarId).Ascending(x => x.RequestId),
            new CreateIndexOptions { Unique = true, Name = "ux_oland_actor_request" }));
        orders.Indexes.CreateOne(new CreateIndexModel<OLandOrder>(Builders<OLandOrder>.IndexKeys.Ascending(x => x.ActiveAvatar),
            new CreateIndexOptions<OLandOrder> { Unique = true, Name = "ux_oland_active_actor", PartialFilterExpression = new BsonDocument("ActiveAvatar", new BsonDocument("$type", "string")) }));
            indexed = true;
        }
        finally { indexGate.Release(); }
    }
    public async Task<List<OLandOrder>> List(string avatar, string beforeOrderId, int limit, CancellationToken token)
    {
        if (limit < 1 || limit > 100) throw new ArgumentOutOfRangeException(nameof(limit));
        var f = Builders<OLandOrder>.Filter;
        var filter = f.Eq(x => x.AvatarId, avatar);
        if (beforeOrderId != null)
        {
            var anchor = await Get(beforeOrderId, token);
            if (anchor?.AvatarId != avatar) throw Conflict("Invalid purchase-history cursor.");
            filter &= f.Lt(x => x.CreatedUtc, anchor.CreatedUtc) | (f.Eq(x => x.CreatedUtc, anchor.CreatedUtc) & f.Lt(x => x.Id, anchor.Id));
        }
        return await orders.Find(filter).SortByDescending(x => x.CreatedUtc).ThenByDescending(x => x.Id).Limit(limit).ToListAsync(token);
    }
    public Task<List<OLandOrder>> PendingMint(CancellationToken token) => orders.Find(x => x.State == "paid" && x.MintState == "not_started").Limit(25).ToListAsync(token);
    public Task<OLandOrder> Get(string id, CancellationToken token) => orders.Find(x => x.Id == id).FirstOrDefaultAsync(token);
    public Task<OLandOrder> FindRequest(string avatar, string request, CancellationToken token) => orders.Find(x => x.AvatarId == avatar && x.RequestId == request).FirstOrDefaultAsync(token);
    public async Task<Dictionary<string, OLandParcelClaim>> Claims(IReadOnlyList<string> parcels, CancellationToken token) =>
        (await claims.Find(Builders<OLandParcelClaim>.Filter.In(x => x.Id, parcels)).ToListAsync(token)).ToDictionary(x => x.Id);
    public async Task<OLandOrder> Reserve(OLandOrder order, CancellationToken token)
    {
        await EnsureIndexes(token);
        using var session = await client.StartSessionAsync(cancellationToken: token);
        try
        {
            return await session.WithTransactionAsync(async (s, ct) =>
            {
                var existing = await orders.Find(s, x => x.AvatarId == order.AvatarId && x.RequestId == order.RequestId).FirstOrDefaultAsync(ct);
                if (existing != null)
                {
                    if (existing.Fingerprint != order.Fingerprint) throw Conflict("Idempotency key belongs to different purchase details.");
                    return existing;
                }
                // Only untouched quotes can time out without a payment-provider reconciliation.
                var conflictingClaims = await claims.Find(s, Builders<OLandParcelClaim>.Filter.In(x => x.Id, order.ParcelIds)).ToListAsync(ct);
                var related = conflictingClaims.Select(c => c.OrderId).Distinct().ToArray();
                var candidateFilter = Builders<OLandOrder>.Filter.Eq(x => x.State, "quoted") & Builders<OLandOrder>.Filter.Lte(x => x.ExpiresUtc, order.CreatedUtc) &
                    (Builders<OLandOrder>.Filter.Eq(x => x.AvatarId, order.AvatarId) | Builders<OLandOrder>.Filter.In(x => x.Id, related));
                var old = await orders.Find(s, candidateFilter).ToListAsync(ct);
                foreach (var expired in old)
                {
                    await orders.UpdateOneAsync(s, x => x.Id == expired.Id && x.State == "quoted", Builders<OLandOrder>.Update.Set(x => x.State, "expired").Unset(x => x.ActiveAvatar), cancellationToken: ct);
                    await claims.DeleteManyAsync(s, Builders<OLandParcelClaim>.Filter.Where(x => x.OrderId == expired.Id && !x.Owned), cancellationToken: ct);
                }
                await orders.InsertOneAsync(s, order, cancellationToken: ct);
                await claims.InsertManyAsync(s, order.ParcelIds.Select(id => new OLandParcelClaim { Id = id, OrderId = order.Id, AvatarId = order.AvatarId }), cancellationToken: ct);
                return order;
            }, Options(), token);
        }
        catch (MongoException ex) when (ex is MongoWriteException || ex is MongoBulkWriteException<OLandParcelClaim> || ex is MongoCommandException { Code: 11000 })
        {
            var replay = await FindRequest(order.AvatarId, order.RequestId, token);
            if (replay != null && replay.Fingerprint == order.Fingerprint) return replay;
            throw Conflict("A selected parcel is reserved/owned, or you already have an unpaid OLAND order.");
        }
    }
    public async Task<bool> Transition(string id, string from, string to, CancellationToken token) =>
        (await orders.UpdateOneAsync(x => x.Id == id && x.State == from, Builders<OLandOrder>.Update.Set(x => x.State, to), cancellationToken: token)).ModifiedCount == 1;
    public async Task AttachStripe(string id, string session, string url, CancellationToken token)
    {
        var filter = Builders<OLandOrder>.Filter.Eq(x => x.Id, id) &
            (Builders<OLandOrder>.Filter.Eq(x => x.StripeSessionId, null) | Builders<OLandOrder>.Filter.Eq(x => x.StripeSessionId, session));
        var result = await orders.UpdateOneAsync(filter, Builders<OLandOrder>.Update.Set(x => x.StripeSessionId, session).Set(x => x.CheckoutUrl, url), cancellationToken: token);
        if (result.MatchedCount != 1) throw Conflict("Checkout session identity changed.");
    }
    public async Task<OLandOrder> GrantPaid(string id, string paymentIdentity, DateTime paidUtc, CancellationToken token)
    {
        using var session = await client.StartSessionAsync(cancellationToken: token);
        return await session.WithTransactionAsync(async (s, ct) =>
        {
            var order = await orders.Find(s, x => x.Id == id).FirstOrDefaultAsync(ct) ?? throw Conflict("Order not found.");
            if (order.State == "paid")
            {
                if (order.PaymentIdentity != paymentIdentity) throw Conflict("Order already has another payment.");
                return order;
            }
            if (order.State is not ("checkout_starting" or "payment_pending")) throw Conflict("Order cannot receive this payment in its current state.");
            var held = await claims.Find(s, x => x.OrderId == id && !x.Owned).ToListAsync(ct);
            if (held.Count != order.ParcelIds.Count || !held.Select(x => x.Id).OrderBy(x => x).SequenceEqual(order.ParcelIds.OrderBy(x => x)))
                throw Conflict("The order no longer holds every selected parcel; payment requires reconciliation.");
            var receipt = await receipts.Find(s, new BsonDocument("_id", paymentIdentity)).FirstOrDefaultAsync(ct);
            if (receipt != null && receipt["orderId"].AsString != id) throw Conflict("Payment has already been used for another order.");
            if (receipt == null) await receipts.InsertOneAsync(s, new BsonDocument { { "_id", paymentIdentity }, { "orderId", id }, { "paidUtc", paidUtc } }, cancellationToken: ct);
            await claims.UpdateManyAsync(s, x => x.OrderId == id, Builders<OLandParcelClaim>.Update.Set(x => x.Owned, true), cancellationToken: ct);
            await orders.UpdateOneAsync(s, x => x.Id == id, Builders<OLandOrder>.Update.Set(x => x.State, "paid").Set(x => x.PaymentIdentity, paymentIdentity).Set(x => x.PaidUtc, paidUtc).Unset(x => x.ActiveAvatar), cancellationToken: ct);
            order.State = "paid"; order.PaymentIdentity = paymentIdentity; order.PaidUtc = paidUtc; order.ActiveAvatar = null;
            return order;
        }, Options(), token);
    }
    public async Task Expire(string id, string expectedState, CancellationToken token)
    {
        using var session = await client.StartSessionAsync(cancellationToken: token);
        await session.WithTransactionAsync(async (s, ct) =>
        {
            var result = await orders.UpdateOneAsync(s, x => x.Id == id && x.State == expectedState,
                Builders<OLandOrder>.Update.Set(x => x.State, "expired").Unset(x => x.ActiveAvatar), cancellationToken: ct);
            if (result.ModifiedCount == 1) await claims.DeleteManyAsync(s, Builders<OLandParcelClaim>.Filter.Where(x => x.OrderId == id && !x.Owned), cancellationToken: ct);
            return true;
        }, Options(), token);
    }
    public async Task<bool> BeginMint(string id, CancellationToken token) =>
        (await orders.UpdateOneAsync(x => x.Id == id && x.State == "paid" && x.MintState == "not_started",
            Builders<OLandOrder>.Update.Set(x => x.MintState, "submitting"), cancellationToken: token)).ModifiedCount == 1;
    public Task CompleteMint(string id, string transactionId, string message, CancellationToken token) => orders.UpdateOneAsync(
        x => x.Id == id && x.State == "paid" && x.MintState == "submitting", Builders<OLandOrder>.Update
            .Set(x => x.MintState, string.IsNullOrWhiteSpace(transactionId) ? "needs_reconciliation" : "minted")
            .Set(x => x.MintTransactionId, transactionId).Set(x => x.MintMessage, message), cancellationToken: token);
    private static TransactionOptions Options() => new(readConcern: ReadConcern.Snapshot, writeConcern: WriteConcern.WMajority);
    private static OLandCommerceException Conflict(string message) => new("OLAND_CONFLICT", message);
}

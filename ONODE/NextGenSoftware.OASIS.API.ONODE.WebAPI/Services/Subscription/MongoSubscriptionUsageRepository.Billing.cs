using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed partial class MongoSubscriptionUsageRepository : ISubscriptionBillingRepository
    {
        private IMongoCollection<SubscriptionRecord> Subscriptions => _database.GetCollection<SubscriptionRecord>("subscription_records");
        private IMongoCollection<OrderRecord> Orders => _database.GetCollection<OrderRecord>("subscription_orders");
        private IMongoCollection<BsonDocument> BillingAudit => _database.GetCollection<BsonDocument>("subscription_billing_audit");

        private void InitializeBillingIndexes()
        {
            Subscriptions.Indexes.CreateMany(new[] {
                new CreateIndexModel<SubscriptionRecord>(Builders<SubscriptionRecord>.IndexKeys.Ascending(x => x.StripeSubscriptionId), new CreateIndexOptions<SubscriptionRecord> {
                    Name = "ux_stripe_subscription", Unique = true, PartialFilterExpression = new BsonDocument("StripeSubscriptionId", new BsonDocument("$type", "string")) }),
                new CreateIndexModel<SubscriptionRecord>(Builders<SubscriptionRecord>.IndexKeys.Ascending(x => x.StripeCustomerId), new CreateIndexOptions<SubscriptionRecord> { Name = "ux_stripe_customer", Unique = true, PartialFilterExpression = new BsonDocument("StripeCustomerId", new BsonDocument("$type", "string")) })
            });
            Orders.Indexes.CreateMany(new[] {
                new CreateIndexModel<OrderRecord>(Builders<OrderRecord>.IndexKeys.Ascending(x => x.StripeInvoiceId), new CreateIndexOptions { Name = "ux_stripe_invoice", Unique = true }),
                new CreateIndexModel<OrderRecord>(Builders<OrderRecord>.IndexKeys.Ascending(x => x.UserId).Descending(x => x.CreatedAt), new CreateIndexOptions { Name = "ix_user_orders" })
            });
            BillingAudit.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(new BsonDocument { { "UserId", 1 }, { "OccurredAtUtc", -1 } }, new CreateIndexOptions { Name = "ix_user_billing_audit" }));
        }

        public Task<SubscriptionRecord> GetSubscriptionAsync(string userId, CancellationToken cancellationToken = default) =>
            Subscriptions.Find(x => x.UserId == userId).FirstOrDefaultAsync(cancellationToken);
        public Task<SubscriptionRecord> FindSubscriptionAsync(string stripeId, bool customer, CancellationToken cancellationToken = default) =>
            Subscriptions.Find(customer ? Builders<SubscriptionRecord>.Filter.Eq(x => x.StripeCustomerId, stripeId)
                : Builders<SubscriptionRecord>.Filter.Eq(x => x.StripeSubscriptionId, stripeId)).FirstOrDefaultAsync(cancellationToken);

        public Task SaveSubscriptionAsync(SubscriptionRecord record, CancellationToken cancellationToken = default)
        {
            ValidateSubscription(record);
            if (record.PlanId != "free" || record.StripeSubscriptionId != null)
                throw new ArgumentException("Paid subscription state must arrive through the authenticated Stripe event protocol.");
            // MongoDB may rerun the whole callback after a write conflict. Keep the
            // request immutable and create each attempt's working state from it.
            string requestJson = JsonSerializer.Serialize(record);
            return TransactAsync(async (session, ct) =>
            {
                await EnsureOpeningBalanceAsync(session, ct);
                var candidate = JsonSerializer.Deserialize<SubscriptionRecord>(requestJson);
                var existing = await Subscriptions.Find(session, x => x.UserId == candidate.UserId).FirstOrDefaultAsync(ct);
                if (existing?.PlanId == "free" && existing.Status is "active" or "free") return true;
                if (existing?.StripeSubscriptionId != null && existing.PlanId != "free" && existing.Status is "active" or "trialing")
                    throw new SubscriptionUsageConflictException("An active Stripe subscription can only be changed by its canonical Stripe state; cancel it before activating a free plan.");
                candidate.UpdatedAt = _time.GetUtcNow().UtcDateTime;
                candidate.FreePlanActivatedAtUtc = candidate.UpdatedAt;
                candidate.StripeCustomerId = existing?.StripeCustomerId;
                candidate.StripeSubscriptionId = existing?.StripeSubscriptionId;
                candidate.StripeSubscriptionCreatedAtUtc = existing?.StripeSubscriptionCreatedAtUtc;
                candidate.AuthorizationVersion = existing?.AuthorizationVersion ?? 0;
                candidate.CreatedAt = existing?.CreatedAt ?? candidate.CreatedAt;
                candidate.PayAsYouGoEnabled = existing?.PayAsYouGoEnabled ?? candidate.PayAsYouGoEnabled;
                await Subscriptions.ReplaceOneAsync(session, x => x.UserId == candidate.UserId, candidate, new ReplaceOptions { IsUpsert = true }, ct);
                await BillingAudit.InsertOneAsync(session, new BsonDocument { { "_id", Guid.NewGuid().ToString() }, { "Kind", "free-plan-activation" },
                    { "UserId", candidate.UserId }, { "OccurredAtUtc", candidate.UpdatedAt }, { "PayloadJson", JsonSerializer.Serialize(candidate) } }, cancellationToken: ct);
                return true;
            }, cancellationToken);
        }

        public Task SetPayAsYouGoAsync(string userId, bool enabled, CancellationToken cancellationToken = default) =>
            TransactAsync(async (session, ct) =>
            {
                await EnsureOpeningBalanceAsync(session, ct);
                var existing = await Subscriptions.Find(session, x => x.UserId == userId).FirstOrDefaultAsync(ct)
                    ?? throw new KeyNotFoundException("A subscription is required before changing billing preferences.");
                existing.PayAsYouGoEnabled = enabled; existing.UpdatedAt = _time.GetUtcNow().UtcDateTime;
                await Subscriptions.ReplaceOneAsync(session, x => x.UserId == userId, existing, cancellationToken: ct);
                await BillingAudit.InsertOneAsync(session, new BsonDocument { { "_id", Guid.NewGuid().ToString() }, { "Kind", "billing-preference" },
                    { "UserId", userId }, { "OccurredAtUtc", existing.UpdatedAt }, { "PayloadJson", JsonSerializer.Serialize(new { PayAsYouGoEnabled = enabled }) } }, cancellationToken: ct);
                return true;
            }, cancellationToken);

        public Task<List<OrderRecord>> GetOrdersAsync(string userId, CancellationToken cancellationToken = default) =>
            Orders.Find(x => x.UserId == userId).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);

        public Task AddOrderAsync(OrderRecord order, CancellationToken cancellationToken = default) =>
            TransactAsync(async (session, ct) => { await EnsureOpeningBalanceAsync(session, ct); await InsertInvoiceAsync(session, order, ct); return true; }, cancellationToken);

        private async Task InsertInvoiceAsync(IClientSessionHandle session, OrderRecord invoice, CancellationToken ct)
        {
            if (invoice == null || !Guid.TryParseExact(invoice.UserId, "D", out _) || string.IsNullOrWhiteSpace(invoice.StripeInvoiceId) ||
                !invoice.StripeInvoiceId.StartsWith("in_", StringComparison.Ordinal) || invoice.Amount < 0 || string.IsNullOrWhiteSpace(invoice.Currency))
                throw new ArgumentException("A measured Stripe invoice, avatar id, nonnegative amount and currency are required.");
            var existing = await Orders.Find(session, x => x.StripeInvoiceId == invoice.StripeInvoiceId).FirstOrDefaultAsync(ct);
            if (existing != null)
            {
                if (existing.UserId != invoice.UserId || existing.Amount != invoice.Amount || existing.Currency != invoice.Currency || existing.Status != invoice.Status)
                    throw new SubscriptionUsageConflictException("Invoice id was already recorded with a different owner, amount, currency or payment status.");
                return;
            }
            var stored = JsonSerializer.Deserialize<OrderRecord>(JsonSerializer.Serialize(invoice));
            stored.Id = invoice.StripeInvoiceId;
            await Orders.InsertOneAsync(session, stored, cancellationToken: ct);
            await BillingAudit.InsertOneAsync(session, new BsonDocument { { "_id", "invoice:" + invoice.StripeInvoiceId }, { "Kind", "invoice-paid" },
                { "UserId", invoice.UserId }, { "OccurredAtUtc", _time.GetUtcNow().UtcDateTime }, { "PayloadJson", JsonSerializer.Serialize(invoice) } }, cancellationToken: ct);
        }

        public Task<bool> ApplyStripeEventAsync(string eventId, string sourceFingerprint, string userId, DateTime eventCreatedAtUtc,
            Func<CancellationToken, Task<SubscriptionRecord>> fetchCanonicalSubscription, OrderRecord invoice, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(eventId) || !eventId.StartsWith("evt_", StringComparison.Ordinal) || sourceFingerprint?.Length != 64 || !Guid.TryParseExact(userId, "D", out _))
                throw new ArgumentException("Stripe event identity, signed payload hash and canonical avatar identity are required.");
            return TransactAsync(async (session, ct) =>
            {
                await EnsureOpeningBalanceAsync(session, ct);
                var seen = await BillingAudit.Find(session, new BsonDocument("_id", "stripe:" + eventId)).FirstOrDefaultAsync(ct);
                if (seen != null)
                {
                    if (seen["SourceFingerprint"].AsString != sourceFingerprint || seen["UserId"].AsString != userId)
                        throw new SubscriptionUsageConflictException("Stripe event id was reused with a different signed payload or avatar.");
                    return false;
                }
                // Reading before the canonical Stripe lookup ensures concurrent subscription writes
                // produce a MongoDB write conflict. The transaction retry then reads Stripe again.
                var current = await Subscriptions.Find(session, x => x.UserId == userId).FirstOrDefaultAsync(ct);
                var canonical = await fetchCanonicalSubscription(ct);
                ValidateSubscription(canonical);
                if (canonical.UserId != userId || string.IsNullOrWhiteSpace(canonical.StripeCustomerId) || string.IsNullOrWhiteSpace(canonical.StripeSubscriptionId) || !canonical.StripeSubscriptionCreatedAtUtc.HasValue)
                    throw new SubscriptionUsageConflictException("Canonical Stripe subscription identity does not match the event avatar.");
                if (current?.StripeSubscriptionId != null && current.StripeSubscriptionId != canonical.StripeSubscriptionId &&
                    current.StripeSubscriptionCreatedAtUtc == canonical.StripeSubscriptionCreatedAtUtc)
                    throw new SubscriptionUsageConflictException("Two Stripe subscriptions have the same creation time; explicitly reconcile duplicate subscriptions before selecting the active plan.");
                var otherOwner = await Subscriptions.Find(session, x => x.UserId != userId && (x.StripeSubscriptionId == canonical.StripeSubscriptionId || x.StripeCustomerId == canonical.StripeCustomerId)).AnyAsync(ct);
                if (otherOwner) throw new SubscriptionUsageConflictException("Stripe customer or subscription is already owned by another avatar.");
                bool superseded = current?.StripeSubscriptionId != null && current.StripeSubscriptionId != canonical.StripeSubscriptionId &&
                    (current.StripeSubscriptionCreatedAtUtc ?? DateTime.MinValue) >= canonical.StripeSubscriptionCreatedAtUtc.Value;
                if (current?.FreePlanActivatedAtUtc != null && canonical.StripeSubscriptionCreatedAtUtc <= current.StripeSubscriptionCreatedAtUtc) superseded = true;
                if (!superseded)
                {
                    canonical.AuthorizationVersion = current?.AuthorizationVersion ?? 0;
                    canonical.PayAsYouGoEnabled = current?.PayAsYouGoEnabled ?? false;
                    canonical.CreatedAt = current?.CreatedAt ?? canonical.CreatedAt;
                    canonical.UpdatedAt = _time.GetUtcNow().UtcDateTime;
                    canonical.LastStripeEventId = eventId;
                    canonical.LastStripeEventCreatedAtUtc = eventCreatedAtUtc;
                    await Subscriptions.ReplaceOneAsync(session, x => x.UserId == userId, canonical, new ReplaceOptions { IsUpsert = true }, ct);
                }
                if (invoice != null)
                {
                    if (invoice.UserId != userId) throw new SubscriptionUsageConflictException("Invoice owner does not match the signed event avatar.");
                    await InsertInvoiceAsync(session, invoice, ct);
                }
                await BillingAudit.InsertOneAsync(session, new BsonDocument { { "_id", "stripe:" + eventId }, { "Kind", "stripe-event" },
                    { "UserId", userId }, { "SourceFingerprint", sourceFingerprint }, { "EventCreatedAtUtc", eventCreatedAtUtc },
                    { "OccurredAtUtc", _time.GetUtcNow().UtcDateTime }, { "Superseded", superseded }, { "PayloadJson", JsonSerializer.Serialize(canonical) }
                }, cancellationToken: ct);
                return true;
            }, cancellationToken);
        }

        private static void ValidateSubscription(SubscriptionRecord record)
        {
            if (record == null || !Guid.TryParseExact(record.UserId, "D", out _) || record.PlanId is not ("free" or "bronze" or "silver" or "gold" or "enterprise") ||
                string.IsNullOrWhiteSpace(record.Status) || (record.StripeSubscriptionId != null && (!record.StripeSubscriptionCreatedAtUtc.HasValue || string.IsNullOrWhiteSpace(record.StripeCustomerId)))) throw new ArgumentException("A valid subscription avatar, explicit plan and status are required.");
        }
    }
}

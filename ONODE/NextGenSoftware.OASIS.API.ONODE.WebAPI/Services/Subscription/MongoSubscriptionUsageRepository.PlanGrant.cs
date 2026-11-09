using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    /// <summary>
    /// Administrator-granted plans (for example putting an internal or partner avatar on Enterprise without a Stripe
    /// subscription). Stripe remains canonical: an avatar with an active Stripe subscription cannot be overridden here,
    /// and every grant is written to the billing audit with the acting administrator and a reason.
    /// </summary>
    public sealed partial class MongoSubscriptionUsageRepository
    {
        public const int MaxAdminGrantYears = 10;

        /// <summary>Rejects a malformed grant before any database work starts.</summary>
        public static void ValidateGrantRequest(string userId, string planId, string actor, string reason)
        {
            if (!Guid.TryParseExact(userId, "D", out _)) throw new ArgumentException("A valid avatar UUID is required.");
            if (planId is not ("free" or "bronze" or "silver" or "gold" or "enterprise")) throw new ArgumentException("Plan must be free, bronze, silver, gold or enterprise.");
            if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("The granting administrator is required.");
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5) throw new ArgumentException("A reason of at least 5 characters is required.");
            if (reason.Trim().Length > 500) throw new ArgumentException("The reason must be 500 characters or fewer.");
        }

        public Task<SubscriptionRecord> GrantPlanAsync(string userId, string planId, string actor, string reason, DateTime? expiresAtUtc, CancellationToken cancellationToken = default)
        {
            ValidateGrantRequest(userId, planId, actor, reason);
            reason = reason.Trim();

            return TransactAsync(async (session, ct) =>
            {
                await EnsureOpeningBalanceAsync(session, ct);
                var now = _time.GetUtcNow().UtcDateTime;
                var end = expiresAtUtc?.ToUniversalTime() ?? now.AddYears(MaxAdminGrantYears);
                if (end <= now) throw new ArgumentException("The expiry must be in the future.");
                if (end > now.AddYears(MaxAdminGrantYears)) throw new ArgumentException($"A grant may last at most {MaxAdminGrantYears} years.");

                var existing = await Subscriptions.Find(session, x => x.UserId == userId).FirstOrDefaultAsync(ct);
                if (existing?.StripeSubscriptionId != null && existing.Status is "active" or "trialing")
                    throw new SubscriptionUsageConflictException("This avatar has an active Stripe subscription, which is canonical; cancel it before granting a plan.");

                var record = new SubscriptionRecord
                {
                    UserId = userId, PlanId = planId, Status = "active",
                    CurrentPeriodStart = now, CurrentPeriodEnd = end,
                    AuthorizationVersion = (existing?.AuthorizationVersion ?? 0) + 1,
                    CreatedAt = existing?.CreatedAt ?? now, UpdatedAt = now,
                    FreePlanActivatedAtUtc = planId == "free" ? now : existing?.FreePlanActivatedAtUtc,
                    PayAsYouGoEnabled = false,
                    AdminGrantedBy = planId == "free" ? null : actor,
                    AdminGrantReason = planId == "free" ? null : reason
                };
                await Subscriptions.ReplaceOneAsync(session, x => x.UserId == userId, record, new ReplaceOptions { IsUpsert = true }, ct);
                await BillingAudit.InsertOneAsync(session, new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() }, { "Kind", "admin-plan-grant" }, { "UserId", userId },
                    { "OccurredAtUtc", now },
                    { "PayloadJson", JsonSerializer.Serialize(new { PlanId = planId, Actor = actor, Reason = reason, PreviousPlanId = existing?.PlanId, ExpiresAtUtc = end }) }
                }, cancellationToken: ct);
                return record;
            }, cancellationToken);
        }
    }
}

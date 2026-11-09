using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed partial class MongoSubscriptionUsageRepository : IAppAccountRepository
    {
        private IMongoCollection<AppAccountRecord> AppAccounts => _database.GetCollection<AppAccountRecord>("subscription_app_accounts");

        public Task<(AppAccountRecord Account, string ApiKey)> CreateAppAccountAsync(string appId, string name, string planId, string actor, string reason, CancellationToken cancellationToken = default)
        {
            AppAccountRules.ValidateAppId(appId);
            AppAccountRules.ValidatePlan(planId);
            AppAccountRules.ValidateActorAndReason(actor, reason);
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100) throw new ArgumentException("A name of up to 100 characters is required.");
            return TransactAsync(async (session, ct) =>
            {
                var now = _time.GetUtcNow().UtcDateTime;
                if (await AppAccounts.Find(session, x => x.AppId == appId).AnyAsync(ct))
                    throw new SubscriptionUsageConflictException("An app account with this id already exists.");
                string key = AppAccountRules.GenerateKey();
                var account = new AppAccountRecord
                {
                    AppId = appId, Name = name.Trim(), PlanId = planId, Status = "active",
                    KeyHashSha256 = AppAccountRules.Hash(key), KeyPrefix = key.Substring(0, 8),
                    CreatedAtUtc = now, CreatedBy = actor, UpdatedAtUtc = now, UpdatedBy = actor, Reason = reason.Trim()
                };
                await AppAccounts.InsertOneAsync(session, account, cancellationToken: ct);
                await AppAccountAuditAsync(session, "app-account-created", account, actor, reason.Trim(), now, ct);
                return (account.Redacted(), key);
            }, cancellationToken);
        }

        public Task<(AppAccountRecord Account, string ApiKey)> RotateAppAccountKeyAsync(string appId, string actor, string reason, CancellationToken cancellationToken = default)
        {
            AppAccountRules.ValidateAppId(appId);
            AppAccountRules.ValidateActorAndReason(actor, reason);
            return TransactAsync(async (session, ct) =>
            {
                var now = _time.GetUtcNow().UtcDateTime;
                var account = await AppAccounts.Find(session, x => x.AppId == appId).FirstOrDefaultAsync(ct)
                    ?? throw new KeyNotFoundException("The app account was not found.");
                string key = AppAccountRules.GenerateKey();
                account.KeyHashSha256 = AppAccountRules.Hash(key); account.KeyPrefix = key.Substring(0, 8);
                account.UpdatedAtUtc = now; account.UpdatedBy = actor; account.Reason = reason.Trim();
                await AppAccounts.ReplaceOneAsync(session, x => x.AppId == appId, account, cancellationToken: ct);
                await AppAccountAuditAsync(session, "app-account-key-rotated", account, actor, reason.Trim(), now, ct);
                return (account.Redacted(), key);
            }, cancellationToken);
        }

        public Task<AppAccountRecord> UpdateAppAccountAsync(string appId, string planId, string status, string actor, string reason, CancellationToken cancellationToken = default)
        {
            AppAccountRules.ValidateAppId(appId);
            AppAccountRules.ValidateActorAndReason(actor, reason);
            if (planId == null && status == null) throw new ArgumentException("Provide a plan, a status, or both.");
            if (planId != null) AppAccountRules.ValidatePlan(planId);
            if (status != null && status is not ("active" or "disabled")) throw new ArgumentException("Status must be active or disabled.");
            return TransactAsync(async (session, ct) =>
            {
                var now = _time.GetUtcNow().UtcDateTime;
                var account = await AppAccounts.Find(session, x => x.AppId == appId).FirstOrDefaultAsync(ct)
                    ?? throw new KeyNotFoundException("The app account was not found.");
                string previous = account.PlanId + "/" + account.Status;
                if (planId != null) account.PlanId = planId;
                if (status != null) account.Status = status;
                account.UpdatedAtUtc = now; account.UpdatedBy = actor; account.Reason = reason.Trim();
                await AppAccounts.ReplaceOneAsync(session, x => x.AppId == appId, account, cancellationToken: ct);
                await AppAccountAuditAsync(session, "app-account-updated", account, actor, reason.Trim(), now, ct, previous);
                return account.Redacted();
            }, cancellationToken);
        }

        public async Task<AppAccountRecord> VerifyAppCredentialAsync(string appId, string apiKey, CancellationToken cancellationToken = default)
        {
            if (!AppAccountRules.IsValidAppId(appId) || string.IsNullOrWhiteSpace(apiKey)) return null;
            var account = await AppAccounts.Find(x => x.AppId == appId).FirstOrDefaultAsync(cancellationToken);
            if (account == null || account.Status != "active" || !AppAccountRules.KeyMatches(apiKey, account.KeyHashSha256)) return null;
            return account.Redacted();
        }

        public async Task<IReadOnlyList<AppAccountRecord>> ListAppAccountsAsync(CancellationToken cancellationToken = default)
        {
            var all = await AppAccounts.Find(_ => true).SortBy(x => x.AppId).Limit(500).ToListAsync(cancellationToken);
            return all.ConvertAll(a => a.Redacted());
        }

        private Task AppAccountAuditAsync(IClientSessionHandle session, string kind, AppAccountRecord account, string actor, string reason, DateTime now, CancellationToken ct, string previous = null) =>
            BillingAudit.InsertOneAsync(session, new BsonDocument
            {
                { "_id", Guid.NewGuid().ToString() }, { "Kind", kind }, { "UserId", "app:" + account.AppId }, { "OccurredAtUtc", now },
                { "PayloadJson", JsonSerializer.Serialize(new { account.AppId, account.PlanId, account.Status, Actor = actor, Reason = reason, Previous = previous }) }
            }, cancellationToken: ct);
    }
}

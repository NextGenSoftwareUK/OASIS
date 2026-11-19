using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed partial class MongoSubscriptionUsageRepository
{
    private IMongoCollection<PaidOverageStatement> OverageStatements => _database.GetCollection<PaidOverageStatement>("subscription_overage_statements");
    private IMongoCollection<PaidOverageAdjustment> OverageAdjustments => _database.GetCollection<PaidOverageAdjustment>("subscription_overage_adjustments");
    private string OverageScope => _database.DatabaseNamespace.DatabaseName;
    private static string StatementId(string user,string month,string key) => "ovg_"+Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(user+":"+month+":"+key)));
    private static DateTime MonthStart(string month) => DateTime.SpecifyKind(DateTime.ParseExact(month,"yyyy-MM",CultureInfo.InvariantCulture),DateTimeKind.Utc);

    public Task<PaidOverageStatement> PrepareAsync(string userId,string month,string requestKey,CancellationToken token,bool creditsOnly=false)
    {
        if (userId?.StartsWith("app:", StringComparison.Ordinal) == true)
            return PrepareAppStatementAsync(userId.Substring(4), month, requestKey, creditsOnly, token);
        return PreparePersonalStatementAsync(userId, month, requestKey, creditsOnly, token);
    }

    public async Task<IReadOnlyList<PaidOverageAccount>> GetClosedAccountsAsync(DateTime now,CancellationToken token)
    {
        var cutoff=new DateTime(now.Year,now.Month,1,0,0,0,DateTimeKind.Utc);
        var month=now.ToString("yyyy-MM",CultureInfo.InvariantCulture);
        var rows=await _events.Aggregate().Match(x=>x.Status=="settled" && x.PaidOveragePricingVersion!=null && x.BillingStatementId==null &&
            x.AuthorizedAtUtc<cutoff && (x.SettledRequestOverageUsd>0 || x.SettledAiOverageUsd>0 || x.PersonalAiCorrectionUsd>0))
            .Group<BsonDocument>(new BsonDocument {{"_id",new BsonDocument {{"UserId","$UserId"},
                {"Month",new BsonDocument("$dateToString",new BsonDocument {{"format","%Y-%m"},{"date","$AuthorizedAtUtc"},{"timezone","UTC"}})}}}})
            .Sort(new BsonDocument { { "_id.Month", 1 }, { "_id.UserId", 1 } }).Limit(25).ToListAsync(token);
        var accounts=rows.Select(x=>new PaidOverageAccount(x["_id"]["UserId"].AsString,x["_id"]["Month"].AsString)).ToList();
        var appRows=await _events.Aggregate().Match(x=>x.Status=="settled" && x.PooledAppId!=null && x.AppBillingStatementId==null &&
            x.AuthorizedAtUtc<cutoff && x.SettledAppRequestOverageUsd>0)
            .Group<BsonDocument>(new BsonDocument {{"_id",new BsonDocument {{"AppId","$PooledAppId"},
                {"Month",new BsonDocument("$dateToString",new BsonDocument {{"format","%Y-%m"},{"date","$AuthorizedAtUtc"},{"timezone","UTC"}})}}}})
            .Sort(new BsonDocument { { "_id.Month", 1 }, { "_id.AppId", 1 } }).Limit(25).ToListAsync(token);
        accounts.AddRange(appRows.Select(x=>new PaidOverageAccount(PoolSubject(x["_id"]["AppId"].AsString),x["_id"]["Month"].AsString)));
        var creditFilter=Builders<PaidOverageAdjustment>.Filter.Eq(x=>x.BillingStatementId,null) & Builders<PaidOverageAdjustment>.Filter.Or(
            Builders<PaidOverageAdjustment>.Filter.Lt(x=>x.Month,month),Builders<PaidOverageAdjustment>.Filter.Lt(x=>x.AmountUsd,0));
        var credits=await OverageAdjustments.Find(creditFilter).SortBy(x=>x.Month).ThenBy(x=>x.Id).Limit(100).ToListAsync(token);
        accounts.AddRange(credits.Select(x=>new PaidOverageAccount(x.UserId,x.Month,x.Month==month)));
        return accounts.Distinct().OrderBy(x=>x.Month,StringComparer.Ordinal).ThenBy(x=>x.UserId,StringComparer.Ordinal).Take(25).ToArray();
    }
    public async Task<IReadOnlyList<PaidOverageStatement>> GetReadyAsync(DateTime now,CancellationToken token) =>
        await OverageStatements.Find(x=>x.Scope==OverageScope && x.State!="paid" && x.State!="void" && x.State!="uncollectible" && x.NextAttemptAtUtc<=now &&
            (x.LeaseUntilUtc==null || x.LeaseUntilUtc<=now)).SortBy(x=>x.NextAttemptAtUtc).Limit(25).ToListAsync(token);
    public async Task<IReadOnlyList<PaidOverageStatement>> GetStatementsAsync(string userId,int limit,CancellationToken token) =>
        await OverageStatements.Find(Builders<PaidOverageStatement>.Filter.Eq(x=>x.Scope,OverageScope) &
            (userId==null ? Builders<PaidOverageStatement>.Filter.Empty : Builders<PaidOverageStatement>.Filter.Eq(x=>x.UserId,userId)))
            .SortByDescending(x=>x.CreatedAtUtc).Limit(Math.Clamp(limit,1,500)).ToListAsync(token);
    public async Task<PaidOverageStatement> GetStatementAsync(string id,CancellationToken token)
    {
        var statement=await OverageStatements.Find(x=>x.Id==id).FirstOrDefaultAsync(token);
        if(statement!=null)RequireStoredStatementBinding(statement);
        return statement;
    }

    private void RequireStoredStatementBinding(PaidOverageStatement statement)
    {
        if(statement.Scope!=OverageScope)
            throw new SubscriptionUsageConflictException("OVERAGE_STATEMENT_SCOPE_MISMATCH: Statement does not belong to this storage scope.");
        PaidOverageStatementBinding.RequirePayer(statement);
    }
    public async Task<PaidOverageStatement> GetStatementForSubscriberAsync(string userId,string id,CancellationToken token)
    {
        if(!Guid.TryParseExact(userId,"D",out var avatar) || avatar==Guid.Empty)
            throw new UnauthorizedAccessException("An authenticated subscriber is required.");
        if(id==null || id.Length!=68 || !id.StartsWith("ovg_",StringComparison.Ordinal) ||
            !id.Substring(4).All(c=>c is >= '0' and <= '9' or >= 'a' and <= 'f'))
            throw new ArgumentException("A valid personal overage statement identity is required.");
        return await OverageStatements.Find(x=>x.Id==id && x.UserId==userId && x.Scope==OverageScope &&
            x.AppId==null && x.PayerAvatarId==null).FirstOrDefaultAsync(token)
            ?? throw new KeyNotFoundException("The subscriber billing statement was not found.");
    }

    public Task<PaidOverageStatement> ClaimAsync(string id,string owner,DateTime now,CancellationToken token) =>
        TransactAsync(async(session,ct)=>
        {
            var statement=await OverageStatements.Find(session,x=>x.Id==id).FirstOrDefaultAsync(ct);
            if(statement==null)return null;
            // Validate before any lease write or gateway call, in the same snapshot as the claim.
            RequireStoredStatementBinding(statement);
            return await OverageStatements.FindOneAndUpdateAsync(session,x=>x.Id==id && x.Scope==OverageScope && x.State!="paid" && x.State!="void" && x.State!="uncollectible" &&
            (x.LeaseUntilUtc==null || x.LeaseUntilUtc<=now),Builders<PaidOverageStatement>.Update.Set(x=>x.LeaseOwner,owner)
            .Set(x=>x.LeaseUntilUtc,now.AddMinutes(10)).Set(x=>x.UpdatedAtUtc,now).Inc(x=>x.Attempts,1),
            new FindOneAndUpdateOptions<PaidOverageStatement>{ReturnDocument=ReturnDocument.After},ct);
        },token);
    public async Task AttachInvoiceAsync(string id,string owner,string invoiceId,CancellationToken token)
    {
        var result=await OverageStatements.UpdateOneAsync(x=>x.Id==id && x.Scope==OverageScope && x.LeaseOwner==owner &&
            (x.StripeInvoiceId==null || x.StripeInvoiceId==invoiceId),Builders<PaidOverageStatement>.Update.Set(x=>x.StripeInvoiceId,invoiceId),cancellationToken:token);
        if(result.MatchedCount!=1)throw new SubscriptionUsageConflictException("The billing lease was lost or another invoice owns this statement.");
    }
    public Task RecordInvoiceAsync(string id,string owner,PaidOverageInvoiceResult invoice,string eventId,string fingerprint,CancellationToken token) =>
        TransactAsync(async(session,ct)=>
        {
            var statement=await OverageStatements.Find(session,x=>x.Id==id).FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Overage statement not found.");
            RequireStoredStatementBinding(statement);
            bool appScope = statement.AppId != null;
            if(statement.State=="paid" && invoice.Status!="paid")return true;
            if(owner!=null && statement.LeaseOwner!=owner && !(statement.State=="paid" && statement.StripeInvoiceId==invoice.InvoiceId && invoice.Status=="paid"))throw new SubscriptionUsageConflictException("The billing collection lease was lost.");
            if(invoice.Subtotal!=statement.AmountMinorUsd || (statement.StripeInvoiceId!=null && statement.StripeInvoiceId!=invoice.InvoiceId) ||
                invoice.Status is not ("paid" or "open" or "uncollectible" or "void"))throw new SubscriptionUsageConflictException("Canonical USD invoice differs from its immutable statement.");
            if(eventId!=null)
            {
                var prior=await BillingAudit.Find(session,new BsonDocument("_id","overage-stripe:"+eventId)).FirstOrDefaultAsync(ct);
                if(prior!=null)
                {
                    if(prior["SourceFingerprint"].AsString!=fingerprint)throw new SubscriptionUsageConflictException("Stripe event id was reused with another payload.");
                    return true;
                }
            }
            statement.StripeInvoiceId=invoice.InvoiceId;statement.State=invoice.Status;statement.HostedInvoiceUrl=invoice.HostedInvoiceUrl;
            statement.PaymentFailed=invoice.Status is "open" or "uncollectible" && invoice.Attempted;
            statement.LeaseOwner=null;statement.LeaseUntilUtc=null;statement.LastError=null;
            statement.UpdatedAtUtc=_time.GetUtcNow().UtcDateTime;statement.NextAttemptAtUtc=statement.UpdatedAtUtc.AddMinutes(5);
            await OverageStatements.ReplaceOneAsync(session,x=>x.Id==id,statement,cancellationToken:ct);
            var blocked=statement.PaymentFailed || await OverageStatements.Find(session,x=>x.UserId==statement.UserId && x.Id!=id && x.PaymentFailed && x.State!="paid").AnyAsync(ct);
            if (appScope)
            {
                var app = await AppAccounts.Find(session, x => x.AppId == statement.AppId).FirstOrDefaultAsync(ct)
                    ?? throw new SubscriptionUsageConflictException("APP_STATEMENT_BINDING_REQUIRED: The app billing account is unavailable.");
                await AppAccounts.UpdateOneAsync(session, x => x.AppId == app.AppId,
                    Builders<AppAccountRecord>.Update.Set(x => x.AppOveragePaymentBlocked, blocked)
                        .Set(x => x.AuthorizationVersion, checked(app.AuthorizationVersion + 1)), cancellationToken: ct);
            }
            else
                await Subscriptions.UpdateOneAsync(session,x=>x.UserId==statement.UserId,Builders<SubscriptionRecord>.Update.Set(x=>x.OveragePaymentBlocked,blocked).Inc(x=>x.AuthorizationVersion,1),cancellationToken:ct);
            if(invoice.Status=="paid" && invoice.InvoiceId!=null)
                await InsertInvoiceAsync(session,new OrderRecord {UserId=appScope ? statement.PayerAvatarId : statement.UserId,StripeInvoiceId=invoice.InvoiceId,Amount=invoice.AmountPaid/100m,
                    BilledAmount=invoice.Subtotal/100m,Currency="USD",Description="OASIS USD paid overages "+statement.Month+(appScope ? " app "+statement.AppId : ""),Status="paid",CreatedAt=_time.GetUtcNow().UtcDateTime},ct);
            if(eventId!=null)
                await BillingAudit.InsertOneAsync(session,new BsonDocument{{"_id","overage-stripe:"+eventId},{"Kind","overage-stripe-event"},
                    {"UserId",statement.UserId},{"SourceFingerprint",fingerprint},{"OccurredAtUtc",statement.UpdatedAtUtc},{"PayloadJson",JsonSerializer.Serialize(invoice)}},cancellationToken:ct);
            return true;
        },token);
    public async Task RetryAsync(string id,string owner,string error,CancellationToken token)
    {
        var now=_time.GetUtcNow().UtcDateTime;
        await OverageStatements.UpdateOneAsync(x=>x.Id==id && x.Scope==OverageScope && x.LeaseOwner==owner,Builders<PaidOverageStatement>.Update
            .Set(x=>x.LeaseOwner,null).Set(x=>x.LeaseUntilUtc,null).Set(x=>x.State,"retry").Set(x=>x.LastError,error)
            .Set(x=>x.NextAttemptAtUtc,now.AddMinutes(5)).Set(x=>x.UpdatedAtUtc,now),cancellationToken:token);
    }
}

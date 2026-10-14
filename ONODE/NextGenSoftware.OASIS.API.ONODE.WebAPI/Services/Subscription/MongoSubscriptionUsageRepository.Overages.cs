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

    public Task<PaidOverageStatement> PrepareAsync(string userId,string month,string requestKey,CancellationToken token)
    {
        if(!Guid.TryParseExact(userId,"D",out _) || string.IsNullOrWhiteSpace(requestKey) || requestKey.Length>200)
            throw new ArgumentException("A canonical subscriber, month and billing request identity are required.");
        var start=MonthStart(month);var end=start.AddMonths(1);
        if(start>_time.GetUtcNow().UtcDateTime)throw new ArgumentException("Future usage cannot be invoiced.");
        return TransactAsync(async(session,ct)=>
        {
            await EnsureOpeningBalanceAsync(session,ct);
            var id=StatementId(userId,month,requestKey);
            var existing=await OverageStatements.Find(session,x=>x.Id==id).FirstOrDefaultAsync(ct);
            if(existing!=null)return existing;
            var subscriber=await Subscriptions.Find(session,x=>x.UserId==userId).FirstOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException("Subscriber billing account was not found.");
            if(string.IsNullOrWhiteSpace(subscriber.StripeCustomerId) || string.IsNullOrWhiteSpace(subscriber.StripeSubscriptionId))
                throw new SubscriptionUsageConflictException("A verified Stripe billing account is required.");
            // This fence serializes consent/plan changes, reservations and concurrent statement preparations.
            subscriber.AuthorizationVersion=checked(subscriber.AuthorizationVersion+1);
            await Subscriptions.ReplaceOneAsync(session,x=>x.UserId==userId,subscriber,cancellationToken:ct);
            var operations=await _events.Find(session,x=>x.UserId==userId && x.AuthorizedAtUtc>=start && x.AuthorizedAtUtc<end &&
                x.Status=="settled" && x.PaidOveragePricingVersion!=null && x.BillingStatementId==null &&
                (x.SettledRequestOverageUsd>0 || x.SettledAiOverageUsd>0))
                .SortBy(x=>x.AuthorizedAtUtc).ThenBy(x=>x.OperationId).Limit(500).ToListAsync(ct);
            var adjustments=await OverageAdjustments.Find(session,x=>x.UserId==userId && x.Month==month && x.BillingStatementId==null).Limit(500).ToListAsync(ct);
            if(operations.Count==0 && adjustments.Count==0)return null;
            var earlier=await OverageStatements.Find(session,x=>x.UserId==userId && x.Month==month).ToListAsync(ct);
            decimal requests=operations.Sum(x=>x.SettledRequestOverageUsd), ai=operations.Sum(x=>x.SettledAiOverageUsd)+adjustments.Sum(x=>x.AmountUsd);
            // A pay-now statement rounds the cumulative month, avoiding repeated sub-cent rounding charges.
            long cents=checked(PaidOveragePricing.MinorUnits(earlier.Sum(x=>x.RequestChargesUsd+x.AiChargesUsd)+requests+ai)-earlier.Sum(x=>x.AmountMinorUsd));
            var now=_time.GetUtcNow().UtcDateTime;
            var statement=new PaidOverageStatement {Id=id,UserId=userId,Month=month,Scope=OverageScope,
                StripeCustomerId=subscriber.StripeCustomerId,StripeSubscriptionId=subscriber.StripeSubscriptionId,
                RequestChargesUsd=requests,AiChargesUsd=ai,AmountMinorUsd=cents,
                OperationIds=operations.Select(x=>x.OperationId).ToArray(),AdjustmentIds=adjustments.Select(x=>x.Id).ToArray(),PricingVersions=operations.Select(x=>x.PaidOveragePricingVersion).Distinct().ToArray(),
                CreatedAtUtc=now,UpdatedAtUtc=now,NextAttemptAtUtc=now};
            await OverageStatements.InsertOneAsync(session,statement,cancellationToken:ct);
            var ids=statement.OperationIds;
            await _events.UpdateManyAsync(session,Builders<UsageEventDocument>.Filter.In(x=>x.OperationId,ids),
                Builders<UsageEventDocument>.Update.Set(x=>x.BillingStatementId,id),cancellationToken:ct);
            if(adjustments.Count>0)await OverageAdjustments.UpdateManyAsync(session,Builders<PaidOverageAdjustment>.Filter.In(x=>x.Id,statement.AdjustmentIds),
                Builders<PaidOverageAdjustment>.Update.Set(x=>x.BillingStatementId,id),cancellationToken:ct);
            await BillingAudit.InsertOneAsync(session,new BsonDocument {{"_id","overage-statement:"+id},{"Kind","overage-statement"},
                {"UserId",userId},{"OccurredAtUtc",now},{"PayloadJson",JsonSerializer.Serialize(statement)}},cancellationToken:ct);
            return statement;
        },token);
    }

    private async Task ApplyOverageCorrectionAsync(IClientSessionHandle session,SubscriptionUsageEvent operation,SubscriptionUsageAuditEntry correction,CancellationToken token)
    {
        var start=MonthStart(correction.Month);var end=start.AddMonths(1);
        var filter=Builders<UsageEventDocument>.Filter.Where(x=>x.UserId==operation.UserId && x.Status=="settled" && x.AuthorizedAtUtc>=start && x.AuthorizedAtUtc<end);
        if(!await _events.Find(session,filter & Builders<UsageEventDocument>.Filter.Ne(x=>x.PaidOveragePricingVersion,null)).AnyAsync(token))return;
        var history=await _audit.Find(session,x=>x.UserId==operation.UserId && x.Month==correction.Month && x.Kind=="correction").ToListAsync(token);
        var offsets=history.GroupBy(x=>x.OperationId).ToDictionary(x=>x.Key,x=>x.Sum(e=>e.SettledCostDeltaUsd));
        var feeRows=await _events.Aggregate(session).Match(filter).Group<BsonDocument>(new BsonDocument{{"_id",BsonNull.Value},{"Fees",new BsonDocument("$sum","$SettledRequestOverageUsd")}}).ToListAsync(token);
        decimal fees=feeRows.FirstOrDefault()?.GetValue("Fees",0).ToDecimal() ?? 0;
        decimal beforeCost=0,afterCost=0,beforeCharges=fees,afterCharges=fees;
        var revisedIds=offsets.Keys.Append(operation.OperationId).ToArray();
        var costFilter=filter & Builders<UsageEventDocument>.Filter.Or(Builders<UsageEventDocument>.Filter.Gt(x=>x.SettledCostUsd,0),Builders<UsageEventDocument>.Filter.In(x=>x.OperationId,revisedIds));
        using var cursor=await _events.Find(session,costFilter).SortBy(x=>x.SettledSequence).ThenBy(x=>x.SettledAtUtc).ThenBy(x=>x.OperationId).ToCursorAsync(token);
        while(await cursor.MoveNextAsync(token))foreach(var current in cursor.Current)
        {
            decimal cost=checked(current.SettledCostUsd+offsets.GetValueOrDefault(current.OperationId));
            decimal revised=checked(cost+(current.OperationId==operation.OperationId ? correction.SettledCostDeltaUsd : 0));
            if(current.PaidOveragePricingVersion!=null)
            {
                decimal bound=Math.Max(0,current.ReservedOverageUsd-current.SettledRequestOverageUsd);
                beforeCharges+=Math.Min(bound,PaidOveragePricing.AiSettlement(current.AuthorizationPolicy,new(){SettledCostUsd=beforeCost},cost));
                afterCharges+=Math.Min(bound,PaidOveragePricing.AiSettlement(current.AuthorizationPolicy,new(){SettledCostUsd=afterCost},revised));
            }
            beforeCost+=cost;afterCost+=revised;
        }
        decimal writeoffs=history.Sum(x=>x.OverageWriteOffDeltaUsd);
        var aggregate=await LoadAggregateAsync(session,operation.UserId,operation.AuthorizedAtUtc,token);
        if(Math.Max(0,beforeCharges-writeoffs)!=aggregate.SettledOverageUsd)
            throw new SubscriptionUsageConflictException("Paid overage history does not balance; reconcile it before financial correction.");
        decimal delta=Math.Max(0,afterCharges-writeoffs)-aggregate.SettledOverageUsd;
        if(delta>0)
        {
            var subscriber=await Subscriptions.Find(session,x=>x.UserId==operation.UserId).FirstOrDefaultAsync(token);
            decimal available=Math.Max(0,(subscriber?.MonthlyOverageLimitUsd ?? 0)-aggregate.SettledOverageUsd-aggregate.ReservedOverageUsd);
            correction.OverageWriteOffDeltaUsd=Math.Max(0,delta-available);
            delta=Math.Min(delta,available);
        }
        correction.SettledOverageDeltaUsd=delta;
        if(delta!=0)await OverageAdjustments.InsertOneAsync(session,new(){Id=correction.Id,UserId=operation.UserId,Month=correction.Month,
            AmountUsd=delta,Reason=correction.Reason,EvidenceReference=correction.ExternalReference,CreatedAtUtc=correction.OccurredAtUtc},cancellationToken:token);
    }

    public async Task<IReadOnlyList<PaidOverageAccount>> GetClosedAccountsAsync(DateTime now,CancellationToken token)
    {
        var cutoff=new DateTime(now.Year,now.Month,1,0,0,0,DateTimeKind.Utc);
        var rows=await _events.Aggregate().Match(x=>x.Status=="settled" && x.PaidOveragePricingVersion!=null && x.BillingStatementId==null &&
            x.AuthorizedAtUtc<cutoff && (x.SettledRequestOverageUsd>0 || x.SettledAiOverageUsd>0))
            .Group<BsonDocument>(new BsonDocument {{"_id",new BsonDocument {{"UserId","$UserId"},
                {"Month",new BsonDocument("$dateToString",new BsonDocument {{"format","%Y-%m"},{"date","$AuthorizedAtUtc"},{"timezone","UTC"}})}}}})
            .Limit(25).ToListAsync(token);
        var accounts=rows.Select(x=>new PaidOverageAccount(x["_id"]["UserId"].AsString,x["_id"]["Month"].AsString)).ToList();
        var creditFilter=Builders<PaidOverageAdjustment>.Filter.Eq(x=>x.BillingStatementId,null) & Builders<PaidOverageAdjustment>.Filter.Or(
            Builders<PaidOverageAdjustment>.Filter.Lt(x=>x.Month,now.ToString("yyyy-MM",CultureInfo.InvariantCulture)),Builders<PaidOverageAdjustment>.Filter.Lt(x=>x.AmountUsd,0));
        var credits=await OverageAdjustments.Find(creditFilter).Limit(100).ToListAsync(token);
        accounts.AddRange(credits.Select(x=>new PaidOverageAccount(x.UserId,x.Month)));
        return accounts.Distinct().Take(25).ToArray();
    }
    public async Task<IReadOnlyList<PaidOverageStatement>> GetReadyAsync(DateTime now,CancellationToken token) =>
        await OverageStatements.Find(x=>x.State!="paid" && x.State!="void" && x.State!="uncollectible" && x.NextAttemptAtUtc<=now &&
            (x.LeaseUntilUtc==null || x.LeaseUntilUtc<=now)).SortBy(x=>x.NextAttemptAtUtc).Limit(25).ToListAsync(token);
    public async Task<IReadOnlyList<PaidOverageStatement>> GetStatementsAsync(string userId,int limit,CancellationToken token) =>
        await OverageStatements.Find(userId==null ? Builders<PaidOverageStatement>.Filter.Empty : Builders<PaidOverageStatement>.Filter.Eq(x=>x.UserId,userId))
            .SortByDescending(x=>x.CreatedAtUtc).Limit(Math.Clamp(limit,1,500)).ToListAsync(token);
    public Task<PaidOverageStatement> GetStatementAsync(string id,CancellationToken token) => OverageStatements.Find(x=>x.Id==id).FirstOrDefaultAsync(token);
    public Task<PaidOverageStatement> ClaimAsync(string id,string owner,DateTime now,CancellationToken token) =>
        OverageStatements.FindOneAndUpdateAsync(x=>x.Id==id && x.State!="paid" && x.State!="void" && x.State!="uncollectible" &&
            (x.LeaseUntilUtc==null || x.LeaseUntilUtc<=now),Builders<PaidOverageStatement>.Update.Set(x=>x.LeaseOwner,owner)
            .Set(x=>x.LeaseUntilUtc,now.AddMinutes(10)).Set(x=>x.UpdatedAtUtc,now).Inc(x=>x.Attempts,1),
            new FindOneAndUpdateOptions<PaidOverageStatement>{ReturnDocument=ReturnDocument.After},token);
    public async Task AttachInvoiceAsync(string id,string owner,string invoiceId,CancellationToken token)
    {
        var result=await OverageStatements.UpdateOneAsync(x=>x.Id==id && x.LeaseOwner==owner &&
            (x.StripeInvoiceId==null || x.StripeInvoiceId==invoiceId),Builders<PaidOverageStatement>.Update.Set(x=>x.StripeInvoiceId,invoiceId),cancellationToken:token);
        if(result.MatchedCount!=1)throw new SubscriptionUsageConflictException("The billing lease was lost or another invoice owns this statement.");
    }
    public Task RecordInvoiceAsync(string id,string owner,PaidOverageInvoiceResult invoice,string eventId,string fingerprint,CancellationToken token) =>
        TransactAsync(async(session,ct)=>
        {
            var statement=await OverageStatements.Find(session,x=>x.Id==id).FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Overage statement not found.");
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
            await Subscriptions.UpdateOneAsync(session,x=>x.UserId==statement.UserId,Builders<SubscriptionRecord>.Update.Set(x=>x.OveragePaymentBlocked,blocked).Inc(x=>x.AuthorizationVersion,1),cancellationToken:ct);
            if(invoice.Status=="paid" && invoice.InvoiceId!=null)
                await InsertInvoiceAsync(session,new OrderRecord {UserId=statement.UserId,StripeInvoiceId=invoice.InvoiceId,Amount=invoice.AmountPaid/100m,
                    BilledAmount=invoice.Subtotal/100m,Currency="USD",Description="OASIS USD paid overages "+statement.Month,Status="paid",CreatedAt=_time.GetUtcNow().UtcDateTime},ct);
            if(eventId!=null)
                await BillingAudit.InsertOneAsync(session,new BsonDocument{{"_id","overage-stripe:"+eventId},{"Kind","overage-stripe-event"},
                    {"UserId",statement.UserId},{"SourceFingerprint",fingerprint},{"OccurredAtUtc",statement.UpdatedAtUtc},{"PayloadJson",JsonSerializer.Serialize(invoice)}},cancellationToken:ct);
            return true;
        },token);
    public async Task RetryAsync(string id,string owner,string error,CancellationToken token)
    {
        var now=_time.GetUtcNow().UtcDateTime;
        await OverageStatements.UpdateOneAsync(x=>x.Id==id && x.LeaseOwner==owner,Builders<PaidOverageStatement>.Update
            .Set(x=>x.LeaseOwner,null).Set(x=>x.LeaseUntilUtc,null).Set(x=>x.State,"retry").Set(x=>x.LastError,error)
            .Set(x=>x.NextAttemptAtUtc,now.AddMinutes(5)).Set(x=>x.UpdatedAtUtc,now),cancellationToken:token);
    }
}

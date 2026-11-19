using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed class PaidOverageStatement
{
    public string AppId {get;set;}
    public string PayerAvatarId {get;set;}
    public string[] AppConsentIds {get;set;}=Array.Empty<string>();
    public string[] PersonalConsentIds {get;set;}=Array.Empty<string>();
    public bool CreditsOnly {get;set;}
    [BsonId] public string Id {get;set;}
    public string UserId {get;set;}
    public string Month {get;set;}
    public string Scope {get;set;}
    public string StripeCustomerId {get;set;}
    public string StripeSubscriptionId {get;set;}
    public string[] OperationIds {get;set;}=Array.Empty<string>();
    public string[] AdjustmentIds {get;set;}=Array.Empty<string>();
    public string[] PricingVersions {get;set;}=Array.Empty<string>();
    [BsonRepresentation(BsonType.Decimal128)] public decimal RequestChargesUsd {get;set;}
    [BsonRepresentation(BsonType.Decimal128)] public decimal AiChargesUsd {get;set;}
    public long AmountMinorUsd {get;set;}
    public string State {get;set;}="pending";
    public string StripeInvoiceId {get;set;}
    public string HostedInvoiceUrl {get;set;}
    public string LeaseOwner {get;set;}
    public DateTime? LeaseUntilUtc {get;set;}
    public DateTime CreatedAtUtc {get;set;}
    public DateTime UpdatedAtUtc {get;set;}
    public DateTime NextAttemptAtUtc {get;set;}
    public bool PaymentFailed {get;set;}
    public int Attempts {get;set;}
    public string LastError {get;set;}
}
public sealed record PaidOverageAccount(string UserId,string Month,bool CreditsOnly=false);
public sealed record PaidOverageInvoiceResult(string InvoiceId,string Status,string HostedInvoiceUrl,long Subtotal,long AmountPaid,bool Attempted);

internal static class PaidOverageStatementBinding
{
    internal static void RequirePayer(PaidOverageStatement statement)
    {
        if (statement == null) throw new ArgumentNullException(nameof(statement));
        if (statement.AppId != null) {
            if (!AppAccountRules.IsValidAppId(statement.AppId) || statement.UserId != MongoSubscriptionUsageRepository.PoolSubject(statement.AppId) ||
                !Guid.TryParseExact(statement.PayerAvatarId, "D", out var payer) || payer == Guid.Empty)
                throw new SubscriptionUsageConflictException("OVERAGE_STATEMENT_BINDING_REQUIRED: App invoice requires its original app and nonempty payer identity.");
        } else if (statement.PayerAvatarId != null || !Guid.TryParseExact(statement.UserId, "D", out var subscriber) || subscriber == Guid.Empty)
            throw new SubscriptionUsageConflictException("OVERAGE_STATEMENT_BINDING_REQUIRED: Personal invoice requires its nonempty original subscriber identity.");
    }
}

public interface IPaidOverageStatementStore
{
    Task<PaidOverageStatement> PrepareAsync(string userId,string month,string requestKey,CancellationToken token,bool creditsOnly=false);
    Task<IReadOnlyList<PaidOverageAccount>> GetClosedAccountsAsync(DateTime now,CancellationToken token);
    Task<IReadOnlyList<PaidOverageStatement>> GetReadyAsync(DateTime now,CancellationToken token);
    Task<IReadOnlyList<PaidOverageStatement>> GetStatementsAsync(string userId,int limit,CancellationToken token);
    Task<PaidOverageStatement> GetStatementAsync(string id,CancellationToken token);
    Task<PaidOverageStatement> GetStatementForSubscriberAsync(string userId,string id,CancellationToken token);
    Task<PaidOverageStatement> ClaimAsync(string id,string owner,DateTime now,CancellationToken token);
    Task AttachInvoiceAsync(string id,string owner,string invoiceId,CancellationToken token);
    Task RecordInvoiceAsync(string id,string owner,PaidOverageInvoiceResult invoice,string eventId,string fingerprint,CancellationToken token);
    Task RetryAsync(string id,string owner,string error,CancellationToken token);
}

public sealed class PaidOverageAdjustment
{
    public string Scope {get;set;}
    public string SourceStatementId {get;set;}
    public string SourceOperationId {get;set;}
    public AppRequestChargeTerms AppRequestChargeTerms {get;set;}
    public PersonalOverageChargeTerms PersonalOverageChargeTerms {get;set;}
    [BsonId] public string Id {get;set;}
    public string UserId {get;set;}
    public string Month {get;set;}
    public string BillingStatementId {get;set;}
    [BsonRepresentation(BsonType.Decimal128)] public decimal AmountUsd {get;set;}
    public string Reason {get;set;}
    public string EvidenceReference {get;set;}
    public DateTime CreatedAtUtc {get;set;}
}

/// <summary>Signed personal liability and writeoff attributed to the affected original operation.</summary>
public sealed class PersonalChargeMovement
{
    public string OperationId {get;set;}
    public string ConsentId {get;set;}
    public string SourceStatementId {get;set;}
    [BsonRepresentation(BsonType.Decimal128)] public decimal ChargeDeltaUsd {get;set;}
    [BsonRepresentation(BsonType.Decimal128)] public decimal WriteOffDeltaUsd {get;set;}
}

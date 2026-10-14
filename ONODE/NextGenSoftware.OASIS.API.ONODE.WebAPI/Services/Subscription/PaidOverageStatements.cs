using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public sealed class PaidOverageStatement
{
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
public sealed record PaidOverageAccount(string UserId,string Month);
public sealed record PaidOverageInvoiceResult(string InvoiceId,string Status,string HostedInvoiceUrl,long Subtotal,long AmountPaid,bool Attempted);

public interface IPaidOverageStatementStore
{
    Task<PaidOverageStatement> PrepareAsync(string userId,string month,string requestKey,CancellationToken token);
    Task<IReadOnlyList<PaidOverageAccount>> GetClosedAccountsAsync(DateTime now,CancellationToken token);
    Task<IReadOnlyList<PaidOverageStatement>> GetReadyAsync(DateTime now,CancellationToken token);
    Task<IReadOnlyList<PaidOverageStatement>> GetStatementsAsync(string userId,int limit,CancellationToken token);
    Task<PaidOverageStatement> GetStatementAsync(string id,CancellationToken token);
    Task<PaidOverageStatement> ClaimAsync(string id,string owner,DateTime now,CancellationToken token);
    Task AttachInvoiceAsync(string id,string owner,string invoiceId,CancellationToken token);
    Task RecordInvoiceAsync(string id,string owner,PaidOverageInvoiceResult invoice,string eventId,string fingerprint,CancellationToken token);
    Task RetryAsync(string id,string owner,string error,CancellationToken token);
}

public sealed class PaidOverageAdjustment
{
    [BsonId] public string Id {get;set;}
    public string UserId {get;set;}
    public string Month {get;set;}
    public string BillingStatementId {get;set;}
    [BsonRepresentation(BsonType.Decimal128)] public decimal AmountUsd {get;set;}
    public string Reason {get;set;}
    public string EvidenceReference {get;set;}
    public DateTime CreatedAtUtc {get;set;}
}

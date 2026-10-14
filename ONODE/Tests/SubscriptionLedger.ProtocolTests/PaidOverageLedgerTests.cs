using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace SubscriptionLedger.ProtocolTests;

public sealed class PaidOverageLedgerTests : IAsyncLifetime
{
    private readonly string _dbName="ovgt_"+Guid.NewGuid().ToString("N");
    private readonly string _user=Guid.NewGuid().ToString("D");
    private MongoClient _client;
    private MongoSubscriptionUsageRepository _ledger;
    private readonly LedgerClock _clock=new();
    private readonly Policy _policy=new();
    private readonly PaidOveragePricing _pricing=new(){Version="overage-test-v1",Currency="USD",
        RequestRatesUsd=new(){{"bronze",.001m},{"silver",.0005m},{"gold",.0002m},{"enterprise",.0001m}}};
    private sealed class Policy : ISubscriptionUsagePolicyProvider
    {
        public SubscriptionUsagePolicy Current=new(){MonthlyRequestLimit=2,DailyCallLimit=1,DailyTokenLimit=10,MonthlyBudgetUsd=1};
        public SubscriptionUsagePolicy GetPolicy(string planId,int karma)=>Current;
    }
    public async Task InitializeAsync()
    {
        string uri=Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI");
        if(string.IsNullOrWhiteSpace(uri))throw new InvalidOperationException("Disposable replica-set connection is required for financial integration coverage.");
        _client=new MongoClient(uri);_ledger=new(_client,_dbName,_clock,_policy,overagePricing:_pricing);
        var inventory=await _ledger.GetMigrationInventoryAsync(default);
        await _ledger.ImportOpeningBalanceAsync("test",new(){MigrationId=Guid.NewGuid().ToString("D"),ApprovedBy="test",SourceDigest=inventory.SourceDigest,EvidenceSha256=new string('a',64),Signature=new string('b',64)},default);
        await _client.GetDatabase(_dbName).GetCollection<SubscriptionRecord>("subscription_records").InsertOneAsync(new(){UserId=_user,PlanId="bronze",Status="active",
            StripeCustomerId="cus_isolated_fixture",StripeSubscriptionId="sub_isolated_fixture",StripeSubscriptionCreatedAtUtc=_clock.Now,CurrentPeriodEnd=_clock.Now.AddMonths(1)});
    }
    public async Task DisposeAsync()
    {
        if(_client==null)return;
        if(!_dbName.StartsWith("ovgt_",StringComparison.Ordinal) || _dbName.Length!=37)throw new InvalidOperationException("Refusing to delete an unexpected test database.");
        await _client.DropDatabaseAsync(_dbName);
    }
    private Task Consent(decimal cap=2)=>_ledger.SetPaidOverageConsentAsync(_user,"sub_isolated_fixture",_pricing.Version,cap,default);
    private SubscriptionUsageEvent Reservation(decimal cost=0.1m)=>new(){OperationId=Guid.NewGuid().ToString("D"),UserId=_user,ConsumingService="WEB6",Endpoint="POST /isolated-billing-test",
        MeterCategory="ai.tokens",RequestedUnits=10,ReservedCostUsd=cost,RequestFingerprint=new string('a',64),Status="authorized",AuthorizedAtUtc=_clock.Now,ExpiresAtUtc=_clock.Now.AddMinutes(15)};
    private async Task<SubscriptionUsageEvent> Authorize(decimal cost=0.1m)=>(await _ledger.AuthorizeAsync(Reservation(cost),0,default)).Event;
    private async Task<SubscriptionUsageAggregate> Finish(SubscriptionUsageEvent op,decimal actual=0.01m,string outcome="succeeded")
    {
        await _ledger.StartAsync(new(){OperationId=op.OperationId,UserId=_user,ConsumingService="WEB6"},default);
        var request=new UsageSettlementRequest {OperationId=op.OperationId,UserId=_user,ConsumingService="WEB6",Outcome=outcome,Provider="isolated-fixture",Model="test-model",
            ProviderRequestId=op.OperationId,PromptTokens=1,ActualCostUsd=actual,EstimatedCostUsd=op.ReservedCostUsd,Units=1,CostSource="isolated-test-measurement",PricingCatalogueVersion="fixture-v1",
            ProviderReceipts=new(){new(){Provider="isolated-fixture",Model="test-model",ProviderRequestId=op.OperationId,PromptTokens=1,Units=1,ActualCostUsd=actual,CostSource="isolated-test-measurement",PricingCatalogueVersion="fixture-v1"}}};
        var settled=await _ledger.SettleAsync(_user,request,default);
        Assert.True((await _ledger.SettleAsync(_user,request,default)).AlreadySettled);
        return settled.Aggregate;
    }
    [Fact]
    public async Task ConsentBypassesIncludedQuotasButOnlySuccessfulExtraRequestsAreBilled()
    {
        await Consent();await Finish(await Authorize());await Finish(await Authorize());
        var third=await Authorize();var totals=await Finish(third);
        Assert.Equal(.001m,totals.SettledOverageUsd);Assert.Equal(0,totals.ReservedOverageUsd);
        var fourth=await Authorize();totals=await Finish(fourth,outcome:"failed");
        Assert.Equal(.001m,totals.SettledOverageUsd);Assert.Equal(4,totals.MonthlyRequests);
    }
    [Fact]
    public async Task FailedAiWorkBillsOnlyVerifiedCostBeyondItsIncludedBudget()
    {
        await Consent();var a=await Authorize(.6m);var b=await Authorize(.6m);
        var first=await Finish(b,.6m,"failed");Assert.Equal(0,first.SettledOverageUsd);
        var second=await Finish(a,.6m,"failed");Assert.Equal(.2m,second.SettledOverageUsd);Assert.Equal(0,second.ReservedOverageUsd);
    }
    [Fact]
    public async Task ConcurrentReservationsCannotExceedTheConsentedSpendingCap()
    {
        await Consent(.50m);
        var results=await Task.WhenAll(Enumerable.Range(0,12).Select(async _=>{try{await Authorize();return true;}catch(SubscriptionUsageLimitException ex){Assert.Equal("OVERAGE_SPENDING_CAP_EXCEEDED",ex.Code);return false;}}));
        Assert.Equal(4,results.Count(x=>x));
        var aggregate=await _ledger.GetAggregateAsync(_user,default);Assert.Equal(.402m,aggregate.ReservedOverageUsd);
        Assert.Equal(4,aggregate.MonthlyRequests);
        _clock.Now=_clock.Now.AddMinutes(16);await _ledger.ExpireAsync(_clock.Now,100,default);
        aggregate=await _ledger.GetAggregateAsync(_user,default);Assert.Equal(0,aggregate.ReservedOverageUsd);Assert.Equal(0,aggregate.MonthlyRequests);
    }
    [Fact]
    public async Task DisablingOveragesDoesNotEraseAuthorizedCharges()
    {
        _policy.Current=new(){MonthlyRequestLimit=2,MonthlyBudgetUsd=.1m};
        await Consent();var operation=await Authorize(.2m);
        await _ledger.SetPayAsYouGoAsync(_user,false);
        Assert.Equal(.1m,(await Finish(operation,.2m)).SettledOverageUsd);
        await Assert.ThrowsAsync<SubscriptionUsageLimitException>(()=>Authorize());
    }
    [Fact]
    public async Task APriceChangeWithoutANewConsentCannotRepriceExistingCustomers()
    {
        await Consent();_pricing.RequestRatesUsd["bronze"]*=2;
        var ex=await Assert.ThrowsAsync<SubscriptionUsageLimitException>(()=>Authorize());Assert.Equal("OVERAGE_TERMS_REQUIRED",ex.Code);
    }
    [Fact]
    public async Task ConcurrentBillingClaimsAllocateEachOperationToOneStatement()
    {
        _policy.Current=new(){MonthlyRequestLimit=0,MonthlyBudgetUsd=.1m};await Consent();await Finish(await Authorize(.7m),.7m);
        var statements=await Task.WhenAll(Enumerable.Range(0,5).Select(i=>_ledger.PrepareAsync(_user,"2026-09","request-"+i,default)));
        var prepared=Assert.Single(statements.Where(x=>x!=null));Assert.Equal(60,prepared.AmountMinorUsd);
        Assert.Single(await _ledger.GetStatementsAsync(_user,100,default));
        var claimed=await _ledger.ClaimAsync(prepared.Id,"worker-1",_clock.Now,default);Assert.NotNull(claimed);
        Assert.Null(await _ledger.ClaimAsync(prepared.Id,"worker-2",_clock.Now,default));
        await _ledger.AttachInvoiceAsync(prepared.Id,"worker-1","in_isolated",default);
        var paid=new PaidOverageInvoiceResult("in_isolated","paid","https://example.test/invoice",60,60,true);
        await _ledger.RecordInvoiceAsync(prepared.Id,"worker-1",paid,null,null,default);
        await _ledger.RecordInvoiceAsync(prepared.Id,null,paid,"evt_isolated","hash",default);
        await _ledger.RecordInvoiceAsync(prepared.Id,null,paid,"evt_isolated","hash",default);
        Assert.Single(await _ledger.GetOrdersAsync(_user));
        await _ledger.RecordInvoiceAsync(prepared.Id,null,paid with {Status="open"},"evt_late_failure","late-hash",default);
        Assert.Equal("paid",(await _ledger.GetStatementAsync(prepared.Id,default)).State);
        await Assert.ThrowsAsync<SubscriptionUsageConflictException>(()=>_ledger.RecordInvoiceAsync(prepared.Id,null,paid with {Subtotal=61},"evt_tampered","tamper",default));
    }
    [Fact]
    public async Task FailedPaymentEnforcesIncludedQuotasUntilTheSameInvoiceIsPaid()
    {
        _policy.Current=new(){MonthlyRequestLimit=0,MonthlyBudgetUsd=.1m};await Consent();await Finish(await Authorize(.7m),.7m);
        var prepared=await _ledger.PrepareAsync(_user,"2026-09","failed-card",default);
        await _ledger.RecordInvoiceAsync(prepared.Id,null,new("in_declined","open",null,60,0,true),"evt_declined","declined",default);
        Assert.True((await _ledger.GetSubscriptionAsync(_user)).OveragePaymentBlocked);
        await Assert.ThrowsAsync<SubscriptionUsageLimitException>(()=>Authorize());
        await _ledger.RecordInvoiceAsync(prepared.Id,null,new("in_declined","paid",null,60,60,true),"evt_paid","paid",default);
        Assert.False((await _ledger.GetSubscriptionAsync(_user)).OveragePaymentBlocked);
        Assert.NotNull(await Authorize());
    }
    [Fact]
    public async Task ACorrectedBilledProviderCostCreatesOneUsdCreditWithoutRewritingHistory()
    {
        _policy.Current=new(){MonthlyRequestLimit=0,MonthlyBudgetUsd=.1m};await Consent();
        var operation=await Authorize(.7m);await Finish(operation,.7m);
        var billed=await _ledger.PrepareAsync(_user,"2026-09","original",default);
        await _ledger.RecordInvoiceAsync(billed.Id,null,new("in_original","paid",null,60,60,true),"evt_original","original",default);
        var correction=new UsageCorrectionRequest {CorrectionId=Guid.NewGuid().ToString("D"),OperationId=operation.OperationId,CorrectionKind="provider-measurement",
            Provider="isolated-fixture",ProviderRequestId=operation.OperationId,Reason="Controlled provider export revised its measured cost",EvidenceReference="isolated-test-export",CostDeltaUsd=-.1m};
        var entry=await _ledger.CorrectAsync("test-admin",correction,default);Assert.Equal(-.1m,entry.SettledOverageDeltaUsd);
        await _ledger.CorrectAsync("test-admin",correction,default);
        var credit=await _ledger.PrepareAsync(_user,"2026-09","credit",default);Assert.Equal(-10,credit.AmountMinorUsd);Assert.Single(credit.AdjustmentIds);
        Assert.Equal(.501m,(await _ledger.GetAggregateAsync(_user,default)).SettledOverageUsd);
        Assert.Equal(.601m,billed.RequestChargesUsd+billed.AiChargesUsd);
    }
    [Fact]
    public async Task FinancialCorrectionCannotExceedTheCustomersApprovedCap()
    {
        _policy.Current=new(){MonthlyRequestLimit=0,MonthlyBudgetUsd=.1m};await Consent(1);
        var operation=await Authorize(.7m);await Finish(operation,.6m);
        await Consent(.51m);
        var entry=await _ledger.CorrectAsync("test-admin",new UsageCorrectionRequest {CorrectionId=Guid.NewGuid().ToString("D"),OperationId=operation.OperationId,
            CorrectionKind="provider-measurement",Provider="isolated-fixture",ProviderRequestId=operation.OperationId,Reason="Controlled positive cost revision",EvidenceReference="isolated-test-export",CostDeltaUsd=.1m},default);
        Assert.Equal(.009m,entry.SettledOverageDeltaUsd);Assert.Equal(.091m,entry.OverageWriteOffDeltaUsd);
    }
    [Fact]
    public async Task PayNowUsesCumulativeCentRoundingAndNeverRoundsEveryRequestUp()
    {
        _policy.Current=new(){MonthlyRequestLimit=0};await Consent();
        for(int i=0;i<4;i++)await Finish(await Authorize(0),0);
        var first=await _ledger.PrepareAsync(_user,"2026-09","first",default);Assert.Equal(0,first.AmountMinorUsd);
        for(int i=0;i<4;i++)await Finish(await Authorize(0),0);
        var second=await _ledger.PrepareAsync(_user,"2026-09","second",default);Assert.Equal(1,second.AmountMinorUsd);
        Assert.Equal(.008m,first.RequestChargesUsd+second.RequestChargesUsd);
    }
}


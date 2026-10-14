using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Stripe;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription;

public sealed class StripeOverageSandboxFactAttribute : FactAttribute
{
    public StripeOverageSandboxFactAttribute()
    {
        if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OASIS_OVERAGE_STRIPE_TEST_KEY")))
            Skip="Explicit owned Stripe sandbox fixture credentials are required; production secrets are never used.";
    }
}
public sealed class PaidOverageStripeSandboxTests
{
    private sealed class Policy : ISubscriptionUsagePolicyProvider
    {
        public SubscriptionUsagePolicy GetPolicy(string plan,int karma)=>new(){MonthlyRequestLimit=0,MonthlyBudgetUsd=.1m};
    }
    private sealed class Clock(DateTime time) : TimeProvider {public override DateTimeOffset GetUtcNow()=>new(time);}
    [StripeOverageSandboxFact]
    public async Task ControlledUsageFixtureCollectsOneRealUsdSandboxInvoiceAndCredit()
    {
        string key=Environment.GetEnvironmentVariable("OASIS_OVERAGE_STRIPE_TEST_KEY");Assert.StartsWith("sk_test_",key);
        string user=Environment.GetEnvironmentVariable("OASIS_OVERAGE_FIXTURE_USER");Assert.True(Guid.TryParseExact(user,"D",out _));
        string customer=Environment.GetEnvironmentVariable("OASIS_OVERAGE_FIXTURE_CUSTOMER");
        string subscription=Environment.GetEnvironmentVariable("OASIS_OVERAGE_FIXTURE_SUBSCRIPTION");
        var mongo=new MongoClient(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        string scope="ovgx_"+Guid.NewGuid().ToString("N");
        var now=DateTime.UtcNow;var clock=new Clock(new DateTime(now.Year,now.Month,1,12,0,0,DateTimeKind.Utc).AddDays(-1));
        var prices=PaidOveragePricing.Load();
        try
        {
            var ledger=new MongoSubscriptionUsageRepository(mongo,scope,clock,new Policy(),overagePricing:prices);
            var inventory=await ledger.GetMigrationInventoryAsync(default);
            await ledger.ImportOpeningBalanceAsync("sandbox-test",new(){MigrationId=Guid.NewGuid().ToString("D"),ApprovedBy="sandbox-test",SourceDigest=inventory.SourceDigest,EvidenceSha256=new string('a',64),Signature=new string('b',64)},default);
            await mongo.GetDatabase(scope).GetCollection<SubscriptionRecord>("subscription_records").InsertOneAsync(new(){UserId=user,PlanId="silver",Status="active",
                StripeCustomerId=customer,StripeSubscriptionId=subscription,StripeSubscriptionCreatedAtUtc=clock.GetUtcNow().UtcDateTime,CurrentPeriodEnd=now.AddMonths(1)});
            await ledger.SetPaidOverageConsentAsync(user,subscription,prices.Version,2,default);
            var op=new SubscriptionUsageEvent {OperationId=Guid.NewGuid().ToString("D"),UserId=user,ConsumingService="WEB6",Endpoint="POST /controlled-sandbox-usage-fixture",
                MeterCategory="ai.tokens",RequestedUnits=10,ReservedCostUsd=.7m,RequestFingerprint=new string('a',64),Status="authorized",AuthorizedAtUtc=clock.GetUtcNow().UtcDateTime,ExpiresAtUtc=clock.GetUtcNow().UtcDateTime.AddMinutes(15)};
            await ledger.AuthorizeAsync(op,0,default);await ledger.StartAsync(new(){OperationId=op.OperationId,UserId=user,ConsumingService="WEB6"},default);
            await ledger.SettleAsync(user,new(){OperationId=op.OperationId,UserId=user,ConsumingService="WEB6",Outcome="succeeded",Provider="controlled-sandbox-fixture",Model="fixture",
                ProviderRequestId=op.OperationId,PromptTokens=1,Units=1,ActualCostUsd=.7m,EstimatedCostUsd=.7m,CostSource="controlled-sandbox-fixture",PricingCatalogueVersion="fixture-v1",
                ProviderReceipts=new(){new(){Provider="controlled-sandbox-fixture",Model="fixture",ProviderRequestId=op.OperationId,PromptTokens=1,Units=1,ActualCostUsd=.7m,CostSource="controlled-sandbox-fixture",PricingCatalogueVersion="fixture-v1"}}},default);
            var gateway=new StripePaidOverageGateway(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string>{{"STRIPE_SECRET_KEY",key}}).Build());
            var billing=new PaidOverageBillingService(ledger,gateway,NullLogger<PaidOverageBillingService>.Instance,clock);
            string month=op.AuthorizedAtUtc.ToString("yyyy-MM");
            var statement=await ledger.PrepareAsync(user,month,"sandbox-proof",default);
            Assert.Equal(60,statement.AmountMinorUsd);
            var paid=await billing.CollectAsync(statement,default);Assert.Equal("paid",paid.State);Assert.StartsWith("in_",paid.StripeInvoiceId);
            Assert.Equal(paid.StripeInvoiceId,(await billing.CollectAsync(statement,default)).StripeInvoiceId);
            var invoices=await new InvoiceService(new StripeClient(key)).ListAsync(new(){Customer=customer,Limit=100});
            Assert.Single(invoices.Data.Where(x=>x.Metadata?.GetValueOrDefault("oasis_overage_statement_id")==statement.Id));
            var invoice=await new InvoiceService(new StripeClient(key)).GetAsync(paid.StripeInvoiceId);
            Assert.False(invoice.Livemode);Assert.Equal("usd",invoice.Currency);Assert.Equal(60,invoice.Subtotal);Assert.Equal(60,invoice.AmountPaid);Assert.Null(invoice.SubscriptionId);
            await ledger.CorrectAsync("sandbox-test",new(){CorrectionId=Guid.NewGuid().ToString("D"),OperationId=op.OperationId,CorrectionKind="provider-measurement",
                Provider="controlled-sandbox-fixture",ProviderRequestId=op.OperationId,Reason="Controlled fixture cost revision; no live provider costs are claimed",EvidenceReference="controlled-sandbox-fixture-export",CostDeltaUsd=-.1m},default);
            var credit=await ledger.PrepareAsync(user,month,"sandbox-credit-proof",default);Assert.Equal(-10,credit.AmountMinorUsd);
            var credited=await billing.CollectAsync(credit,default);Assert.Equal("paid",credited.State);
            var creditInvoice=await new InvoiceService(new StripeClient(key)).GetAsync(credited.StripeInvoiceId);Assert.False(creditInvoice.Livemode);Assert.Equal(-10,creditInvoice.Subtotal);
            // Restore only the credit created by this owned test; retain the immutable invoice proof.
            await new CustomerBalanceTransactionService(new StripeClient(key)).CreateAsync(customer,new CustomerBalanceTransactionCreateOptions {
                Amount=10,Currency="usd",Description="Restore the isolated overage verification fixture after its USD credit test",Metadata=new(){{"oasis_ledger_scope",scope},{"oasis_credit_invoice",creditInvoice.Id}}});
            string evidence=Environment.GetEnvironmentVariable("OASIS_OVERAGE_EVIDENCE_PATH");
            if(!string.IsNullOrEmpty(evidence))await System.IO.File.WriteAllTextAsync(evidence,JsonSerializer.Serialize(new {scope,controlledUsageFixture=true,liveProviderCostsClaimed=false,
                chargeInvoice=invoice.Id,currency=invoice.Currency,subtotal=invoice.Subtotal,amountPaid=invoice.AmountPaid,creditInvoice=creditInvoice.Id,creditSubtotal=creditInvoice.Subtotal,duplicateInvoices=0,verifiedAt=DateTime.UtcNow}));
        }
        finally
        {
            if(!scope.StartsWith("ovgx_",StringComparison.Ordinal) || scope.Length!=37)throw new InvalidOperationException("Refusing to remove an unexpected fixture database.");
            await mongo.DropDatabaseAsync(scope);
        }
    }
}

using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private static async Task<string> PrepareFreshAppUsageThroughSdkAsync(HttpClient http, Func<string, string> token,
        IMongoDatabase db, string[] accounts, Assembly api, object repository, string serviceKey)
    {
        const string ns = "NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.";
        const string appId = "owned-app-a";
        var apps = db.GetCollection<BsonDocument>("subscription_app_accounts");
        var billing = db.GetCollection<BsonDocument>("subscription_billing_audit");
        async Task<object?> Call(string method, params object?[] args)
        {
            var task = (Task)repository.GetType().GetMethod(method)!.Invoke(repository, args)!;
            await task;
            return task.GetType().GetProperty("Result")?.GetValue(task);
        }
        var rotated = (await Call("RotateAppAccountKeyAsync", appId, "native-fresh-fixture", "Owned shipping client usage", CancellationToken.None))!;
        var appKey = (string)rotated.GetType().GetField("Item2")!.GetValue(rotated)!;
        await Call("SetAppOwnersAsync", appId, accounts, "native-fresh-fixture", "Owned original payer and portal owner", CancellationToken.None);
        var priceType = api.GetType(ns + "AppOveragePrices", true)!;
        var prices = priceType.GetMethod("Load")!.Invoke(null, null)!;
        var rate = (decimal)priceType.GetMethod("Rate")!.Invoke(prices, ["bronze"])!;
        Assert.Equal(0.001m, rate);
        var now = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).UtcDateTime;
        var settingsType = api.GetType(ns + "AppPaygSettings", true)!;
        var settings = Activator.CreateInstance(settingsType)!;
        var consentId = Guid.NewGuid().ToString("D");
        // Only original payer/readiness and quota boundary are financial INPUT fixtures.
        // The operation, start acknowledgement, provider execution, settlement and allocation below are real.
        foreach (var field in new Dictionary<string, object> {
            ["Enabled"] = true, ["MonthlyCapUsd"] = 0.5m, ["ConsentSchemaVersion"] = 1,
            ["ConsentId"] = consentId, ["BillingAvatarId"] = accounts[1], ["ConsentBy"] = accounts[1],
            ["ConsentAtUtc"] = now, ["PaymentVerifiedAtUtc"] = now, ["BillingAuthorizationVersion"] = 1L,
            ["PricingVersion"] = priceType.GetProperty("Version")!.GetValue(prices)!,
            ["PricingSha256"] = priceType.GetProperty("PricingSha256")!.GetValue(prices)!,
            ["Currency"] = "USD", ["RatePerRequestUsd"] = rate,
            ["VerifiedStripeCustomerId"] = "owned-fresh-private-customer", ["VerifiedStripeSubscriptionId"] = "owned-fresh-private-subscription"
        }) settingsType.GetProperty(field.Key)!.SetValue(settings, field.Value);
        await apps.UpdateOneAsync(new BsonDocument("_id", appId), new BsonDocument("$set", new BsonDocument {
            { "PlanId", "bronze" }, { "PooledQuota", true }, { "PerUserPlanId", "silver" }, { "Payg", settings.ToBsonDocument(settingsType) }
        }));
        await db.GetCollection<BsonDocument>("subscription_records").UpdateOneAsync(new BsonDocument("_id", accounts[1]),
            new BsonDocument("$set", new BsonDocument {
                { "StripeCustomerId", "owned-fresh-private-customer" }, { "StripeSubscriptionId", "owned-fresh-private-subscription" },
                { "AuthorizationVersion", 1L }, { "OveragePaymentBlocked", false }
            }));
        var evidence = new BsonDocument {
            { "_id", "app-payg-consent:" + consentId }, { "Kind", "app-payg-settings-changed" }, { "UserId", "app:" + appId },
            { "OccurredAtUtc", now }, { "PayloadJson", JsonSerializer.Serialize(new { AppId = appId, Actor = accounts[1], Settings = settings }) }
        };
        await billing.InsertOneAsync(evidence);
        await db.GetCollection<BsonDocument>("subscription_usage_buckets").InsertOneAsync(new BsonDocument {
            { "_id", "app:" + appId + ":month:" + now.ToString("yyyy-MM") }, { "UserId", "app:" + appId },
            { "PeriodType", "month" }, { "Period", now.ToString("yyyy-MM") }, { "Requests", 300000L }, { "UpdatedAtUtc", now }
        });
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token(accounts[0]));
        var client = new Web4SubscriptionAuthorizationClient(http, http.BaseAddress!.ToString(), "WEB5", serviceKey, appId, appKey);
        var operationId = Guid.NewGuid().ToString("D");
        var request = new UsageAuthorizationRequest {
            OperationId = operationId, ConsumingService = "WEB5", Endpoint = "/owned-fresh-usage", MeterCategory = "api.request",
            RequestedUnits = 1, EstimatedCostUsd = 0m, RequestFingerprint = new string('e', 64)
        };
        var decision = await client.AuthorizeUsageAsync(token(accounts[0]), request);
        Assert.True(decision.Allowed, decision.Code + ": " + decision.Message);
        Assert.Equal(operationId, decision.OperationId);
        Assert.True((await client.AuthorizeUsageAsync(token(accounts[0]), request)).AlreadyAuthorized);
        var start = new UsageStartRequest { OperationId = operationId, UserId = accounts[0], ConsumingService = "WEB5" };
        Assert.True((await client.StartUsageAsync(start)).Started);
        var native = db.GetCollection<BsonDocument>("owned_fresh_http_measurements");
        var nativeId = ObjectId.GenerateNewId();
        await native.InsertOneAsync(new BsonDocument { { "_id", nativeId }, { "OperationId", operationId } });
        Assert.Equal(operationId, (await native.Find(new BsonDocument("_id", nativeId)).SingleAsync())["OperationId"].AsString);
        var receipt = new UsageProviderReceipt {
            Provider = "MongoDBOASIS", ProviderRequestId = "owned-native:" + nativeId, ClientRequestId = operationId,
            Units = 1, ActualCostUsd = 0, CostSource = "owned-local-no-provider-fee", PricingCatalogueVersion = "owned-local-zero-cost"
        };
        var settlement = new UsageSettlementRequest {
            OperationId = operationId, UserId = accounts[0], ConsumingService = "WEB5", Outcome = "succeeded",
            Provider = receipt.Provider, ProviderRequestId = receipt.ProviderRequestId, Units = 1, ActualCostUsd = 0,
            CostSource = receipt.CostSource, PricingCatalogueVersion = receipt.PricingCatalogueVersion, ProviderReceipts = [receipt]
        };
        Assert.True((await client.SettleUsageAsync(token(accounts[0]), settlement)).Settled);
        Assert.True((await client.SettleUsageAsync(token(accounts[0]), settlement)).AlreadySettled);
        var events = db.GetCollection<BsonDocument>("subscription_usage_events");
        var settled = await events.Find(new BsonDocument("OperationId", operationId)).SingleAsync();
        Assert.Equal("settled", settled["Status"].AsString);
        Assert.Equal(rate, settled["SettledAppRequestOverageUsd"].ToDecimal());
        Assert.Equal(consentId, settled["AppRequestChargeTerms"]["ConsentId"].AsString);
        Assert.True(settled.GetValue("AppBillingStatementId", BsonNull.Value).IsBsonNull);
        Assert.Equal(0L, await db.GetCollection<BsonDocument>("subscription_overage_statements").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        // The shipping client must preserve an actual app policy denial, rather than treating it as an outage.
        Assert.Equal(1L, (await billing.DeleteOneAsync(new BsonDocument("_id", evidence["_id"]))).DeletedCount);
        var denied = await client.AuthorizeUsageAsync(token(accounts[0]), new UsageAuthorizationRequest {
            OperationId = Guid.NewGuid().ToString("D"), ConsumingService = "WEB5", Endpoint = "/owned-fresh-usage",
            MeterCategory = "api.request", RequestedUnits = 1, RequestFingerprint = new string('e', 64)
        });
        Assert.False(denied.Allowed); Assert.Equal(402, denied.StatusCode); Assert.Equal("APP_BILLING_CONSENT_REQUIRED", denied.Code);
        await billing.InsertOneAsync(evidence); // Exact owned input restored before statement preparation resolves original evidence.
        Assert.Equal(1L, await events.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Console.WriteLine("Fresh app usage native SDK verified: shipping authorization client, actual authenticated controller/service/app identity and authoritative karma; one reservation/replay/start, real Mongo insert acknowledgement and one measured zero-provider-fee settlement/replay. Original app platform fee0.001 retained and unallocated. Shipping client preserved explicit missing-consent402; exact owned original input restored. Original payer/readiness/quota boundary are INPUT fixtures, not genuine Stripe proof.");
        return operationId;
    }

    private static async Task VerifyFreshAppUsageAfterBrowserAsync(IMongoDatabase db, string operationId, string payer, object repository)
    {
        var statements = await db.GetCollection<BsonDocument>("subscription_overage_statements")
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var statement = Assert.Single(statements);
        Assert.Equal("owned-app-a", statement["AppId"].AsString);
        Assert.Equal(payer, statement["PayerAvatarId"].AsString);
        Assert.Equal("owned-fresh-private-customer", statement["StripeCustomerId"].AsString);
        Assert.Equal(0.001m, statement["RequestChargesUsd"].ToDecimal());
        Assert.Equal(0L, statement["AmountMinorUsd"].ToInt64());
        Assert.Equal("paid", statement["State"].AsString);
        Assert.Equal(operationId, statement["OperationIds"].AsBsonArray.Single().AsString);
        var stored = await db.GetCollection<BsonDocument>("subscription_usage_events").Find(new BsonDocument("OperationId", operationId)).SingleAsync();
        Assert.Equal(statement["_id"], stored["AppBillingStatementId"]);
        Assert.Equal(1L, stored["AppChargeVersion"].ToInt64());
        Assert.Equal(1L, await db.GetCollection<BsonDocument>("owned_fresh_http_measurements").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var checkTask = (Task)repository.GetType().GetMethod("CheckAppChargeProjectionAsync")!.Invoke(repository,
            ["owned-app-a", statement["Month"].AsString, CancellationToken.None])!;
        await checkTask;
        var check = checkTask.GetType().GetProperty("Result")!.GetValue(checkTask)!;
        Assert.True((bool)check.GetType().GetProperty("Balanced")!.GetValue(check)!);
        Assert.Equal(0.001m, (decimal)check.GetType().GetProperty("ExpectedSettledUsd")!.GetValue(check)!);
        Assert.Equal(0.001m, (decimal)check.GetType().GetProperty("ActualSettledUsd")!.GetValue(check)!);
        Console.WriteLine("Fresh app usage browser/native final state verified: real settled operation allocated once through public app-owner SDK/browser route; original payer/customer and0.001 platform fee retained; cumulative USD rounding0 cents, actual zero-due gateway paid once; app money projection balanced at0.001. No preloaded statement, provider execution duplicate, external payment or genuine Stripe readiness claim.");
    }
}

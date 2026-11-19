using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private static async Task VerifyAppBillingPolicyHttpAsync(HttpClient http, Func<string, string> token,
        IMongoDatabase db, string[] accounts, Assembly api, object repository, string serviceKey)
    {
        const string ns = "NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.";
        const string appId = "owned-app-a";
        var priorPool = Environment.GetEnvironmentVariable("OASIS_POOLED_APP_QUOTAS");
        var priorShards = Environment.GetEnvironmentVariable("OASIS_POOL_SHARDS");
        try
        {
            Environment.SetEnvironmentVariable("OASIS_POOLED_APP_QUOTAS", "true");
            Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", "1");
            var apps = db.GetCollection<BsonDocument>("subscription_app_accounts");
            var records = db.GetCollection<BsonDocument>("subscription_records");
            var billing = db.GetCollection<BsonDocument>("subscription_billing_audit");
            var now = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).UtcDateTime;
            var rotate = (Task)repository.GetType().GetMethod("RotateAppAccountKeyAsync")!.Invoke(repository,
                [appId, "native-policy-fixture", "Owned policy refusal credentials", CancellationToken.None])!;
            await rotate;
            var rotated = rotate.GetType().GetProperty("Result")!.GetValue(rotate)!;
            var appKey = (string)rotated.GetType().GetField("Item2")!.GetValue(rotated)!;
            var priceType = api.GetType(ns + "AppOveragePrices", true)!;
            var prices = priceType.GetMethod("Load")!.Invoke(null, null)!;
            var settingsType = api.GetType(ns + "AppPaygSettings", true)!;
            var settings = Activator.CreateInstance(settingsType)!;
            var consentId = Guid.NewGuid().ToString("D");
            // Explicit original financial INPUT evidence, never a real Stripe account or readiness proof.
            foreach (var field in new Dictionary<string, object> {
                ["Enabled"] = true, ["MonthlyCapUsd"] = 0.5m, ["ConsentSchemaVersion"] = 1,
                ["ConsentId"] = consentId, ["BillingAvatarId"] = accounts[1], ["ConsentBy"] = accounts[1],
                ["ConsentAtUtc"] = now, ["PaymentVerifiedAtUtc"] = now, ["BillingAuthorizationVersion"] = 1L,
                ["PricingVersion"] = priceType.GetProperty("Version")!.GetValue(prices)!,
                ["PricingSha256"] = priceType.GetProperty("PricingSha256")!.GetValue(prices)!,
                ["Currency"] = "USD", ["RatePerRequestUsd"] = priceType.GetMethod("Rate")!.Invoke(prices, ["bronze"])!,
                ["VerifiedStripeCustomerId"] = "owned-policy-private-customer", ["VerifiedStripeSubscriptionId"] = "owned-policy-private-subscription"
            }) settingsType.GetProperty(field.Key)!.SetValue(settings, field.Value);
            await apps.UpdateOneAsync(new BsonDocument("_id", appId), new BsonDocument("$set", new BsonDocument {
                { "PlanId", "bronze" }, { "PooledQuota", true }, { "PerUserPlanId", "silver" },
                { "OwnerAvatarIds", new BsonArray { accounts[1] } },
                { "Payg", settings.ToBsonDocument(settingsType) }
            }));
            await records.UpdateOneAsync(new BsonDocument("_id", accounts[1]), new BsonDocument("$set", new BsonDocument {
                { "StripeCustomerId", "owned-policy-private-customer" }, { "StripeSubscriptionId", "owned-policy-private-subscription" },
                { "AuthorizationVersion", 1L }, { "OveragePaymentBlocked", false }
            }));
            var month = now.ToString("yyyy-MM");
            await db.GetCollection<BsonDocument>("subscription_usage_buckets").InsertOneAsync(new BsonDocument {
                { "_id", "app:" + appId + ":month:" + month }, { "UserId", "app:" + appId },
                { "PeriodType", "month" }, { "Period", month }, { "Requests", 300000L }, { "UpdatedAtUtc", now }
            });
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token(accounts[0]));
            http.DefaultRequestHeaders.Add("X-OASIS-Service", "WEB5");
            http.DefaultRequestHeaders.Add("X-OASIS-Service-Key", serviceKey);
            http.DefaultRequestHeaders.Add("X-OASIS-App-Id", appId);
            http.DefaultRequestHeaders.Add("X-OASIS-App-Key", appKey);
            async Task<string> Snapshot()
            {
                var rows = new List<string>();
                foreach (var name in (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x))
                    rows.Add(name + ":" + string.Join("\n", (await db.GetCollection<BsonDocument>(name)
                        .Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).Select(x => x.ToJson())));
                return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", rows))));
            }
            async Task Refused(string code, HttpStatusCode status)
            {
                var before = await Snapshot();
                var operationId = Guid.NewGuid().ToString("D");
                using var content = new StringContent(JsonSerializer.Serialize(new {
                    operationId, consumingService = "WEB5", endpoint = "/owned-policy-refusal", meterCategory = "api.request",
                    requestFingerprint = new string('f', 64), requestedUnits = 1, estimatedCostUsd = 0m
                }), Encoding.UTF8, "application/json");
                using var response = await http.PostAsync("/api/subscription/usage/authorize", content);
                var body = await response.Content.ReadAsStringAsync();
                Assert.True(response.StatusCode == status, $"Expected {status}/{code}, got {response.StatusCode}: {body}");
                using var json = JsonDocument.Parse(body);
                Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
                Assert.Equal(operationId, json.RootElement.GetProperty("operationId").GetString());
                Assert.False(json.RootElement.GetProperty("allowed").GetBoolean());
                Assert.DoesNotContain("OPERATION_ID_CONFLICT", body);
                Assert.DoesNotContain("owned-policy-private", body);
                Assert.Equal(before, await Snapshot());
            }
            await Refused("APP_BILLING_CONSENT_REQUIRED", HttpStatusCode.PaymentRequired);
            await billing.InsertOneAsync(new BsonDocument {
                { "_id", "app-payg-consent:" + consentId }, { "Kind", "app-payg-settings-changed" },
                { "UserId", "app:" + appId }, { "OccurredAtUtc", now },
                { "PayloadJson", JsonSerializer.Serialize(new { AppId = appId, Actor = accounts[1], Settings = settings },
                    new JsonSerializerOptions()) }
            });
            await apps.UpdateOneAsync(new BsonDocument("_id", appId),
                new BsonDocument("$set", new BsonDocument("Payg.MonthlyCapUsd", new Decimal128(0.51m))));
            await Refused("APP_BILLING_CONSENT_CONFLICT", HttpStatusCode.Conflict);
            await apps.UpdateOneAsync(new BsonDocument("_id", appId),
                new BsonDocument("$set", new BsonDocument("Payg.MonthlyCapUsd", new Decimal128(0.5m))));
            await records.UpdateOneAsync(new BsonDocument("_id", accounts[1]),
                new BsonDocument("$set", new BsonDocument("Status", "cancelled")));
            await Refused("APP_BILLING_PAYER_UNAVAILABLE", HttpStatusCode.PaymentRequired);
            await records.UpdateOneAsync(new BsonDocument("_id", accounts[1]),
                new BsonDocument("$set", new BsonDocument("Status", "active")));
            await apps.UpdateOneAsync(new BsonDocument("_id", appId),
                new BsonDocument("$set", new BsonDocument("Payg.ConsentId", Guid.Empty.ToString("D"))));
            await Refused("APP_OVERAGE_TERMS_REQUIRED", HttpStatusCode.PaymentRequired);
            await apps.UpdateOneAsync(new BsonDocument("_id", appId), new BsonDocument("$set", new BsonDocument {
                { "Payg.ConsentId", consentId }, { "Payg.BillingAvatarId", Guid.Empty.ToString("D") }
            }));
            await Refused("APP_OVERAGE_TERMS_REQUIRED", HttpStatusCode.PaymentRequired);
            Assert.Equal(0L, await db.GetCollection<BsonDocument>("subscription_usage_events")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Console.WriteLine("App billing policy native HTTP verified: distinct missing-consent402, changed-consent409, unavailable-payer402 and empty consent/payer402 decisions preserve operation identity, expose no private payer evidence and mutate no collection. Actual service/app credentials, authenticated MVC, authoritative karma, real repository/Mongo; original financial INPUT fixtures only. No provider execution, Stripe readiness/payment, SDK/browser or hosted proof.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OASIS_POOLED_APP_QUOTAS", priorPool);
            Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", priorShards);
            Console.WriteLine("Owned app billing policy process settings restored.");
        }
    }
}

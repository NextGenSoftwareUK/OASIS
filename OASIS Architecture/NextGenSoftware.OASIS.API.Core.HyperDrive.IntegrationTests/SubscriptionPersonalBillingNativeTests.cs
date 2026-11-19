using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private sealed record PersonalBillingFixture(string PendingId, string LeasedId, string ForeignId,
        string LeaseOwner, string PricingVersion, string PricingHash, long OriginalOrders);

    private static async Task<PersonalBillingFixture> PreparePersonalBillingHttpAsync(HttpClient http,
        Func<string, string> token, IMongoDatabase db, string[] accounts, object repository)
    {
        const string pay = "/api/subscription/overages/pay-now";
        const string consent = "/api/subscription/toggle-pay-as-you-go";
        var now = DateTime.UtcNow;
        var month = now.ToString("yyyy-MM");
        var records = db.GetCollection<BsonDocument>("subscription_records");
        var statements = db.GetCollection<BsonDocument>("subscription_overage_statements");
        var orders = db.GetCollection<BsonDocument>("subscription_orders");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token(accounts[0]));
        using var pricing = JsonDocument.Parse(await http.GetStringAsync("/api/subscription/overages/prices"));
        var version = pricing.RootElement.GetProperty("version").GetString()!;
        var hash = pricing.RootElement.GetProperty("pricingSha256").GetString()!;
        // Original financial INPUT fixtures only: no Stripe-issued account/readiness/payment proof.
        await records.UpdateOneAsync(new BsonDocument("_id", accounts[0]), new BsonDocument("$set", new BsonDocument {
            { "StripeCustomerId", "owned-personal-private-customer" }, { "StripeSubscriptionId", "owned-personal-private-subscription" },
            { "PayAsYouGoEnabled", true }, { "MonthlyOverageLimitUsd", new Decimal128(5m) },
            { "PaidOverageTermsVersion", version }, { "PaidOverageTermsSha256", hash }, { "PaidOverageTermsAcceptedAtUtc", now }
        }));
        async Task<string> Seed(string user, string state)
        {
            var id = "ovg_" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(user + ":" + month + ":" + Guid.NewGuid().ToString("D"))));
            await statements.InsertOneAsync(new BsonDocument {
                { "_id", id }, { "UserId", user }, { "Month", month }, { "Scope", db.DatabaseNamespace.DatabaseName },
                { "StripeCustomerId", "owned-personal-private-customer" }, { "StripeSubscriptionId", "owned-personal-private-subscription" },
                { "RequestChargesUsd", new Decimal128(0m) }, { "AiChargesUsd", new Decimal128(0m) }, { "AmountMinorUsd", 0L },
                { "State", state }, { "PaymentFailed", false }, { "LastError", "owned-personal-private-error" },
                { "OperationIds", new BsonArray() }, { "AdjustmentIds", new BsonArray() }, { "PricingVersions", new BsonArray { version } },
                { "CreatedAtUtc", now }, { "UpdatedAtUtc", now }, { "NextAttemptAtUtc", now }
            });
            return id;
        }
        var pending = await Seed(accounts[0], "pending");
        var leased = await Seed(accounts[0], "retry");
        var foreign = await Seed(accounts[1], "pending");
        var leaseOwner = Guid.NewGuid().ToString("D");
        var claim = (Task)repository.GetType().GetMethod("ClaimAsync")!.Invoke(repository,
            [leased, leaseOwner, now, CancellationToken.None])!;
        await claim;
        Assert.NotNull(claim.GetType().GetProperty("Result")!.GetValue(claim));
        async Task<string> Snapshot()
        {
            var rows = new List<string>();
            foreach (var name in (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x))
                rows.Add(name + ":" + string.Join("\n", (await db.GetCollection<BsonDocument>(name)
                    .Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).Select(x => x.ToJson())));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", rows))));
        }
        async Task<string> Send(string path, string body, HttpStatusCode expected)
        {
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(path, content);
            var text = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == expected, $"Expected {expected}, got {response.StatusCode}: {text}");
            return text;
        }
        var before = await Snapshot();
        foreach (var body in new[] { "{}", "[]", "null", "{\"enabled\":null}", "{\"enabled\":\"false\"}",
            "{\"enabled\":false,\"Enabled\":false}", "{\"enabled\":false,\"stripeCustomerId\":\"forged\"}",
            "{\"enabled\":true}", "{\"enabled\":false,\"monthlyLimitUsd\":\"5\"}" })
            await Send(consent, body, HttpStatusCode.BadRequest);
        foreach (var body in new[] { "{}", "[]", "null", "{\"billingRequestId\":\"invalid\"}",
            "{\"billingRequestId\":\"00000000-0000-0000-0000-000000000000\"}",
            "{\"statementId\":\"" + pending + "\",\"StatementId\":\"" + pending + "\"}",
            "{\"statementId\":\"" + pending + "\",\"month\":\"" + month + "\"}",
            "{\"statementId\":\"" + pending + "\",\"billingRequestId\":\"" + Guid.NewGuid().ToString("D") + "\"}",
            "{\"statementId\":\"" + pending + "\",\"payerAvatarId\":\"forged\"}",
            "{\"statementId\":null}" })
            await Send(pay, body, HttpStatusCode.BadRequest);
        await Send(pay, JsonSerializer.Serialize(new { statementId = foreign }), HttpStatusCode.NotFound);
        await Send(pay, JsonSerializer.Serialize(new { statementId = "ovg_" + new string('a', 64) }), HttpStatusCode.NotFound);
        Assert.Equal(before, await Snapshot());
        var original = await statements.Find(new BsonDocument("_id", pending)).SingleAsync();
        foreach (var changed in new[] {
            new BsonDocument("$set", new BsonDocument("Scope", "owned-other-scope")),
            new BsonDocument("$set", new BsonDocument { { "AppId", "owned-app-a" }, { "PayerAvatarId", accounts[0] } }) })
        {
            await statements.UpdateOneAsync(new BsonDocument("_id", pending), changed);
            before = await Snapshot();
            await Send(pay, JsonSerializer.Serialize(new { statementId = pending }), HttpStatusCode.NotFound);
            Assert.Equal(before, await Snapshot());
            await statements.ReplaceOneAsync(new BsonDocument("_id", pending), original);
        }
        before = await Snapshot();
        for (var i = 0; i < 2; i++)
        {
            using var empty = JsonDocument.Parse(await Send(pay, JsonSerializer.Serialize(new {
                billingRequestId = Guid.NewGuid().ToString("D"), month
            }), HttpStatusCode.OK));
            Assert.True(empty.RootElement.GetProperty("noCharges").GetBoolean());
            Assert.Equal(before, await Snapshot());
        }
        using (var view = JsonDocument.Parse(await http.GetStringAsync("/api/subscription/overages/statements")))
        {
            Assert.Equal(2, view.RootElement.GetArrayLength());
            var allowed = new[] { "id", "month", "amountMinorUsd", "state", "paymentFailed", "hostedInvoiceUrl", "createdAtUtc" };
            foreach (var statement in view.RootElement.EnumerateArray())
                Assert.All(statement.EnumerateObject(), field => Assert.Contains(field.Name, allowed));
            Assert.DoesNotContain("owned-personal-private", view.RootElement.GetRawText());
            Assert.DoesNotContain(foreign, view.RootElement.GetRawText());
        }
        Assert.Equal(before, await Snapshot());
        Console.WriteLine("Personal billing native HTTP verified: explicit/typed/known/unique intent; malformed/mixed/foreign/unknown/app/other-scope recovery refused without collection mutation; two empty preparations make no version writes; subscriber projection excludes private routing/lease/errors and foreign records. Actual held lease created through shipping store. Statement/account INPUT fixtures only; no Stripe readiness/positive consent/nonzero payment.");
        return new(pending, leased, foreign, leaseOwner, version, hash,
            await orders.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    private static async Task VerifyPersonalBillingAfterBrowserAsync(IMongoDatabase db, PersonalBillingFixture fixture, string user)
    {
        var statements = db.GetCollection<BsonDocument>("subscription_overage_statements");
        Assert.Equal(3L, await statements.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal("paid", (await statements.Find(new BsonDocument("_id", fixture.PendingId)).SingleAsync())["State"].AsString);
        var leased = await statements.Find(new BsonDocument("_id", fixture.LeasedId)).SingleAsync();
        Assert.Equal("retry", leased["State"].AsString);
        Assert.Equal(fixture.LeaseOwner, leased["LeaseOwner"].AsString);
        Assert.Equal(1, leased["Attempts"].ToInt32());
        Assert.Equal("pending", (await statements.Find(new BsonDocument("_id", fixture.ForeignId)).SingleAsync())["State"].AsString);
        var record = await db.GetCollection<BsonDocument>("subscription_records").Find(new BsonDocument("_id", user)).SingleAsync();
        Assert.False(record["PayAsYouGoEnabled"].AsBoolean);
        Assert.Equal("owned-personal-private-customer", record["StripeCustomerId"].AsString);
        Assert.Equal("owned-personal-private-subscription", record["StripeSubscriptionId"].AsString);
        Assert.Equal(fixture.PricingVersion, record["PaidOverageTermsVersion"].AsString);
        Assert.Equal(fixture.PricingHash, record["PaidOverageTermsSha256"].AsString);
        Assert.Equal(5m, record["MonthlyOverageLimitUsd"].ToDecimal());
        Assert.Equal(fixture.OriginalOrders, await db.GetCollection<BsonDocument>("subscription_orders").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(0L, await db.GetCollection<BsonDocument>("subscription_usage_events").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(1L, await db.GetCollection<BsonDocument>("subscription_billing_audit").CountDocumentsAsync(
            new BsonDocument { { "Kind", "billing-preference" }, { "UserId", user } }));
        Console.WriteLine("Personal billing browser/native final state verified: actual opt-out preserved original terms/cap and one preference audit; original zero-due invoice paid once, genuine held collection lease/retry and foreign pending unchanged; no new statement/operation/order. Actual SDK/UI/MVC/Mongo/zero-due collector; original statement/account INPUT fixtures, not nonzero Stripe/payment/webhook/hosted proof.");
    }
}

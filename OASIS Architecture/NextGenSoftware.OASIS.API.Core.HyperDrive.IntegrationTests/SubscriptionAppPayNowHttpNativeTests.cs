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
    private static async Task VerifyAppPayNowHttpAsync(HttpClient http, Func<string, string> token,
        IMongoDatabase db, string[] accounts)
    {
        const string path = "/api/subscription/app-owner/owned-app-a/payg/pay-now";
        var month = DateTime.UtcNow.ToString("yyyy-MM");
        async Task<string> Send(string json, HttpStatusCode status)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(path, content);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == status, $"Expected {status}, got {response.StatusCode}: {body}");
            return body;
        }
        string Request(string id) => JsonSerializer.Serialize(new { billingRequestId = id, month });
        async Task<string> Snapshot()
        {
            var parts = new List<string>();
            foreach (var name in (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x))
            {
                var rows = await db.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty)
                    .Sort(new BsonDocument("_id", 1)).ToListAsync();
                parts.Add(name + ":" + string.Join("\n", rows.Select(x => x.ToJson())));
            }
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", parts))));
        }
        var requestId = Guid.NewGuid().ToString("D");
        await Send(Request(requestId), HttpStatusCode.Unauthorized);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token(accounts[0]));
        using (var empty = JsonDocument.Parse(await Send(Request(requestId), HttpStatusCode.OK)))
        {
            var result = empty.RootElement.GetProperty("result");
            Assert.Equal("owned-app-a", result.GetProperty("appId").GetString());
            Assert.True(result.GetProperty("noCharges").GetBoolean());
        }
        var before = await Snapshot();
        await Send(Request(requestId), HttpStatusCode.OK);
        Assert.Equal(before, await Snapshot());
        foreach (var invalid in new[] {
            "null", "[]", "{}", "{\"billingRequestId\":\"invalid\"}",
            "{\"billingRequestId\":\"00000000-0000-0000-0000-000000000000\"}",
            "{\"billingRequestId\":\"" + requestId + "\",\"BillingRequestId\":\"" + requestId + "\"}",
            "{\"billingRequestId\":\"" + requestId + "\",\"month\":\"invalid\"}",
            "{\"billingRequestId\":\"" + requestId + "\",\"month\":17}",
            "{\"billingRequestId\":\"" + requestId + "\",\"payerAvatarId\":\"" + accounts[1] + "\"}",
            "{\"billingRequestId\":\"" + requestId + "\",\"creditsOnly\":true}" })
            await Send(invalid, HttpStatusCode.BadRequest);
        Assert.Equal(before, await Snapshot());
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token(accounts[1]));
        await Send(Request(requestId), HttpStatusCode.Forbidden);
        Assert.Equal(before, await Snapshot());

        // Owned persisted statement INPUT FIXTURE for replay/transport only. It is not
        // operation allocation, genuine Stripe identity/readiness or external payment proof.
        var id = await SeedOwnedZeroAppStatementAsync(db, "owned-app-a", accounts[0], requestId, month);
        var statements = db.GetCollection<BsonDocument>("subscription_overage_statements");
        before = await Snapshot();
        await Send(Request(requestId), HttpStatusCode.Forbidden);
        Assert.Equal(before, await Snapshot());
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token(accounts[0]));
        using (var paid = JsonDocument.Parse(await Send(Request(requestId), HttpStatusCode.OK)))
        {
            var result = paid.RootElement.GetProperty("result");
            Assert.False(result.GetProperty("noCharges").GetBoolean());
            var view = result.GetProperty("statement");
            Assert.Equal(id, view.GetProperty("id").GetString());
            Assert.Equal("paid", view.GetProperty("state").GetString());
            Assert.Equal(0m, view.GetProperty("amountUsd").GetDecimal());
            Assert.False(view.GetProperty("paymentFailed").GetBoolean());
            var publicNames = new[] { "id", "month", "amountUsd", "state", "paymentFailed", "hostedInvoiceUrl" };
            Assert.All(view.EnumerateObject(), field => Assert.Contains(field.Name, publicNames));
            Assert.DoesNotContain("owned-private", paid.RootElement.GetRawText());
            Assert.DoesNotContain("stripe", paid.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
        }
        before = await Snapshot();
        await Send(Request(requestId), HttpStatusCode.OK);
        Assert.Equal(before, await Snapshot());
        var apps = db.GetCollection<BsonDocument>("subscription_app_accounts");
        await apps.UpdateOneAsync(new BsonDocument("_id", "owned-app-a"),
            new BsonDocument("$set", new BsonDocument("OwnerAvatarIds", new BsonArray())));
        before = await Snapshot();
        await Send(Request(requestId), HttpStatusCode.Forbidden);
        Assert.Equal(before, await Snapshot());
        Assert.Equal(1L, await statements.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal("paid", (await statements.Find(new BsonDocument("_id", id)).SingleAsync())["State"].AsString);
        Console.WriteLine("App pay-now native HTTP verified: real authenticated controller/repository/zero-due gateway-store; empty polling unchanged; strict intent validation; foreign and revoked owners cannot read/recollect replay; public statement projection excludes private IDs; zero collection replay unchanged. Persisted statement INPUT FIXTURE only, not allocation/Stripe/browser/hosted acceptance.");
    }
    private static async Task<string> SeedOwnedZeroAppStatementAsync(IMongoDatabase db, string appId,
        string actor, string requestId, string month)
    {
        var id = "ovga_" + Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {
            Scope = db.DatabaseNamespace.DatabaseName, Subject = "app:" + appId, Month = month, RequestKey = requestId
        })));
        var now = DateTime.UtcNow;
        var statements = db.GetCollection<BsonDocument>("subscription_overage_statements");
        await statements.InsertOneAsync(new BsonDocument {
            { "_id", id }, { "AppId", appId }, { "UserId", "app:" + appId },
            { "PayerAvatarId", actor }, { "Month", month }, { "Scope", db.DatabaseNamespace.DatabaseName },
            { "StripeCustomerId", "owned-private-customer-http" }, { "StripeSubscriptionId", "owned-private-subscription-http" },
            { "AppConsentIds", new BsonArray { Guid.NewGuid().ToString("D") } }, { "CreditsOnly", false },
            { "OperationIds", new BsonArray() }, { "AdjustmentIds", new BsonArray() }, { "PricingVersions", new BsonArray { "owned-input" } },
            { "RequestChargesUsd", new Decimal128(0m) }, { "AiChargesUsd", new Decimal128(0m) }, { "AmountMinorUsd", 0L },
            { "State", "pending" }, { "PaymentFailed", false }, { "CreatedAtUtc", now }, { "UpdatedAtUtc", now }, { "NextAttemptAtUtc", now }
        });
        return id;
    }

}

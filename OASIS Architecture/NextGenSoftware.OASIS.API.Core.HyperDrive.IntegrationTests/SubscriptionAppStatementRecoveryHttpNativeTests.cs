using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private static async Task VerifyAppStatementRecoveryHttpAsync(HttpClient http, Func<string, string> token,
        IMongoDatabase db, string[] accounts)
    {
        const string path = "/api/subscription/app-owner/owned-app-a/payg/pay-now";
        var month = DateTime.UtcNow.ToString("yyyy-MM");
        var id = await SeedOwnedZeroAppStatementAsync(db, "owned-app-a", accounts[0], Guid.NewGuid().ToString("D"), month);
        var foreign = await SeedOwnedZeroAppStatementAsync(db, "owned-app-b", accounts[1], Guid.NewGuid().ToString("D"), month);
        var statements = db.GetCollection<BsonDocument>("subscription_overage_statements");
        var apps = db.GetCollection<BsonDocument>("subscription_app_accounts");
        async Task<string> Snapshot()
        {
            var rows = new List<string>();
            foreach (var name in (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x))
                rows.Add(name + ":" + string.Join("\n", (await db.GetCollection<BsonDocument>(name)
                    .Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).Select(x => x.ToJson())));
            return string.Join("\n", rows);
        }
        async Task<string> Send(string json, HttpStatusCode expected)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(path, content);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == expected, $"Expected {expected}, got {response.StatusCode}: {body}");
            return body;
        }
        string Existing(string statement) => JsonSerializer.Serialize(new { statementId = statement });
        var before = await Snapshot();
        await Send(Existing(id), HttpStatusCode.Unauthorized);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token(accounts[1]));
        await Send(Existing(id), HttpStatusCode.Forbidden);
        Assert.Equal(before, await Snapshot());
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token(accounts[0]));
        await Send(Existing(foreign), HttpStatusCode.NotFound);
        await Send(Existing("ovga_" + new string('a', 64)), HttpStatusCode.NotFound);
        foreach (var invalid in new[] {
            Existing(""), Existing("ovga_invalid"),
            JsonSerializer.Serialize(new { statementId = id, month }),
            JsonSerializer.Serialize(new { statementId = id, billingRequestId = Guid.NewGuid().ToString("D") }),
            "{\"statementId\":\"" + id + "\",\"StatementId\":\"" + id + "\"}" })
            await Send(invalid, HttpStatusCode.BadRequest);
        Assert.Equal(before, await Snapshot());
        using (var result = JsonDocument.Parse(await Send(Existing(id), HttpStatusCode.OK)))
        {
            var view = result.RootElement.GetProperty("result").GetProperty("statement");
            Assert.Equal(id, view.GetProperty("id").GetString());
            Assert.Equal(month, view.GetProperty("month").GetString());
            Assert.Equal("paid", view.GetProperty("state").GetString());
            Assert.Equal(0m, view.GetProperty("amountUsd").GetDecimal());
            Assert.DoesNotContain("owned-private", result.RootElement.GetRawText());
            Assert.DoesNotContain("stripe", result.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
        }
        Assert.Equal(2L, await statements.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal("pending", (await statements.Find(new BsonDocument("_id", foreign)).SingleAsync())["State"].AsString);
        before = await Snapshot();
        await Send(Existing(id), HttpStatusCode.OK);
        Assert.Equal(before, await Snapshot());
        await apps.UpdateOneAsync(new BsonDocument("_id", "owned-app-a"),
            new BsonDocument("$set", new BsonDocument("OwnerAvatarIds", new BsonArray())));
        before = await Snapshot();
        await Send(Existing(id), HttpStatusCode.Forbidden);
        Assert.Equal(before, await Snapshot());
        Assert.Equal(0L, await db.GetCollection<BsonDocument>("subscription_usage_events").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Console.WriteLine("App statement recovery native HTTP verified: explicit existing-statement collection uses original statement without new allocation; real zero-due gateway/store; private IDs redacted; duplicate/ambiguous/repriced intent, unknown/cross-app statement and foreign/revoked owner refused without mutation; paid retry unchanged. Owned statement INPUT fixtures, not genuine Stripe/payment/webhook/browser/hosted proof.");
    }
}

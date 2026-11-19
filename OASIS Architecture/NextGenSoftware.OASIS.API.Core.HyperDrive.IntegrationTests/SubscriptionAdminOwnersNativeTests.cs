using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private sealed record SubscriptionAdminOwnersFixture(string FinancialState, BsonDocument App,
        BsonDocument ForeignApp, Dictionary<string, string> AuditState);

    private static async Task<SubscriptionAdminOwnersFixture> PrepareSubscriptionAdminOwnersAsync(
        IMongoDatabase db, object repository, string[] accounts)
    {
        var apps = db.GetCollection<BsonDocument>("subscription_app_accounts");
        Assert.Equal(0L, await apps.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        foreach (var index in Enumerable.Range(0, accounts.Length))
        {
            var appId = index == 0 ? "owned-app-a" : "owned-app-b";
            // Native setup uses the shipping creation and owner transaction, not partial stored-document substitutes.
            await (Task)repository.GetType().GetMethod("CreateAppAccountAsync")!.Invoke(repository,
                [appId, "Owned owner fixture " + index, "silver", accounts[0], "Owned owner fixture creation", true, "bronze", CancellationToken.None])!;
            await (Task)repository.GetType().GetMethod("SetAppOwnersAsync")!.Invoke(repository,
                [appId, new List<string> { accounts[index] }, accounts[0], "Owned owner fixture initialization", CancellationToken.None])!;
        }
        var stored = await apps.Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync();
        Assert.Equal(2, stored.Count);
        var audit = await db.GetCollection<BsonDocument>("subscription_billing_audit")
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        return new(await SubscriptionAdminFinancialStateAsync(db, "subscription_billing_audit"), stored[0], stored[1],
            audit.ToDictionary(row => row["_id"].AsString, row => row.ToJson()));
    }

    private static async Task VerifySubscriptionAdminOwnersAsync(IMongoDatabase db, SubscriptionAdminOwnersFixture before, string[] accounts)
    {
        var apps = await db.GetCollection<BsonDocument>("subscription_app_accounts")
            .Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync();
        Assert.Equal(2, apps.Count);
        Assert.True(before.ForeignApp.ToJson() == apps[1].ToJson(), "Foreign app state must remain unchanged.");
        Assert.Equal(new[] { accounts[1] }, apps[0]["OwnerAvatarIds"].AsBsonArray.Select(value => value.AsString).ToArray());
        Assert.Equal(accounts[0], apps[0]["UpdatedBy"].AsString);
        Assert.Equal("Owned browser late owner assignment", apps[0]["Reason"].AsString);
        BsonDocument Unchanged(BsonDocument row)
        {
            var copy = row.DeepClone().AsBsonDocument;
            foreach (var name in new[] { "OwnerAvatarIds", "UpdatedAtUtc", "UpdatedBy", "Reason" }) copy.Remove(name);
            return copy;
        }
        Assert.True(Unchanged(before.App).ToJson() == Unchanged(apps[0]).ToJson(), "Non-owner account fields and API-key hash must remain unchanged.");
        var audit = await db.GetCollection<BsonDocument>("subscription_billing_audit")
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        Assert.Equal(before.AuditState.Count + 4, audit.Count);
        foreach (var original in before.AuditState)
            Assert.True(original.Value == Assert.Single(audit.Where(row => row["_id"].AsString == original.Key)).ToJson(), "Original audit records must remain unchanged.");
        var added = audit.Where(row => !before.AuditState.ContainsKey(row["_id"].AsString)).ToList();
        var expected = new Dictionary<string, (string Previous, string[] Owners)>
        {
            ["Owned browser owner assignment"] = (accounts[0], [accounts[1]]),
            ["Owned browser canonical duplicate assignment"] = (accounts[1], [accounts[1]]),
            ["Owned browser owner removal"] = (accounts[1], []),
            ["Owned browser late owner assignment"] = ("", [accounts[1]])
        };
        foreach (var row in added)
        {
            Assert.Equal("app-account-owners-set", row["Kind"].AsString);
            Assert.Equal("app:owned-app-a", row["UserId"].AsString);
            using var payload = JsonDocument.Parse(row["PayloadJson"].AsString);
            var value = payload.RootElement;
            var reason = value.GetProperty("Reason").GetString()!;
            Assert.True(expected.Remove(reason, out var transition), "Every added audit must match exactly one original requested transition.");
            Assert.Equal(accounts[0], value.GetProperty("Actor").GetString());
            Assert.Equal("owned-app-a", value.GetProperty("AppId").GetString());
            Assert.Equal(transition.Previous, value.GetProperty("PreviousOwners").GetString());
            Assert.Equal(transition.Owners, value.GetProperty("Owners").EnumerateArray().Select(id => id.GetString()).ToArray());
            Assert.False(value.TryGetProperty("ApiKey", out _));
            Assert.False(value.TryGetProperty("KeyHashSha256", out _));
        }
        Assert.Empty(expected);
        Assert.True(before.FinancialState == await SubscriptionAdminFinancialStateAsync(db, "subscription_billing_audit"), "Seven other financial collections must remain unchanged.");
        Console.WriteLine("Subscription admin owners native final state verified: actual canonical owner assignment/removal/late assignment, four exact original-Wizard/previous-owner/reason audit transitions, canonical UUID uniqueness, original audits and seven other financial collections unchanged, foreign app and all non-owner account settings/API-key hashes unchanged. No hosted/full portal/Stripe acceptance.");
    }
}

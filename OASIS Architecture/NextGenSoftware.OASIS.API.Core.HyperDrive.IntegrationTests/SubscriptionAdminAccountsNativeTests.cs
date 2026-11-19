using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private sealed class SubscriptionAdminAccountsFixture(string financialState, long auditCount)
    {
        public string FinancialState { get; } = financialState;
        public long AuditCount { get; } = auditCount;
        public int CredentialReads;
    }
    private sealed record OwnedAppCredentialInput(string AppId, string ApiKey);

    private static async Task<SubscriptionAdminAccountsFixture> PrepareSubscriptionAdminAccountsAsync(
        WebApplication app, IMongoDatabase db, object repository, string control)
    {
        Assert.Equal(0L, await db.GetCollection<BsonDocument>("subscription_app_accounts").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var fixture = new SubscriptionAdminAccountsFixture(await SubscriptionAdminFinancialStateAsync(db,
            "subscription_billing_audit"), await db.GetCollection<BsonDocument>("subscription_billing_audit")
            .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        // Test-assembly-only bounded, nonce-guarded observation of the actual repository's credential reader.
        // Business create/update/rotation use the shipping administrator controller, never this observer.
        app.MapPost("/__owned-app-credential", async (HttpContext context) =>
        {
            if (context.Request.Headers["X-Owned-App-Control"].ToString() != control)
                return Results.StatusCode(403);
            if (Interlocked.Increment(ref fixture.CredentialReads) > 7) return Results.StatusCode(409);
            var input = await context.Request.ReadFromJsonAsync<OwnedAppCredentialInput>();
            if (input == null || input.AppId != "owned-admin-browser" || string.IsNullOrEmpty(input.ApiKey) || input.ApiKey.Length > 128)
                return Results.BadRequest();
            var task = (Task)repository.GetType().GetMethod("VerifyAppCredentialAsync")!
                .Invoke(repository, [input.AppId, input.ApiKey, context.RequestAborted])!;
            await task;
            var accepted = task.GetType().GetProperty("Result")!.GetValue(task) != null;
            return (IResult)Results.Ok(new { accepted });
        });
        return fixture;
    }

    private static async Task VerifySubscriptionAdminAccountsAsync(IMongoDatabase db, SubscriptionAdminAccountsFixture before, string actor)
    {
        Assert.Equal(7, before.CredentialReads);
        var row = Assert.Single(await db.GetCollection<BsonDocument>("subscription_app_accounts")
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync());
        Assert.Equal("owned-admin-browser", row["_id"].AsString);
        Assert.Equal("silver", row["PlanId"].AsString);
        Assert.Equal("active", row["Status"].AsString);
        Assert.True(row["PooledQuota"].AsBoolean);
        Assert.Equal("bronze", row["PerUserPlanId"].AsString);
        Assert.Equal(actor, row["CreatedBy"].AsString);
        Assert.Equal(actor, row["UpdatedBy"].AsString);
        Assert.Equal(64, row["KeyHashSha256"].AsString.Length);
        var audit = db.GetCollection<BsonDocument>("subscription_billing_audit");
        Assert.Equal(before.AuditCount + 6, await audit.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var entries = await audit.Find(new BsonDocument("UserId", "app:owned-admin-browser")).ToListAsync();
        Assert.Equal(6, entries.Count);
        Assert.Single(entries.Where(x => x["Kind"].AsString == "app-account-created"));
        Assert.Equal(2, entries.Count(x => x["Kind"].AsString == "app-account-key-rotated"));
        Assert.Equal(3, entries.Count(x => x["Kind"].AsString == "app-account-updated"));
        foreach (var entry in entries)
        {
            using var payload = JsonDocument.Parse(entry["PayloadJson"].AsString);
            Assert.Equal(actor, payload.RootElement.GetProperty("Actor").GetString());
            Assert.StartsWith("Owned browser ", payload.RootElement.GetProperty("Reason").GetString());
            Assert.False(payload.RootElement.TryGetProperty("ApiKey", out _));
            Assert.False(payload.RootElement.TryGetProperty("KeyHashSha256", out _));
        }
        Assert.Equal(before.FinancialState, await SubscriptionAdminFinancialStateAsync(db, "subscription_billing_audit"));
        Console.WriteLine("Subscription admin app accounts native final state verified: one real active pooled silver account with bronze per-user plan, six exact administrator audit transitions, seven actual repository credential reads including rotation and disabled rejection; other financial collections unchanged. Keys never included in returned browser evidence or audit payload. No hosted/full usage-admission/Stripe acceptance.");
    }
}

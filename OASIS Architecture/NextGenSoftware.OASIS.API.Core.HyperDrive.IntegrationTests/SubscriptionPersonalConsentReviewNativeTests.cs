using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private sealed record PersonalConsentReviewFixture(string Version, string Hash, long Orders,
        BsonDocument OtherAccount, Func<int> Phase);

    private static async Task<PersonalConsentReviewFixture> PreparePersonalConsentReviewBrowserAsync(WebApplication app,
        IMongoDatabase db, Assembly api, object repository, string[] accounts, string control)
    {
        const string ns = "NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.";
        var prices = api.GetType(ns + "PaidOveragePricing", true)!;
        var pricing = prices.GetMethod("Load")!.Invoke(null, null)!;
        var version = (string)prices.GetProperty("Version")!.GetValue(pricing)!;
        var hash = (string)prices.GetProperty("PricingSha256")!.GetValue(pricing)!;
        var records = db.GetCollection<BsonDocument>("subscription_records");
        // Original account/readiness INPUTS, not Stripe-issued identity or remote payment verification.
        await records.UpdateOneAsync(new BsonDocument("_id", accounts[0]), new BsonDocument("$set", new BsonDocument {
            { "StripeCustomerId", "owned-consent-review-private-customer" }, { "StripeSubscriptionId", "owned-consent-review-private-subscription" },
            { "PayAsYouGoEnabled", true }, { "MonthlyOverageLimitUsd", new Decimal128(5m) },
            { "PaidOverageTermsVersion", version }, { "PaidOverageTermsSha256", hash }, { "PaidOverageTermsAcceptedAtUtc", DateTime.UtcNow }
        }));
        var other = await records.Find(new BsonDocument("_id", accounts[1])).SingleAsync();
        var orders = await db.GetCollection<BsonDocument>("subscription_orders").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        var phase = 0;
        app.MapPost("/__owned-consent-review/{stage}", async (HttpContext context, string stage) => {
            if (context.Request.Headers["X-Owned-Consent-Review-Control"].ToString() != control) return Results.StatusCode(403);
            var expected = stage switch { "bind" => 0, "stale" => 1, "restore" => 2, "stale-hash" => 3, "restore-hash" => 4, _ => -1 };
            if (expected < 0 || Interlocked.CompareExchange(ref phase, expected + 1, expected) != expected) return Results.StatusCode(409);
            if (expected == 0) {
                var task = (Task)repository.GetType().GetMethod("GetSubscriptionAsync")!.Invoke(repository, [accounts[0], CancellationToken.None])!;
                await task;
                var record = task.GetType().GetProperty("Result")!.GetValue(task)!;
                var snapshot = api.GetType(ns + "PersonalBillingPayerSnapshot", true)!.GetMethod("AfterVerification")!
                    .Invoke(null, [record, DateTime.UtcNow])!;
                await (Task)repository.GetType().GetMethod("SetPaidOverageConsentAsync")!.Invoke(repository,
                    [accounts[0], snapshot, version, hash, 5m, CancellationToken.None])!;
            } else {
                var field = expected <= 2 ? "PaidOverageTermsVersion" : "PaidOverageTermsSha256";
                var current = expected <= 2 ? version : hash;
                var stale = expected <= 2 ? "owned-stale-pricing-input" : new string('f', 64);
                var from = expected % 2 == 1 ? current : stale;
                var to = expected % 2 == 1 ? stale : current;
                var changed = await records.UpdateOneAsync(new BsonDocument { { "_id", accounts[0] }, { field, from } },
                    new BsonDocument("$set", new BsonDocument(field, to)));
                if (changed.MatchedCount != 1) return Results.StatusCode(409);
            }
            return (IResult)Results.Ok(new { stage });
        });
        return new(version, hash, orders, other, () => Volatile.Read(ref phase));
    }

    private static async Task VerifyPersonalConsentReviewAfterBrowserAsync(IMongoDatabase db, PersonalConsentReviewFixture fixture, string[] accounts)
    {
        Assert.Equal(5, fixture.Phase());
        var records = db.GetCollection<BsonDocument>("subscription_records");
        var current = await records.Find(new BsonDocument("_id", accounts[0])).SingleAsync();
        Assert.False(current["PayAsYouGoEnabled"].AsBoolean);
        Assert.Equal("owned-consent-review-private-customer", current["StripeCustomerId"].AsString);
        Assert.Equal("owned-consent-review-private-subscription", current["StripeSubscriptionId"].AsString);
        Assert.Equal(5m, current["MonthlyOverageLimitUsd"].ToDecimal());
        Assert.Equal(fixture.Version, current["PaidOverageTermsVersion"].AsString);
        Assert.Equal(fixture.Hash, current["PaidOverageTermsSha256"].AsString);
        Assert.True(current["PaidOveragePreferenceVersion"].ToInt64() > current["PaidOverageConsentPreferenceVersion"].ToInt64());
        var audit = db.GetCollection<BsonDocument>("subscription_billing_audit");
        Assert.Equal(1L, await audit.CountDocumentsAsync(new BsonDocument { { "UserId", accounts[0] }, { "Kind", "overage-consent" } }));
        Assert.Equal(1L, await audit.CountDocumentsAsync(new BsonDocument { { "UserId", accounts[0] }, { "Kind", "billing-preference" } }));
        Assert.Equal(1L, await audit.CountDocumentsAsync(new BsonDocument("_id", "personal-overage-consent:" + current["PaidOverageConsentId"].AsString)));
        Assert.Equal(fixture.OtherAccount.ToJson(), (await records.Find(new BsonDocument("_id", accounts[1])).SingleAsync()).ToJson());
        Assert.Equal(fixture.Orders, await db.GetCollection<BsonDocument>("subscription_orders").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(0L, await db.GetCollection<BsonDocument>("subscription_usage_events").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Assert.Equal(0L, await db.GetCollection<BsonDocument>("subscription_overage_statements").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Console.WriteLine("Personal consent review native final state verified: one protected original consent and one real explicit opt-out; pricing input restored, original private routing/cap retained, foreign account unchanged, no operation/statement/order allocation. Account/readiness INPUTS only, not genuine Stripe verification/payment, production Startup, hosted or full portal acceptance.");
    }
}

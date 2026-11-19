using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public Task SubscriptionPublicAdmissionRejectsInvalidRotatedAndDisabledAppCredentials() =>
        RunSubscriptionHttp(false, usage: true, ownerPayNow: true, appAdmission: true);

    private static async Task VerifyAppCredentialAdmissionAsync(HttpClient http, Func<string, string> token,
        IMongoDatabase db, string[] accounts, object repository, string serviceKey)
    {
        const string appId = "owned-admission-app";
        async Task<object> Call(string method, params object?[] args)
        {
            var task = (Task)repository.GetType().GetMethod(method)!.Invoke(repository, args)!;
            await task;
            return task.GetType().GetProperty("Result")!.GetValue(task)!;
        }
        var created = await Call("CreateAppAccountAsync", appId, "Owned public admission", "bronze",
            accounts[0], "Owned credential admission input", true, "bronze", CancellationToken.None);
        var originalKey = (string)created.GetType().GetField("Item2")!.GetValue(created)!;
        async Task<string> Snapshot()
        {
            var rows = new List<string>();
            foreach (var name in (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x))
                rows.Add(name + ":" + string.Join("\n", (await db.GetCollection<BsonDocument>(name)
                    .Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).Select(x => x.ToJson())));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", rows))));
        }
        var calls = 0;
        async Task Refuse(string? id, string? key)
        {
            var before = await Snapshot();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/subscription/usage/authorize");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token(accounts[0]));
            request.Headers.Add("X-OASIS-Service", "WEB5");
            request.Headers.Add("X-OASIS-Service-Key", serviceKey);
            if (id != null) request.Headers.Add("X-OASIS-App-Id", id);
            if (key != null) request.Headers.Add("X-OASIS-App-Key", key);
            request.Content = JsonContent.Create(new {
                operationId = Guid.NewGuid().ToString("D"), consumingService = "WEB5",
                endpoint = "/owned-app-credential-refusal", meterCategory = "api.request",
                requestFingerprint = new string('a', 64), requestedUnits = 1, estimatedCostUsd = 0m
            });
            using var response = await http.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);
            Assert.True(json.RootElement.GetProperty("isError").GetBoolean());
            Assert.Equal("APP_CREDENTIAL_INVALID", json.RootElement.GetProperty("code").GetString());
            Assert.DoesNotContain(originalKey, body);
            if (!string.IsNullOrEmpty(key)) Assert.DoesNotContain(key, body);
            Assert.Equal(before, await Snapshot());
            calls++;
        }
        await Refuse(appId, null);
        await Refuse(null, originalKey);
        await Refuse(appId, "owned-incorrect-key");
        await Refuse("owned-missing-app", originalKey);
        await Refuse("invalid app id", originalKey);
        var rotated = await Call("RotateAppAccountKeyAsync", appId, accounts[0], "Owned revoked key input", CancellationToken.None);
        var currentKey = (string)rotated.GetType().GetField("Item2")!.GetValue(rotated)!;
        Assert.NotEqual(originalKey, currentKey);
        await Refuse(appId, originalKey);
        await Call("UpdateAppAccountAsync", appId, null, "disabled", accounts[0], "Owned disabled account input", null, null, CancellationToken.None);
        await Refuse(appId, currentKey);
        Assert.Equal(7, calls);
        Assert.Equal(0L, await db.GetCollection<BsonDocument>("subscription_usage_events").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        Console.WriteLine("Public app credential admission verified: seven actual authenticated controller refusals for partial, incorrect, missing, malformed, rotated-old and disabled credentials; exact APP_CREDENTIAL_INVALID401 and unchanged native database per request, zero reservations. Setup used shipping transactional account create/rotate/disable. No mocked business response, successful-admission rerun, provider execution or Stripe/hosted/full portal acceptance.");
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private sealed class SubscriptionSettingsFixture : IAsyncDisposable
    {
        private readonly MongoClient _client=new(MongoUri);
        private readonly string _name="subscription_settings_"+Guid.NewGuid().ToString("N");
        public IMongoDatabase Database=>_client.GetDatabase(_name);
        public IMongoCollection<BsonDocument> Records=>Database.GetCollection<BsonDocument>("subscription_records");
        public ISubscriptionSettingsAuthority Authority {get;private set;}=null!;
        public Guid ForeignId {get;}=Guid.NewGuid();
        private BsonDocument? _foreignBefore;

        public static async Task<SubscriptionSettingsFixture> Create(Assembly api)
        {
            if(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI")!=MongoUri)
                throw new InvalidOperationException("The exact owned loopback Mongo fixture is required.");
            var fixture=new SubscriptionSettingsFixture();
            Assert.DoesNotContain(fixture._name,await(await fixture._client.ListDatabaseNamesAsync()).ToListAsync());
            try
            {
                const string ns="NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.";
                var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
                    ["SUBSCRIPTION_MONGODB_CONNECTION_STRING"]=MongoUri,
                    ["SUBSCRIPTION_MONGODB_DATABASE"]=fixture._name,
                    ["SUBSCRIPTION_LEDGER_INITIAL_STATE"]="empty-new-installation"
                }).Build();
                var repository=Activator.CreateInstance(api.GetType(ns+"MongoSubscriptionUsageRepository",true)!,[config])!;
                fixture.Authority=(ISubscriptionSettingsAuthority)Activator.CreateInstance(
                    api.GetType(ns+"SubscriptionSettingsAuthority",true)!,[repository,repository])!;
                // Establish the empty ledger through its shipping free-plan transaction before billing inputs.
                var recordType=api.GetType(ns+"SubscriptionRecord",true)!;
                var initial=Activator.CreateInstance(recordType)!;
                recordType.GetProperty("UserId")!.SetValue(initial,Guid.NewGuid().ToString("D"));
                recordType.GetProperty("PlanId")!.SetValue(initial,"free");
                recordType.GetProperty("Status")!.SetValue(initial,"active");
                recordType.GetProperty("CurrentPeriodEnd")!.SetValue(initial,DateTime.UtcNow.AddDays(1));
                await (Task)repository.GetType().GetMethod("SaveSubscriptionAsync")!
                    .Invoke(repository,[initial,CancellationToken.None])!;
                Assert.Equal(1,await fixture.Database.GetCollection<BsonDocument>("subscription_usage_migrations")
                    .CountDocumentsAsync(new BsonDocument("_id","v1-opening-balance")));
                var foreign=new BsonDocument {{"_id",fixture.ForeignId.ToString("D")},{"PlanId","gold"},
                    {"Status","active"},{"PayAsYouGoEnabled",false},{"UpdatedAt",DateTime.UtcNow}};
                await fixture.Records.InsertOneAsync(foreign);
                fixture._foreignBefore=await fixture.Records.Find(new BsonDocument("_id",fixture.ForeignId.ToString("D"))).SingleAsync();
                return fixture;
            }
            catch {await fixture.DisposeAsync();throw;}
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if(_foreignBefore!=null)
                    Assert.Equal(_foreignBefore,await Records.Find(new BsonDocument("_id",ForeignId.ToString("D"))).SingleAsync());
            }
            finally
            {
                Assert.StartsWith("subscription_settings_",_name);
                await _client.DropDatabaseAsync(_name);
                Assert.DoesNotContain(_name,await(await _client.ListDatabaseNamesAsync()).ToListAsync());
                Console.WriteLine("Owned subscription settings database absence verified; foreign billing record preserved.");
            }
        }
    }

    private static async Task VerifySubscriptionSettingsHttp(HttpClient client,SubscriptionSettingsFixture ledger,
        IOASISStorageProvider store,Guid avatarId)
    {
        const string route="/api/settings/subscription-settings";
        const string aggregateRoute="/api/settings/get-all-settings-for-current-logged-in-avatar";
        var owner=avatarId.ToString("D");
        var filter=new BsonDocument("_id",owner);
        await ledger.Records.DeleteOneAsync(filter);
        var before=await Read(store,avatarId);
        var account=await store.LoadAvatarAsync(avatarId);
        Assert.False(account.IsError,account.Message);
        var metadata=StoredRecordJson.CaptureRuntimeToken(account.Result.MetaData).ToString();
        var password=account.Result.Password;
        async Task<JsonDocument> Get(string url)
        {
            using var response=await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK,response.StatusCode);
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }
        async Task Refuse(object update,string code)
        {
            using var response=await client.PutAsJsonAsync(route,update);
            Assert.Equal(HttpStatusCode.OK,response.StatusCode);
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.GetProperty("isError").GetBoolean());
            Assert.Equal(code,json.RootElement.GetProperty("errorCode").GetString());
        }
        using(var absent=await Get(route))
        {
            Assert.False(absent.RootElement.GetProperty("isError").GetBoolean());
            var settings=absent.RootElement.GetProperty("result");
            Assert.False(settings.GetProperty("hasSubscription").GetBoolean());
            Assert.False(settings.GetProperty("isActive").GetBoolean());
            Assert.Equal(JsonValueKind.Null,settings.GetProperty("currentPlan").ValueKind);
            Assert.Equal(JsonValueKind.Null,settings.GetProperty("expiresAt").ValueKind);
            Assert.False(settings.TryGetProperty("paymentMethod",out _));
            Assert.False(settings.TryGetProperty("maxRequestsPerMonth",out _));
        }
        Assert.Equal(0,await ledger.Records.CountDocumentsAsync(filter));
        await Refuse(new {currentPlan="enterprise",isActive=true},"SUBSCRIPTION_SETTINGS_INVALID");
        await Refuse(new {payAsYouGoEnabled="false"},"SUBSCRIPTION_SETTINGS_INVALID");
        await Refuse(new {payAsYouGoEnabled=true},"SUBSCRIPTION_CONSENT_REQUIRED");
        await Refuse(new {payAsYouGoEnabled=false},"SUBSCRIPTION_REQUIRED");
        Assert.Equal(0,await ledger.Records.CountDocumentsAsync(filter));

        // Native billing INPUT, not Stripe/readiness/payment acceptance. No fake HTTP or SDK replies.
        var expires=DateTime.UtcNow.AddDays(2);
        await ledger.Records.InsertOneAsync(new BsonDocument {{"_id",owner},{"PlanId","silver"},
            {"Status","active"},{"PayAsYouGoEnabled",true},{"PaidOveragePreferenceVersion",4L},
            {"CurrentPeriodEnd",expires},{"UpdatedAt",DateTime.UtcNow}});
        var nativeBefore=await ledger.Records.Find(filter).SingleAsync();
        using(var current=await Get(route))
        {
            Assert.False(current.RootElement.GetProperty("isError").GetBoolean());
            var settings=current.RootElement.GetProperty("result");
            Assert.True(settings.GetProperty("hasSubscription").GetBoolean());
            Assert.True(settings.GetProperty("isActive").GetBoolean());
            Assert.Equal("silver",settings.GetProperty("currentPlan").GetString());
            Assert.Equal(3000000,settings.GetProperty("maxRequestsPerMonth").GetInt32());
            Assert.Equal(10,settings.GetProperty("maxStorageGB").GetInt32());
            Assert.True(settings.GetProperty("payAsYouGoEnabled").GetBoolean());
            Assert.Equal(0,settings.GetProperty("requestsUsedThisMonth").GetInt64());
            Assert.False(settings.TryGetProperty("paymentMethod",out _));
            Assert.False(settings.TryGetProperty("maxAvatars",out _));
        }
        await Refuse(new {payAsYouGoEnabled=true},"SUBSCRIPTION_CONSENT_REQUIRED");
        await Refuse(new {payAsYouGoEnabled=false,currentPlan="gold"},"SUBSCRIPTION_SETTINGS_INVALID");
        await Refuse(new {avatarId=ledger.ForeignId,payAsYouGoEnabled=false},"SUBSCRIPTION_SETTINGS_INVALID");
        using(var scoped=await Get(route+"?avatarId="+ledger.ForeignId.ToString("D")))
            Assert.Equal("silver",scoped.RootElement.GetProperty("result").GetProperty("currentPlan").GetString());
        Assert.Equal(nativeBefore,await ledger.Records.Find(filter).SingleAsync());
        var audit=ledger.Database.GetCollection<BsonDocument>("subscription_billing_audit");
        var auditFilter=new BsonDocument {{"UserId",owner},{"Kind","billing-preference"}};
        var previousAudit=await audit.CountDocumentsAsync(auditFilter);
        using(var response=await client.PutAsJsonAsync(route,new {payAsYouGoEnabled=false}))
        using(var saved=JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
        {
            Assert.False(saved.RootElement.GetProperty("isError").GetBoolean());
            Assert.True(saved.RootElement.GetProperty("result").GetBoolean());
            Assert.True(saved.RootElement.GetProperty("isSaved").GetBoolean());
        }
        var disabled=await ledger.Records.Find(filter).SingleAsync();
        Assert.False(disabled["PayAsYouGoEnabled"].AsBoolean);
        Assert.Equal(5,disabled["PaidOveragePreferenceVersion"].ToInt64());
        Assert.Equal("silver",disabled["PlanId"].AsString);
        Assert.Equal(nativeBefore["CurrentPeriodEnd"],disabled["CurrentPeriodEnd"]);
        Assert.Equal(previousAudit+1,await audit.CountDocumentsAsync(auditFilter));
        using(var all=await Get(aggregateRoute))
        {
            Assert.False(all.RootElement.GetProperty("isError").GetBoolean());
            var subscription=all.RootElement.GetProperty("result").GetProperty("subscription");
            Assert.Equal("silver",subscription.GetProperty("currentPlan").GetString());
            Assert.False(subscription.GetProperty("payAsYouGoEnabled").GetBoolean());
        }
        await ledger.Records.UpdateOneAsync(filter,new BsonDocument("$set",new BsonDocument("CurrentPeriodEnd",DateTime.UtcNow.AddDays(-1))));
        using(var expired=await Get(route))
            Assert.False(expired.RootElement.GetProperty("result").GetProperty("isActive").GetBoolean());
        await ledger.Records.UpdateOneAsync(filter,new BsonDocument("$set",new BsonDocument("PlanId","owned-unknown-plan")));
        using(var invalid=await Get(aggregateRoute))
        {
            Assert.True(invalid.RootElement.GetProperty("isError").GetBoolean());
            Assert.Equal("SUBSCRIPTION_PLAN_UNKNOWN",invalid.RootElement.GetProperty("errorCode").GetString());
            Assert.DoesNotContain("using test data",invalid.RootElement.GetProperty("message").GetString());
        }
        var currentProfile=await Read(store,avatarId);
        SameRevisions(before,currentProfile);
        Assert.Equal((before.AvatarId,before.Title,before.FirstName,before.LastName,before.Username,
                before.Email,before.Address,before.PortraitSha256),
            (currentProfile.AvatarId,currentProfile.Title,currentProfile.FirstName,currentProfile.LastName,currentProfile.Username,
                currentProfile.Email,currentProfile.Address,currentProfile.PortraitSha256));
        var after=await store.LoadAvatarAsync(avatarId);
        Assert.False(after.IsError,after.Message);
        Assert.Equal(password,after.Result.Password);
        Assert.Equal(metadata,StoredRecordJson.CaptureRuntimeToken(after.Result.MetaData).ToString());
        Console.WriteLine("Canonical subscription settings HTTP/native lifecycle verified: "+store.ProviderType.Value+
            "; no absent-plan fabrication, exact catalogue quotas, entitlement/consent refusals, one real billing opt-out audit/version, expired state and aggregate errors, avatar fields unchanged.");
    }
}

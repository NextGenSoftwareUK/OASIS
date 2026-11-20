using System.Net;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private static async Task VerifyAggregateSnapshotHttp(HttpClient client,SubscriptionSettingsFixture ledger,
        IOASISStorageProvider store,AvatarManager avatars,MongoStore mongo,Guid id)
    {
        var language=store.ProviderType.Value switch {
            ProviderType.MongoDBOASIS=>"en",ProviderType.SQLLiteDBOASIS=>"fr",ProviderType.Neo4jOASIS=>"es",
            _=>throw new InvalidOperationException("Unexpected owned provider.") };
        var loaded=await store.LoadAvatarAsync(id);
        Assert.False(loaded.IsError,loaded.Message);
        loaded.Result.MetaData=new Dictionary<string,object> { ["preferences"]=new Dictionary<string,object> {
            ["hyperDrive"]=new Dictionary<string,object>{{"costOptimization",true},{"preferredProvider",store.ProviderType.Value.ToString()}},
            ["notifications"]=new Dictionary<string,object>{{"emailNotifications",false}},
            ["privacy"]=new Dictionary<string,object>{{"profileVisibility","private"}},
            ["system"]=new Dictionary<string,object>{{"language",language},{"theme","light"}}
        }};
        var saved=await avatars.SaveAvatarAsync(loaded.Result);
        Assert.False(saved.IsError,saved.Message);Assert.True(saved.IsSaved);
        var filter=new BsonDocument("_id",id.ToString("D"));
        await ledger.Records.DeleteOneAsync(filter);
        await ledger.Records.InsertOneAsync(new BsonDocument {{"_id",id.ToString("D")},{"PlanId","silver"},
            {"Status","active"},{"PayAsYouGoEnabled",false},{"CurrentPeriodEnd",DateTime.UtcNow.AddDays(1)},
            {"UpdatedAt",DateTime.UtcNow}});
        var before=await Read(store,id);
        var accountBefore=await store.LoadAvatarAsync(id);
        Assert.False(accountBefore.IsError,accountBefore.Message);
        var metadata=StoredRecordJson.CaptureRuntimeToken(accountBefore.Result.MetaData).ToString();
        var password=accountBefore.Result.Password;
        await mongo.SetAvatarReadProfilingAsync(true);
        long count;
        try
        {
            var start=await mongo.AvatarReadCommandCountAsync();
            using var response=await client.GetAsync("/api/settings/get-all-settings-for-current-logged-in-avatar");
            Assert.Equal(HttpStatusCode.OK,response.StatusCode);
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.False(json.RootElement.GetProperty("isError").GetBoolean());
            var settings=json.RootElement.GetProperty("result");
            Assert.Equal(id,settings.GetProperty("avatarId").GetGuid());
            Assert.True(settings.GetProperty("hyperDrive").GetProperty("costOptimization").GetBoolean());
            Assert.Equal(store.ProviderType.Value.ToString(),settings.GetProperty("hyperDrive").GetProperty("preferredProvider").GetString());
            Assert.False(settings.GetProperty("notifications").GetProperty("emailNotifications").GetBoolean());
            Assert.Equal("private",settings.GetProperty("privacy").GetProperty("profileVisibility").GetString());
            Assert.Equal(language,settings.GetProperty("system").GetProperty("language").GetString());
            Assert.Equal("light",settings.GetProperty("system").GetProperty("theme").GetString());
            Assert.Equal("silver",settings.GetProperty("subscription").GetProperty("currentPlan").GetString());
            count=await mongo.AvatarReadCommandCountAsync()-start;
            // The host authenticates through Mongo once; only a Mongo preference binding adds another avatar read.
            Assert.Equal(store.ProviderType.Value==ProviderType.MongoDBOASIS?2L:1L,count);
        }
        finally {await mongo.SetAvatarReadProfilingAsync(false);}
        var after=await Read(store,id);SameRevisions(before,after);
        Assert.Equal((before.Title,before.FirstName,before.LastName,before.Username,before.Email,before.Address),
            (after.Title,after.FirstName,after.LastName,after.Username,after.Email,after.Address));
        var accountAfter=await store.LoadAvatarAsync(id);
        Assert.False(accountAfter.IsError,accountAfter.Message);
        Assert.Equal(password,accountAfter.Result.Password);
        Assert.Equal(metadata,StoredRecordJson.CaptureRuntimeToken(accountAfter.Result.MetaData).ToString());
        Console.WriteLine("Aggregate native snapshot verified: "+store.ProviderType.Value+
            "; selected persisted categories and canonical billing, Mongo avatar read commands="+count+
            ", profile/revisions/password/metadata unchanged.");
    }
}

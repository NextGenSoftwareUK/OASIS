using Neo4j.Driver;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Providers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class AvatarProfileAtomicNeoNativeTests
{
    private sealed class OwnedStore:IAsyncDisposable
    {
        public const string Uri="bolt://127.0.0.1:27687";
        private const string Password="oasis-profile-fixture-only";
        public IDriver Driver {get;}
        public Neo4jOASIS Provider {get;}
        private readonly List<string> _ids=[];
        public OwnedStore(bool history=false)
        {
            if(Environment.GetEnvironmentVariable("OASIS_PROFILE_NEO4J_FIXTURE")!="owned-local-27687")
                throw new InvalidOperationException("The exact owned loopback Bolt fixture is required.");
            Driver=GraphDatabase.Driver(Uri,AuthTokens.Basic("neo4j",Password));
            Provider=new Neo4jOASIS(Uri,"neo4j",Password){IsVersionControlEnabled=history};
        }
        public async Task Activate()
        {var result=await Provider.ActivateProviderAsync();Assert.False(result.IsError,result.Message);}
        public async Task<(IAvatar Account,IAvatarDetail Detail)> Seed()
        {
            var id=Guid.NewGuid();var name="neo-profile-"+id.ToString("N");
            using(var session=Driver.AsyncSession())
            {
                var before=await(await session.RunAsync("MATCH (n) WHERE (n:Avatar OR n:AvatarDetail) AND n.Id=$id RETURN count(n) AS count",new{id=id.ToString()})).SingleAsync();
                Assert.Equal(0L,before["count"].As<long>());
            }
            _ids.Add(id.ToString());
            var account=await Provider.SaveAvatarAsync(new Avatar{Id=id,Username=name,Email=name+"@example.test",Title="Before",
                FirstName="Before",LastName="Before",Password="stored-hash",RefreshToken="session",VerificationToken="verification",
                CreatedDate=DateTime.UtcNow,Version=1,VersionId=Guid.NewGuid(),IsActive=true,AvatarType=new EnumValue<AvatarType>(AvatarType.User)});
            var detail=await Provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=name,Email=name+"@example.test",Address="address-"+name,
                CreatedDate=DateTime.UtcNow,Version=1,VersionId=Guid.NewGuid(),Karma=37,XP=42,ActiveQuestId=Guid.NewGuid(),ActiveObjectiveId=Guid.NewGuid(),
                Inventory=new List<IInventoryItem>{new InventoryItem{Id=Guid.NewGuid(),Name="keep-inventory",Quantity=3}}});
            Assert.False(account.IsError,account.Message);Assert.False(detail.IsError,detail.Message);
            return(account.Result,detail.Result);
        }
        public async Task<(string Account,string Detail)> Raw(Guid id)
        {
            using var session=Driver.AsyncSession();
            var row=await(await session.RunAsync("MATCH (a:Avatar {Id:$id}),(d:AvatarDetail {Id:$id}) RETURN a.SnapshotJson AS account,d.SnapshotJson AS detail",new{id=id.ToString()})).SingleAsync();
            return(row["account"].As<string>(),row["detail"].As<string>());
        }
        public async ValueTask DisposeAsync()
        {
            try
            {
                await Provider.DeActivateProviderAsync();
                using var session=Driver.AsyncSession();
                await(await session.RunAsync("MATCH (n) WHERE (n:Avatar OR n:AvatarDetail) AND n.Id IN $ids DETACH DELETE n",new{ids=_ids})).ConsumeAsync();
                await(await session.RunAsync("MATCH (v:OasisRecordVersion) WHERE v.RecordId IN $ids DELETE v",new{ids=_ids})).ConsumeAsync();
                var remaining=await(await session.RunAsync("MATCH (n) WHERE ((n:Avatar OR n:AvatarDetail) AND n.Id IN $ids) OR (n:OasisRecordVersion AND n.RecordId IN $ids) RETURN count(n) AS count",new{ids=_ids})).SingleAsync();
                Assert.Equal(0L,remaining["count"].As<long>());
            }
            finally{await Driver.CloseAsync();}
        }
    }

    private static AvatarProfileMutation Edit((IAvatar Account,IAvatarDetail Detail) opened,
        params (AvatarProfileProperty Field,string? Value)[] fields)
        =>new(opened.Account.Id,new(opened.Account.Version,opened.Account.VersionId),new(opened.Detail.Version,opened.Detail.VersionId),
            fields.Select(f=>new KeyValuePair<AvatarProfileProperty,string>(f.Field,f.Value!)));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NativeProfileEditsPreserveUnknownStateAndCanonicalHistory(bool history)
    {
        await using var store=new OwnedStore(history);await store.Activate();var opened=await store.Seed();
        var raw=await store.Raw(opened.Account.Id);
        var accountJson=OasisJson.ParseStoredObject(raw.Account);accountJson["NativeExactInteger"]=new JRaw("9007199254740993");
        var detailJson=OasisJson.ParseStoredObject(raw.Detail);detailJson["NativeGameState"]=new JObject{{"score",71}};
        using(var session=store.Driver.AsyncSession())
            await(await session.RunAsync("MATCH (a:Avatar {Id:$id}),(d:AvatarDetail {Id:$id}) SET a.SnapshotJson=$account,d.SnapshotJson=$detail,d.NativeBytes=$bytes",
                new{id=opened.Account.Id.ToString(),account=accountJson.ToString(Newtonsoft.Json.Formatting.None),detail=detailJson.ToString(Newtonsoft.Json.Formatting.None),bytes=new byte[]{0,17,255}})).ConsumeAsync();
        var username=opened.Account.Username+"-edited";
        var result=await store.Provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit(opened,
            (AvatarProfileProperty.Title," After "),(AvatarProfileProperty.FirstName,"Changed"),(AvatarProfileProperty.LastName,"Name"),
            (AvatarProfileProperty.Address,"Changed"),(AvatarProfileProperty.Username,username),(AvatarProfileProperty.Email,username+"@example.test")));
        Assert.False(result.IsError,result.Message);Assert.True(result.IsSaved);Assert.Equal(2,result.SavedCount);
        Assert.Equal("After",result.Result.Title);Assert.Equal("Changed",result.Result.Address);
        var account=await store.Provider.LoadAvatarAsync(opened.Account.Id);var detail=await store.Provider.LoadAvatarDetailAsync(opened.Account.Id);
        Assert.False(account.IsError,account.Message);Assert.False(detail.IsError,detail.Message);
        Assert.Equal("stored-hash",account.Result.Password);Assert.Equal("session",account.Result.RefreshToken);Assert.Equal("verification",account.Result.VerificationToken);
        Assert.Equal(username,account.Result.Username);Assert.Equal(username,detail.Result.Username);Assert.Equal(account.Result.Email,detail.Result.Email);
        Assert.Equal(opened.Detail.ActiveQuestId,detail.Result.ActiveQuestId);Assert.Equal(opened.Detail.ActiveObjectiveId,detail.Result.ActiveObjectiveId);
        Assert.Equal(37,detail.Result.Karma);Assert.Equal(42,detail.Result.XP);Assert.Equal(3,Assert.Single(detail.Result.Inventory).Quantity);
        Assert.Equal(opened.Account.Version+1,account.Result.Version);Assert.Equal(opened.Detail.Version+1,detail.Result.Version);
        Assert.Equal(opened.Account.VersionId,account.Result.PreviousVersionId);Assert.Equal(opened.Detail.VersionId,detail.Result.PreviousVersionId);
        Assert.Equal(account.Result.ModifiedDate,detail.Result.ModifiedDate);Assert.Equal(opened.Account.Id,account.Result.ModifiedByAvatarId);
        raw=await store.Raw(opened.Account.Id);
        Assert.Equal("9007199254740993",OasisJson.ParseStoredObject(raw.Account)["NativeExactInteger"]!.ToString());
        Assert.Equal(71,OasisJson.ParseStoredObject(raw.Detail)["NativeGameState"]!["score"]!.Value<int>());
        Assert.Equal(account.Result.FullName,OasisJson.ParseStoredObject(raw.Account)["Name"]!.Value<string>());
        using(var session=store.Driver.AsyncSession())
        {
            var node=await(await session.RunAsync("MATCH (d:AvatarDetail {Id:$id}) RETURN d.NativeBytes AS bytes",new{id=opened.Account.Id.ToString()})).SingleAsync();
            Assert.Equal(new byte[]{0,17,255},node["bytes"].As<byte[]>());
        }
        if(history)
        {
            var previous=await store.Provider.LoadAvatarAsync(opened.Account.Id,opened.Account.Version);
            Assert.False(previous.IsError,previous.Message);Assert.Equal("Before",previous.Result.Title);
            using var session=store.Driver.AsyncSession();
            var archived=await(await session.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Avatar',RecordId:$id,Version:$version}) RETURN v.SnapshotJson AS snapshot",
                new{id=opened.Account.Id.ToString(),version=opened.Account.Version})).SingleAsync();
            Assert.Equal("9007199254740993",OasisJson.ParseStoredObject(archived["snapshot"].As<string>())["NativeExactInteger"]!.ToString());
        }
        var cleared=await store.Provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit((account.Result,detail.Result),
            (AvatarProfileProperty.Title,null),(AvatarProfileProperty.FirstName,null),(AvatarProfileProperty.LastName,""),(AvatarProfileProperty.Address,null)));
        Assert.False(cleared.IsError,cleared.Message);Assert.Equal("",cleared.Result.Title);Assert.Equal("",cleared.Result.Address);
        account=await store.Provider.LoadAvatarAsync(opened.Account.Id);detail=await store.Provider.LoadAvatarDetailAsync(opened.Account.Id);
        var beforeNoop=await store.Raw(opened.Account.Id);
        var noop=await store.Provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit((account.Result,detail.Result),
            (AvatarProfileProperty.Title,""),(AvatarProfileProperty.Address,"")));
        Assert.False(noop.IsError,noop.Message);Assert.False(noop.IsSaved);Assert.Equal(0,noop.SavedCount);
        Assert.Equal(beforeNoop,await store.Raw(opened.Account.Id));
    }

    [Fact]
    public async Task NativeSecondRecordAndLoginIdentityCollisionsRollBackTheFirstRecord()
    {
        await using var store=new OwnedStore();await store.Activate();var opened=await store.Seed();var other=await store.Seed();
        var before=await store.Raw(opened.Account.Id);var constraint="profile_username_fixture_"+Guid.NewGuid().ToString("N");
        using var session=store.Driver.AsyncSession();
        await(await session.RunAsync("CREATE CONSTRAINT "+constraint+" FOR (d:AvatarDetail) REQUIRE d.Username IS UNIQUE")).ConsumeAsync();
        // Reserve a secondary-only login identity. It is free in Avatar, so the
        // primary update succeeds, then the native AvatarDetail constraint rejects
        // the second update. No substituted SDK result or transaction is involved.
        try
        {
            var secondary="secondary-only-"+Guid.NewGuid().ToString("N");
            var otherRaw=await store.Raw(other.Account.Id);var otherDetail=OasisJson.ParseStoredObject(otherRaw.Detail);
            otherDetail["Username"]=secondary;
            await(await session.RunAsync("MATCH (d:AvatarDetail {Id:$id}) SET d.Username=$username,d.SnapshotJson=$snapshot",
                new{id=other.Account.Id.ToString(),username=secondary,snapshot=otherDetail.ToString(Newtonsoft.Json.Formatting.None)})).ConsumeAsync();
            var failed=await store.Provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit(opened,
                (AvatarProfileProperty.Title,"Must roll back"),(AvatarProfileProperty.Username,secondary)));
            Assert.True(failed.IsError);Assert.False(failed.IsSaved);Assert.Null(failed.Result);
            Assert.Equal(before,await store.Raw(opened.Account.Id));
            Assert.False((await store.Provider.LoadAvatarAsync(opened.Account.Id)).IsError);
            foreach(var field in new[]{AvatarProfileProperty.Username,AvatarProfileProperty.Email})
            {
                var value=field==AvatarProfileProperty.Username?other.Account.Username:other.Account.Email;
                var collision=await store.Provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit(opened,
                    (AvatarProfileProperty.Title,"Must roll back"),(AvatarProfileProperty.Address,"Changed"),(field,value)));
                Assert.True(collision.IsError);Assert.Equal(AvatarProfileErrors.IdentityConflict,collision.ErrorCode);
                Assert.False(collision.IsSaved);Assert.Null(collision.Result);Assert.Equal(before,await store.Raw(opened.Account.Id));
            }
        }
        finally{await(await session.RunAsync("DROP CONSTRAINT "+constraint+" IF EXISTS")).ConsumeAsync();}
    }

    [Fact]
    public async Task NativeRevisionAndOwnershipChecksProtectGameStateAndConcurrentEdits()
    {
        await using var store=new OwnedStore();await store.Activate();var opened=await store.Seed();
        var edit=Edit(opened,(AvatarProfileProperty.Title,"Stale"),(AvatarProfileProperty.Address,"Stale"));
        var foreign=await store.Provider.ApplyAvatarProfileAsync(Guid.NewGuid(),edit);
        Assert.True(foreign.IsError);Assert.Equal(AvatarProfileErrors.Forbidden,foreign.ErrorCode);
        var raw=await store.Raw(opened.Account.Id);var game=OasisJson.ParseStoredObject(raw.Detail);
        game["XP"]=999;game["Version"]=opened.Detail.Version+1;game["VersionId"]=Guid.NewGuid().ToString();
        using(var session=store.Driver.AsyncSession())
            await(await session.RunAsync("MATCH (d:AvatarDetail {Id:$id}) SET d.SnapshotJson=$snapshot",new{id=opened.Account.Id.ToString(),snapshot=game.ToString(Newtonsoft.Json.Formatting.None)})).ConsumeAsync();
        var before=await store.Raw(opened.Account.Id);
        var stale=await store.Provider.ApplyAvatarProfileAsync(opened.Account.Id,edit);
        Assert.True(stale.IsError);Assert.Equal(AvatarProfileErrors.Conflict,stale.ErrorCode);Assert.Equal(before,await store.Raw(opened.Account.Id));
        var account=await store.Provider.LoadAvatarAsync(opened.Account.Id);var detail=await store.Provider.LoadAvatarDetailAsync(opened.Account.Id);
        Assert.Equal(999,detail.Result.XP);
        var results=await Task.WhenAll(new[]{"First","Second"}.Select(label=>store.Provider.ApplyAvatarProfileAsync(opened.Account.Id,
            Edit((account.Result,detail.Result),(AvatarProfileProperty.Title,label),(AvatarProfileProperty.Address,label)))));
        Assert.Single(results.Where(r=>!r.IsError));Assert.Single(results.Where(r=>r.ErrorCode==AvatarProfileErrors.Conflict));
        account=await store.Provider.LoadAvatarAsync(opened.Account.Id);detail=await store.Provider.LoadAvatarDetailAsync(opened.Account.Id);
        Assert.Equal(account.Result.Title,detail.Result.Address);Assert.Equal(999,detail.Result.XP);
        var latest=Edit((account.Result,detail.Result),(AvatarProfileProperty.Title,"Refused"));
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        var stopped=await store.Provider.ApplyAvatarProfileAsync(opened.Account.Id,latest,cancelled.Token);
        Assert.True(stopped.IsError);Assert.Equal("AVATAR_PROFILE_CANCELLED",stopped.ErrorCode);
        using(var session=store.Driver.AsyncSession())
            await(await session.RunAsync("MATCH (d:AvatarDetail {Id:$id}) DETACH DELETE d",new{id=opened.Account.Id.ToString()})).ConsumeAsync();
        var missing=await store.Provider.ApplyAvatarProfileAsync(opened.Account.Id,latest);
        Assert.True(missing.IsError);Assert.Equal(AvatarProfileErrors.NotFound,missing.ErrorCode);Assert.False(missing.IsSaved);
        Assert.Equal(account.Result.Title,(await store.Provider.LoadAvatarAsync(opened.Account.Id)).Result.Title);
    }
}

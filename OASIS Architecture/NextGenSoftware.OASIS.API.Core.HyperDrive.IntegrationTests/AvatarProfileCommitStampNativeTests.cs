using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Providers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class AvatarProfileCommitStampNativeTests
{
    private const string MongoUri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";

    private sealed class MongoStore:IAsyncDisposable
    {
        private readonly MongoClient _client=new(MongoUri);
        private readonly string _name="profile_stamp_"+Guid.NewGuid().ToString("N");
        public MongoDBOASIS Provider {get;private set;}=null!;
        public async Task Activate()
        {
            Assert.DoesNotContain(_name,await(await _client.ListDatabaseNamesAsync()).ToListAsync());
            Provider=new MongoDBOASIS(MongoUri,_name);
            var active=await Provider.ActivateProviderAsync();Assert.False(active.IsError,active.Message);
        }
        public async ValueTask DisposeAsync()
        {
            if(Provider==null)return;
            try{await Provider.DeActivateProviderAsync();}
            finally
            {
                Assert.StartsWith("profile_stamp_",_name);
                await _client.DropDatabaseAsync(_name);
                Assert.DoesNotContain(_name,await(await _client.ListDatabaseNamesAsync()).ToListAsync());
            }
        }
    }

    private sealed class BoltStore:IAsyncDisposable
    {
        private readonly IDriver _driver=GraphDatabase.Driver("bolt://127.0.0.1:27687",AuthTokens.Basic("neo4j","oasis-profile-fixture-only"));
        public Neo4jOASIS Provider {get;}=new("bolt://127.0.0.1:27687","neo4j","oasis-profile-fixture-only");
        private Guid? _owned;
        public async Task Activate(Guid id)
        {
            using var session=_driver.AsyncSession();
            var absent=await(await session.RunAsync("MATCH (n) WHERE ((n:Avatar OR n:AvatarDetail) AND n.Id=$id) OR (n:OasisRecordVersion AND n.RecordId=$id) RETURN count(n) AS count",new{id=id.ToString()})).SingleAsync();
            Assert.Equal(0L,absent["count"].As<long>());_owned=id;
            var active=await Provider.ActivateProviderAsync();Assert.False(active.IsError,active.Message);
        }
        public async ValueTask DisposeAsync()
        {
            try
            {
                try{await Provider.DeActivateProviderAsync();}
                finally
                {
                    if(_owned.HasValue)
                    {
                        using var session=_driver.AsyncSession();var id=_owned.Value.ToString();
                        await(await session.RunAsync("MATCH (n) WHERE (n:Avatar OR n:AvatarDetail) AND n.Id=$id DETACH DELETE n",new{id})).ConsumeAsync();
                        await(await session.RunAsync("MATCH (v:OasisRecordVersion) WHERE v.RecordId=$id DELETE v",new{id})).ConsumeAsync();
                        var remaining=await(await session.RunAsync("MATCH (n) WHERE ((n:Avatar OR n:AvatarDetail) AND n.Id=$id) OR (n:OasisRecordVersion AND n.RecordId=$id) RETURN count(n) AS count",new{id})).SingleAsync();
                        Assert.Equal(0L,remaining["count"].As<long>());
                    }
                }
            }
            finally{await _driver.CloseAsync();}
        }
    }

    private sealed class SqliteStore:IDisposable
    {
        private readonly DirectoryInfo _directory=Directory.CreateTempSubdirectory("oasis-profile-stamp-");
        public SQLLiteDBOASIS Provider {get;}
        public SqliteStore()=>Provider=new SQLLiteDBOASIS($"Data Source={Path.Combine(_directory.FullName,"profile.db")};Pooling=False");
        public void Dispose()
        {
            try{Provider.Dispose();}
            finally
            {
                Assert.StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()))+Path.DirectorySeparatorChar,Path.GetFullPath(_directory.FullName));
                Assert.StartsWith("oasis-profile-stamp-",_directory.Name);
                _directory.Delete(true);Assert.False(Directory.Exists(_directory.FullName));
            }
        }
    }

    private static AvatarProfileMutation Edit(Guid id,AvatarProfileSnapshot before,params (AvatarProfileProperty Field,string Value)[] fields)
        =>new(id,before.AvatarRevision,before.DetailRevision,fields.Select(f=>new KeyValuePair<AvatarProfileProperty,string>(f.Field,f.Value)));

    private static async Task<AvatarProfileSnapshot> Read(IOASISStorageProvider provider,Guid id)
    {
        var account=await provider.LoadAvatarAsync(id);var detail=await provider.LoadAvatarDetailAsync(id);
        Assert.False(account.IsError,account.Message);Assert.False(detail.IsError,detail.Message);
        return new(id,account.Result.Title,account.Result.FirstName,account.Result.LastName,account.Result.Username,
            account.Result.Email,detail.Result.Address,new(account.Result.Version,account.Result.VersionId),new(detail.Result.Version,detail.Result.VersionId));
    }

    private static void SameRevisions(AvatarProfileSnapshot expected,AvatarProfileSnapshot actual)
    {
        Assert.Equal(expected.AvatarRevision.Version,actual.AvatarRevision.Version);
        Assert.Equal(expected.AvatarRevision.VersionId,actual.AvatarRevision.VersionId);
        Assert.Equal(expected.DetailRevision.Version,actual.DetailRevision.Version);
        Assert.Equal(expected.DetailRevision.VersionId,actual.DetailRevision.VersionId);
    }

    [Fact]
    public async Task OneCommandKeepsLogicalRevisionsAndExactAuditAcrossThreeNativeStores()
    {
        if(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI")!=MongoUri ||
            Environment.GetEnvironmentVariable("OASIS_PROFILE_NEO4J_FIXTURE")!="owned-local-27687")
            throw new InvalidOperationException("Both exact owned loopback fixtures are required.");
        var id=Guid.NewGuid();var avatarVersion=Guid.NewGuid();var detailVersion=Guid.NewGuid();
        var username="profile-stamp-"+id.ToString("N");
        await using var mongo=new MongoStore();await mongo.Activate();
        await using var bolt=new BoltStore();await bolt.Activate(id);
        using var sqlite=new SqliteStore();
        var activated=await sqlite.Provider.ActivateProviderAsync();Assert.False(activated.IsError,activated.Message);
        IOASISStorageProvider[] providers=[mongo.Provider,sqlite.Provider,bolt.Provider];
        AvatarProfileSnapshot? before=null;
        foreach(var provider in providers)
        {
            // Identical persisted logical baselines, created through each real SDK.
            var account=await provider.SaveAvatarAsync(new Avatar{Id=id,Username=username,Email=username+"@example.test",
                Title="Before",FirstName="Before",LastName="Name",Password="stored-hash",IsActive=true,
                CreatedDate=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc),Version=1,VersionId=avatarVersion,
                AvatarType=new EnumValue<AvatarType>(AvatarType.User)});
            var detail=await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=username,Email=username+"@example.test",
                Address="Before",XP=42,CreatedDate=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc),Version=1,VersionId=detailVersion});
            Assert.False(account.IsError,account.Message);Assert.False(detail.IsError,detail.Message);
            var stored=await Read(provider,id);
            if(before==null)before=stored;else SameRevisions(before,stored);
        }
        var both=Edit(id,before!,(AvatarProfileProperty.FirstName,"Changed"),(AvatarProfileProperty.Address,"Changed"));
        Assert.Equal(DateTimeKind.Utc,both.ModifiedAtUtc.Kind);
        Assert.NotEqual(Guid.Empty,both.AvatarNextVersionId);Assert.NotEqual(Guid.Empty,both.DetailNextVersionId);
        Assert.NotEqual(both.AvatarNextVersionId,both.DetailNextVersionId);
        AvatarProfileSnapshot? committed=null;
        foreach(var provider in providers)
        {
            var result=await ((IAvatarProfileStorageProvider)provider).ApplyAvatarProfileAsync(id,both);
            Assert.False(result.IsError,result.Message);Assert.True(result.IsSaved);Assert.Equal(2,result.SavedCount);
            Assert.Equal(both.AvatarNextVersionId,result.Result.AvatarRevision.VersionId);
            Assert.Equal(both.DetailNextVersionId,result.Result.DetailRevision.VersionId);
            Assert.Equal(before!.AvatarRevision.Version+1,result.Result.AvatarRevision.Version);
            Assert.Equal(before.DetailRevision.Version+1,result.Result.DetailRevision.Version);
            var account=await provider.LoadAvatarAsync(id);var detail=await provider.LoadAvatarDetailAsync(id);
            Assert.False(account.IsError,account.Message);Assert.False(detail.IsError,detail.Message);
            Assert.Equal(both.ModifiedAtUtc.Ticks,account.Result.ModifiedDate.ToUniversalTime().Ticks);
            Assert.Equal(both.ModifiedAtUtc.Ticks,detail.Result.ModifiedDate.ToUniversalTime().Ticks);
            Assert.Equal(42,detail.Result.XP);Assert.Equal("stored-hash",account.Result.Password);
            if(committed==null)committed=result.Result;else SameRevisions(committed,result.Result);
            SameRevisions(result.Result,await Read(provider,id));
            // Reusing a submitted command is not an idempotency or replay grant.
            var stale=await ((IAvatarProfileStorageProvider)provider).ApplyAvatarProfileAsync(id,both);
            Assert.True(stale.IsError);Assert.Equal(AvatarProfileErrors.Conflict,stale.ErrorCode);
            SameRevisions(result.Result,await Read(provider,id));
        }
        // Generated stamps never advance unchanged records. One-sided edits must
        // keep the other row's revision/audit, on every actual storage engine.
        var accountOnly=Edit(id,committed!,(AvatarProfileProperty.Title,"Changed"));
        foreach(var provider in providers)
        {
            var result=await ((IAvatarProfileStorageProvider)provider).ApplyAvatarProfileAsync(id,accountOnly);
            Assert.False(result.IsError,result.Message);Assert.Equal(1,result.SavedCount);
            Assert.Equal(accountOnly.AvatarNextVersionId,result.Result.AvatarRevision.VersionId);
            Assert.Equal(both.DetailNextVersionId,result.Result.DetailRevision.VersionId);
            var detail=await provider.LoadAvatarDetailAsync(id);Assert.False(detail.IsError,detail.Message);
            Assert.Equal(both.ModifiedAtUtc.Ticks,detail.Result.ModifiedDate.ToUniversalTime().Ticks);
        }
        var afterAccount=await Read(providers[0],id);
        var detailOnly=Edit(id,afterAccount,(AvatarProfileProperty.Address,"Detail only"));
        foreach(var provider in providers)
        {
            var result=await ((IAvatarProfileStorageProvider)provider).ApplyAvatarProfileAsync(id,detailOnly);
            Assert.False(result.IsError,result.Message);Assert.Equal(1,result.SavedCount);
            Assert.Equal(accountOnly.AvatarNextVersionId,result.Result.AvatarRevision.VersionId);
            Assert.Equal(detailOnly.DetailNextVersionId,result.Result.DetailRevision.VersionId);
            var account=await provider.LoadAvatarAsync(id);Assert.False(account.IsError,account.Message);
            Assert.Equal(accountOnly.ModifiedAtUtc.Ticks,account.Result.ModifiedDate.ToUniversalTime().Ticks);
            var noop=await ((IAvatarProfileStorageProvider)provider).ApplyAvatarProfileAsync(id,Edit(id,result.Result,
                (AvatarProfileProperty.Title,"Changed"),(AvatarProfileProperty.Address,"Detail only")));
            Assert.False(noop.IsError,noop.Message);Assert.False(noop.IsSaved);Assert.Equal(0,noop.SavedCount);
            SameRevisions(result.Result,noop.Result);
            SameRevisions(result.Result,await Read(provider,id));
        }
    }
}

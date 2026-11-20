using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Providers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    private const string MongoUri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";

    private sealed class MongoStore:IAsyncDisposable
    {
        private readonly MongoClient _client=new(MongoUri);
        private readonly string _name="profile_stamp_"+Guid.NewGuid().ToString("N");
        private readonly PortraitNativeReadBarrier? _portraitBarrier;
        public MongoStore(PortraitNativeReadBarrier? barrier = null) => _portraitBarrier = barrier;
        public IOASISStorageProvider CreatePeerProvider() => new MongoDBOASIS(MongoUri,_name);
        public async Task<string> ReadDocumentNativeAsync(Guid id)
        {
            var row = await _client.GetDatabase(_name)
                .GetCollection<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar>("Avatar")
                .Find(record => record.HolonId == id).Project<BsonDocument>(new BsonDocument()).SingleAsync();
            return row.ToJson();
        }
        public async Task<string> ReadHolonDocumentNativeAsync(Guid id)
        {
            var row = await _client.GetDatabase(_name)
                .GetCollection<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon>("Holon")
                .Find(record => record.HolonId == id).Project<BsonDocument>(new BsonDocument()).SingleAsync();
            return row.ToJson();
        }
        public IOASISStorageProvider CreatePreferencePeerProvider(PreferenceNativeReadBarrier barrier) =>
            new PreferenceReadGatedMongo(MongoUri,_name,barrier);
        public async Task<(string? Address,string? Portrait)> ReadPortraitNativeStateAsync(Guid id)
        {
            var row=await _client.GetDatabase(_name)
                .GetCollection<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail>("AvatarDetail")
                .Find(record=>record.HolonId==id).SingleAsync();
            return(row.Address,row.Portrait);
        }
        public MongoDBOASIS Provider {get;private set;}=null!;
        internal IMongoDatabase Admin=>_client.GetDatabase("admin");
        internal Task SetAvatarReadProfilingAsync(bool enabled) => _client.GetDatabase(_name)
            .RunCommandAsync<BsonDocument>(new BsonDocument("profile",enabled?2:0));
        internal Task<long> AvatarReadCommandCountAsync() => _client.GetDatabase(_name)
            .GetCollection<BsonDocument>("system.profile").CountDocumentsAsync(new BsonDocument("$or",new BsonArray {
                new BsonDocument("command.find","Avatar"),new BsonDocument("command.aggregate","Avatar") }));
        // The independent observer is outside Provider's app-name-scoped fault.
        // Native verification must not wait for the faulted client's next heartbeat.
        internal async Task<AvatarProfileSnapshot> ReadNative(Guid id)
        {
            var db=_client.GetDatabase(_name);
            var account=await db.GetCollection<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar>("Avatar")
                .Find(record=>record.HolonId==id).SingleAsync();
            var detail=await db.GetCollection<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail>("AvatarDetail")
                .Find(record=>record.HolonId==id).SingleAsync();
            return new(account.HolonId,account.Title,account.FirstName,account.LastName,account.Username,account.Email,detail.Address,
                new(account.Version,account.VersionId),new(detail.Version,detail.VersionId));
        }
        public async Task Activate()
        {
            Assert.DoesNotContain(_name,await(await _client.ListDatabaseNamesAsync()).ToListAsync());
            var connection=new MongoUrlBuilder(MongoUri){ApplicationName="profile-stamp-"+Guid.NewGuid().ToString("N")}.ToString();
            Provider=_portraitBarrier==null ? new MongoDBOASIS(connection,_name)
                : new PortraitReadGatedMongo(connection,_name,_portraitBarrier);
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
        public Neo4jOASIS Provider {get;}
        public BoltStore(PortraitNativeReadBarrier? barrier = null) => Provider=barrier==null
            ? new Neo4jOASIS("bolt://127.0.0.1:27687","neo4j","oasis-profile-fixture-only")
            : new PortraitReadGatedNeo("bolt://127.0.0.1:27687","neo4j","oasis-profile-fixture-only",barrier);
        public IOASISStorageProvider CreatePeerProvider() =>
            new Neo4jOASIS("bolt://127.0.0.1:27687","neo4j","oasis-profile-fixture-only");
        public async Task<string> ReadDocumentNativeAsync(Guid id)
        {
            using var session = _driver.AsyncSession();
            var row = await (await session.RunAsync("MATCH (a:Avatar {Id:$id}) RETURN a.SnapshotJson AS account",
                new { id = id.ToString() })).SingleAsync();
            return row["account"].As<string>();
        }
        public async Task<string> ReadHolonDocumentNativeAsync(Guid id)
        {
            using var session = _driver.AsyncSession();
            var row = await (await session.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot",
                new { id = id.ToString() })).SingleAsync();
            return row["snapshot"].As<string>();
        }
        public IOASISStorageProvider CreatePreferencePeerProvider(PreferenceNativeReadBarrier barrier) =>
            new PreferenceReadGatedNeo("bolt://127.0.0.1:27687","neo4j","oasis-profile-fixture-only",barrier);
        public async Task<string?> ReadFirstNameNativeAsync(Guid id)
        {
            using var session=_driver.AsyncSession();
            var row=await(await session.RunAsync("MATCH (a:Avatar {Id:$id}) RETURN a.SnapshotJson AS account",
                new{id=id.ToString()})).SingleAsync();
            return (string?)Newtonsoft.Json.Linq.JObject.Parse(row["account"].As<string>())["FirstName"];
        }
        public async Task<(string? Address,string? Portrait)> ReadPortraitNativeStateAsync(Guid id)
        {
            using var session=_driver.AsyncSession();
            var row=await(await session.RunAsync("MATCH (d:AvatarDetail {Id:$id}) RETURN d.SnapshotJson AS detail",
                new{id=id.ToString()})).SingleAsync();
            var detail=Newtonsoft.Json.Linq.JObject.Parse(row["detail"].As<string>());
            return((string?)detail["Address"],(string?)detail["Portrait"]);
        }
        private readonly HashSet<Guid> _owned = new();
        public async Task TrackOwnedActorAsync(Guid id)
        {
            Assert.NotEqual(Guid.Empty,id);
            Assert.DoesNotContain(id,_owned);
            using var session=_driver.AsyncSession();
            var absent=await(await session.RunAsync("MATCH (n) WHERE ((n:Avatar OR n:AvatarDetail OR n:Holon) AND n.Id=$id) OR (n:OasisRecordVersion AND n.RecordId=$id) RETURN count(n) AS count",new{id=id.ToString()})).SingleAsync();
            Assert.Equal(0L,absent["count"].As<long>());_owned.Add(id);
        }
        public async Task Activate(Guid id)
        {
            await TrackOwnedActorAsync(id);
            var active=await Provider.ActivateProviderAsync();Assert.False(active.IsError,active.Message);
        }
        public async ValueTask DisposeAsync()
        {
            try
            {
                try{await Provider.DeActivateProviderAsync();}
                finally
                {
                    foreach(var owned in _owned)
                    {
                        using var session=_driver.AsyncSession();var id=owned.ToString();
                        await(await session.RunAsync("MATCH (n) WHERE (n:Avatar OR n:AvatarDetail OR n:Holon) AND n.Id=$id DETACH DELETE n",new{id})).ConsumeAsync();
                        await(await session.RunAsync("MATCH (v:OasisRecordVersion) WHERE v.RecordId=$id DELETE v",new{id})).ConsumeAsync();
                        var remaining=await(await session.RunAsync("MATCH (n) WHERE ((n:Avatar OR n:AvatarDetail OR n:Holon) AND n.Id=$id) OR (n:OasisRecordVersion AND n.RecordId=$id) RETURN count(n) AS count",new{id})).SingleAsync();
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
        private string Connection => $"Data Source={Path.Combine(_directory.FullName,"profile.db")};Pooling=False";
        public SqliteStore(PortraitNativeReadBarrier? barrier = null) => Provider=barrier==null
            ? new SQLLiteDBOASIS(Connection) : new PortraitReadGatedSqlite(Connection,barrier);
        public IOASISStorageProvider CreatePeerProvider() => new SQLLiteDBOASIS(Connection);
        public IOASISStorageProvider CreatePreferencePeerProvider(PreferenceNativeReadBarrier barrier) =>
            new PreferenceReadGatedSqlite(Connection,barrier);
        public async Task<string> ReadMetadataJsonNativeAsync(Guid id)
        {
            using var connection=new Microsoft.Data.Sqlite.SqliteConnection(Connection);await connection.OpenAsync();
            using var command=connection.CreateCommand();
            command.CommandText="SELECT MetaDataJson FROM Avatar WHERE Id=$id";
            command.Parameters.AddWithValue("$id",id.ToString());
            return Assert.IsType<string>(await command.ExecuteScalarAsync());
        }
        public async Task<string> ReadHolonMetadataJsonNativeAsync(Guid id)
        {
            using var connection=new Microsoft.Data.Sqlite.SqliteConnection(Connection);await connection.OpenAsync();
            using var command=connection.CreateCommand();
            command.CommandText="SELECT MetaDataJson FROM Holon WHERE Id=$id";
            command.Parameters.AddWithValue("$id",id.ToString());
            return Assert.IsType<string>(await command.ExecuteScalarAsync());
        }
        public async Task<string?> ReadFirstNameNativeAsync(Guid id)
        {
            using var connection=new Microsoft.Data.Sqlite.SqliteConnection(Connection);await connection.OpenAsync();
            using var command=connection.CreateCommand();
            command.CommandText="SELECT FirstName FROM Avatar WHERE Id=$id";
            command.Parameters.AddWithValue("$id",id.ToString());
            var value=await command.ExecuteScalarAsync();
            return value is DBNull?null:(string?)value;
        }
        public async Task<(string? Address,string? Portrait)> ReadPortraitNativeStateAsync(Guid id)
        {
            using var connection=new Microsoft.Data.Sqlite.SqliteConnection(Connection);await connection.OpenAsync();
            using var command=connection.CreateCommand();
            command.CommandText="SELECT Address,Portrait FROM AvatarDetail WHERE Id=$id";
            command.Parameters.AddWithValue("$id",id.ToString());
            using var reader=await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            var address=reader.IsDBNull(0)?null:reader.GetString(0);
            var portrait=reader.IsDBNull(1)?null:reader.GetString(1);
            Assert.False(await reader.ReadAsync());
            return(address,portrait);
        }
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

    private static void RequireOwnedFixtures()
    {
        if(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI")!=MongoUri ||
            Environment.GetEnvironmentVariable("OASIS_PROFILE_NEO4J_FIXTURE")!="owned-local-27687")
            throw new InvalidOperationException("Both exact owned loopback fixtures are required.");
    }

    private static async Task<AvatarProfileSnapshot> SeedRoutingStores(IOASISStorageProvider[] providers,Guid id,string password="stored-hash")
    {
        var username="profile-route-"+id.ToString("N");var accountVersion=Guid.NewGuid();var detailVersion=Guid.NewGuid();
        AvatarProfileSnapshot? baseline=null;
        foreach(var provider in providers)
        {
            var account=await provider.SaveAvatarAsync(new Avatar{Id=id,Username=username,Email=username+"@example.test",
                Title="Before",FirstName="Before",LastName="Name",Password=password,IsActive=true,
                CreatedDate=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc),Version=1,VersionId=accountVersion,
                AvatarType=new EnumValue<AvatarType>(AvatarType.User)});
            var detail=await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=username,Email=username+"@example.test",
                Address="Before",XP=42,CreatedDate=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc),Version=1,VersionId=detailVersion});
            Assert.False(account.IsError,account.Message);Assert.False(detail.IsError,detail.Message);
            var current=await Read(provider,id);
            if(baseline==null)baseline=current;else SameRevisions(baseline,current);
        }
        return baseline!;
    }

    private static ProviderManager Routing(IOASISStorageProvider[] providers,bool replicate,string mode=HyperDriveModes.V2)
    {
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS
        {
            HyperDriveMode=mode,StorageProviders=new StorageProviderSettings(),
            SubscriptionConfig=new(){PlanType="Gold"}
        }};
        var manager=new ProviderManager(null,dna);
        foreach(var provider in providers)Assert.True(manager.RegisterProvider(provider));
        var types=providers.Select(provider=>provider.ProviderType.Value).ToArray();
        var configured=manager.PublishRoutingOverrides(new ProviderRoutingOverrides(
            autoFailoverEnabled:true,autoReplicationEnabled:replicate,autoLoadBalancingEnabled:false,
            failoverProviders:types,replicationProviders:types),providers[0].ProviderType.Value);
        Assert.False(configured.IsError,configured.Message);return manager;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProfileRoutingReplicatesOnlyCommittedLogicalFields(bool synchronous)
    {
        RequireOwnedFixtures();var id=Guid.NewGuid();
        await using var mongo=new MongoStore();await mongo.Activate();
        await using var bolt=new BoltStore();await bolt.Activate(id);
        using var sqlite=new SqliteStore();Assert.False((await sqlite.Provider.ActivateProviderAsync()).IsError);
        IOASISStorageProvider[] providers=[mongo.Provider,sqlite.Provider,bolt.Provider];
        var before=await SeedRoutingStores(providers,id);var manager=Routing(providers,true);var router=new OASISHyperDrive(manager);
        var command=Edit(id,before,(AvatarProfileProperty.Title,"Routed"),(AvatarProfileProperty.Address,"Routed"));
        var request=new StorageOperationRequest{Operation="ApplyAvatarProfile",AvatarId=id,Payload=command};
        var result=synchronous?router.RouteRequest<AvatarProfileSnapshot>(request):await router.RouteRequestAsync<AvatarProfileSnapshot>(request);
        Assert.False(result.IsError,result.Exception?.ToString()??result.Message);Assert.False(result.IsWarning,result.Message);
        Assert.True(result.IsSaved);Assert.Equal(2,result.SavedCount);
        Assert.Equal(2,manager.LastReplicationDiagnostic.SucceededCount);Assert.Equal(0,manager.LastReplicationDiagnostic.FailedCount);
        Assert.True(result.MetaData.ContainsKey("hyperDriveReplicationDiagnostic"));
        foreach(var provider in providers)
        {
            var stored=await Read(provider,id);SameRevisions(result.Result,stored);
            Assert.Equal("Routed",stored.Title);Assert.Equal("Routed",stored.Address);
        }
        var replicaAttempts=providers.Skip(1).Select(provider=>manager.PerformanceMonitor.GetMetrics(provider.ProviderType.Value).TotalRequests).ToArray();
        var noop=new StorageOperationRequest{Operation="ApplyAvatarProfile",AvatarId=id,
            Payload=Edit(id,result.Result,(AvatarProfileProperty.Title,"Routed"),(AvatarProfileProperty.Address,"Routed"))};
        var unchanged=synchronous?router.RouteRequest<AvatarProfileSnapshot>(noop):await router.RouteRequestAsync<AvatarProfileSnapshot>(noop);
        Assert.False(unchanged.IsError,unchanged.Message);Assert.False(unchanged.IsSaved);
        Assert.False(unchanged.MetaData?.ContainsKey("hyperDriveReplicationDiagnostic")??false);
        var stale=synchronous?router.RouteRequest<AvatarProfileSnapshot>(request):await router.RouteRequestAsync<AvatarProfileSnapshot>(request);
        Assert.True(stale.IsError);Assert.Equal(AvatarProfileErrors.Conflict,stale.ErrorCode);
        Assert.False(stale.MetaData?.ContainsKey("hyperDriveFailoverDiagnostic")??false);
        var fanout=await router.ReplicateRequestAsync<AvatarProfileSnapshot>(request);
        Assert.True(fanout.IsError);Assert.Equal("AVATAR_PROFILE_COMMITTED_EVIDENCE_REQUIRED",fanout.ErrorCode);
        var foreign=await router.RouteRequestAsync<AvatarProfileSnapshot>(new StorageOperationRequest
            {Operation="ApplyAvatarProfile",AvatarId=Guid.NewGuid(),Payload=command});
        Assert.True(foreign.IsError);Assert.Equal(AvatarProfileErrors.Forbidden,foreign.ErrorCode);
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        var cancellation=await router.RouteRequestAsync<AvatarProfileSnapshot>(new StorageOperationRequest
            {Operation="ApplyAvatarProfile",AvatarId=id,Payload=command,ProfileCancellationToken=cancelled.Token});
        Assert.True(cancellation.IsError);Assert.Equal("AVATAR_PROFILE_CANCELLED",cancellation.ErrorCode);
        Assert.Equal(replicaAttempts,providers.Skip(1).Select(provider=>manager.PerformanceMonitor.GetMetrics(provider.ProviderType.Value).TotalRequests).ToArray());
    }

    [Fact]
    public async Task ProfileRoutingStopsAfterAnActualUnknownNativeCommit()
    {
        RequireOwnedFixtures();var id=Guid.NewGuid();
        await using var mongo=new MongoStore();await mongo.Activate();
        await using var bolt=new BoltStore();await bolt.Activate(id);
        using var sqlite=new SqliteStore();Assert.False((await sqlite.Provider.ActivateProviderAsync()).IsError);
        IOASISStorageProvider[] providers=[mongo.Provider,sqlite.Provider,bolt.Provider];
        var before=await SeedRoutingStores(providers,id);var manager=Routing(providers,true);var router=new OASISHyperDrive(manager);
        await mongo.Admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","alwaysOn"},
            {"data",new BsonDocument{{"failCommands",new BsonArray{"commitTransaction"}},
                {"appName",new MongoUrl(mongo.Provider.ConnectionString).ApplicationName},
                {"writeConcernError",new BsonDocument{{"code",64},{"errmsg","owned profile routed acknowledgement fault"}}}}}});
        try
        {
            var result=await router.RouteRequestAsync<AvatarProfileSnapshot>(new StorageOperationRequest
                {Operation="ApplyAvatarProfile",AvatarId=id,Payload=Edit(id,before,
                    (AvatarProfileProperty.Title,"Committed without acknowledgement"),(AvatarProfileProperty.Address,"Committed without acknowledgement"))});
            Assert.True(result.IsError);Assert.False(result.IsSaved);Assert.Equal(AvatarProfileErrors.CommitUnknown,result.ErrorCode);
            Assert.True(NativeMutationReconciliation.IsRequired(result.Exception));
            Assert.NotEmpty(NativeMutationReconciliation.OriginalTransactionIds(result.Exception));
            Assert.False(result.MetaData?.ContainsKey("hyperDriveReplicationDiagnostic")??false);
            Assert.False(result.MetaData?.ContainsKey("hyperDriveFailoverDiagnostic")??false);
        }
        finally{await mongo.Admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","off"}});}
        Assert.Equal("Committed without acknowledgement",(await Read(mongo.Provider,id)).Title);
        foreach(var provider in providers.Skip(1))
        {
            var stored=await Read(provider,id);SameRevisions(before,stored);Assert.Equal("Before",stored.Title);Assert.Equal("Before",stored.Address);
            Assert.Equal(0,manager.PerformanceMonitor.GetMetrics(provider.ProviderType.Value)?.TotalRequests??0);
        }
    }

    [Fact]
    public async Task ProfileRoutingFailsOverAfterRealPreCommitFindFailure()
    {
        RequireOwnedFixtures();var id=Guid.NewGuid();
        await using var mongo=new MongoStore();await mongo.Activate();
        await using var bolt=new BoltStore();await bolt.Activate(id);
        using var sqlite=new SqliteStore();Assert.False((await sqlite.Provider.ActivateProviderAsync()).IsError);
        IOASISStorageProvider[] providers=[mongo.Provider,sqlite.Provider,bolt.Provider];
        var before=await SeedRoutingStores(providers,id);var manager=Routing(providers,false);var router=new OASISHyperDrive(manager);
        await mongo.Admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","alwaysOn"},
            {"data",new BsonDocument{{"failCommands",new BsonArray{"find"}},
                {"appName",new MongoUrl(mongo.Provider.ConnectionString).ApplicationName},{"errorCode",91}}}});
        try
        {
            var result=await router.RouteRequestAsync<AvatarProfileSnapshot>(new StorageOperationRequest
                {Operation="ApplyAvatarProfile",AvatarId=id,Payload=Edit(id,before,
                    (AvatarProfileProperty.Title,"Failover saved"),(AvatarProfileProperty.Address,"Failover saved"))});
            Assert.False(result.IsError,result.Exception?.ToString()??result.Message);Assert.True(result.IsSaved);
            Assert.True(manager.LastFailoverDiagnostic.Succeeded);
            Assert.Equal(ProviderType.SQLLiteDBOASIS,manager.LastFailoverDiagnostic.SelectedProvider);
            Assert.Equal("Failover saved",(await Read(sqlite.Provider,id)).Title);
        }
        finally{await mongo.Admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","off"}});}
        var unchangedPrimary=await mongo.ReadNative(id);
        SameRevisions(before,unchangedPrimary);
        Assert.Equal(before.Title,unchangedPrimary.Title);Assert.Equal(before.Address,unchangedPrimary.Address);
        SameRevisions(before,await Read(bolt.Provider,id));
    }

    [Fact]
    public async Task ProfileRoutingRoundRobinKeepsAllThreeReplicasOnTheAcknowledgedRevision()
    {
        RequireOwnedFixtures();var id=Guid.NewGuid();
        await using var mongo=new MongoStore();await mongo.Activate();
        await using var bolt=new BoltStore();await bolt.Activate(id);
        using var sqlite=new SqliteStore();Assert.False((await sqlite.Provider.ActivateProviderAsync()).IsError);
        IOASISStorageProvider[] providers=[mongo.Provider,sqlite.Provider,bolt.Provider];
        var before=await SeedRoutingStores(providers,id);var manager=Routing(providers,true);var router=new OASISHyperDrive(manager);
        var types=providers.Select(provider=>provider.ProviderType.Value).ToArray();
        var configured=manager.PublishRoutingOverrides(new ProviderRoutingOverrides(true,true,true,
            failoverProviders:types,replicationProviders:types,loadBalancingProviders:types),types[0]);
        Assert.False(configured.IsError,configured.Message);
        var serving=new HashSet<string>();
        // Three bounded writes exercise one complete rotation, never a soak loop.
        for(var index=0;index<3;index++)
        {
            var title="rotation-"+index;
            var result=await router.RouteRequestAsync<AvatarProfileSnapshot>(new StorageOperationRequest
                {Operation="ApplyAvatarProfile",AvatarId=id,Payload=Edit(id,before,(AvatarProfileProperty.Title,title))},
                LoadBalancingStrategy.RoundRobin);
            Assert.False(result.IsError,result.Exception?.ToString()??result.Message);Assert.False(result.IsWarning,result.Message);
            using var route=System.Text.Json.JsonDocument.Parse(result.MetaData["hyperDriveRoute"]);
            serving.Add(route.RootElement.GetProperty("ServingProvider").GetString()!);
            Assert.Equal(2,manager.LastReplicationDiagnostic.SucceededCount);
            foreach(var provider in providers)
            {
                var stored=await Read(provider,id);SameRevisions(result.Result,stored);Assert.Equal(title,stored.Title);
            }
            before=result.Result;
        }
        Assert.Equal(types.Select(type=>type.ToString()).OrderBy(value=>value),serving.OrderBy(value=>value));
    }


    [Theory]
    [InlineData(HyperDriveModes.Legacy)]
    [InlineData(HyperDriveModes.V2)]
    public async Task ProfileManagerUsesCapturedRoutingAndAtomicNativeCommand(string mode)
    {
        RequireOwnedFixtures();var id=Guid.NewGuid();
        await using var mongo=new MongoStore();await mongo.Activate();
        await using var bolt=new BoltStore();await bolt.Activate(id);
        using var sqlite=new SqliteStore();Assert.False((await sqlite.Provider.ActivateProviderAsync()).IsError);
        IOASISStorageProvider[] providers=[mongo.Provider,sqlite.Provider,bolt.Provider];
        var baseline=await SeedRoutingStores(providers,id);var manager=Routing(providers,true,mode);
        var types=providers.Select(provider=>provider.ProviderType.Value).ToArray();
        Assert.False(manager.PublishRoutingOverrides(new ProviderRoutingOverrides(true,true,true,
            failoverProviders:types,replicationProviders:types,loadBalancingProviders:new[]{ProviderType.Neo4jOASIS}),
            ProviderType.MongoDBOASIS).IsError);
        var before=manager.CaptureRoutingPolicy().Result;
        var sdk=new AvatarManager(null,manager.OASISDNA,manager);
        var command=Edit(id,baseline,(AvatarProfileProperty.Title,"SDK manager"),(AvatarProfileProperty.Address,"SDK manager"));
        var result=await sdk.ApplyAvatarProfileAsync(id,command);
        Assert.False(result.IsError,result.Exception?.ToString()??result.Message);Assert.False(result.IsWarning,result.Message);
        Assert.True(result.IsSaved);Assert.Equal(2,result.SavedCount);
        using(var route=System.Text.Json.JsonDocument.Parse(result.MetaData["hyperDriveRoute"]))
            Assert.Equal(nameof(ProviderType.Neo4jOASIS),route.RootElement.GetProperty("ServingProvider").GetString());
        Assert.True(result.MetaData.ContainsKey("hyperDriveReplicationDiagnostic"));
        foreach(var provider in providers)
        {
            var persisted=await Read(provider,id);SameRevisions(result.Result,persisted);
            Assert.Equal("SDK manager",persisted.Title);Assert.Equal("SDK manager",persisted.Address);
        }
        var requests=providers.Sum(provider=>manager.PerformanceMonitor.GetMetrics(provider.ProviderType.Value)?.TotalRequests??0);
        var foreign=await sdk.ApplyAvatarProfileAsync(Guid.NewGuid(),command);
        Assert.True(foreign.IsError);Assert.Equal(AvatarProfileErrors.Forbidden,foreign.ErrorCode);
        var invalid=await sdk.ApplyAvatarProfileAsync(id,command,ProviderType.All);
        Assert.True(invalid.IsError);Assert.Equal(AvatarProfileErrors.Invalid,invalid.ErrorCode);
        var missing=await sdk.ApplyAvatarProfileAsync(id,null!);
        Assert.True(missing.IsError);Assert.Equal(AvatarProfileErrors.Invalid,missing.ErrorCode);
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        var stopped=await sdk.ApplyAvatarProfileAsync(id,command,cancellationToken:cancelled.Token);
        Assert.True(stopped.IsError);Assert.Equal("AVATAR_PROFILE_CANCELLED",stopped.ErrorCode);
        Assert.Equal(requests,providers.Sum(provider=>manager.PerformanceMonitor.GetMetrics(provider.ProviderType.Value)?.TotalRequests??0));
        var after=manager.CaptureRoutingPolicy().Result;
        Assert.Equal(before.PreferredProvider,after.PreferredProvider);
        Assert.Equal(before.AutoFailoverEnabled,after.AutoFailoverEnabled);
        Assert.Equal(before.AutoReplicationEnabled,after.AutoReplicationEnabled);
        Assert.Equal(before.AutoLoadBalancingEnabled,after.AutoLoadBalancingEnabled);
        Assert.Equal(before.FailoverProviders,after.FailoverProviders);
        Assert.Equal(before.ReplicationProviders,after.ReplicationProviders);
        Assert.Equal(before.LoadBalancingProviders,after.LoadBalancingProviders);
        Assert.Equal(mode,manager.OASISDNA.OASIS.HyperDriveMode);
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

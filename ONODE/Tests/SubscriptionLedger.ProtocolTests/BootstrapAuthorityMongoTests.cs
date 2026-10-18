using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

public sealed class BootstrapAuthorityMongoTests : IAsyncLifetime
{
    private readonly Guid _id = Guid.NewGuid();
    private readonly DateTime _now = new(2026,10,18,2,0,0,DateTimeKind.Utc);
    private MongoClient _client;
    private IMongoDatabase _database;
    private IMongoCollection<BootstrapAuthorityRecord> _records;
    private MongoBootstrapAuthority _authority;
    private readonly Clock _clock = new();
    private bool _expireOnRead;
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026,10,18,2,0,0,TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private BootstrapAuthorityRecord Record(long revision=1) => new()
    { Id=_id.ToString("D"),Revision=revision,Ready=true,Enabled=true,Role=AvatarType.User,TokenVersion=2,
        KarmaVersion=3,Karma=123,VerifiedUntilUtc=_now.AddMinutes(1) };

    public async Task InitializeAsync()
    {
        var uri=Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI");
        if(string.IsNullOrWhiteSpace(uri)) throw new InvalidOperationException("Owned replica URI required; no silent skip.");
        var settings=MongoClientSettings.FromConnectionString(uri);
        settings.ClusterConfigurator=builder=>builder.Subscribe<CommandSucceededEvent>(e=>
        { if(_expireOnRead && e.CommandName=="find") _clock.Now=new DateTimeOffset(_now.AddMinutes(1)); });
        if(settings.Servers.Any(x=>x.Host!="127.0.0.1" || x.Port!=27029) || settings.ReplicaSetName!="oasisRecoveryRs")
            throw new InvalidOperationException("Bootstrap tests require the retained isolated replica only.");
        _client=new MongoClient(settings);
        _database=_client.GetDatabase("usage_bootstrap_authority_test");
        _records=_database.GetCollection<BootstrapAuthorityRecord>("private_bootstrap_authority").WithWriteConcern(WriteConcern.WMajority);
        _authority=new MongoBootstrapAuthority(_database,_clock);
        // Controlled trusted provisioning belongs to this test, not an implicit production upsert.
        await _records.InsertOneAsync(Record());
    }

    public async Task DisposeAsync()
    {
        if(_records!=null) await _records.DeleteOneAsync(x=>x.Id==_id.ToString("D"));
        _client?.Dispose();
    }

    [Fact] public async Task ConcurrentInvalidationHasExactlyOneWinner()
    {
        var attempts=Enumerable.Range(0,16).Select(async _=>
        {try {return await _authority.InvalidateAsync(_id,1);}catch(InvalidOperationException){return 0L;}});
        var results=await Task.WhenAll(attempts);
        Assert.Single(results,x=>x==2);Assert.Equal(15,results.Count(x=>x==0));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.ReadAsync(_id,2));
        var stored=await _records.Find(x=>x.Id==_id.ToString("D")).SingleAsync();
        Assert.Equal(2,stored.Revision);Assert.False(stored.Ready);
    }

    [Fact] public async Task PublishRestoresOnlyExactInvalidatedRevisionAndRejectsReplay()
    {
        await _authority.InvalidateAsync(_id,1);
        var stale=Record();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.PublishAsync(stale));
        var next=Record(2);next.KarmaVersion=4;next.Karma=456;
        await _authority.PublishAsync(next);
        Assert.Equal(456,(await _authority.ReadAsync(_id,2)).Karma);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.PublishAsync(next));
    }

    [Theory][InlineData(true)][InlineData(false)] public async Task PublicationCannotRollBackVersions(bool security)
    {
        await _authority.InvalidateAsync(_id,1);
        var next=Record(2);if(security)next.TokenVersion=1;else next.KarmaVersion=2;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.PublishAsync(next));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.ReadAsync(_id,2));
    }

    [Fact] public async Task NewRepositoryCannotForgetAnUnfinishedMutation()
    {
        await _authority.InvalidateAsync(_id,1);
        using var otherClient=new MongoClient(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var restarted=new MongoBootstrapAuthority(otherClient.GetDatabase("usage_bootstrap_authority_test"),_clock);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>restarted.ReadAsync(_id,2));
        var next=Record(2);next.TokenVersion=3;next.Enabled=false;
        await restarted.PublishAsync(next);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>_authority.ReadAsync(_id,2));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>_authority.ReadAsync(_id,3));
    }

    [Theory][InlineData(true)][InlineData(false)] public async Task ChangedValuesRequireNewVersions(bool security)
    {
        await _authority.InvalidateAsync(_id,1);
        var next=Record(2);if(security)next.Enabled=false;else next.Karma=999;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.PublishAsync(next));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.ReadAsync(_id,2));
    }

    [Fact] public async Task ExpiryDuringMongoReadIsRejectedAtResponseTime()
    {
        _expireOnRead=true;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.ReadAsync(_id,2));
        Assert.Equal(_now.AddMinutes(1),_clock.GetUtcNow().UtcDateTime);
    }

    [Fact] public async Task RoleChangeRequiresNewTokenVersionAndRevokesOldTokens()
    {
        await _authority.InvalidateAsync(_id,1);
        var next=Record(2);next.Role=AvatarType.Wizard;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.PublishAsync(next));
        next.TokenVersion=3;
        await _authority.PublishAsync(next);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>_authority.ReadAsync(_id,2));
        Assert.Equal(AvatarType.Wizard,(await _authority.ReadAsync(_id,3)).Role);
    }

    [Theory][InlineData(null)][InlineData(999)] public async Task MissingOrUnknownPersistedRoleCannotAuthenticate(int? role)
    {
        await _records.UpdateOneAsync(x=>x.Id==_id.ToString("D"),
            Builders<BootstrapAuthorityRecord>.Update.Set(x=>x.Role,role.HasValue?(AvatarType?)role.Value:null));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.ReadAsync(_id,2));
    }

    [Fact] public async Task TrustedProvisioningInsertsOnlyOnceWithoutReplacingExistingAuthority()
    {
        await _records.DeleteOneAsync(x=>x.Id==_id.ToString("D"));
        await _authority.ProvisionVerifiedAsync(Record());
        Assert.Equal(123,(await _authority.ReadAsync(_id,2)).Karma);
        var overwrite=Record();overwrite.Karma=999;overwrite.Role=AvatarType.Wizard;
        var error=await Assert.ThrowsAsync<MongoWriteException>(()=>_authority.ProvisionVerifiedAsync(overwrite));
        Assert.Equal(ServerErrorCategory.DuplicateKey,error.WriteError.Category);
        var unchanged=await _authority.ReadAsync(_id,2);
        Assert.Equal(123,unchanged.Karma);Assert.Equal(AvatarType.User,unchanged.Role);
    }

    [Fact] public async Task ProvisioningCannotReenableAnUnfinishedMutation()
    {
        await _authority.InvalidateAsync(_id,1);
        var error=await Assert.ThrowsAsync<MongoWriteException>(()=>_authority.ProvisionVerifiedAsync(Record()));
        Assert.Equal(ServerErrorCategory.DuplicateKey,error.WriteError.Category);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.ReadAsync(_id,2));
        Assert.Equal(2,(await _records.Find(x=>x.Id==_id.ToString("D")).SingleAsync()).Revision);
    }

    [Theory][InlineData(0)][InlineData(2)] public async Task ProvisioningRequiresInitialRevision(long revision)
    {
        await _records.DeleteOneAsync(x=>x.Id==_id.ToString("D"));
        await Assert.ThrowsAnyAsync<ArgumentException>(()=>_authority.ProvisionVerifiedAsync(Record(revision)));
        Assert.Equal(0,await _records.CountDocumentsAsync(x=>x.Id==_id.ToString("D")));
    }

    [Fact] public async Task LogicalMutationRunsOnceOnlyAfterDurableInvalidation()
    {
        var calls=0;
        var result=await _authority.MutateVerifiedAsync(_id,1,async (revision,token)=>
        {
            calls++;
            await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.ReadAsync(_id,2,token));
            var persisted=await _records.Find(x=>x.Id==_id.ToString("D")).SingleAsync(token);
            Assert.False(persisted.Ready);Assert.Equal(revision,persisted.Revision);
            var next=Record(revision);next.KarmaVersion=4;next.Karma=456;
            return ("canonical-success",next);
        });
        Assert.Equal("canonical-success",result);Assert.Equal(1,calls);
        Assert.Equal(456,(await _authority.ReadAsync(_id,2)).Karma);
    }

    [Fact] public async Task InvalidationConflictNeverStartsCanonicalMutation()
    {
        var calls=0;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.MutateVerifiedAsync(_id,2,(revision,token)=>
        { calls++;return Task.FromResult((true,Record(revision))); }));
        Assert.Equal(0,calls);Assert.Equal(123,(await _authority.ReadAsync(_id,2)).Karma);
    }

    [Fact] public async Task CanonicalFailureNeverRetriesOrRestoresOldAuthority()
    {
        var calls=0;var failure=new InvalidOperationException("controlled uncertain canonical outcome");
        var thrown=await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.MutateVerifiedAsync<bool>(_id,1,(revision,token)=>
        { calls++;throw failure; }));
        Assert.Same(failure,thrown);Assert.Equal(1,calls);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.ReadAsync(_id,2));
    }

    [Theory][InlineData(true)][InlineData(false)] public async Task WrongVerifiedMutationIdentityCannotPublish(bool wrongAvatar)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.MutateVerifiedAsync(_id,1,(revision,token)=>
        {var next=Record(revision);if(wrongAvatar)next.Id=Guid.NewGuid().ToString("D");else next.Revision++;return Task.FromResult((true,next));}));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.ReadAsync(_id,2));
    }

    [Fact] public async Task CancellationAfterCanonicalMutationKeepsAuthorityUnavailable()
    {
        using var cancellation=new CancellationTokenSource();var calls=0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>_authority.MutateVerifiedAsync(_id,1,(revision,token)=>
        {calls++;cancellation.Cancel();return Task.FromResult((true,Record(revision)));},cancellation.Token));
        Assert.Equal(1,calls);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>_authority.ReadAsync(_id,2));
    }
}

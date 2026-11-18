using System.Reflection;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Providers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class AvatarProfileAtomicSqliteNativeTests
{
    private sealed class OwnedStore:IDisposable
    {
        public DirectoryInfo? Directory { get; }
        public string Connection { get; }
        public SQLLiteDBOASIS Provider { get; }
        public OwnedStore(int mode)
        {
            if(mode==0)Directory=System.IO.Directory.CreateTempSubdirectory("oasis-profile-sqlite-");
            Connection=mode switch
            {
                0=>$"Data Source={Path.Combine(Directory!.FullName,"profile.db")};Pooling=False",
                1=>"Data Source=:memory:;Pooling=False",
                2=>"Data Source=private-profile;Mode=Memory;Cache=Private;Pooling=False",
                3=>$"Data Source=shared-profile-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False",
                _=>throw new ArgumentOutOfRangeException(nameof(mode))
            };
            Provider=new SQLLiteDBOASIS(Connection);
        }
        public void Dispose()
        {
            Provider.Dispose();
            if(Directory!=null)
            {
                Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()),Path.GetFullPath(Directory.FullName));
                Assert.StartsWith("oasis-profile-sqlite-",Directory.Name);
                Directory.Delete(true);Assert.False(System.IO.Directory.Exists(Directory.FullName));
            }
        }
    }

    // Observe the provider's actual DSN; do not reconstruct a separate memory
    // store or replace its SDK client. This field never leaves the fixture.
    private static string NativeConnection(SQLLiteDBOASIS provider)
        =>(string)typeof(SQLLiteDBOASIS).GetField("_ownConnectionString",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;

    private static async Task Activate(SQLLiteDBOASIS provider)
    {var result=await provider.ActivateProviderAsync();Assert.False(result.IsError,result.Message);}

    private static async Task<(IAvatar Account,IAvatarDetail Detail)> Seed(SQLLiteDBOASIS provider)
    {
        var id=Guid.NewGuid();var username="profile-"+id.ToString("N");
        var account=await provider.SaveAvatarAsync(new Avatar{Id=id,Username=username,Email=username+"@example.test",
            Title="Before",FirstName="Before",LastName="Before",Password="stored-hash",RefreshToken="session",
            VerificationToken="verification",IsActive=true,CreatedDate=DateTime.UtcNow,Version=1,VersionId=Guid.NewGuid(),
            AvatarType=new EnumValue<AvatarType>(AvatarType.User)});
        var detail=await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=username,Email=username+"@example.test",
            Address="Before",ActiveQuestId=Guid.NewGuid(),ActiveObjectiveId=Guid.NewGuid(),Karma=37,XP=42,
            CreatedDate=DateTime.UtcNow,Version=1,VersionId=Guid.NewGuid(),
            Inventory=new List<IInventoryItem>{new InventoryItem{Id=Guid.NewGuid(),Name="keep-inventory",Quantity=3}}});
        Assert.False(account.IsError,account.Message);Assert.False(detail.IsError,detail.Message);
        return(account.Result,detail.Result);
    }

    private static AvatarProfileMutation Edit((IAvatar Account,IAvatarDetail Detail) opened,
        params (AvatarProfileProperty Field,string? Value)[] fields)
        =>new(opened.Account.Id,new(opened.Account.Version,opened.Account.VersionId),new(opened.Detail.Version,opened.Detail.VersionId),
            fields.Select(f=>new KeyValuePair<AvatarProfileProperty,string>(f.Field,f.Value!)));

    private static async Task Execute(SQLLiteDBOASIS provider,string sql,Guid? id=null)
    {
        using var connection=new SqliteConnection(NativeConnection(provider));await connection.OpenAsync();
        using var command=connection.CreateCommand();command.CommandText=sql;
        if(id.HasValue)command.Parameters.AddWithValue("$id",id.Value.ToString());
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> Raw(SQLLiteDBOASIS provider,Guid id)
    {
        using var connection=new SqliteConnection(NativeConnection(provider));await connection.OpenAsync();
        var result=new System.Text.StringBuilder();
        foreach(var table in new[]{"Avatar","AvatarDetail"})
        {
            using var command=connection.CreateCommand();command.CommandText=$"SELECT * FROM {table} WHERE Id=$id";
            command.Parameters.AddWithValue("$id",id.ToString());
            using var reader=await command.ExecuteReaderAsync();
            while(await reader.ReadAsync())
                for(var i=0;i<reader.FieldCount;i++)
                    result.Append(reader.GetName(i)).Append(':').Append(reader.GetFieldType(i).FullName).Append(':')
                        .Append(reader.GetValue(i) is byte[] bytes?Convert.ToHexString(bytes):reader.GetValue(i)).Append('\n');
        }
        return result.ToString();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task AtomicEditClearAndNoopPreserveNativeAndManagedFieldsAcrossFileAndMemory(int mode)
    {
        using var store=new OwnedStore(mode);var provider=store.Provider;await Activate(provider);
        var opened=await Seed(provider);
        await Execute(provider,"ALTER TABLE Avatar ADD COLUMN NativeProfileTest BLOB; ALTER TABLE AvatarDetail ADD COLUMN NativeGameTest BLOB; "
            +"UPDATE Avatar SET NativeProfileTest=X'010203'; UPDATE AvatarDetail SET NativeGameTest=X'040506';");
        var username=opened.Account.Username+"-edited";
        var saved=await provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit(opened,
            (AvatarProfileProperty.Title," After "),(AvatarProfileProperty.FirstName,"After"),(AvatarProfileProperty.LastName,"After"),
            (AvatarProfileProperty.Username,username),(AvatarProfileProperty.Email,username+"@example.test"),(AvatarProfileProperty.Address,"After")));
        Assert.False(saved.IsError,saved.Message);Assert.True(saved.IsSaved);Assert.Equal(2,saved.SavedCount);
        Assert.Equal("After",saved.Result.Title);Assert.Equal("After",saved.Result.Address);
        Assert.Equal(2,saved.Result.AvatarRevision.Version);Assert.Equal(2,saved.Result.DetailRevision.Version);
        var account=await provider.LoadAvatarAsync(opened.Account.Id);var detail=await provider.LoadAvatarDetailAsync(opened.Account.Id);
        Assert.False(account.IsError,account.Message);Assert.False(detail.IsError,detail.Message);
        Assert.Equal(username,account.Result.Username);Assert.Equal(username,detail.Result.Username);
        Assert.Equal(account.Result.Email,detail.Result.Email);Assert.Equal("stored-hash",account.Result.Password);
        Assert.Equal("session",account.Result.RefreshToken);Assert.Equal("verification",account.Result.VerificationToken);
        Assert.Equal(opened.Detail.ActiveQuestId,detail.Result.ActiveQuestId);Assert.Equal(opened.Detail.ActiveObjectiveId,detail.Result.ActiveObjectiveId);
        Assert.Equal(37,detail.Result.Karma);Assert.Equal(42,detail.Result.XP);
        var inventory=Assert.Single(detail.Result.Inventory);Assert.Equal("keep-inventory",inventory.Name);Assert.Equal(3,inventory.Quantity);
        Assert.Equal(opened.Account.VersionId,account.Result.PreviousVersionId);Assert.Equal(opened.Detail.VersionId,detail.Result.PreviousVersionId);
        Assert.Equal(account.Result.ModifiedDate,detail.Result.ModifiedDate);Assert.Equal(DateTimeKind.Utc,account.Result.ModifiedDate.Kind);
        Assert.Equal(opened.Account.Id,account.Result.ModifiedByAvatarId);Assert.Equal(opened.Account.Id,detail.Result.ModifiedByAvatarId);
        var raw=await Raw(provider,opened.Account.Id);Assert.Contains("010203",raw);Assert.Contains("040506",raw);
        var cleared=await provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit((account.Result,detail.Result),
            (AvatarProfileProperty.Title,null),(AvatarProfileProperty.FirstName,""),(AvatarProfileProperty.LastName,null),(AvatarProfileProperty.Address,null)));
        Assert.False(cleared.IsError,cleared.Message);Assert.True(cleared.IsSaved);
        Assert.Equal("",cleared.Result.Title);Assert.Equal("",cleared.Result.FirstName);Assert.Equal("",cleared.Result.LastName);Assert.Equal("",cleared.Result.Address);
        account=await provider.LoadAvatarAsync(opened.Account.Id);detail=await provider.LoadAvatarDetailAsync(opened.Account.Id);
        var beforeNoop=await Raw(provider,opened.Account.Id);
        var noop=await provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit((account.Result,detail.Result),
            (AvatarProfileProperty.Title,""),(AvatarProfileProperty.Address,"")));
        Assert.False(noop.IsError,noop.Message);Assert.False(noop.IsSaved);Assert.Equal(0,noop.SavedCount);
        Assert.Equal(beforeNoop,await Raw(provider,opened.Account.Id));
    }

    [Fact]
    public async Task ForeignStaleMissingAndVanishedFileAuthoritiesNeverPartiallyWriteOrCreateAReplacement()
    {
        using var store=new OwnedStore(0);var provider=store.Provider;await Activate(provider);var opened=await Seed(provider);
        var edit=Edit(opened,(AvatarProfileProperty.Title,"Changed"),(AvatarProfileProperty.Address,"Changed"));
        var foreign=await provider.ApplyAvatarProfileAsync(Guid.NewGuid(),edit);
        Assert.True(foreign.IsError);Assert.Equal(AvatarProfileErrors.Forbidden,foreign.ErrorCode);
        await Execute(provider,"UPDATE AvatarDetail SET XP=999,Version=2 WHERE Id=$id",opened.Account.Id);
        var before=await Raw(provider,opened.Account.Id);
        var stale=await provider.ApplyAvatarProfileAsync(opened.Account.Id,edit);
        Assert.True(stale.IsError);Assert.Equal(AvatarProfileErrors.Conflict,stale.ErrorCode);Assert.Null(stale.Result);
        Assert.Equal(before,await Raw(provider,opened.Account.Id));
        var deleted=await provider.DeleteAvatarDetailAsync(opened.Account.Id,softDelete:false);
        Assert.False(deleted.IsError,deleted.Message);
        var afterDelete=await Raw(provider,opened.Account.Id);
        var missing=await provider.ApplyAvatarProfileAsync(opened.Account.Id,edit);
        Assert.True(missing.IsError);Assert.Equal(AvatarProfileErrors.NotFound,missing.ErrorCode);
        Assert.Equal(afterDelete,await Raw(provider,opened.Account.Id));
        var path=Path.Combine(store.Directory!.FullName,"profile.db");var held=path+".owned-backup";
        File.Move(path,held);
        try
        {
            var vanished=await provider.ApplyAvatarProfileAsync(opened.Account.Id,edit);
            Assert.True(vanished.IsError);Assert.Equal(AvatarProfileErrors.StorageUnavailable,vanished.ErrorCode);
            Assert.False(File.Exists(path));Assert.False(vanished.IsSaved);
        }
        finally{File.Move(held,path);}
    }

    [Fact]
    public async Task SecondRowNativeTriggerAndUniqueIdentityRejectionsRollBackBothRecords()
    {
        using var store=new OwnedStore(0);var provider=store.Provider;await Activate(provider);var opened=await Seed(provider);
        var before=await Raw(provider,opened.Account.Id);
        await Execute(provider,"CREATE TRIGGER reject_profile_address BEFORE UPDATE OF Address ON AvatarDetail WHEN NEW.Address='reject-address' "
            +"BEGIN SELECT RAISE(ABORT,'owned profile second-row rejection'); END;");
        var failed=await provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit(opened,
            (AvatarProfileProperty.Title,"Must roll back"),(AvatarProfileProperty.Address,"reject-address")));
        Assert.True(failed.IsError);Assert.False(failed.IsSaved);Assert.Null(failed.Result);
        Assert.Equal(before,await Raw(provider,opened.Account.Id));
        var other=await Seed(provider);
        foreach(var field in new[]{AvatarProfileProperty.Username,AvatarProfileProperty.Email})
        {
            var value=field==AvatarProfileProperty.Username?other.Account.Username:other.Account.Email;
            var collision=await provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit(opened,
                (AvatarProfileProperty.Title,"Must roll back"),(AvatarProfileProperty.Address,"Must roll back"),(field,value)));
            Assert.True(collision.IsError);Assert.Equal(AvatarProfileErrors.IdentityConflict,collision.ErrorCode);
            Assert.False(collision.IsSaved);Assert.Null(collision.Result);Assert.Equal(before,await Raw(provider,opened.Account.Id));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ConcurrentNativeConnectionsCommitOneProfileAndRefuseTheStalePeer(int mode)
    {
        using var store=new OwnedStore(mode);var provider=store.Provider;await Activate(provider);var opened=await Seed(provider);
        using var start=new Barrier(2);
        async Task<NextGenSoftware.OASIS.Common.OASISResult<AvatarProfileSnapshot>> Write(string label)
        {
            if(!start.SignalAndWait(TimeSpan.FromSeconds(10)))throw new TimeoutException("Owned native contenders did not start.");
            return await provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit(opened,
                (AvatarProfileProperty.Title,label),(AvatarProfileProperty.Address,label)));
        }
        var results=await Task.WhenAll(Task.Run(()=>Write("First")),Task.Run(()=>Write("Second")));
        var winner=Assert.Single(results.Where(r=>!r.IsError));Assert.True(winner.IsSaved);
        var loser=Assert.Single(results.Where(r=>r.IsError));Assert.Equal(AvatarProfileErrors.Conflict,loser.ErrorCode);
        var account=await provider.LoadAvatarAsync(opened.Account.Id);var detail=await provider.LoadAvatarDetailAsync(opened.Account.Id);
        Assert.False(account.IsError,account.Message);Assert.False(detail.IsError,detail.Message);
        Assert.Equal(account.Result.Title,detail.Result.Address);Assert.Equal(winner.Result.Title,account.Result.Title);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MemoryOwnershipIsPrivateUnlessExplicitlySharedAndEndsAtLastOwner(bool shared)
    {
        var name="profile-memory-"+Guid.NewGuid().ToString("N");
        var connection=$"Data Source={name};Mode=Memory;Cache={(shared?"Shared":"Private")};Pooling=False";
        var first=new SQLLiteDBOASIS(connection);var second=new SQLLiteDBOASIS(connection);
        string canonical;
        try
        {
            await Activate(first);await Activate(second);var opened=await Seed(first);canonical=NativeConnection(first);
            var observed=await second.LoadAvatarAsync(opened.Account.Id);Assert.False(observed.IsError,observed.Message);
            Assert.Equal(shared,observed.Result!=null);
            await first.DeActivateProviderAsync();await Activate(first);
            Assert.Equal(opened.Account.Id,(await first.LoadAvatarAsync(opened.Account.Id)).Result.Id);
            first.Dispose();
            if(shared)Assert.Equal(opened.Account.Id,(await second.LoadAvatarAsync(opened.Account.Id)).Result.Id);
        }
        finally{first.Dispose();second.Dispose();}
        using var fresh=new SqliteConnection(canonical);await fresh.OpenAsync();
        using var query=fresh.CreateCommand();query.CommandText="SELECT COUNT(*) FROM sqlite_schema WHERE type='table' AND name='Avatar'";
        Assert.Equal(0L,(long)(await query.ExecuteScalarAsync())!);
    }
}

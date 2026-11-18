using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Providers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class AvatarProfileAtomicMongoNativeTests
{
    private const string Uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";

    private static async Task Owned(Func<MongoDBOASIS,IMongoDatabase,MongoClient,Task> action,bool history=false)
    {
        if(Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI")!=Uri)
            throw new InvalidOperationException("The exact owned loopback replica set is required.");
        var name="profile_atomic_"+Guid.NewGuid().ToString("N");
        var client=new MongoClient(Uri);
        Assert.DoesNotContain(name,await(await client.ListDatabaseNamesAsync()).ToListAsync());
        var connection=new MongoUrlBuilder(Uri){ApplicationName="oasis-profile-atomic-"+Guid.NewGuid().ToString("N")}.ToString();
        var provider=new MongoDBOASIS(connection,name){IsVersionControlEnabled=history};
        try
        {
            var activated=await provider.ActivateProviderAsync();
            Assert.False(activated.IsError,activated.Message);
            await action(provider,client.GetDatabase(name),client);
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            Assert.StartsWith("profile_atomic_",name);
            await client.DropDatabaseAsync(name);
            Assert.DoesNotContain(name,await(await client.ListDatabaseNamesAsync()).ToListAsync());
        }
    }

    private static async Task<(IAvatar Account,IAvatarDetail Detail)> Seed(MongoDBOASIS provider)
    {
        var id=Guid.NewGuid();var username="atomic-"+id.ToString("N");
        var account=await provider.SaveAvatarAsync(new Avatar
        {
            Id=id,Username=username,Email=username+"@example.test",Title="Original",FirstName="Before",LastName="Before",
            Password="stored-hash",VerificationToken="verification",RefreshToken="session",IsActive=true,
            CreatedDate=DateTime.UtcNow,Version=1,VersionId=Guid.NewGuid(),AvatarType=new EnumValue<AvatarType>(AvatarType.User)
        });
        var detail=await provider.SaveAvatarDetailAsync(new AvatarDetail
        {
            Id=id,Username=username,Email=username+"@example.test",Address="Original",ActiveQuestId=Guid.NewGuid(),
            ActiveObjectiveId=Guid.NewGuid(),Karma=37,XP=42,CreatedDate=DateTime.UtcNow,Version=1,VersionId=Guid.NewGuid()
        });
        Assert.False(account.IsError,account.Message);Assert.False(detail.IsError,detail.Message);
        return(account.Result,detail.Result);
    }

    private static AvatarProfileMutation Edit((IAvatar Account,IAvatarDetail Detail) opened,
        params (AvatarProfileProperty Field,string? Value)[] fields)
        =>new(opened.Account.Id,new(opened.Account.Version,opened.Account.VersionId),
            new(opened.Detail.Version,opened.Detail.VersionId),fields.Select(f=>new KeyValuePair<AvatarProfileProperty,string>(f.Field,f.Value!)));

    private static async Task<(BsonDocument Account,BsonDocument Detail)> Native(IMongoDatabase db)
        =>(await db.GetCollection<BsonDocument>("Avatar").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync(),
           await db.GetCollection<BsonDocument>("AvatarDetail").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AtomicEditableFieldsAndClearsPreserveNativeManagedStateAndAdvanceVersionsOnce(bool history)
        =>await Owned(async(provider,db,client)=>
        {
            var opened=await Seed(provider);
            await db.GetCollection<BsonDocument>("Avatar").UpdateOneAsync(FilterDefinition<BsonDocument>.Empty,
                new BsonDocument("$set",new BsonDocument("NativePrivateField","keep-account")));
            await db.GetCollection<BsonDocument>("AvatarDetail").UpdateOneAsync(FilterDefinition<BsonDocument>.Empty,
                new BsonDocument("$set",new BsonDocument("NativeGameField",new BsonDocument("score",71))));
            var username=opened.Account.Username+"-edited";
            var changed=await provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit(opened,
                (AvatarProfileProperty.Title," Changed "),(AvatarProfileProperty.FirstName,"After"),
                (AvatarProfileProperty.LastName,"After"),(AvatarProfileProperty.Address,"After"),
                (AvatarProfileProperty.Username,username),(AvatarProfileProperty.Email,username+"@example.test")));
            Assert.False(changed.IsError,changed.Message);Assert.True(changed.IsSaved);Assert.Equal(2,changed.SavedCount);
            Assert.Equal("Changed",changed.Result.Title);Assert.Equal("After",changed.Result.Address);
            Assert.Equal(2,changed.Result.AvatarRevision.Version);Assert.Equal(2,changed.Result.DetailRevision.Version);
            var currentAccount=await provider.LoadAvatarAsync(opened.Account.Id);
            var currentDetail=await provider.LoadAvatarDetailAsync(opened.Account.Id);
            Assert.False(currentAccount.IsError,currentAccount.Message);Assert.False(currentDetail.IsError,currentDetail.Message);
            Assert.Equal("stored-hash",currentAccount.Result.Password);Assert.Equal("session",currentAccount.Result.RefreshToken);
            Assert.Equal("verification",currentAccount.Result.VerificationToken);
            Assert.Equal(opened.Detail.ActiveQuestId,currentDetail.Result.ActiveQuestId);
            Assert.Equal(opened.Detail.ActiveObjectiveId,currentDetail.Result.ActiveObjectiveId);
            Assert.Equal(37,currentDetail.Result.Karma);Assert.Equal(42,currentDetail.Result.XP);
            Assert.Equal(username,currentAccount.Result.Username);Assert.Equal(username,currentDetail.Result.Username);
            Assert.Equal(currentAccount.Result.Email,currentDetail.Result.Email);
            Assert.Equal(opened.Account.VersionId,currentAccount.Result.PreviousVersionId);
            Assert.Equal(opened.Detail.VersionId,currentDetail.Result.PreviousVersionId);
            var raw=await Native(db);
            Assert.Equal("keep-account",raw.Account["NativePrivateField"].AsString);
            Assert.Equal(71,raw.Detail["NativeGameField"]["score"].AsInt32);
            if(history)
            {
                var previous=await provider.LoadAvatarAsync(opened.Account.Id,1);
                Assert.False(previous.IsError,previous.Message);Assert.Equal("Original",previous.Result.Title);
            }
            var now=(currentAccount.Result,currentDetail.Result);
            var cleared=await provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit(now,
                (AvatarProfileProperty.Title,null),(AvatarProfileProperty.Address,null)));
            Assert.False(cleared.IsError,cleared.Message);Assert.True(cleared.IsSaved);
            Assert.Equal("",cleared.Result.Title);Assert.Equal("",cleared.Result.Address);
            var clearAccount=await provider.LoadAvatarAsync(opened.Account.Id);
            var clearDetail=await provider.LoadAvatarDetailAsync(opened.Account.Id);
            var beforeNoop=await Native(db);
            var noop=await provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit((clearAccount.Result,clearDetail.Result),
                (AvatarProfileProperty.Title,""),(AvatarProfileProperty.Address,"")));
            Assert.False(noop.IsError,noop.Message);Assert.False(noop.IsSaved);Assert.Equal(0,noop.SavedCount);
            Assert.Equal(beforeNoop,await Native(db));
        },history);

    [Fact]
    public async Task StaleGameRevisionForeignActorAndMissingDetailNeverPartiallyWrite()
        =>await Owned(async(provider,db,client)=>
        {
            var opened=await Seed(provider);
            var request=Edit(opened,(AvatarProfileProperty.Title,"Changed"),(AvatarProfileProperty.Address,"Changed"));
            var foreign=await provider.ApplyAvatarProfileAsync(Guid.NewGuid(),request);
            Assert.True(foreign.IsError);Assert.Equal(AvatarProfileErrors.Forbidden,foreign.ErrorCode);
            // Real native game mutation made after the profile was opened.
            await db.GetCollection<BsonDocument>("AvatarDetail").UpdateOneAsync(FilterDefinition<BsonDocument>.Empty,
                new BsonDocument("$set",new BsonDocument{{"XP",999},{"Version",2},{"VersionId",Guid.NewGuid().ToString("D")}}));
            var before=await Native(db);
            var stale=await provider.ApplyAvatarProfileAsync(opened.Account.Id,request);
            Assert.True(stale.IsError);Assert.False(stale.IsSaved);Assert.Null(stale.Result);
            Assert.Equal(AvatarProfileErrors.Conflict,stale.ErrorCode);Assert.Equal(before,await Native(db));
            await db.GetCollection<BsonDocument>("AvatarDetail").DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
            var missing=await provider.ApplyAvatarProfileAsync(opened.Account.Id,request);
            Assert.True(missing.IsError);Assert.Equal(AvatarProfileErrors.NotFound,missing.ErrorCode);
            Assert.Equal(before.Account,await db.GetCollection<BsonDocument>("Avatar").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync());
        });

    [Fact]
    public async Task NativeSecondRecordRejectionRollsBackFirstRecord()
        =>await Owned(async(provider,db,client)=>
        {
            var opened=await Seed(provider);var before=await Native(db);
            await db.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","AvatarDetail"},
                {"validator",new BsonDocument("Address",new BsonDocument("$ne","reject-this-address"))},
                {"validationLevel","strict"},{"validationAction","error"}});
            var result=await provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit(opened,
                (AvatarProfileProperty.Title,"Must roll back"),(AvatarProfileProperty.Address,"reject-this-address")));
            Assert.True(result.IsError);Assert.False(result.IsSaved);Assert.Null(result.Result);
            Assert.Equal(before,await Native(db));
        });

    [Fact]
    public async Task ConcurrentProfilesAndUniqueLoginIndexesPreventLostEditsAndIdentityCollisions()
        =>await Owned(async(provider,db,client)=>
        {
            var opened=await Seed(provider);
            var edits=new[]{Edit(opened,(AvatarProfileProperty.Title,"First"),(AvatarProfileProperty.Address,"First")),
                Edit(opened,(AvatarProfileProperty.Title,"Second"),(AvatarProfileProperty.Address,"Second"))};
            var results=await Task.WhenAll(edits.Select(edit=>provider.ApplyAvatarProfileAsync(opened.Account.Id,edit)));
            Assert.Single(results.Where(r=>!r.IsError));Assert.Single(results.Where(r=>r.ErrorCode==AvatarProfileErrors.Conflict));
            var raw=await Native(db);Assert.Equal(raw.Account["Title"],raw.Detail["Address"]);
            var current=await provider.LoadAvatarAsync(opened.Account.Id);var detail=await provider.LoadAvatarDetailAsync(opened.Account.Id);
            var other=await Seed(provider);
            var beforeAccount=await db.GetCollection<BsonDocument>("Avatar").Find(new BsonDocument("Username",current.Result.Username)).SingleAsync();
            var beforeDetail=await db.GetCollection<BsonDocument>("AvatarDetail").Find(new BsonDocument("Username",detail.Result.Username)).SingleAsync();
            foreach(var field in new[]{AvatarProfileProperty.Username,AvatarProfileProperty.Email})
            {
                var value=field==AvatarProfileProperty.Username?other.Account.Username:other.Account.Email;
                var collision=await provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit((current.Result,detail.Result),
                    (AvatarProfileProperty.Title,"Must roll back"),(AvatarProfileProperty.Address,"Must roll back"),(field,value)));
                Assert.True(collision.IsError);Assert.Equal(AvatarProfileErrors.IdentityConflict,collision.ErrorCode);
                Assert.False(collision.IsSaved);Assert.Null(collision.Result);
            }
            Assert.Equal(beforeAccount,await db.GetCollection<BsonDocument>("Avatar").Find(new BsonDocument("Username",current.Result.Username)).SingleAsync());
            Assert.Equal(beforeDetail,await db.GetCollection<BsonDocument>("AvatarDetail").Find(new BsonDocument("Username",detail.Result.Username)).SingleAsync());
        });

    [Fact]
    public async Task UnknownNativeCommitRetainsRealTransactionIdentityWithoutReportingSuccess()
        =>await Owned(async(provider,db,client)=>
        {
            var opened=await Seed(provider);
            var admin=client.GetDatabase("admin");
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","alwaysOn"},
                {"data",new BsonDocument{{"failCommands",new BsonArray{"commitTransaction"}},
                    {"appName",new MongoUrl(provider.ConnectionString).ApplicationName},
                    {"writeConcernError",new BsonDocument{{"code",64},{"errmsg","owned profile post-commit acknowledgement fault"}}}}}});
            try
            {
                var result=await provider.ApplyAvatarProfileAsync(opened.Account.Id,Edit(opened,
                    (AvatarProfileProperty.Title,"Unacknowledged"),(AvatarProfileProperty.Address,"Unacknowledged")));
                Assert.True(result.IsError);Assert.Equal(AvatarProfileErrors.CommitUnknown,result.ErrorCode);
                Assert.False(result.IsSaved);Assert.Null(result.Result);
                var evidence=Assert.IsAssignableFrom<INativeMutationReconciliationRequired>(result.Exception);
                Assert.False(string.IsNullOrWhiteSpace(evidence.OriginalTransactionId));
                var native=Assert.IsType<MongoTransactionReconciliationException>(result.Exception);
                Assert.True(BsonDocument.Parse(native.SessionId).Contains("id"));
                Assert.True(native.TransactionNumber>=0);
                Assert.IsType<MongoWriteConcernException>(native.InnerException);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","off"}});
            }
            var committed=await Native(db);
            Assert.Equal("Unacknowledged",committed.Account["Title"].AsString);
            Assert.Equal("Unacknowledged",committed.Detail["Address"].AsString);
            Assert.Equal(2,committed.Account["Version"].AsInt32);Assert.Equal(2,committed.Detail["Version"].AsInt32);
        });
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Xunit;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class CosmicIdentityTombstoneTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task IdentityReadsResolveRetainedTombstonesAcrossGraphHints(bool synchronous,bool byKey)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        const string name="cosmic_identity_tombstone_fixture";var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri,name);var reader=new Provider(uri,name);var complete=false;var actor=Guid.NewGuid();
        var root=new Holon {Id=Guid.NewGuid(),Name="owned tombstone root",Children=new List<IHolon>{
            new Holon {Id=Guid.NewGuid(),Name="owned tombstone child",Children=new List<IHolon>{new Holon{Id=Guid.NewGuid(),Name="owned tombstone grandchild"}}}}};
        IEnumerable<IHolon> Nodes(IHolon item){yield return item;foreach(var child in item.Children)foreach(var node in Nodes(child))yield return node;}
        async Task<IHolon> Write(IHolon value,ConditionalHolonOperation operation,bool create=false)
        {
            var conditions=Nodes(value).Select(h=>new HolonWriteCondition(h.Id,create ? null:CosmicConcurrency.GetProviderRevisionId(h,ProviderType.MongoDBOASIS)));
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),actor,ProviderType.MongoDBOASIS,conditions),operation,new[]{value});
            var result=synchronous ? provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
            Assert.False(result.IsError,result.Message+result.Exception);return Assert.Single(result.Result);
        }
        async Task<OASISResult<IHolon>> Read(string key,bool children,bool recursive,int depth,bool fromProvider,int version=0)
        {
            if(byKey)return synchronous ? reader.LoadHolon(key,children,recursive,depth,loadChildrenFromProvider:fromProvider,version:version)
                :await reader.LoadHolonAsync(key,children,recursive,depth,loadChildrenFromProvider:fromProvider,version:version);
            return synchronous ? reader.LoadHolon(root.Id,children,recursive,depth,loadChildrenFromProvider:fromProvider,version:version)
                :await reader.LoadHolonAsync(root.Id,children,recursive,depth,loadChildrenFromProvider:fromProvider,version:version);
        }
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);Assert.False((await reader.ActivateProviderAsync()).IsError);
            var initial=await Write(root,ConditionalHolonOperation.Save,true);var soft=await Write(initial,ConditionalHolonOperation.SoftDelete);
            var key=soft.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];
            foreach(var fromProvider in new[]{false,true})
            foreach(var children in new[]{false,true})
            foreach(var recursive in new[]{false,true})
            foreach(var depth in new[]{0,1,2})
            {
                var result=await Read(key,children,recursive,depth,fromProvider);Assert.False(result.IsError,result.Message+result.Exception);Assert.NotNull(result.Result);
                Assert.Equal(soft.VersionId,result.Result.VersionId);Assert.Equal(soft.CreatedDate,result.Result.CreatedDate);Assert.Equal(soft.DeletedDate,result.Result.DeletedDate);
                Assert.Equal(actor,result.Result.DeletedByAvatarId);Assert.Equal(2,result.Result.Version);
                Assert.Equal(children ? 1:0,result.Result.Children.Count());
                if(children)
                {
                    var child=Assert.Single(result.Result.Children);Assert.Equal(soft.Children.Single().VersionId,child.VersionId);Assert.Equal(soft.DeletedDate,child.DeletedDate);
                    Assert.Equal(recursive && depth!=1 ? 1:0,child.Children.Count());
                }
            }
            var exact=await Read(key,false,false,0,false,2);Assert.NotNull(exact.Result);Assert.Equal(soft.VersionId,exact.Result.VersionId);
            var missingVersion=await Read(key,false,false,0,false,1);Assert.Null(missingVersion.Result);Assert.Equal(OASISResultErrorCodes.RecordNotFound,missingVersion.ErrorCode);
            await Write(soft,ConditionalHolonOperation.HardDelete);
            foreach(var linked in new[]{false,true})Assert.Null((await Read(key,true,true,0,linked)).Result);
            Assert.Equal(0L,await database.GetCollection<MongoDB.Bson.BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<MongoDB.Bson.BsonDocument>.Empty));
            complete=true;
        }
        finally
        {
            await reader.DeActivateProviderAsync();await provider.DeActivateProviderAsync();
            if(complete)await client.DropDatabaseAsync(name);
        }
    }
}

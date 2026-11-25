using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    // Regression: production held legacy rows sharing one HolonId. The whole-collection listing
    // correctly refuses them as ambiguous, but owner-scoped listings (the sandbox sweep) must filter
    // natively and keep working for every other owner.
    private static async Task VerifyMongoOwnerScopedListingAsync(MongoDBOASIS mongo,OASISHyperDrive router,bool synchronous,IMongoCollection<NativeHolon> records)
    {
        var owner=Guid.NewGuid();var other=Guid.NewGuid();
        var mine=new Holon {Id=Guid.NewGuid(),IsActive=true,HolonType=HolonType.Holon,Name="owner scoped listing"};
        var legacy=new Holon {Id=Guid.NewGuid(),IsActive=true,HolonType=HolonType.Holon,Name="legacy duplicate identity"};
        var ids=new[] {mine.Id,legacy.Id};
        try
        {
            foreach(var node in new[] {mine,legacy}) (await mongo.SaveHolonAsync(node,saveChildren:false)).IsError.Should().BeFalse();
            (await records.UpdateOneAsync(row=>row.HolonId==mine.Id,Builders<NativeHolon>.Update.Set(row=>row.CreatedByAvatarId,owner.ToString("D")))).MatchedCount.Should().Be(1);
            (await records.UpdateOneAsync(row=>row.HolonId==legacy.Id,Builders<NativeHolon>.Update.Set(row=>row.CreatedByAvatarId,other.ToString("D")))).MatchedCount.Should().Be(1);
            var raw=records.Database.GetCollection<BsonDocument>(records.CollectionNamespace.CollectionName);
            var copy=await raw.Find(new BsonDocument("HolonId",legacy.Id.ToString())).FirstOrDefaultAsync()
                ?? await raw.Find(Builders<BsonDocument>.Filter.Eq("HolonId",new BsonBinaryData(legacy.Id,GuidRepresentation.Standard))).FirstAsync();
            copy["_id"]=ObjectId.GenerateNewId();await raw.InsertOneAsync(copy);

            var unscoped=synchronous ? mongo.LoadAllHolons(loadChildren:false):await mongo.LoadAllHolonsAsync(loadChildren:false);
            unscoped.IsError.Should().BeTrue("the legacy duplicate identity is genuinely ambiguous");

            var direct=synchronous ? mongo.LoadAllHolons(owner,false,HolonType.All,loadChildren:false)
                :await mongo.LoadAllHolonsAsync(owner,false,HolonType.All,loadChildren:false);
            direct.IsError.Should().BeFalse(direct.Exception?.ToString() ?? direct.Message);
            direct.Result.Select(holon=>holon.Id).Should().Equal(mine.Id);

            var request=new StorageOperationRequest {Operation="LoadAllHolons",HolonType=HolonType.All,LoadChildren=false,
                AvatarId=owner,IncludePublic=false,PreferredProvider=ProviderType.MongoDBOASIS};
            var routed=synchronous ? router.RouteRequest<IEnumerable<IHolon>>(request):await router.RouteRequestAsync<IEnumerable<IHolon>>(request);
            routed.IsError.Should().BeFalse(routed.Exception?.ToString() ?? routed.Message);
            routed.Result.Select(holon=>holon.Id).Should().Equal(mine.Id);
        }
        finally {await records.DeleteManyAsync(row=>ids.Contains(row.HolonId));}
    }
}

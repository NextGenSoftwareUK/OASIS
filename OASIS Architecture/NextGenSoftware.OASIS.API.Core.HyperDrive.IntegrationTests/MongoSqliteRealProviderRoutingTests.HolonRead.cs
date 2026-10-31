using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoHolonReadsAreNonMutatingAsync(MongoDBOASIS fault,MongoDBOASIS healthy,SQLLiteDBOASIS sqlite,
        OASISDNA dna,IMongoDatabase admin,string app,bool synchronous,IMongoCollection<NativeHolon> records)
    {
        var holon=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="read must not repair stored keys"};
        var saved=await healthy.SaveHolonAsync(holon,saveChildren:false);saved.IsError.Should().BeFalse(saved.Message);
        var key=saved.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];
        await records.UpdateOneAsync(row=>row.HolonId==holon.Id,Builders<NativeHolon>.Update.Set(row=>row.ProviderUniqueStorageKey,
            new Dictionary<ProviderType,string>{{ProviderType.MongoDBOASIS,"deliberately stale stored metadata"},{ProviderType.SQLLiteDBOASIS,"preserve unrelated provider key"}}));
        var before=(await records.Find(row=>row.HolonId==holon.Id).SingleAsync()).ToBsonDocument();
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
        manager.RegisterProvider(fault);manager.RegisterProvider(sqlite);
        var providers=new[] {new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
        manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);BsonDocument? configured=null;BsonDocument? stopped=null;
        try
        {
            configured=await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","alwaysOn"},
                {"data",new BsonDocument {{"failCommands",new BsonArray {"update","insert","delete","findAndModify"}},{"appName",app},{"errorCode",2}}}});
            foreach(var byKey in new[] {false,true})
            {
                var request=new StorageOperationRequest {Operation=byKey ? "LoadHolonByProviderKey":"LoadHolon",HolonId=holon.Id,ProviderKey=key,LoadChildren=false,PreferredProvider=ProviderType.MongoDBOASIS};
                var read=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                read.IsError.Should().BeFalse(read.Message);read.IsLoaded.Should().BeTrue();read.Result.Id.Should().Be(holon.Id);
                read.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS].Should().Be(key);
                read.Result.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS].Should().Be("preserve unrelated provider key");
            }
            (await records.Find(row=>row.HolonId==holon.Id).SingleAsync()).ToBsonDocument().ToBson().Should().Equal(before.ToBson());
        }
        finally
        {
            stopped=await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
            await records.DeleteManyAsync(row=>row.HolonId==holon.Id);
        }
        stopped!["count"].ToInt64().Should().Be(configured!["count"].ToInt64(),"a read must not even attempt a repair write");
        await VerifyMongoHolonGraphReadsAsync(healthy,router,synchronous,records);
        await VerifyMongoCollectionGraphReadsAsync(healthy,sqlite,router,synchronous,records);
        foreach(var byKey in new[] {false,true})
        {
            var stale=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="stale SQLite read must not replace healthy Mongo absence"};
            var missingKey=ObjectId.GenerateNewId().ToString();stale.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS]=missingKey;
            (await sqlite.SaveHolonAsync(stale,saveChildren:false)).IsError.Should().BeFalse();
            var count=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            var request=new StorageOperationRequest {Operation=byKey ? "LoadHolonByProviderKey":"LoadHolon",HolonId=stale.Id,ProviderKey=missingKey,LoadChildren=false,PreferredProvider=ProviderType.MongoDBOASIS};
            var missing=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
            missing.IsError.Should().BeFalse();missing.IsLoaded.Should().BeFalse();missing.Result.Should().BeNull();missing.Exception.Should().BeNull();missing.ErrorCode.Should().Be(OASISResultErrorCodes.RecordNotFound);
            (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(count);
            (await sqlite.LoadHolonAsync(stale.Id,loadChildren:false)).Result.Name.Should().Be(stale.Name);
            (await sqlite.DeleteHolonAsync(stale.Id)).IsError.Should().BeFalse();
        }
    }
}

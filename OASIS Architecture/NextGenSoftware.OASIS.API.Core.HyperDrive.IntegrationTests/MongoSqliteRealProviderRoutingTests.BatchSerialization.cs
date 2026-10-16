using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoBatchSerializationFailuresAsync(MongoDBOASIS fault,MongoDBOASIS healthy,SQLLiteDBOASIS sqlite,
        OASISDNA dna,IMongoDatabase admin,string app,bool synchronous,IMongoCollection<NativeHolon> records)
    {
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
        manager.RegisterProvider(fault);manager.RegisterProvider(sqlite);
        var providers=new[] {new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
        manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);var raw=records.Database.GetCollection<BsonDocument>("Holon");
        foreach(var operation in new[] {"Import","SaveHolons","graph"})
        foreach(var updating in new[] {false,true})
        {
            var good=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="retained native first"};
            var bad=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="retained native second"};
            var ids=new[] {good.Id,bad.Id};
            try
            {
                foreach(var node in new[] {good,bad})
                {
                    if(updating) (await healthy.SaveHolonAsync(node,saveChildren:false)).IsError.Should().BeFalse();
                    (await sqlite.SaveHolonAsync(new Holon {Id=node.Id,IsActive=true,Name="stale SQL copy"},saveChildren:false)).IsError.Should().BeFalse();
                }
                var nativeBefore=new Dictionary<Guid,byte[]>();
                foreach(var node in await records.Find(row=>ids.Contains(row.HolonId)).ToListAsync())
                    nativeBefore[node.HolonId]=(await raw.Find(new BsonDocument("_id",ObjectId.Parse(node.Id))).SingleAsync()).ToBson();
                good.Name="first candidate must not be written";bad.MetaData["unsupported"]=new Action(()=>{});
                if(operation=="graph") {good.Children=new List<IHolon>{bad};bad.ParentHolonId=good.Id;}
                var configured=await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    {"configureFailPoint","failCommand"},{"mode","alwaysOn"},
                    {"data",new BsonDocument {{"failCommands",new BsonArray {"insert","update"}},{"appName",app},{"errorCode",2}}}});
                var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                var request=new StorageOperationRequest {Operation=operation=="graph" ? "SaveHolon":operation,
                    Payload=operation=="graph" ? good:new List<IHolon>{good,bad},SaveChildren=operation=="graph",ChildrenFromProvider=operation=="graph",PreferredProvider=ProviderType.MongoDBOASIS};
                if(operation=="Import") AssertMongoBatchPayloadFailure(synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request), canonicalPreflight:true);
                else if(operation=="SaveHolons") AssertMongoBatchPayloadFailure(synchronous ? router.RouteRequest<IEnumerable<IHolon>>(request):await router.RouteRequestAsync<IEnumerable<IHolon>>(request), canonicalPreflight:true);
                else AssertMongoBatchPayloadFailure(synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request), canonicalPreflight:true);
                var stopped=await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
                stopped["count"].ToInt64().Should().Be(configured["count"].ToInt64(),"no native insert/update may be attempted before all payloads serialize");
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before);
                var retained=await records.Find(row=>ids.Contains(row.HolonId)).ToListAsync();
                retained.Count.Should().Be(updating ? 2:0);
                foreach(var row in retained) (await raw.Find(new BsonDocument("_id",ObjectId.Parse(row.Id))).SingleAsync()).ToBson().Should().Equal(nativeBefore[row.HolonId]);
                foreach(var id in ids) (await sqlite.LoadHolonAsync(id,loadChildren:false)).Result.Name.Should().Be("stale SQL copy");
                if(updating)
                {
                    bad.MetaData.Remove("unsupported");
                    var key=retained.Single(row=>row.HolonId==good.Id).Id;
                    await VerifyMongoUnknownFieldRetentionAsync(records.Database,"Holon",key,async ()=>
                    {
                        if(operation=="Import") AssertMongoBatchSuccess(synchronous ? fault.Import(new[] {good,bad}):await fault.ImportAsync(new[] {good,bad}));
                        else if(operation=="SaveHolons") AssertMongoBatchSuccess(synchronous ? fault.SaveHolons(new[] {good,bad},saveChildren:false):await fault.SaveHolonsAsync(new[] {good,bad},saveChildren:false));
                        else AssertMongoBatchSuccess(synchronous ? fault.SaveHolon(good,saveChildrenOnProvider:true):await fault.SaveHolonAsync(good,saveChildrenOnProvider:true));
                    });
                    (await records.Find(row=>row.HolonId==good.Id).SingleAsync()).Name.Should().Be(good.Name);
                }
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
                await records.DeleteManyAsync(row=>ids.Contains(row.HolonId));
                foreach(var id in ids) if((await sqlite.LoadHolonAsync(id,loadChildren:false)).Result!=null) (await sqlite.DeleteHolonAsync(id)).IsError.Should().BeFalse();
            }
        }
    }

    private static void AssertMongoBatchPayloadFailure<T>(OASISResult<T> result, bool canonicalPreflight=false)
    {
        result.IsError.Should().BeTrue();result.IsSaved.Should().BeFalse();result.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
        // Graph snapshots now reject unsupported payloads before entering the BSON transaction boundary.
        // Routed import now shares canonical staging; direct BSON preparation remains separately supported.
        if(canonicalPreflight) result.Exception.Should().BeOfType<Newtonsoft.Json.JsonSerializationException>();
        else
        {
            result.Exception.Should().BeAssignableTo<BsonSerializationException>();
            result.Exception!.GetBaseException().Should().BeOfType<NotSupportedException>();
        }
        Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception).Should().BeFalse();
    }

    private static void AssertMongoBatchSuccess<T>(OASISResult<T> result)
    {
        result.IsError.Should().BeFalse(result.Exception?.ToString() ?? result.Message);result.IsWarning.Should().BeFalse();
        result.IsSaved.Should().BeTrue();result.SavedCount.Should().Be(2);
    }
}

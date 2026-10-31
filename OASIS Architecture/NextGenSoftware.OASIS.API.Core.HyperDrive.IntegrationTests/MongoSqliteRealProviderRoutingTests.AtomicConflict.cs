using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
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
    private static async Task VerifyMongoAtomicWriteConflictAsync(MongoDBOASIS fault,MongoDBOASIS healthy,SQLLiteDBOASIS sqlite,
        OASISDNA dna,IMongoDatabase admin,string app,bool synchronous,IMongoCollection<NativeHolon> records)
    {
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
        manager.RegisterProvider(fault);manager.RegisterProvider(sqlite);
        var providers=new[] {new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
        manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);
        foreach(var operation in new[] {"Import","SaveHolons","graph"})
        {
            var first=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="native first"};
            var second=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="native second"};
            var ids=new[] {first.Id,second.Id};Task<OASISResult<object>>? pending=null;
            try
            {
                foreach(var node in new[] {first,second})
                {
                    (await healthy.SaveHolonAsync(node,saveChildren:false)).IsError.Should().BeFalse();
                    (await sqlite.SaveHolonAsync(new Holon {Id=node.Id,IsActive=true,Name="stale SQL copy"},saveChildren:false)).IsError.Should().BeFalse();
                }
                first.Name="stale replacement";second.Name="candidate second";
                if(operation=="graph") {first.Children=new List<IHolon>{second};second.ParentHolonId=first.Id;}
                var configured=await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    {"configureFailPoint","failCommand"},{"mode",new BsonDocument("times",1)},
                    {"data",new BsonDocument {{"failCommands",new BsonArray {"update"}},{"appName",app},{"blockConnection",true},{"blockTimeMS",1500}}}});
                var request=new StorageOperationRequest {Operation=operation=="graph" ? "SaveHolon":operation,
                    Payload=operation=="graph" ? first:new List<IHolon>{first,second},SaveChildren=operation=="graph",ChildrenFromProvider=operation=="graph",PreferredProvider=ProviderType.MongoDBOASIS};
                async Task<OASISResult<object>> Execute()
                {
                    if(operation=="Import") return NormalizeMongoAtomicOutcome(synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request));
                    if(operation=="SaveHolons") return NormalizeMongoAtomicOutcome(synchronous ? router.RouteRequest<IEnumerable<IHolon>>(request):await router.RouteRequestAsync<IEnumerable<IHolon>>(request));
                    return NormalizeMongoAtomicOutcome(synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request));
                }
                var sqlBefore=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                pending=synchronous ? Task.Run(Execute):Execute();
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"waitForFailPoint","failCommand"},{"timesEntered",configured["count"].ToInt64()+1},{"maxTimeMS",5000}});
                await records.UpdateOneAsync(row=>row.HolonId==first.Id,Builders<NativeHolon>.Update.Set(row=>row.Name,"concurrent winner"));
                var result=await pending;
                result.IsError.Should().BeTrue();result.IsSaved.Should().BeFalse();result.ErrorCode.Should().Be(StorageLifecycleConflictException.ErrorCode);
                ContainsMongoWriteConflict(result.Exception).Should().BeTrue("retain the actual native write-conflict cause, including cleanup aggregates");
                NativeMutationReconciliation.IsRequired(result.Exception).Should().BeFalse("this server-aborted write never reached commit dispatch");
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(sqlBefore);
                (await records.Find(row=>row.HolonId==first.Id).SingleAsync()).Name.Should().Be("concurrent winner");
                (await records.Find(row=>row.HolonId==second.Id).SingleAsync()).Name.Should().Be("native second");
                foreach(var id in ids) (await sqlite.LoadHolonAsync(id,loadChildren:false)).Result.Name.Should().Be("stale SQL copy");
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
                if(pending!=null) await pending;
                await records.DeleteManyAsync(row=>ids.Contains(row.HolonId));
                foreach(var id in ids) if((await sqlite.LoadHolonAsync(id,loadChildren:false)).Result!=null) (await sqlite.DeleteHolonAsync(id)).IsError.Should().BeFalse();
            }
        }
    }

    private static OASISResult<object> NormalizeMongoAtomicOutcome<T>(OASISResult<T> source)
    {
        var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<T,object>(source);result.Result=source.Result!;return result;
    }

    private static bool ContainsMongoWriteConflict(Exception? cause)
        => cause!=null && (cause is MongoCommandException command && command.Code==112
            || cause is MongoWriteException write && write.WriteError?.Code==112
            || cause is AggregateException group && group.InnerExceptions.Any(ContainsMongoWriteConflict)
            || ContainsMongoWriteConflict(cause.InnerException));
}

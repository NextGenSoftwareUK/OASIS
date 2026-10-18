using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoHolonDeleteOutcomesAsync(MongoDBOASIS mongo,SQLLiteDBOASIS sqlite,
        ProviderManager manager,OASISHyperDrive router,bool synchronous)
    {
        var priorFailover=manager.IsAutoFailOverEnabled;
        manager.IsAutoFailOverEnabled=true;
        manager.SetAndReplaceAutoFailOverListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError.Should().BeFalse();
        try
        {
            foreach(var byKey in new[] {false,true})
            {
                var holon=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="stale SQLite copy must survive Mongo absence"};
                var saved=await mongo.SaveHolonAsync(holon,saveChildren:false);saved.IsError.Should().BeFalse(saved.Message);
                var key=saved.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];
                (await sqlite.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
                var deleted=byKey ? synchronous ? mongo.DeleteHolon(key):await mongo.DeleteHolonAsync(key)
                    : synchronous ? mongo.DeleteHolon(holon.Id):await mongo.DeleteHolonAsync(holon.Id);
                deleted.IsError.Should().BeFalse(deleted.Exception?.ToString() ?? deleted.Message);deleted.IsDeleted.Should().BeTrue();deleted.DeletedCount.Should().Be(1);
                deleted.Result.Id.Should().Be(holon.Id);
                (await mongo.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Should().BeNull();
                var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                var request=new StorageOperationRequest {Operation=byKey ? "DeleteHolonByProviderKey":"DeleteHolon",HolonId=holon.Id,ProviderKey=key,SoftDelete=false,PreferredProvider=ProviderType.MongoDBOASIS};
                var absent=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                absent.IsError.Should().BeTrue();absent.IsDeleted.Should().BeFalse();absent.Result.Should().BeNull();absent.Exception.Should().BeNull();
                absent.ErrorCode.Should().Be(OASISResultErrorCodes.RecordNotFound);
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before);
                (await sqlite.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Name.Should().Be(holon.Name);
                (await sqlite.DeleteHolonAsync(holon.Id)).IsError.Should().BeFalse();
            }
        }
        finally {manager.IsAutoFailOverEnabled=priorFailover;}
    }

    private static async Task VerifyMongoHolonDeleteWriteConcernAsync(MongoDBOASIS fault,MongoDBOASIS healthy,SQLLiteDBOASIS sqlite,
        OASISDNA dna,IMongoDatabase admin,string app,bool synchronous,IMongoCollection<API.Providers.MongoDBOASIS.Entities.Holon> records)
    {
        await VerifyMongoHolonReadsAreNonMutatingAsync(fault,healthy,sqlite,dna,admin,app,synchronous,records);
        await VerifyMongoHolonSaveWriteConcernAsync(fault,healthy,sqlite,dna,admin,app,synchronous,records);
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
        manager.RegisterProvider(fault);manager.RegisterProvider(sqlite);
        var providers=new[] {new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
        manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);
        foreach(var byKey in new[] {false,true})
        {
            var holon=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="retained SQLite copy after Mongo write concern fault"};
            var saved=await healthy.SaveHolonAsync(holon,saveChildren:false);saved.IsError.Should().BeFalse(saved.Message);
            var key=saved.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];
            holon.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS]=key;
            (await sqlite.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    {"configureFailPoint","failCommand"},{"mode","alwaysOn"},
                    {"data",new BsonDocument {{"failCommands",new BsonArray {"findAndModify"}},{"appName",app},
                        {"writeConcernError",new BsonDocument {{"code",64},{"errmsg","owned post-command write concern fault"}}}}}});
                var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                var request=new StorageOperationRequest {Operation=byKey ? "DeleteHolonByProviderKey":"DeleteHolon",HolonId=holon.Id,ProviderKey=key,SoftDelete=false,PreferredProvider=ProviderType.MongoDBOASIS};
                var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                result.IsError.Should().BeTrue();result.IsDeleted.Should().BeFalse();result.Result.Should().BeNull();
                var failure=result.Exception.Should().BeOfType<MongoMutationReconciliationException>().Which;
                failure.RecordId.Should().Be(holon.Id);failure.OriginalTransactionId.Should().BeEmpty();failure.InnerException.Should().BeOfType<MongoWriteConcernException>();
                Core.Helpers.NativeMutationReconciliation.IsRequired(failure).Should().BeTrue();
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before);
                (await healthy.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Should().BeNull("the actual Mongo delete ran before write-concern failure");
                (await sqlite.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Name.Should().Be(holon.Name);
            }
            finally {await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});}
            (await sqlite.DeleteHolonAsync(holon.Id)).IsError.Should().BeFalse();
        }
        var racing=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="before concurrent update"};
        (await healthy.SaveHolonAsync(racing,saveChildren:false)).IsError.Should().BeFalse();
        Task<OASISResult<IHolon>>? pending=null;
        try
        {
            var configured=await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                {"configureFailPoint","failCommand"},{"mode",new BsonDocument("times",1)},
                {"data",new BsonDocument {{"failCommands",new BsonArray {"findAndModify"}},{"appName",app},{"blockConnection",true},{"blockTimeMS",1500}}}});
            var request=new StorageOperationRequest {Operation="DeleteHolon",HolonId=racing.Id,SoftDelete=false,PreferredProvider=ProviderType.MongoDBOASIS};
            var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            pending=synchronous ? Task.Run(()=>router.RouteRequest<IHolon>(request)):router.RouteRequestAsync<IHolon>(request);
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"waitForFailPoint","failCommand"},{"timesEntered",configured["count"].ToInt64()+1},{"maxTimeMS",5000}});
            // Native server has entered findAndModify; the repository already captured its expected snapshot.
            await records.UpdateOneAsync(row=>row.HolonId==racing.Id,Builders<API.Providers.MongoDBOASIS.Entities.Holon>.Update.Set(row=>row.Name,"concurrent update must survive"));
            var rejected=await pending;rejected.IsError.Should().BeTrue();rejected.IsDeleted.Should().BeFalse();
            rejected.ErrorCode.Should().Be(Core.Helpers.StorageLifecycleConflictException.ErrorCode);
            Core.Helpers.NativeMutationReconciliation.IsRequired(rejected.Exception).Should().BeFalse("the native command returned a known no-match result");
            (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before);
            (await records.Find(row=>row.HolonId==racing.Id).SingleAsync()).Name.Should().Be("concurrent update must survive");
        }
        finally
        {
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
            if(pending!=null) await pending;
            await records.DeleteManyAsync(row=>row.HolonId==racing.Id);
        }
        var duplicate=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="ambiguous fixture"};
        (await healthy.SaveHolonAsync(duplicate,saveChildren:false)).IsError.Should().BeFalse();
        try
        {
            var second=await records.Find(row=>row.HolonId==duplicate.Id).SingleAsync();second.Id=ObjectId.GenerateNewId().ToString();
            var rejected=await FluentActions.Awaiting(()=>records.InsertOneAsync(second)).Should().ThrowAsync<MongoWriteException>();
            rejected.Which.WriteError.Code.Should().Be(11000,"the existing canonical public identity index must prevent ambiguity");
            (await records.CountDocumentsAsync(row=>row.HolonId==duplicate.Id)).Should().Be(1);
            (await records.Find(row=>row.HolonId==duplicate.Id).SingleAsync()).Name.Should().Be(duplicate.Name);
        }
        finally {await records.DeleteManyAsync(row=>row.HolonId==duplicate.Id);}
    }
}

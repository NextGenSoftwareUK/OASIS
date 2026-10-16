using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyMongoHolonSaveWriteConcernAsync(MongoDBOASIS fault,MongoDBOASIS healthy,SQLLiteDBOASIS sqlite,
        OASISDNA dna,IMongoDatabase admin,string app,bool synchronous,IMongoCollection<NativeHolon> records)
    {
        await VerifyMongoSerializationFailuresAsync(healthy,sqlite,dna,synchronous,records.Database);
        await VerifyMongoAtomicWriteConflictAsync(fault,healthy,sqlite,dna,admin,app,synchronous,records);
        await VerifyMongoAvatarTransactionConflictsAsync(fault,healthy,sqlite,dna,admin,app,synchronous,records.Database);
        await VerifyMongoAvatarCommitAcknowledgementsAsync(fault,healthy,sqlite,dna,admin,app,synchronous,records.Database);
        await VerifyMongoBatchSerializationFailuresAsync(fault,healthy,sqlite,dna,admin,app,synchronous,records);
        await VerifyMongoAvatarSaveWriteConcernAsync(fault,healthy,sqlite,dna,admin,app,synchronous,records.Database);
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
        manager.RegisterProvider(fault);manager.RegisterProvider(sqlite);
        var providers=new[] {new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
        manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);
        foreach(var updating in new[] {false,true})
        {
            var holon=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="SQLite copy must survive uncertain Mongo save"};
            if(updating) (await healthy.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
            (await sqlite.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    {"configureFailPoint","failCommand"},{"mode","alwaysOn"},
                    {"data",new BsonDocument {{"failCommands",new BsonArray {updating ? "update":"insert"}},{"appName",app},
                        {"writeConcernError",new BsonDocument {{"code",64},{"errmsg","owned save post-command write concern fault"}}}}}});
                holon.Name="Mongo native save completed before acknowledgement fault";
                var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                var request=new StorageOperationRequest {Operation="SaveHolon",Payload=holon,SaveChildren=false,PreferredProvider=ProviderType.MongoDBOASIS};
                var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                result.IsError.Should().BeTrue();result.IsSaved.Should().BeFalse();result.Result.Should().BeNull();
                var failure=result.Exception.Should().BeOfType<MongoMutationReconciliationException>().Which;
                failure.RecordId.Should().Be(holon.Id);failure.OriginalTransactionId.Should().BeEmpty();failure.InnerException.Should().NotBeNull();
                var native=failure.InnerException;
                (native is MongoWriteConcernException || native is MongoWriteException write && write.WriteConcernError?.Code==64)
                    .Should().BeTrue("the actual native write-concern cause must survive conversion/routing");
                Core.Helpers.NativeMutationReconciliation.IsRequired(failure).Should().BeTrue();
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before);
                (await records.Find(row=>row.HolonId==holon.Id).SingleAsync()).Name.Should().Be(holon.Name,"the real native write completed");
                (await sqlite.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Name.Should().Be("SQLite copy must survive uncertain Mongo save");
                if(updating)
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        {"configureFailPoint","failCommand"},{"mode",new BsonDocument("times",1)},
                        {"data",new BsonDocument {{"failCommands",new BsonArray {"update"}},{"appName",app},{"errorCode",2}}}});
                    var retained=holon.Name;holon.Name="definitely rejected update must not execute";
                    var rejected=synchronous ? fault.SaveHolon(holon,saveChildren:false):await fault.SaveHolonAsync(holon,saveChildren:false);
                    rejected.IsError.Should().BeTrue();rejected.Result.Should().BeNull();
                    rejected.Exception.Should().BeOfType<MongoCommandException>().Which.Code.Should().Be(2);
                    Core.Helpers.NativeMutationReconciliation.IsRequired(rejected.Exception).Should().BeFalse();
                    (await records.Find(row=>row.HolonId==holon.Id).SingleAsync()).Name.Should().Be(retained);
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
                    var key=(await records.Find(row=>row.HolonId==holon.Id).SingleAsync()).Id;
                    await VerifyMongoUnknownFieldRetentionAsync(records.Database,"Holon",key,async ()=>
                    {
                        var saved=synchronous ? healthy.SaveHolon(holon,saveChildren:false):await healthy.SaveHolonAsync(holon,saveChildren:false);
                        saved.IsError.Should().BeFalse(saved.Message);
                    });
                }
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
                await records.DeleteManyAsync(row=>row.HolonId==holon.Id);
                (await sqlite.DeleteHolonAsync(holon.Id)).IsError.Should().BeFalse();
            }
        }
        foreach(var mutation in new[] {"rename","softDelete","addField"})
        {
            var holon=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="before concurrent change"};
            (await healthy.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
            (await sqlite.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
            Task<Common.OASISResult<IHolon>>? pending=null;
            try
            {
                var configured=await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    {"configureFailPoint","failCommand"},{"mode",new BsonDocument("times",1)},
                    {"data",new BsonDocument {{"failCommands",new BsonArray {"update"}},{"appName",app},{"blockConnection",true},{"blockTimeMS",1500}}}});
                holon.Name="stale replacement must not overwrite concurrent change";
                var request=new StorageOperationRequest {Operation="SaveHolon",Payload=holon,SaveChildren=false,PreferredProvider=ProviderType.MongoDBOASIS};
                var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                pending=synchronous ? Task.Run(()=>router.RouteRequest<IHolon>(request)):router.RouteRequestAsync<IHolon>(request);
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"waitForFailPoint","failCommand"},{"timesEntered",configured["count"].ToInt64()+1},{"maxTimeMS",5000}});
                if(mutation=="addField")
                {
                    var key=(await records.Find(row=>row.HolonId==holon.Id).SingleAsync()).Id;
                    await records.Database.GetCollection<BsonDocument>("Holon").UpdateOneAsync(new BsonDocument("_id",ObjectId.Parse(key)),Builders<BsonDocument>.Update.Set("concurrent_extra",true));
                }
                else
                {
                    var change=Builders<NativeHolon>.Update.Set(row=>row.Name,"concurrent change must survive");
                    if(mutation=="softDelete") change=change.Set(row=>row.DeletedDate,DateTime.UtcNow).Set(row=>row.IsActive,false);
                    await records.UpdateOneAsync(row=>row.HolonId==holon.Id,change);
                }
                var result=await pending;
                result.IsError.Should().BeTrue();result.IsSaved.Should().BeFalse();result.Result.Should().BeNull();
                result.ErrorCode.Should().Be(Core.Helpers.StorageLifecycleConflictException.ErrorCode);
                Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception).Should().BeFalse("CAS returned known no-match, not uncertain completion");
                (manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(before);
                var retained=await records.Find(row=>row.HolonId==holon.Id).SingleAsync();
                retained.Name.Should().Be(mutation=="addField" ? "before concurrent change":"concurrent change must survive");
                (retained.DeletedDate!=DateTime.MinValue).Should().Be(mutation=="softDelete");
                if(mutation=="addField")
                    (await records.Database.GetCollection<BsonDocument>("Holon").Find(new BsonDocument("_id",ObjectId.Parse(retained.Id))).SingleAsync())["concurrent_extra"].AsBoolean.Should().BeTrue();
                (await sqlite.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Name.Should().Be("before concurrent change");
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
                if(pending!=null) await pending;
                await records.DeleteManyAsync(row=>row.HolonId==holon.Id);
                (await sqlite.DeleteHolonAsync(holon.Id)).IsError.Should().BeFalse();
            }
        }
    }

    private static async Task VerifyMongoUnknownFieldRetentionAsync(IMongoDatabase database,string collection,string key,Func<Task> save)
    {
        var raw=database.GetCollection<BsonDocument>(collection);
        var filter=new BsonDocument("_id",ObjectId.Parse(key));
        var future=new BsonDocument {{"version",7},{"opaque",new BsonDocument {{"$ne",false}}}};
        await raw.UpdateOneAsync(filter,Builders<BsonDocument>.Update.Set("future_schema",future));
        await save();
        var persisted=await raw.Find(filter).SingleAsync();
        persisted.Contains("future_schema").Should().BeTrue("a mapped update must not erase unrecognized top-level schema fields");
        persisted["future_schema"].Should().Be(future);
    }
}

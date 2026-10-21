using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public sealed partial class OracleNativeAcceptanceTests
{
    [TestMethod]
    public async Task ReconcileKnownProjectionAssertionFixtures()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        Assert.AreEqual("7B6F8CA06B6046ED32464DC794050B5A162DF97A71AE9A55BE2DC71A2F51DBB5",Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(work,"oracle-projection-assertion-failed.trx")))));
        var names=new[]{"canonical-metadata.pending.json","canonical-records.pending.json","conditional-parent-projection-0.pending.json","conditional-parent-projection-1.pending.json","conditional-parent-projection-2.pending.json","graph-three-level.pending.json"};
        CollectionAssert.AreEquivalent(names,Directory.GetFiles(work,"*.pending.json").Select(Path.GetFileName).ToArray());
        await using var connection=new OracleConnection(Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING"));await connection.OpenAsync();
        var rows=new List<(string Table,Guid Id,string Json)>();
        async Task Observe(string table,Guid id,string expectedName,Guid? corruptParent=null){
            var json=await Read(connection,table,id);Assert.IsNotNull(json);
            using var parsed=JsonDocument.Parse(json);Assert.AreEqual(id,parsed.RootElement.GetProperty("Id").GetGuid());
            Assert.AreEqual(expectedName,parsed.RootElement.GetProperty(table=="OASIS_HOLONS"?"Name":"Username").GetString());
            if(corruptParent.HasValue){
                Assert.AreEqual(Guid.Empty,parsed.RootElement.GetProperty("ParentHolonId").GetGuid());
                await using var parent=new OracleCommand("SELECT PARENT_HOLON_ID FROM OASIS_HOLONS WHERE ID=:Id",connection){BindByName=true};parent.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
                Assert.AreEqual(corruptParent.Value,new Guid((byte[])(await parent.ExecuteScalarAsync())!));
            }
            rows.Add((table,id,json));
        }
        foreach(var name in names){
            using var journal=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(work,name)));var data=journal.RootElement;
            if(name.StartsWith("conditional-parent",StringComparison.Ordinal)){
                Assert.AreEqual("before-owned-projection-fixture",data.GetProperty("state").GetString());
                await AssertReceiptAbsent(connection,data.GetProperty("operationId").GetGuid());
                await Observe("OASIS_HOLONS",data.GetProperty("id").GetGuid(),"owned-parent-projection",data.GetProperty("wrongParent").GetGuid());
            }else if(name=="canonical-metadata.pending.json"){
                Assert.AreEqual("before-canonical-metadata-fixture",data.GetProperty("state").GetString());
                await Observe("OASIS_HOLONS",data.GetProperty("id").GetGuid(),"owned-canonical-metadata-current");
            }else if(name=="canonical-records.pending.json"){
                Assert.AreEqual("before-native-canonical-records",data.GetProperty("state").GetString());var id=data.GetProperty("id").GetGuid();var expected="owned-oracle-"+id.ToString("N");Assert.AreEqual(expected,data.GetProperty("name").GetString());
                foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS","OASIS_HOLONS"})await Observe(table,id,expected);
                await Observe("OASIS_HOLONS",data.GetProperty("childId").GetGuid(),"owned independently current child");
            }else{
                Assert.AreEqual("before-native-three-level-graph",data.GetProperty("state").GetString());var ids=data.GetProperty("ids").EnumerateArray().Select(item=>item.GetGuid()).ToArray();Assert.AreEqual(3,ids.Length);
                for(var index=0;index<ids.Length;index++)await Observe("OASIS_HOLONS",ids[index],new[]{"owned graph root","owned graph child","owned graph leaf"}[index]);
            }
        }
        Assert.AreEqual(11,rows.Count);Assert.AreEqual(11,rows.Select(row=>(row.Table,row.Id)).Distinct().Count());
        await using(var transaction=(OracleTransaction)await connection.BeginTransactionAsync()){
            foreach(var row in rows){
                await using var delete=new OracleCommand($"DELETE FROM {row.Table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",connection){BindByName=true,Transaction=transaction};
                delete.Parameters.Add("Id",OracleDbType.Raw).Value=row.Id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=row.Json;Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());
            }
            await transaction.CommitAsync();
        }
        foreach(var row in rows)Assert.IsNull(await Read(connection,row.Table,row.Id));
        foreach(var name in names)File.Delete(Path.Combine(work,name));
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ChangedLaterGraphCallerDoesNotReachNativeWrite(bool synchronous)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"later-caller-{synchronous}.pending.json");Assert.IsFalse(File.Exists(journal));
        var first=new Holon{Id=Guid.NewGuid(),Name="owned first graph caller"};
        var second=new Holon{Id=Guid.NewGuid(),Name="owned later graph caller"};
        var provider=new LostCommitProvider(connection){IsVersionControlEnabled=true};
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            Assert.IsNull(await Read(verifier,"OASIS_HOLONS",first.Id));Assert.IsNull(await Read(verifier,"OASIS_HOLONS",second.Id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{first=first.Id,second=second.Id,state="before-owned-later-caller-write"}));file.Flush(true);
            }
            provider.AfterCommit=()=>second.Name="newer caller edit";
            var result=synchronous?provider.SaveHolons(new[]{first,second}):await provider.SaveHolonsAsync(new[]{first,second});
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);
            Assert.IsInstanceOfType<OraclePartialMutationException>(result.Exception);
            CollectionAssert.AreEqual(new[]{first.Id},((OraclePartialMutationException)result.Exception).AcknowledgedRecordIds.ToArray());
            Assert.AreEqual(1,provider.CommitCalls);Assert.AreEqual(1,first.Version);
            Assert.AreEqual("newer caller edit",second.Name);Assert.AreEqual(0,second.Version);
            Assert.IsNull(await Read(verifier,"OASIS_HOLONS",second.Id));
            var committed=await Read(verifier,"OASIS_HOLONS",first.Id);Assert.IsNotNull(committed);
            using(var parsed=JsonDocument.Parse(committed))Assert.AreEqual("owned first graph caller",parsed.RootElement.GetProperty("Name").GetString());
            provider.AfterCommit=null;
            await using var cleanup=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
            cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=first.Id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=committed;
            Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",first.Id));
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task ConditionalMutationRejectsCorruptParentProjection(int operation)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"conditional-parent-projection-{operation}.pending.json");Assert.IsFalse(File.Exists(journal));
        var root=new Holon{Id=Guid.NewGuid(),Name="owned-parent-projection"};var wrongParent=Guid.NewGuid();var operationId=Guid.NewGuid();var actor=Guid.NewGuid();
        var authority=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS;var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            Assert.IsNull(await Read(verifier,"OASIS_HOLONS",root.Id));await AssertReceiptAbsent(verifier,operationId);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id=root.Id,wrongParent,operationId,actor,operation,state="before-owned-projection-fixture"}));file.Flush(true);}
            var seed=await provider.SaveHolonAsync(root,saveChildren:false);Assert.IsFalse(seed.IsError,seed.Exception?.ToString());var original=await Read(verifier,"OASIS_HOLONS",root.Id);Assert.IsNotNull(original);
            await using(var corrupt=new OracleCommand("UPDATE OASIS_HOLONS SET PARENT_HOLON_ID=:Parent WHERE ID=:Id AND PARENT_HOLON_ID IS NULL",verifier){BindByName=true}){corrupt.Parameters.Add("Parent",OracleDbType.Raw).Value=wrongParent.ToByteArray();corrupt.Parameters.Add("Id",OracleDbType.Raw).Value=root.Id.ToByteArray();Assert.AreEqual(1,await corrupt.ExecuteNonQueryAsync());}
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operationId,actor,authority,new[]{new HolonWriteCondition(root.Id,CosmicConcurrency.GetProviderRevisionId(root,authority))}),(ConditionalHolonOperation)operation,new[]{root},saveChildren:false);
            var rejected=await provider.ExecuteConditionalHolonWriteAsync(request);Assert.IsTrue(rejected.IsError);Assert.IsNull(rejected.Result);Assert.IsFalse(rejected.IsSaved);Assert.IsFalse(rejected.IsDeleted);Assert.IsInstanceOfType<InvalidDataException>(rejected.Exception);StringAssert.Contains(rejected.Exception.Message,"native projection is inconsistent");
            Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(rejected.Exception));await AssertReceiptAbsent(verifier,operationId);Assert.AreEqual(original,await Read(verifier,"OASIS_HOLONS",root.Id));
            // Restore only this known synthetic corruption after independent original-payload readback.
            await using(var restore=new OracleCommand("UPDATE OASIS_HOLONS SET PARENT_HOLON_ID=NULL WHERE ID=:Id AND PARENT_HOLON_ID=:Parent",verifier){BindByName=true}){restore.Parameters.Add("Id",OracleDbType.Raw).Value=root.Id.ToByteArray();restore.Parameters.Add("Parent",OracleDbType.Raw).Value=wrongParent.ToByteArray();Assert.AreEqual(1,await restore.ExecuteNonQueryAsync());}
            Assert.IsFalse((await provider.DeleteHolonWithSoftDeleteAsync(root.Id,false)).IsError);Assert.IsNull(await Read(verifier,"OASIS_HOLONS",root.Id));await AssertReceiptAbsent(verifier,operationId);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    [TestMethod]
    public async Task ConditionalPersistenceOwnsCreationAndModificationAuditFields()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;var journal=Path.Combine(work,"conditional-audit.pending.json");Assert.IsFalse(File.Exists(journal));
        var authority=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS;var forged=Guid.NewGuid();var root=new Holon{Id=Guid.NewGuid(),Name="owned-audit-create",CreatedDate=new DateTime(2100,1,1,0,0,0,DateTimeKind.Utc),CreatedByAvatarId=forged,ModifiedDate=new DateTime(2101,1,1),ModifiedByAvatarId=forged,CreatedProviderType=new NextGenSoftware.Utilities.EnumValue<NextGenSoftware.OASIS.API.Core.Enums.ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.EthereumOASIS)};
        var create=ConditionalCreate(root,Guid.NewGuid());var nextOperation=Guid.NewGuid();var modifier=Guid.NewGuid();var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();Assert.IsNull(await Read(verifier,"OASIS_HOLONS",root.Id));await AssertReceiptAbsent(verifier,create.Write.OperationId);await AssertReceiptAbsent(verifier,nextOperation);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id=root.Id,createOperation=create.Write.OperationId,nextOperation,actor=create.Write.ActorId,modifier,state="before-conditional-audit-fixture"}));file.Flush(true);}
            var first=await provider.ExecuteConditionalHolonWriteAsync(create);Assert.IsFalse(first.IsError,first.Exception?.ToString());var ack=first.Result.Single();Assert.AreEqual(create.Write.ActorId,ack.CreatedByAvatarId);Assert.AreNotEqual(root.CreatedDate,ack.CreatedDate);Assert.AreEqual(authority,ack.CreatedProviderType.Value);Assert.AreEqual(authority,ack.InstanceSavedOnProviderType.Value);Assert.AreEqual(DateTime.MinValue,ack.ModifiedDate);Assert.AreEqual(Guid.Empty,ack.ModifiedByAvatarId);
            var updated=new Holon{Id=root.Id,Name="owned-audit-update",CreatedDate=root.CreatedDate,CreatedByAvatarId=forged,CreatedProviderType=root.CreatedProviderType,ModifiedDate=root.ModifiedDate,ModifiedByAvatarId=forged};
            var update=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(nextOperation,modifier,authority,new[]{new HolonWriteCondition(root.Id,CosmicConcurrency.GetProviderRevisionId(ack,authority))}),ConditionalHolonOperation.Save,new[]{updated});
            var second=await provider.ExecuteConditionalHolonWriteAsync(update);Assert.IsFalse(second.IsError,second.Exception?.ToString());var committed=second.Result.Single();Assert.AreEqual(ack.CreatedDate,committed.CreatedDate);Assert.AreEqual(create.Write.ActorId,committed.CreatedByAvatarId);Assert.AreEqual(authority,committed.CreatedProviderType.Value);Assert.AreEqual(modifier,committed.ModifiedByAvatarId);Assert.AreNotEqual(updated.ModifiedDate,committed.ModifiedDate);
            var raw=await Read(verifier,"OASIS_HOLONS",root.Id);Assert.IsNotNull(raw);var native=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>(raw);Assert.AreEqual(ack.CreatedDate,native.CreatedDate);Assert.AreEqual(create.Write.ActorId,native.CreatedByAvatarId);Assert.AreEqual(modifier,native.ModifiedByAvatarId);
            var replay=await provider.ExecuteConditionalHolonWriteAsync(update);Assert.IsFalse(replay.IsError,replay.Exception?.ToString());Assert.AreEqual(committed.ModifiedDate,replay.Result.Single().ModifiedDate);Assert.AreEqual(forged,root.CreatedByAvatarId);Assert.AreEqual(forged,updated.CreatedByAvatarId);
            var removed=await provider.DeleteHolonWithSoftDeleteAsync(root.Id,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",root.Id));
            foreach(var request in new[]{create,update}){await using var cleanup=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash",verifier){BindByName=true};cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=request.Write.OperationId.ToByteArray();cleanup.Parameters.Add("Hash",OracleDbType.Varchar2).Value=ConditionalHolonPlan.Create(request,authority).Fingerprint;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());await AssertReceiptAbsent(verifier,request.Write.OperationId);}File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ConditionalRootOnlyDeletionLeavesExcludedChildUnchanged(bool hard)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;var journal=Path.Combine(work,$"conditional-root-delete-{hard}.pending.json");Assert.IsFalse(File.Exists(journal));
        var root=new Holon{Id=Guid.NewGuid(),Name="owned-root-only-delete"};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="owned-excluded-child"};root.Children.Add(child);var operation=Guid.NewGuid();var provider=new LostCommitProvider(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();foreach(var id in new[]{root.Id,child.Id})Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));await AssertReceiptAbsent(verifier,operation);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=new[]{root.Id,child.Id},operation,hard,state="before-root-only-conditional-delete"}));file.Flush(true);}
            var seed=await provider.SaveHolonAsync(root);Assert.IsFalse(seed.IsError,seed.Exception?.ToString());var originalChild=await Read(verifier,"OASIS_HOLONS",child.Id);Assert.IsNotNull(originalChild);var commits=provider.CommitCalls;
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,Guid.NewGuid(),NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS,new[]{new HolonWriteCondition(root.Id,CosmicConcurrency.GetProviderRevisionId(root,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS))}),hard?ConditionalHolonOperation.HardDelete:ConditionalHolonOperation.SoftDelete,new[]{root},saveChildren:false);
            var deleted=await provider.ExecuteConditionalHolonWriteAsync(request);Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());Assert.IsTrue(deleted.IsDeleted);Assert.IsTrue(deleted.Result.Single().IsDeleted);Assert.AreEqual(2,deleted.Result.Single().Version);Assert.AreEqual(0,deleted.Result.Single().Children.Count);Assert.AreEqual(commits+1,provider.CommitCalls);
            var replay=await provider.ExecuteConditionalHolonWriteAsync(request);Assert.IsFalse(replay.IsError,replay.Exception?.ToString());Assert.AreEqual(deleted.Result.Single().VersionId,replay.Result.Single().VersionId);Assert.AreEqual(commits+1,provider.CommitCalls);Assert.AreEqual(originalChild,await Read(verifier,"OASIS_HOLONS",child.Id));Assert.IsNull((await provider.LoadHolonAsync(root.Id,loadChildren:false)).Result);Assert.AreEqual(1,root.Version);Assert.AreEqual(1,child.Version);
            if(hard)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",root.Id));else{var raw=await Read(verifier,"OASIS_HOLONS",root.Id);Assert.IsNotNull(raw);var tombstone=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>(raw);Assert.IsTrue(tombstone.IsDeleted);Assert.AreEqual(2,tombstone.Version);}
            var childLoad=await provider.LoadHolonAsync(child.Id,loadChildren:false);Assert.IsFalse(childLoad.IsError,childLoad.Exception?.ToString());Assert.AreEqual(root.Id,childLoad.Result.ParentHolonId);
            foreach(var id in hard?new[]{child.Id}:new[]{child.Id,root.Id}){var removed=await provider.DeleteHolonWithSoftDeleteAsync(id,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));}
            await using(var cleanup=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash",verifier){BindByName=true}){cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();cleanup.Parameters.Add("Hash",OracleDbType.Varchar2).Value=ConditionalHolonPlan.Create(request,request.Write.Authority).Fingerprint;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());}await AssertReceiptAbsent(verifier,operation);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ConditionalMixedRootsCommitAtomicallyOrRejectAll(bool synchronous)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;var journal=Path.Combine(work,$"conditional-mixed-roots-{synchronous}.pending.json");Assert.IsFalse(File.Exists(journal));
        var existing=new Holon{Id=Guid.NewGuid(),Name="owned-mixed-existing"};var created=new Holon{Id=Guid.NewGuid(),Name="owned-mixed-create"};var failedOperation=Guid.NewGuid();var successfulOperation=Guid.NewGuid();var actor=Guid.NewGuid();var authority=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS;var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();foreach(var id in new[]{created.Id,existing.Id})Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));await AssertReceiptAbsent(verifier,failedOperation);await AssertReceiptAbsent(verifier,successfulOperation);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=new[]{created.Id,existing.Id},failedOperation,successfulOperation,actor,synchronous,state="before-mixed-conditional-roots"}));file.Flush(true);}
            var seed=await provider.SaveHolonAsync(existing,saveChildren:false);Assert.IsFalse(seed.IsError,seed.Exception?.ToString());var original=await Read(verifier,"OASIS_HOLONS",existing.Id);Assert.IsNotNull(original);var updated=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(existing);updated.Name="owned-mixed-updated";
            IConditionalHolonStorageProvider conditional=provider;
            ConditionalHolonWriteRequest Request(Guid operation,Guid? revision)=>new(new ConditionalHolonWrite(operation,actor,authority,new[]{new HolonWriteCondition(created.Id,null),new HolonWriteCondition(existing.Id,revision)}),ConditionalHolonOperation.Save,new[]{created,updated},saveChildren:false);
            var rejected=synchronous?conditional.ExecuteConditionalHolonWrite(Request(failedOperation,Guid.NewGuid())):await conditional.ExecuteConditionalHolonWriteAsync(Request(failedOperation,Guid.NewGuid()));Assert.IsTrue(rejected.IsError);Assert.IsInstanceOfType<CosmicConcurrencyException>(rejected.Exception);Assert.AreEqual(existing.Id,((CosmicConcurrencyException)rejected.Exception).Conflicts.Single().HolonId);Assert.IsNull(await Read(verifier,"OASIS_HOLONS",created.Id));Assert.AreEqual(original,await Read(verifier,"OASIS_HOLONS",existing.Id));await AssertReceiptAbsent(verifier,failedOperation);
            var request=Request(successfulOperation,CosmicConcurrency.GetProviderRevisionId(existing,authority));var accepted=synchronous?conditional.ExecuteConditionalHolonWrite(request):await conditional.ExecuteConditionalHolonWriteAsync(request);Assert.IsFalse(accepted.IsError,accepted.Exception?.ToString());Assert.IsTrue(accepted.IsSaved);
            var replay=synchronous?conditional.ExecuteConditionalHolonWrite(request):await conditional.ExecuteConditionalHolonWriteAsync(request);Assert.IsFalse(replay.IsError,replay.Exception?.ToString());
            foreach(var result in new[]{accepted,replay}){var roots=result.Result.ToArray();Assert.AreEqual(2,roots.Length);Assert.AreEqual(created.Id,roots[0].Id);Assert.AreEqual(existing.Id,roots[1].Id);Assert.AreEqual(1,roots[0].Version);Assert.AreEqual(2,roots[1].Version);Assert.AreEqual(updated.Name,roots[1].Name);}
            foreach(var id in new[]{created.Id,existing.Id}){var raw=await Read(verifier,"OASIS_HOLONS",id);Assert.IsNotNull(raw);using var parsed=JsonDocument.Parse(raw);Assert.AreEqual(id==created.Id?1:2,parsed.RootElement.GetProperty("Version").GetInt32());}
            Assert.AreEqual(0,created.Version);Assert.AreEqual(1,updated.Version);
            foreach(var id in new[]{created.Id,existing.Id}){var removed=await provider.DeleteHolonWithSoftDeleteAsync(id,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));}
            await using(var cleanup=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash",verifier){BindByName=true}){cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=successfulOperation.ToByteArray();cleanup.Parameters.Add("Hash",OracleDbType.Varchar2).Value=ConditionalHolonPlan.Create(request,authority).Fingerprint;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());}await AssertReceiptAbsent(verifier,successfulOperation);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)]
    public async Task ConditionalGraphOptionsPersistAndReplayOnlySelectedNodes(int mode)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;var journal=Path.Combine(work,$"conditional-options-{mode}.pending.json");Assert.IsFalse(File.Exists(journal));
        var root=new Holon{Id=Guid.NewGuid(),Name="owned-conditional-options-root"};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="owned-conditional-options-child"};var leaf=new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id,Name="owned-conditional-options-leaf"};root.Children.Add(child);child.Children.Add(leaf);var rows=new[]{root,child,leaf};var selected=mode==0?1:mode==3?3:2;var operation=Guid.NewGuid();
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,Guid.NewGuid(),NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS,rows.Take(selected).Select(r=>new HolonWriteCondition(r.Id,null))),ConditionalHolonOperation.Save,new[]{root},saveChildren:mode!=0,recursive:mode!=1,maxChildDepth:mode==2?1:0);
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();foreach(var row in rows)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));await AssertReceiptAbsent(verifier,operation);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=rows.Select(r=>r.Id).ToArray(),operation,mode,selected,state="before-conditional-options-fixture"}));file.Flush(true);}
            var saved=await provider.ExecuteConditionalHolonWriteAsync(request);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);
            var replay=await provider.ExecuteConditionalHolonWriteAsync(request);Assert.IsFalse(replay.IsError,replay.Exception?.ToString());
            foreach(var outcome in new[]{saved,replay}){var ack=outcome.Result.Single();Assert.AreEqual(root.Id,ack.Id);Assert.AreEqual(1,ack.Version);Assert.AreEqual(selected==1?0:1,ack.Children.Count);if(selected>1){Assert.AreEqual(child.Id,ack.Children.Single().Id);Assert.AreEqual(selected==3?1:0,ack.Children.Single().Children.Count);if(selected==3)Assert.AreEqual(leaf.Id,ack.Children.Single().Children.Single().Id);}}
            for(var index=0;index<rows.Length;index++){
                var raw=await Read(verifier,"OASIS_HOLONS",rows[index].Id);Assert.AreEqual(0,rows[index].Version);Assert.IsNull(CosmicConcurrency.GetProviderRevisionId(rows[index],request.Write.Authority));
                if(index>=selected)Assert.IsNull(raw);else{Assert.IsNotNull(raw);using var parsed=JsonDocument.Parse(raw);Assert.AreEqual(1,parsed.RootElement.GetProperty("Version").GetInt32());Assert.AreEqual(rows[index].ParentHolonId,parsed.RootElement.GetProperty("ParentHolonId").GetGuid());}
            }
            foreach(var row in rows.Take(selected).Reverse()){var removed=await provider.DeleteHolonWithSoftDeleteAsync(row.Id,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));}
            await using(var cleanup=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash",verifier){BindByName=true}){cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();cleanup.Parameters.Add("Hash",OracleDbType.Varchar2).Value=ConditionalHolonPlan.Create(request,request.Write.Authority).Fingerprint;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());}await AssertReceiptAbsent(verifier,operation);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)][DataRow(5)][DataRow(6)][DataRow(7)][DataRow(8)][DataRow(9)]
    public async Task ConditionalCorruptOrWrongGraphReceiptCannotReportReplaySuccess(int scenario)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;var journal=Path.Combine(work,$"conditional-receipt-integrity-{scenario}.pending.json");Assert.IsFalse(File.Exists(journal));
        var root=new Holon{Id=Guid.NewGuid(),Name="owned-receipt-integrity"};var otherId=Guid.NewGuid();var request=ConditionalCreate(root,Guid.NewGuid());var provider=new LostCommitProvider(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();Assert.IsNull(await Read(verifier,"OASIS_HOLONS",root.Id));Assert.IsNull(await Read(verifier,"OASIS_HOLONS",otherId));await AssertReceiptAbsent(verifier,request.Write.OperationId);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id=root.Id,otherId,operation=request.Write.OperationId,scenario,state="before-receipt-integrity-fixture"}));file.Flush(true);}
            var first=await provider.ExecuteConditionalHolonWriteAsync(request);Assert.IsFalse(first.IsError,first.Exception?.ToString());Assert.AreEqual(1,provider.CommitCalls);var originalRecord=await Read(verifier,"OASIS_HOLONS",root.Id);Assert.IsNotNull(originalRecord);string originalReceipt;
            await using(var read=new OracleCommand("SELECT RESULT_JSON FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",verifier){BindByName=true}){read.Parameters.Add("Id",OracleDbType.Raw).Value=request.Write.OperationId.ToByteArray();originalReceipt=(await read.ExecuteScalarAsync())!.ToString()!;}
            var plan=ConditionalHolonPlan.Create(request,request.Write.Authority);string corrupt;
            if(scenario==0)corrupt=originalReceipt.Replace(root.Name,"corrupted-receipt-name");
            else if(scenario==1)corrupt=ConditionalHolonReceipt.Write(new[]{new Holon{Id=otherId,Name="different acknowledged identity",Version=1}},plan);
            else if(scenario==8||scenario==9)corrupt=scenario==8?"{":"[]";
            else if(scenario>=4){
                var invalid=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(first.Result.Single());
                if(scenario==4)invalid.Version=0;else if(scenario==5)CosmicConcurrency.SetProviderRevisionId(invalid,request.Write.Authority,Guid.Empty);else if(scenario==6)invalid.VersionId=Guid.Empty;else invalid.ProviderUniqueStorageKey[request.Write.Authority]="wrong-native-key";
                corrupt=ConditionalHolonReceipt.Write(new[]{invalid},plan);
            }
            else{
                var otherRequest=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(scenario==2?Guid.NewGuid():request.Write.OperationId,request.Write.ActorId,request.Write.Authority,request.Write.Conditions),ConditionalHolonOperation.Save,new[]{new Holon{Id=root.Id,Name=scenario==3?"different intent":root.Name}});
                var otherPlan=ConditionalHolonPlan.Create(otherRequest,request.Write.Authority);if(scenario==2)Assert.AreEqual(plan.Fingerprint,otherPlan.Fingerprint);else Assert.AreNotEqual(plan.Fingerprint,otherPlan.Fingerprint);
                corrupt=ConditionalHolonReceipt.Write(first.Result,otherPlan);
            }
            Assert.AreNotEqual(originalReceipt,corrupt);
            await using(var inject=new OracleCommand("UPDATE OASIS_WRITE_RECEIPTS SET RESULT_JSON=:Corrupt WHERE OPERATION_ID=:Id AND DBMS_LOB.COMPARE(RESULT_JSON,:Original)=0",verifier){BindByName=true}){inject.Parameters.Add("Corrupt",OracleDbType.Clob).Value=corrupt;inject.Parameters.Add("Id",OracleDbType.Raw).Value=request.Write.OperationId.ToByteArray();inject.Parameters.Add("Original",OracleDbType.Clob).Value=originalReceipt;Assert.AreEqual(1,await inject.ExecuteNonQueryAsync());}
            var replay=await provider.ExecuteConditionalHolonWriteAsync(request);Assert.IsTrue(replay.IsError);Assert.IsFalse(replay.IsSaved);Assert.IsNull(replay.Result);Assert.IsInstanceOfType<InvalidDataException>(replay.Exception);Assert.AreEqual(1,provider.CommitCalls);Assert.AreEqual(originalRecord,await Read(verifier,"OASIS_HOLONS",root.Id));Assert.IsNull(await Read(verifier,"OASIS_HOLONS",otherId));
            Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(replay.Exception));if(scenario>=8)Assert.IsInstanceOfType<Newtonsoft.Json.JsonException>(replay.Exception.InnerException);
            await using(var unchanged=new OracleCommand("SELECT RESULT_JSON FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",verifier){BindByName=true}){unchanged.Parameters.Add("Id",OracleDbType.Raw).Value=request.Write.OperationId.ToByteArray();Assert.AreEqual(corrupt,(await unchanged.ExecuteScalarAsync())!.ToString());}
            var removed=await provider.DeleteHolonWithSoftDeleteAsync(root.Id,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",root.Id));
            await using(var cleanup=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash AND DBMS_LOB.COMPARE(RESULT_JSON,:Corrupt)=0",verifier){BindByName=true}){cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=request.Write.OperationId.ToByteArray();cleanup.Parameters.Add("Hash",OracleDbType.Varchar2).Value=ConditionalHolonPlan.Create(request,request.Write.Authority).Fingerprint;cleanup.Parameters.Add("Corrupt",OracleDbType.Clob).Value=corrupt;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());}await AssertReceiptAbsent(verifier,request.Write.OperationId);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    [TestMethod]
    public async Task ConditionalCancellationDuringNativeLockWaitRollsBackContender()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;var journal=Path.Combine(work,"conditional-cancel-wait.pending.json");Assert.IsFalse(File.Exists(journal));
        var root=new Holon{Id=Guid.NewGuid(),Name="owned-cancel-original"};var original=ConditionalCreate(root,Guid.NewGuid());var contender=ConditionalCreate(new Holon{Id=root.Id,Name="owned-cancel-contender"},Guid.NewGuid());
        var provider=new HeldConditionalCommitProvider(connection){IsVersionControlEnabled=true};using var cancellation=new CancellationTokenSource();Task<NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolon>>>? first=null,second=null;
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();Assert.IsNull(await Read(verifier,"OASIS_HOLONS",root.Id));await AssertReceiptAbsent(verifier,original.Write.OperationId);await AssertReceiptAbsent(verifier,contender.Write.OperationId);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id=root.Id,originalOperation=original.Write.OperationId,contenderOperation=contender.Write.OperationId,state="before-conditional-cancellation-wait"}));file.Flush(true);}
            first=provider.ExecuteConditionalHolonWriteAsync(original);var blocker=await provider.Locked.Task.WaitAsync(TimeSpan.FromSeconds(15));second=provider.ExecuteConditionalHolonWriteAsync(contender,cancellation.Token);var deadline=DateTime.UtcNow.AddSeconds(15);var blocked=false;
            do{await using var query=new OracleCommand("SELECT COUNT(*) FROM V$SESSION WHERE BLOCKING_SESSION=:Id AND EVENT='enq: TX - row lock contention'",verifier){BindByName=true};query.Parameters.Add("Id",OracleDbType.Int32).Value=blocker;blocked=Convert.ToInt32(await query.ExecuteScalarAsync())>0;if(!blocked)await Task.Delay(100);}while(!blocked&&DateTime.UtcNow<deadline);
            Assert.IsTrue(blocked);Assert.IsFalse(second.IsCompleted);cancellation.Cancel();var cancelled=await second.WaitAsync(TimeSpan.FromSeconds(15));Assert.IsTrue(cancelled.IsError);Assert.IsFalse(cancelled.IsSaved);Assert.IsNull(cancelled.Result);Assert.IsInstanceOfType<OperationCanceledException>(cancelled.Exception);Assert.AreEqual(cancellation.Token,((OperationCanceledException)cancelled.Exception).CancellationToken);if(cancelled.Exception.InnerException!=null){Assert.IsInstanceOfType<OracleException>(cancelled.Exception.InnerException);Assert.AreEqual(1013,((OracleException)cancelled.Exception.InnerException).Number);}Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(cancelled.Exception));Assert.IsFalse(first.IsCompleted);await AssertReceiptAbsent(verifier,contender.Write.OperationId);
            provider.Release.TrySetResult(true);var committed=await first.WaitAsync(TimeSpan.FromSeconds(30));Assert.IsFalse(committed.IsError,committed.Exception?.ToString());Assert.AreEqual(1,provider.CommitCalls);var raw=await Read(verifier,"OASIS_HOLONS",root.Id);Assert.IsNotNull(raw);using(var parsed=JsonDocument.Parse(raw)){Assert.AreEqual(root.Name,parsed.RootElement.GetProperty("Name").GetString());Assert.AreEqual(1,parsed.RootElement.GetProperty("Version").GetInt32());}await AssertReceiptAbsent(verifier,contender.Write.OperationId);
            var cleanupProvider=new OracleDBOASIS(connection);var removed=await cleanupProvider.DeleteHolonWithSoftDeleteAsync(root.Id,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",root.Id));
            await using(var cleanup=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash",verifier){BindByName=true}){cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=original.Write.OperationId.ToByteArray();cleanup.Parameters.Add("Hash",OracleDbType.Varchar2).Value=ConditionalHolonPlan.Create(original,original.Write.Authority).Fingerprint;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());}await AssertReceiptAbsent(verifier,original.Write.OperationId);File.Delete(journal);
        }finally{provider.Release.TrySetResult(true);if(first!=null&&second!=null)await Task.WhenAll(first,second).WaitAsync(TimeSpan.FromSeconds(30));else if(first!=null)await first.WaitAsync(TimeSpan.FromSeconds(30));await provider.DeActivateProviderAsync();}
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task OrdinarySaveInvalidatesConditionalRevisionWithOrWithoutHistory(bool history)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;var journal=Path.Combine(work,$"conditional-ordinary-{history}.pending.json");Assert.IsFalse(File.Exists(journal));
        var root=new Holon{Id=Guid.NewGuid(),Name="owned-ordinary-before"};var rejectedOperation=Guid.NewGuid();var acceptedOperation=Guid.NewGuid();var actor=Guid.NewGuid();var authority=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS;var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=history};
        ConditionalHolonWriteRequest Request(Holon snapshot,Guid operation)=>new(new ConditionalHolonWrite(operation,actor,authority,new[]{new HolonWriteCondition(snapshot.Id,CosmicConcurrency.GetProviderRevisionId(snapshot,authority))}),ConditionalHolonOperation.Save,new[]{snapshot},saveChildren:false);
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();Assert.IsNull(await Read(verifier,"OASIS_HOLONS",root.Id));await AssertReceiptAbsent(verifier,rejectedOperation);await AssertReceiptAbsent(verifier,acceptedOperation);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id=root.Id,rejectedOperation,acceptedOperation,history,actor,state="before-ordinary-conditional-revision"}));file.Flush(true);}
            var seeded=await provider.SaveHolonAsync(root,saveChildren:false);Assert.IsFalse(seeded.IsError,seeded.Exception?.ToString());var old=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(root);var oldRevision=CosmicConcurrency.GetProviderRevisionId(old,authority);Assert.IsTrue(oldRevision.HasValue);
            root.Name="owned-ordinary-after";var ordinary=await provider.SaveHolonAsync(root,saveChildren:false);Assert.IsFalse(ordinary.IsError,ordinary.Exception?.ToString());var actualRevision=CosmicConcurrency.GetProviderRevisionId(root,authority);Assert.AreNotEqual(oldRevision,actualRevision);var original=await Read(verifier,"OASIS_HOLONS",root.Id);Assert.IsNotNull(original);
            var stale=await provider.ExecuteConditionalHolonWriteAsync(Request(old,rejectedOperation));Assert.IsTrue(stale.IsError);Assert.IsInstanceOfType<CosmicConcurrencyException>(stale.Exception);var conflict=((CosmicConcurrencyException)stale.Exception).Conflicts.Single();Assert.AreEqual(oldRevision,conflict.ExpectedRevisionId);Assert.AreEqual(actualRevision,conflict.ActualRevisionId);Assert.AreEqual(root.Version,conflict.ActualVersion);Assert.AreEqual(original,await Read(verifier,"OASIS_HOLONS",root.Id));await AssertReceiptAbsent(verifier,rejectedOperation);
            var current=Request(NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(root),acceptedOperation);var accepted=await provider.ExecuteConditionalHolonWriteAsync(current);Assert.IsFalse(accepted.IsError,accepted.Exception?.ToString());Assert.AreEqual(history?3:1,accepted.Result.Single().Version);Assert.AreNotEqual(actualRevision,CosmicConcurrency.GetProviderRevisionId(accepted.Result.Single(),authority));
            var removed=await provider.DeleteHolonWithSoftDeleteAsync(root.Id,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",root.Id));
            await using(var cleanup=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash",verifier){BindByName=true}){cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=acceptedOperation.ToByteArray();cleanup.Parameters.Add("Hash",OracleDbType.Varchar2).Value=ConditionalHolonPlan.Create(current,authority).Fingerprint;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());}await AssertReceiptAbsent(verifier,acceptedOperation);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    private sealed class HeldConditionalCommitProvider(string connection):OracleDBOASIS(connection)
    {
        public readonly TaskCompletionSource<int> Locked=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<bool> Release=new(TaskCreationOptions.RunContinuationsAsynchronously);public int CommitCalls;
        protected override async Task CommitLifecycleTransactionAsync(OracleTransaction transaction)
        {
            if(Interlocked.Increment(ref CommitCalls)==1){
                await using var sid=new OracleCommand("SELECT SYS_CONTEXT('USERENV','SID') FROM DUAL",transaction.Connection){Transaction=transaction};Locked.TrySetResult(int.Parse((await sid.ExecuteScalarAsync())!.ToString()!));
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(30));
            }
            await base.CommitLifecycleTransactionAsync(transaction);
        }
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)][DataRow(5)][DataRow(6)]
    public async Task ConditionalConcurrentNativeClaimsHaveOneWinner(int scenario)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;var journal=Path.Combine(work,$"conditional-concurrent-{scenario}.pending.json");Assert.IsFalse(File.Exists(journal));
        var root=new Holon{Id=Guid.NewGuid(),Name="owned-conditional-claim"};if(scenario==4)root.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS]="foreign-key-a";if(scenario==5)root.PreviousVersionProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS]="previous-key-a";
        if(scenario==6)CosmicConcurrency.SetProviderRevisionId(root,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS,Guid.NewGuid());
        var original=ConditionalCreate(root,Guid.NewGuid());var operation=scenario==2?Guid.NewGuid():original.Write.OperationId;
        var contenderRoot=new Holon{Id=root.Id,Name=scenario==1?"changed-intent":root.Name};if(scenario==4)contenderRoot.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS]="foreign-key-b";if(scenario==5)contenderRoot.PreviousVersionProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS]="previous-key-b";
        if(scenario==6)CosmicConcurrency.SetProviderRevisionId(contenderRoot,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS,Guid.NewGuid());
        var contender=scenario==0?original:new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,scenario==3?Guid.NewGuid():original.Write.ActorId,original.Write.Authority,original.Write.Conditions),ConditionalHolonOperation.Save,new[]{contenderRoot});
        var provider=new HeldConditionalCommitProvider(connection){IsVersionControlEnabled=true};Task<NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolon>>>? first=null,second=null;
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();Assert.IsNull(await Read(verifier,"OASIS_HOLONS",root.Id));await AssertReceiptAbsent(verifier,original.Write.OperationId);if(scenario==2)await AssertReceiptAbsent(verifier,operation);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id=root.Id,originalOperation=original.Write.OperationId,operation,scenario,actor=original.Write.ActorId,state="before-concurrent-conditional-claim"}));file.Flush(true);}
            first=provider.ExecuteConditionalHolonWriteAsync(original);var blocker=await provider.Locked.Task.WaitAsync(TimeSpan.FromSeconds(15));second=provider.ExecuteConditionalHolonWriteAsync(contender);
            var deadline=DateTime.UtcNow.AddSeconds(15);var blocked=false;
            do{await using var query=new OracleCommand("SELECT COUNT(*) FROM V$SESSION WHERE BLOCKING_SESSION=:Id AND EVENT='enq: TX - row lock contention'",verifier){BindByName=true};query.Parameters.Add("Id",OracleDbType.Int32).Value=blocker;blocked=Convert.ToInt32(await query.ExecuteScalarAsync())>0;if(!blocked)await Task.Delay(100);}while(!blocked&&DateTime.UtcNow<deadline);
            Assert.IsTrue(blocked,"Contender must be observed waiting on original native transaction.");Assert.IsFalse(first.IsCompleted);Assert.IsFalse(second.IsCompleted);provider.Release.TrySetResult(true);var results=await Task.WhenAll(first,second).WaitAsync(TimeSpan.FromSeconds(30));Assert.IsFalse(results[0].IsError,results[0].Exception?.ToString());
            if(scenario==0){Assert.IsFalse(results[1].IsError,results[1].Exception?.ToString());Assert.AreEqual(results[0].Result.Single().VersionId,results[1].Result.Single().VersionId);}
            else if(scenario==1||scenario>=3){Assert.IsTrue(results[1].IsError);Assert.AreEqual("COSMIC_OPERATION_ID_REUSED",results[1].ErrorCode);}
            else{Assert.IsTrue(results[1].IsError);Assert.IsInstanceOfType<CosmicConcurrencyException>(results[1].Exception);await AssertReceiptAbsent(verifier,operation);}
            Assert.AreEqual(1,provider.CommitCalls);var raw=await Read(verifier,"OASIS_HOLONS",root.Id);Assert.IsNotNull(raw);using(var record=JsonDocument.Parse(raw)){Assert.AreEqual(1,record.RootElement.GetProperty("Version").GetInt32());Assert.AreEqual(root.Name,record.RootElement.GetProperty("Name").GetString());}
            Assert.AreEqual(original.Write.ActorId,NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>(raw).CreatedByAvatarId);
            var cleanupProvider=new OracleDBOASIS(connection);var removed=await cleanupProvider.DeleteHolonWithSoftDeleteAsync(root.Id,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",root.Id));
            await using(var cleanup=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash",verifier){BindByName=true}){cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=original.Write.OperationId.ToByteArray();cleanup.Parameters.Add("Hash",OracleDbType.Varchar2).Value=ConditionalHolonPlan.Create(original,original.Write.Authority).Fingerprint;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());}await AssertReceiptAbsent(verifier,original.Write.OperationId);File.Delete(journal);
        }finally{provider.Release.TrySetResult(true);if(first!=null&&second!=null)await Task.WhenAll(first,second).WaitAsync(TimeSpan.FromSeconds(30));else if(first!=null)await first.WaitAsync(TimeSpan.FromSeconds(30));await provider.DeActivateProviderAsync();}
    }
    private static ConditionalHolonWriteRequest ConditionalCreate(Holon root,Guid operation)
        =>new(new ConditionalHolonWrite(operation,Guid.NewGuid(),NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS,new[]{root}.Concat(root.Children).Select(h=>new HolonWriteCondition(h.Id,null))),ConditionalHolonOperation.Save,new[]{root});
    private static async Task AssertReceiptAbsent(OracleConnection connection,Guid operation)
    {
        await using var count=new OracleCommand("SELECT COUNT(*) FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",connection){BindByName=true};count.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();Assert.AreEqual(0,Convert.ToInt32(await count.ExecuteScalarAsync()));
    }
    [TestMethod]
    public async Task ConditionalLostCommitReturnPreservesOriginalReceiptWithoutWriteReplay()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;var journal=Path.Combine(work,"conditional-lost-commit.pending.json");Assert.IsFalse(File.Exists(journal));
        var root=new Holon{Id=Guid.NewGuid(),Name="owned-conditional-lost-root"};root.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="owned-conditional-lost-child"});var ids=new[]{root.Id,root.Children.Single().Id};var operation=Guid.NewGuid();var request=ConditionalCreate(root,operation);
        var provider=new LostCommitProvider(connection){IsVersionControlEnabled=true,LoseNextCommit=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();foreach(var id in ids)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));await AssertReceiptAbsent(verifier,operation);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,operation,actor=request.Write.ActorId,state="before-conditional-lost-commit"}));file.Flush(true);}
            var outcome=await provider.ExecuteConditionalHolonWriteAsync(request);Assert.IsTrue(outcome.IsError);Assert.IsFalse(outcome.IsSaved);Assert.IsNull(outcome.Result);Assert.IsInstanceOfType<OracleConditionalReconciliationException>(outcome.Exception);Assert.AreEqual(operation,((OracleConditionalReconciliationException)outcome.Exception).OperationId);Assert.AreSame(provider.Lost,outcome.Exception.InnerException);Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(outcome.Exception));Assert.AreEqual(1,provider.CommitCalls);Assert.AreEqual(0,root.Version);Assert.IsNull(CosmicConcurrency.GetProviderRevisionId(root,request.Write.Authority));
            Assert.AreEqual(0,outcome.SavedCount);Assert.AreEqual(0,outcome.DeletedCount);
            foreach(var id in ids){var raw=await Read(verifier,"OASIS_HOLONS",id);Assert.IsNotNull(raw);using var parsed=JsonDocument.Parse(raw);Assert.AreEqual(1,parsed.RootElement.GetProperty("Version").GetInt32());}
            await using(var receipt=new OracleCommand("SELECT REQUEST_HASH,RESULT_JSON FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",verifier){BindByName=true}){receipt.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();await using var reader=await receipt.ExecuteReaderAsync();Assert.IsTrue(await reader.ReadAsync());Assert.AreEqual(ConditionalHolonPlan.Create(request,request.Write.Authority).Fingerprint,reader.GetString(0));var acknowledged=ConditionalHolonReceipt.Read(reader.GetString(1));Assert.AreEqual(root.Id,acknowledged.Single().Id);Assert.AreEqual(1,acknowledged.Single().Children.Single().Version);}
            // Explicit original-receipt reconciliation after independent readback, not retrying native writes.
            var replay=await provider.ExecuteConditionalHolonWriteAsync(request);Assert.IsFalse(replay.IsError,replay.Exception?.ToString());Assert.AreEqual(1,replay.Result.Single().Version);Assert.AreEqual(1,provider.CommitCalls);
            Assert.AreEqual(2,replay.SavedCount);Assert.AreEqual(0,replay.DeletedCount);Assert.IsTrue(replay.HasAnyHolonsChanged);
            foreach(var id in ids.Reverse()){var removed=await provider.DeleteHolonWithSoftDeleteAsync(id,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));}
            await using(var cleanup=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash",verifier){BindByName=true}){cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();cleanup.Parameters.Add("Hash",OracleDbType.Varchar2).Value=ConditionalHolonPlan.Create(request,request.Write.Authority).Fingerprint;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());}await AssertReceiptAbsent(verifier,operation);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    [TestMethod]
    public async Task ConditionalSecondNodeFailureRollsBackGraphAndReceipt()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;var journal=Path.Combine(work,"conditional-rollback.pending.json");Assert.IsFalse(File.Exists(journal));
        var root=new Holon{Id=Guid.NewGuid(),Name="owned-rollback-root"};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="owned-rollback-child"};root.Children.Add(child);var operation=Guid.NewGuid();var request=ConditionalCreate(root,operation);const string trigger="OASIS_COND_ROLLBACK";var provider=new LostCommitProvider(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();foreach(var id in new[]{root.Id,child.Id})Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));await AssertReceiptAbsent(verifier,operation);
            await using(var exists=new OracleCommand("SELECT COUNT(*) FROM USER_TRIGGERS WHERE TRIGGER_NAME=:Name",verifier){BindByName=true}){exists.Parameters.Add("Name",OracleDbType.Varchar2).Value=trigger;Assert.AreEqual(0,Convert.ToInt32(await exists.ExecuteScalarAsync()));}
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=new[]{root.Id,child.Id},operation,trigger,state="before-conditional-rollback"}));file.Flush(true);}
            await using(var inject=new OracleCommand($"CREATE TRIGGER {trigger} BEFORE INSERT ON OASIS_HOLONS FOR EACH ROW BEGIN IF :NEW.ID=HEXTORAW('{Convert.ToHexString(child.Id.ToByteArray())}') THEN RAISE_APPLICATION_ERROR(-20002,'owned conditional child failure'); END IF; END;",verifier)){await inject.ExecuteNonQueryAsync();}
            var result=await provider.ExecuteConditionalHolonWriteAsync(request);Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType<OracleException>(result.Exception);Assert.AreEqual(20002,((OracleException)result.Exception).Number);Assert.AreEqual(0,provider.CommitCalls);
            foreach(var id in new[]{root.Id,child.Id})Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));await AssertReceiptAbsent(verifier,operation);Assert.AreEqual(0,root.Version);Assert.AreEqual(0,child.Version);
            await using(var drop=new OracleCommand($"DROP TRIGGER {trigger}",verifier)){await drop.ExecuteNonQueryAsync();}File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    [TestMethod]
    public async Task AtomicConditionalGraphReceiptsReplayExactAcknowledgements()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,"conditional-graph.pending.json");Assert.IsFalse(File.Exists(journal));var unicode=new string('樹',32769)+"🌳🌱";var root=new Holon{Id=Guid.NewGuid(),Name="owned-conditional-root",Description=unicode,ProviderMetaData=null};var child=new Holon{Id=Guid.NewGuid(),Name="owned-conditional-child",ParentHolonId=root.Id};root.Children.Add(child);
        var ids=new[]{root.Id,child.Id};var operations=Enumerable.Range(0,5).Select(_=>Guid.NewGuid()).ToArray();var actor=Guid.NewGuid();var authority=NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS;
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};var committed=new List<NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonWriteRequest>();
        NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonWriteRequest Request(Holon graph,int index,NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonOperation operation,bool create=false)=>new(
            new NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonWrite(operations[index],actor,authority,new[]{graph}.Concat(graph.Children).Select(h=>new NextGenSoftware.OASIS.API.Core.Interfaces.HolonWriteCondition(h.Id,create?null:CosmicConcurrency.GetProviderRevisionId(h,authority)))),operation,new[]{graph});
        Holon Copy(NextGenSoftware.OASIS.API.Core.Interfaces.IHolon source){var copy=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(source);copy.Children=source.Children.Select(c=>(NextGenSoftware.OASIS.API.Core.Interfaces.IHolon)Copy(c)).ToList();return copy;}
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();foreach(var id in ids)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,operations,actor,state="before-conditional-native-graph"}));file.Flush(true);}
            var create=Request(root,0,NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonOperation.Save,true);var first=await provider.ExecuteConditionalHolonWriteAsync(create);Assert.IsFalse(first.IsError,first.Exception?.ToString());Assert.IsTrue(first.IsSaved);committed.Add(create);
            Assert.AreEqual(2,first.SavedCount);Assert.AreEqual(0,first.DeletedCount);Assert.IsTrue(first.HasAnyHolonsChanged);
            Assert.AreEqual(0,root.Version);var ack1=first.Result.Single();Assert.AreEqual(1,ack1.Version);Assert.AreEqual(1,ack1.Children.Single().Version);
            var publishedRequest=new ConditionalHolonWriteRequest(create.Write,ConditionalHolonOperation.Save,new[]{Copy(ack1)});var publishedReplay=await provider.ExecuteConditionalHolonWriteAsync(publishedRequest);Assert.IsFalse(publishedReplay.IsError,publishedReplay.Exception?.ToString());Assert.AreEqual(ack1.VersionId,publishedReplay.Result.Single().VersionId);
            Assert.AreEqual(unicode,ack1.Description);
            Assert.AreEqual(2,publishedReplay.SavedCount);Assert.AreEqual(0,publishedReplay.DeletedCount);Assert.IsTrue(publishedReplay.HasAnyHolonsChanged);
            await using(var receipt=new OracleCommand("SELECT RESULT_JSON FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",verifier){BindByName=true}){receipt.Parameters.Add("Id",OracleDbType.Raw).Value=operations[0].ToByteArray();var raw=(await receipt.ExecuteScalarAsync())!.ToString()!;Assert.IsTrue(raw.Length>32767);Assert.AreEqual(unicode,ConditionalHolonReceipt.Read(raw,ConditionalHolonPlan.Create(create,authority)).Single().Description);}
            var modified=Copy(ack1);modified.Name="owned-updated-conditional-root";modified.Description="newer compact payload";var update=Request(modified,1,NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonOperation.Save);
            var second=await provider.ExecuteConditionalHolonWriteAsync(update);Assert.IsFalse(second.IsError,second.Exception?.ToString());committed.Add(update);Assert.AreEqual(2,second.Result.Single().Version);
            var replay=await provider.ExecuteConditionalHolonWriteAsync(create);Assert.IsFalse(replay.IsError,replay.Exception?.ToString());Assert.AreEqual(1,replay.Result.Single().Version);Assert.AreEqual(root.Name,replay.Result.Single().Name);
            Assert.AreEqual(unicode,replay.Result.Single().Description);Assert.AreEqual("newer compact payload",second.Result.Single().Description);
            foreach(var saved in new[]{second,replay}){Assert.AreEqual(2,saved.SavedCount);Assert.AreEqual(0,saved.DeletedCount);Assert.IsTrue(saved.HasAnyHolonsChanged);}
            var stale=await provider.ExecuteConditionalHolonWriteAsync(Request(Copy(ack1),2,NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonOperation.Save));Assert.IsTrue(stale.IsError);Assert.IsInstanceOfType<NextGenSoftware.OASIS.API.Core.Interfaces.CosmicConcurrencyException>(stale.Exception);
            var changed=Copy(root);changed.Name="different-intent";var reused=await provider.ExecuteConditionalHolonWriteAsync(Request(changed,0,NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonOperation.Save,true));Assert.IsTrue(reused.IsError);Assert.AreEqual("COSMIC_OPERATION_ID_REUSED",reused.ErrorCode);
            foreach(var id in ids){var raw=await Read(verifier,"OASIS_HOLONS",id);Assert.IsNotNull(raw);using var parsed=JsonDocument.Parse(raw);Assert.AreEqual(2,parsed.RootElement.GetProperty("Version").GetInt32());}
            var soft=Request(Copy(second.Result.Single()),3,NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonOperation.SoftDelete);var tombstone=await provider.ExecuteConditionalHolonWriteAsync(soft);Assert.IsFalse(tombstone.IsError,tombstone.Exception?.ToString());committed.Add(soft);Assert.IsTrue(tombstone.IsDeleted);Assert.AreEqual(3,tombstone.Result.Single().Version);
            foreach(var id in ids)Assert.IsNull((await provider.LoadHolonAsync(id,loadChildren:false)).Result);
            Assert.AreEqual(0,tombstone.SavedCount);Assert.AreEqual(2,tombstone.DeletedCount);Assert.IsTrue(tombstone.HasAnyHolonsChanged);
            var hard=Request(Copy(tombstone.Result.Single()),4,NextGenSoftware.OASIS.API.Core.Interfaces.ConditionalHolonOperation.HardDelete);var removed=await provider.ExecuteConditionalHolonWriteAsync(hard);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());committed.Add(hard);foreach(var id in ids)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));
            var historicalReplay=await provider.ExecuteConditionalHolonWriteAsync(create);Assert.IsFalse(historicalReplay.IsError,historicalReplay.Exception?.ToString());Assert.AreEqual(1,historicalReplay.Result.Single().Version);foreach(var id in ids)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));
            Assert.AreEqual(unicode,historicalReplay.Result.Single().Description);
            Assert.AreEqual(0,removed.SavedCount);Assert.AreEqual(2,removed.DeletedCount);Assert.IsTrue(removed.HasAnyHolonsChanged);
            Assert.AreEqual(2,historicalReplay.SavedCount);Assert.AreEqual(0,historicalReplay.DeletedCount);Assert.IsTrue(historicalReplay.HasAnyHolonsChanged);
            foreach(var request in committed){await using var cleanup=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash",verifier){BindByName=true};cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=request.Write.OperationId.ToByteArray();cleanup.Parameters.Add("Hash",OracleDbType.Varchar2).Value=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ConditionalHolonPlan.Create(request,authority).Fingerprint;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());await AssertReceiptAbsent(verifier,request.Write.OperationId);}
            await using(var absent=new OracleCommand("SELECT COUNT(*) FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",verifier){BindByName=true}){absent.Parameters.Add("Id",OracleDbType.Raw).Value=operations[2].ToByteArray();Assert.AreEqual(0,Convert.ToInt32(await absent.ExecuteScalarAsync()));}
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    private sealed class HeldUpdateProvider(string connection):OracleDBOASIS(connection)
    {
        public readonly TaskCompletionSource<int> Locked=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<bool> Release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _writes;
        protected override async Task<int> ExecuteCanonicalWriteAsync(OracleCommand command)
        {
            if(Interlocked.Increment(ref _writes)==1){
                await using var identity=new OracleCommand("SELECT SYS_CONTEXT('USERENV','SID') FROM DUAL",command.Connection){Transaction=command.Transaction};
                Locked.TrySetResult(int.Parse((await identity.ExecuteScalarAsync())!.ToString()!,System.Globalization.CultureInfo.InvariantCulture));
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(30));
            }
            return await base.ExecuteCanonicalWriteAsync(command);
        }
    }
    [TestMethod]
    public async Task NativeRowLockSerializesUpdatesWithoutLosingHistory()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,"row-lock-history.pending.json");Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var seed=new Holon{Id=id,Name="owned-row-lock-seed"};
        var provider=new HeldUpdateProvider(connection){IsVersionControlEnabled=true};var setup=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>>? firstTask=null,secondTask=null;
        try{
            Assert.IsFalse((await setup.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,state="before-native-row-lock-history"}));file.Flush(true);}
            var seeded=await setup.SaveHolonAsync(seed,saveChildren:false);Assert.IsFalse(seeded.IsError,seeded.Exception?.ToString());
            var first=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(seed);var second=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<Holon>(seed);first.Name="owned-first-update";second.Name="owned-second-update";
            firstTask=provider.SaveHolonAsync(first,saveChildren:false);var blocker=await provider.Locked.Task.WaitAsync(TimeSpan.FromSeconds(15));
            secondTask=provider.SaveHolonAsync(second,saveChildren:false);var deadline=DateTime.UtcNow.AddSeconds(15);var blocked=false;
            do{
                await using var query=new OracleCommand("SELECT COUNT(*) FROM V$SESSION WHERE BLOCKING_SESSION=:Blocker AND EVENT='enq: TX - row lock contention'",verifier){BindByName=true};query.Parameters.Add("Blocker",OracleDbType.Int32).Value=blocker;
                blocked=Convert.ToInt32(await query.ExecuteScalarAsync(),System.Globalization.CultureInfo.InvariantCulture)>0;
                if(!blocked)await Task.Delay(100);
            }while(!blocked&&DateTime.UtcNow<deadline);
            Assert.IsTrue(blocked,"Actual Oracle session must report waiting on this original writer's row lock.");Assert.IsFalse(firstTask.IsCompleted);Assert.IsFalse(secondTask.IsCompleted);Assert.AreEqual(1,first.Version);Assert.AreEqual(1,second.Version);
            provider.Release.TrySetResult(true);var results=await Task.WhenAll(firstTask,secondTask).WaitAsync(TimeSpan.FromSeconds(30));foreach(var result in results){Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);}
            Assert.AreEqual(2,first.Version);Assert.AreEqual(3,second.Version);Assert.AreEqual(seed.VersionId,first.PreviousVersionId);Assert.AreEqual(first.VersionId,second.PreviousVersionId);
            var raw=await Read(verifier,"OASIS_HOLONS",id);Assert.IsNotNull(raw);using(var stored=JsonDocument.Parse(raw)){Assert.AreEqual("owned-second-update",stored.RootElement.GetProperty("Name").GetString());var archive=stored.RootElement.GetProperty("_oasis_record_history").GetProperty("records");Assert.AreEqual(2,archive.GetArrayLength());Assert.AreEqual("owned-row-lock-seed",archive[0].GetProperty("Name").GetString());Assert.AreEqual("owned-first-update",archive[1].GetProperty("Name").GetString());}
            var removed=await setup.DeleteHolonWithSoftDeleteAsync(id,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));File.Delete(journal);
        }finally{
            provider.Release.TrySetResult(true);if(firstTask!=null&&secondTask!=null)await Task.WhenAll(firstTask,secondTask).WaitAsync(TimeSpan.FromSeconds(30));else if(firstTask!=null)await firstTask.WaitAsync(TimeSpan.FromSeconds(30));
            await provider.DeActivateProviderAsync();await setup.DeActivateProviderAsync();
        }
    }
    [TestMethod]
    public async Task CanonicalMetadataMatchesBooleanNumberAndLongTextAcrossVersions()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,"canonical-metadata.pending.json");Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var large=new string('x',6000);
        var row=new Holon{Id=id,Name="owned-canonical-metadata",MetaData=new(){["flag"]=true,["number"]=42,["large"]=large}};
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,state="before-canonical-metadata-fixture"}));file.Flush(true);}
            var first=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(first.IsError,first.Exception?.ToString());row.Name+="-current";var second=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(second.IsError,second.Exception?.ToString());
            foreach(var version in new[]{0,1})foreach(var sync in new[]{false,true}){
                foreach(var pair in new Dictionary<string,string>{{"flag","True"},{"number","42"},{"large",large}}){
                    var found=sync?provider.LoadHolonsByMetaData(pair.Key,pair.Value,loadChildren:false,version:version):await provider.LoadHolonsByMetaDataAsync(pair.Key,pair.Value,loadChildren:false,version:version);
                    Assert.IsFalse(found.IsError,found.Exception?.ToString());Assert.AreEqual(version==0?2:1,found.Result.Single(r=>r.Id==id).Version);
                }
                foreach(var mode in new[]{NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All,NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.Any}){
                    var filter=new Dictionary<string,string>{{"flag","True"},{"large",large}};
                    var found=sync?provider.LoadHolonsByMetaData(filter,mode,loadChildren:false,version:version):await provider.LoadHolonsByMetaDataAsync(filter,mode,loadChildren:false,version:version);
                    Assert.IsFalse(found.IsError,found.Exception?.ToString());Assert.IsTrue(found.Result.Any(r=>r.Id==id));
                }
                var missing=await provider.LoadHolonsByMetaDataAsync("absent","",loadChildren:false,version:version);Assert.IsFalse(missing.IsError,missing.Exception?.ToString());Assert.IsFalse(missing.Result.Any(r=>r.Id==id));
            }
            var removed=await provider.DeleteHolonWithSoftDeleteAsync(id,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    // Diagnostic evidence of an OPEN query-semantics gap, not provider acceptance.
    [TestMethod]
    public async Task NativeMetadataScalarDefaultsDifferFromCanonicalMatcher()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
        await using var command=new OracleCommand("SELECT JSON_VALUE(:Document,'$.flag'),JSON_VALUE(:Document,'$.large') FROM DUAL",verifier){BindByName=true};
        command.Parameters.Add("Document",OracleDbType.Clob).Value=JsonSerializer.Serialize(new{flag=true,large=new string('x',6000)});
        await using var reader=await command.ExecuteReaderAsync();Assert.IsTrue(await reader.ReadAsync());
        Assert.AreEqual("true",reader.GetString(0));Assert.IsTrue(reader.IsDBNull(1));
        Assert.AreEqual("True",NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.MetadataValueText(true));
        Assert.IsTrue(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.MatchesMetadata(new Dictionary<string,object>{{"large",new string('x',6000)}},new Dictionary<string,string>{{"large",new string('x',6000)}},NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All));
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task HolonIndexSelectionCannotContradictCanonicalParentOrType(bool parent)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"holon-index-{parent}.pending.json");Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var selectedParent=Guid.NewGuid();
        var row=new Holon{Id=id,Name="owned-holon-index",ParentHolonId=Guid.NewGuid(),MetaData=new(){["owned-index-fixture"]=id.ToString()}};
        var selectedType=Enum.GetValues<NextGenSoftware.OASIS.API.Core.Enums.HolonType>().First(t=>t!=row.HolonType&&t!=NextGenSoftware.OASIS.API.Core.Enums.HolonType.All);
        var provider=new OracleDBOASIS(connection);var column=parent?"PARENT_HOLON_ID":"HOLON_TYPE";
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,parent,selectedParent,selectedType,state="before-holon-index-fixture"}));file.Flush(true);}
            var saved=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());var original=await Read(verifier,"OASIS_HOLONS",id);Assert.IsNotNull(original);
            await using(var corrupt=new OracleCommand($"UPDATE OASIS_HOLONS SET {column}=:Indexed WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Original)=0",verifier){BindByName=true}){
                corrupt.Parameters.Add("Indexed",parent?OracleDbType.Raw:OracleDbType.Int32).Value=parent?(object)selectedParent.ToByteArray():(int)selectedType;
                corrupt.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();corrupt.Parameters.Add("Original",OracleDbType.Clob).Value=original;Assert.AreEqual(1,await corrupt.ExecuteNonQueryAsync());
            }
            foreach(var sync in new[]{false,true}){
                var found=parent?(sync?provider.LoadHolonsForParent(selectedParent,loadChildren:false):await provider.LoadHolonsForParentAsync(selectedParent,loadChildren:false)):(sync?provider.LoadAllHolons(selectedType,loadChildren:false):await provider.LoadAllHolonsAsync(selectedType,loadChildren:false));
                Assert.IsTrue(found.IsError);Assert.IsNull(found.Result);Assert.IsInstanceOfType<InvalidDataException>(found.Exception);Assert.AreEqual(original,await Read(verifier,"OASIS_HOLONS",id));
                var searchParams=new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new(){["owned-index-fixture"]=id.ToString()}};
                var searched=sync?provider.Search(searchParams,loadChildren:false):await provider.SearchAsync(searchParams,loadChildren:false);
                Assert.IsTrue(searched.IsError,"Collection-based search must not conceal inconsistent native projections.");
                Assert.IsNull(searched.Result);Assert.IsInstanceOfType<InvalidDataException>(searched.Exception);
                Assert.AreEqual(original,await Read(verifier,"OASIS_HOLONS",id));
                var proposed=new Holon{Id=id,Name="must-not-repair-corrupt-projection",ParentHolonId=row.ParentHolonId};
                var rejectedSave=sync?provider.SaveHolon(proposed,saveChildren:false):await provider.SaveHolonAsync(proposed,saveChildren:false);
                Assert.IsTrue(rejectedSave.IsError);Assert.IsFalse(rejectedSave.IsSaved);Assert.IsNull(rejectedSave.Result);
                Assert.IsInstanceOfType<InvalidDataException>(rejectedSave.Exception);Assert.AreEqual(0,proposed.Version);
                foreach(var softDelete in new[]{true,false}){
                    var rejectedDelete=sync?provider.DeleteHolonWithSoftDelete(id,softDelete):await provider.DeleteHolonWithSoftDeleteAsync(id,softDelete);
                    Assert.IsTrue(rejectedDelete.IsError);Assert.IsFalse(rejectedDelete.Result);
                    Assert.IsInstanceOfType<InvalidDataException>(rejectedDelete.Exception);
                    Assert.AreEqual(original,await Read(verifier,"OASIS_HOLONS",id));
                }
                if(!parent){
                    var single=sync?provider.LoadHolonsByMetaData("owned-index-fixture",id.ToString(),selectedType,loadChildren:false):await provider.LoadHolonsByMetaDataAsync("owned-index-fixture",id.ToString(),selectedType,loadChildren:false);
                    var pairs=new Dictionary<string,string>{{"owned-index-fixture",id.ToString()}};
                    var multiple=sync?provider.LoadHolonsByMetaData(pairs,NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All,selectedType,loadChildren:false):await provider.LoadHolonsByMetaDataAsync(pairs,NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All,selectedType,loadChildren:false);
                    foreach(var metadata in new[]{single,multiple}){Assert.IsTrue(metadata.IsError);Assert.IsNull(metadata.Result);Assert.IsInstanceOfType<InvalidDataException>(metadata.Exception);}
                }
            }
            await using var cleanup=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Original)=0",verifier){BindByName=true};cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Original",OracleDbType.Clob).Value=original;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)]
    public async Task IndexedIdentityCannotReturnDifferentCanonicalIdentity(int kind)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"indexed-identity-{kind}.pending.json");Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var name="owned-index-"+id.ToString("N");
        var table=kind<2?"OASIS_AVATARS":"OASIS_AVATAR_DETAILS";var column=kind%2==0?"USERNAME":"EMAIL";var indexed=name+"-wrong@example.invalid";
        var provider=new OracleDBOASIS(connection);
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();Assert.IsNull(await Read(verifier,table,id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,table,column,indexed,state="before-indexed-identity-fixture"}));file.Flush(true);}
            if(kind<2){var saved=await provider.SaveAvatarAsync(new Avatar{Id=id,Username=name,Email=name+"@example.invalid"});Assert.IsFalse(saved.IsError,saved.Exception?.ToString());}
            else{var saved=await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=name,Email=name+"@example.invalid"});Assert.IsFalse(saved.IsError,saved.Exception?.ToString());}
            var original=await Read(verifier,table,id);Assert.IsNotNull(original);
            await using(var corrupt=new OracleCommand($"UPDATE {table} SET {column}=:Indexed WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Original)=0",verifier){BindByName=true}){
                corrupt.Parameters.Add("Indexed",OracleDbType.Varchar2).Value=indexed;corrupt.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();corrupt.Parameters.Add("Original",OracleDbType.Clob).Value=original;Assert.AreEqual(1,await corrupt.ExecuteNonQueryAsync());
            }
            foreach(var sync in new[]{false,true}){
                if(kind<2){var loaded=kind==0?(sync?provider.LoadAvatarByUsername(indexed):await provider.LoadAvatarByUsernameAsync(indexed)):(sync?provider.LoadAvatarByEmail(indexed):await provider.LoadAvatarByEmailAsync(indexed));Assert.IsTrue(loaded.IsError);Assert.IsNull(loaded.Result);Assert.IsInstanceOfType<InvalidDataException>(loaded.Exception);}
                else{var loaded=kind==2?(sync?provider.LoadAvatarDetailByUsername(indexed):await provider.LoadAvatarDetailByUsernameAsync(indexed)):(sync?provider.LoadAvatarDetailByEmail(indexed):await provider.LoadAvatarDetailByEmailAsync(indexed));Assert.IsTrue(loaded.IsError);Assert.IsNull(loaded.Result);Assert.IsInstanceOfType<InvalidDataException>(loaded.Exception);}
            }
            Assert.AreEqual(original,await Read(verifier,table,id));
            if(kind<2)foreach(var soft in new[]{false,true})foreach(var sync in new[]{false,true}){
                var deleted=kind==0?(sync?provider.DeleteAvatarByUsername(indexed,soft):await provider.DeleteAvatarByUsernameAsync(indexed,soft)):(sync?provider.DeleteAvatarByEmail(indexed,soft):await provider.DeleteAvatarByEmailAsync(indexed,soft));
                Assert.IsTrue(deleted.IsError);Assert.IsFalse(deleted.Result);Assert.IsInstanceOfType<InvalidDataException>(deleted.Exception);
                Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(deleted.Exception));Assert.AreEqual(original,await Read(verifier,table,id));
            }
            await using var cleanup=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND {column}=:Indexed AND DBMS_LOB.COMPARE(DATA_JSON,:Original)=0",verifier){BindByName=true};
            cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Indexed",OracleDbType.Varchar2).Value=indexed;cleanup.Parameters.Add("Original",OracleDbType.Clob).Value=original;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,table,id));File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    [TestMethod]
    public async Task UnicodeClobHistorySurvivesUpdatesDisablementAndLifecycle()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,"unicode-clob.pending.json");Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();
        var payloads=new[]{new string('界',32769)+"🌳first",new string('種',32770)+"🌱second",new string('森',32771)+"🍃third"};
        var row=new Holon{Id=id,Name="owned-unicode-clob",Description=payloads[0]};var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,state="before-native-unicode-clob"}));file.Flush(true);}
            for(var i=0;i<payloads.Length;i++){
                if(i==2)provider.IsVersionControlEnabled=false;row.Description=payloads[i];
                var saved=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);Assert.AreEqual(i+1,row.Version);
                var raw=await Read(verifier,"OASIS_HOLONS",id);Assert.IsNotNull(raw);Assert.IsTrue(raw.Length>32767);using var parsed=JsonDocument.Parse(raw);Assert.AreEqual(payloads[i],parsed.RootElement.GetProperty("Description").GetString());
                var current=await provider.LoadHolonAsync(id,loadChildren:false);Assert.IsFalse(current.IsError,current.Exception?.ToString());Assert.AreEqual(payloads[i],current.Result.Description);
            }
            for(var version=1;version<=3;version++){
                var historical=await provider.LoadHolonAsync(id,loadChildren:false,version:version);Assert.IsFalse(historical.IsError,historical.Exception?.ToString());Assert.AreEqual(payloads[version-1],historical.Result.Description);
            }
            var deleted=await provider.DeleteHolonAsync(id);Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());Assert.IsTrue(deleted.Result.IsDeleted);Assert.AreEqual(4,deleted.Result.Version);
            var tombstoneRaw=await Read(verifier,"OASIS_HOLONS",id);Assert.IsNotNull(tombstoneRaw);using(var tombstone=JsonDocument.Parse(tombstoneRaw)){Assert.AreEqual(payloads[2],tombstone.RootElement.GetProperty("Description").GetString());Assert.AreEqual(3,tombstone.RootElement.GetProperty("_oasis_record_history").GetProperty("records").GetArrayLength());}
            Assert.IsNull((await provider.LoadHolonAsync(id,loadChildren:false)).Result);
            for(var version=1;version<=3;version++){var historical=await provider.LoadHolonAsync(id,loadChildren:false,version:version);Assert.IsFalse(historical.IsError,historical.Exception?.ToString());Assert.AreEqual(payloads[version-1],historical.Result.Description);}
            var removed=await provider.DeleteHolonWithSoftDeleteAsync(id,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    private sealed class DispatchObserverProvider(string connection,Action<OracleCommand> observe):OracleDBOASIS(connection)
    {
        protected override Task<int> ExecuteCanonicalWriteAsync(OracleCommand command)
        {observe(command);return base.ExecuteCanonicalWriteAsync(command);}
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)]
    public async Task GeneratedIdentityRemainsPrivateUntilAcknowledgedAndMatchesNativeRecord(int kind)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"generated-identity-{kind}.pending.json");Assert.IsFalse(File.Exists(journal));
        var name="owned-generated-"+Guid.NewGuid().ToString("N");
        var avatar=new Avatar{Username=name,Email=name+"@example.invalid"};var detail=new AvatarDetail{Username=name,Email=avatar.Email};
        var root=new Holon{Name=name};var child=new Holon{Name=name+"-child"};if(kind==3)root.Children.Add(child);
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase[] sources=kind==0?new[]{avatar}:kind==1?new[]{detail}:kind==2?new[]{root}:new[]{root,child};
        var table=kind==0?"OASIS_AVATARS":kind==1?"OASIS_AVATAR_DETAILS":"OASIS_HOLONS";var dispatched=new List<Guid>();
        var provider=new DispatchObserverProvider(connection,command=>{
            var index=dispatched.Count;Assert.IsTrue(index<sources.Length);Assert.AreEqual(Guid.Empty,sources[index].Id);Assert.AreEqual(0,sources[index].Version);
            Assert.IsFalse(sources[index].ProviderUniqueStorageKey?.ContainsKey(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS)==true);
            var id=new Guid((byte[])command.Parameters["Id"].Value);Assert.AreNotEqual(Guid.Empty,id);Assert.IsFalse(dispatched.Contains(id));dispatched.Add(id);
            // Persist generated original identities before dispatch; a failed assertion/run must not orphan unknown writes.
            using var file=new FileStream(journal,FileMode.Append,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);
            var bytes=JsonSerializer.SerializeToUtf8Bytes(new{ids=dispatched.ToArray(),table,kind,name,state="before-generated-native-dispatch"});file.Write(bytes);file.WriteByte((byte)'\n');file.Flush(true);
        }){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            if(kind==0){var result=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);Assert.AreSame(avatar,result.Result);}
            else if(kind==1){var result=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);Assert.AreSame(detail,result.Result);}
            else{var result=await provider.SaveHolonAsync(root,saveChildren:kind==3);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);Assert.AreSame(root,result.Result);}
            Assert.AreEqual(sources.Length,dispatched.Count);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();var originals=new List<string>();
            for(var i=0;i<sources.Length;i++){
                var source=sources[i];Assert.AreEqual(dispatched[i],source.Id);Assert.AreEqual(1,source.Version);Assert.AreNotEqual(Guid.Empty,source.VersionId);
                Assert.AreEqual(source.Id.ToString(),source.ProviderUniqueStorageKey[NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS]);
                var raw=await Read(verifier,table,source.Id);Assert.IsNotNull(raw);originals.Add(raw);using var parsed=JsonDocument.Parse(raw);
                Assert.AreEqual(source.Id,parsed.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(source.VersionId,parsed.RootElement.GetProperty("VersionId").GetGuid());Assert.AreEqual(1,parsed.RootElement.GetProperty("Version").GetInt32());
                if(source is NextGenSoftware.OASIS.API.Core.Interfaces.IHolon sourceHolon){var native=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>(raw);var token=CosmicConcurrency.GetProviderRevisionId(sourceHolon,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS);Assert.IsTrue(token.HasValue);Assert.AreNotEqual(Guid.Empty,token.Value);Assert.AreEqual(token,CosmicConcurrency.GetProviderRevisionId(native,NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS));}
                if(kind==3&&i==1){Assert.AreEqual(root.Id,child.ParentHolonId);Assert.AreEqual(root.Id,parsed.RootElement.GetProperty("ParentHolonId").GetGuid());}
            }
            // Only acknowledged, independently read original snapshots can be removed.
            for(var i=sources.Length-1;i>=0;i--){
                await using var cleanup=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Original)=0",verifier){BindByName=true};
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=dispatched[i].ToByteArray();cleanup.Parameters.Add("Original",OracleDbType.Clob).Value=originals[i];
                Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,table,dispatched[i]));
            }
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
    private sealed class CreateRaceProvider(string connection):OracleDBOASIS(connection)
    {
        public Func<Task>? BeforeWrite;public int WriteCalls;
        protected override async Task<int> ExecuteCanonicalWriteAsync(OracleCommand command)
        {
            WriteCalls++;if(BeforeWrite!=null){var callback=BeforeWrite;BeforeWrite=null;await callback();}
            return await base.ExecuteCanonicalWriteAsync(command);
        }
    }
    private sealed class LostCommitProvider(string connection):OracleDBOASIS(connection)
    {
        public bool LoseNextCommit;public int CommitCalls;public int LoseCommitNumber;
        public Action? AfterCommit;
        public readonly IOException Lost=new("owned response withheld after actual Oracle SDK commit");
        protected override async Task CommitLifecycleTransactionAsync(OracleTransaction transaction)
        {
            CommitCalls++;await base.CommitLifecycleTransactionAsync(transaction);
            if(AfterCommit!=null){var callback=AfterCommit;AfterCommit=null;callback();}
            if(LoseNextCommit||CommitCalls==LoseCommitNumber){LoseNextCommit=false;throw Lost;}
        }
    }
    [TestMethod]
    public async Task RealSdkCanonicalRecordsSelectorsAndMetadataPaths()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")
            ?? throw new InvalidOperationException("Explicit owned test connection required.");
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")
            ?? throw new InvalidOperationException("Use the retained Oracle environment.");
        var journal=Path.Combine(work,"canonical-records.pending.json");
        var resume=Environment.GetEnvironmentVariable("OASIS_ORACLE_RESUME_CANONICAL")=="1";
        Assert.AreEqual(resume,File.Exists(journal),"Resume requires the original journal; a fresh run requires absence.");
        var provider=new LostCommitProvider(connection){IsVersionControlEnabled=true};var id=Guid.NewGuid();var childId=Guid.NewGuid();var name="owned-oracle-"+id.ToString("N");
        if(resume){
            using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
            Assert.AreEqual("before-native-canonical-records",intent.RootElement.GetProperty("state").GetString());
            id=intent.RootElement.GetProperty("id").GetGuid();name=intent.RootElement.GetProperty("name").GetString()!;
            childId=intent.RootElement.GetProperty("childId").GetGuid();
            Assert.AreEqual("owned-oracle-"+id.ToString("N"),name);
        }
        var avatar=new Avatar{Id=id,Username=name,Email=name+"@example.invalid",VerificationToken=name+"-original-token",ResetToken=name+"-original-reset",RefreshTokens=new(){new RefreshToken{Token=name+"-original-refresh",Expires=DateTime.UtcNow.AddDays(1)}}};
        var detail=new AvatarDetail{Id=id,Username=name,Email=avatar.Email,Karma=321,XP=456};
        void AppendOwnedHistory()=>detail.KarmaAkashicRecords.Add(new KarmaAkashicRecord{
            AvatarId=id,Date=new DateTime(2026,10,20,0,0,0,DateTimeKind.Utc).AddSeconds(detail.KarmaAkashicRecords.Count),
            Karma=detail.KarmaAkashicRecords.Count==0?321:11,TotalKarma=detail.Karma,
            KarmaSourceTitle="Oracle native",KarmaSourceDesc="owned Oracle history",WebLink="https://example.invalid/owned-history",
            Provider=new(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS),
            KarmaSource=new(NextGenSoftware.OASIS.API.Core.Enums.KarmaSourceType.Game),
            KarmaEarntOrLost=new(NextGenSoftware.OASIS.API.Core.Enums.KarmaEarntOrLost.Earnt),
            KarmaTypePositive=new(NextGenSoftware.OASIS.API.Core.Enums.KarmaTypePositive.Other),
            KarmaTypeNegative=new(NextGenSoftware.OASIS.API.Core.Enums.KarmaTypeNegative.None)});
        void AssertOwnedHistory(IAvatarDetail actual,int count){
            Assert.AreEqual(count,actual.KarmaAkashicRecords.Count);
            for(var index=0;index<count;index++){
                var expected=detail.KarmaAkashicRecords[index];var entry=actual.KarmaAkashicRecords[index];
                Assert.AreEqual(id,entry.AvatarId);Assert.AreEqual(expected.Date,entry.Date);Assert.AreEqual(expected.Karma,entry.Karma);
                Assert.AreEqual(expected.TotalKarma,entry.TotalKarma);Assert.AreEqual(expected.Provider.Value,entry.Provider.Value);
                Assert.AreEqual(expected.KarmaSource.Value,entry.KarmaSource.Value);Assert.AreEqual(expected.KarmaEarntOrLost.Value,entry.KarmaEarntOrLost.Value);
                Assert.AreEqual(expected.KarmaTypePositive.Value,entry.KarmaTypePositive.Value);Assert.AreEqual(expected.KarmaTypeNegative.Value,entry.KarmaTypeNegative.Value);
                Assert.AreEqual(expected.KarmaSourceTitle,entry.KarmaSourceTitle);Assert.AreEqual(expected.KarmaSourceDesc,entry.KarmaSourceDesc);Assert.AreEqual(expected.WebLink,entry.WebLink);
            }
        }
        AppendOwnedHistory();
        detail.Inventory.Add(new InventoryItem{Id=Guid.NewGuid(),Name="owned oak seed",Quantity=7,GameSource="Our World"});
        var holon=new Holon{Id=id,Name=name,ParentHolonId=Guid.NewGuid(),CreatedByAvatarId=id,
            MetaData=new(){["owned.fixture"]="literal-dot",["owner's key"]="literal-apostrophe",["quote\"key"]="literal-quote"}};
        holon.Children.Add(new Holon{Id=Guid.NewGuid(),Name="must-not-be-embedded"});
        try{
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Exception?.ToString());Assert.IsTrue(provider.IsProviderActivated);
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            if(!resume){
            foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS","OASIS_HOLONS"})Assert.IsNull(await Read(verifier,table,id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,childId,name,state="before-native-canonical-records"}));file.Flush(true);
            }
            var savedAvatar=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(savedAvatar.IsError,savedAvatar.Exception?.ToString());
            var savedDetail=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(savedDetail.IsError,savedDetail.Exception?.ToString());
            var savedHolon=await provider.SaveHolonAsync(holon,saveChildren:false);Assert.IsFalse(savedHolon.IsError,savedHolon.Exception?.ToString());
            Assert.IsNull(await Read(verifier,"OASIS_HOLONS",childId));
            var childSaved=await provider.SaveHolonAsync(new Holon{Id=childId,ParentHolonId=id,Name="owned independently current child",Version=5},saveChildren:false);Assert.IsFalse(childSaved.IsError,childSaved.Exception?.ToString());
            }else{
                // Reconcile originals; never submit these writes again.
                var originalHolon=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>((await Read(verifier,"OASIS_HOLONS",id))!);
                Assert.AreEqual(id,originalHolon.Id);Assert.AreEqual(name,originalHolon.Name);
                holon=originalHolon;
            }
            Assert.AreEqual(id,(await provider.LoadAvatarAsync(id)).Result.Id);
            Assert.AreEqual(id,(await provider.LoadAvatarByUsernameAsync(name)).Result.Id);
            var byEmail=await provider.LoadAvatarByEmailAsync(avatar.Email);Assert.IsFalse(byEmail.IsError,byEmail.Exception?.ToString());Assert.AreEqual(id,byEmail.Result.Id);
            foreach(var loaded in new[]{await provider.LoadAvatarDetailAsync(id),await provider.LoadAvatarDetailByUsernameAsync(name),await provider.LoadAvatarDetailByEmailAsync(avatar.Email)}){
                Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(321L,loaded.Result.Karma);Assert.AreEqual(detail.XP,loaded.Result.XP);
                Assert.AreEqual(7,loaded.Result.Inventory.Single().Quantity);
                if(!resume)AssertOwnedHistory(loaded.Result,1);
            }
            var readHolon=await provider.LoadHolonAsync(id,loadChildren:false);Assert.IsFalse(readHolon.IsError,readHolon.Exception?.ToString());
            Assert.AreEqual(holon.ParentHolonId,readHolon.Result.ParentHolonId);Assert.AreEqual(0,readHolon.Result.Children.Count);
            foreach(var pair in holon.MetaData){
                var found=await provider.LoadHolonsByMetaDataAsync(pair.Key,(string)pair.Value,holon.HolonType,loadChildren:false);
                Assert.IsFalse(found.IsError,found.Exception?.ToString());Assert.IsTrue(found.Result.Any(row=>row.Id==id));
            }
            var exported=await provider.ExportAllDataForAvatarByIdAsync(holon.CreatedByAvatarId);Assert.IsFalse(exported.IsError,exported.Exception?.ToString());Assert.IsTrue(exported.Result.Any(row=>row.Id==id));
            if(!resume){
                foreach(var synchronous in new[]{false,true}){
                    if(synchronous)provider.IsVersionControlEnabled=false;
                    var oldName=avatar.Username;var oldEmail=avatar.Email;
                    avatar.Username=name+(synchronous?"-sync":"-async");avatar.Email=avatar.Username+"@example.invalid";
                    avatar.VerificationToken=avatar.Username+"-current-token";
                    avatar.ResetToken=avatar.Username+"-current-reset";avatar.RefreshTokens=new(){new RefreshToken{Token=avatar.Username+"-current-refresh",Expires=DateTime.UtcNow.AddDays(1)}};
                    detail.Username=avatar.Username;detail.Email=avatar.Email;detail.Karma+=11;detail.XP+=17;detail.Inventory[0].Quantity+=2;
                    AppendOwnedHistory();
                    holon.Name=avatar.Username;holon.ParentHolonId=Guid.NewGuid();holon.CreatedByAvatarId=Guid.NewGuid();holon.MetaData["owned.fixture"]=avatar.Username;
                    var updateAvatar=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);
                    var updateDetail=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);
                    var updateHolon=synchronous?provider.SaveHolon(holon,saveChildren:false):await provider.SaveHolonAsync(holon,saveChildren:false);
                    Assert.IsFalse(updateAvatar.IsError,updateAvatar.Exception?.ToString());Assert.IsTrue(updateAvatar.IsSaved);Assert.AreSame(avatar,updateAvatar.Result);
                    Assert.IsFalse(updateDetail.IsError,updateDetail.Exception?.ToString());Assert.IsTrue(updateDetail.IsSaved);Assert.AreSame(detail,updateDetail.Result);
                    Assert.IsFalse(updateHolon.IsError,updateHolon.Exception?.ToString());Assert.IsTrue(updateHolon.IsSaved);Assert.AreSame(holon,updateHolon.Result);
                    Assert.AreEqual(synchronous?3:2,avatar.Version);Assert.AreEqual(avatar.Version,detail.Version);Assert.AreEqual(avatar.Version,holon.Version);
                    foreach(var loaded in new[]{await provider.LoadAvatarAsync(id),await provider.LoadAvatarByUsernameAsync(avatar.Username),await provider.LoadAvatarByEmailAsync(avatar.Email)}){
                        Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(avatar.Username,loaded.Result.Username);Assert.AreEqual(avatar.Email,loaded.Result.Email);
                    }
                    Assert.IsNull((await provider.LoadAvatarByUsernameAsync(oldName)).Result);Assert.IsNull((await provider.LoadAvatarByEmailAsync(oldEmail)).Result);
                    foreach(var loaded in new[]{await provider.LoadAvatarDetailAsync(id),await provider.LoadAvatarDetailByUsernameAsync(detail.Username),await provider.LoadAvatarDetailByEmailAsync(detail.Email)}){
                        Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(detail.Karma,loaded.Result.Karma);Assert.AreEqual(detail.XP,loaded.Result.XP);Assert.AreEqual(detail.Inventory[0].Quantity,loaded.Result.Inventory.Single().Quantity);
                        AssertOwnedHistory(loaded.Result,detail.KarmaAkashicRecords.Count);
                    }
                    Assert.IsNull((await provider.LoadAvatarDetailByUsernameAsync(oldName)).Result);Assert.IsNull((await provider.LoadAvatarDetailByEmailAsync(oldEmail)).Result);
                    var loadedHolon=await provider.LoadHolonAsync(id,loadChildren:false);Assert.IsFalse(loadedHolon.IsError,loadedHolon.Exception?.ToString());
                    Assert.AreEqual(holon.Name,loadedHolon.Result.Name);Assert.AreEqual(holon.ParentHolonId,loadedHolon.Result.ParentHolonId);
                    var found=await provider.LoadHolonsByMetaDataAsync("owned.fixture",avatar.Username,holon.HolonType,loadChildren:false);Assert.IsFalse(found.IsError,found.Exception?.ToString());Assert.IsTrue(found.Result.Any(row=>row.Id==id));
                    var raw=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<AvatarDetail>((await Read(verifier,"OASIS_AVATAR_DETAILS",id))!);
                    Assert.AreEqual(detail.Karma,raw.Karma);Assert.AreEqual(detail.XP,raw.XP);Assert.AreEqual(detail.Inventory[0].Quantity,raw.Inventory.Single().Quantity);
                    AssertOwnedHistory(raw,detail.KarmaAkashicRecords.Count);
                }
                foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS","OASIS_HOLONS"}){
                    using var history=JsonDocument.Parse((await Read(verifier,table,id))!);
                    Assert.AreEqual(3,history.RootElement.GetProperty("Version").GetInt32());
                    var archive=history.RootElement.GetProperty("_oasis_record_history").GetProperty("records");
                    Assert.AreEqual(2,archive.GetArrayLength());Assert.AreEqual(1,archive[0].GetProperty("Version").GetInt32());Assert.AreEqual(2,archive[1].GetProperty("Version").GetInt32());
                    if(table=="OASIS_AVATAR_DETAILS"){Assert.AreEqual(456,archive[0].GetProperty("XP").GetInt64());Assert.AreEqual(321,archive[0].GetProperty("Karma").GetInt64());}
                }
            }
            var invalidKeyDelete=await provider.DeleteAvatarAsync(name,false);
            if(!resume){
                foreach(var old in new[]{await provider.LoadAvatarAsync(id,1),await provider.LoadAvatarByProviderKeyAsync(id.ToString(),1),await provider.LoadAvatarByUsernameAsync(name,1),await provider.LoadAvatarByEmailAsync(name+"@example.invalid",1)}){
                    Assert.IsFalse(old.IsError,old.Exception?.ToString());Assert.AreEqual(1,old.Result.Version);Assert.AreEqual(name,old.Result.Username);
                }
                foreach(var old in new[]{await provider.LoadAvatarDetailAsync(id,1),await provider.LoadAvatarDetailByUsernameAsync(name,1),await provider.LoadAvatarDetailByEmailAsync(name+"@example.invalid",1)}){
                    Assert.IsFalse(old.IsError,old.Exception?.ToString());Assert.AreEqual(1,old.Result.Version);Assert.AreEqual(321L,old.Result.Karma);Assert.AreEqual(456L,old.Result.XP);Assert.AreEqual(7,old.Result.Inventory.Single().Quantity);
                    AssertOwnedHistory(old.Result,1);
                }
                var oldHolon=await provider.LoadHolonAsync(id,loadChildren:false,version:1);Assert.IsFalse(oldHolon.IsError,oldHolon.Exception?.ToString());Assert.AreEqual(name,oldHolon.Result.Name);
                foreach(var selectedVersion in new[]{0,1})foreach(var mode in new[]{NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All,NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.Any})foreach(var sync in new[]{false,true}){
                    var unfiltered=sync?provider.LoadHolonsByMetaData(new Dictionary<string,string>(),mode,loadChildren:false,version:selectedVersion):await provider.LoadHolonsByMetaDataAsync(new Dictionary<string,string>(),mode,loadChildren:false,version:selectedVersion);
                    Assert.IsFalse(unfiltered.IsError,unfiltered.Exception?.ToString());Assert.AreEqual(selectedVersion==0?3:1,unfiltered.Result.Single(row=>row.Id==id).Version);
                }
                foreach(var root in new[]{await provider.LoadHolonAsync(id,loadChildrenFromProvider:true,version:1),await provider.LoadHolonAsync(id,loadChildrenFromProvider:true,maxChildDepth:1,version:1)}){
                    Assert.IsFalse(root.IsError,root.Exception?.ToString());Assert.AreEqual(1,root.Result.Version);Assert.AreEqual(5,root.Result.Children.Single().Version);Assert.AreEqual(childId,root.Result.Children.Single().Id);
                }
                var noChildren=await provider.LoadHolonAsync(id,loadChildren:false,loadChildrenFromProvider:true,version:1);Assert.AreEqual(0,noChildren.Result.Children.Count);
                var historicalParents=await provider.LoadHolonsForParentAsync(oldHolon.Result.ParentHolonId,loadChildrenFromProvider:true,version:1);Assert.IsFalse(historicalParents.IsError,historicalParents.Exception?.ToString());Assert.AreEqual(5,historicalParents.Result.Single(row=>row.Id==id).Children.Single().Version);
                foreach(var synchronous in new[]{false,true}){
                    var legacyAll=synchronous?provider.LoadAllHolonsLegacy(version:1,loadChildrenFromProvider:true):await provider.LoadAllHolonsAsyncLegacy(version:1,loadChildrenFromProvider:true);
                    var legacyParent=synchronous?provider.LoadHolonsForParentLegacy(oldHolon.Result.ParentHolonId,version:1,loadChildrenFromProvider:true):await provider.LoadHolonsForParentAsyncLegacy(oldHolon.Result.ParentHolonId,version:1,loadChildrenFromProvider:true);
                    var legacyKey=synchronous?provider.LoadHolonsForParentLegacy(oldHolon.Result.ParentHolonId.ToString(),version:1,loadChildrenFromProvider:true):await provider.LoadHolonsForParentAsyncLegacy(oldHolon.Result.ParentHolonId.ToString(),version:1,loadChildrenFromProvider:true);
                    foreach(var legacy in new[]{legacyAll,legacyParent,legacyKey}){
                        Assert.IsFalse(legacy.IsError,legacy.Exception?.ToString());var root=legacy.Result.Single(row=>row.Id==id);Assert.AreEqual(1,root.Version);Assert.AreEqual(name,root.Name);Assert.AreEqual(5,root.Children.Single().Version);
                    }
                    var clipped=synchronous?provider.LoadAllHolonsLegacy(loadChildren:false,version:1,loadChildrenFromProvider:true):await provider.LoadAllHolonsAsyncLegacy(loadChildren:false,version:1,loadChildrenFromProvider:true);
                    Assert.IsFalse(clipped.IsError,clipped.Exception?.ToString());Assert.AreEqual(0,clipped.Result.Single(row=>row.Id==id).Children.Count);
                }
                foreach(var currentRoots in new[]{await provider.LoadAllHolonsAsync(loadChildrenFromProvider:true),await provider.LoadHolonsForParentAsync(holon.ParentHolonId,loadChildrenFromProvider:true),await provider.LoadHolonsByMetaDataAsync("owned.fixture",avatar.Username,loadChildrenFromProvider:true),await provider.LoadHolonsByMetaDataAsync(new Dictionary<string,string>{{"owned.fixture",avatar.Username}},NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All,loadChildrenFromProvider:true)}){
                    Assert.IsFalse(currentRoots.IsError,currentRoots.Exception?.ToString());Assert.AreEqual(3,currentRoots.Result.Single(row=>row.Id==id).Version);Assert.AreEqual(5,currentRoots.Result.Single(row=>row.Id==id).Children.Single().Version);
                }
                var oldAvatars=await provider.LoadAllAvatarsAsync(1);Assert.IsFalse(oldAvatars.IsError,oldAvatars.Exception?.ToString());Assert.AreEqual(name,oldAvatars.Result.Single(row=>row.Id==id).Username);
                var oldDetails=await provider.LoadAllAvatarDetailsAsync(1);Assert.IsFalse(oldDetails.IsError,oldDetails.Exception?.ToString());Assert.AreEqual(456L,oldDetails.Result.Single(row=>row.Id==id).XP);
                foreach(var historical in new[]{
                    await provider.LoadAllHolonsAsync(version:1),
                    await provider.LoadHolonsForParentAsync(oldHolon.Result.ParentHolonId,version:1),
                    await provider.LoadHolonsByMetaDataAsync("owned.fixture","literal-dot",version:1),
                    await provider.LoadHolonsByMetaDataAsync(new Dictionary<string,string>{{"owned.fixture","literal-dot"}},NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All,version:1),
                    await provider.ExportAllAsync(1),await provider.ExportAllDataForAvatarByIdAsync(id,1),
                    await provider.ExportAllDataForAvatarByUsernameAsync(name,1),await provider.ExportAllDataForAvatarByEmailAsync(name+"@example.invalid",1)}){
                    Assert.IsFalse(historical.IsError,historical.Exception?.ToString());Assert.AreEqual(1,historical.Result.Single(row=>row.Id==id).Version);
                }
                Assert.IsFalse((await provider.LoadHolonsForParentAsync(oldHolon.Result.ParentHolonId)).Result.Any(row=>row.Id==id));
                Assert.IsFalse((await provider.ExportAllDataForAvatarByIdAsync(id)).Result.Any(row=>row.Id==id));
                Assert.AreEqual(0,(await provider.LoadAllHolonsAsync(version:99)).Result.Count());
                var search=await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new(){["owned.fixture"]="literal-dot"}},version:1);
                Assert.IsFalse(search.IsError,search.Exception?.ToString());Assert.AreEqual(1,search.Result.SearchResultHolons.Single(row=>row.Id==id).Version);Assert.AreEqual(5,search.Result.SearchResultHolons.Single(row=>row.Id==id).Children.Single().Version);
                foreach(var exportedRoot in new[]{await provider.ExportAllAsync(1),await provider.ExportAllDataForAvatarByIdAsync(id,1),await provider.ExportAllDataForAvatarByUsernameAsync(name,1),await provider.ExportAllDataForAvatarByEmailAsync(name+"@example.invalid",1)}){
                    Assert.IsFalse(exportedRoot.IsError,exportedRoot.Exception?.ToString());Assert.AreEqual(5,exportedRoot.Result.Single(row=>row.Id==id).Children.Single().Version);
                }
                var historicalCredential=await provider.LoadAvatarByVerificationTokenAsync(name+"-original-token",1);Assert.IsTrue(historicalCredential.IsError);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(historicalCredential.Exception);
                Assert.IsNull((await provider.LoadAvatarByVerificationTokenAsync(name+"-original-token")).Result);
                var currentCredential=await provider.LoadAvatarByVerificationTokenAsync(avatar.VerificationToken);Assert.IsFalse(currentCredential.IsError,currentCredential.Exception?.ToString());Assert.AreEqual(id,currentCredential.Result.Id);Assert.AreEqual(3,currentCredential.Result.Version);
                foreach(var synchronous in new[]{false,true}){
                    foreach(var current in new[]{synchronous?provider.LoadAvatarByResetToken(avatar.ResetToken):await provider.LoadAvatarByResetTokenAsync(avatar.ResetToken),synchronous?provider.LoadAvatarByRefreshToken(avatar.RefreshTokens.Single().Token):await provider.LoadAvatarByRefreshTokenAsync(avatar.RefreshTokens.Single().Token)}){
                        Assert.IsFalse(current.IsError,current.Exception?.ToString());Assert.AreEqual(id,current.Result.Id);Assert.AreEqual(3,current.Result.Version);
                    }
                    foreach(var old in new[]{synchronous?provider.LoadAvatarByResetToken(name+"-original-reset"):await provider.LoadAvatarByResetTokenAsync(name+"-original-reset"),synchronous?provider.LoadAvatarByRefreshToken(name+"-original-refresh"):await provider.LoadAvatarByRefreshTokenAsync(name+"-original-refresh")}){Assert.IsTrue(old.IsError);Assert.IsNull(old.Result);}
                    foreach(var historical in new[]{synchronous?provider.LoadAvatarByResetToken(name+"-original-reset",1):await provider.LoadAvatarByResetTokenAsync(name+"-original-reset",1),synchronous?provider.LoadAvatarByRefreshToken(name+"-original-refresh",1):await provider.LoadAvatarByRefreshTokenAsync(name+"-original-refresh",1)}){Assert.IsTrue(historical.IsError);Assert.IsNull(historical.Result);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(historical.Exception);}
                }
                Assert.IsNull((await provider.LoadAvatarAsync(id,99)).Result);Assert.IsNull((await provider.LoadAvatarDetailAsync(id,99)).Result);Assert.IsNull((await provider.LoadHolonAsync(id,version:99)).Result);
            }
            if(!resume){
                var caller=holon;var callerVersion=caller.Version;var callerVersionId=caller.VersionId;var commits=provider.CommitCalls;
                caller.Name+="-lost-save-ack";provider.LoseNextCommit=true;
                var lostSave=await provider.SaveHolonAsync(caller,saveChildren:false);Assert.IsTrue(lostSave.IsError);Assert.IsFalse(lostSave.IsSaved);Assert.IsNull(lostSave.Result);
                Assert.AreEqual(commits+1,provider.CommitCalls);Assert.AreEqual(callerVersion,caller.Version);Assert.AreEqual(callerVersionId,caller.VersionId);
                Assert.IsInstanceOfType<OracleLifecycleReconciliationException>(lostSave.Exception);Assert.AreSame(provider.Lost,lostSave.Exception.InnerException);
                var committed=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>((await Read(verifier,"OASIS_HOLONS",id))!);
                Assert.AreEqual(callerVersion+1,committed.Version);Assert.AreEqual(caller.Name,committed.Name);Assert.AreEqual(callerVersionId,committed.PreviousVersionId);
                holon=committed; // Original committed outcome reconciled by independent native read; no resubmission.
            }
            Assert.IsTrue(invalidKeyDelete.IsError);Assert.IsInstanceOfType<FormatException>(invalidKeyDelete.Exception);
            Assert.IsNotNull(await Read(verifier,"OASIS_AVATARS",id));
            provider.IsVersionControlEnabled=true;
            if(!resume){
                var beforeAvatar=await Read(verifier,"OASIS_AVATARS",id);var beforeDetail=await Read(verifier,"OASIS_AVATAR_DETAILS",id);
                var trigger="OASIS_TEST_"+id.ToString("N")[..20];
                await using(var create=new OracleCommand($"CREATE TRIGGER {trigger} BEFORE UPDATE ON OASIS_AVATAR_DETAILS FOR EACH ROW BEGIN IF :NEW.ID=HEXTORAW('{Convert.ToHexString(id.ToByteArray())}') THEN RAISE_APPLICATION_ERROR(-20001,'owned lifecycle rollback proof'); END IF; END;",verifier))await create.ExecuteNonQueryAsync();
                var rejected=await provider.DeleteAvatarAsync(id,true);Assert.IsTrue(rejected.IsError);Assert.IsFalse(rejected.Result);
                Assert.IsFalse(rejected.IsSaved);Assert.IsFalse(rejected.IsDeleted);Assert.AreEqual(0,rejected.DeletedCount);
                Assert.IsInstanceOfType<OracleLifecycleReconciliationException>(rejected.Exception);
                Assert.IsInstanceOfType<OracleException>(rejected.Exception.InnerException);Assert.AreEqual(20001,((OracleException)rejected.Exception.InnerException!).Number);
                Assert.AreEqual(beforeAvatar,await Read(verifier,"OASIS_AVATARS",id),"First row update must roll back when second-row native update fails.");
                Assert.AreEqual(beforeDetail,await Read(verifier,"OASIS_AVATAR_DETAILS",id));
                await using(var drop=new OracleCommand($"DROP TRIGGER {trigger}",verifier))await drop.ExecuteNonQueryAsync();
            }
            if(!resume){
                var commits=provider.CommitCalls;provider.LoseNextCommit=true;
                var lost=await provider.DeleteAvatarAsync(id,true);Assert.IsTrue(lost.IsError);Assert.IsFalse(lost.Result);
                Assert.IsFalse(lost.IsSaved);Assert.IsFalse(lost.IsDeleted);Assert.AreEqual(0,lost.DeletedCount);
                Assert.AreEqual(commits+1,provider.CommitCalls);Assert.IsInstanceOfType<OracleLifecycleReconciliationException>(lost.Exception);Assert.AreSame(provider.Lost,lost.Exception.InnerException);
                Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(lost.Exception));
                var committedAvatar=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Avatar>((await Read(verifier,"OASIS_AVATARS",id))!);
                var committedDetail=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<AvatarDetail>((await Read(verifier,"OASIS_AVATAR_DETAILS",id))!);
                Assert.IsTrue(committedAvatar.IsDeleted);Assert.IsTrue(committedDetail.IsDeleted);Assert.AreEqual(committedAvatar.DeletedDate,committedDetail.DeletedDate);
                // Original native outcome is now independently reconciled; only then exercise repeat semantics below.
            }
            var soft=await provider.DeleteAvatarAsync(id,true);Assert.IsFalse(soft.IsError,soft.Exception?.ToString());Assert.IsTrue(soft.Result);
            var deletedAvatar=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Avatar>((await Read(verifier,"OASIS_AVATARS",id))!);
            var deletedDetail=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<AvatarDetail>((await Read(verifier,"OASIS_AVATAR_DETAILS",id))!);
            Assert.IsTrue(deletedAvatar.IsDeleted);Assert.IsFalse(deletedAvatar.IsActive);Assert.IsTrue(deletedDetail.IsDeleted);Assert.IsFalse(deletedDetail.IsActive);
            Assert.AreEqual(deletedAvatar.DeletedDate,deletedDetail.DeletedDate);var deletedAt=deletedAvatar.DeletedDate;
            Assert.AreEqual(avatar.Version+1,deletedAvatar.Version);Assert.AreEqual(detail.Version+1,deletedDetail.Version);
            var retainedDetail=await Read(verifier,"OASIS_AVATAR_DETAILS",id);
            using(var history=JsonDocument.Parse(retainedDetail!))Assert.AreEqual(1,history.RootElement.GetProperty("_oasis_record_history").GetProperty("records")[0].GetProperty("Version").GetInt32());
            Assert.IsNull((await provider.LoadAvatarAsync(id)).Result);Assert.IsNull((await provider.LoadAvatarDetailAsync(id)).Result);
            if(!resume){Assert.AreEqual(1,(await provider.LoadAvatarAsync(id,1)).Result.Version);Assert.AreEqual(1,(await provider.LoadAvatarDetailAsync(id,1)).Result.Version);}
            Assert.IsNull((await provider.LoadAvatarDetailByUsernameAsync(deletedDetail.Username)).Result);Assert.IsNull((await provider.LoadAvatarDetailByEmailAsync(deletedDetail.Email)).Result);
            var details=await provider.LoadAllAvatarDetailsAsync();Assert.IsFalse(details.IsError,details.Exception?.ToString());Assert.IsFalse(details.Result.Any(row=>row.Id==id));
            provider.IsVersionControlEnabled=false;
            var repeated=await provider.DeleteAvatarAsync(id,true);Assert.IsFalse(repeated.IsError,repeated.Exception?.ToString());Assert.AreEqual(retainedDetail,await Read(verifier,"OASIS_AVATAR_DETAILS",id));
            var repeatedDetail=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<AvatarDetail>((await Read(verifier,"OASIS_AVATAR_DETAILS",id))!);
            Assert.AreEqual(deletedAt,repeatedDetail.DeletedDate);
            var keyDelete=await provider.DeleteAvatarAsync(id.ToString(),false);Assert.IsFalse(keyDelete.IsError,keyDelete.Exception?.ToString());Assert.IsTrue(keyDelete.Result);
            Assert.IsNull(await Read(verifier,"OASIS_AVATARS",id));
            Assert.IsNull(await Read(verifier,"OASIS_AVATAR_DETAILS",id));
            provider.IsVersionControlEnabled=true;
            if(!resume){
                var commits=provider.CommitCalls;provider.LoseNextCommit=true;
                var lost=await provider.DeleteHolonAsync(id);Assert.IsTrue(lost.IsError);Assert.IsNull(lost.Result);Assert.AreEqual(commits+1,provider.CommitCalls);
                Assert.IsInstanceOfType<OracleLifecycleReconciliationException>(lost.Exception);Assert.AreSame(provider.Lost,lost.Exception.InnerException);
                Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(lost.Exception));
                var committed=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>((await Read(verifier,"OASIS_HOLONS",id))!);
                Assert.IsTrue(committed.IsDeleted);Assert.IsFalse(committed.IsActive);Assert.AreEqual(holon.ParentHolonId,committed.ParentHolonId);
            }
            var holonDelete=await provider.DeleteHolonAsync(id);Assert.IsFalse(holonDelete.IsError,holonDelete.Exception?.ToString());
            Assert.IsTrue(holonDelete.Result.IsDeleted);Assert.IsFalse(holonDelete.Result.IsActive);
            var tombstone=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>((await Read(verifier,"OASIS_HOLONS",id))!);
            Assert.IsTrue(tombstone.IsDeleted);Assert.IsFalse(tombstone.IsActive);Assert.AreEqual(holonDelete.Result.DeletedDate,tombstone.DeletedDate);
            Assert.AreEqual(holon.ParentHolonId,tombstone.ParentHolonId);Assert.IsNull((await provider.LoadHolonAsync(id)).Result);
            var repeatHolon=provider.DeleteHolonWithSoftDelete(id.ToString(),true);Assert.IsFalse(repeatHolon.IsError,repeatHolon.Exception?.ToString());Assert.IsTrue(repeatHolon.Result);
            var repeatedHolon=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>((await Read(verifier,"OASIS_HOLONS",id))!);
            Assert.AreEqual(tombstone.DeletedDate,repeatedHolon.DeletedDate);
            var hardHolon=await provider.DeleteHolonWithSoftDeleteAsync(id.ToString(),false);Assert.IsFalse(hardHolon.IsError,hardHolon.Exception?.ToString());Assert.IsTrue(hardHolon.Result);
            Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));
            var childDelete=await provider.DeleteHolonWithSoftDeleteAsync(childId,false);Assert.IsFalse(childDelete.IsError,childDelete.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",childId));
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();Assert.IsFalse(provider.IsProviderActivated);}
    }

    [TestMethod]
    public async Task DuplicateIdentityIsRejectedInsteadOfSelectingArbitraryAvatar()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,"identity-ambiguity.pending.json");
        var resume=Environment.GetEnvironmentVariable("OASIS_ORACLE_RECONCILE_AMBIGUITY")=="1";
        Assert.AreEqual(resume,File.Exists(journal));
        var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};var name="owned-duplicate-"+ids[0].ToString("N");
        if(resume){
            using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
            Assert.AreEqual("before-native-duplicate-identities",intent.RootElement.GetProperty("state").GetString());
            ids=intent.RootElement.GetProperty("ids").EnumerateArray().Select(value=>value.GetGuid()).ToArray();
            Assert.AreEqual(2,ids.Length);Assert.AreNotEqual(ids[0],ids[1]);name="owned-duplicate-"+ids[0].ToString("N");
            Assert.AreEqual(name,intent.RootElement.GetProperty("name").GetString());
        }
        var email=name+"@example.invalid";var provider=new OracleDBOASIS(connection);
        try{
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Exception?.ToString());
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            if(!resume){
                foreach(var id in ids)foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"})Assert.IsNull(await Read(verifier,table,id));
                using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                    await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,name,state="before-native-duplicate-identities"}));file.Flush(true);
                }
                foreach(var id in ids){
                    var avatar=await provider.SaveAvatarAsync(new Avatar{Id=id,Username=name,Email=email,VerificationToken=name+"-verify",ResetToken=name+"-reset",RefreshTokens=new(){new RefreshToken{Token=name+"-refresh",Expires=DateTime.UtcNow.AddDays(1)}}});Assert.IsFalse(avatar.IsError,avatar.Exception?.ToString());
                    var detail=await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=name,Email=email});Assert.IsFalse(detail.IsError,detail.Exception?.ToString());
                }
            }
            foreach(var id in ids){
                var avatar=await provider.LoadAvatarAsync(id);Assert.IsFalse(avatar.IsError,avatar.Exception?.ToString());Assert.AreEqual(name,avatar.Result.Username);Assert.AreEqual(email,avatar.Result.Email);
                var detail=await provider.LoadAvatarDetailAsync(id);Assert.IsFalse(detail.IsError,detail.Exception?.ToString());Assert.AreEqual(name,detail.Result.Username);Assert.AreEqual(email,detail.Result.Email);
            }
            Check(await provider.LoadAvatarByUsernameAsync(name));Check(provider.LoadAvatarByUsername(name));
            Check(await provider.LoadAvatarByEmailAsync(email));Check(provider.LoadAvatarByEmail(email));
            Check(await provider.LoadAvatarDetailByUsernameAsync(name));Check(provider.LoadAvatarDetailByUsername(name));
            Check(await provider.LoadAvatarDetailByEmailAsync(email));Check(provider.LoadAvatarDetailByEmail(email));
            Check(await provider.ExportAllDataForAvatarByUsernameAsync(name));Check(provider.ExportAllDataForAvatarByUsername(name));
            Check(await provider.ExportAllDataForAvatarByEmailAsync(email));Check(provider.ExportAllDataForAvatarByEmail(email));
            foreach(var credentials in new[]{await provider.LoadAvatarByVerificationTokenAsync(name+"-verify"),provider.LoadAvatarByVerificationToken(name+"-verify"),await provider.LoadAvatarByResetTokenAsync(name+"-reset"),provider.LoadAvatarByResetToken(name+"-reset"),await provider.LoadAvatarByRefreshTokenAsync(name+"-refresh"),provider.LoadAvatarByRefreshToken(name+"-refresh")}){
                Assert.IsTrue(credentials.IsError);Assert.IsNull(credentials.Result);Assert.IsFalse(credentials.IsLoaded);Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.AmbiguousIdentity,credentials.ErrorCode);
            }
            foreach(var id in ids)foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"}){
                var actual=await Read(verifier,table,id);Assert.IsNotNull(actual);
                await using var delete=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                delete.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=actual;
                Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,table,id));
            }
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
        static void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){
            Assert.IsTrue(result.IsError,"Duplicate identity must not silently pick the first record.");Assert.IsNull(result.Result);
            Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
        }
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task ActualConcurrentCreatorWinsWithoutOverwriteOrCallerPublication(int kind)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"create-race-{kind}.pending.json");Assert.IsFalse(File.Exists(journal));
        var id=Guid.NewGuid();var name="owned-race-"+id.ToString("N");var table=kind==0?"OASIS_AVATARS":kind==1?"OASIS_AVATAR_DETAILS":"OASIS_HOLONS";
        var provider=new CreateRaceProvider(connection){IsVersionControlEnabled=true};var rival=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase Row(string marker){
            NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase row=kind==0?new Avatar{Id=id,Username=name+marker,Email=name+marker+"@example.invalid"}:kind==1?new AvatarDetail{Id=id,Username=name+marker,Email=name+marker+"@example.invalid"}:new Holon{Id=id,Name=name+marker};
            row.MetaData=new(){["owned_race"]=marker};return row;
        }
        var loser=Row("loser");var originalKeys=loser.ProviderUniqueStorageKey;
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);Assert.IsFalse((await rival.ActivateProviderAsync()).IsError);
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();Assert.IsNull(await Read(verifier,table,id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,table,kind,state="before-native-create-race"}));file.Flush(true);
            }
            string? winnerJson=null;
            provider.BeforeWrite=async()=>{
                var winner=Row("winner");
                if(winner is Avatar avatar)Success(await rival.SaveAvatarAsync(avatar));else if(winner is AvatarDetail detail)Success(await rival.SaveAvatarDetailAsync(detail));else Success(await rival.SaveHolonAsync((Holon)winner,saveChildren:false));
                winnerJson=await Read(verifier,table,id);Assert.IsNotNull(winnerJson);
            };
            if(loser is Avatar avatar)Conflict(await provider.SaveAvatarAsync(avatar));else if(loser is AvatarDetail detail)Conflict(await provider.SaveAvatarDetailAsync(detail));else Conflict(await provider.SaveHolonAsync((Holon)loser,saveChildren:false));
            Assert.AreEqual(1,provider.WriteCalls);Assert.AreEqual(0,loser.Version);Assert.AreEqual(Guid.Empty,loser.VersionId);Assert.AreSame(originalKeys,loser.ProviderUniqueStorageKey);Assert.AreEqual(0,originalKeys.Count);
            var actual=await Read(verifier,table,id);Assert.AreEqual(winnerJson,actual);
            using(var document=JsonDocument.Parse(actual!)){Assert.AreEqual(1,document.RootElement.GetProperty("Version").GetInt32());Assert.AreEqual("winner",document.RootElement.GetProperty("MetaData").GetProperty("owned_race").GetString());}
            var first=Row("update-a");var second=Row("update-b");
            async Task Save(OracleDBOASIS writer,NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase row){
                if(row is Avatar a)Success(await writer.SaveAvatarAsync(a));else if(row is AvatarDetail d)Success(await writer.SaveAvatarDetailAsync(d));else Success(await writer.SaveHolonAsync((Holon)row,saveChildren:false));
            }
            await Task.WhenAll(Save(provider,first),Save(rival,second));
            CollectionAssert.AreEquivalent(new[]{2,3},new[]{first.Version,second.Version});
            actual=await Read(verifier,table,id);
            using(var document=JsonDocument.Parse(actual!)){
                Assert.AreEqual(3,document.RootElement.GetProperty("Version").GetInt32());
                var history=document.RootElement.GetProperty("_oasis_record_history").GetProperty("records");Assert.AreEqual(2,history.GetArrayLength());
                Assert.AreEqual("winner",history[0].GetProperty("MetaData").GetProperty("owned_race").GetString());
                CollectionAssert.AreEquivalent(new[]{"update-a","update-b"},new[]{history[1].GetProperty("MetaData").GetProperty("owned_race").GetString(),document.RootElement.GetProperty("MetaData").GetProperty("owned_race").GetString()});
            }
            var corrupt=Newtonsoft.Json.Linq.JObject.Parse(actual!);
            if(kind==0)corrupt["_oasis_record_history"]!["records"]![0]!["Version"]=3;
            else if(kind==1)corrupt["_oasis_record_history"]!["schema"]=999;
            else corrupt["id"]=Guid.NewGuid().ToString();
            var corruptJson=corrupt.ToString(Newtonsoft.Json.Formatting.None);
            await using(var inject=new OracleCommand($"UPDATE {table} SET DATA_JSON=:Corrupt WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true}){
                inject.Parameters.Add("Corrupt",OracleDbType.Clob).Value=corruptJson;inject.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();inject.Parameters.Add("Expected",OracleDbType.Clob).Value=actual;
                Assert.AreEqual(1,await inject.ExecuteNonQueryAsync());
            }
            Assert.AreEqual(corruptJson,await Read(verifier,table,id));provider.IsVersionControlEnabled=false;
            var rejected=Row("must-reject-corruption");var writes=provider.WriteCalls;
            if(rejected is Avatar a)Corruption(await provider.SaveAvatarAsync(a));else if(rejected is AvatarDetail d)Corruption(await provider.SaveAvatarDetailAsync(d));else Corruption(await provider.SaveHolonAsync((Holon)rejected,saveChildren:false));
            Assert.AreEqual(writes,provider.WriteCalls,"Corrupt original must be rejected BEFORE mutation dispatch.");Assert.AreEqual(0,rejected.Version);Assert.AreEqual(Guid.Empty,rejected.VersionId);Assert.AreEqual(0,rejected.ProviderUniqueStorageKey.Count);
            if(kind==0)Corruption(await provider.LoadAvatarAsync(id));else if(kind==1)Corruption(await provider.LoadAvatarDetailAsync(id));else Corruption(await provider.LoadHolonAsync(id,loadChildren:false));
            if(kind==0)Corruption(await provider.DeleteAvatarAsync(id));else if(kind==1)Corruption(await provider.LoadAllAvatarDetailsAsync());else {Corruption(await provider.DeleteHolonAsync(id));Corruption(await provider.LoadAllHolonsAsync(loadChildren:false));}
            actual=await Read(verifier,table,id);Assert.AreEqual(corruptJson,actual,"Do not trim, rewrite or silently repair the corrupt original.");
            await using var delete=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
            delete.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=actual;
            Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,table,id));File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();await rival.DeActivateProviderAsync();}
        static void Success<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);}
        static void Conflict<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.IsInstanceOfType<InvalidDataException>(result.Exception);
            Assert.IsInstanceOfType<OracleException>(result.Exception.InnerException);Assert.AreEqual(1,((OracleException)result.Exception.InnerException!).Number);
            Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
        }
        static void Corruption<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);if(result.Result is bool deleted)Assert.IsFalse(deleted);else Assert.IsNull(result.Result);Assert.IsInstanceOfType<InvalidDataException>(result.Exception);}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task BatchAndImportStopAfterUncertainSecondCommit(bool import)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"batch-uncertain-{import}.pending.json");Assert.IsFalse(File.Exists(journal));
        var rows=Enumerable.Range(0,3).Select(index=>new Holon{Id=Guid.NewGuid(),Name="owned batch boundary "+index}).ToArray();
        var provider=new LostCommitProvider(connection){IsVersionControlEnabled=true,LoseCommitNumber=2};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            foreach(var row in rows)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=rows.Select(row=>row.Id).ToArray(),import,state="before-native-batch-uncertainty"}));file.Flush(true);
            }
            Exception cause;
            if(import){var result=await provider.ImportAsync(rows);Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsSaved);Assert.AreEqual(1,result.SavedCount);cause=result.Exception;}
            else{var result=await provider.SaveHolonsAsync(rows,saveChildren:false,curentChildDepth:0,continueOnError:true);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(1,result.SavedCount);Assert.AreEqual(1,result.Result.Count());cause=result.Exception;}
            Assert.IsInstanceOfType<OraclePartialMutationException>(cause);CollectionAssert.AreEqual(new[]{rows[0].Id},((OraclePartialMutationException)cause).AcknowledgedRecordIds.ToArray());
            Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(cause));Assert.AreSame(provider.Lost,cause.InnerException!.InnerException);
            Assert.AreEqual(2,provider.CommitCalls);Assert.AreEqual(1,rows[0].Version);Assert.AreEqual(0,rows[1].Version);Assert.AreEqual(0,rows[2].Version);Assert.IsNull(await Read(verifier,"OASIS_HOLONS",rows[2].Id));
            foreach(var row in rows.Take(2)){
                var actual=await Read(verifier,"OASIS_HOLONS",row.Id);Assert.IsNotNull(actual);using(var document=JsonDocument.Parse(actual)){Assert.AreEqual(row.Id,document.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(row.Name,document.RootElement.GetProperty("Name").GetString());Assert.AreEqual(1,document.RootElement.GetProperty("Version").GetInt32());}
                await using var delete=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                delete.Parameters.Add("Id",OracleDbType.Raw).Value=row.Id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=actual;
                Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));
            }
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    [TestMethod]
    public async Task SharedGraphPlanPersistsThreeLevelsAndPublishesParentIds()
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,"graph-three-level.pending.json");Assert.IsFalse(File.Exists(journal));
        var root=new Holon{Id=Guid.NewGuid(),Name="owned graph root"};var child=new Holon{Id=Guid.NewGuid(),Name="owned graph child"};var leaf=new Holon{Id=Guid.NewGuid(),Name="owned graph leaf"};root.Children.Add(child);child.Children.Add(leaf);
        var rows=new[]{root,child,leaf};var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();foreach(var row in rows)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=rows.Select(row=>row.Id).ToArray(),state="before-native-three-level-graph"}));file.Flush(true);}
            var saved=await provider.SaveHolonAsync(root);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);Assert.AreSame(root,saved.Result);Assert.AreEqual(root.Id,child.ParentHolonId);Assert.AreEqual(child.Id,leaf.ParentHolonId);
            foreach(var row in rows){Assert.AreEqual(1,row.Version);var actual=await Read(verifier,"OASIS_HOLONS",row.Id);Assert.IsNotNull(actual);using var document=JsonDocument.Parse(actual);Assert.AreEqual(row.ParentHolonId,document.RootElement.GetProperty("ParentHolonId").GetGuid());}
            var deep=await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);Assert.IsFalse(deep.IsError,deep.Exception?.ToString());Assert.AreEqual(leaf.Id,deep.Result.Children.Single().Children.Single().Id);
            var shallow=await provider.LoadHolonAsync(root.Id,maxChildDepth:1,loadChildrenFromProvider:true);Assert.AreEqual(0,shallow.Result.Children.Single().Children.Count);
            foreach(var version in new[]{0,1}){
                var bounded=await provider.LoadHolonsForParentAsync(root.Id,maxChildDepth:1,loadChildrenFromProvider:true,version:version);
                Assert.IsFalse(bounded.IsError,bounded.Exception?.ToString());Assert.AreEqual(child.Id,bounded.Result.Single().Id);Assert.AreEqual(0,bounded.Result.Single().Children.Count);
                var boundedSync=provider.LoadHolonsForParent(root.Id.ToString(),maxChildDepth:1,loadChildrenFromProvider:true,version:version);
                Assert.IsFalse(boundedSync.IsError,boundedSync.Exception?.ToString());Assert.AreEqual(0,boundedSync.Result.Single().Children.Count);
            }
            foreach(var row in rows.Reverse()){var deleted=await provider.DeleteHolonWithSoftDeleteAsync(row.Id,false);Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));}File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)]
    public async Task GraphSaveClippingDoesNotPersistOrPublishExcludedDescendants(int mode)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"graph-clip-{mode}.pending.json");Assert.IsFalse(File.Exists(journal));
        var rows=Enumerable.Range(0,3).Select(index=>new Holon{Id=Guid.NewGuid(),Name="owned clipped graph "+index}).ToArray();rows[0].Children.Add(rows[1]);rows[1].Children.Add(rows[2]);
        var selected=mode==0||mode==3?1:2;var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();foreach(var row in rows)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=rows.Select(row=>row.Id).ToArray(),mode,state="before-native-graph-clipping"}));file.Flush(true);}
            if(mode==3){var saved=await provider.SaveHolonsAsync(new[]{rows[0]},maxChildDepth:1,curentChildDepth:1);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);}
            else{var saved=await provider.SaveHolonAsync(rows[0],saveChildren:mode!=0,recursive:mode!=1,maxChildDepth:mode==2?1:0);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);}
            for(var index=0;index<rows.Length;index++){
                var actual=await Read(verifier,"OASIS_HOLONS",rows[index].Id);
                if(index>=selected){Assert.IsNull(actual);Assert.AreEqual(0,rows[index].Version);Assert.AreEqual(Guid.Empty,rows[index].ParentHolonId);Assert.AreEqual(0,rows[index].ProviderUniqueStorageKey.Count);}
                else{Assert.IsNotNull(actual);Assert.AreEqual(1,rows[index].Version);if(index>0)Assert.AreEqual(rows[index-1].Id,rows[index].ParentHolonId);}
            }
            foreach(var row in rows.Take(selected).Reverse()){var removed=await provider.DeleteHolonWithSoftDeleteAsync(row.Id,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));}File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task DefiniteFailureSkipsDescendantsAndHonorsIndependentContinuation(bool continueOnError)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"graph-definite-{continueOnError}.pending.json");Assert.IsFalse(File.Exists(journal));
        var bad=new Holon{Id=Guid.NewGuid(),Name="owned failing graph parent"};var child=new Holon{Id=Guid.NewGuid(),Name="owned skipped descendant"};var independent=new Holon{Id=Guid.NewGuid(),Name="owned independent root"};bad.Children.Add(child);
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();foreach(var row in new[]{bad,child,independent})Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=new[]{bad.Id,child.Id,independent.Id},continueOnError,state="before-native-definite-graph-failure"}));file.Flush(true);}
            var initial=await provider.SaveHolonAsync(new Holon{Id=bad.Id,Name=bad.Name},saveChildren:false);Assert.IsFalse(initial.IsError,initial.Exception?.ToString());
            var before=await Read(verifier,"OASIS_HOLONS",bad.Id);var corrupt=Newtonsoft.Json.Linq.JObject.Parse(before!);corrupt["_oasis_record_history"]!["schema"]=999;var rejectedOriginal=corrupt.ToString(Newtonsoft.Json.Formatting.None);
            await using(var inject=new OracleCommand("UPDATE OASIS_HOLONS SET DATA_JSON=:Corrupt WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true}){inject.Parameters.Add("Corrupt",OracleDbType.Clob).Value=rejectedOriginal;inject.Parameters.Add("Id",OracleDbType.Raw).Value=bad.Id.ToByteArray();inject.Parameters.Add("Expected",OracleDbType.Clob).Value=before;Assert.AreEqual(1,await inject.ExecuteNonQueryAsync());}
            var result=await provider.SaveHolonsAsync(new[]{bad,independent},curentChildDepth:0,continueOnError:continueOnError);Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(continueOnError?1:0,result.Result.Count());
            Assert.IsNull(await Read(verifier,"OASIS_HOLONS",child.Id));Assert.AreEqual(0,child.Version);Assert.AreEqual(Guid.Empty,child.ParentHolonId);Assert.AreEqual(0,bad.Version);Assert.AreEqual(rejectedOriginal,await Read(verifier,"OASIS_HOLONS",bad.Id));
            if(continueOnError){Assert.IsInstanceOfType<OraclePartialMutationException>(result.Exception);CollectionAssert.AreEqual(new[]{independent.Id},((OraclePartialMutationException)result.Exception).AcknowledgedRecordIds.ToArray());Assert.AreEqual(1,independent.Version);Assert.IsNotNull(await Read(verifier,"OASIS_HOLONS",independent.Id));var removed=await provider.DeleteHolonWithSoftDeleteAsync(independent.Id,false);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",independent.Id));}
            else{Assert.IsInstanceOfType<InvalidDataException>(result.Exception);Assert.IsNull(await Read(verifier,"OASIS_HOLONS",independent.Id));Assert.AreEqual(0,independent.Version);}
            await using var cleanup=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=bad.Id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=rejectedOriginal;
            Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,"OASIS_HOLONS",bad.Id));File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)]
    public async Task AcknowledgedSaveCannotOverwriteChangedCallerContract(int kind)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"caller-publication-{kind}.pending.json");Assert.IsFalse(File.Exists(journal));
        var id=Guid.NewGuid();var childId=Guid.NewGuid();var editedId=Guid.NewGuid();var name="owned publication "+id.ToString("N");
        var table=kind==0?"OASIS_AVATARS":kind==1?"OASIS_AVATAR_DETAILS":"OASIS_HOLONS";
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase caller=kind==0?new Avatar{Id=id,Username=id.ToString("N"),Email=id.ToString("N")+"@example.invalid"}:kind==1?new AvatarDetail{Id=id,Username=id.ToString("N"),Email=id.ToString("N")+"@example.invalid"}:new Holon{Id=id,Name=name};
        if(caller is AvatarDetail detail)detail.Inventory.Add(new InventoryItem{Id=Guid.NewGuid(),Quantity=1});
        if(kind==3)((Holon)caller).Children.Add(new Holon{Id=childId,Name="owned unattempted child"});
        var keys=caller.ProviderUniqueStorageKey;var provider=new LostCommitProvider(connection){IsVersionControlEnabled=true};
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();Assert.IsNull(await Read(verifier,table,id));Assert.IsNull(await Read(verifier,"OASIS_HOLONS",childId));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,childId,editedId,table,kind,state="before-native-caller-publication"}));file.Flush(true);}
            provider.AfterCommit=()=>{if(kind==0)caller.Id=editedId;else if(caller is AvatarDetail d)d.Inventory[0].Quantity=2;else caller.Name=name+" edited";};
            Exception cause;int acknowledgedCount;
            if(caller is Avatar avatar){var saved=await provider.SaveAvatarAsync(avatar);Check(saved);cause=saved.Exception;acknowledgedCount=saved.SavedCount;}
            else if(caller is AvatarDetail d){var saved=await provider.SaveAvatarDetailAsync(d);Check(saved);cause=saved.Exception;acknowledgedCount=saved.SavedCount;}
            else{var saved=await provider.SaveHolonAsync((Holon)caller,saveChildren:kind==3);Check(saved);cause=saved.Exception;acknowledgedCount=saved.SavedCount;}
            Assert.IsInstanceOfType<OracleLifecycleReconciliationException>(cause);Assert.AreEqual(id,((OracleLifecycleReconciliationException)cause).RecordId);Assert.IsInstanceOfType<InvalidOperationException>(cause.InnerException);Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(cause));
            Assert.AreEqual(1,provider.CommitCalls);Assert.AreEqual(kind==0?editedId:id,caller.Id);Assert.AreEqual(0,caller.Version);Assert.AreEqual(Guid.Empty,caller.VersionId);Assert.AreSame(keys,caller.ProviderUniqueStorageKey);Assert.AreEqual(0,keys.Count);
            if(caller is AvatarDetail changed)Assert.AreEqual(2,changed.Inventory[0].Quantity);else if(kind>=2)Assert.AreEqual(name+" edited",caller.Name);
            var actual=await Read(verifier,table,id);Assert.IsNotNull(actual);using(var document=JsonDocument.Parse(actual)){
                Assert.AreEqual(id,document.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(1,document.RootElement.GetProperty("Version").GetInt32());
                if(kind==1)Assert.AreEqual(1,document.RootElement.GetProperty("Inventory")[0].GetProperty("Quantity").GetInt32());else if(kind>=2)Assert.AreEqual(name,document.RootElement.GetProperty("Name").GetString());
            }
            Assert.IsNull(await Read(verifier,"OASIS_HOLONS",childId));
            await using var cleanup=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=actual;
            Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,table,id));File.Delete(journal);
            Assert.AreEqual(1,acknowledgedCount,"The native commit was acknowledged before caller publication failed.");
        }finally{await provider.DeActivateProviderAsync();}
        static void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);}
    }

    [TestMethod][TestCategory("Reconciliation")]
    public async Task ReconcileKnownHistoricalExportFixture()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var originalReport=Path.Combine(work,"oracle-historical-export-before.trx");
        Assert.AreEqual("49A7E27CC064176F400C197DBBF3FBC9B644738EB6B52C3D09E6DD371A860E66",Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(originalReport))));
        var journal=Path.Combine(work,"canonical-records.pending.json");using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
        var id=Guid.Parse("7dcc1028-b6ec-4b72-8646-551212209f11");var name="owned-oracle-"+id.ToString("N");
        Assert.AreEqual(id,intent.RootElement.GetProperty("id").GetGuid());Assert.AreEqual(name,intent.RootElement.GetProperty("name").GetString());
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var provider=new OracleDBOASIS(connection);
        try{
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Exception?.ToString());
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            var originals=new Dictionary<string,string>();
            foreach(var table in new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS","OASIS_HOLONS"}){
                var json=await Read(verifier,table,id);Assert.IsNotNull(json);using var document=JsonDocument.Parse(json);
                Assert.AreEqual(id,document.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(3,document.RootElement.GetProperty("Version").GetInt32());
                Assert.AreEqual(name+"-sync",document.RootElement.GetProperty(table=="OASIS_HOLONS"?"Name":"Username").GetString());
                Assert.AreEqual(2,document.RootElement.GetProperty("_oasis_record_history").GetProperty("records").GetArrayLength());originals.Add(table,json);
            }
            var old=await provider.LoadHolonAsync(id,version:1);Assert.IsFalse(old.IsError,old.Exception?.ToString());Assert.AreEqual(id,old.Result.CreatedByAvatarId);
            foreach(var loaded in new[]{await provider.ExportAllDataForAvatarByUsernameAsync(name,1),await provider.ExportAllDataForAvatarByEmailAsync(name+"@example.invalid",1),await provider.ExportAllDataForAvatarByIdAsync(id,1),await provider.LoadHolonsForParentAsync(old.Result.ParentHolonId,version:1),await provider.LoadHolonsByMetaDataAsync("owned.fixture","literal-dot",version:1)}){
                Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());Assert.AreEqual(name,loaded.Result.Single(row=>row.Id==id).Name);
            }
            var searched=await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new(){["owned.fixture"]="literal-dot"}},version:1);
            Assert.IsFalse(searched.IsError,searched.Exception?.ToString());Assert.AreEqual(name,searched.Result.SearchResultHolons.Single(row=>row.Id==id).Name);
            foreach(var original in originals){
                await using var delete=new OracleCommand($"DELETE FROM {original.Key} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
                delete.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=original.Value;
                Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,original.Key,id));
            }
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    [TestMethod][TestCategory("Reconciliation")]
    public async Task ReconcileKnownCorruptAvatarFixture()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        Assert.AreEqual("ECFC83772408C854DFD31A808212D1A7DCA3D502EBC2C502362765B2100CE8BD",Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(work,"oracle-corrupt-avatar-before.trx")))));
        var id=Guid.Parse("792f3ede-f423-4e00-b297-6f6c1f0947ae");var journal=Path.Combine(work,"create-race-0.pending.json");
        using(var intent=JsonDocument.Parse(await File.ReadAllTextAsync(journal))){Assert.AreEqual(id,intent.RootElement.GetProperty("id").GetGuid());Assert.AreEqual("OASIS_AVATARS",intent.RootElement.GetProperty("table").GetString());Assert.AreEqual(0,intent.RootElement.GetProperty("kind").GetInt32());}
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;var provider=new OracleDBOASIS(connection);
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            var original=await Read(verifier,"OASIS_AVATARS",id);Assert.IsNotNull(original);
            using(var document=JsonDocument.Parse(original)){
                Assert.AreEqual(id,document.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(3,document.RootElement.GetProperty("Version").GetInt32());
                Assert.AreEqual(3,document.RootElement.GetProperty("_oasis_record_history").GetProperty("records")[0].GetProperty("Version").GetInt32());
                Assert.AreEqual("winner",document.RootElement.GetProperty("_oasis_record_history").GetProperty("records")[0].GetProperty("MetaData").GetProperty("owned_race").GetString());
            }
            var read=await provider.LoadAvatarAsync(id);Assert.IsTrue(read.IsError);Assert.IsNull(read.Result);Assert.IsInstanceOfType<InvalidDataException>(read.Exception);
            var deleted=await provider.DeleteAvatarAsync(id);Assert.IsTrue(deleted.IsError);Assert.IsFalse(deleted.Result);Assert.IsInstanceOfType<InvalidDataException>(deleted.Exception);
            Assert.AreEqual(original,await Read(verifier,"OASIS_AVATARS",id));
            await using var cleanup=new OracleCommand("DELETE FROM OASIS_AVATARS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true};
            cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=original;
            Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(verifier,"OASIS_AVATARS",id));File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    private static async Task<string?> Read(OracleConnection connection,string table,Guid id)
    {
        await using var command=new OracleCommand($"SELECT DATA_JSON FROM {table} WHERE ID=:Id",connection){BindByName=true};
        command.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
        var value=await command.ExecuteScalarAsync();return value==null||value==DBNull.Value?null:value.ToString();
    }
}

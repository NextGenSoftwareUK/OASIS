using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    private sealed class ExportVisibilityReconciliationOwner(string connection):OracleDBOASIS(connection)
    {
        protected override Task ProvisionStorageSchemaAsync(OracleConnection connection)
            =>throw new InvalidOperationException("Original export reconciliation cannot provision or repair schema.");
    }

    [TestMethod]
    public async Task ReconcileKnownAvatarExportVisibilityFixture()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,"canonical-records.pending.json");
        static string Hash(byte[] value)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(value));
        Assert.AreEqual("18F3900D400A7640D9B0BD4173940DEBDD46FCD531EC0CDBCA1A311CF245AEAE",Hash(await File.ReadAllBytesAsync(journal)));
        Assert.AreEqual("93B3C03959D69651074FD6E1F428CF1F97A90CFA57FC5C8EC0FF0C4D4E1B407B",Hash(await File.ReadAllBytesAsync(Path.Combine(work,"oracle-native-current.trx"))));
        var id=Guid.Parse("6a75895d-5e07-463a-9bf3-df5dfcdf6e9c");var child=Guid.Parse("13f385bf-1650-4fb0-8ce9-8e6da2860222");
        var name="owned-oracle-"+id.ToString("N");
        using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
        Assert.AreEqual(id,intent.RootElement.GetProperty("id").GetGuid());Assert.AreEqual(child,intent.RootElement.GetProperty("childId").GetGuid());Assert.AreEqual(name,intent.RootElement.GetProperty("name").GetString());
        var proof=Path.Combine(work,"oracle-export-visibility-reconciled.json");
        Assert.IsFalse(File.Exists(proof));Assert.IsFalse(File.Exists(proof+".next"));
        var rows=new[]{(Table:"OASIS_AVATARS",Id:id),(Table:"OASIS_AVATAR_DETAILS",Id:id),(Table:"OASIS_HOLONS",Id:id),(Table:"OASIS_HOLONS",Id:child)};
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var provider=new ExportVisibilityReconciliationOwner(connection);await using var verifier=new OracleConnection(connection);
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await verifier.OpenAsync();
            var originals=new List<string>();
            foreach(var row in rows){
                var json=await Read(verifier,row.Table,row.Id);Assert.IsNotNull(json);using var document=JsonDocument.Parse(json);
                var root=document.RootElement;
                Assert.AreEqual(row.Id,root.GetProperty("Id").GetGuid());Assert.AreEqual(row.Id==child?5:3,root.GetProperty("Version").GetInt32());
                Assert.AreEqual(row.Id==child?"owned independently current child":name+"-sync",root.GetProperty(row.Table=="OASIS_HOLONS"?"Name":"Username").GetString());
                Assert.IsFalse(root.GetProperty("IsDeleted").GetBoolean());
                if(row.Id==child){Assert.AreEqual(id,root.GetProperty("ParentHolonId").GetGuid());Assert.AreEqual(Guid.Empty,root.GetProperty("CreatedByAvatarId").GetGuid());}
                else{
                    var history=root.GetProperty("_oasis_record_history").GetProperty("records");Assert.AreEqual(2,history.GetArrayLength());
                    Assert.AreEqual(1,history[0].GetProperty("Version").GetInt32());Assert.AreEqual(2,history[1].GetProperty("Version").GetInt32());
                    if(row.Table=="OASIS_HOLONS")Assert.AreEqual(id,history[0].GetProperty("CreatedByAvatarId").GetGuid());
                }
                originals.Add(json);
            }
            var unscoped=await provider.ExportAllAsync(1);Assert.IsFalse(unscoped.IsError,unscoped.Exception?.ToString());
            Assert.AreEqual(5,unscoped.Result.Single(row=>row.Id==id).Children.Single().Version);
            foreach(var scoped in new[]{await provider.ExportAllDataForAvatarByIdAsync(id,1),await provider.ExportAllDataForAvatarByUsernameAsync(name+"-sync",1),await provider.ExportAllDataForAvatarByEmailAsync(name+"-sync@example.invalid",1)}){
                Assert.IsFalse(scoped.IsError,scoped.Exception?.ToString());Assert.AreEqual(1,scoped.Result.Single(row=>row.Id==id).Version);
                Assert.AreEqual(0,scoped.Result.Single(row=>row.Id==id).Children.Count);
            }
            for(var index=0;index<rows.Length;index++)Assert.AreEqual(originals[index],await Read(verifier,rows[index].Table,rows[index].Id));
            var hashes=rows.Select((row,index)=>new{row.Table,row.Id,SHA256=Hash(System.Text.Encoding.UTF8.GetBytes(originals[index]))}).ToArray();
            async Task Proof(bool cleaned){
                await using(var file=new FileStream(proof+".next",FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                    await JsonSerializer.SerializeAsync(file,new{state=cleaned?"verified-cleaned":"observed-before-exact-cleanup",rows=hashes,unscopedChildVersion=5,scopedChildCount=0,writeReplay=false,cleaned});file.Flush(true);
                }
                File.Move(proof+".next",proof,overwrite:cleaned);
            }
            await Proof(false);
            await using(var transaction=verifier.BeginTransaction()){
                for(var index=0;index<rows.Length;index++){
                    await using var delete=new OracleCommand($"DELETE FROM {rows[index].Table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true,Transaction=transaction};
                    delete.Parameters.Add("Id",OracleDbType.Raw).Value=rows[index].Id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=originals[index];
                    Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());
                }
                transaction.Commit();
            }
            foreach(var row in rows)Assert.IsNull(await Read(verifier,row.Table,row.Id));
            await Proof(true);File.Move(journal,Path.Combine(work,"canonical-export-visibility.resolved.json"));
        }finally{await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod]
    [DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeScopedGraphProtectsPrivateBranchesAndStoredMembership(bool sync,bool includePublic)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"scoped-visibility-{sync}-{includePublic}.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var actor=Guid.NewGuid();var parent=Guid.NewGuid();
        var conditionalOperation=Guid.NewGuid();
        var root=new Holon{Id=Guid.NewGuid(),ParentHolonId=parent,CreatedByAvatarId=actor,Name="owned visibility root",MetaData=new(){{"scope",actor.ToString("D")}}};
        var owned=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor,Name="owned visibility child"};
        var visible=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),IsPublic=true,Name="owned public visibility fixture"};
        var hidden=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),Name="owned private visibility fixture"};
        var leaf=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor,IsPublic=true,Name="owned child of hidden branch"};
        root.Children.Add(owned);root.Children.Add(visible);root.Children.Add(hidden);hidden.Children.Add(leaf);
        var rows=new[]{root,owned,visible,hidden,leaf};
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        await using var verifier=new OracleConnection(connection);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await verifier.OpenAsync();
            foreach(var row in rows)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
            {await JsonSerializer.SerializeAsync(file,new{ids=rows.Select(row=>row.Id).ToArray(),actor,parent,conditionalOperation,state="before-owned-scoped-visibility-create"});file.Flush(true);}
            var saved=await provider.SaveHolonAsync(root);
            Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);
            var originals=new List<(Guid Id,string Json)>();
            foreach(var row in rows){var json=await Read(verifier,"OASIS_HOLONS",row.Id);Assert.IsNotNull(json);originals.Add((row.Id,json));}

            IOASISStorageProvider contract=provider;
            var selected=sync?contract.LoadHolonsForParent(parent,actor,includePublic,loadChildrenFromProvider:true)
                :await contract.LoadHolonsForParentAsync(parent,actor,includePublic,loadChildrenFromProvider:true);
            Assert.IsFalse(selected.IsError,selected.Exception?.ToString());Assert.IsTrue(selected.IsLoaded);Assert.AreEqual(1,selected.LoadedCount);
            var copy=(Holon)selected.Result.Single();Assert.AreEqual(root.Id,copy.Id);
            CollectionAssert.AreEqual(includePublic?new[]{owned.Id,visible.Id}:new[]{owned.Id},copy.Children.Select(child=>child.Id).ToArray());
            Assert.AreEqual(root.ChildRoleManifestJson,copy.ChildRoleManifestJson,"Filtering must retain the original store-owned relationship observation.");
            copy.AcceptChanges();
            var rejected=sync?provider.SaveHolon(copy):await provider.SaveHolonAsync(copy);
            Assert.IsTrue(rejected.IsError,"A scoped selection must not replace hidden stored membership.");Assert.IsFalse(rejected.IsSaved);
            await AssertReceiptAbsent(verifier,conditionalOperation);
            var conditions=new[]{copy}.Concat(copy.Children).Select(row=>new HolonWriteCondition(row.Id,Guid.NewGuid())).ToArray();
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(conditionalOperation,actor,
                NextGenSoftware.OASIS.API.Core.Enums.ProviderType.OracleDBOASIS,conditions),ConditionalHolonOperation.Save,new[]{copy});
            var conditional=sync?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
            Assert.IsTrue(conditional.IsError);Assert.IsFalse(conditional.IsSaved);
            Assert.IsInstanceOfType<InvalidOperationException>(conditional.Exception,"Partial membership admission must precede native revision checks.");
            await AssertReceiptAbsent(verifier,conditionalOperation);
            foreach(var row in originals)Assert.AreEqual(row.Json,await Read(verifier,"OASIS_HOLONS",row.Id),"Scoped reads and rejected graph saves must not alter canonical rows/history.");
            var complete=await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);
            Assert.IsFalse(complete.IsError,complete.Exception?.ToString());Assert.AreEqual(3,complete.Result.Children.Count);
            Assert.AreEqual(leaf.Id,complete.Result.Children.Single(child=>child.Id==hidden.Id).Children.Single().Id);
            Assert.AreEqual(3,root.Children.Count,"The caller's acknowledged source graph remains intact.");

            var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=actor,FilterByMetaData=new(){{"scope",actor.ToString("D")}}};
            var search=sync?provider.Search(query):await provider.SearchAsync(query);
            var exports=new List<NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolon>>>();
            foreach(var exportVersion in new[]{0,1,-1})exports.Add(sync?provider.ExportAllDataForAvatarById(actor,exportVersion):await provider.ExportAllDataForAvatarByIdAsync(actor,exportVersion));
            foreach(var row in originals)Assert.AreEqual(row.Json,await Read(verifier,"OASIS_HOLONS",row.Id),"Search projection must not mutate canonical records.");

            await using(var transaction=verifier.BeginTransaction())
            {
                foreach(var row in originals)
                {
                    await using var delete=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true,Transaction=transaction};
                    delete.Parameters.Add("Id",OracleDbType.Raw).Value=row.Id.ToByteArray();
                    delete.Parameters.Add("Expected",OracleDbType.Clob).Value=row.Json;
                    Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());
                }
                transaction.Commit();
            }
            foreach(var row in originals)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));
            File.Delete(journal);
            // Assert output after exact fixture teardown, so a privacy regression retains evidence, not test data.
            Assert.IsFalse(search.IsError,search.Exception?.ToString());Assert.AreEqual(1,search.LoadedCount);
            var searchRoot=(Holon)search.Result.SearchResultHolons.Single();
            CollectionAssert.AreEqual(new[]{owned.Id},searchRoot.Children.Select(child=>child.Id).ToArray());
            Assert.AreEqual(root.ChildRoleManifestJson,searchRoot.ChildRoleManifestJson);
            for(var exportIndex=0;exportIndex<exports.Count;exportIndex++){
                var exported=exports[exportIndex];
                Assert.IsFalse(exported.IsError,exported.Exception?.ToString());Assert.IsTrue(exported.IsLoaded);
                Assert.IsTrue(exported.Result.All(row=>row.CreatedByAvatarId==actor));
                var exportedRoot=exported.Result.Single(row=>row.Id==root.Id);
                CollectionAssert.AreEqual(exportIndex==2?Array.Empty<Guid>():new[]{owned.Id},exportedRoot.Children.Select(row=>row.Id).ToArray());
                Assert.AreEqual(root.ChildRoleManifestJson,((Holon)exportedRoot).ChildRoleManifestJson);
            }
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}

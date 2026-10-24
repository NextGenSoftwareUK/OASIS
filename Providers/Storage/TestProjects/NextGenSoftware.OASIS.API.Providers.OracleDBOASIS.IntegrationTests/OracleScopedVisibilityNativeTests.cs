using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [DataTestMethod]
    [DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeScopedGraphProtectsPrivateBranchesAndStoredMembership(bool sync,bool includePublic)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"scoped-visibility-{sync}-{includePublic}.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var actor=Guid.NewGuid();var parent=Guid.NewGuid();
        var root=new Holon{Id=Guid.NewGuid(),ParentHolonId=parent,CreatedByAvatarId=actor,Name="owned visibility root"};
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
            {await JsonSerializer.SerializeAsync(file,new{ids=rows.Select(row=>row.Id).ToArray(),actor,parent,state="before-owned-scoped-visibility-create"});file.Flush(true);}
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
            foreach(var row in originals)Assert.AreEqual(row.Json,await Read(verifier,"OASIS_HOLONS",row.Id),"Scoped reads and rejected graph saves must not alter canonical rows/history.");
            var complete=await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);
            Assert.IsFalse(complete.IsError,complete.Exception?.ToString());Assert.AreEqual(3,complete.Result.Children.Count);
            Assert.AreEqual(leaf.Id,complete.Result.Children.Single(child=>child.Id==hidden.Id).Children.Single().Id);
            Assert.AreEqual(3,root.Children.Count,"The caller's acknowledged source graph remains intact.");

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
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}

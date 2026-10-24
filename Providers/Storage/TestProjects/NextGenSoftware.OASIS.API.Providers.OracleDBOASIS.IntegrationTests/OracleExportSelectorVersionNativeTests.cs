using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [TestMethod][TestCategory("Reconciliation")]
    public async Task ReconcileKnownIndependentExportIdentityFixture()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,"canonical-records.pending.json");
        Assert.AreEqual("8B7778C22B7E5F766FD19EEAB0FDE693A413A7E41A192704F0A70DCEB3E85A63",Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(work,"oracle-independent-export-native-current.trx")))));
        Assert.AreEqual("FD74D97C155DA064D49D6C874AF76733993FD42345F44D19CCC7EE7A0E9DBF35",Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(journal))));
        var id=Guid.Parse("7ad556ce-6ec5-48e2-8f77-52adae71b7c3");var child=Guid.Parse("578ea423-5401-40f3-adfc-e9ab9a684bf2");
        var name="owned-oracle-"+id.ToString("N");
        using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
        Assert.AreEqual(id,intent.RootElement.GetProperty("id").GetGuid());Assert.AreEqual(child,intent.RootElement.GetProperty("childId").GetGuid());
        Assert.AreEqual(name,intent.RootElement.GetProperty("name").GetString());
        var rows=new[]{(Table:"OASIS_AVATARS",Id:id),(Table:"OASIS_AVATAR_DETAILS",Id:id),(Table:"OASIS_HOLONS",Id:id),(Table:"OASIS_HOLONS",Id:child)};
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var provider=new OracleDBOASIS(connection);await using var verifier=new OracleConnection(connection);
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await verifier.OpenAsync();
            var originals=new List<string>();
            foreach(var row in rows){
                var json=await Read(verifier,row.Table,row.Id);Assert.IsNotNull(json);using var document=JsonDocument.Parse(json);
                Assert.AreEqual(row.Id,document.RootElement.GetProperty("Id").GetGuid());
                Assert.AreEqual(row.Id==child?5:3,document.RootElement.GetProperty("Version").GetInt32());
                Assert.AreEqual(row.Id==child?"owned independently current child":name+"-sync",document.RootElement.GetProperty(row.Table=="OASIS_HOLONS"?"Name":"Username").GetString());
                if(row.Id==child)Assert.AreEqual(id,document.RootElement.GetProperty("ParentHolonId").GetGuid());
                else Assert.AreEqual(2,document.RootElement.GetProperty("_oasis_record_history").GetProperty("records").GetArrayLength());
                originals.Add(json);
            }
            var snapshot=Path.Combine(work,"oracle-export-selector-failure-snapshot.json");
            using(var file=new FileStream(snapshot,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(rows.Select((row,index)=>new{row.Table,row.Id,Json=originals[index]})));file.Flush(true);
            }
            foreach(var selected in new[]{await provider.ExportAllDataForAvatarByUsernameAsync(name+"-sync",1),await provider.ExportAllDataForAvatarByEmailAsync(name+"-sync@example.invalid",1)}){
                Assert.IsFalse(selected.IsError,selected.Exception?.ToString());Assert.AreEqual(name,selected.Result.Single(row=>row.Id==id).Name);
                Assert.AreEqual(5,selected.Result.Single(row=>row.Id==id).Children.Single().Version);
            }
            for(var index=0;index<rows.Length;index++)Assert.AreEqual(originals[index],await Read(verifier,rows[index].Table,rows[index].Id));
            await using(var transaction=verifier.BeginTransaction()){
                for(var index=0;index<rows.Length;index++){
                    await using var delete=new OracleCommand($"DELETE FROM {rows[index].Table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true,Transaction=transaction};
                    delete.Parameters.Add("Id",OracleDbType.Raw).Value=rows[index].Id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=originals[index];
                    Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());
                }
                transaction.Commit();
            }
            foreach(var row in rows)Assert.IsNull(await Read(verifier,row.Table,row.Id));
            File.Move(journal,Path.Combine(work,"canonical-export-identity.resolved.json"));
        }finally{await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeExportVersionDoesNotRequireMatchingIdentityVersion(bool sync,bool email)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,$"independent-export-{sync}-{email}.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var owner=Guid.NewGuid();var own=Guid.NewGuid();var foreign=Guid.NewGuid();
        var name="owned-independent-export-"+owner.ToString("N");
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        await using var verifier=new OracleConnection(connection);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);await verifier.OpenAsync();
            var rows=new[]{(Table:"OASIS_AVATARS",Id:owner),(Table:"OASIS_HOLONS",Id:own),(Table:"OASIS_HOLONS",Id:foreign)};
            foreach(var row in rows)Assert.IsNull(await Read(verifier,row.Table,row.Id));
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{owner,own,foreign,name,sync,email,state="before-owned-independent-export"}));file.Flush(true);
            }
            var avatar=new Avatar{Id=owner,Username=name,Email=name+"@example.invalid",IsActive=true};
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);Assert.AreEqual(1,avatar.Version);
            avatar.Username=name+"-current";avatar.Email=avatar.Username+"@example.invalid";
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);Assert.AreEqual(2,avatar.Version);
            var holon=new Holon{Id=own,Name=name,CreatedByAvatarId=owner,IsActive=true};
            var other=new Holon{Id=foreign,Name=name+"-foreign",CreatedByAvatarId=Guid.NewGuid(),IsActive=true};
            for(var version=1;version<=3;version++){
                holon.Name=name+"-v"+version;other.Name=name+"-foreign-v"+version;
                var saved=await provider.SaveHolonAsync(holon,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                Assert.IsFalse((await provider.SaveHolonAsync(other,saveChildren:false)).IsError);
            }
            var snapshots=new List<string>();foreach(var row in rows)snapshots.Add((await Read(verifier,row.Table,row.Id))!);
            foreach(var version in new[]{0,1,2,3}){
                var selected=email
                    ?sync?provider.ExportAllDataForAvatarByEmail(avatar.Email,version):await provider.ExportAllDataForAvatarByEmailAsync(avatar.Email,version)
                    :sync?provider.ExportAllDataForAvatarByUsername(avatar.Username,version):await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username,version);
                Assert.IsFalse(selected.IsError,selected.Exception?.ToString());Assert.IsTrue(selected.IsLoaded);Assert.AreEqual(1,selected.LoadedCount);
                var expected=version==0?3:version;
                Assert.AreEqual(own,selected.Result.Single().Id);Assert.AreEqual(expected,selected.Result.Single().Version);
                Assert.AreEqual(name+"-v"+expected,selected.Result.Single().Name);
            }
            var allVersions=email
                ?sync?provider.ExportAllDataForAvatarByEmail(avatar.Email,-1):await provider.ExportAllDataForAvatarByEmailAsync(avatar.Email,-1)
                :sync?provider.ExportAllDataForAvatarByUsername(avatar.Username,-1):await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username,-1);
            Assert.IsFalse(allVersions.IsError,allVersions.Exception?.ToString());Assert.IsTrue(allVersions.IsLoaded);Assert.AreEqual(3,allVersions.LoadedCount);
            var versions=allVersions.Result.ToArray();CollectionAssert.AreEqual(new[]{1,2,3},versions.Select(row=>row.Version).ToArray());
            Assert.IsTrue(versions.All(row=>row.Id==own&&row.CreatedByAvatarId==owner));
            for(var index=0;index<versions.Length;index++)Assert.AreEqual(name+"-v"+(index+1),versions[index].Name);
            // Explicit avatar history still supports old identity fields; export's Version belongs to holons, not identities.
            var oldAvatar=email?await provider.LoadAvatarByEmailAsync(name+"@example.invalid",1):await provider.LoadAvatarByUsernameAsync(name,1);
            Assert.IsFalse(oldAvatar.IsError);Assert.AreEqual(owner,oldAvatar.Result.Id);Assert.AreEqual(1,oldAvatar.Result.Version);
            var oldExport=email?await provider.ExportAllDataForAvatarByEmailAsync(name+"@example.invalid",1):await provider.ExportAllDataForAvatarByUsernameAsync(name,1);
            Assert.IsTrue(oldExport.IsError);Assert.IsNull(oldExport.Result);
            for(var index=0;index<rows.Length;index++)Assert.AreEqual(snapshots[index],await Read(verifier,rows[index].Table,rows[index].Id));
            // Delete only exact original owned canonical bytes (including embedded history).
            await using(var transaction=verifier.BeginTransaction()){
                for(var index=0;index<rows.Length;index++){
                    await using var delete=new OracleCommand($"DELETE FROM {rows[index].Table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true,Transaction=transaction};
                    delete.Parameters.Add("Id",OracleDbType.Raw).Value=rows[index].Id.ToByteArray();
                    delete.Parameters.Add("Expected",OracleDbType.Clob).Value=snapshots[index];
                    Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());
                }
                transaction.Commit();
            }
            foreach(var row in rows)Assert.IsNull(await Read(verifier,row.Table,row.Id));
            File.Delete(journal);
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}

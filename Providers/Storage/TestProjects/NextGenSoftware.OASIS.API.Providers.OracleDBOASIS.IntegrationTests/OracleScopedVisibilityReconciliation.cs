using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [TestMethod]
    public async Task ReconcileKnownScopedVisibilityFixtures()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        Assert.AreEqual("0FE26BD560D687AB9FA760BD5250FF814F29A12ABFA0F35CE49A2D08CFA5D942",
            Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(work,"oracle-native-current.trx")))));
        var expectedNames=new[]{"scoped-visibility-False-False.pending.json","scoped-visibility-False-True.pending.json",
            "scoped-visibility-True-False.pending.json","scoped-visibility-True-True.pending.json"};
        CollectionAssert.AreEquivalent(expectedNames,Directory.GetFiles(work,"*.pending.json").Select(Path.GetFileName).ToArray());
        var connectionString=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        await using var verifier=new OracleConnection(connectionString);await verifier.OpenAsync();
        var provider=new OracleDBOASIS(connectionString){IsVersionControlEnabled=true};
        var originals=new List<(Guid Id,string Json,int Version)>();
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var name in expectedNames)
            {
                using var journal=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(work,name)));
                var data=journal.RootElement;Assert.AreEqual("before-owned-scoped-visibility-create",data.GetProperty("state").GetString());
                var ids=data.GetProperty("ids").EnumerateArray().Select(value=>value.GetGuid()).ToArray();
                Assert.AreEqual(5,ids.Length);Assert.AreEqual(5,ids.Distinct().Count());
                var actor=data.GetProperty("actor").GetGuid();var parent=data.GetProperty("parent").GetGuid();
                var includePublic=name.EndsWith("-True.pending.json",StringComparison.Ordinal);
                var names=new[]{"owned visibility root","owned visibility child","owned public visibility fixture","owned private visibility fixture","owned child of hidden branch"};
                for(var index=0;index<ids.Length;index++)
                {
                    var json=await Read(verifier,"OASIS_HOLONS",ids[index]);Assert.IsNotNull(json);
                    var document=OasisJson.ParseStoredObject(json);document["id"]=ids[index].ToString();
                    var versions=new EmbeddedRecordHistory().ReadAll(ids[index],document,StoredRecordJson.DeserializeStoredHolon);
                    var expectedVersion=index<2 || index==2&&includePublic?2:1;
                    Assert.AreEqual(expectedVersion,versions.Count);Assert.AreEqual(expectedVersion,versions.Last().Version);
                    foreach(var row in versions)
                    {
                        Assert.AreEqual(ids[index],row.Id);Assert.AreEqual(names[index],row.Name);Assert.IsFalse(row.IsDeleted);Assert.IsTrue(row.IsActive);
                        Assert.AreEqual(index==0?parent:index==4?ids[3]:ids[0],row.ParentHolonId);
                        Assert.AreEqual(index is 2 or 4,row.IsPublic);
                        if(index is 0 or 1 or 4)Assert.AreEqual(actor,row.CreatedByAvatarId);
                        else Assert.AreNotEqual(actor,row.CreatedByAvatarId);
                        var manifest=HolonChildRoleManifestJson.Deserialize(row.ChildRoleManifestJson);
                        Assert.AreEqual(row.Id,manifest.ParentId);Assert.AreEqual(1,manifest.Roles.Count);Assert.AreEqual("Children",manifest.Roles.Single().Name);
                        var members=index==0?(row.Version==1?new[]{ids[1],ids[2],ids[3]}:includePublic?new[]{ids[1],ids[2]}:new[]{ids[1]})
                            :index==3?new[]{ids[4]}:Array.Empty<Guid>();
                        CollectionAssert.AreEqual(members,manifest.Roles.Single().ChildIds.ToArray());
                    }
                    originals.Add((ids[index],json,expectedVersion));
                }
                IOASISStorageProvider contract=provider;
                var selected=await contract.LoadHolonsForParentAsync(parent,actor,includePublic,loadChildrenFromProvider:true);
                Assert.IsFalse(selected.IsError,selected.Exception?.ToString());var copy=(Holon)selected.Result.Single();copy.AcceptChanges();
                var rejected=await provider.SaveHolonAsync(copy);
                Assert.IsTrue(rejected.IsError,"The repaired provider must reject the original scoped replacement.");Assert.IsFalse(rejected.IsSaved);
            }
            Assert.AreEqual(20,originals.Count);Assert.AreEqual(20,originals.Select(row=>row.Id).Distinct().Count());
            foreach(var row in originals)Assert.AreEqual(row.Json,await Read(verifier,"OASIS_HOLONS",row.Id));
            var evidence=Path.Combine(work,"scoped-visibility-reconciliation-evidence.json");
            using(var file=new FileStream(evidence,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(file,originals.Select(row=>new{row.Id,row.Version,Bytes=Encoding.UTF8.GetByteCount(row.Json),SHA256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(row.Json)))}));file.Flush(true);
            }
            await using(var transaction=verifier.BeginTransaction())
            {
                foreach(var row in originals)
                {
                    await using var delete=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true,Transaction=transaction};
                    delete.Parameters.Add("Id",OracleDbType.Raw).Value=row.Id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=row.Json;
                    Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());
                }
                transaction.Commit();
            }
            foreach(var row in originals)Assert.IsNull(await Read(verifier,"OASIS_HOLONS",row.Id));
            foreach(var name in expectedNames)File.Delete(Path.Combine(work,name));
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}

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
    public async Task ReconcileKnownAcknowledgementViewFixtures()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        Assert.AreEqual("1B7B5182CB589BB700C5F2F1B3D625E133F54E72BF45B3CCB65354C2C3293911",
            Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(work,"oracle-native-current.trx")))));
        var names=new[]{"named-child-roles-False-True.pending.json","named-child-roles-True-True.pending.json",
            "conditional-root-delete-False.pending.json","conditional-root-delete-True.pending.json"};
        CollectionAssert.AreEquivalent(names,Directory.GetFiles(work,"*.pending.json").Select(Path.GetFileName).ToArray());
        await using var connection=new OracleConnection(Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING"));await connection.OpenAsync();
        var rows=new List<(Guid Id,string Json)>();var receipts=new List<(Guid Id,string Hash,string Json)>();
        foreach(var name in names)
        {
            using var journal=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(work,name)));var data=journal.RootElement;
            var ids=data.GetProperty("ids").EnumerateArray().Select(value=>value.GetGuid()).ToArray();Assert.AreEqual(2,ids.Length);Assert.AreNotEqual(ids[0],ids[1]);
            var named=name.StartsWith("named-",StringComparison.Ordinal);var hard=!named&&data.GetProperty("hard").GetBoolean();
            Assert.AreEqual(named?"before-owned-named-role-fixture":"before-root-only-conditional-delete",data.GetProperty("state").GetString());
            var operation=named?data.GetProperty("operations")[0].GetGuid():data.GetProperty("operation").GetGuid();
            if(named)foreach(var unused in data.GetProperty("operations").EnumerateArray().Skip(1))await AssertReceiptAbsent(connection,unused.GetGuid());
            await using var select=new OracleCommand("SELECT REQUEST_HASH,RESULT_JSON FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",connection){BindByName=true};
            select.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();string hash;string json;
            await using(var reader=await select.ExecuteReaderAsync()){Assert.IsTrue(await reader.ReadAsync());Assert.IsFalse(reader.IsDBNull(1));hash=reader.GetString(0);json=reader.GetString(1);Assert.IsFalse(await reader.ReadAsync());}
            using(var envelope=JsonDocument.Parse(json)){Assert.AreEqual(operation,envelope.RootElement.GetProperty("operation").GetGuid());Assert.AreEqual(hash,envelope.RootElement.GetProperty("intent").GetString());}
            var acknowledged=ConditionalHolonReceipt.Read(json);Assert.AreEqual(1,acknowledged.Count);var root=(Holon)acknowledged.Single();
            Assert.AreEqual(ids[0],root.Id);Assert.AreEqual(named?1:2,root.Version);Assert.AreEqual(!named,root.IsDeleted);
            Assert.AreEqual(named?"owned named-role root":"owned-root-only-delete",root.Name);
            var manifest=HolonChildRoleManifestJson.Deserialize(root.ChildRoleManifestJson);Assert.AreEqual(root.Id,manifest.ParentId);
            if(named){Assert.AreEqual(2,manifest.Roles.Count);Assert.AreEqual(0,manifest.Roles.Single(role=>role.Name=="Children").ChildIds.Count);CollectionAssert.AreEqual(new[]{ids[1]},manifest.Roles.Single(role=>role.Name=="Components").ChildIds.ToArray());Assert.AreEqual(data.GetProperty("actor").GetGuid(),root.CreatedByAvatarId);}
            else{Assert.AreEqual(1,manifest.Roles.Count);CollectionAssert.AreEqual(new[]{ids[1]},manifest.Roles.Single().ChildIds.ToArray());}
            var result=ConditionalHolonAcknowledgement.Create(acknowledged,named?ConditionalHolonOperation.Save:hard?ConditionalHolonOperation.HardDelete:ConditionalHolonOperation.SoftDelete);
            Assert.IsFalse(result.IsError);Assert.AreEqual(!named,result.IsDeleted);Assert.AreEqual(named,result.IsSaved);
            foreach(var id in ids)
            {
                var current=await Read(connection,"OASIS_HOLONS",id);
                if(id==ids[0]&&hard){Assert.IsNull(current);continue;}
                Assert.IsNotNull(current);var record=StoredRecordJson.DeserializeStoredHolon(current);
                Assert.AreEqual(id,record.Id);Assert.AreEqual(id==ids[0]&&!named?2:1,record.Version);
                Assert.AreEqual(id==ids[0]&&!named,record.IsDeleted);
                Assert.AreEqual(id==ids[0]?root.Name:named?"owned named-role component":"owned-excluded-child",record.Name);
                if(id==ids[1])Assert.AreEqual(ids[0],record.ParentHolonId);
                if(id==ids[0])Assert.AreEqual(root.ChildRoleManifestJson,record.ChildRoleManifestJson);
                rows.Add((id,current));
            }
            await using var locks=new OracleCommand("SELECT COUNT(*) FROM OASIS_WRITE_OPERATION_LOCKS WHERE OPERATION_ID=:Id",connection){BindByName=true};locks.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();Assert.AreEqual(0,Convert.ToInt32(await locks.ExecuteScalarAsync()));
            receipts.Add((operation,hash,json));
        }
        Assert.AreEqual(7,rows.Count);Assert.AreEqual(7,rows.Select(row=>row.Id).Distinct().Count());Assert.AreEqual(4,receipts.Count);
        string Sha(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        using(var file=new FileStream(Path.Combine(work,"acknowledgement-view-reconciliation-evidence.json"),FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
        {await JsonSerializer.SerializeAsync(file,new{Rows=rows.Select(row=>new{row.Id,SHA256=Sha(row.Json)}),Receipts=receipts.Select(row=>new{row.Id,row.Hash,SHA256=Sha(row.Json)})});file.Flush(true);}
        await using(var transaction=connection.BeginTransaction())
        {
            foreach(var row in rows){await using var delete=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",connection){BindByName=true,Transaction=transaction};delete.Parameters.Add("Id",OracleDbType.Raw).Value=row.Id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=row.Json;Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());}
            foreach(var receipt in receipts){await using var delete=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash AND DBMS_LOB.COMPARE(RESULT_JSON,:Expected)=0",connection){BindByName=true,Transaction=transaction};delete.Parameters.Add("Id",OracleDbType.Raw).Value=receipt.Id.ToByteArray();delete.Parameters.Add("Hash",OracleDbType.Varchar2).Value=receipt.Hash;delete.Parameters.Add("Expected",OracleDbType.Clob).Value=receipt.Json;Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());}
            transaction.Commit();
        }
        foreach(var row in rows)Assert.IsNull(await Read(connection,"OASIS_HOLONS",row.Id));foreach(var receipt in receipts)await AssertReceiptAbsent(connection,receipt.Id);
        foreach(var name in names)File.Delete(Path.Combine(work,name));
    }
}

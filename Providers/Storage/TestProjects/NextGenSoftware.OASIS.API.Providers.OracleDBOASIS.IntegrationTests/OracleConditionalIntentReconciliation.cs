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
    public async Task ReconcileKnownConditionalIntentFixture()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var path=Path.Combine(work,"conditional-graph.pending.json");
        Assert.AreEqual("9B7A77A78CD95CDC1F90DEC955ECAA02BF63DF4EC1B37B7A42672E76A2092C69",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(work,"oracle-native-current.trx")))));
        Assert.AreEqual("1D24A34810495B5BA4E9114E15258BFA477D66763009567E82152B3BDF3FFF3D",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))));
        CollectionAssert.AreEqual(new[]{path},Directory.GetFiles(work,"*.pending.json"));
        using var journal=JsonDocument.Parse(await File.ReadAllTextAsync(path));var data=journal.RootElement;
        Assert.AreEqual("before-conditional-native-graph",data.GetProperty("state").GetString());
        var ids=data.GetProperty("ids").EnumerateArray().Select(value=>value.GetGuid()).ToArray();Assert.AreEqual(2,ids.Length);
        var operations=data.GetProperty("operations").EnumerateArray().Select(value=>value.GetGuid()).ToArray();Assert.AreEqual(5,operations.Length);
        var actor=data.GetProperty("actor").GetGuid();var authority=Core.Enums.ProviderType.OracleDBOASIS;
        var root=new Holon{Id=ids[0],Name="owned-conditional-root",Description=new string('樹',32769)+"🌳🌱",ProviderMetaData=null};
        root.Children.Add(new Holon{Id=ids[1],ParentHolonId=root.Id,Name="owned-conditional-child"});
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operations[0],actor,authority,ids.Select(id=>new HolonWriteCondition(id,null))),ConditionalHolonOperation.Save,new[]{root});
        var plan=ConditionalHolonPlan.Create(request,authority);
        await using var connection=new OracleConnection(Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING"));await connection.OpenAsync();
        foreach(var unused in operations.Skip(1))await AssertReceiptAbsent(connection,unused);
        string hash;string receiptJson;
        await using(var read=new OracleCommand("SELECT REQUEST_HASH,RESULT_JSON FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",connection){BindByName=true})
        {
            read.Parameters.Add("Id",OracleDbType.Raw).Value=operations[0].ToByteArray();await using var reader=await read.ExecuteReaderAsync();
            Assert.IsTrue(await reader.ReadAsync());Assert.IsFalse(reader.IsDBNull(1));hash=reader.GetString(0);receiptJson=reader.GetString(1);Assert.IsFalse(await reader.ReadAsync());
        }
        Assert.AreEqual(plan.Fingerprint,hash);var acknowledged=ConditionalHolonReceipt.Read(receiptJson,plan);
        var ackRoot=acknowledged.Single();Assert.AreEqual(root.Description,ackRoot.Description);Assert.AreEqual(1,ackRoot.Children.Count);
        var rows=new List<(Guid Id,string Json)>();
        foreach(var ack in new[]{ackRoot}.Concat(ackRoot.Children))
        {
            Assert.AreEqual(1,ack.Version);Assert.AreEqual(actor,ack.CreatedByAvatarId);Assert.IsFalse(ack.IsDeleted);
            var json=await Read(connection,"OASIS_HOLONS",ack.Id);Assert.IsNotNull(json);
            Assert.IsTrue(StoredRecordPayloadEquality.Equivalent(StoredRecordJson.CaptureRuntimeToken(ack),StoredRecordJson.CaptureRuntimeToken(StoredRecordJson.DeserializeStoredHolon(json))),"Native record differs from its original acknowledged receipt.");
            rows.Add((ack.Id,json));
        }
        CollectionAssert.AreEquivalent(ids,rows.Select(row=>row.Id).ToArray());
        foreach(var operation in operations)
        {
            await using var guard=new OracleCommand("SELECT COUNT(*) FROM OASIS_WRITE_OPERATION_LOCKS WHERE OPERATION_ID=:Id",connection){BindByName=true};
            guard.Parameters.Add("Id",OracleDbType.Raw).Value=operation.ToByteArray();Assert.AreEqual(0,Convert.ToInt32(await guard.ExecuteScalarAsync()));
        }
        string Sha(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        using(var file=new FileStream(Path.Combine(work,"conditional-intent-reconciliation-evidence.json"),FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
        {await JsonSerializer.SerializeAsync(file,new{Rows=rows.Select(row=>new{row.Id,SHA256=Sha(row.Json)}),Operation=operations[0],Intent=hash,ReceiptSHA256=Sha(receiptJson)});file.Flush(true);}
        // Exact original fixture teardown, never replay the acknowledged mutation.
        await using(var transaction=connection.BeginTransaction())
        {
            foreach(var row in rows)
            {
                await using var delete=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",connection){BindByName=true,Transaction=transaction};
                delete.Parameters.Add("Id",OracleDbType.Raw).Value=row.Id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=row.Json;Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());
            }
            await using var receipt=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash AND DBMS_LOB.COMPARE(RESULT_JSON,:Expected)=0",connection){BindByName=true,Transaction=transaction};
            receipt.Parameters.Add("Id",OracleDbType.Raw).Value=operations[0].ToByteArray();receipt.Parameters.Add("Hash",OracleDbType.Varchar2).Value=hash;receipt.Parameters.Add("Expected",OracleDbType.Clob).Value=receiptJson;Assert.AreEqual(1,await receipt.ExecuteNonQueryAsync());transaction.Commit();
        }
        foreach(var row in rows)Assert.IsNull(await Read(connection,"OASIS_HOLONS",row.Id));foreach(var operation in operations)await AssertReceiptAbsent(connection,operation);
        File.Delete(path);
    }
}

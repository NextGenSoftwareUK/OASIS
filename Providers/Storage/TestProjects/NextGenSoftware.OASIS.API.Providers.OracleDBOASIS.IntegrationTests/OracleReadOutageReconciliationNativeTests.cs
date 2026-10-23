using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [TestMethod]
    public async Task ReconcileKnownReadAcknowledgementConnectionOutage()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_ORACLE_RECONCILE_READ_OUTAGE"),"Explicit original-fixture reconciliation required.");
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var pending=Path.Combine(work,"read-acknowledgements.pending.json");
        var resolved=Path.Combine(work,"read-acknowledgements.resolved.json");
        Assert.IsFalse(File.Exists(resolved));
        static async Task<byte[]> Verified(string path,string expected)
        {
            var bytes=await File.ReadAllBytesAsync(path);
            Assert.AreEqual(expected,Convert.ToHexString(SHA256.HashData(bytes)),"Original evidence changed; no cleanup.");
            return bytes;
        }
        var intent=await Verified(pending,"C83043A1A52487CB881ADE96B9038C328F78052CCC6DE126DCDB82D1043DA198");
        var evidence=await Verified(Path.Combine(work,"read-acknowledgements-failure.json"),"470441EA9F0D9062305760949A92C303574FD668AF8CAD3369586B92E87F3F68");
        await Verified(Path.Combine(work,"oracle-read-only-native-connection.json"),"7438D43A1DAA41C55A32EEE3B9B585639E5C7F2259A4047EA7FF5870D9EF36D0");
        using var journal=JsonDocument.Parse(intent);using var failure=JsonDocument.Parse(evidence);
        var ids=journal.RootElement.GetProperty("ids").EnumerateArray().Select(x=>x.GetGuid()).ToArray();Assert.AreEqual(3,ids.Length);
        var rows=failure.RootElement.GetProperty("rows").EnumerateArray().ToArray();Assert.AreEqual(4,rows.Length);
        await using var native=new OracleConnection(Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING"));
        await native.OpenAsync();using var transaction=native.BeginTransaction();
        foreach(var row in rows)
        {
            var table=row.GetProperty("table").GetString()!;var id=row.GetProperty("id").GetGuid();
            Assert.IsTrue(new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS","OASIS_HOLONS"}.Contains(table));Assert.IsTrue(ids.Contains(id));
            string raw;
            using(var read=new OracleCommand($"SELECT DATA_JSON FROM {table} WHERE ID=:Id FOR UPDATE",native){BindByName=true,Transaction=transaction,CommandTimeout=10})
            {
                read.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
                await using var reader=await read.ExecuteReaderAsync();Assert.IsTrue(await reader.ReadAsync());raw=reader.GetString(0);Assert.IsFalse(await reader.ReadAsync());
            }
            var bytes=Encoding.UTF8.GetBytes(raw);Assert.AreEqual(row.GetProperty("bytes").GetInt32(),bytes.Length);
            Assert.AreEqual(row.GetProperty("sha256").GetString(),Convert.ToHexString(SHA256.HashData(bytes)));
            using var delete=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",native){BindByName=true,Transaction=transaction,CommandTimeout=10};
            delete.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=raw;
            Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());
        }
        // A lost commit acknowledgement leaves the intent intact for inspection;
        // this mode never retries a delete or infers absence from a failed read.
        await transaction.CommitAsync();
        foreach(var row in rows)Assert.IsNull(await Read(native,row.GetProperty("table").GetString()!,row.GetProperty("id").GetGuid()));
        CollectionAssert.AreEqual(intent,await File.ReadAllBytesAsync(pending));
        CollectionAssert.AreEqual(evidence,await File.ReadAllBytesAsync(Path.Combine(work,"read-acknowledgements-failure.json")));
        File.Move(pending,resolved); // Retain the original intent bytes, not a replacement journal.
    }
}

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

// Explicit local provisioning only. NOT selected by the ordinary native suite.
[TestClass][DoNotParallelize]
public sealed class OracleIdentitySchemaUpgradeNativeTests
{
    private sealed record Column(string Table,string Name,int Bytes,int Characters,string Semantics);

    [TestMethod]
    public async Task UpgradeVerifiedEmptyRetainedIdentityColumnsToCharacters()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_ORACLE_ALLOW_TEST_IDENTITY_SCHEMA_UPGRADE"),"Schema provisioning requires explicit test-only opt-in before any connection is read.");
        var connectionString=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var builder=new OracleConnectionStringBuilder(connectionString);
        Assert.AreEqual("127.0.0.1:11521/FREEPDB1",builder.DataSource);
        Assert.AreEqual("SYSTEM",builder.UserID);
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,"identity-character-upgrade.pending.json");
        var proof=Path.Combine(work,"identity-character-upgrade.verified.json");
        Assert.IsFalse(Directory.EnumerateFiles(work,"*.pending.json").Any());Assert.IsFalse(File.Exists(proof));
        await using var connection=new OracleConnection(connectionString);await connection.OpenAsync();
        var tables=new[]{"OASIS_AVATARS","OASIS_AVATAR_DETAILS"};
        async Task AssertEmpty()
        {
            foreach(var table in tables){
                await using var count=new OracleCommand("SELECT COUNT(*) FROM "+table,connection);
                Assert.AreEqual(0,Convert.ToInt32(await count.ExecuteScalarAsync()),"Do not alter an identity table containing any data.");
            }
        }
        async Task<List<Column>> Snapshot()
        {
            var columns=new List<Column>();
            foreach(var table in tables){
                await using var read=new OracleCommand("SELECT COLUMN_NAME,DATA_TYPE,DATA_LENGTH,CHAR_LENGTH,CHAR_USED FROM USER_TAB_COLUMNS WHERE TABLE_NAME=:TableName AND COLUMN_NAME IN ('USERNAME','EMAIL') ORDER BY COLUMN_NAME",connection){BindByName=true};
                read.Parameters.Add("TableName",OracleDbType.Varchar2).Value=table;
                await using var reader=await read.ExecuteReaderAsync();
                while(await reader.ReadAsync()){
                    Assert.AreEqual("VARCHAR2",reader.GetString(1));
                    columns.Add(new(table,reader.GetString(0),reader.GetInt32(2),reader.GetInt32(3),reader.GetString(4)));
                }
            }
            Assert.AreEqual(4,columns.Count);Assert.AreEqual(4,columns.Select(row=>(row.Table,row.Name)).Distinct().Count());return columns;
        }
        await using(var charset=new OracleCommand("SELECT VALUE FROM NLS_DATABASE_PARAMETERS WHERE PARAMETER='NLS_CHARACTERSET'",connection))
            Assert.AreEqual("AL32UTF8",(string)(await charset.ExecuteScalarAsync())!);
        await AssertEmpty();var before=await Snapshot();
        foreach(var column in before){Assert.AreEqual("B",column.Semantics);Assert.AreEqual(256,column.Bytes);Assert.AreEqual(256,column.Characters);}
        async Task Persist(string path,object value)
        {
            using var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);
            await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(value));file.Flush(true);
        }
        await Persist(journal,new{state="before-explicit-empty-local-identity-character-upgrade",before,utc=DateTime.UtcNow});
        await using(var timeout=new OracleCommand("ALTER SESSION SET DDL_LOCK_TIMEOUT=5",connection))await timeout.ExecuteNonQueryAsync();
        foreach(var column in before){
            // All identifiers came from fixed tables and the two fixed dictionary names above.
            await using var alter=new OracleCommand($"ALTER TABLE {column.Table} MODIFY ({column.Name} VARCHAR2(256 CHAR))",connection);
            await alter.ExecuteNonQueryAsync();
        }
        var after=await Snapshot();foreach(var column in after){Assert.AreEqual("C",column.Semantics);Assert.AreEqual(256,column.Characters);Assert.AreEqual(1024,column.Bytes);}
        await AssertEmpty();
        await Persist(proof,new{state="verified-empty-local-identity-character-upgrade",before,after,utc=DateTime.UtcNow});
        File.Delete(journal);
        // DDL commits independently: any failure above preserves intent/partial schema for review.
    }
}

using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [DataTestMethod]
    [DataRow(false,"")][DataRow(true,"")]
    [DataRow(false,"SYS$TRANSACTION")][DataRow(true,"SYS$TRANSACTION")]
    [DataRow(false,"SYS$SESSION")][DataRow(true,"SYS$SESSION")]
    public async Task NativeTableDurabilityAdmissionUsesActualCatalogWithoutAlteringCanonicalTables(bool synchronous,string duration)
    {
        const string probe="OASIS_DURABILITY_PROBE";
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var journal=Path.Combine(work,"table-durability.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var definitions=(Dictionary<string,Dictionary<string,string>>)typeof(OracleDBOASIS).GetField("StorageSchema",BindingFlags.NonPublic|BindingFlags.Static)!.GetValue(null)!;
        Assert.IsFalse(definitions.ContainsKey(probe));var definition=new Dictionary<string,string>{{"ID","RAW"}};
        await using var native=new OracleConnection(connection);await native.OpenAsync();
        async Task<decimal?> Identity()
        {
            await using var command=new OracleCommand("SELECT OBJECT_ID FROM USER_OBJECTS WHERE OBJECT_NAME=:Name AND OBJECT_TYPE='TABLE'",native){BindByName=true};
            command.Parameters.Add("Name",OracleDbType.Varchar2).Value=probe;
            var value=await command.ExecuteScalarAsync();return value is null or DBNull?null:Convert.ToDecimal(value);
        }
        Assert.IsNull(await Identity(),"Never reuse or replace an unowned table.");
        await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{state="before-owned-durability-probe",table=probe,duration}));
        decimal? createdId=null;bool? accepted=null;Exception? observed=null;
        try
        {
            var clause=duration==""?"":duration=="SYS$TRANSACTION"?" ON COMMIT DELETE ROWS":" ON COMMIT PRESERVE ROWS";
            await using(var create=new OracleCommand($"CREATE {(duration==""?"":"GLOBAL TEMPORARY ")}TABLE {probe}(ID RAW(16) NOT NULL PRIMARY KEY){clause}",native))await create.ExecuteNonQueryAsync();
            createdId=await Identity();Assert.IsNotNull(createdId);
            await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{state="owned-durability-probe-created",table=probe,duration,objectId=createdId}));
            await using(var metadata=new OracleCommand("SELECT TEMPORARY,DURATION FROM USER_TABLES WHERE TABLE_NAME=:Name",native){BindByName=true}){
                metadata.Parameters.Add("Name",OracleDbType.Varchar2).Value=probe;
                await using var reader=await metadata.ExecuteReaderAsync();Assert.IsTrue(await reader.ReadAsync());
                Assert.AreEqual(duration==""?"N":"Y",reader.GetString(0));
                Assert.AreEqual(duration==""?null:duration,reader.IsDBNull(1)?null:reader.GetString(1));
            }
            // Test-only extension of the private expected-schema map: the real activation
            // validates this owned probe through the SAME path as every canonical table.
            // No canonical table, role, account or schema is replaced or altered.
            definitions.Add(probe,definition);
            var owner=new NoSchemaDdlOwner(connection);
            var result=synchronous?owner.ActivateProvider():await owner.ActivateProviderAsync();
            accepted=!result.IsError&&result.Result;observed=result.Exception;
            Assert.AreEqual(0,owner.ProvisionCalls);await owner.DeActivateProviderAsync();
        }
        finally
        {
            if(definitions.TryGetValue(probe,out var current)){
                Assert.AreSame(definition,current);definitions.Remove(probe);
            }
            if(createdId.HasValue){
                Assert.AreEqual(createdId,await Identity(),"Cleanup must target exactly the acknowledged fixture object.");
                await using(var rows=new OracleCommand($"SELECT COUNT(*) FROM {probe}",native))Assert.AreEqual(0,Convert.ToInt32(await rows.ExecuteScalarAsync()));
                await using(var drop=new OracleCommand($"DROP TABLE {probe}",native))await drop.ExecuteNonQueryAsync();
                Assert.IsNull(await Identity());File.Delete(journal);
            }
            // A CREATE with unknown acknowledgement keeps its original journal for review.
        }
        Assert.AreEqual(duration=="",accepted,"Permanent native tables only; temporary records are not durable provider storage.");
        if(duration!="")Assert.IsInstanceOfType<InvalidDataException>(observed);
    }
}

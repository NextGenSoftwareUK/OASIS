using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [DataTestMethod]
    [DataRow(false,0)][DataRow(true,0)]
    [DataRow(false,1)][DataRow(true,1)]
    [DataRow(false,2)][DataRow(true,2)]
    [DataRow(false,3)][DataRow(true,3)]
    [DataRow(false,4)][DataRow(true,4)]
    [DataRow(false,5)][DataRow(true,5)]
    public async Task NativeExtraColumnsMustPermitCanonicalInsertWithoutSchemaRepair(bool synchronous,int scenario)
    {
        const string probe="OASIS_EXTRA_COLUMN_PROBE";
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,"extra-column.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var definitions=(Dictionary<string,Dictionary<string,string>>)typeof(OracleDBOASIS).GetField("StorageSchema",BindingFlags.NonPublic|BindingFlags.Static)!.GetValue(null)!;
        Assert.IsFalse(definitions.ContainsKey(probe));
        var definition=new Dictionary<string,string>{{"ID","RAW"}};
        var extra=scenario switch{
            0=>"EXTRA VARCHAR2(20)",
            1=>"EXTRA VARCHAR2(20) DEFAULT 'ok' NOT NULL",
            2=>"EXTRA VARCHAR2(20) NOT NULL",
            3=>"EXTRA NUMBER GENERATED ALWAYS AS IDENTITY NOT NULL",
            4=>"EXTRA VARCHAR2(32) GENERATED ALWAYS AS (RAWTOHEX(ID)) VIRTUAL NOT NULL",
            5=>"EXTRA VARCHAR2(20) INVISIBLE NOT NULL",
            _=>throw new ArgumentOutOfRangeException(nameof(scenario))};
        await using var native=new OracleConnection(connection);await native.OpenAsync();
        async Task<decimal?> Identity(){
            await using var command=new OracleCommand("SELECT OBJECT_ID FROM USER_OBJECTS WHERE OBJECT_NAME=:Name AND OBJECT_TYPE='TABLE'",native){BindByName=true};
            command.Parameters.Add("Name",OracleDbType.Varchar2).Value=probe;
            var value=await command.ExecuteScalarAsync();return value is null or DBNull?null:Convert.ToDecimal(value);
        }
        Assert.IsNull(await Identity(),"Never replace an existing table.");
        await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{state="before-owned-extra-column-probe",table=probe,scenario}));
        decimal? createdId=null;bool? accepted=null;Exception? observed=null;int? insertError=null;string? generatedSequence=null;
        try{
            await using(var create=new OracleCommand($"CREATE TABLE {probe}(ID RAW(16) NOT NULL PRIMARY KEY,{extra})",native))await create.ExecuteNonQueryAsync();
            createdId=await Identity();Assert.IsNotNull(createdId);
            if(scenario==3){
                await using var identity=new OracleCommand("SELECT SEQUENCE_NAME FROM USER_TAB_IDENTITY_COLS WHERE TABLE_NAME=:Name AND COLUMN_NAME='EXTRA'",native){BindByName=true};
                identity.Parameters.Add("Name",OracleDbType.Varchar2).Value=probe;
                generatedSequence=(string)(await identity.ExecuteScalarAsync())!;Assert.IsFalse(string.IsNullOrWhiteSpace(generatedSequence));
            }
            await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{state="owned-extra-column-probe-created",table=probe,scenario,objectId=createdId,generatedSequence}));
            definitions.Add(probe,definition);
            var owner=new NoSchemaDdlOwner(connection);
            var result=synchronous?owner.ActivateProvider():await owner.ActivateProviderAsync();
            accepted=!result.IsError&&result.Result;observed=result.Exception;
            Assert.AreEqual(0,owner.ProvisionCalls);await owner.DeActivateProviderAsync();
            // Actual canonical-shape INSERT omits unknown columns; all rows roll back.
            await using var transaction=native.BeginTransaction();
            try{
                await using var insert=new OracleCommand($"INSERT INTO {probe}(ID) VALUES(:Id)",native){BindByName=true,Transaction=transaction};
                insert.Parameters.Add("Id",OracleDbType.Raw,16).Value=Guid.NewGuid().ToByteArray();
                try{Assert.AreEqual(1,await insert.ExecuteNonQueryAsync());}
                catch(OracleException ex) when(ex.Number==1400){insertError=ex.Number;}
            }finally{transaction.Rollback();}
        }finally{
            if(definitions.TryGetValue(probe,out var current)){Assert.AreSame(definition,current);definitions.Remove(probe);}
            if(createdId.HasValue){
                Assert.AreEqual(createdId,await Identity(),"Only the acknowledged owned object may be removed.");
                await using(var rows=new OracleCommand($"SELECT COUNT(*) FROM {probe}",native))Assert.AreEqual(0,Convert.ToInt32(await rows.ExecuteScalarAsync()));
                await using(var drop=new OracleCommand($"DROP TABLE {probe} PURGE",native))await drop.ExecuteNonQueryAsync();
                Assert.IsNull(await Identity());
                if(generatedSequence is not null){
                    await using var remaining=new OracleCommand("SELECT COUNT(*) FROM USER_OBJECTS WHERE OBJECT_NAME=:Name AND OBJECT_TYPE='SEQUENCE'",native){BindByName=true};
                    remaining.Parameters.Add("Name",OracleDbType.Varchar2).Value=generatedSequence;
                    Assert.AreEqual(0,Convert.ToInt32(await remaining.ExecuteScalarAsync()));
                    Console.WriteLine(JsonSerializer.Serialize(new{retiredProbeObjectId=createdId,retiredIdentitySequence=generatedSequence}));
                }
                File.Delete(journal);
            }
        }
        var writable=scenario is not (2 or 5);
        Console.WriteLine(JsonSerializer.Serialize(new{scenario,accepted,insertError}));
        Assert.AreEqual(writable,accepted,"Activation must reject omitted required columns without defaults, including invisible columns.");
        Assert.AreEqual(writable?null:1400,insertError,"Catalog admission must agree with actual canonical-shape INSERT capability.");
        if(!writable)Assert.IsInstanceOfType<InvalidDataException>(observed);
    }
}

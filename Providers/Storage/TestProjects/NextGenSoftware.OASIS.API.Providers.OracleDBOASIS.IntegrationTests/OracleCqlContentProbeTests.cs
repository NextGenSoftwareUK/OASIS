using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class OracleCqlContentProbeTests
{
    [TestMethod]
    public async Task ReadOnlyCompiledJsonPredicatesPreserveTypesPathsAndEmptyStrings()
    {
        var uri=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")
            ?? throw new InvalidOperationException("Explicit retained Oracle runner required.");
        Assert.AreEqual("127.0.0.1:11521/FREEPDB1",new OracleConnectionStringBuilder(uri).DataSource);
        await using var connection=new OracleConnection(uri);
        await connection.OpenAsync();
        var id=Guid.NewGuid();var actor=Guid.NewGuid();var observed=0;
        async Task Check(string label,JObject document,Expression<Func<Holon,bool>> predicate,bool expected)
        {
            var captured=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.OracleDBOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor,ProviderType.OracleDBOASIS,new[]{id}).Result).Result
                .Where(predicate);
            Assert.IsFalse(captured.IsError,label+": "+captured.Message);
            var compiled=OracleCqlCompiler.Compile(captured.Result);
            Assert.IsFalse(compiled.IsError,label+": "+compiled.Message);
            // Substitute only the source table with a bound synthetic row. The
            // production predicate and binding owner are unchanged. No DDL/DML,
            // provider activation/index setup or business-row reads occur.
            const string synthetic="FROM (SELECT :payload DATA_JSON,:identity ID,0 HOLON_TYPE,0 IS_DELETED,CAST(NULL AS RAW(16)) PARENT_HOLON_ID FROM DUAL)";
            await using var command=new OracleCommand(compiled.Result.Sql.Replace("FROM OASIS_HOLONS",synthetic,StringComparison.Ordinal),connection){BindByName=true};
            compiled.Result.BindParameters(command);
            command.Parameters.Add("payload",OracleDbType.Clob).Value=document.ToString(Newtonsoft.Json.Formatting.None);
            command.Parameters.Add("identity",OracleDbType.Raw,16).Value=id.ToByteArray();
            await using var reader=await command.ExecuteReaderAsync();
            var matched=await reader.ReadAsync();
            Assert.AreEqual(expected,matched,label);
            if(matched)Assert.IsFalse(await reader.ReadAsync(),"One synthetic input row only.");
            observed++;
        }
        await Check("name",new JObject{{"Name","Espresso"}},h=>h.Name=="Espresso",true);
        await Check("contains",new JObject{{"Description","Strong coffee"}},h=>h.Description.Contains("coffee"),true);
        await Check("empty-equality",new JObject{{"Name",""}},h=>h.Name=="",true);
        await Check("null-is-not-empty",new JObject{{"Name",JValue.CreateNull()}},h=>h.Name=="",false);
        await Check("empty-contains",new JObject{{"Name",""}},h=>h.Name.Contains(""),true);
        await Check("array-leaf",new JObject{{"Name",new JArray("Espresso")}},h=>h.Name=="Espresso",false);
        await Check("metadata",new JObject{{"MetaData",new JObject{{"category","coffee"}}}},h=>CosmicQueryMetadata.Value<string>(h,"category")=="coffee",true);
        await Check("numeric-not-string",new JObject{{"MetaData",new JObject{{"category",123}}}},h=>CosmicQueryMetadata.Value<string>(h,"category")=="123",false);
        await Check("array-parent",new JObject{{"MetaData",new JArray(new JObject{{"category","coffee"}})}},h=>CosmicQueryMetadata.Value<string>(h,"category")=="coffee",false);
        await Check("nested-array",new JObject{{"MetaData",new JObject{{"location",new JArray(new JObject{{"city","London"}})}}}},h=>CosmicQueryMetadata.Value<string>(h,"location","city")=="London",false);
        await Check("numeric-object-key",new JObject{{"MetaData",new JObject{{"numeric",new JObject{{"0","coffee"}}}}}},h=>CosmicQueryMetadata.Value<string>(h,"numeric","0")=="coffee",true);
        await Check("numeric-array-step",new JObject{{"MetaData",new JObject{{"numeric",new JArray("coffee")}}}},h=>CosmicQueryMetadata.Value<string>(h,"numeric","0")=="coffee",false);
        var key="category'\"; DROP TABLE anything;--";
        await Check("escaped-key",new JObject{{"MetaData",new JObject{{key,"coffee"}}}},h=>CosmicQueryMetadata.Value<string>(h,key)=="coffee",true);
        await Check("trailing-space",new JObject{{"Name","Espresso "}},h=>h.Name=="Espresso",false);
        var bound=new string('x',4000);
        await Check("4000bytes",new JObject{{"Name",bound}},h=>h.Name==bound,true);
        Assert.AreEqual(15,observed);
    }
}

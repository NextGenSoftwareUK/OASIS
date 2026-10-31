using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public sealed class OracleBootstrapNativeTests
{
    private const string Schema="OASIS_BOOTSTRAP_TEST";
    private static readonly string[] Tables={"OASIS_AVATARS","OASIS_AVATAR_DETAILS","OASIS_HOLONS","OASIS_WRITE_RECEIPTS","OASIS_WRITE_OPERATION_LOCKS","OASIS_WRITE_PREPARATIONS"};
    private sealed record Identity(string User,int UserId,string Database,string Container,string Tablespace);
    private sealed class Owner(string connection):OracleDBOASIS(connection)
    {
        public int ProvisionCalls;
        protected override async Task ProvisionStorageSchemaAsync(OracleConnection connection){ProvisionCalls++;await base.ProvisionStorageSchemaAsync(connection);}
    }
    private static async Task<object?> Scalar(OracleConnection connection,string sql)
    {await using var command=new OracleCommand(sql,connection);return await command.ExecuteScalarAsync();}
    private static async Task Execute(OracleConnection connection,string sql)
    {await using var command=new OracleCommand(sql,connection);await command.ExecuteNonQueryAsync();}
    private static async Task WriteNew(string path,object value)
    {
        await using var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);
        await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(value));file.Flush(true);
    }
    private static async Task WriteState(string path,object value)
    {
        if(!File.Exists(path)){await WriteNew(path,value);return;}
        await WriteNew(path+".next",value);File.Move(path+".next",path,true);
    }
    private static async Task<string?> Read(OracleConnection connection,string table,Guid id)
    {
        Assert.IsTrue(Tables.Contains(table));
        await using var command=new OracleCommand($"SELECT DATA_JSON FROM {table} WHERE ID=:Id",connection){BindByName=true};
        command.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();
        var value=await command.ExecuteScalarAsync();return value==null||value==DBNull.Value?null:(string)value;
    }
    private static async Task<Dictionary<string,int>> TableIds(OracleConnection connection)
    {
        var values=new Dictionary<string,int>(StringComparer.Ordinal);
        await using var command=new OracleCommand("SELECT OBJECT_NAME,OBJECT_ID FROM USER_OBJECTS WHERE OBJECT_TYPE='TABLE'",connection);
        await using var reader=await command.ExecuteReaderAsync();while(await reader.ReadAsync())values.Add(reader.GetString(0),reader.GetInt32(1));
        return values;
    }
    private static async Task RequireEmpty(OracleConnection connection,IReadOnlyDictionary<string,int> identities)
    {
        var current=await TableIds(connection);CollectionAssert.AreEquivalent(identities.Keys.ToArray(),current.Keys.ToArray());
        foreach(var (table,id) in identities){
            Assert.AreEqual(id,current[table]);Assert.IsTrue(Tables.Contains(table));
            Assert.AreEqual(0,Convert.ToInt32(await Scalar(connection,$"SELECT COUNT(*) FROM {table}")),"Preserve any unexpected bootstrap-schema data.");
        }
    }

    [TestMethod]
    public async Task NativeInitialAndPartialSchemaProvisioningUsesOneOwnedEmptyNamespace()
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_ORACLE_ALLOW_BOOTSTRAP_TEST"),"Explicit retained-local bootstrap fixture approval is required.");
        var settings=new OracleConnectionStringBuilder(Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING"));
        Assert.AreEqual("SYSTEM",settings.UserID.ToUpperInvariant());
        Assert.AreEqual("127.0.0.1:11521/FREEPDB1",settings.DataSource,"Bootstrap fixture is local-only; never provision a hosted/customer schema.");
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var marker=Path.Combine(work,"oracle-bootstrap-schema.json");var identityPending=Path.Combine(work,"oracle-bootstrap-schema.pending.json");
        var pending=Path.Combine(work,"oracle-bootstrap-fixture.pending.json");
        Assert.IsFalse(File.Exists(identityPending));Assert.IsFalse(File.Exists(pending));Assert.IsFalse(File.Exists(pending+".next"));
        await using var admin=new OracleConnection(settings.ConnectionString);await admin.OpenAsync();
        var database=(string)(await Scalar(admin,"SELECT SYS_CONTEXT('USERENV','DB_UNIQUE_NAME') FROM DUAL"))!;
        var container=(string)(await Scalar(admin,"SELECT SYS_CONTEXT('USERENV','CON_NAME') FROM DUAL"))!;Assert.AreEqual("FREEPDB1",container);
        var tablespace=(string)(await Scalar(admin,"SELECT PROPERTY_VALUE FROM DATABASE_PROPERTIES WHERE PROPERTY_NAME='DEFAULT_PERMANENT_TABLESPACE'"))!;
        Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(tablespace,"^[A-Z][A-Z0-9_$#]{0,127}$"));
        Assert.AreEqual(1,Convert.ToInt32(await Scalar(admin,$"SELECT COUNT(*) FROM DBA_TABLESPACES WHERE TABLESPACE_NAME='{tablespace}' AND CONTENTS='PERMANENT' AND STATUS='ONLINE'")));
        var exists=Convert.ToInt32(await Scalar(admin,$"SELECT COUNT(*) FROM DBA_USERS WHERE USERNAME='{Schema}'"));
        Identity identity;
        if(File.Exists(marker)){
            identity=JsonSerializer.Deserialize<Identity>(await File.ReadAllTextAsync(marker))!;
            Assert.AreEqual(Schema,identity.User);Assert.AreEqual(database,identity.Database);Assert.AreEqual(container,identity.Container);Assert.AreEqual(tablespace,identity.Tablespace);
            Assert.AreEqual(1,exists);Assert.AreEqual(identity.UserId,Convert.ToInt32(await Scalar(admin,$"SELECT USER_ID FROM DBA_USERS WHERE USERNAME='{Schema}'")));
        }else{
            Assert.AreEqual(0,exists,"Never adopt or modify an unowned existing test account.");
            await WriteNew(identityPending,new{state="before-owned-bootstrap-identity",user=Schema,database,container,tablespace,quotaBytes=16777216});
            await Execute(admin,$"CREATE USER {Schema} NO AUTHENTICATION DEFAULT TABLESPACE {tablespace} QUOTA 16M ON {tablespace}");
            await Execute(admin,$"GRANT CREATE SESSION TO {Schema}");await Execute(admin,$"ALTER USER {Schema} GRANT CONNECT THROUGH SYSTEM");
            identity=new(Schema,Convert.ToInt32(await Scalar(admin,$"SELECT USER_ID FROM DBA_USERS WHERE USERNAME='{Schema}'")),database,container,tablespace);
            await WriteNew(marker,identity);File.Delete(identityPending);
        }
        Assert.AreEqual("NONE",await Scalar(admin,$"SELECT AUTHENTICATION_TYPE FROM DBA_USERS WHERE USERNAME='{Schema}'"));
        Assert.AreEqual(1,Convert.ToInt32(await Scalar(admin,$"SELECT COUNT(*) FROM DBA_SYS_PRIVS WHERE GRANTEE='{Schema}' AND PRIVILEGE='CREATE SESSION'")));
        Assert.AreEqual(1,Convert.ToInt32(await Scalar(admin,$"SELECT COUNT(*) FROM DBA_SYS_PRIVS WHERE GRANTEE='{Schema}'")));
        Assert.AreEqual(0,Convert.ToInt32(await Scalar(admin,$"SELECT COUNT(*) FROM DBA_ROLE_PRIVS WHERE GRANTEE='{Schema}'")));
        Assert.AreEqual(1,Convert.ToInt32(await Scalar(admin,$"SELECT COUNT(*) FROM DBA_PROXIES WHERE CLIENT='{Schema}' AND PROXY='SYSTEM'")));
        Assert.AreEqual(16777216L,Convert.ToInt64(await Scalar(admin,$"SELECT MAX_BYTES FROM DBA_TS_QUOTAS WHERE USERNAME='{Schema}' AND TABLESPACE_NAME='{tablespace}'")));
        var proxy=new OracleConnectionStringBuilder{UserID=Schema,ProxyUserId=settings.UserID,ProxyPassword=settings.Password,DataSource=settings.DataSource,Pooling=true,MinPoolSize=0,MaxPoolSize=4,ConnectionTimeout=10}.ConnectionString;
        await using var native=new OracleConnection(proxy);await native.OpenAsync();Assert.AreEqual(Schema,await Scalar(native,"SELECT SYS_CONTEXT('USERENV','SESSION_USER') FROM DUAL"));
        Assert.AreEqual(0,Convert.ToInt32(await Scalar(native,"SELECT COUNT(*) FROM USER_OBJECTS")),"Keep any residual or unknown fixture; never reset it.");
        var avatarId=Guid.NewGuid();var holonId=Guid.NewGuid();var owner=new Owner(proxy){IsVersionControlEnabled=true};
        Dictionary<string,int>? expectedIds=null;
        async Task State(string phase)=>await WriteState(pending,new{state=phase,identity,avatarId,holonId,expectedTableIds=expectedIds,tables=Tables});
        await State("before-owned-bootstrap-create-table-grant");var grantSent=false;long allocated=0;
        try{
            grantSent=true;await Execute(admin,$"GRANT CREATE TABLE TO {Schema}");
            await State("before-initial-provider-provisioning");
            var activation=await owner.ActivateProviderAsync();Assert.IsFalse(activation.IsError,activation.Exception?.ToString());Assert.IsTrue(activation.Result);Assert.AreEqual(1,owner.ProvisionCalls);
            expectedIds=await TableIds(native);CollectionAssert.AreEquivalent(Tables,expectedIds.Keys.ToArray());await RequireEmpty(native,expectedIds);
            await State("before-owned-bootstrap-row-fixture");
            var username=new string('é',256);var email="bootstrap-"+avatarId.ToString("N")+"@example.invalid";
            Assert.IsFalse((await owner.SaveAvatarAsync(new Avatar{Id=avatarId,Username=username,Email=email,IsActive=true})).IsError);
            Assert.IsFalse((await owner.SaveAvatarDetailAsync(new AvatarDetail{Id=avatarId,Username=username,Email=email,IsActive=true,Karma=17,XP=23})).IsError);
            Assert.IsFalse((await owner.SaveHolonAsync(new Holon{Id=holonId,Name="owned-bootstrap-holon",CreatedByAvatarId=avatarId,IsActive=true},saveChildren:false)).IsError);
            var avatar=await owner.LoadAvatarAsync(avatarId);Assert.IsFalse(avatar.IsError);Assert.AreEqual(username,avatar.Result.Username);
            var detail=await owner.LoadAvatarDetailAsync(avatarId);Assert.IsFalse(detail.IsError);Assert.AreEqual(17L,detail.Result.Karma);Assert.AreEqual(23L,detail.Result.XP);
            var holon=await owner.LoadHolonAsync(holonId,loadChildren:false);Assert.IsFalse(holon.IsError);Assert.AreEqual("owned-bootstrap-holon",holon.Result.Name);
            var rows=new List<(string Table,Guid Id,string Json)>();
            foreach(var (table,id) in new[]{("OASIS_AVATARS",avatarId),("OASIS_AVATAR_DETAILS",avatarId),("OASIS_HOLONS",holonId)}){
                var json=await Read(native,table,id);Assert.IsNotNull(json);using var parsed=JsonDocument.Parse(json);Assert.AreEqual(id,parsed.RootElement.GetProperty("Id").GetGuid());rows.Add((table,id,json));
            }
            allocated=Convert.ToInt64(await Scalar(native,"SELECT NVL(SUM(BYTES),0) FROM USER_SEGMENTS"));Assert.IsTrue(allocated<=16777216);
            await using(var transaction=(OracleTransaction)await native.BeginTransactionAsync()){
                foreach(var (table,id,json) in rows){
                    await using var cleanup=new OracleCommand($"DELETE FROM {table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",native){BindByName=true,Transaction=transaction};
                    cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=json;Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());
                }
                await transaction.CommitAsync();
            }
            foreach(var (table,id,_) in rows)Assert.IsNull(await Read(native,table,id));await RequireEmpty(native,expectedIds);
            await State("before-partial-schema-owned-empty-preparation-table-drop");
            var originalIds=new Dictionary<string,int>(expectedIds);await Execute(native,"DROP TABLE OASIS_WRITE_PREPARATIONS PURGE");
            await State("before-partial-provider-provisioning");
            activation=await owner.ActivateProviderAsync();Assert.IsFalse(activation.IsError,activation.Exception?.ToString());Assert.IsTrue(activation.Result);Assert.AreEqual(2,owner.ProvisionCalls);
            expectedIds=await TableIds(native);CollectionAssert.AreEquivalent(Tables,expectedIds.Keys.ToArray());
            foreach(var table in Tables)if(table=="OASIS_WRITE_PREPARATIONS")Assert.AreNotEqual(originalIds[table],expectedIds[table]);else Assert.AreEqual(originalIds[table],expectedIds[table]);
            await RequireEmpty(native,expectedIds);
            activation=await owner.ActivateProviderAsync();Assert.IsFalse(activation.IsError);Assert.AreEqual(2,owner.ProvisionCalls,"Complete schema must not repeatedly execute DDL.");
            await State("before-owned-empty-bootstrap-table-teardown");
            foreach(var table in Tables){await RequireEmpty(native,expectedIds);await Execute(native,$"DROP TABLE {table} PURGE");expectedIds.Remove(table);await State("during-owned-empty-bootstrap-table-teardown");}
            Assert.AreEqual(0,Convert.ToInt32(await Scalar(native,"SELECT COUNT(*) FROM USER_OBJECTS")));
            Assert.AreEqual(0,Convert.ToInt64(await Scalar(native,"SELECT NVL(SUM(BYTES),0) FROM USER_SEGMENTS")));
        }finally{
            await owner.DeActivateProviderAsync();
            if(grantSent){Assert.AreEqual(identity.UserId,Convert.ToInt32(await Scalar(admin,$"SELECT USER_ID FROM DBA_USERS WHERE USERNAME='{Schema}'")));await Execute(admin,$"REVOKE CREATE TABLE FROM {Schema}");}
        }
        Assert.AreEqual(1,Convert.ToInt32(await Scalar(admin,$"SELECT COUNT(*) FROM DBA_SYS_PRIVS WHERE GRANTEE='{Schema}'")));
        File.Delete(pending);
        Console.WriteLine(JsonSerializer.Serialize(new{verifiedBootstrapSchema=Schema,identity.UserId,initialAndPartialProvisionCalls=owner.ProvisionCalls,allocatedDuringFixture=allocated,finalAllocatedBytes=0,retainedReusableIdentity=true,otherSchemasReset=false}));
    }
}

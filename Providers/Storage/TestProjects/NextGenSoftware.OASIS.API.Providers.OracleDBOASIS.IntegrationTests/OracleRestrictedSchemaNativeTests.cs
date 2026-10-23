using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    private const string RestrictedSchema="OASIS_PRIV_TEST";
    private sealed record RestrictedSchemaIdentity(string User,int UserId,string Database,string Container,string Tablespace);

    private static async Task<string> RestrictedConnectionAsync(OracleConnection admin,string work)
    {
        // An opened connection may redact its password; use the protected runner environment,
        // never enable PersistSecurityInfo or print the resulting proxy connection string.
        var settings=new OracleConnectionStringBuilder(Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING"));
        Assert.AreEqual("SYSTEM",settings.UserID.ToUpperInvariant());
        Assert.AreEqual("127.0.0.1:11521/FREEPDB1",settings.DataSource);
        var marker=Path.Combine(work,"oracle-restricted-schema.json");
        var pending=Path.Combine(work,"oracle-restricted-schema.pending.json");
        Assert.IsFalse(File.Exists(pending),"Preserve and reconcile original schema bootstrap; never replay DDL.");
        async Task<object?> Scalar(string sql){await using var command=new OracleCommand(sql,admin);return await command.ExecuteScalarAsync();}
        async Task Execute(string sql){await using var command=new OracleCommand(sql,admin);await command.ExecuteNonQueryAsync();}
        var database=(string)(await Scalar("SELECT SYS_CONTEXT('USERENV','DB_UNIQUE_NAME') FROM DUAL"))!;
        var container=(string)(await Scalar("SELECT SYS_CONTEXT('USERENV','CON_NAME') FROM DUAL"))!;
        Assert.AreEqual("FREEPDB1",container);
        var tablespace=(string)(await Scalar("SELECT PROPERTY_VALUE FROM DATABASE_PROPERTIES WHERE PROPERTY_NAME='DEFAULT_PERMANENT_TABLESPACE'"))!;
        Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(tablespace,"^[A-Z][A-Z0-9_$#]{0,127}$"),"Review unusual tablespace identifier before DDL.");
        Assert.AreEqual(1,Convert.ToInt32(await Scalar($"SELECT COUNT(*) FROM DBA_TABLESPACES WHERE TABLESPACE_NAME='{tablespace}' AND CONTENTS='PERMANENT' AND STATUS='ONLINE'")));
        var exists=Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM DBA_USERS WHERE USERNAME='OASIS_PRIV_TEST'"));
        var proxy=new OracleConnectionStringBuilder{UserID=RestrictedSchema,ProxyUserId=settings.UserID,
            ProxyPassword=settings.Password,DataSource=settings.DataSource,Pooling=false,ConnectionTimeout=10}.ConnectionString;
        if(File.Exists(marker))
        {
            var identity=JsonSerializer.Deserialize<RestrictedSchemaIdentity>(await File.ReadAllTextAsync(marker))!;
            Assert.AreEqual(RestrictedSchema,identity.User);Assert.AreEqual(database,identity.Database);Assert.AreEqual(container,identity.Container);Assert.AreEqual(tablespace,identity.Tablespace);Assert.AreEqual(1,exists);
            Assert.AreEqual(identity.UserId,Convert.ToInt32(await Scalar("SELECT USER_ID FROM DBA_USERS WHERE USERNAME='OASIS_PRIV_TEST'")));
        }
        else
        {
            Assert.AreEqual(0,exists,"An unowned existing account must not be modified or adopted.");
            await File.WriteAllTextAsync(pending,JsonSerializer.Serialize(new{state="before-owned-restricted-schema-bootstrap",user=RestrictedSchema,database,container,tablespace}));
            // One retained schema, no authentication secret and no duplicate database/runtime.
            await Execute($"CREATE USER OASIS_PRIV_TEST NO AUTHENTICATION DEFAULT TABLESPACE {tablespace} QUOTA 16M ON {tablespace}");
            await Execute("GRANT CREATE SESSION TO OASIS_PRIV_TEST");
            await Execute("ALTER USER OASIS_PRIV_TEST GRANT CONNECT THROUGH SYSTEM");
            await Execute("GRANT CREATE TABLE TO OASIS_PRIV_TEST");
            try
            {
                var provisioner=new OracleDBOASIS(proxy);
                try{var provisioned=await provisioner.ActivateProviderAsync();Assert.IsFalse(provisioned.IsError,provisioned.Exception?.GetType().Name+": "+provisioned.Message);}
                finally{await provisioner.DeActivateProviderAsync();}
            }
            finally{await Execute("REVOKE CREATE TABLE FROM OASIS_PRIV_TEST");}
            var userId=Convert.ToInt32(await Scalar("SELECT USER_ID FROM DBA_USERS WHERE USERNAME='OASIS_PRIV_TEST'"));
            // The durable ownership marker is required for reuse and must not be pruned with reports.
            await File.WriteAllTextAsync(marker,JsonSerializer.Serialize(new RestrictedSchemaIdentity(RestrictedSchema,userId,database,container,tablespace)));
            File.Delete(pending);
        }
        Assert.AreEqual("NONE",(string)(await Scalar("SELECT AUTHENTICATION_TYPE FROM DBA_USERS WHERE USERNAME='OASIS_PRIV_TEST'"))!);
        Assert.AreEqual(1,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM DBA_SYS_PRIVS WHERE GRANTEE='OASIS_PRIV_TEST' AND PRIVILEGE='CREATE SESSION'")));
        Assert.AreEqual(1,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM DBA_SYS_PRIVS WHERE GRANTEE='OASIS_PRIV_TEST'")));
        Assert.AreEqual(0,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM DBA_ROLE_PRIVS WHERE GRANTEE='OASIS_PRIV_TEST'")));
        Assert.AreEqual(1,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM DBA_PROXIES WHERE CLIENT='OASIS_PRIV_TEST' AND PROXY='SYSTEM'")));
        return proxy;
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeRestrictedSchemaActivatesAndStoresWithoutProvisioningPrivileges(bool synchronous)
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        await using var admin=new OracleConnection(Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING"));await admin.OpenAsync();
        var connection=await RestrictedConnectionAsync(admin,work);
        await using var native=new OracleConnection(connection);await native.OpenAsync();
        async Task<object?> Scalar(string sql){await using var command=new OracleCommand(sql,native);return await command.ExecuteScalarAsync();}
        Assert.AreEqual(RestrictedSchema,(string)(await Scalar("SELECT SYS_CONTEXT('USERENV','SESSION_USER') FROM DUAL"))!);
        Assert.AreEqual("SYSTEM",(string)(await Scalar("SELECT SYS_CONTEXT('USERENV','PROXY_USER') FROM DUAL"))!);
        Assert.AreEqual(0,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM SESSION_PRIVS WHERE PRIVILEGE<>'CREATE SESSION'")));
        Assert.AreEqual(0,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM SESSION_ROLES")));
        var objectIds=(string)(await Scalar("SELECT LISTAGG(OBJECT_ID,',') WITHIN GROUP(ORDER BY OBJECT_NAME) FROM USER_OBJECTS WHERE OBJECT_TYPE='TABLE'"))!;
        Assert.AreEqual(6,Convert.ToInt32(await Scalar("SELECT COUNT(*) FROM USER_TABLES")));
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        var id=Guid.NewGuid();var holonId=Guid.NewGuid();var name="owned-restricted-"+id.ToString("N");
        var journal=Path.Combine(work,"restricted-schema-fixture.pending.json");Assert.IsFalse(File.Exists(journal));
        try
        {
            var active=synchronous?provider.ActivateProvider():await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);
            Assert.AreEqual(objectIds,(string)(await Scalar("SELECT LISTAGG(OBJECT_ID,',') WITHIN GROUP(ORDER BY OBJECT_NAME) FROM USER_OBJECTS WHERE OBJECT_TYPE='TABLE'"))!);
            foreach(var row in new[]{("OASIS_AVATARS",id),("OASIS_AVATAR_DETAILS",id),("OASIS_HOLONS",holonId)})Assert.IsNull(await Read(native,row.Item1,row.Item2));
            await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{state="before-owned-restricted-schema-fixture",schema=RestrictedSchema,id,holonId,name,synchronous}));
            var avatar=new Avatar{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true};
            var detail=new AvatarDetail{Id=id,Username=name,Email=avatar.Email,Karma=345,XP=678,IsActive=true};
            var holon=new Holon{Id=holonId,Name=name,MetaData=null,IsActive=true};
            var savedAvatar=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);Assert.IsFalse(savedAvatar.IsError,savedAvatar.Message);
            var savedDetail=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(savedDetail.IsError,savedDetail.Message);
            var savedHolon=synchronous?provider.SaveHolon(holon,saveChildren:false):await provider.SaveHolonAsync(holon,saveChildren:false);Assert.IsFalse(savedHolon.IsError,savedHolon.Message);
            var loadedAvatar=synchronous?provider.LoadAvatarByUsername(name):await provider.LoadAvatarByUsernameAsync(name);
            Assert.IsFalse(loadedAvatar.IsError,loadedAvatar.Message);Assert.AreEqual(id,loadedAvatar.Result.Id);
            var loadedDetail=synchronous?provider.LoadAvatarDetailByEmail(avatar.Email):await provider.LoadAvatarDetailByEmailAsync(avatar.Email);
            Assert.IsFalse(loadedDetail.IsError,loadedDetail.Message);Assert.AreEqual(345,loadedDetail.Result.Karma);Assert.AreEqual(678,loadedDetail.Result.XP);
            var loadedHolon=synchronous?provider.LoadHolon(holonId,loadChildren:false):await provider.LoadHolonAsync(holonId,loadChildren:false);
            Assert.IsFalse(loadedHolon.IsError,loadedHolon.Message);Assert.AreEqual(name,loadedHolon.Result.Name);
            var removed=synchronous?provider.DeleteHolon(holonId):await provider.DeleteHolonAsync(holonId);Assert.IsFalse(removed.IsError,removed.Message);Assert.IsTrue(removed.IsDeleted);
            var removedIdentity=synchronous?provider.DeleteAvatar(id,false):await provider.DeleteAvatarAsync(id,false);Assert.IsFalse(removedIdentity.IsError,removedIdentity.Message);Assert.IsTrue(removedIdentity.Result);
            foreach(var row in new[]{("OASIS_AVATARS",id),("OASIS_AVATAR_DETAILS",id),("OASIS_HOLONS",holonId)})Assert.IsNull(await Read(native,row.Item1,row.Item2));
            Assert.AreEqual(objectIds,(string)(await Scalar("SELECT LISTAGG(OBJECT_ID,',') WITHIN GROUP(ORDER BY OBJECT_NAME) FROM USER_OBJECTS WHERE OBJECT_TYPE='TABLE'"))!);
            File.Delete(journal);
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    private const string RestrictedSchema="OASIS_PRIV_TEST";
    private sealed class LiveDriverLostAckOwner(string connection):OracleLiveSchemaPreflight.ExistingSchemaOwner(connection)
    {
        internal int Commits;
        internal string? IntentToEdit,EditedIntent;
        protected override async Task CommitLifecycleTransactionAsync(OracleTransaction transaction)
        {
            await base.CommitLifecycleTransactionAsync(transaction);Commits++;
            if(IntentToEdit!=null){
                if(Commits==1){
                    var document=System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(IntentToEdit))!;document["owned-control-tamper"]=true;
                    EditedIntent=document.ToJsonString();await File.WriteAllTextAsync(IntentToEdit,EditedIntent);
                }
                return;
            }
            throw new IOException("Owned live-driver postcommit response withholding.");
        }
    }
    private sealed record RestrictedSchemaIdentity(string User,int UserId,string Database,string Container,string Tablespace);

    internal static string RestrictedTestConnectionString(OracleConnectionStringBuilder settings)
        =>new OracleConnectionStringBuilder{UserID=RestrictedSchema,ProxyUserId=settings.UserID,
            ProxyPassword=settings.Password,DataSource=settings.DataSource,Pooling=true,
            MinPoolSize=0,MaxPoolSize=16,ConnectionTimeout=10}.ConnectionString;

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
        var proxy=RestrictedTestConnectionString(settings);
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
            // Reuse the retained least-privilege schema to exercise the exact
            // hosted preflight implementation. Local TCP is explicitly NOT TLS proof.
            var database=(string)(await Scalar("SELECT SYS_CONTEXT('USERENV','DB_UNIQUE_NAME') FROM DUAL"))!;
            var service=(string)(await Scalar("SELECT SYS_CONTEXT('USERENV','SERVICE_NAME') FROM DUAL"))!;
            await OracleLiveSchemaPreflight.RunLocalControlAsync(connection,database,RestrictedSchema,service,work);
            var proofPath=Path.Combine(work,"oracle-schema-preflight.current.json");
            var proof=await File.ReadAllTextAsync(proofPath);
            using(var parsed=JsonDocument.Parse(proof)){
                Assert.AreEqual("verified-read-only",parsed.RootElement.GetProperty("state").GetString());
                Assert.IsFalse(parsed.RootElement.GetProperty("tcpsRequired").GetBoolean());
                Assert.IsFalse(parsed.RootElement.GetProperty("crudVerified").GetBoolean());
                Assert.IsFalse(parsed.RootElement.GetProperty("fullProviderVerified").GetBoolean());
                Assert.AreEqual(0,parsed.RootElement.GetProperty("provisioningCalls").GetInt32());
                var target=parsed.RootElement.GetProperty("target");
                Assert.AreEqual("tcp",target.GetProperty("Protocol").GetString()!.ToLowerInvariant());
                Assert.ThrowsException<InvalidDataException>(()=>OracleLiveSchemaPreflight.ValidateTarget(
                    new(database,RestrictedSchema,RestrictedSchema,service,target.GetProperty("Protocol").GetString()!),database,RestrictedSchema,service,true));
            }
            Assert.AreEqual(objectIds,(string)(await Scalar("SELECT LISTAGG(OBJECT_ID,',') WITHIN GROUP(ORDER BY OBJECT_NAME) FROM USER_OBJECTS WHERE OBJECT_TYPE='TABLE'"))!);
            foreach(var row in new[]{("OASIS_AVATARS",id),("OASIS_AVATAR_DETAILS",id),("OASIS_HOLONS",holonId)})Assert.IsNull(await Read(native,row.Item1,row.Item2));
            Console.WriteLine("LOCAL read-only schema preflight proof, not hosted TLS: "+proof);
            Assert.AreEqual(proof,await File.ReadAllTextAsync(proofPath));File.Delete(proofPath);
            await OracleLiveRecordAcceptance.RunLocalControlAsync(connection,database,RestrictedSchema,service,work);
            var recordProofPath=Path.Combine(work,"oracle-live-records.current.json");var recordProof=await File.ReadAllTextAsync(recordProofPath);
            using(var parsed=JsonDocument.Parse(recordProof)){
                Assert.AreEqual("verified-record-lifecycle",parsed.RootElement.GetProperty("state").GetString());
                Assert.IsTrue(parsed.RootElement.GetProperty("crudVerified").GetBoolean());Assert.IsFalse(parsed.RootElement.GetProperty("tcpsRequired").GetBoolean());
                Assert.IsFalse(parsed.RootElement.GetProperty("fullProviderVerified").GetBoolean());Assert.AreEqual(9,parsed.RootElement.GetProperty("publicMutationCalls").GetInt32());
                Assert.AreEqual(0,parsed.RootElement.GetProperty("provisioningCalls").GetInt32());Assert.AreEqual(0,parsed.RootElement.GetProperty("currentNativeRecordsRemaining").GetInt32());
                foreach(var ownedRow in new[]{("OASIS_AVATARS",parsed.RootElement.GetProperty("actor").GetGuid()),("OASIS_AVATAR_DETAILS",parsed.RootElement.GetProperty("actor").GetGuid()),("OASIS_HOLONS",parsed.RootElement.GetProperty("rootId").GetGuid()),("OASIS_HOLONS",parsed.RootElement.GetProperty("childId").GetGuid())})Assert.IsNull(await Read(native,ownedRow.Item1,ownedRow.Item2));
            }
            Assert.AreEqual(objectIds,(string)(await Scalar("SELECT LISTAGG(OBJECT_ID,',') WITHIN GROUP(ORDER BY OBJECT_NAME) FROM USER_OBJECTS WHERE OBJECT_TYPE='TABLE'"))!);
            Assert.IsFalse(File.Exists(Path.Combine(work,"oracle-live-records.pending.json")));
            Console.WriteLine("LOCAL bounded provider record proof, not hosted TLS: "+recordProof);
            Assert.AreEqual(recordProof,await File.ReadAllTextAsync(recordProofPath));File.Delete(recordProofPath);
            if(synchronous){
                foreach(var tamper in new[]{false,true}){
                var liveIntent=Path.Combine(work,"oracle-live-records.pending.json");
                var lostOwner=new LiveDriverLostAckOwner(connection){IntentToEdit=tamper?liveIntent:null};
                InvalidDataException? unknown=null;
                try{await OracleLiveRecordAcceptance.RunLocalControlAsync(connection,database,RestrictedSchema,service,work,_=>lostOwner);}
                catch(InvalidDataException failure){unknown=failure;}
                if(unknown==null){
                    var unexpectedProof=await File.ReadAllTextAsync(recordProofPath);using var unexpected=JsonDocument.Parse(unexpectedProof);
                    Assert.AreEqual(0,unexpected.RootElement.GetProperty("currentNativeRecordsRemaining").GetInt32());
                    foreach(var absent in new[]{("OASIS_AVATARS",unexpected.RootElement.GetProperty("actor").GetGuid()),("OASIS_AVATAR_DETAILS",unexpected.RootElement.GetProperty("actor").GetGuid()),("OASIS_HOLONS",unexpected.RootElement.GetProperty("rootId").GetGuid()),("OASIS_HOLONS",unexpected.RootElement.GetProperty("childId").GetGuid())})Assert.IsNull(await Read(native,absent.Item1,absent.Item2));
                    Console.WriteLine("LOCAL driver incorrectly continued after synthetic intent edit; independently verified owned row absence: "+unexpectedProof);
                    Console.WriteLine("Original edited synthetic intent: "+lostOwner.EditedIntent);
                    Assert.AreEqual(unexpectedProof,await File.ReadAllTextAsync(recordProofPath));File.Delete(recordProofPath);
                    Assert.Fail("Modified intent was overwritten and the driver continued instead of preserving original evidence.");
                }
                if(tamper)StringAssert.Contains(unknown.Message,"intent changed");
                else Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(unknown));
                Assert.AreEqual(1,lostOwner.Commits,"No write retry or error cleanup may follow a lost acknowledgement.");Assert.AreEqual(0,lostOwner.ProvisionCalls);
                var originalIntent=await File.ReadAllTextAsync(liveIntent);if(tamper)Assert.AreEqual(lostOwner.EditedIntent,originalIntent);
                using var parsed=JsonDocument.Parse(originalIntent);var actor=parsed.RootElement.GetProperty("actor").GetGuid();
                Assert.AreEqual("before-create-avatar",parsed.RootElement.GetProperty("state").GetString());Assert.AreEqual(1,parsed.RootElement.GetProperty("admittedPublicMutationCalls").GetInt32());
                var original=await Read(native,"OASIS_AVATARS",actor);Assert.IsNotNull(original);
                using(var stored=JsonDocument.Parse(original)){
                    Assert.AreEqual(actor,stored.RootElement.GetProperty("Id").GetGuid());Assert.AreEqual(1,stored.RootElement.GetProperty("Version").GetInt32());
                    Assert.AreEqual(parsed.RootElement.GetProperty("name").GetString(),stored.RootElement.GetProperty("Username").GetString());
                }
                foreach(var absent in new[]{("OASIS_AVATAR_DETAILS",actor),("OASIS_HOLONS",parsed.RootElement.GetProperty("rootId").GetGuid()),("OASIS_HOLONS",parsed.RootElement.GetProperty("childId").GetGuid())})Assert.IsNull(await Read(native,absent.Item1,absent.Item2));
                Assert.IsFalse(File.Exists(recordProofPath));Assert.AreEqual(originalIntent,await File.ReadAllTextAsync(liveIntent));
                Console.WriteLine("LOCAL original driver fault fixture observed before exact cleanup: "+JsonSerializer.Serialize(new{actor,tamper,SHA256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(original))),commits=lostOwner.Commits,replay=false}));
                await using(var transaction=native.BeginTransaction()){
                    await using var delete=new OracleCommand("DELETE FROM OASIS_AVATARS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",native){BindByName=true,Transaction=transaction};
                    delete.Parameters.Add("Id",OracleDbType.Raw).Value=actor.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=original;Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());transaction.Commit();
                }
                Assert.IsNull(await Read(native,"OASIS_AVATARS",actor));Assert.AreEqual(originalIntent,await File.ReadAllTextAsync(liveIntent));File.Delete(liveIntent);
                }
            }
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}

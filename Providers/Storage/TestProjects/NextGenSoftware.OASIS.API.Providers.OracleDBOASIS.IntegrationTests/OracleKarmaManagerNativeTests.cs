using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeV2KarmaManagerPersistsLedgerAndDetailHistory(bool synchronous)
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        await using var admin=new OracleConnection(Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING"));await admin.OpenAsync();
        var connection=await RestrictedConnectionAsync(admin,work); // Reuse the SAME restricted schema, no second environment.
        var journal=Path.Combine(work,"v2-karma-manager.pending.json");
        Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var name="owned-native-karma-"+id.ToString("N");
        var ledgerId=new Guid(MD5.HashData(Encoding.UTF8.GetBytes($"settings:{id}:karma")));
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        await using var native=new OracleConnection(connection);await native.OpenAsync();
        await using(var privileges=new OracleCommand("SELECT COUNT(*) FROM SESSION_PRIVS WHERE PRIVILEGE<>'CREATE SESSION'",native))
            Assert.AreEqual(0,Convert.ToInt32(await privileges.ExecuteScalarAsync()));
        var identities=new[]{(Table:"OASIS_AVATARS",Id:id),(Table:"OASIS_AVATAR_DETAILS",Id:id),(Table:"OASIS_HOLONS",Id:ledgerId)};
        foreach(var row in identities)Assert.IsNull(await Read(native,row.Table,row.Id));
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            // Settings bootstrap must distinguish a known absent ledger from a provider failure.
            // This read-only admission runs BEFORE creating any synthetic profile or journal.
            var absent=await TypedOracleRoleManager(provider).LoadHolonAsync(ledgerId,loadChildren:false,
                providerType:ProviderType.OracleDBOASIS,allowMissing:true);
            Assert.IsFalse(absent.IsError,absent.Message);Assert.IsNull(absent.Result);
            await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{state="before-owned-v2-karma-manager",schema=RestrictedSchema,id,ledgerId,name,synchronous}));
            Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true})).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=name,Email=name+"@example.invalid",Karma=0,XP=222,IsActive=true})).IsError);
            var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new(){PlanType="Gold"}}};
            var providers=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
            providers.RegisterProvider(provider);Assert.IsFalse(providers.SetAndActivateCurrentStorageProvider(provider).IsError);
            // Same existing isolated constructor used by native AWS affinity acceptance; no globals replaced.
            AvatarManager Manager()=> (AvatarManager)typeof(AvatarManager).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,
                new[]{typeof(IOASISStorageProvider),typeof(OASISDNA),typeof(ProviderManager)},null)!.Invoke(new object[]{provider,dna,providers});
            var manager=Manager();var payload=new AvatarDetail{Id=id};
            async Task<KarmaAkashicRecord?> Mutate(bool remove,bool fromPayload)
            {
                if(synchronous){
                    if(remove&&fromPayload)return manager.RemoveKarmaFromAvatar(payload,KarmaTypeNegative.DropLitter,KarmaSourceType.Game,"owned","native Oracle",providerType:ProviderType.OracleDBOASIS);
                    var result=remove?manager.RemoveKarmaFromAvatar(id,KarmaTypeNegative.DropLitter,KarmaSourceType.Game,"owned","native Oracle",providerType:ProviderType.OracleDBOASIS)
                        :fromPayload?manager.AddKarmaToAvatar(payload,KarmaTypePositive.ContributingToTheOASISWithCode,KarmaSourceType.Game,"owned","native Oracle",providerType:ProviderType.OracleDBOASIS)
                        :manager.AddKarmaToAvatar(id,KarmaTypePositive.ContributingToTheOASISWithCode,KarmaSourceType.Game,"owned","native Oracle",providerType:ProviderType.OracleDBOASIS);
                    Assert.IsFalse(result.IsError,result.Message);Assert.IsFalse(result.IsWarning,result.Message);return result.Result;
                }
                return remove?fromPayload?await manager.RemoveKarmaFromAvatarAsync(payload,KarmaTypeNegative.DropLitter,KarmaSourceType.Game,"owned","native Oracle",providerType:ProviderType.OracleDBOASIS)
                    :await manager.RemoveKarmaFromAvatarAsync(id,KarmaTypeNegative.DropLitter,KarmaSourceType.Game,"owned","native Oracle",providerType:ProviderType.OracleDBOASIS)
                    :fromPayload?await manager.AddKarmaToAvatarAsync(payload,KarmaTypePositive.ContributingToTheOASISWithCode,KarmaSourceType.Game,"owned","native Oracle",providerType:ProviderType.OracleDBOASIS)
                    :await manager.AddKarmaToAvatarAsync(id,KarmaTypePositive.ContributingToTheOASISWithCode,KarmaSourceType.Game,"owned","native Oracle",providerType:ProviderType.OracleDBOASIS);
            }
            var totals=new long[]{500,1000,500,0};
            for(var stage=0;stage<4;stage++){
                var result=await Mutate(stage>=2,stage%2==1);Assert.IsNotNull(result,"Original manager outcome missing; preserve journal, no replay.");
                Assert.AreEqual(id,result.AvatarId);Assert.AreEqual(ProviderType.OracleDBOASIS,result.Provider.Value);Assert.AreEqual(totals[stage],result.TotalKarma);
                var detail=await provider.LoadAvatarDetailAsync(id);Assert.IsFalse(detail.IsError,detail.Message);
                Assert.AreEqual(totals[stage],detail.Result.Karma);Assert.AreEqual(222L,detail.Result.XP);Assert.AreEqual(stage+1,detail.Result.KarmaAkashicRecords.Count);
                for(var index=0;index<=stage;index++){
                    var entry=detail.Result.KarmaAkashicRecords[index];Assert.AreEqual(id,entry.AvatarId);Assert.AreEqual(totals[index],entry.TotalKarma);
                    Assert.AreEqual(ProviderType.OracleDBOASIS,entry.Provider.Value);Assert.AreEqual(KarmaSourceType.Game,entry.KarmaSource.Value);
                    Assert.AreEqual("owned",entry.KarmaSourceTitle);Assert.AreEqual("native Oracle",entry.KarmaSourceDesc);
                    Assert.AreEqual(index<2?KarmaEarntOrLost.Earnt:KarmaEarntOrLost.Lost,entry.KarmaEarntOrLost.Value);
                    Assert.AreEqual(index<2?KarmaTypePositive.ContributingToTheOASISWithCode:KarmaTypePositive.None,entry.KarmaTypePositive.Value);
                    Assert.AreEqual(index<2?KarmaTypeNegative.None:KarmaTypeNegative.DropLitter,entry.KarmaTypeNegative.Value);
                    Assert.AreNotEqual(DateTime.MinValue,entry.Date);
                }
                var ledger=await provider.LoadHolonAsync(ledgerId,loadChildren:false);Assert.IsFalse(ledger.IsError,ledger.Message);
                Assert.AreEqual(totals[stage].ToString(System.Globalization.CultureInfo.InvariantCulture),ledger.Result.MetaData["totalKarma"].ToString());
                var history=Core.Serialization.StoredRecordJson.Deserialize<List<Dictionary<string,object>>>(
                    Core.Serialization.StoredRecordJson.Serialize(ledger.Result.MetaData["history"]));
                Assert.IsNotNull(history);Assert.AreEqual(stage+1,history.Count);
                using var raw=JsonDocument.Parse((await Read(native,"OASIS_HOLONS",ledgerId))!);
                Assert.AreEqual(id,raw.RootElement.GetProperty("CreatedByAvatarId").GetGuid());
                Assert.AreSame(provider,providers.CurrentStorageProvider);
                manager=Manager(); // Fresh domain manager must recover durable state, not its predecessor's cache.
            }
            var defaultAward=synchronous?manager.AddKarmaToAvatar(id,KarmaTypePositive.Walking100Steps,KarmaSourceType.Game,"owned","native Oracle default").Result
                :await manager.AddKarmaToAvatarAsync(id,KarmaTypePositive.Walking100Steps,KarmaSourceType.Game,"owned","native Oracle default");
            Assert.IsNotNull(defaultAward);Assert.AreEqual(ProviderType.OracleDBOASIS,defaultAward.Provider.Value);Assert.AreEqual(10L,defaultAward.TotalKarma);
            var finalDetail=await provider.LoadAvatarDetailAsync(id);Assert.IsFalse(finalDetail.IsError,finalDetail.Message);
            Assert.AreEqual(10L,finalDetail.Result.Karma);Assert.AreEqual(222L,finalDetail.Result.XP);Assert.AreEqual(5,finalDetail.Result.KarmaAkashicRecords.Count);
            Assert.AreSame(provider,providers.CurrentStorageProvider);
            foreach(var row in identities){
                var expected=await Read(native,row.Table,row.Id);Assert.IsNotNull(expected);
                using(var parsed=JsonDocument.Parse(expected))Assert.AreEqual(row.Id,parsed.RootElement.GetProperty("Id").GetGuid());
                await using var cleanup=new OracleCommand($"DELETE FROM {row.Table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",native){BindByName=true};
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=row.Id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=expected;
                Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());Assert.IsNull(await Read(native,row.Table,row.Id));
            }
            File.Delete(journal);
        }
        finally{await provider.DeActivateProviderAsync();}
    }

    [TestMethod]
    public async Task ReconcileKnownKarmaHistoryAssertionFixture()
    {
        var work=Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!;
        var report=Path.Combine(work,"oracle-karma-manager-history-shape-assertion.trx");
        var pending=Path.Combine(work,"v2-karma-manager.pending.json");
        Assert.AreEqual("4F20A71B1FE484F1A9B9ACC4C08AB7ABAE92B87CC5EAE902BA598F2FE2E5F494",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(report))));
        Assert.AreEqual("336783EB7ED626BA9210B92307AD3254605447DFFE374147F8B05BC36B407361",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(pending))));
        Assert.AreEqual(1,Directory.GetFiles(work,"*.pending.json").Length);
        using var intent=JsonDocument.Parse(await File.ReadAllTextAsync(pending));var original=intent.RootElement;
        var id=original.GetProperty("id").GetGuid();var ledgerId=original.GetProperty("ledgerId").GetGuid();var name=original.GetProperty("name").GetString();
        Assert.AreEqual(Guid.Parse("d70ba50f-f2fa-43d9-b10b-4a50afde823c"),id);Assert.AreEqual(Guid.Parse("69efe47b-3148-6e7c-c686-5d890092b8fa"),ledgerId);
        Assert.AreEqual("before-owned-v2-karma-manager",original.GetProperty("state").GetString());Assert.AreEqual(RestrictedSchema,original.GetProperty("schema").GetString());
        await using var admin=new OracleConnection(Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING"));await admin.OpenAsync();
        await using var native=new OracleConnection(await RestrictedConnectionAsync(admin,work));await native.OpenAsync();
        var rows=new List<(string Table,Guid Id,string Json)>();
        foreach(var row in new[]{("OASIS_AVATARS",id),("OASIS_AVATAR_DETAILS",id),("OASIS_HOLONS",ledgerId)}){
            var json=await Read(native,row.Item1,row.Item2);Assert.IsNotNull(json);rows.Add((row.Item1,row.Item2,json));
        }
        var avatar=Core.Serialization.StoredRecordJson.DeserializeStored<Avatar>(rows[0].Json)!;
        var detail=Core.Serialization.StoredRecordJson.DeserializeStored<AvatarDetail>(rows[1].Json)!;
        var ledger=Core.Serialization.StoredRecordJson.DeserializeStored<Holon>(rows[2].Json)!;
        Assert.AreEqual(id,avatar.Id);Assert.AreEqual(name,avatar.Username);Assert.AreEqual(name+"@example.invalid",avatar.Email);
        Assert.AreEqual(id,detail.Id);Assert.AreEqual(name,detail.Username);Assert.AreEqual(500L,detail.Karma);Assert.AreEqual(222L,detail.XP);
        Assert.AreEqual(1,detail.KarmaAkashicRecords.Count);Assert.AreEqual(id,detail.KarmaAkashicRecords[0].AvatarId);Assert.AreEqual(500L,detail.KarmaAkashicRecords[0].TotalKarma);
        Assert.AreEqual(ledgerId,ledger.Id);Assert.AreEqual(id,ledger.CreatedByAvatarId);Assert.AreEqual("500",ledger.MetaData["totalKarma"].ToString());
        var history=Core.Serialization.StoredRecordJson.Deserialize<List<Dictionary<string,object>>>(Core.Serialization.StoredRecordJson.Serialize(ledger.MetaData["history"]));
        Assert.IsNotNull(history);Assert.AreEqual(1,history.Count);
        foreach(var table in new[]{"OASIS_WRITE_RECEIPTS","OASIS_WRITE_PREPARATIONS","OASIS_WRITE_OPERATION_LOCKS"}){
            await using var count=new OracleCommand($"SELECT COUNT(*) FROM {table}",native);Assert.AreEqual(0,Convert.ToInt32(await count.ExecuteScalarAsync()));
        }
        await using(var transaction=(OracleTransaction)await native.BeginTransactionAsync()){
            foreach(var row in rows){
                await using var cleanup=new OracleCommand($"DELETE FROM {row.Table} WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",native){BindByName=true,Transaction=transaction};
                cleanup.Parameters.Add("Id",OracleDbType.Raw).Value=row.Id.ToByteArray();cleanup.Parameters.Add("Expected",OracleDbType.Clob).Value=row.Json;
                Assert.AreEqual(1,await cleanup.ExecuteNonQueryAsync());
            }
            await transaction.CommitAsync();
        }
        foreach(var row in rows)Assert.IsNull(await Read(native,row.Table,row.Id));File.Delete(pending);
    }
}

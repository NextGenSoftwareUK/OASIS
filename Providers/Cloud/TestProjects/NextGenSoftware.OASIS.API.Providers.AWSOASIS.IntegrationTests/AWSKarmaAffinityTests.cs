using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeKarmaManagersKeepExplicitAndDefaultProviderAffinity(bool synchronous)
    {
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")??throw new InvalidOperationException("Retained runner required.");
        var path=Path.GetFullPath(Path.Combine(work,"aws-karma-affinity.db"));
        Assert.IsFalse(File.Exists(path),"Preserve prior karma fixture; no overwrite or replay.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var verified=false;
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            foreach(var requestedAws in new[] {true,false})
            {
                IOASISStorageProvider requested=requestedAws?_provider:sqlite;
                IOASISStorageProvider initial=requestedAws?sqlite:_provider;
                var id=Guid.NewGuid();
                foreach(var store in new[] {initial,requested})
                {
                    var isInitial=ReferenceEquals(store,initial);var name="owned-karma-"+id.ToString("N");
                    var profile=await store.SaveAvatarAsync(new Avatar {Id=id,Username=name,Email=name+"@example.invalid",IsActive=true});
                    Assert.IsFalse(profile.IsError,profile.Message);
                    var detail=await store.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username=name,Email=name+"@example.invalid",
                        XP=isInitial?111:222,Karma=isInitial?37:0,IsActive=true});
                    Assert.IsFalse(detail.IsError,detail.Message);
                }
                var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                    HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
                var providers=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
                providers.RegisterProvider(initial);providers.RegisterProvider(requested);
                Assert.IsFalse(providers.SetAndActivateCurrentStorageProvider(initial).IsError);
                // Use the existing isolated constructor without changing process-global
                // singletons or adding a production API solely for this test.
                var constructor=typeof(AvatarManager).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,
                    new[] {typeof(IOASISStorageProvider),typeof(OASISDNA),typeof(ProviderManager)},null)!;
                var manager=(AvatarManager)constructor.Invoke(new object[] {initial,dna,providers});
                var requestedType=requested.ProviderType.Value;
                var avatar=new AvatarDetail {Id=id};
                async Task<KarmaAkashicRecord?> Mutate(bool remove,bool payload)
                {
                    if(synchronous)
                    {
                        if(remove&&payload)return manager.RemoveKarmaFromAvatar(avatar,KarmaTypeNegative.DropLitter,KarmaSourceType.Game,"owned","native affinity",providerType:requestedType);
                        var result=remove
                            ?manager.RemoveKarmaFromAvatar(id,KarmaTypeNegative.DropLitter,KarmaSourceType.Game,"owned","native affinity",providerType:requestedType)
                            :payload?manager.AddKarmaToAvatar(avatar,KarmaTypePositive.ContributingToTheOASISWithCode,KarmaSourceType.Game,"owned","native affinity",providerType:requestedType)
                                :manager.AddKarmaToAvatar(id,KarmaTypePositive.ContributingToTheOASISWithCode,KarmaSourceType.Game,"owned","native affinity",providerType:requestedType);
                        Assert.IsFalse(result.IsError,result.Message);
                        Assert.IsFalse(result.IsWarning,result.Message);
                        return result.Result;
                    }
                    return remove
                        ?payload?await manager.RemoveKarmaFromAvatarAsync(avatar,KarmaTypeNegative.DropLitter,KarmaSourceType.Game,"owned","native affinity",providerType:requestedType)
                            :await manager.RemoveKarmaFromAvatarAsync(id,KarmaTypeNegative.DropLitter,KarmaSourceType.Game,"owned","native affinity",providerType:requestedType)
                        :payload?await manager.AddKarmaToAvatarAsync(avatar,KarmaTypePositive.ContributingToTheOASISWithCode,KarmaSourceType.Game,"owned","native affinity",providerType:requestedType)
                            :await manager.AddKarmaToAvatarAsync(id,KarmaTypePositive.ContributingToTheOASISWithCode,KarmaSourceType.Game,"owned","native affinity",providerType:requestedType);
                }
                var ledgerId=new Guid(MD5.HashData(Encoding.UTF8.GetBytes($"settings:{id}:karma")));
                var expectedTotals=new long[] {500,1000,500,0};
                for(var stage=0;stage<4;stage++)
                {
                    var record=await Mutate(stage>=2,stage%2==1);
                    Assert.IsNotNull(record,"Native karma operation returned no committed record.");
                    Assert.AreEqual(id,record.AvatarId);Assert.AreEqual(requestedType,record.Provider.Value);
                    Assert.AreEqual(expectedTotals[stage],record.TotalKarma);
                    var selected=await requested.LoadAvatarDetailAsync(id);var untouched=await initial.LoadAvatarDetailAsync(id);
                    Assert.IsFalse(selected.IsError,selected.Message);Assert.IsFalse(untouched.IsError,untouched.Message);
                    Assert.AreEqual(expectedTotals[stage],selected.Result.Karma);Assert.AreEqual(222L,selected.Result.XP);
                    Assert.AreEqual(stage+1,selected.Result.KarmaAkashicRecords.Count,
                        $"History projection missing: provider={requestedType}, synchronous={synchronous}, stage={stage}, avatar={id}.");
                    Assert.AreEqual(37L,untouched.Result.Karma);Assert.AreEqual(111L,untouched.Result.XP);
                    var ledger=await requested.LoadHolonAsync(ledgerId,loadChildren:false);
                    Assert.IsFalse(ledger.IsError,ledger.Message);Assert.IsNotNull(ledger.Result);
                    Assert.AreEqual(expectedTotals[stage].ToString(System.Globalization.CultureInfo.InvariantCulture),ledger.Result.MetaData["totalKarma"].ToString());
                    Assert.AreEqual(stage+1,JArray.FromObject(ledger.Result.MetaData["history"]).Count);
                    Assert.AreEqual(0,(await initial.LoadAvatarDetailAsync(id)).Result.KarmaAkashicRecords.Count);
                    Assert.IsNull((await initial.LoadHolonAsync(ledgerId,loadChildren:false)).Result);
                    Assert.AreSame(initial,providers.CurrentStorageProvider);
                }
                var defaultRecord=synchronous
                    ?manager.AddKarmaToAvatar(id,KarmaTypePositive.Walking100Steps,KarmaSourceType.Game,"owned","default affinity").Result
                    :await manager.AddKarmaToAvatarAsync(id,KarmaTypePositive.Walking100Steps,KarmaSourceType.Game,"owned","default affinity");
                Assert.IsNotNull(defaultRecord);Assert.AreEqual(initial.ProviderType.Value,defaultRecord.Provider.Value);
                Assert.AreEqual(10L,(await initial.LoadAvatarDetailAsync(id)).Result.Karma);
                Assert.AreEqual(0L,(await requested.LoadAvatarDetailAsync(id)).Result.Karma);
                Assert.AreSame(initial,providers.CurrentStorageProvider);
            }
            verified=true;
        }
        finally
        {
            await sqlite.DeActivateProviderAsync();
            if(verified)foreach(var file in new[] {path,path+"-wal",path+"-shm"})if(File.Exists(file))File.Delete(file);
        }
    }
}

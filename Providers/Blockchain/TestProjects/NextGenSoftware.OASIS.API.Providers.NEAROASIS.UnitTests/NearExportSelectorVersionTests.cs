using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Security.Cryptography;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass,DoNotParallelize]
public class NearExportSelectorVersionTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task ExportResolvesCurrentIdentityAndPreservesHolonVersion(bool synchronous,bool email)
    {
        var provider=new Probe();var result=await Invoke(provider,synchronous,email,3);
        Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(0,provider.IdentityVersion);
        Assert.AreEqual(3,provider.ExportVersion);Assert.AreEqual(provider.Owner,provider.ExportOwner);
        Assert.AreEqual(3,result.Result.Single().Version);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task IdentityFailurePreservesEnvelopeAndDoesNotExport(bool synchronous,bool email)
    {
        var provider=new Probe{Fail=true};var result=await Invoke(provider,synchronous,email,3);
        Assert.IsTrue(result.IsError);Assert.AreSame(provider.Cause,result.Exception);
        Assert.AreEqual("owned-detail",result.DetailedMessage);Assert.AreEqual("owned-code",result.ErrorCode);
        Assert.AreEqual(2,result.WarningCount);Assert.AreEqual("owned",result.MetaData["marker"]);
        Assert.AreEqual(0,provider.ExportCalls);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NegativeVersionFailsBeforeIdentityDispatch(bool synchronous,bool email)
    {
        var provider=new Probe();var result=await Invoke(provider,synchronous,email,-1);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentOutOfRangeException));
        Assert.AreEqual(0,provider.IdentityCalls);Assert.AreEqual(0,provider.ExportCalls);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task InvalidOwnedExportFailsBeforeSdkActivation(bool synchronous,bool negative)
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","local","local","test.near");
        var id=negative?Guid.NewGuid():Guid.Empty;var version=negative?-1:0;
        var result=synchronous?provider.ExportAllDataForAvatarById(id,version):await provider.ExportAllDataForAvatarByIdAsync(id,version);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
        Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task OwnedExportCountsOnlyMatchingEncryptedRoots(bool synchronous,bool empty)
    {
        const string keySetting="OASIS_NEAR_STORAGE_ENCRYPTION_KEY",rowsSetting="OASIS_NEAR_EXPORT_FIXTURE_ROWS";
        var previousKey=Environment.GetEnvironmentVariable(keySetting);var previousRows=Environment.GetEnvironmentVariable(rowsSetting);
        var bytes=RandomNumberGenerator.GetBytes(32);
        try
        {
            Environment.SetEnvironmentVariable(keySetting,Convert.ToBase64String(bytes));
            var provider=new NEAROASIS("http://127.0.0.1:1","localnet","localnet","contract.local","sender.local","");
            var serviceType=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
            var service=Activator.CreateInstance(serviceType,new object[]{"http://127.0.0.1:1","localnet","contract.local","sender.local","unused","node",Path.Combine(AppContext.BaseDirectory,"Fixtures","storage-snapshot.mjs")})!;
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(NEAROASIS).GetField("_sdk",flags)!.SetValue(provider,service);
            typeof(NEAROASIS).GetField("_isActivated",flags)!.SetValue(provider,true);
            var owner=Guid.NewGuid();var rows=new List<string[]>();
            using var codec=new EncryptedRecordCodec(bytes,compress:true);
            for(var index=0;index<3;index++)
            {
                var record=new Holon{Id=Guid.NewGuid(),Version=3,CreatedByAvatarId=!empty && index<2?owner:Guid.NewGuid()};
                var key=$"holon:{record.Id:D}";
                var context=(string)serviceType.GetMethod("StorageContext",flags)!.Invoke(service,new object[]{key})!;
                rows.Add(new[]{key,codec.Encrypt(context,OasisJson.Serialize(record))});
            }
            Environment.SetEnvironmentVariable(rowsSetting,System.Text.Json.JsonSerializer.Serialize(rows));
            var result=synchronous?provider.ExportAllDataForAvatarById(owner,3):await provider.ExportAllDataForAvatarByIdAsync(owner,3);
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);
            Assert.AreEqual(empty?0:2,result.LoadedCount);Assert.AreEqual(result.LoadedCount,result.Result.Count());
            Assert.IsTrue(result.Result.All(row=>row.CreatedByAvatarId==owner && row.Version==3));
        }
        finally
        {
            Environment.SetEnvironmentVariable(keySetting,previousKey);Environment.SetEnvironmentVariable(rowsSetting,previousRows);
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    private static Task<OASISResult<IEnumerable<IHolon>>> Invoke(Probe provider,bool synchronous,bool email,int version)
        => synchronous?Task.FromResult(email?provider.ExportAllDataForAvatarByEmail("owned@example.invalid",version):provider.ExportAllDataForAvatarByUsername("owned",version))
            :email?provider.ExportAllDataForAvatarByEmailAsync("owned@example.invalid",version):provider.ExportAllDataForAvatarByUsernameAsync("owned",version);

    private sealed class Probe():NEAROASIS("http://127.0.0.1:1","local","local","test.near")
    {
        public readonly Guid Owner=Guid.NewGuid();public readonly IOException Cause=new("controlled identity failure");
        public bool Fail;public int IdentityCalls,IdentityVersion,ExportCalls,ExportVersion;public Guid ExportOwner;
        private Task<OASISResult<IAvatar>> Resolve(int version)
        {
            IdentityCalls++;IdentityVersion=version;
            return Task.FromResult(Fail?new OASISResult<IAvatar>{IsError=true,Exception=Cause,ErrorCode="owned-code",DetailedMessage="owned-detail",WarningCount=2,MetaData=new(){["marker"]="owned"}}
                :new OASISResult<IAvatar>{Result=new Avatar{Id=Owner,Version=2},IsLoaded=true,LoadedCount=1});
        }
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email,int version=0)=>Resolve(version);
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username,int version=0)=>Resolve(version);
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid owner,int version=0)
        {
            ExportCalls++;ExportVersion=version;ExportOwner=owner;
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=new[]{new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner,Version=version}},IsLoaded=true,LoadedCount=1});
        }
    }
}

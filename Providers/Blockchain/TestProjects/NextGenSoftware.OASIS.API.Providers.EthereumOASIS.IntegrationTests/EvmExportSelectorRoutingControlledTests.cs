using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

// No RPC/signing: checks production selector dispatch and exact identity-error forwarding.
[TestClass]
public sealed class EvmExportSelectorRoutingControlledTests
{
    private sealed class Observation
    {
        internal int Calls,Version=int.MinValue;
        internal readonly IOException Cause=new("owned identity lookup failure");
        internal Task<OASISResult<IAvatar>> Read(int version){
            Calls++;Version=version;
            return Task.FromResult(new OASISResult<IAvatar>{IsError=true,Exception=Cause,Message="owned identity",DetailedMessage="owned diagnostics",ErrorCode="owned-identity-rejection",IsWarning=true,WarningCount=3,ErrorCount=2,MetaData=new(){{"owned-evidence","identity-stage"}}});
        }
    }
    private interface IProbe {IOASISStorageProvider Storage{get;} Observation State{get;}}
    private sealed class EthereumProbe: EthereumOASIS,IProbe
    {
        public Observation State{get;}=new();public IOASISStorageProvider Storage=>this;
        internal EthereumProbe():base("http://127.0.0.1:1","unused",31337,"0x0000000000000000000000000000000000000001"){IsProviderActivated=true;}
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string selector,int version=0)=>State.Read(version);
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string selector,int version=0)=>State.Read(version);
    }
    private sealed class Web3Probe: Web3CoreOASISBaseProvider,IProbe
    {
        public Observation State{get;}=new();public IOASISStorageProvider Storage=>this;
        internal Web3Probe():base("http://127.0.0.1:1","unused","0x0000000000000000000000000000000000000001",31337){}
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string selector,int version=0)=>State.Read(version);
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string selector,int version=0)=>State.Read(version);
    }
    public static IEnumerable<object[]> Cases(){foreach(var ethereum in new[]{false,true})foreach(var sync in new[]{false,true})foreach(var email in new[]{false,true})yield return new object[]{ethereum,sync,email};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task ExportSelectsCurrentIdentityAndPreservesLookupFailure(bool ethereum,bool sync,bool email)
    {
        IProbe probe=ethereum?new EthereumProbe():new Web3Probe();var provider=probe.Storage;
        var result=email
            ?sync?provider.ExportAllDataForAvatarByEmail("owned@example.invalid",3):await provider.ExportAllDataForAvatarByEmailAsync("owned@example.invalid",3)
            :sync?provider.ExportAllDataForAvatarByUsername("owned",3):await provider.ExportAllDataForAvatarByUsernameAsync("owned",3);
        Assert.AreEqual(1,probe.State.Calls);Assert.AreEqual(0,probe.State.Version,"Holon export version must not select avatar history.");
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreSame(probe.State.Cause,result.Exception);
        Assert.AreEqual("owned-identity-rejection",result.ErrorCode);Assert.AreEqual("owned diagnostics",result.DetailedMessage);
        Assert.IsTrue(result.IsWarning);Assert.AreEqual(3,result.WarningCount);Assert.AreEqual(2,result.ErrorCount);Assert.AreEqual("identity-stage",result.MetaData["owned-evidence"]);
        Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
    }
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task NegativeVersionRejectsBeforeIdentityOrRpcDispatch(bool ethereum,bool sync,bool email)
    {
        IProbe probe=ethereum?new EthereumProbe():new Web3Probe();var provider=probe.Storage;
        var result=email
            ?sync?provider.ExportAllDataForAvatarByEmail("owned@example.invalid",-1):await provider.ExportAllDataForAvatarByEmailAsync("owned@example.invalid",-1)
            :sync?provider.ExportAllDataForAvatarByUsername("owned",-1):await provider.ExportAllDataForAvatarByUsernameAsync("owned",-1);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
        Assert.AreEqual(0,probe.State.Calls);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
    }
}

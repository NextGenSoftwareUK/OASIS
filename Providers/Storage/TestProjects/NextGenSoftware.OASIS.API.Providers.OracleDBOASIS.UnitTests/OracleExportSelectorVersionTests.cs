using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public class OracleExportSelectorVersionTests
{
    // Dispatch-only control: actual managed Oracle I/O is covered by native acceptance.
    private sealed class Probe : OracleDBOASIS
    {
        internal int IdentityCalls, ExportCalls, ExportVersion;
        internal readonly Guid Owner=Guid.NewGuid();
        internal Probe():base("User Id=unused;Data Source=unused"){}
        private Task<OASISResult<IAvatar>> Identity(int version)
        {
            IdentityCalls++;
            if(version!=0)return Task.FromResult(new OASISResult<IAvatar>{IsError=true,Message="Independent identity version absent"});
            return Task.FromResult(new OASISResult<IAvatar>{Result=new Avatar{Id=Owner,Version=1},IsLoaded=true,LoadedCount=1});
        }
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string name,int version=0)=>Identity(version);
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email,int version=0)=>Identity(version);
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id,int version=0)
        {
            Assert.AreEqual(Owner,id);ExportCalls++;ExportVersion=version;
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=new[]{new Holon{Id=Guid.NewGuid(),Version=version}},IsLoaded=true,LoadedCount=1});
        }
    }

    [DataTestMethod][DataRow(false,false,3)][DataRow(false,true,3)][DataRow(true,false,3)][DataRow(true,true,3)]
    [DataRow(false,false,-1)][DataRow(false,true,-1)][DataRow(true,false,-1)][DataRow(true,true,-1)]
    public async Task ExportVersionDoesNotSelectIdentityHistory(bool sync,bool email,int version)
    {
        var provider=new Probe();
        var result=email
            ?sync?provider.ExportAllDataForAvatarByEmail("owner@example.invalid",version):await provider.ExportAllDataForAvatarByEmailAsync("owner@example.invalid",version)
            :sync?provider.ExportAllDataForAvatarByUsername("owner",version):await provider.ExportAllDataForAvatarByUsernameAsync("owner",version);
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);
        Assert.AreEqual(1,provider.IdentityCalls);Assert.AreEqual(1,provider.ExportCalls);Assert.AreEqual(version,provider.ExportVersion);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task InvalidVersionRejectsBeforeIdentityDispatch(bool sync,bool email)
    {
        var provider=new Probe();
        var result=email
            ?sync?provider.ExportAllDataForAvatarByEmail("owner@example.invalid",-2):await provider.ExportAllDataForAvatarByEmailAsync("owner@example.invalid",-2)
            :sync?provider.ExportAllDataForAvatarByUsername("owner",-2):await provider.ExportAllDataForAvatarByUsernameAsync("owner",-2);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
        Assert.AreEqual(0,provider.IdentityCalls);Assert.AreEqual(0,provider.ExportCalls);
    }
}

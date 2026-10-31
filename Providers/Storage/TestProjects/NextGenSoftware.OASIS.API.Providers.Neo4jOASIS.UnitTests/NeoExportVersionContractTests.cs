using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class NeoExportVersionContractTests
{
    private sealed class Probe:Provider
    {
        public Probe():base("bolt://127.0.0.1:17688","neo4j","owned",()=>Neo4jLookupOutcomeTests.ReadProbe.Make<IDriver>(0)){}
        public readonly Guid Owner=Guid.NewGuid();public int Selectors,IdentityVersion=-99,ExportVersion=-99;
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string name,int version=0)
        {Selectors++;IdentityVersion=version;return Task.FromResult(new OASISResult<IAvatar>(new Avatar{Id=Owner}){IsLoaded=true,LoadedCount=1});}
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email,int version=0)=>LoadAvatarByUsernameAsync(email,version);
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id,int version=0)
        {Assert.AreEqual(Owner,id);ExportVersion=version;return Task.FromResult(new OASISResult<IEnumerable<IHolon>>(Array.Empty<IHolon>()){IsLoaded=true});}
    }
    public static IEnumerable<object[]> Cases(){foreach(var version in new[]{-2,-1,0,3})foreach(var sync in new[]{false,true})foreach(var email in new[]{false,true})yield return new object[]{version,sync,email};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task NamedExportVersionSelectsHolonRootsNotAvatarHistory(int version,bool sync,bool email)
    {
        var provider=new Probe();
        try{
            var result=email?sync?provider.ExportAllDataForAvatarByEmail("owned@example.invalid",version):await provider.ExportAllDataForAvatarByEmailAsync("owned@example.invalid",version)
                :sync?provider.ExportAllDataForAvatarByUsername("owned",version):await provider.ExportAllDataForAvatarByUsernameAsync("owned",version);
            Assert.AreEqual(version < -1,result.IsError,result.Exception?.ToString());
            if(version < -1){Assert.AreEqual(0,provider.Selectors);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);}
            else{Assert.AreEqual(1,provider.Selectors);Assert.AreEqual(0,provider.IdentityVersion);Assert.AreEqual(version,provider.ExportVersion);}
        }finally{await provider.DeActivateProviderAsync();}
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task AllVersionExportSupportsAcknowledgedEmptyStore(bool sync)
    {
        var provider=new Provider("bolt://127.0.0.1:17688","neo4j","owned",()=>Neo4jLookupOutcomeTests.ReadProbe.Make<IDriver>(0)){IsProviderActivated=true};
        try{var result=sync?provider.ExportAll(-1):await provider.ExportAllAsync(-1);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(0,result.Result.Count());Assert.AreEqual(0,result.LoadedCount);}
        finally{provider.IsProviderActivated=false;}
    }
}

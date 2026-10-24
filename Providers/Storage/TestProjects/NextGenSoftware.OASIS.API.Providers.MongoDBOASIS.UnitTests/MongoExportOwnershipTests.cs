using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoExportOwnershipTests
{
    private sealed class Probe:MongoDBOASIS
    {
        internal readonly Guid Owner=Guid.NewGuid(),Own=Guid.NewGuid(),Foreign=Guid.NewGuid();
        internal int Reads,IdentityReads,SelectedVersion;
        internal Probe():base("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","owned-export-controls"){IsProviderActivated=true;}
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        {
            Reads++;SelectedVersion=version;
            IEnumerable<IHolon> rows=new IHolon[]{new NextGenSoftware.OASIS.API.Core.Holons.Holon{Id=Own,CreatedByAvatarId=Owner,Version=3},new NextGenSoftware.OASIS.API.Core.Holons.Holon{Id=Foreign,CreatedByAvatarId=Guid.NewGuid(),Version=3}};
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=rows,IsLoaded=true,LoadedCount=2});
        }
        private Task<OASISResult<IAvatar>> Identity(int version){IdentityReads++;Assert.AreEqual(0,version);return Task.FromResult(new OASISResult<IAvatar>{Result=new NextGenSoftware.OASIS.API.Core.Holons.Avatar{Id=Owner,Version=1},IsLoaded=true,LoadedCount=1});}
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string name,int version=0)=>Identity(version);
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email,int version=0)=>Identity(version);
    }

    public static IEnumerable<object[]> Cases(){foreach(var selector in new[]{0,1,2,3})foreach(var sync in new[]{false,true})foreach(var version in new[]{0,3})yield return new object[]{selector,sync,version};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task ExportScopesOwnerAndForwardsRootVersion(int selector,bool sync,int version)
    {
        var provider=new Probe();
        var result=selector switch{
            0=>sync?provider.ExportAll(version):await provider.ExportAllAsync(version),
            1=>sync?provider.ExportAllDataForAvatarById(provider.Owner,version):await provider.ExportAllDataForAvatarByIdAsync(provider.Owner,version),
            2=>sync?provider.ExportAllDataForAvatarByUsername("owned",version):await provider.ExportAllDataForAvatarByUsernameAsync("owned",version),
            _=>sync?provider.ExportAllDataForAvatarByEmail("owned@example.invalid",version):await provider.ExportAllDataForAvatarByEmailAsync("owned@example.invalid",version)
        };
        Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(selector==0?2:1,result.LoadedCount);
        if(selector!=0)Assert.IsTrue(result.Result.All(row=>row.Id==provider.Own&&row.CreatedByAvatarId==provider.Owner));
        Assert.AreEqual(1,provider.Reads);Assert.AreEqual(version,provider.SelectedVersion);Assert.AreEqual(selector>=2?1:0,provider.IdentityReads);
    }
}

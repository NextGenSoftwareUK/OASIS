using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public class OracleMetadataSnapshotTests
{
    private sealed class Probe(Holon[] rows,Action change):OracleDBOASIS("Unused Keyword=never connect")
    {
        public int Reads;
        protected override Task<List<T>> ReadRecordCollectionAsync<T>(string table,int version,bool exportAllVersions=false)
        {Assert.AreEqual("OASIS_HOLONS",table);Assert.IsFalse(exportAllVersions);Reads++;change();return Task.FromResult(rows.Cast<T>().ToList());}
    }
    public static IEnumerable<object[]> Cases(){foreach(var mutation in Enumerable.Range(0,4))foreach(var sync in new[]{false,true})foreach(var version in new[]{0,1})foreach(var mode in new[]{MetaKeyValuePairMatchMode.All,MetaKeyValuePairMatchMode.Any})yield return new object[]{mutation,sync,version,mode};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task MetadataSelectionUsesOriginalPairsAcrossTheNativeRead(int mutation,bool sync,int version,MetaKeyValuePairMatchMode mode)
    {
        var expected=new Holon{Id=Guid.NewGuid(),Name="first",MetaData=new(){{"zone","park"},{"region","north"}}};
        var other=new Holon{Id=Guid.NewGuid(),Name="other",MetaData=new(){{"zone","city"},{"region","south"}}};
        var pairs=new Dictionary<string,string>{{"zone","park"},{"region","north"}};
        void Change(){switch(mutation){case 0:pairs["zone"]="city";pairs["region"]="south";break;case 1:pairs.Clear();break;case 2:pairs.Remove("zone");pairs["region"]="south";break;default:pairs.Clear();pairs["absent"]="other";break;}}
        var provider=new Probe([expected,other],Change);
        var result=sync?provider.LoadHolonsByMetaData(pairs,mode,loadChildren:false,version:version):await provider.LoadHolonsByMetaDataAsync(pairs,mode,loadChildren:false,version:version);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);Assert.AreEqual(expected.Id,result.Result.Single().Id);Assert.AreEqual(1,provider.Reads);
    }
}

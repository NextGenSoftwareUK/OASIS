using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public class NearMetadataParityTests
{
    private sealed class StoredRows : NEAROASIS
    {
        public int Reads;
        public readonly Holon Row=new() {Id=Guid.NewGuid(),MetaData=new() {{"enabled",true},{"amount",1.5m},{"marker","owned"}}};
        public StoredRows():base("http://127.0.0.1:1","unused","unused","unused") { }
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType,bool loadChildren=true,bool recursive=true,
            int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        {Reads++;return Task.FromResult(new OASISResult<IEnumerable<IHolon>>(new[] {Row}));}
    }

    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task PublicMetadataUsesCanonicalBooleanAndInvariantNumber(bool synchronous)
    {
        var culture=CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");
            var provider=new StoredRows();
            foreach(var pair in new[] {new KeyValuePair<string,string>("amount","1.5"),new("enabled","True")}) {
                var found=synchronous ? provider.LoadHolonsByMetaData(pair.Key,pair.Value,HolonType.All)
                    :await provider.LoadHolonsByMetaDataAsync(pair.Key,pair.Value,HolonType.All);
                Assert.IsFalse(found.IsError,found.Message);Assert.IsTrue(found.IsLoaded);Assert.AreEqual(provider.Row.Id,found.Result.Single().Id);
            }
            Assert.IsFalse((await provider.LoadHolonsByMetaDataAsync("enabled","true",HolonType.All)).Result.Any(),"Preserve shared case-sensitive boolean text, not a new JSON coercion policy.");
            var all=await provider.LoadHolonsByMetaDataAsync(new() {{"enabled","True"},{"amount","1.5"}},MetaKeyValuePairMatchMode.All,HolonType.All);
            Assert.IsFalse(all.IsError,all.Message);Assert.AreEqual(provider.Row.Id,all.Result.Single().Id);
            var any=provider.LoadHolonsByMetaData(new() {{"missing","missing"},{"enabled","True"}},MetaKeyValuePairMatchMode.Any,HolonType.All);
            Assert.IsFalse(any.IsError,any.Message);Assert.AreEqual(provider.Row.Id,any.Result.Single().Id);
        }
        finally {CultureInfo.CurrentCulture=culture;}
    }

    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task InvalidMetadataModeAndKeysFailBeforeStorageRead(bool synchronous)
    {
        var provider=new StoredRows();
        foreach(var pairs in new[] {new Dictionary<string,string>{{"marker","owned"}},new Dictionary<string,string>{{" ","owned"}}}) {
            var mode=pairs.ContainsKey("marker")?(MetaKeyValuePairMatchMode)999:MetaKeyValuePairMatchMode.All;
            var invalid=synchronous ? provider.LoadHolonsByMetaData(pairs,mode,HolonType.All)
                :await provider.LoadHolonsByMetaDataAsync(pairs,mode,HolonType.All);
            Assert.IsTrue(invalid.IsError);Assert.IsNull(invalid.Result);
            Assert.IsInstanceOfType(invalid.Exception,typeof(ArgumentException));
        }
        Assert.AreEqual(0,provider.Reads);
    }
}

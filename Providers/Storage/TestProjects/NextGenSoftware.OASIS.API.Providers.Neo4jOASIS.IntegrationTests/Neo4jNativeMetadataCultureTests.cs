using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod][DataRow("de-DE")][DataRow("fr-FR")]
    public async Task NativeMetadataIndexesUseInvariantNumericValues(string culture)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var prior=CultureInfo.CurrentCulture;var provider=new Provider(uri,"neo4j","oasis-local-test-only");
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var root=new Holon{Id=Guid.NewGuid(),Name="owned invariant metadata"};root.MetaData["amount"]=2.5m;root.MetaData["ratio"]=1.25d;
        try
        {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);
            Assert.IsFalse((await provider.SaveHolonAsync(root,saveChildren:false)).IsError);
            var byValue=await provider.LoadHolonsByMetaDataAsync("amount","2.5",loadChildren:false);
            var synchronous=provider.LoadHolonsByMetaData("amount","2.5",loadChildren:false);
            var byPairs=await provider.LoadHolonsByMetaDataAsync(new Dictionary<string,string>{{"amount","2.5"},{"ratio","1.25"}},MetaKeyValuePairMatchMode.All,loadChildren:false);
            using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var check=raw.AsyncSession();
            var inspected=await(await check.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS json,h.MetaLookupTokens AS tokens",new{id=root.Id.ToString()})).SingleAsync();
            Assert.IsTrue(inspected["json"].As<string>().Contains(root.Id.ToString(),StringComparison.Ordinal));
            var tokens=inspected["tokens"].As<List<string>>();
            await(await check.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h",new{id=root.Id.ToString()})).ConsumeAsync();
            foreach(var result in new[]{byValue,synchronous,byPairs})
            {Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(root.Id,result.Result.Single().Id);}
            CollectionAssert.Contains(tokens,"[\"amount\",\"2.5\"]");CollectionAssert.Contains(tokens,"[\"ratio\",\"1.25\"]");
        }
        finally{CultureInfo.CurrentCulture=prior;await provider.DeActivateProviderAsync();}
    }
}

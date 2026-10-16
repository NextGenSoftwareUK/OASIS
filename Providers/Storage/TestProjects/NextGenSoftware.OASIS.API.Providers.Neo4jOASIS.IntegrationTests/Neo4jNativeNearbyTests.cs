using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeNearbyUsesCanonicalCoordinatesAndPreservesCompleteRecords(bool avatars)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var session=raw.AsyncSession();
        Assert.AreEqual(0L,(await (await session.RunAsync("MATCH (n) WHERE n:Avatar OR n:Holon RETURN count(n) AS count")).SingleAsync())["count"].As<long>());
        var provider=new Provider(uri,"neo4j","oasis-local-test-only");
        var records=new List<IHolonBase>();var verified=false;
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            for(var i=0;i<55;i++)
            {
                IHolonBase record=avatars ? new Avatar {Username="nearby-"+Guid.NewGuid()}:new Holon();
                record.Id=Guid.NewGuid();record.Name="nearby "+i;record.Description="full canonical record";
                record.HolonType=avatars ? HolonType.Avatar:i==0 ? HolonType.Park:HolonType.Holon;
                record.IsActive=i!=54;
                record.MetaData=new Dictionary<string,object> {{"proof","retained"}};
                if(i!=53)
                {
                    record.MetaData["Latitude"]=i==51 ? "0.0005":i==52 ? "0.002":"0";
                    record.MetaData["Longitude"]="0";
                }
                records.Add(record);
                var saved=avatars ? (await provider.SaveAvatarAsync((IAvatar)record)).IsError:(await provider.SaveHolonAsync((IHolon)record,saveChildren:false)).IsError;
                Assert.IsFalse(saved);
            }
            var prior=System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("fr-FR");
                var results=avatars ? provider.GetAvatarsNearMe(0,0,100).Result.Cast<IHolonBase>().ToArray():provider.GetHolonsNearMe(0,0,100).Result.Cast<IHolonBase>().ToArray();
                Assert.AreEqual(52,results.Length,"No arbitrary fifty-record cap; exclude distant, absent-location and inactive records.");
                CollectionAssert.AreEquivalent(records.Take(52).Select(r=>r.Id).ToArray(),results.Select(r=>r.Id).ToArray());
                foreach(var result in results)
                {
                    Assert.AreEqual("full canonical record",result.Description);
                    Assert.AreEqual("retained",result.MetaData["proof"].ToString());
                    Assert.AreEqual(records.Single(r=>r.Id==result.Id).Version,result.Version);
                }
                if(avatars) Assert.AreEqual(51,provider.GetAvatarsNearMe(0,0,0).Result.Count());
                else
                {
                    Assert.AreEqual(51,provider.GetHolonsNearMe(0,0,0).Result.Count());
                    var parks=provider.GetHolonsNearMe(0,0,100,HolonType.Park);
                    Assert.IsFalse(parks.IsError,parks.Message);Assert.IsTrue(parks.IsLoaded);
                    Assert.AreEqual(records[0].Id,parks.Result.Single().Id);
                }
            }
            finally {System.Globalization.CultureInfo.CurrentCulture=prior;}
            verified=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if(verified) await (await session.RunAsync("MATCH (n) WHERE (n:Avatar OR n:Holon) AND n.Id IN $ids DETACH DELETE n",new {ids=records.Select(r=>r.Id.ToString()).ToArray()})).ConsumeAsync();
        }
    }
}

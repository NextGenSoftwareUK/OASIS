using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private static readonly Guid GuidA=Guid.Parse("abcdefab-1234-5678-9abc-abcdefabcdef");
    private static readonly Guid GuidB=Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly string[] InvalidGuidKeys={"uuid-compact","uuid-braces","uuid-whitespace","uuid-nul","uuid-newline","uuid-nonhex","uuid-array","uuid-boolean","uuid-object","uuid-number","uuid-unicode"};
    private static void AddGuidFixtures(Holon[] records)
    {
        object?[] values={GuidA.ToString("D").ToUpperInvariant(),GuidB.ToString("D"),null,"not-a-guid",42};
        object?[] peers={GuidA.ToString("D"),GuidA.ToString("D").ToUpperInvariant(),null,"not-a-guid",42};
        object[] invalid={GuidA.ToString("N"),GuidA.ToString("B"),GuidA.ToString("D")+" ",GuidA.ToString("D")+"\0",GuidA.ToString("D")+"\n",
            "g"+GuidA.ToString("D").Substring(1),new[]{GuidA.ToString("D")},true,new Dictionary<string,object>{{"value",GuidA.ToString("D")}},42,GuidA.ToString("D").Replace("-","\u2010")};
        for(var i=0;i<records.Length;i++)
        {
            records[i].MetaData["uuid"]=values[i]!;records[i].MetaData["uuid-peer"]=peers[i]!;
            records[i].MetaData["uuid-owner"]=records[i].Id.ToString("D").ToUpperInvariant();
            for(var j=0;j<InvalidGuidKeys.Length;j++)records[i].MetaData[InvalidGuidKeys[j]]=invalid[j];
        }
        records[0].MetaData["uuid-zero"]=Guid.Empty;
    }
    private static async Task VerifyGuidDomains(Provider provider,Holon[] records)
    {
        async Task Matches(Expression<Func<Holon,bool>> predicate,int[] expected)
        {
            var built=CosmicQuery<Holon>.Create().Where(predicate);Assert.IsFalse(built.IsError,built.Message);
            await Check(built.Result,expected,false);
        }
        async Task Check(CosmicQuery<Holon> query,int[] expected,bool ordered)
        {
            var plan=CosmicQueryPlanner.Plan(query,ProviderType.Neo4jOASIS,new[]{new CosmicQueryTarget(ProviderType.Neo4jOASIS,Provider.CqlCapabilities)});
            Assert.IsFalse(plan.IsError,plan.Message);
            var result=await provider.ExecuteCosmicQueryAsync(plan.Result);Assert.IsFalse(result.IsError,result.Message+result.Exception);
            var actual=result.Result.Select(row=>row.Id).ToArray();var ids=expected.Select(i=>records[i].Id).ToArray();
            if(ordered)CollectionAssert.AreEqual(ids,actual);else CollectionAssert.AreEquivalent(ids,actual);
        }
        var target=GuidA;var lower=GuidA.ToString("D");var upper=lower.ToUpperInvariant();
        await Matches(h=>CosmicQueryMetadata.Value<Guid>(h,"uuid")==target,new[]{0});
        await Matches(h=>target==CosmicQueryMetadata.Value<Guid>(h,"uuid"),new[]{0});
        await Matches(h=>CosmicQueryMetadata.Value<Guid>(h,"uuid")!=target,new[]{1});
        await Matches(h=>CosmicQueryMetadata.Value<Guid?>(h,"uuid")!=target,new[]{1,2});
        await Matches(h=>CosmicQueryMetadata.Value<Guid?>(h,"uuid")==null,new[]{2});
        await Matches(h=>!(CosmicQueryMetadata.Value<Guid>(h,"uuid")==target),new[]{1,2,3,4});
        var members=new Guid?[]{target,null};
        await Matches(h=>members.Contains(CosmicQueryMetadata.Value<Guid?>(h,"uuid")),new[]{0,2});
        await Matches(h=>CosmicQueryMetadata.Value<Guid>(h,"uuid")==CosmicQueryMetadata.Value<Guid>(h,"uuid-peer"),new[]{0});
        await Matches(h=>CosmicQueryMetadata.Value<Guid?>(h,"uuid")==CosmicQueryMetadata.Value<Guid?>(h,"uuid-peer"),new[]{0,2});
        await Matches(h=>CosmicQueryMetadata.Value<Guid?>(h,"uuid")!=CosmicQueryMetadata.Value<Guid?>(h,"uuid-peer"),new[]{1});
        await Matches(h=>h.Id==CosmicQueryMetadata.Value<Guid>(h,"uuid-owner"),new[]{0,1,2,3,4});
        await Matches(h=>CosmicQueryMetadata.Value<Guid>(h,"uuid-zero")==Guid.Empty,new[]{0});
        await Matches(h=>CosmicQueryMetadata.Value<string>(h,"uuid")==upper,new[]{0});
        await Matches(h=>CosmicQueryMetadata.Value<string>(h,"uuid")==lower,Array.Empty<int>());
        foreach(var key in InvalidGuidKeys.Concat(new[]{"uuid-missing"}))
            await Matches(h=>CosmicQueryMetadata.Value<Guid>(h,key)!=target,Array.Empty<int>());
        var invalid=new[]{3,4}.OrderBy(i=>records[i].Id.ToString("D"),StringComparer.Ordinal).ToArray();
        var ascending=CosmicQuery<Holon>.Create().OrderBy(h=>CosmicQueryMetadata.Value<Guid?>(h,"uuid"));
        Assert.IsFalse(ascending.IsError,ascending.Message);await Check(ascending.Result,new[]{2,1,0}.Concat(invalid).ToArray(),true);
        var descending=CosmicQuery<Holon>.Create().OrderByDescending(h=>CosmicQueryMetadata.Value<Guid?>(h,"uuid"));
        Assert.IsFalse(descending.IsError,descending.Message);await Check(descending.Result,invalid.Concat(new[]{0,1,2}).ToArray(),true);
    }
}

using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private static readonly (string Key,long Minimum,long Maximum)[] NarrowDomains={
        ("narrow-byte",0,255),("narrow-sbyte",-128,127),("narrow-short",-32768,32767),
        ("narrow-ushort",0,65535),("narrow-uint",0,4294967295)};

    private static void AddNarrowIntegerFixtures(Holon[] records)
    {
        foreach(var domain in NarrowDomains)
        {
            records[0].MetaData[domain.Key]=domain.Minimum;
            records[1].MetaData[domain.Key]=domain.Maximum;
            records[2].MetaData[domain.Key]=null!;
            records[3].MetaData[domain.Key]=domain.Minimum-1;
            records[4].MetaData[domain.Key]=domain.Maximum+1;
        }
    }

    private static async Task VerifyNarrowIntegerDomains(Provider provider,Holon[] records)
    {
        await VerifyNarrow<byte>(provider,records,NarrowDomains[0]);
        await VerifyNarrow<sbyte>(provider,records,NarrowDomains[1]);
        await VerifyNarrow<short>(provider,records,NarrowDomains[2]);
        await VerifyNarrow<ushort>(provider,records,NarrowDomains[3]);
        await VerifyNarrow<uint>(provider,records,NarrowDomains[4]);
    }

    private static async Task VerifyNarrow<T>(Provider provider,Holon[] records,(string Key,long Minimum,long Maximum) domain) where T:struct
    {
        var key=domain.Key;
        var owner=Expression.Parameter(typeof(Holon),"h");
        var marker=typeof(CosmicQueryMetadata).GetMethod(nameof(CosmicQueryMetadata.Value))!;
        Expression Path(Type type)=>Expression.Call(marker.MakeGenericMethod(type),owner,
            Expression.NewArrayInit(typeof(string),Expression.Constant(domain.Key)));
        var minimum=(T)Convert.ChangeType(domain.Minimum,typeof(T),System.Globalization.CultureInfo.InvariantCulture);
        var maximum=(T)Convert.ChangeType(domain.Maximum,typeof(T),System.Globalization.CultureInfo.InvariantCulture);
        CosmicQuery<Holon> Query(Expression body)
        {
            var built=CosmicQuery<Holon>.Create().Where(Expression.Lambda<Func<Holon,bool>>(body,owner));
            Assert.IsFalse(built.IsError,built.Message);return built.Result;
        }
        async Task Matches(CosmicQuery<Holon> query,int[] expected,bool ordered=false)
        {
            var plan=CosmicQueryPlanner.Plan(query,ProviderType.Neo4jOASIS,
                new[]{new CosmicQueryTarget(ProviderType.Neo4jOASIS,Provider.CqlCapabilities)});
            Assert.IsFalse(plan.IsError,plan.Message);
            var loaded=await provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);
            var actual=loaded.Result.Select(row=>row.Id).ToArray();var ids=expected.Select(i=>records[i].Id).ToArray();
            if(ordered)CollectionAssert.AreEqual(ids,actual,domain.Key);
            else CollectionAssert.AreEquivalent(ids,actual,domain.Key);
        }
        await Matches(Query(Expression.Equal(Path(typeof(T)),Expression.Constant(minimum,typeof(T)))),new[]{0});
        await Matches(Query(Expression.Equal(Path(typeof(T)),Expression.Constant(maximum,typeof(T)))),new[]{1});
        await Matches(Query(Expression.NotEqual(Path(typeof(T)),Expression.Constant(minimum,typeof(T)))),new[]{1});
        var nullable=typeof(T?);
        var values=Expression.NewArrayInit(nullable,Expression.Convert(Expression.Constant(minimum,typeof(T)),nullable),Expression.Constant(null,nullable));
        var contains=Expression.Call(typeof(Enumerable),nameof(Enumerable.Contains),new[]{nullable},values,Path(nullable));
        await Matches(Query(contains),new[]{0,2});
        var fields=Expression.LessThan(Expression.Convert(Expression.Property(owner,nameof(Holon.Version)),typeof(long)),Expression.Convert(Path(typeof(T)),typeof(long)));
        await Matches(Query(fields),new[]{1});
        var order=CosmicQuery<Holon>.Create().OrderBy(h=>CosmicQueryMetadata.Value<T?>(h,key));
        Assert.IsFalse(order.IsError,order.Message);
        var invalid=new[]{3,4}.OrderBy(i=>records[i].Id.ToString("D"),StringComparer.Ordinal).ToArray();
        await Matches(order.Result,new[]{2,0,1}.Concat(invalid).ToArray(),true);
        var descending=CosmicQuery<Holon>.Create().OrderByDescending(h=>CosmicQueryMetadata.Value<T?>(h,key));
        Assert.IsFalse(descending.IsError,descending.Message);
        await Matches(descending.Result,invalid.Concat(new[]{1,0,2}).ToArray(),true);
    }
}

using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    private static readonly Type[] EnumDomains={typeof(HolonType),typeof(ProviderType)};
    private static object[] DefinedEnumValues(Type type)=>Enum.GetValues(type).Cast<object>().Distinct().OrderBy(Convert.ToInt64).ToArray();
    private static void AddEnumFixtures(Holon[] records)
    {
        foreach(var type in EnumDomains)
        {
            var key="enum-"+type.Name;var known=DefinedEnumValues(type);
            var undefined=Enumerable.Range(0,10000).Select(i=>(long)i).First(i=>!known.Any(value=>Convert.ToInt64(value)==i));
            object?[] values={known.First(),known.Last(),null,undefined,Convert.ToInt64(known.Last()).ToString(System.Globalization.CultureInfo.InvariantCulture)};
            for(var i=0;i<records.Length;i++)
            {
                records[i].MetaData[key]=values[i]!;records[i].MetaData[key+"-other"]=values[i]!;
                records[i].MetaData[key+"-upper"]=known.Last();
                records[i].MetaData[key+"-fraction"]=Convert.ToInt64(known.Last())+0.5m;
                records[i].MetaData[key+"-decimal"]=Convert.ToInt64(known.Last())+0.0m;
                records[i].MetaData[key+"-name"]=known.Last().ToString()!;
                records[i].MetaData[key+"-boolean"]=true;
                records[i].MetaData[key+"-array"]=new[]{Convert.ToInt64(known.Last())};
            }
        }
    }
    private static async Task VerifyEnumDomains(Provider provider,Holon[] records)
    {
        foreach(var type in EnumDomains)
        {
            var key="enum-"+type.Name;var known=DefinedEnumValues(type);var last=known.Last();
            var owner=Expression.Parameter(typeof(Holon),"h");
            Expression Path(Type declared,string name)=>Expression.Call(typeof(CosmicQueryMetadata),nameof(CosmicQueryMetadata.Value),new[]{declared},owner,
                Expression.NewArrayInit(typeof(string),Expression.Constant(name)));
            CosmicQuery<Holon> Query(Expression body)
            {
                var built=CosmicQuery<Holon>.Create().Where(Expression.Lambda<Func<Holon,bool>>(body,owner));
                Assert.IsFalse(built.IsError,built.Message);return built.Result;
            }
            async Task Matches(CosmicQuery<Holon> query,int[] expected,bool ordered=false)
            {
                var plan=CosmicQueryPlanner.Plan(query,ProviderType.Neo4jOASIS,new[]{new CosmicQueryTarget(ProviderType.Neo4jOASIS,Provider.CqlCapabilities)});
                Assert.IsFalse(plan.IsError,plan.Message);
                var result=await provider.ExecuteCosmicQueryAsync(plan.Result);Assert.IsFalse(result.IsError,result.Message+result.Exception);
                var ids=expected.Select(i=>records[i].Id).ToArray();var actual=result.Result.Select(row=>row.Id).ToArray();
                if(ordered)CollectionAssert.AreEqual(ids,actual,key);else CollectionAssert.AreEquivalent(ids,actual,key);
            }
            var underlying=Enum.GetUnderlyingType(type);var path=Path(type,key);
            foreach(var operation in new[]{ExpressionType.Equal,ExpressionType.NotEqual,ExpressionType.GreaterThan,ExpressionType.GreaterThanOrEqual,ExpressionType.LessThan,ExpressionType.LessThanOrEqual})
            {
                var body=Expression.MakeBinary(operation,Expression.Convert(path,underlying),Expression.Constant(Convert.ChangeType(last,underlying),underlying));
                var expected=operation switch{ExpressionType.Equal or ExpressionType.GreaterThanOrEqual=>new[]{1},
                    ExpressionType.NotEqual or ExpressionType.LessThan=>new[]{0},ExpressionType.LessThanOrEqual=>new[]{0,1},_=>Array.Empty<int>()};
                await Matches(Query(body),expected);
            }
            foreach(var suffix in new[]{"-fraction","-decimal","-name","-boolean","-array","-missing"})
                await Matches(Query(Expression.NotEqual(Path(type,key+suffix),Expression.Constant(last,type))),Array.Empty<int>());
            var nullable=typeof(Nullable<>).MakeGenericType(type);var members=Array.CreateInstance(nullable,2);members.SetValue(last,1);
            var contains=Expression.Call(typeof(Enumerable),nameof(Enumerable.Contains),new[]{nullable},Expression.Constant(members,nullable.MakeArrayType()),Path(nullable,key));
            await Matches(Query(contains),new[]{1,2});
            await Matches(Query(Expression.Equal(Path(nullable,key),Expression.Constant(null,nullable))),new[]{2});
            await Matches(Query(Expression.Equal(Path(nullable,key),Path(nullable,key+"-other"))),new[]{0,1,2});
            await Matches(Query(Expression.NotEqual(Path(nullable,key),Path(nullable,key+"-upper"))),new[]{0,2});
            await Matches(Query(Expression.LessThan(Expression.Convert(path,underlying),Expression.Convert(Path(type,key+"-upper"),underlying))),new[]{0});
            var fields=Expression.Equal(path,Path(type,key+"-other"));await Matches(Query(fields),new[]{0,1});
            var ordering=Query(fields).OrderByDescending(Expression.Lambda<Func<Holon,int>>(Expression.Convert(path,typeof(int)),owner));
            Assert.IsFalse(ordering.IsError,ordering.Message);await Matches(ordering.Result,new[]{1,0},true);
            var invalid=new[]{3,4}.OrderBy(i=>records[i].Id.ToString("D"),StringComparer.Ordinal).ToArray();
            var all=CosmicQuery<Holon>.Create().OrderBy(Expression.Lambda<Func<Holon,int>>(Expression.Convert(path,typeof(int)),owner));
            Assert.IsFalse(all.IsError,all.Message);await Matches(all.Result,new[]{2,0,1}.Concat(invalid).ToArray(),true);
            var reversed=CosmicQuery<Holon>.Create().OrderByDescending(Expression.Lambda<Func<Holon,int>>(Expression.Convert(path,typeof(int)),owner));
            Assert.IsFalse(reversed.IsError,reversed.Message);await Matches(reversed.Result,invalid.Concat(new[]{1,0,2}).ToArray(),true);
        }
    }
}

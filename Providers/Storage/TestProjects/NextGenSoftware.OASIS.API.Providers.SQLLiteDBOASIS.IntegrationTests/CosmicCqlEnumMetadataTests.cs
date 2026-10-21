using System.Linq.Expressions;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeCqlRegisteredEnumsAdmitOnlyDefinedIntegerValues(bool providerEnum)
    {
        var type=providerEnum?typeof(ProviderType):typeof(HolonType);
        var known=System.Enum.GetValues(type).Cast<object>().Distinct().OrderBy(Convert.ToInt64).ToArray();
        var first=known.First();var last=known.Last();
        var lower=Convert.ToInt64(first);var upper=Convert.ToInt64(last);
        var undefined=Enumerable.Range(0,10000).Select(i=>(long)i).First(i=>!known.Any(value=>Convert.ToInt64(value)==i));
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        object?[] values={null,null,first,last,undefined,upper.ToString(),true,1.5d,new[]{upper}};
        var ids=new List<Guid>();
        for(var index=0;index<values.Length;index++)
        {
            var row=new Holon{Id=Guid.NewGuid(),MetaData=index==0?new():new(){["enum"]=values[index]!,["other"]=values[index]!}};
            (await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();ids.Add(row.Id);
        }
        var owner=Expression.Parameter(typeof(Holon),"h");
        MethodCallExpression Path(Type declared,string key)=>Expression.Call(typeof(CosmicQueryMetadata),nameof(CosmicQueryMetadata.Value),new[]{declared},owner,
            Expression.NewArrayInit(typeof(string),Expression.Constant(key)));
        var path=Path(type,"enum");var underlying=System.Enum.GetUnderlyingType(type);
        foreach(var operation in new[]{ExpressionType.Equal,ExpressionType.NotEqual,ExpressionType.GreaterThan,ExpressionType.GreaterThanOrEqual,ExpressionType.LessThan,ExpressionType.LessThanOrEqual})
        {
            var predicate=Expression.Lambda<Func<Holon,bool>>(Expression.MakeBinary(operation,Expression.Convert(path,underlying),
                Expression.Constant(Convert.ChangeType(last,underlying),underlying)),owner);
            var result=await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
            result.IsError.Should().BeFalse(result.Message+result.Exception);
            var expected=operation switch{ExpressionType.Equal or ExpressionType.GreaterThanOrEqual=>new[]{ids[3]},
                ExpressionType.NotEqual or ExpressionType.LessThan=>new[]{ids[2]},ExpressionType.LessThanOrEqual=>new[]{ids[2],ids[3]},_=>Array.Empty<Guid>()};
            result.Result.Select(row=>row.Id).Should().BeEquivalentTo(expected);
        }
        var nullable=typeof(Nullable<>).MakeGenericType(type);var members=Array.CreateInstance(nullable,2);members.SetValue(last,1);
        var contains=Expression.Lambda<Func<Holon,bool>>(Expression.Call(typeof(Enumerable),nameof(Enumerable.Contains),new[]{nullable},
            Expression.Constant(members,nullable.MakeArrayType()),Path(nullable,"enum")),owner);
        var memberResult=await provider.ExecuteCosmicQueryAsync(CqlPlan(contains));
        memberResult.IsError.Should().BeFalse(memberResult.Message+memberResult.Exception);
        memberResult.Result.Select(row=>row.Id).Should().BeEquivalentTo(new[]{ids[1],ids[3]});
        var fields=Expression.Lambda<Func<Holon,bool>>(Expression.Equal(path,Path(type,"other")),owner);
        var fieldResult=await provider.ExecuteCosmicQueryAsync(CqlPlan(fields));
        fieldResult.IsError.Should().BeFalse(fieldResult.Message+fieldResult.Exception);
        fieldResult.Result.Select(row=>row.Id).Should().BeEquivalentTo(new[]{ids[2],ids[3]});
        var ordered=CosmicQuery<Holon>.Create().Where(fields)
            .OrderByDescending(Expression.Lambda<Func<Holon,int>>(Expression.Convert(path,typeof(int)),owner));
        ordered.IsError.Should().BeFalse(ordered.Message);
        var page=await provider.ExecuteCosmicQueryAsync(WindowPlan(ordered.Result));
        page.IsError.Should().BeFalse(page.Message+page.Exception);page.Result.Select(row=>row.Id).Should().Equal(ids[3],ids[2]);
    }
}

using System.Linq.Expressions;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData("byte")] [InlineData("sbyte")] [InlineData("short")] [InlineData("ushort")] [InlineData("uint")]
    public async Task NativeCqlNarrowIntegerDomainsHonorRangeAndRejectCoercion(string domain)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (long minimum,long maximum)=domain switch{
            "byte"=>((long)byte.MinValue,(long)byte.MaxValue),"sbyte"=>((long)sbyte.MinValue,(long)sbyte.MaxValue),
            "short"=>((long)short.MinValue,(long)short.MaxValue),"ushort"=>((long)ushort.MinValue,(long)ushort.MaxValue),
            _=>((long)uint.MinValue,(long)uint.MaxValue)};
        object?[] values={null,null,minimum,maximum,minimum-1,maximum+1,"1",true,1.5d,new[]{1},ulong.MaxValue};
        var ids=new List<Guid>();
        for(var index=0;index<values.Length;index++)
        {
            var row=new Holon{Id=Guid.NewGuid(),MetaData=index==0?new():new(){["integer"]=values[index]!,["other"]=values[index]!}};
            (await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();ids.Add(row.Id);
        }
        Expression<Func<Holon,bool>> predicate=domain switch{
            "byte"=>h=>CosmicQueryMetadata.Value<byte>(h,"integer")>=byte.MinValue,
            "sbyte"=>h=>CosmicQueryMetadata.Value<sbyte>(h,"integer")>=sbyte.MinValue,
            "short"=>h=>CosmicQueryMetadata.Value<short>(h,"integer")>=short.MinValue,
            "ushort"=>h=>CosmicQueryMetadata.Value<ushort>(h,"integer")>=ushort.MinValue,
            _=>h=>CosmicQueryMetadata.Value<uint>(h,"integer")>=uint.MinValue};
        var result=await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        result.Result.Select(row=>row.Id).Should().BeEquivalentTo(new[]{ids[2],ids[3]});
        foreach(var operation in new[]{ExpressionType.Equal,ExpressionType.NotEqual,ExpressionType.GreaterThan,ExpressionType.GreaterThanOrEqual,ExpressionType.LessThan,ExpressionType.LessThanOrEqual})
        {
            var type=domain switch{"byte"=>typeof(byte),"sbyte"=>typeof(sbyte),"short"=>typeof(short),"ushort"=>typeof(ushort),_=>typeof(uint)};
            var parameter=Expression.Parameter(typeof(Holon),"h");
            var marker=Expression.Call(typeof(CosmicQueryMetadata),nameof(CosmicQueryMetadata.Value),new[]{type},parameter,
                Expression.NewArrayInit(typeof(string),Expression.Constant("integer")));
            var comparison=Expression.MakeBinary(operation,Expression.Convert(marker,typeof(long)),Expression.Constant(maximum));
            var exact=Expression.Lambda<Func<Holon,bool>>(comparison,parameter);
            var compared=await provider.ExecuteCosmicQueryAsync(CqlPlan(exact));
            compared.IsError.Should().BeFalse(compared.Message+compared.Exception);
            var expected=operation switch{
                ExpressionType.Equal or ExpressionType.GreaterThanOrEqual=>new[]{ids[3]},
                ExpressionType.NotEqual or ExpressionType.LessThan=>new[]{ids[2]},
                ExpressionType.LessThanOrEqual=>new[]{ids[2],ids[3]},_=>Array.Empty<Guid>()};
            compared.Result.Select(row=>row.Id).Should().BeEquivalentTo(expected);
        }
        var orderedType=domain switch{"byte"=>typeof(byte),"sbyte"=>typeof(sbyte),"short"=>typeof(short),"ushort"=>typeof(ushort),_=>typeof(uint)};
        var owner=Expression.Parameter(typeof(Holon),"h");
        var first=Expression.Call(typeof(CosmicQueryMetadata),nameof(CosmicQueryMetadata.Value),new[]{orderedType},owner,
            Expression.NewArrayInit(typeof(string),Expression.Constant("integer")));
        var other=Expression.Call(typeof(CosmicQueryMetadata),nameof(CosmicQueryMetadata.Value),new[]{orderedType},owner,
            Expression.NewArrayInit(typeof(string),Expression.Constant("other")));
        var fields=Expression.Lambda<Func<Holon,bool>>(Expression.Equal(first,other),owner);
        var fieldResult=await provider.ExecuteCosmicQueryAsync(CqlPlan(fields));
        fieldResult.IsError.Should().BeFalse(fieldResult.Message+fieldResult.Exception);
        fieldResult.Result.Select(row=>row.Id).Should().BeEquivalentTo(new[]{ids[2],ids[3]});
        var selector=Expression.Lambda<Func<Holon,long>>(Expression.Convert(first,typeof(long)),owner);
        var ordered=CosmicQuery<Holon>.Create().Where(predicate).OrderByDescending(selector);
        ordered.IsError.Should().BeFalse(ordered.Message);
        var page=await provider.ExecuteCosmicQueryAsync(WindowPlan(ordered.Result));
        page.IsError.Should().BeFalse(page.Message+page.Exception);
        page.Result.Select(row=>row.Id).Should().Equal(ids[3],ids[2]);
    }

    [Theory]
    [InlineData("byte")] [InlineData("sbyte")] [InlineData("short")] [InlineData("ushort")] [InlineData("uint")]
    public async Task NativeCqlNarrowNullableIntegerMembershipIncludesOnlyExplicitNullAndAdmittedValues(string domain)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        object?[] values={null,null,1,2,"1",1.5d,true};var ids=new List<Guid>();
        for(var index=0;index<values.Length;index++)
        {
            var row=new Holon{Id=Guid.NewGuid(),MetaData=index==0?new():new(){["integer"]=values[index]!}};
            (await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();ids.Add(row.Id);
        }
        byte?[] bytes={null,1};sbyte?[] signed={null,1};short?[] shorts={null,1};ushort?[] unsigned={null,1};uint?[] uints={null,1};
        Expression<Func<Holon,bool>> predicate=domain switch{
            "byte"=>h=>bytes.Contains(CosmicQueryMetadata.Value<byte?>(h,"integer")),
            "sbyte"=>h=>signed.Contains(CosmicQueryMetadata.Value<sbyte?>(h,"integer")),
            "short"=>h=>shorts.Contains(CosmicQueryMetadata.Value<short?>(h,"integer")),
            "ushort"=>h=>unsigned.Contains(CosmicQueryMetadata.Value<ushort?>(h,"integer")),
            _=>h=>uints.Contains(CosmicQueryMetadata.Value<uint?>(h,"integer"))};
        var result=await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        result.Result.Select(row=>row.Id).Should().BeEquivalentTo(new[]{ids[1],ids[2]});
    }
}

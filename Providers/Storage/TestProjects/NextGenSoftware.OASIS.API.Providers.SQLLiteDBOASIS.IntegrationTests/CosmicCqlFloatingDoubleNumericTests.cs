using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData("zero")] [InlineData("unit")] [InlineData("negativeUnit")]
    [InlineData("maximum")] [InlineData("minimum")] [InlineData("fraction")]
    public async Task NativeFloatingDoubleScalarRetainsBoundaryWithoutHydration(string kind)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var value=kind switch { "unit"=>double.Epsilon,"negativeUnit"=>-double.Epsilon,
            "maximum"=>double.MaxValue,"minimum"=>double.MinValue,"fraction"=>0.1d,_=>0d };
        var ids=await SeedNumeric(provider,value);
        var source=CosmicQuery<ScalarMustNotConstruct>.Create().Where(h=>ids.Contains(h.Id)).Result;
        var sum=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")).Result));
        sum.IsError.Should().BeFalse(sum.Message+sum.Exception); sum.Result.Should().Be(value);
        var mean=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")).Result));
        mean.IsError.Should().BeFalse(mean.Message+mean.Exception); mean.Result.Should().Be(value);
    }

    [Fact]
    public async Task NativeFloatingDoubleCancellationPreservesSmallestQuantum()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,double.MaxValue,double.MaxValue,double.MinValue,double.MinValue,double.Epsilon);
        var source=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).Result;
        var sum=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")).Result));
        sum.IsError.Should().BeFalse(sum.Message+sum.Exception); sum.Result.Should().Be(double.Epsilon);
    }

    [Fact]
    public async Task NativeFloatingDoubleOverflowDoesNotInvalidateMean()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,double.MaxValue,double.MaxValue);
        var source=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).Result;
        var sum=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")).Result));
        sum.ErrorCode.Should().Be("CQL_SCALAR_OVERFLOW"); sum.IsLoaded.Should().BeFalse();
        var mean=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")).Result));
        mean.IsError.Should().BeFalse(mean.Message+mean.Exception); mean.Result.Should().Be(double.MaxValue);
    }

    [Fact]
    public async Task NativeFloatingDoubleNullableAndEmptyContractsUseTypedPackets()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,null!,1.25d);
        var source=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).Result;
        var mean=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Average(h=>CosmicQueryMetadata.Value<double?>(h,"value")).Result));
        mean.IsError.Should().BeFalse(mean.Message+mean.Exception); mean.Result.Should().Be(1.25d);
        var absent=Guid.NewGuid(); var empty=CosmicQuery<Holon>.Create().Where(h=>h.Id==absent).Result;
        var zero=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,empty.Sum(h=>CosmicQueryMetadata.Value<double?>(h,"value")).Result));
        zero.IsError.Should().BeFalse(zero.Message+zero.Exception); zero.Result.Should().Be(0d);
        var none=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,empty.Average(h=>CosmicQueryMetadata.Value<double?>(h,"value")).Result));
        none.IsError.Should().BeFalse(none.Message+none.Exception); none.Result.Should().BeNull();
        var required=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,empty.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")).Result));
        required.ErrorCode.Should().Be("CQL_EMPTY_SEQUENCE");
    }

    [Theory]
    [InlineData("text")] [InlineData("boolean")] [InlineData("null")]
    [InlineData("underflow")] [InlineData("overflow")]
    public async Task NativeFloatingDoubleRejectsInvalidParticipatingDomains(string kind)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        object value=kind switch { "boolean"=>true,"null"=>null!,
            "underflow"=>JsonDocument.Parse("1e-10000").RootElement.Clone(),
            "overflow"=>JsonDocument.Parse("1e10000").RootElement.Clone(),_=>"1.25" };
        var ids=await SeedNumeric(provider,value);
        var source=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).Result;
        var result=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")).Result));
        result.ErrorCode.Should().Be("CQL_DATA_INVALID"); result.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task NativeFloatingDoubleGroupsFilterOrderAndPagePublishedValues()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedGroups(provider,("small",0.5d),("small",1.5d),("large",3d),("large",5d));
        var query=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id))
            .GroupBy(h=>CosmicQueryMetadata.Value<string>(h,"category"))
            .Select(g=>new {g.Key,Total=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"stock")),Mean=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"stock"))})
            .Where(g=>g.Total>=2d && g.Mean>=1d).OrderByDescending(g=>g.Mean).Skip(1).Take(1).Result;
        var result=await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider,query),new[] {provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        var row=result.Result.Providers.Single().Rows.Single();
        row.Key.Should().Be("small"); row.Total.Should().Be(2d); row.Mean.Should().Be(1d);
    }

    [Fact]
    public async Task NativeFloatingDoubleGroupedOverflowCannotHideBehindTakeZero()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedGroups(provider,("one",double.MaxValue),("one",double.MaxValue));
        var query=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).GroupBy(h=>CosmicQueryMetadata.Value<string>(h,"category"))
            .Select(g=>g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"stock"))).Take(0).Result;
        var result=await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider,query),new[] {provider});
        result.ErrorCode.Should().Be("CQL_SCALAR_OVERFLOW"); result.Result.Should().BeNull();
    }

    [Fact]
    public async Task NativeFloatingDoubleNullableGroupsKeepZeroAndNullDistinct()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedGroups(provider,("empty",null),("full",double.MaxValue));
        var query=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).GroupBy(h=>CosmicQueryMetadata.Value<string>(h,"category"))
            .Select(g=>new {g.Key,Total=g.Sum(h=>CosmicQueryMetadata.Value<double?>(h,"stock")),Mean=g.Average(h=>CosmicQueryMetadata.Value<double?>(h,"stock"))})
            .Where(g=>g.Total==0d && g.Mean==null).Result;
        var result=await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider,query),new[] {provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        var row=result.Result.Providers.Single().Rows.Single(); row.Key.Should().Be("empty"); row.Total.Should().Be(0d); row.Mean.Should().BeNull();
    }

    [Fact]
    public async Task NativeFloatingDoubleGroupedSumMatchesItsExactSourceKey()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,double.MaxValue);
        var query=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).GroupBy(h=>CosmicQueryMetadata.Value<double>(h,"value"))
            .Select(g=>new {g.Key,Total=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value"))}).Where(g=>g.Total==g.Key).Result;
        var result=await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider,query),new[] {provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception); result.Result.Providers.Single().Rows.Single().Total.Should().Be(double.MaxValue);
    }

    [Fact]
    public async Task NativeFloatingDoubleSourceWindowExcludesLaterMalformedValues()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,1.25d,"outside window");
        var source=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).OrderBy(h=>h.Name).Take(1).Result;
        var result=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")).Result));
        result.IsError.Should().BeFalse(result.Message+result.Exception); result.Result.Should().Be(1.25d);
    }

    [Fact]
    public async Task NativeFloatingDoubleInputWorkDoesNotInheritGraphPublicationLimit()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,Enumerable.Repeat((object)double.Epsilon,137).ToArray());
        var graph=CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot,maximumNodes:1).Result;
        var source=CosmicQuery<ScalarMustNotConstruct>.Create().WithChildren(graph).Where(h=>h.Name.StartsWith("owned numeric ",StringComparison.Ordinal)).Result;
        var result=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")).Result));
        result.IsError.Should().BeFalse(result.Message+result.Exception); result.Result.Should().Be(137d*double.Epsilon);
    }

    [Fact]
    public async Task NativeFloatingDoublePreCancellationDoesNotPoisonProviderReuse()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,1.25d,1.25d);
        var source=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).Result;
        var plan=ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")).Result);
        using var cancellation=new CancellationTokenSource(); cancellation.Cancel();
        (await provider.ExecuteCosmicScalarAsync(plan,cancellation.Token)).ErrorCode.Should().Be("CQL_QUERY_CANCELLED");
        var result=await provider.ExecuteCosmicScalarAsync(plan); result.IsError.Should().BeFalse(result.Message+result.Exception); result.Result.Should().Be(2.5d);
    }

    [Fact]
    public async Task NativeFloatingDoublePrivacyExcludesMalformedHiddenInputs()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actor=Guid.NewGuid();var foreign=Guid.NewGuid();var ids=new List<Guid>();
        for(var index=0;index<3;index++)
        {
            var row=new Holon {Id=Guid.NewGuid(),CreatedByAvatarId=index==0?actor:foreign,IsPublic=index==1,
                MetaData=new() { ["value"]=index==0?(object)1.25d:index==1?2.5d:"hidden invalid",["category"]="one" }};
            (await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();ids.Add(row.Id);
        }
        var keys=ids.ToArray();
        var source=CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Where(h=>keys.Contains(h.Id)).Result;
        var sum=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")).Result));
        sum.IsError.Should().BeFalse(sum.Message+sum.Exception);sum.Result.Should().Be(3.75d);
        var query=source.GroupBy(h=>CosmicQueryMetadata.Value<string>(h,"category"))
            .Select(g=>new {Count=g.Count(),Mean=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value"))}).Result;
        var result=await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider,query),new[] {provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception);var selected=result.Result.Providers.Single().Rows.Single();
        selected.Count.Should().Be(2);selected.Mean.Should().Be(1.875d);
    }

    [Fact]
    public async Task NativeFloatingDoubleGroupedMeanFiltersItsFinalRoundedValue()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedGroups(provider,("tiny",3d*double.Epsilon),("tiny",0d));
        var expected=2d*double.Epsilon;
        var query=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).GroupBy(h=>CosmicQueryMetadata.Value<string>(h,"category"))
            .Select(g=>new {Mean=g.Average(h=>CosmicQueryMetadata.Value<double>(h,"stock"))}).Where(g=>g.Mean==expected).OrderBy(g=>g.Mean).Take(1).Result;
        var result=await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider,query),new[] {provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception);result.Result.Providers.Single().Rows.Single().Mean.Should().Be(expected);
    }
}

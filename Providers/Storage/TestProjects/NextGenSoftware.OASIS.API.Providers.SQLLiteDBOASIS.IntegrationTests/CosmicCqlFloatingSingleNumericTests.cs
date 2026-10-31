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
    public async Task NativeFloatingSingleScalarRetainsBoundaryWithoutHydration(string kind)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var value=kind switch { "unit"=>float.Epsilon,"negativeUnit"=>-float.Epsilon,
            "maximum"=>float.MaxValue,"minimum"=>float.MinValue,"fraction"=>0.1f,_=>0f };
        var ids=await SeedNumeric(provider,value);
        var source=CosmicQuery<ScalarMustNotConstruct>.Create().Where(h=>ids.Contains(h.Id)).Result;
        var sum=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<float>(h,"value")).Result));
        sum.IsError.Should().BeFalse(sum.Message+sum.Exception); sum.Result.Should().Be(value);
        var mean=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Average(h=>CosmicQueryMetadata.Value<float>(h,"value")).Result));
        mean.IsError.Should().BeFalse(mean.Message+mean.Exception); mean.Result.Should().Be(value);
    }

    [Fact]
    public async Task NativeFloatingSingleCancellationPreservesSmallestQuantum()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,float.MaxValue,float.MaxValue,float.MinValue,float.MinValue,float.Epsilon);
        var source=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).Result;
        var sum=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<float>(h,"value")).Result));
        sum.IsError.Should().BeFalse(sum.Message+sum.Exception); sum.Result.Should().Be(float.Epsilon);
    }

    [Fact]
    public async Task NativeFloatingSingleOverflowDoesNotInvalidateMean()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,float.MaxValue,float.MaxValue);
        var source=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).Result;
        var sum=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<float>(h,"value")).Result));
        sum.ErrorCode.Should().Be("CQL_SCALAR_OVERFLOW"); sum.IsLoaded.Should().BeFalse();
        var mean=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Average(h=>CosmicQueryMetadata.Value<float>(h,"value")).Result));
        mean.IsError.Should().BeFalse(mean.Message+mean.Exception); mean.Result.Should().Be(float.MaxValue);
    }

    [Fact]
    public async Task NativeFloatingSingleNullableAndEmptyContractsUseTypedPackets()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,null!,1.25f);
        var source=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).Result;
        var mean=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Average(h=>CosmicQueryMetadata.Value<float?>(h,"value")).Result));
        mean.IsError.Should().BeFalse(mean.Message+mean.Exception); mean.Result.Should().Be(1.25f);
        var absent=Guid.NewGuid(); var empty=CosmicQuery<Holon>.Create().Where(h=>h.Id==absent).Result;
        var zero=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,empty.Sum(h=>CosmicQueryMetadata.Value<float?>(h,"value")).Result));
        zero.IsError.Should().BeFalse(zero.Message+zero.Exception); zero.Result.Should().Be(0f);
        var none=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,empty.Average(h=>CosmicQueryMetadata.Value<float?>(h,"value")).Result));
        none.IsError.Should().BeFalse(none.Message+none.Exception); none.Result.Should().BeNull();
        var required=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,empty.Average(h=>CosmicQueryMetadata.Value<float>(h,"value")).Result));
        required.ErrorCode.Should().Be("CQL_EMPTY_SEQUENCE");
    }

    [Theory]
    [InlineData("text")] [InlineData("boolean")] [InlineData("null")]
    [InlineData("underflow")] [InlineData("overflow")]
    public async Task NativeFloatingSingleRejectsInvalidParticipatingDomains(string kind)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        object value=kind switch { "boolean"=>true,"null"=>null!,
            "underflow"=>JsonDocument.Parse("1e-10000").RootElement.Clone(),
            "overflow"=>JsonDocument.Parse("1e10000").RootElement.Clone(),_=>"1.25" };
        var ids=await SeedNumeric(provider,value);
        var source=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).Result;
        var result=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<float>(h,"value")).Result));
        result.ErrorCode.Should().Be("CQL_DATA_INVALID"); result.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task NativeFloatingSingleGroupsFilterOrderAndPagePublishedValues()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedGroups(provider,("small",0.5f),("small",1.5f),("large",3f),("large",5f));
        var query=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id))
            .GroupBy(h=>CosmicQueryMetadata.Value<string>(h,"category"))
            .Select(g=>new {g.Key,Total=g.Sum(h=>CosmicQueryMetadata.Value<float>(h,"stock")),Mean=g.Average(h=>CosmicQueryMetadata.Value<float>(h,"stock"))})
            .Where(g=>g.Total>=2f && g.Mean>=1f).OrderByDescending(g=>g.Mean).Skip(1).Take(1).Result;
        var result=await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider,query),new[] {provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        var row=result.Result.Providers.Single().Rows.Single();
        row.Key.Should().Be("small"); row.Total.Should().Be(2f); row.Mean.Should().Be(1f);
    }

    [Fact]
    public async Task NativeFloatingSingleGroupedOverflowCannotHideBehindTakeZero()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedGroups(provider,("one",float.MaxValue),("one",float.MaxValue));
        var query=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).GroupBy(h=>CosmicQueryMetadata.Value<string>(h,"category"))
            .Select(g=>g.Sum(h=>CosmicQueryMetadata.Value<float>(h,"stock"))).Take(0).Result;
        var result=await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider,query),new[] {provider});
        result.ErrorCode.Should().Be("CQL_SCALAR_OVERFLOW"); result.Result.Should().BeNull();
    }

    [Fact]
    public async Task NativeFloatingSingleNullableGroupsKeepZeroAndNullDistinct()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedGroups(provider,("empty",null),("full",float.MaxValue));
        var query=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).GroupBy(h=>CosmicQueryMetadata.Value<string>(h,"category"))
            .Select(g=>new {g.Key,Total=g.Sum(h=>CosmicQueryMetadata.Value<float?>(h,"stock")),Mean=g.Average(h=>CosmicQueryMetadata.Value<float?>(h,"stock"))})
            .Where(g=>g.Total==0f && g.Mean==null).Result;
        var result=await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider,query),new[] {provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception);
        var row=result.Result.Providers.Single().Rows.Single(); row.Key.Should().Be("empty"); row.Total.Should().Be(0f); row.Mean.Should().BeNull();
    }

    [Fact]
    public async Task NativeFloatingSingleGroupedSumMatchesItsExactSourceKey()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,float.MaxValue);
        var query=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).GroupBy(h=>CosmicQueryMetadata.Value<float>(h,"value"))
            .Select(g=>new {g.Key,Total=g.Sum(h=>CosmicQueryMetadata.Value<float>(h,"value"))}).Where(g=>g.Total==g.Key).Result;
        var result=await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider,query),new[] {provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception); result.Result.Providers.Single().Rows.Single().Total.Should().Be(float.MaxValue);
    }

    [Fact]
    public async Task NativeFloatingSingleSourceWindowExcludesLaterMalformedValues()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,1.25f,"outside window");
        var source=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).OrderBy(h=>h.Name).Take(1).Result;
        var result=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<float>(h,"value")).Result));
        result.IsError.Should().BeFalse(result.Message+result.Exception); result.Result.Should().Be(1.25f);
    }

    [Fact]
    public async Task NativeFloatingSingleInputWorkDoesNotInheritGraphPublicationLimit()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,Enumerable.Repeat((object)float.Epsilon,137).ToArray());
        var graph=CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot,maximumNodes:1).Result;
        var source=CosmicQuery<ScalarMustNotConstruct>.Create().WithChildren(graph).Where(h=>h.Name.StartsWith("owned numeric ",StringComparison.Ordinal)).Result;
        var result=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<float>(h,"value")).Result));
        result.IsError.Should().BeFalse(result.Message+result.Exception); result.Result.Should().Be(137f*float.Epsilon);
    }

    [Fact]
    public async Task NativeFloatingSinglePreCancellationDoesNotPoisonProviderReuse()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedNumeric(provider,1.25f,1.25f);
        var source=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).Result;
        var plan=ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<float>(h,"value")).Result);
        using var cancellation=new CancellationTokenSource(); cancellation.Cancel();
        (await provider.ExecuteCosmicScalarAsync(plan,cancellation.Token)).ErrorCode.Should().Be("CQL_QUERY_CANCELLED");
        var result=await provider.ExecuteCosmicScalarAsync(plan); result.IsError.Should().BeFalse(result.Message+result.Exception); result.Result.Should().Be(2.5f);
    }

    [Fact]
    public async Task NativeFloatingSinglePrivacyExcludesMalformedHiddenInputs()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actor=Guid.NewGuid();var foreign=Guid.NewGuid();var ids=new List<Guid>();
        for(var index=0;index<3;index++)
        {
            var row=new Holon {Id=Guid.NewGuid(),CreatedByAvatarId=index==0?actor:foreign,IsPublic=index==1,
                MetaData=new() { ["value"]=index==0?(object)1.25f:index==1?2.5f:"hidden invalid",["category"]="one" }};
            (await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();ids.Add(row.Id);
        }
        var keys=ids.ToArray();
        var source=CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Where(h=>keys.Contains(h.Id)).Result;
        var sum=await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,source.Sum(h=>CosmicQueryMetadata.Value<float>(h,"value")).Result));
        sum.IsError.Should().BeFalse(sum.Message+sum.Exception);sum.Result.Should().Be(3.75f);
        var query=source.GroupBy(h=>CosmicQueryMetadata.Value<string>(h,"category"))
            .Select(g=>new {Count=g.Count(),Mean=g.Average(h=>CosmicQueryMetadata.Value<float>(h,"value"))}).Result;
        var result=await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider,query),new[] {provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception);var selected=result.Result.Providers.Single().Rows.Single();
        selected.Count.Should().Be(2);selected.Mean.Should().Be(1.875f);
    }

    [Fact]
    public async Task NativeFloatingSingleGroupedMeanFiltersItsFinalRoundedValue()
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids=await SeedGroups(provider,("tiny",3f*float.Epsilon),("tiny",0f));
        var expected=2f*float.Epsilon;
        var query=CosmicQuery<Holon>.Create().Where(h=>ids.Contains(h.Id)).GroupBy(h=>CosmicQueryMetadata.Value<string>(h,"category"))
            .Select(g=>new {Mean=g.Average(h=>CosmicQueryMetadata.Value<float>(h,"stock"))}).Where(g=>g.Mean==expected).OrderBy(g=>g.Mean).Take(1).Result;
        var result=await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider,query),new[] {provider});
        result.IsError.Should().BeFalse(result.Message+result.Exception);result.Result.Providers.Single().Rows.Single().Mean.Should().Be(expected);
    }
}

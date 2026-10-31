using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicFloatingCaptureTests
{
    [Fact]
    public void RequiredAndNullableAveragePreserveTheFloatingResultType()
    {
        var source = CosmicQuery<Holon>.Create();
        var wrapped = new OASISResult<CosmicQuery<Holon>>(source);
        Verify(source.Average(h => CosmicQueryMetadata.Value<float>(h,"value")), typeof(float));
        Verify(source.Average(h => CosmicQueryMetadata.Value<float?>(h,"value")), typeof(float?));
        Verify(source.Average(h => CosmicQueryMetadata.Value<double>(h,"value")), typeof(double));
        Verify(source.Average(h => CosmicQueryMetadata.Value<double?>(h,"value")), typeof(double?));
        Verify(wrapped.Average(h => CosmicQueryMetadata.Value<float>(h,"value")), typeof(float));
        Verify(wrapped.Average(h => CosmicQueryMetadata.Value<float?>(h,"value")), typeof(float?));
        Verify(wrapped.Average(h => CosmicQueryMetadata.Value<double>(h,"value")), typeof(double));
        Verify(wrapped.Average(h => CosmicQueryMetadata.Value<double?>(h,"value")), typeof(double?));
    }
    private static void Verify<T>(OASISResult<CosmicScalarQuery<Holon,T>> intent, Type expected)
    {
        Assert.False(intent.IsError,intent.Message);
        Assert.Equal(expected,typeof(T)); Assert.Equal(expected,intent.Result.Selector.ValueType);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void CaptureDoesNotGrantArithmeticPermissionAndIntegerPacketsRejectIt(bool single)
    {
        var source = CosmicQuery<Holon>.Create();
        var reads = new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(),new[] { typeof(float),typeof(double) });
        var old = new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum,reads);
        var legacy = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(),new[] { old });
        var explicitPermission = new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(),new[]
        {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum,reads,new[] {single ? typeof(float) : typeof(double)})
        });
        if(single)
        {
            var intent=source.Sum(h=>CosmicQueryMetadata.Value<float>(h,"value"));
            Assert.False(intent.IsError,intent.Message);
            Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",legacy.Admit(intent.Result).ErrorCode);
            Assert.False(explicitPermission.Admit(intent.Result).IsError);
            Assert.Equal("CQL_PLAN_INVALID",CosmicNumericScalarResults.FromParts(intent.Result,0,0,0).ErrorCode);
            var empty=CosmicFloatingAggregateResults.FromParts(intent.Result,new long[10],0);
            Assert.False(empty.IsError,empty.Message); Assert.Equal(0f,empty.Result);
        }
        else
        {
            var intent=source.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value"));
            Assert.False(intent.IsError,intent.Message);
            Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",legacy.Admit(intent.Result).ErrorCode);
            Assert.False(explicitPermission.Admit(intent.Result).IsError);
            Assert.Equal("CQL_PLAN_INVALID",CosmicNumericScalarResults.FromParts(intent.Result,0,0,0).ErrorCode);
            var empty=CosmicFloatingAggregateResults.FromParts(intent.Result,new long[70],0);
            Assert.False(empty.IsError,empty.Message); Assert.Equal(0d,empty.Result);
        }
    }

    [Fact]
    public void GroupedCapturePreservesAllFourNumericDomains()
    {
        var query=CosmicQuery<Holon>.Create().GroupBy(h=>h.Name).Select(g=>new
        {
            Single=g.Sum(h=>CosmicQueryMetadata.Value<float>(h,"value")),
            SingleMean=g.Average(h=>CosmicQueryMetadata.Value<float?>(h,"value")),
            Double=g.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")),
            DoubleMean=g.Average(h=>CosmicQueryMetadata.Value<double?>(h,"value"))
        });
        Assert.False(query.IsError,query.Message);
        Assert.Equal(new[] {typeof(float),typeof(float?),typeof(double),typeof(double?)},query.Result.Columns.Select(c=>c.ValueType));
        foreach(var column in query.Result.Columns)
            _=new CosmicGroupedAggregateOrderingCapability(column.Operation!.Value,column.ValueType);
    }
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void GroupedFloatingAveragePublishesWithoutDecimalConversion(bool single)
    {
        var grouping=CosmicQuery<Holon>.Create().GroupBy(h=>h.Name);
        if(single)
        {
            var query=grouping.Select(g=>g.Average(h=>CosmicQueryMetadata.Value<float>(h,"value"))).Result;
            var result=CosmicGroupedResults.Materialize(query,new[] {new CosmicGroupedRow(2,new object[] {"one"},float.MaxValue)});
            Assert.False(result.IsError,result.Message); Assert.Equal(float.MaxValue,Assert.Single(result.Result));
            Assert.True(CosmicGroupedResults.Materialize(query,new[] {new CosmicGroupedRow(2,new object[] {"one"},float.NaN)}).IsError);
        }
        else
        {
            var query=grouping.Select(g=>g.Average(h=>CosmicQueryMetadata.Value<double>(h,"value"))).Result;
            var result=CosmicGroupedResults.Materialize(query,new[] {new CosmicGroupedRow(2,new object[] {"one"},double.MaxValue)});
            Assert.False(result.IsError,result.Message); Assert.Equal(double.MaxValue,Assert.Single(result.Result));
            Assert.True(CosmicGroupedResults.Materialize(query,new[] {new CosmicGroupedRow(2,new object[] {"one"},double.PositiveInfinity)}).IsError);
        }
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void ScalarFloatingAverageAndEmptySumUseTheirTypedOwner(bool single)
    {
        var source=CosmicQuery<Holon>.Create();
        if(single)
        {
            var average=source.Average(h=>CosmicQueryMetadata.Value<float>(h,"value")).Result;
            Assert.False(CosmicSelectedScalarResults.FromValue(average,float.MaxValue,true).IsError);
            Assert.True(CosmicSelectedScalarResults.FromValue(average,double.MaxValue,true).IsError);
            Assert.True(CosmicSelectedScalarResults.FromValue(average,float.NaN,true).IsError);
            var empty=CosmicSelectedScalarResults.FromValue(source.Sum(h=>CosmicQueryMetadata.Value<float>(h,"value")).Result,null!,false);
            Assert.False(empty.IsError,empty.Message); Assert.Equal(0f,empty.Result);
        }
        else
        {
            var average=source.Average(h=>CosmicQueryMetadata.Value<double>(h,"value")).Result;
            Assert.False(CosmicSelectedScalarResults.FromValue(average,double.MaxValue,true).IsError);
            Assert.True(CosmicSelectedScalarResults.FromValue(average,double.NaN,true).IsError);
            var empty=CosmicSelectedScalarResults.FromValue(source.Sum(h=>CosmicQueryMetadata.Value<double>(h,"value")).Result,null!,false);
            Assert.False(empty.IsError,empty.Message); Assert.Equal(0d,empty.Result);
        }
    }

}

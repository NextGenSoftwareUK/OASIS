using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicInt32DoubleAverageTests
{
    private static CosmicScalarQuery<Holon,double> Query => CosmicQuery<Holon>.Create().AverageAsDouble(row=>row.Version).Result;
    private static CosmicScalarQuery<Holon,double?> NullableQuery => CosmicQuery<Holon>.Create()
        .AverageAsDouble(row=>CosmicQueryMetadata.Value<int?>(row,"value")).Result;

    [Fact]
    public void DoubleOutputNeedsIndependentEnrollmentAndDecimalContractRemainsIntact()
    {
        var reads=new CosmicProjectionCapabilities(new[]{new CosmicProjectionFieldCapability("Version",typeof(int))},
            new[]{typeof(int),typeof(int?)});
        CosmicScalarCapabilities Profile(bool enrolled)=>new(Array.Empty<CosmicScalarOperation>(),
            new[]{new CosmicScalarSelectionCapability(CosmicScalarOperation.Average,reads,new[]{typeof(int),typeof(int?)},enrolled)});
        Assert.True(Query.UsesInt32DoubleAverage);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Profile(false).Admit(Query).ErrorCode);
        Assert.False(Profile(true).Admit(Query).IsError);
        Assert.False(Profile(true).Admit(NullableQuery).IsError);
        var exact=CosmicQuery<Holon>.Create().Average(row=>row.Version).Result;
        Assert.False(exact.UsesInt32DoubleAverage);Assert.False(Profile(false).Admit(exact).IsError);
        Assert.Equal(1m/3m,CosmicNumericScalarResults.FromParts(exact,0,1,3).Result);
    }

    [Fact]
    public void NativeInt32PacketsMatchStandardLinqIncludingLargeAndNegativeValues()
    {
        var random=new Random(817);
        var vectors=new List<int[]> {new[]{int.MaxValue,int.MaxValue},new[]{int.MinValue,int.MinValue},
            new[]{int.MaxValue,int.MinValue,7},new[]{1,0,0},new[]{-1,0,0}};
        for(var index=1;index<=40;index++)vectors.Add(Enumerable.Range(0,index).Select(_=>random.Next(int.MinValue,int.MaxValue)).ToArray());
        foreach(var values in vectors)
        {
            var packet=CosmicNumericScalarResults.FromParts(Query,0,values.Sum(value=>(long)value),values.Length);
            Assert.False(packet.IsError,packet.Message);
            Assert.Equal(BitConverter.DoubleToInt64Bits(values.Average()),BitConverter.DoubleToInt64Bits(packet.Result));
        }
    }

    [Fact]
    public void SumAboveBinary64IntegerPrecisionConvertsOnceBeforeDivision()
    {
        const long sum=9007199254740993L;const long count=5000000;
        var result=CosmicNumericScalarResults.FromParts(Query,0,sum,count);
        Assert.False(result.IsError,result.Message);
        Assert.Equal(BitConverter.DoubleToInt64Bits((double)sum/count),BitConverter.DoubleToInt64Bits(result.Result));
    }

    [Fact]
    public void NullableAndEmptyMeansKeepStandardResultDomains()
    {
        var values=new int?[]{null,int.MaxValue,7,null,int.MinValue};
        var result=CosmicNumericScalarResults.FromParts(NullableQuery,0,6,3);
        Assert.False(result.IsError,result.Message);Assert.Equal(values.Average(),result.Result);
        var empty=CosmicNumericScalarResults.FromParts(NullableQuery,0,0,0);
        Assert.False(empty.IsError);Assert.True(empty.IsLoaded);Assert.Null(empty.Result);
        Assert.Equal("CQL_EMPTY_SEQUENCE",CosmicNumericScalarResults.FromParts(Query,0,0,0).ErrorCode);
    }

    [Fact]
    public void NewResultModeRetainsTheSharedNativePacketBounds()
    {
        Assert.Equal("CQL_DATA_INVALID",CosmicNumericScalarResults.FromParts(Query,1,0,1).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID",CosmicNumericScalarResults.FromParts(Query,0,1,0).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID",CosmicNumericScalarResults.FromParts(Query,0,0,-1).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID",CosmicNumericScalarResults.FromParts(Query,0,(long)int.MaxValue+1,1).ErrorCode);
        Assert.Equal("CQL_QUERY_LIMIT",CosmicNumericScalarResults.FromParts(Query,0,0,(long)CosmicNumericScalarResults.MaximumParticipatingValues+1).ErrorCode);
    }
}

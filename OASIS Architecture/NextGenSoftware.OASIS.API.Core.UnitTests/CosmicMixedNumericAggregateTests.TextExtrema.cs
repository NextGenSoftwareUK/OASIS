using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicMixedNumericAggregateTests
{
    [Theory]
    [InlineData(CosmicScalarOperation.Min, "A")]
    [InlineData(CosmicScalarOperation.Max, "😀")]
    public async Task TextExtremaUseTheLiveWinnerAndUnicodeScalarOrdering(CosmicScalarOperation operation, string expected)
    {
        var f = await Create(typeof(string), true, new object[] { "!", "A", "\uE000", null! },
            new object[] { "z", "😀", null! }, overlap: 1);
        var r = CosmicMixedProjectionResults.Aggregate(f.Query, Actor, f.Mapping, f.Stored, f.Live, operation);
        Assert.False(r.IsError, r.Message); Assert.Equal(typeof(string), r.Result.ValueType);
        Assert.Equal(4L, r.Result.ParticipatingCount); Assert.Equal(expected, r.Result.Value);
    }

    [Theory]
    [InlineData(CosmicScalarOperation.Min)]
    [InlineData(CosmicScalarOperation.Max)]
    public async Task EmptyAndAllNullTextExtremaAreLoadedNull(CosmicScalarOperation operation)
    {
        foreach (var values in new[] { Array.Empty<object>(), new object[] { null! } })
        {
            var f = await Create(typeof(string), true, values, values);
            var r = CosmicMixedProjectionResults.Aggregate(f.Query, Actor, f.Mapping, f.Stored, f.Live, operation);
            Assert.False(r.IsError, r.Message); Assert.True(r.IsLoaded);
            Assert.Equal(0L, r.Result.ParticipatingCount); Assert.Null(r.Result.Value);
        }
    }

    [Theory]
    [InlineData(CosmicScalarOperation.Min, "")]
    [InlineData(CosmicScalarOperation.Max, "é")]
    public async Task TextExtremaPreserveEmptyNulAndUnnormalizedUnicode(CosmicScalarOperation operation, string expected)
    {
        var f = await Create(typeof(string), true, new object[] { "", "e\u0301", "a\0b" },
            new object[] { "é", "Z" });
        var r = CosmicMixedProjectionResults.Aggregate(f.Query, Actor, f.Mapping, f.Stored, f.Live, operation);
        Assert.False(r.IsError, r.Message); Assert.Equal(expected, r.Result.Value);
    }

    [Theory]
    [InlineData(CosmicScalarOperation.Sum)]
    [InlineData(CosmicScalarOperation.Average)]
    public async Task TextExtremaDoNotAdmitNumericStringCoercion(CosmicScalarOperation operation)
    {
        var f = await Create(typeof(string), true, new object[] { "2" }, new object[] { "3" });
        var r = CosmicMixedProjectionResults.Aggregate(f.Query, Actor, f.Mapping, f.Stored, f.Live, operation);
        Assert.True(r.IsError); Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", r.ErrorCode);
    }
}

using System;
using System.Globalization;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlUtcDateMappingContractTests
{
    private static NativeRecordFilter Compile(string value)
        => Assert.Single(CqlV1Compiler.CompileVirtualMatches(new[] { new CqlV1Filter {
            MetadataPath = new[] { "stamp" }, Scalar = CqlV1Scalar.DateTime,
            Operator = CqlV1Operator.GreaterThanOrEqual, Value = JsonSerializer.SerializeToElement(value)
        } }));

    [Theory]
    [InlineData("0001-01-01T00:00:00Z")]
    [InlineData("2000-01-01T00:00:00.0000001Z")]
    [InlineData("2000-01-01T00:00:00.0000002Z")]
    [InlineData("9999-12-31T23:59:59.9999999Z")]
    public void MappedUtcDatesRetainKindAndEveryTick(string text)
    {
        var expected = DateTime.Parse(text, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        var filter = Compile(text);
        Assert.Equal(typeof(DateTime), filter.InputType);
        var value = Assert.IsType<DateTime>(filter.Value);
        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(expected.Ticks, value.Ticks);
        Assert.Equal(expected.Ticks, Assert.IsType<DateTime>(filter.WithField("native_stamp").Value).Ticks);
    }

    [Theory]
    [InlineData("0000-01-01T00:00:00Z")]
    [InlineData("1900-02-29T00:00:00Z")]
    [InlineData("2000-02-30T00:00:00Z")]
    [InlineData("2000-01-01T00:00:60Z")]
    [InlineData("2000-01-01T00:00:00+00:00")]
    [InlineData("2000-01-01T00:00:00.000000001Z")]
    [InlineData("2000-01-01T00:00:00")]
    public void MappedUtcDatesRefuseInvalidCalendarAmbiguityAndRounding(string text)
        => Assert.Throws<ArgumentException>(() => Compile(text));

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void NativeUtcDateLiteralsRefuseUnprovenTimezone(DateTimeKind kind)
        => Assert.Throws<ArgumentException>(() => new NativeRecordFilter("stamp",
            CosmicQueryComparisonOperator.Equal, new DateTime(2000,1,1,0,0,0,kind)));

    [Fact]
    public void NativeUtcProjectionPreservesTypedTicksAndRefusesLocalCells()
    {
        var value = new DateTime(2000,1,1,0,0,0,DateTimeKind.Utc).AddTicks(1);
        var column = new NativeProjectionColumn("stamp","Moment",typeof(DateTime));
        var cell = new NativeProjectionCell(NativeProjectionValueState.Present,value);
        var result = NativeProjectionResults.Capture("events",new[]{column},
            new[]{new NativeProjectionRow("events","k",new[]{cell})});
        Assert.False(result.IsError,result.Message);
        var actual = Assert.IsType<DateTime>(Assert.Single(Assert.Single(result.Result).Cells).Value);
        Assert.Equal(DateTimeKind.Utc,actual.Kind);
        Assert.Equal(value.Ticks,actual.Ticks);
        Assert.Throws<ArgumentException>(() => new NativeProjectionCell(NativeProjectionValueState.Present,
            DateTime.SpecifyKind(value,DateTimeKind.Local)));
        Assert.Throws<ArgumentException>(() => new NativeProjectionCell(NativeProjectionValueState.Present,
            DateTime.SpecifyKind(value,DateTimeKind.Unspecified)));
    }
}

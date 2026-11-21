using System;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class NativeGuidProjectionContractTests
{
    [Fact]
    public void GuidColumnKeepsExactTypeAndMappedField()
    {
        var original = new NativeProjectionColumn("source", "Alias", typeof(Guid), true);
        var mapped = original.WithField("native");
        Assert.Equal("source", original.Field);
        Assert.Equal("native", mapped.Field);
        Assert.Equal("Alias", mapped.Alias);
        Assert.Equal(typeof(Guid), mapped.InputType);
        Assert.Equal(typeof(Guid), mapped.ValueType);
        Assert.True(mapped.Nullable);
        Assert.Same(mapped, Assert.Single(NativeProjectionColumn.Capture(new[] { mapped })));
    }

    [Fact]
    public void GuidPayloadChargesSixteenBytes()
    {
        var columns = new[] { new NativeProjectionColumn("source", "Alias", typeof(Guid)) };
        var rows = new[] { new NativeProjectionRow("S", "K",
            new[] { new NativeProjectionCell(NativeProjectionValueState.Present, Guid.Empty) }) };
        Assert.True(NativeProjectionResults.Capture("S", columns, rows, 1, 18).IsLoaded);
        Assert.Equal("NATIVE_PROJECTION_PAYLOAD_LIMIT",
            NativeProjectionResults.Capture("S", columns, rows, 1, 17).ErrorCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GuidMissingRespectsDeclaredNullability(bool nullable)
    {
        var columns = new[] { new NativeProjectionColumn("source", "Alias", typeof(Guid), nullable) };
        var rows = new[] { new NativeProjectionRow("S", "K",
            new[] { new NativeProjectionCell(NativeProjectionValueState.Missing) }) };
        var result = NativeProjectionResults.Capture("S", columns, rows);
        Assert.Equal(!nullable, result.IsError);
        if (nullable) Assert.True(result.IsLoaded);
        else Assert.Equal("NATIVE_PROJECTION_RESULT_INVALID", result.ErrorCode);
    }

    [Fact]
    public void GuidProjectionNeverAcceptsAnUnconvertedTextCell()
    {
        var columns = new[] { new NativeProjectionColumn("source", "Alias", typeof(Guid)) };
        var rows = new[] { new NativeProjectionRow("S", "K",
            new[] { new NativeProjectionCell(NativeProjectionValueState.Present, Guid.NewGuid().ToString("D")) }) };
        Assert.Equal("NATIVE_PROJECTION_RESULT_INVALID",
            NativeProjectionResults.Capture("S", columns, rows).ErrorCode);
    }

    [Fact]
    public void GuidProjectionCannotUseStringLength()
        => Assert.Throws<ArgumentException>(() => new NativeProjectionColumn("source", "Alias", typeof(Guid),
            true, NativeRecordFunction.Utf16Length));
}

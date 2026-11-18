using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlStringLengthWireTests_NativeResultProjection
{
    private static NativeProjectionRow Row(string key, NativeProjectionCell cell, string source = "s")
        => new(source, key, new[] { cell });

    [Fact]
    public void ResultAliasesCanSelectTheSameSourceAsTextAndLengthWithoutChangingItsDomain()
    {
        var columns = new List<NativeProjectionColumn>
        {
            new("label", "Text", typeof(string), nullable: true),
            new("label", "Units", typeof(string), nullable: true, NativeRecordFunction.Utf16Length)
        };
        var frozen = NativeProjectionColumn.Capture(columns); columns.Clear();
        Assert.Equal(2, frozen.Count);
        Assert.Equal(typeof(string), frozen[0].ValueType);
        Assert.Equal(typeof(string), frozen[1].InputType);
        Assert.Equal(typeof(int), frozen[1].ValueType);
        Assert.Equal("label", frozen[1].Field);
        var rebound = frozen[1].WithField("native_label");
        Assert.Equal("Units", rebound.Alias);
        Assert.Equal(NativeRecordFunction.Utf16Length, rebound.Function);
        Assert.Equal("label", frozen[1].Field);
        Assert.Throws<ArgumentException>(() => NativeProjectionColumn.Capture(new[] { frozen[0], frozen[0] }));
    }

    [Fact]
    public void LengthRejectsNontextAndUndefinedFunctionSchemas()
    {
        Assert.Throws<ArgumentException>(() => new NativeProjectionColumn("score", "Units", typeof(long), true, NativeRecordFunction.Utf16Length));
        Assert.Throws<ArgumentException>(() => new NativeProjectionColumn("label", "Units", typeof(string), true, (NativeRecordFunction)999));
        Assert.Throws<ArgumentException>(() => new NativeProjectionColumn("label", "Units", typeof(string), true, NativeRecordFunction.Contains));
        Assert.Throws<ArgumentException>(() => NativeProjectionColumn.Capture(null));
        Assert.Throws<ArgumentException>(() => new NativeProjectionRow("s", "k", null));
        Assert.Equal("NATIVE_PROJECTION_RESULT_INVALID", NativeProjectionResults.Capture("s",
            new[] { new NativeProjectionColumn("label", "Text", typeof(string)) }, null).ErrorCode);
    }

    [Fact]
    public void MissingNullAndPresentAreDistinctAndCannotBypassExactOutputTypes()
    {
        var column = new NativeProjectionColumn("label", "Units", typeof(string), true, NativeRecordFunction.Utf16Length);
        var rows = new[]
        {
            Row("missing", new(NativeProjectionValueState.Missing)),
            Row("null", new(NativeProjectionValueState.Null)),
            Row("value", new(NativeProjectionValueState.Present, 4))
        };
        var accepted = NativeProjectionResults.Capture("s", new[] { column }, rows);
        Assert.False(accepted.IsError, accepted.Message);
        Assert.Equal(NativeProjectionValueState.Missing, accepted.Result[0].Cells[0].State);
        Assert.Equal(NativeProjectionValueState.Null, accepted.Result[1].Cells[0].State);
        var wrong = NativeProjectionResults.Capture("s", new[] { column },
            new[] { Row("wrong", new(NativeProjectionValueState.Present, 4L)) });
        Assert.Equal("NATIVE_PROJECTION_RESULT_INVALID", wrong.ErrorCode);
        var required = new NativeProjectionColumn("label", "Units", typeof(string), false, NativeRecordFunction.Utf16Length);
        Assert.True(NativeProjectionResults.Capture("s", new[] { required }, rows).IsError);
        Assert.Throws<ArgumentException>(() => new NativeProjectionCell(NativeProjectionValueState.Missing, 4));
    }

    [Fact]
    public void NativeResponseRejectsDuplicateIdentityWrongSourceAndPartialShape()
    {
        var schema = new[] { new NativeProjectionColumn("label", "Text", typeof(string)) };
        var row = Row("key", new(NativeProjectionValueState.Present, "hello"));
        Assert.True(NativeProjectionResults.Capture("s", schema, new[] { row, row }).IsError);
        Assert.True(NativeProjectionResults.Capture("other", schema, new[] { row }).IsError);
        Assert.True(NativeProjectionResults.Capture("s", schema, new[] { row, Row("second", row.Cells[0]) }, maximumRows: 1).IsError);
        Assert.True(NativeProjectionResults.Capture("s",
            new[] { schema[0], new NativeProjectionColumn("other", "Other", typeof(string)) }, new[] { row }).IsError);
    }

    [Fact]
    public void ByteLimitUsesUtf8AndCellListsAreDetachedFromCallerMutation()
    {
        var cells = new List<NativeProjectionCell> { new(NativeProjectionValueState.Present, "😀") };
        var row = new NativeProjectionRow("s", "k", cells); cells.Clear();
        Assert.Single(row.Cells);
        var schema = new[] { new NativeProjectionColumn("label", "Text", typeof(string)) };
        Assert.False(NativeProjectionResults.Capture("s", schema, new[] { row }, maximumPayloadBytes: 6).IsError);
        Assert.Equal("NATIVE_PROJECTION_PAYLOAD_LIMIT",
            NativeProjectionResults.Capture("s", schema, new[] { row }, maximumPayloadBytes: 5).ErrorCode);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => NativeProjectionResults.Capture("s", schema,
            new[] { row }, cancellationToken: cancellation.Token));
    }
    [Fact]
    public void IdentityOnlyResultsHaveNoPropertyCellsAndKeepAllResponseGuards()
    {
        var empty = Array.Empty<NativeProjectionColumn>();
        Assert.Empty(NativeProjectionColumn.Capture(empty));
        var row = new NativeProjectionRow("s", "one", Array.Empty<NativeProjectionCell>());
        Assert.Empty(row.Cells);
        var accepted = NativeProjectionResults.Capture("s", empty, new[] { row }, maximumPayloadBytes: 4);
        Assert.False(accepted.IsError, accepted.Message);
        Assert.Equal("one", Assert.Single(accepted.Result).NativeKey);
        Assert.Equal("NATIVE_PROJECTION_PAYLOAD_LIMIT",
            NativeProjectionResults.Capture("s", empty, new[] { row }, maximumPayloadBytes: 3).ErrorCode);
        Assert.Equal("NATIVE_PROJECTION_RESULT_INVALID",
            NativeProjectionResults.Capture("s", empty, new[] { row, row }).ErrorCode);
        Assert.Equal("NATIVE_PROJECTION_RESULT_INVALID",
            NativeProjectionResults.Capture("other", empty, new[] { row }).ErrorCode);
        Assert.Equal("NATIVE_PROJECTION_RESULT_INVALID", NativeProjectionResults.Capture("s", empty,
            new[] { new NativeProjectionRow("s", "one", new[] { new NativeProjectionCell(NativeProjectionValueState.Present, "unselected") }) }).ErrorCode);
        Assert.Throws<ArgumentException>(() => new NativeProjectionRow("s", "one", null));
    }

}

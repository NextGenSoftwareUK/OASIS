using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicProjectionRecordTests
{
    [Fact]
    public void RuntimeColumnsRetainTypedPathsSourceAndImmutableOrder()
    {
        var source = CosmicQuery<Holon>.Create().Skip(2).Take(3).Result;
        var columns = new[]
        {
            CosmicProjectionSelection<Holon>.Create("label", h => h.Name).Result,
            CosmicProjectionSelection<Holon>.Create("stock", h => CosmicQueryMetadata.Value<long?>(h, "nested", "stock")).Result
        };
        var selected = source.SelectColumns(columns);
        Assert.False(selected.IsError, selected.Message);
        columns[0] = columns[1];
        Assert.Same(source, selected.Result.Source);
        Assert.False(selected.Result.IsScalar);
        Assert.Equal("label", selected.Result.Columns[0].Alias);
        Assert.Equal(typeof(long?), selected.Result.Columns[1].Path.ValueType);
        Assert.Equal(new[] { "nested", "stock" }, selected.Result.Columns[1].Path.Segments);
    }

    [Fact]
    public void RuntimeRecordsUseTheSameExactDomainValidationBeforePublication()
    {
        var query = CosmicQuery<Holon>.Create().SelectColumns(new[]
        { CosmicProjectionSelection<Holon>.Create("stock", h => CosmicQueryMetadata.Value<long>(h, "stock")).Result }).Result;
        var rejected = CosmicProjectionResults.Materialize(query,
            new[] { new CosmicProjectionRow(7L), new CosmicProjectionRow("8") });
        Assert.True(rejected.IsError); Assert.Null(rejected.Result);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", rejected.ErrorCode);
    }

    [Fact]
    public void ConstructedRecordsAreDetachedAndRetainAliasValueCorrespondence()
    {
        var query = CosmicQuery<Holon>.Create().SelectColumns(new[]
        {
            CosmicProjectionSelection<Holon>.Create("label", h => h.Name).Result,
            CosmicProjectionSelection<Holon>.Create("stock", h => CosmicQueryMetadata.Value<long?>(h, "stock")).Result
        }).Result;
        var raw = new object[] { "coffee", 9007199254740993L };
        var rows = new[] { new CosmicProjectionRow(raw), new CosmicProjectionRow("tea", null) };
        var result = CosmicProjectionResults.Materialize(query, rows);
        Assert.False(result.IsError, result.Message);
        raw[0] = "changed";
        var record = result.Result[0];
        Assert.Equal(new[] { "label", "stock" }, record.Aliases);
        Assert.Equal("coffee", record["label"]);
        Assert.Equal(9007199254740993L, record["stock"]);
        Assert.Null(result.Result[1]["stock"]);
        Assert.Throws<NotSupportedException>(() => ((IList<object>)record.Values)[0] = "changed");
        Assert.Throws<KeyNotFoundException>(() => record["missing"]);
    }

    [Fact]
    public void EmptyDuplicateOrMissingColumnsCannotCreateARecordShape()
    {
        var source = CosmicQuery<Holon>.Create();
        var column = CosmicProjectionSelection<Holon>.Create("label", h => h.Name).Result;
        Assert.True(source.SelectColumns(Array.Empty<CosmicProjectionSelection<Holon>>()).IsError);
        Assert.True(source.SelectColumns(new[] { column, column }).IsError);
        Assert.True(source.SelectColumns(new CosmicProjectionSelection<Holon>[] { null }).IsError);
        Assert.True(source.SelectColumns(null).IsError);
    }

    [Fact]
    public void OversizedListsAreRejectedBeforeIndexAccessOrEnumeration()
    {
        var columns = new OversizedColumns();
        Assert.True(CosmicQuery<Holon>.Create().SelectColumns(columns).IsError);
        Assert.Equal(0, columns.Reads);
    }

    [Fact]
    public void ExpressionsAndAliasesUseTheExistingProjectionCompilerWithoutInvocation()
    {
        Calls = 0;
        Assert.True(CosmicProjectionSelection<Holon>.Create("unsafe", h => Execute(h.Name)).IsError);
        Assert.True(CosmicProjectionSelection<Holon>.Create("cast", h => (long)h.Version).IsError);
        Assert.True(CosmicProjectionSelection<Holon>.Create("", h => h.Name).IsError);
        Assert.True(CosmicProjectionSelection<Holon>.Create(new string('x', 129), h => h.Name).IsError);
        Assert.Equal(0, Calls);
    }

    [Fact]
    public void SourceErrorsAndMissingSourceRemainExplicitErrors()
    {
        var column = CosmicProjectionSelection<Holon>.Create("label", h => h.Name).Result;
        var source = CosmicQuery<Holon>.Create().Where(h => Execute(h.Name) == "x");
        var result = source.SelectColumns(new[] { column });
        Assert.True(result.IsError); Assert.Equal(source.ErrorCode, result.ErrorCode);
        CosmicQuery<Holon> missing = null;
        Assert.True(missing.SelectColumns(new[] { column }).IsError);
    }

    private static int Calls;
    private static string Execute(string value) { Calls++; return value; }
    private sealed class OversizedColumns : IReadOnlyList<CosmicProjectionSelection<Holon>>
    {
        public int Reads;
        public int Count => 33;
        public CosmicProjectionSelection<Holon> this[int index] { get { Reads++; throw new InvalidOperationException(); } }
        public IEnumerator<CosmicProjectionSelection<Holon>> GetEnumerator() => throw new InvalidOperationException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

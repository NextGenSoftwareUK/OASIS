using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicProjectionResultTests
{
    public class Snapshot
    {
        public static int Calls;
        public Snapshot() { Calls++; }
        public string Label { get; set; }
        public int Revision { get; set; }
    }

    [Fact]
    public void EntireNativeBufferIsValidatedBeforeAnyDtoConstructor()
    {
        var query = CosmicQuery<Holon>.Create().Select(holon => new Snapshot { Label = holon.Name, Revision = holon.Version }).Result;
        Snapshot.Calls = 0;
        var invalid = CosmicProjectionResults.Materialize(query,
            new[] { new CosmicProjectionRow("coffee", 1), new CosmicProjectionRow("tea", "2") });
        Assert.True(invalid.IsError);
        Assert.Equal(0, Snapshot.Calls);
        var valid = CosmicProjectionResults.Materialize(query, new[] { new CosmicProjectionRow("coffee", 1) });
        Assert.False(valid.IsError, valid.Message);
        Assert.Equal("coffee", valid.Result.Single().Label);
        Assert.Equal(1, Snapshot.Calls);
    }

    [Fact]
    public void ExactLongAndNullableDomainsAreNotCoerced()
    {
        var query = CosmicQuery<Holon>.Create().Select(holon => CosmicQueryMetadata.Value<long?>(holon, "stock")).Result;
        var valid = CosmicProjectionResults.Materialize(query,
            new[] { new CosmicProjectionRow(9007199254740993L), new CosmicProjectionRow(new object[] { null }) });
        Assert.False(valid.IsError, valid.Message);
        Assert.Equal(9007199254740993L, valid.Result[0]);
        Assert.Null(valid.Result[1]);
        Assert.True(CosmicProjectionResults.Materialize(query, new[] { new CosmicProjectionRow(9007199254740992d) }).IsError);
    }

    [Fact]
    public void CardinalityPayloadAndCancellationFailBeforeConstruction()
    {
        var query = CosmicQuery<Holon>.Create().Select(holon => new Snapshot { Label = holon.Name, Revision = holon.Version }).Result;
        Snapshot.Calls = 0;
        var rows = new[] { new CosmicProjectionRow("coffee", 1), new CosmicProjectionRow("tea", 2) };
        Assert.True(CosmicProjectionResults.Materialize(query, rows, maximumResults: 1).IsError);
        Assert.True(CosmicProjectionResults.Materialize(query, rows, maximumPayloadBytes: 2).IsError);
        Assert.True(CosmicProjectionResults.Materialize(query, rows,
            cancellationToken: new CancellationToken(true)).IsError);
        Assert.Equal(0, Snapshot.Calls);
    }

    [Fact]
    public void NativeRowsSnapshotCellsAndRejectInvalidStringsAndNullValueTypes()
    {
        var values = new object[] { "coffee" };
        var row = new CosmicProjectionRow(values);
        values[0] = "changed";
        Assert.Equal("coffee", row.Values[0]);
        var query = CosmicQuery<Holon>.Create().Select(holon => holon.Name).Result;
        Assert.True(CosmicProjectionResults.Materialize(query, new[] { new CosmicProjectionRow("\ud800") }).IsError);
        var integer = CosmicQuery<Holon>.Create().Select(holon => holon.Version).Result;
        Assert.True(CosmicProjectionResults.Materialize(integer, new[] { new CosmicProjectionRow(new object[] { null }) }).IsError);
    }
    [Fact]
    public void AnonymousConstructionRetainsTypedMemberOrder()
    {
        var query = CosmicQuery<Holon>.Create().Select(holon => new { holon.Name, holon.Version }).Result;
        var result = CosmicProjectionResults.Materialize(query, new[] { new CosmicProjectionRow("coffee", 7) });
        Assert.False(result.IsError, result.Message);
        Assert.Equal("coffee", result.Result.Single().Name);
        Assert.Equal(7, result.Result.Single().Version);
    }

    public class ThrowingSetter
    {
        public string Label { get => "unused"; set => throw new InvalidOperationException("controlled application setter"); }
    }

    [Fact]
    public void ApplicationConstructionFailureDoesNotPublishPartialDtos()
    {
        var query = CosmicQuery<Holon>.Create().Select(holon => new ThrowingSetter { Label = holon.Name }).Result;
        var result = CosmicProjectionResults.Materialize(query, new[] { new CosmicProjectionRow("coffee") });
        Assert.True(result.IsError);
        Assert.False(result.IsLoaded);
        Assert.Null(result.Result);
        Assert.Equal("CQL_PROJECTION_CONSTRUCTION_FAILED", result.ErrorCode);
    }

}

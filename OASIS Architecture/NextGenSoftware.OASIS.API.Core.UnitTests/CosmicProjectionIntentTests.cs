using System;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicProjectionIntentTests
{
    [Fact]
    public void SelectionRetainsSourceWindowAccessAndGraphWithoutExecutingDtoCode()
    {
        var actor = Guid.NewGuid();
        var access = CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        var graph = CosmicQueryGraphOptions.Create(maxChildDepth: 2, maximumNodes: 4).Result;
        var source = CosmicQuery<Holon>.Create().WithReadAccess(access).Result
            .WithChildren(graph).Result.Skip(2).Result.Take(3).Result;
        Snapshot.Calls = 0;

        var selected = source.Select(holon => new Snapshot { Label = holon.Name, Revision = holon.Version });

        Assert.False(selected.IsError, selected.Message);
        Assert.Same(source, selected.Result.Source);
        Assert.Equal(2, selected.Result.Columns.Count);
        Assert.Equal("Label", selected.Result.Columns[0].Alias);
        Assert.Equal("Name", selected.Result.Columns[0].Path.Segments[0]);
        Assert.Equal(typeof(int), selected.Result.Columns[1].Path.ValueType);
        Assert.Equal(0, Snapshot.Calls);
    }

    [Fact]
    public void AnonymousAndScalarSelectionsUseTypedNativePaths()
    {
        var source = CosmicQuery<Holon>.Create();
        var anonymous = source.Select(holon => new { holon.Id,
            Stock = CosmicQueryMetadata.Value<long?>(holon, "stock") });
        Assert.False(anonymous.IsError, anonymous.Message);
        Assert.Equal(typeof(long?), anonymous.Result.Columns[1].Path.ValueType);
        Assert.True(anonymous.Result.Columns[1].Path.IsMetadata);
        var scalar = source.Select(holon => holon.Name);
        Assert.False(scalar.IsError, scalar.Message);
        Assert.Single(scalar.Result.Columns);
    }

    [Fact]
    public void ArbitraryMethodsAndEntitySelectionAreRejectedWithoutInvocation()
    {
        var source = CosmicQuery<Holon>.Create();
        Snapshot.Calls = 0;
        Assert.True(source.Select(holon => Dangerous(holon.Name)).IsError);
        Assert.True(source.Select(holon => holon).IsError);
        Assert.True(source.Select(holon => new Holon { Name = holon.Name }).IsError);
        Assert.Equal(0, Snapshot.Calls);
    }

    [Fact]
    public void CollectionInitializersAndComputedValuesAreNotNativeSelections()
    {
        var source = CosmicQuery<Holon>.Create();
        Assert.True(source.Select(holon => new[] { holon.Name }).IsError);
        Assert.True(source.Select(holon => new { Computed = holon.Version + 1 }).IsError);
        Assert.True(source.Select(holon => new { Constant = "coffee" }).IsError);
    }

    [Fact]
    public void FailedSourceAndNullInputsStayErrors()
    {
        var failed = CosmicQuery<Holon>.Create().Where(holon => Dangerous(holon.Name) == "x");
        var selected = failed.Select(holon => holon.Name);
        Assert.True(selected.IsError);
        Assert.Equal(failed.ErrorCode, selected.ErrorCode);
        CosmicQuery<Holon> missing = null;
        Assert.True(missing.Select(holon => holon.Name).IsError);
        Assert.True(CosmicQuery<Holon>.Create().Select((System.Linq.Expressions.Expression<Func<Holon, string>>)null).IsError);
    }

    [Fact]
    public void OversizedSelectionsAreRejectedBeforeAnyNativeWork()
    {
        var selected = CosmicQuery<Holon>.Create().Select(holon => new
        {
            Column0 = holon.Name,
            Column1 = holon.Name,
            Column2 = holon.Name,
            Column3 = holon.Name,
            Column4 = holon.Name,
            Column5 = holon.Name,
            Column6 = holon.Name,
            Column7 = holon.Name,
            Column8 = holon.Name,
            Column9 = holon.Name,
            Column10 = holon.Name,
            Column11 = holon.Name,
            Column12 = holon.Name,
            Column13 = holon.Name,
            Column14 = holon.Name,
            Column15 = holon.Name,
            Column16 = holon.Name,
            Column17 = holon.Name,
            Column18 = holon.Name,
            Column19 = holon.Name,
            Column20 = holon.Name,
            Column21 = holon.Name,
            Column22 = holon.Name,
            Column23 = holon.Name,
            Column24 = holon.Name,
            Column25 = holon.Name,
            Column26 = holon.Name,
            Column27 = holon.Name,
            Column28 = holon.Name,
            Column29 = holon.Name,
            Column30 = holon.Name,
            Column31 = holon.Name,
            Column32 = holon.Name
        });
        Assert.True(selected.IsError);
        Assert.Equal("CQL_PROJECTION_UNSUPPORTED", selected.ErrorCode);
    }

    [Fact]
    public void NumericConversionsCannotLoseTheirDeclaredResultDomain()
    {
        var selected = CosmicQuery<Holon>.Create().Select(holon => (long)holon.Version);
        Assert.True(selected.IsError);
        Assert.Equal("CQL_PROJECTION_UNSUPPORTED", selected.ErrorCode);
    }

    private static string Dangerous(string name) { Snapshot.Calls++; return name; }
    public class Snapshot
    {
        public static int Calls;
        public Snapshot() { Calls++; }
        public string Label { get; set; }
        public int Revision { get; set; }
    }
}

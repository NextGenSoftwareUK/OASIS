using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedQueryTests
{
    [Fact]
    public void CompositeKeyPreservesTheOriginalTrustedSourceWithoutConstructingIt()
    {
        var scope = CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result;
        var access = CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot).Result;
        var source = CosmicQuery<Holon>.Create(scope).WithReadAccess(access).WithChildren(graph)
            .Where(h => h.Version > 0).OrderBy(h => h.Name).Skip(2).Take(5).Result;
        var result = source.GroupBy(h => new { h.Version, City = CosmicQueryMetadata.Value<string>(h, "location", "city") });
        Assert.False(result.IsError, result.Message);
        Assert.Same(source, result.Result.Source);
        Assert.Same(scope, result.Result.Source.Scope);
        Assert.Same(access, result.Result.Source.ReadAccess);
        Assert.Same(graph, result.Result.Source.GraphOptions);
        Assert.Equal(2, result.Result.Key.Columns.Count);
        Assert.Equal(new[] { "location", "city" }, result.Result.Key.Columns[1].Path.Segments);
        Assert.Equal(2, result.Result.Source.Offset);
        Assert.Equal(5, result.Result.Source.Limit);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10001, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 33554433)]
    public void InvalidOutputBoundsRejectBeforeCapture(int groups, int bytes)
        => Assert.True(CosmicGroupingOptions.Create(groups, bytes).IsError);

    [Fact]
    public void OutputLimitsAreIndependentAndImmutable()
    {
        var options = CosmicGroupingOptions.Create(7, 1024).Result;
        var source = CosmicQuery<Holon>.Create();
        var grouped = source.GroupBy(h => h.Name, options).Result;
        Assert.Same(options, grouped.Options);
        Assert.Equal(7, grouped.Options.MaximumGroups);
        Assert.Equal(1024, grouped.Options.MaximumPayloadBytes);
        Assert.Null(grouped.Source.Limit);
    }

    [Fact]
    public void ArbitraryKeyCodeIsRejectedWithoutExecutingIt()
    {
        var result = CosmicQuery<Holon>.Create().GroupBy(h => Forbidden(h));
        Assert.True(result.IsError);
    }

    private static string Forbidden(Holon _) => throw new InvalidOperationException("Key code must never execute.");

    [Fact]
    public void KeyDtoConstructorIsCapturedWithoutInvocation()
    {
        var result = CosmicQuery<Holon>.Create().GroupBy(h => new KeyDto { Name = h.Name });
        Assert.False(result.IsError, result.Message);
        Assert.Single(result.Result.Key.Columns);
    }

    public sealed class KeyDto
    {
        public KeyDto() => throw new InvalidOperationException("Grouping capture cannot construct user objects.");
        public string Name { get; set; } = "";
    }

    [Fact]
    public void NineKeyColumnsRejectBeforeExecution()
    {
        var result = CosmicQuery<Holon>.Create().GroupBy(h => new
        { A = h.Version, B = h.Version, C = h.Version, D = h.Version, E = h.Version,
            F = h.Version, G = h.Version, H = h.Version, I = h.Version });
        Assert.True(result.IsError);
    }

    [Fact]
    public void EightKeyColumnsAreAdmittedForLaterProviderPlanning()
    {
        var result = CosmicQuery<Holon>.Create().GroupBy(h => new
        { A = h.Version, B = h.Version, C = h.Version, D = h.Version,
            E = h.Version, F = h.Version, G = h.Version, H = h.Version });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(8, result.Result.Key.Columns.Count);
    }

    [Fact]
    public void NullableAndMissingKeyDomainsRemainExplicit()
    {
        var result = CosmicQuery<Holon>.Create().GroupBy(h => CosmicQueryMetadata.Value<long?>(h, "optional"));
        Assert.False(result.IsError, result.Message);
        Assert.Equal(typeof(long?), result.Result.Key.Columns[0].Path.ValueType);
    }

    [Fact]
    public void UnsupportedDecimalKeyCannotBorrowIntegerEquality()
        => Assert.True(CosmicQuery<Holon>.Create().GroupBy(h => CosmicQueryMetadata.Value<decimal>(h, "money")).IsError);

    [Fact]
    public void SourceFailureIsPreserved()
    {
        var source = new OASISResult<CosmicQuery<Holon>> { IsError = true, ErrorCode = "source-failed", Message = "source rejection" };
        var result = source.GroupBy(h => h.Name);
        Assert.True(result.IsError);
        Assert.Equal("source-failed", result.ErrorCode);
    }

    [Fact]
    public void MissingSourceOrSelectorCannotCaptureAGroup()
    {
        Assert.True(((CosmicQuery<Holon>)null!).GroupBy(h => h.Name).IsError);
        Assert.True(CosmicQuery<Holon>.Create().GroupBy((Expression<Func<Holon, string>>)null!).IsError);
    }
}

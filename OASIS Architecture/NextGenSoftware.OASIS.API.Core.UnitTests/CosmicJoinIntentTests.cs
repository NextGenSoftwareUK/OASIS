using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicJoinIntentTests
{
    public sealed class Key
    {
        public static int Constructions;
        public Key() { Constructions++; }
        public string Name { get; set; } = "";
        public Guid Id { get; set; }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CapturePreservesBothSourcesWindowsAccessAndStrongestConsistency(bool leftJoin)
    {
        var actor = CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var left = CosmicQuery<Holon>.Create().WithReadAccess(actor).Where(row => row.IsPublic)
            .OrderBy(row => row.Name).Skip(2).Take(3).Result;
        var right = CosmicQuery<Holon>.Create().WithReadAccess(actor).Where(row => row.IsActive)
            .WithChildren(CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot).Result)
            .OrderBy(row => row.Id).Take(4).Result;
        var result = leftJoin ? left.LeftJoin(right, row => row.Id, row => row.ParentHolonId)
            : left.Join(right, row => row.Id, row => row.ParentHolonId);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.Left.Should().BeSameAs(left);
        result.Result.Right.Should().BeSameAs(right);
        result.Result.Kind.Should().Be(leftJoin ? CosmicJoinKind.Left : CosmicJoinKind.Inner);
        result.Result.RequiredConsistency.Should().Be(CosmicQueryGraphConsistency.ProviderSnapshot);
        result.Result.Keys.Single().Left.Segments.Should().Equal("Id");
        result.Result.Keys.Single().Right.Segments.Should().Equal("ParentHolonId");
    }

    [Fact]
    public void CompositeDtoCaptureAlignsNamedComponentsWithoutConstructingKeys()
    {
        Key.Constructions = 0;
        var result = CosmicQuery<Holon>.Create().Join(CosmicQuery<Holon>.Create(),
            row => new Key { Name = row.Name, Id = row.Id },
            row => new Key { Id = row.ParentHolonId, Name = row.Description });
        result.IsError.Should().BeFalse(result.Message);
        Key.Constructions.Should().Be(0);
        result.Result.Keys.Select(key => key.Alias).Should().Equal("Name", "Id");
        result.Result.Keys[0].Right.Segments.Should().Equal("Description");
        result.Result.Keys[1].Right.Segments.Should().Equal("ParentHolonId");
    }

    [Fact]
    public void EquivalentNamedScopeOrderIsAcceptedAndInputArrayCannotMutateIt()
    {
        var providers = new[] { ProviderType.MongoDBOASIS, ProviderType.Neo4jOASIS };
        var left = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(providers).Result);
        var right = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(providers.Reverse().ToArray()).Result);
        providers[0] = ProviderType.SQLLiteDBOASIS;
        left.Join(right, row => row.Id, row => row.Id).IsError.Should().BeFalse();
        left.Scope.Providers.Should().NotContain(ProviderType.SQLLiteDBOASIS);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void DifferentScopesCannotBecomeAnImplicitDistributedJoin(bool named)
    {
        var left = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible);
        var right = CosmicQuery<Holon>.Create(named
            ? CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result : CosmicQueryScope.CurrentDefault);
        left.Join(right, row => row.Id, row => row.Id).ErrorCode.Should().Be("CQL_JOIN_SCOPE_MISMATCH");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ActorMismatchOrUnboundSideCannotDropAnAccessRestriction(bool unbound)
    {
        var left = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var right = unbound ? CosmicQuery<Holon>.Create()
            : CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        left.LeftJoin(right, row => row.Id, row => row.Id).ErrorCode.Should().Be("CQL_JOIN_ACCESS_MISMATCH");
    }

    [Fact]
    public void NineComponentsAreRejectedBeforeAnyExecution()
    {
        var result = CosmicQuery<Holon>.Create().Join(CosmicQuery<Holon>.Create(),
            row => new { A=row.Id,B=row.Id,C=row.Id,D=row.Id,E=row.Id,F=row.Id,G=row.Id,H=row.Id,I=row.Id },
            row => new { A=row.Id,B=row.Id,C=row.Id,D=row.Id,E=row.Id,F=row.Id,G=row.Id,H=row.Id,I=row.Id });
        result.ErrorCode.Should().Be("CQL_JOIN_UNSUPPORTED");
    }

    [Fact]
    public void ArbitraryKeyMethodsAreRejectedWithoutCallingThem()
    {
        var calls = 0;
        string Forbidden() { calls++; return "key"; }
        // A captured delegate is executable application code, never a native path.
        Func<string> method = Forbidden;
        var result = CosmicQuery<Holon>.Create().Join(CosmicQuery<Holon>.Create(), row => method(), row => row.Name);
        result.IsError.Should().BeTrue();
        calls.Should().Be(0);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void FluentSourceFailurePreservesItsOriginalError(bool rightFailed)
    {
        var fault = new OASISResult<CosmicQuery<Holon>> { IsError=true, ErrorCode="SOURCE_FAILURE", Message="original" };
        var valid = new OASISResult<CosmicQuery<Holon>>(CosmicQuery<Holon>.Create());
        var result = (rightFailed ? valid : fault).Join(rightFailed ? fault : valid, row => row.Id, row => row.Id);
        result.ErrorCode.Should().Be("SOURCE_FAILURE");
        result.Message.Should().Be("original");
    }

    [Fact]
    public void NullableMetadataKeyPathsAreCapturedBeforeTheClosureChanges()
    {
        var name = "tenant";
        var result = CosmicQuery<Holon>.Create().Join(CosmicQuery<Holon>.Create(),
            row => CosmicQueryMetadata.Value<int?>(row, name),
            row => CosmicQueryMetadata.Value<int?>(row, "other"));
        name = "mutated";
        result.IsError.Should().BeFalse(result.Message);
        result.Result.Keys.Single().Left.ValueType.Should().Be(typeof(int?));
        result.Result.Keys.Single().Left.Segments.Should().Equal("tenant");
        result.Result.Keys.Single().Right.Segments.Should().Equal("other");
    }

    [Fact]
    public void NumericKeyConversionCannotSilentlyChangeTheExactDomain()
    {
        var result = CosmicQuery<Holon>.Create().Join(CosmicQuery<Holon>.Create(),
            row => (long)row.Version, row => (long)row.Version);
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void NullSourceIsAnExplicitResultError()
    {
        CosmicQuery<Holon> missing = null!;
        CosmicQuery<Holon>.Create().Join(missing, row => row.Id, row => row.Id)
            .ErrorCode.Should().Be("CQL_JOIN_INVALID");
    }
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void EveryIndependentBudgetRejectsInvalidBounds(int index)
    {
        var result = index switch {
            0 => CosmicJoinOptions.Create(maximumLeftRows:0),
            1 => CosmicJoinOptions.Create(maximumRightRows:10001),
            2 => CosmicJoinOptions.Create(maximumPairs:0),
            3 => CosmicJoinOptions.Create(maximumPairs:CosmicJoinOptions.MaximumPairLimit+1),
            4 => CosmicJoinOptions.Create(maximumResults:0),
            5 => CosmicJoinOptions.Create(maximumLeftPayloadBytes:0),
            6 => CosmicJoinOptions.Create(maximumRightPayloadBytes:0),
            _ => CosmicJoinOptions.Create(maximumPayloadBytes:CosmicProjectionResults.MaximumPayloadLimit+1) };
        result.ErrorCode.Should().Be("CQL_JOIN_INVALID");
    }
}
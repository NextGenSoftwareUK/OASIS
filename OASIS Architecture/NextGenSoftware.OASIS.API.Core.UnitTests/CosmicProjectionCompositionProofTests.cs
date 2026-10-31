using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicProjectionCompositionProofTests
{
    private static readonly Guid Actor = Guid.Parse("10000000-0000-0000-0000-000000000000");
    private static readonly Guid A = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid B = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid C = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static CosmicProjectionQuery<Holon, Snapshot> Selection() => CosmicQuery<Holon>.Create()
        .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Actor).Result).Take(3)
        .Select(h => new Snapshot { Name = h.Name, Revision = h.Version }).Result;
    private static CosmicProjectionInputRow Input(Guid id, string name, object revision, bool accessible = true)
        => new(id, accessible, accessible ? Actor : Guid.Empty, new[] { (object)name, revision }, Array.Empty<object>());
    private static CosmicProjectionInputRow[] Inputs() => new[] { Input(A, "C", 2), Input(B, "A", 1), Input(C, "B", 2) };
    private static CosmicProjectionStageProof Stage(params Guid[] ids) => new(ids);
    private static CosmicProjectionQuery<Holon, Snapshot> Query() => Selection().Where(row => row.Revision >= 2).OrderBy(row => row.Name).Take(1).Result;
    private static CosmicProjectionCompositionPacket Packet(CosmicProjectionInputRow[]? input = null,
        CosmicProjectionStageProof[]? stages = null, CosmicProjectionCompositionRow[]? output = null, long sourceCount = 3, long work = 7)
        => new(input ?? Inputs(), stages ?? new[] { Stage(A, C), Stage(C, A), Stage(C) },
            output ?? new[] { new CosmicProjectionCompositionRow(C, "B", 2) }, sourceCount, work);

    [Fact]
    public void CompleteNativeProofPublishesOnlyAfterEveryStageAndExactPayloadValidate()
    {
        Snapshot.Calls = 0;
        var result = CosmicProjectionCompositionResults.Materialize(Query(), Packet());
        Assert.False(result.IsError, result.Message);
        Assert.Equal("B", result.Result.Single().Name);
        Assert.Equal(1, Snapshot.Calls);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void AlteredNativeFilterOrderingOrWindowCannotConstructAnyDto(int stage)
    {
        Snapshot.Calls = 0;
        var stages = new[] { Stage(A, C), Stage(C, A), Stage(C) };
        stages[stage] = stage == 0 ? Stage(B, C) : stage == 1 ? Stage(A, C) : Stage(A);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(Query(), Packet(stages: stages)).ErrorCode);
        Assert.Equal(0, Snapshot.Calls);
    }

    [Fact]
    public void FilterAfterTakeCannotBePushedBeforeTheWindow()
    {
        var query = Selection().Take(1).Where(row => row.Name == "B").Result;
        var valid = new CosmicProjectionCompositionPacket(Inputs(), new[] { Stage(A), Stage() }, Array.Empty<CosmicProjectionCompositionRow>(), 3, 4);
        Assert.False(CosmicProjectionCompositionResults.Materialize(query, valid).IsError);
        var pushed = new CosmicProjectionCompositionPacket(Inputs(), new[] { Stage(C), Stage(C) }, new[] { new CosmicProjectionCompositionRow(C, "B", 2) }, 3, 4);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(query, pushed).ErrorCode);
    }

    [Fact]
    public void LaterSortKeepsPreviousStageRankForEqualKeys()
    {
        var query = Selection().OrderBy(row => row.Name).OrderBy(row => row.Revision).Result;
        var rows = new[] { new CosmicProjectionCompositionRow(B, "A", 1), new CosmicProjectionCompositionRow(C, "B", 2), new CosmicProjectionCompositionRow(A, "C", 2) };
        var valid = new CosmicProjectionCompositionPacket(Inputs(), new[] { Stage(B, C, A), Stage(B, C, A) }, rows, 3, 6);
        Assert.False(CosmicProjectionCompositionResults.Materialize(query, valid).IsError);
        var unstable = new CosmicProjectionCompositionPacket(Inputs(), new[] { Stage(B, C, A), Stage(B, A, C) }, rows, 3, 6);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(query, unstable).ErrorCode);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void EmptyFinalPageCannotHideMalformedOrInaccessibleInput(bool malformed)
    {
        Snapshot.Calls = 0;
        var query = Selection().Take(0).Result;
        var input = Inputs(); input[1] = Input(B, "A", malformed ? (object)"bad" : 1, accessible: malformed);
        var packet = new CosmicProjectionCompositionPacket(input, new[] { Stage() }, Array.Empty<CosmicProjectionCompositionRow>(), 3, 3);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(query, packet).ErrorCode);
        Assert.Equal(0, Snapshot.Calls);
    }

    [Fact]
    public void InputStageAndOutputIdentitiesAndCountsMustAllAgree()
    {
        var query = Query();
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(query, Packet(sourceCount: 2)).ErrorCode);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(query, Packet(work: 6)).ErrorCode);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(query, Packet(stages: new[] { Stage(A, C) })).ErrorCode);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(query, Packet(output: new[] { new CosmicProjectionCompositionRow(A, "B", 2) })).ErrorCode);
        var duplicates = Inputs(); duplicates[1] = Input(A, "A", 1);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(query, Packet(input: duplicates)).ErrorCode);
    }

    [Fact]
    public void OutputCannotChangeValuesOrDiscardRequiredColumns()
    {
        Snapshot.Calls = 0;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(Query(), Packet(output: new[] { new CosmicProjectionCompositionRow(C, "changed", 2) })).ErrorCode);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(Query(), Packet(output: new[] { new CosmicProjectionCompositionRow(C, "B") })).ErrorCode);
        Assert.Equal(0, Snapshot.Calls);
    }

    [Theory]
    [InlineData(2, 1000, 50, 1000)] [InlineData(3, 1, 50, 1000)]
    [InlineData(3, 1000, 6, 1000)] [InlineData(3, 1000, 50, 79)]
    public void InputBytesRowsWorkAndStageProofBytesHaveIndependentBudgets(int rows, int bytes, int work, int proofBytes)
    {
        var options = new CosmicProjectionCompositionOptions(rows, bytes, work, proofBytes);
        Assert.Equal("CQL_RESULT_LIMIT", CosmicProjectionCompositionResults.Materialize(Query(), Packet(), options).ErrorCode);
    }

    [Fact]
    public void PublishedOutputCountAndBytesCannotBeConfusedWithProofLimits()
    {
        Assert.Equal("CQL_RESULT_LIMIT", CosmicProjectionCompositionResults.Materialize(Query(), Packet(), maximumResults: 0).ErrorCode);
        Assert.Equal("CQL_RESULT_LIMIT", CosmicProjectionCompositionResults.Materialize(Query(), Packet(), maximumPayloadBytes: 1).ErrorCode);
    }

    [Fact]
    public void CancellationDoesNotConstructDtos()
    {
        Snapshot.Calls = 0;
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Equal("CQL_QUERY_CANCELLED", CosmicProjectionCompositionResults.Materialize(Query(), Packet(), cancellationToken: cancellation.Token).ErrorCode);
        Assert.Equal(0, Snapshot.Calls);
    }

    [Fact]
    public void SignedZeroAndDecimalRepresentationArePayloadIdentity()
    {
        var doubles = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<double>(h, "reading")).Take(1).Result;
        var negativeZero = BitConverter.Int64BitsToDouble(long.MinValue);
        var input = new[] { new CosmicProjectionInputRow(A, true, Actor, new object[] { negativeZero }, Array.Empty<object>()) };
        var changed = new CosmicProjectionCompositionPacket(input, new[] { Stage(A) }, new[] { new CosmicProjectionCompositionRow(A, 0d) }, 1, 1);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(doubles, changed).ErrorCode);
        var decimals = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<decimal>(h, "price")).Take(1).Result;
        var scaled = new[] { new CosmicProjectionInputRow(A, true, Actor, new object[] { 1.00m }, Array.Empty<object>()) };
        var normalized = new CosmicProjectionCompositionPacket(scaled, new[] { Stage(A) }, new[] { new CosmicProjectionCompositionRow(A, 1m) }, 1, 1);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(decimals, normalized).ErrorCode);
    }

    [Fact]
    public void NullableScalarAndRuntimeRecordUseTheSameProofBarrier()
    {
        var scalar = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<int?>(h, "value")).Where(value => value == null).Result;
        var input = new[] { new CosmicProjectionInputRow(A, true, Actor, new object[] { null! }, Array.Empty<object>()) };
        var packet = new CosmicProjectionCompositionPacket(input, new[] { Stage(A) }, new[] { new CosmicProjectionCompositionRow(A, new object[] { null! }) }, 1, 1);
        Assert.Null(CosmicProjectionCompositionResults.Materialize(scalar, packet).Result.Single());
        var record = CosmicQuery<Holon>.Create().SelectColumns(new[] { CosmicProjectionSelection<Holon>.Create("revision", h => h.Version).Result }).Where(row => (int)row["revision"] == 2).Result;
        var numbers = new[] { new CosmicProjectionInputRow(A, true, Actor, new object[] { 2 }, Array.Empty<object>()) };
        var valid = new CosmicProjectionCompositionPacket(numbers, new[] { Stage(A) }, new[] { new CosmicProjectionCompositionRow(A, 2) }, 1, 1);
        Assert.Equal(2, CosmicProjectionCompositionResults.Materialize(record, valid).Result.Single()["revision"]);
    }

    [Fact]
    public void CanonicalSelectedIdentityCannotContradictInputProof()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Id).Take(1).Result;
        var input = new[] { new CosmicProjectionInputRow(A, true, Actor, new object[] { B }, Array.Empty<object>()) };
        var packet = new CosmicProjectionCompositionPacket(input, new[] { Stage(A) }, new[] { new CosmicProjectionCompositionRow(A, B) }, 1, 1);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(query, packet).ErrorCode);
    }

    [Fact]
    public void SourceOrderingProofCannotInventCanonicalMissingOrConflictingValues()
    {
        var query = CosmicQuery<Holon>.Create().OrderBy(h => h.Name).Select(h => h.Name).Take(1).Result;
        foreach (var state in new[] { 0, 1, 3 })
        {
            var input = new[] { new CosmicProjectionInputRow(A, true, Actor, new object[] { "coffee" }, new object[] { null! }, new[] { state }) };
            var packet = new CosmicProjectionCompositionPacket(input, new[] { Stage(A) }, new[] { new CosmicProjectionCompositionRow(A, "coffee") }, 1, 1);
            Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(query, packet).ErrorCode);
        }
        var conflicting = new[] { new CosmicProjectionInputRow(A, true, Actor, new object[] { "coffee" }, new object[] { "tea" }) };
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", CosmicProjectionCompositionResults.Materialize(query,
            new CosmicProjectionCompositionPacket(conflicting, new[] { Stage(A) }, new[] { new CosmicProjectionCompositionRow(A, "coffee") }, 1, 1)).ErrorCode);
    }

    [Fact]
    public void ProofSnapshotsRemainDetachedFromCallerArrays()
    {
        var ids = new[] { C }; var stage = Stage(ids); ids[0] = A;
        Assert.Equal(C, stage.Identities.Single());
        var values = new object[] { "B", 2 }; var input = new CosmicProjectionInputRow(C, true, Actor, values, Array.Empty<object>()); values[0] = "changed";
        Assert.Equal("B", input.Values[0]);
    }

    public sealed class Snapshot
    {
        public static int Calls;
        public Snapshot() { Calls++; }
        public string Name { get; set; } = string.Empty;
        public int Revision { get; set; }
    }
}

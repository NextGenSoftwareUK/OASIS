using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicProjectionComputedCompositionProofTests
{
    public sealed class LengthRow
    {
        public string Name { get; set; } = "";
        public int Units { get; set; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ComputedOutputUsesItsOwnDomainWithoutConflictingWithRawSource(bool malformed)
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var query = CosmicQuery<Holon>.Create().Take(1)
            .Select(row => new LengthRow { Name = row.Name, Units = row.Name.Length })
            .Where(row => row.Units > 0).Result;
        object units = malformed ? (object)"3" : 3;
        var packet = new CosmicProjectionCompositionPacket(
            new[] { new CosmicProjectionInputRow(id, true, Guid.Empty,
                new object[] { "abc", units }, Array.Empty<object>()) },
            new[] { new CosmicProjectionStageProof(new[] { id }) },
            new[] { new CosmicProjectionCompositionRow(id, "abc", units) }, 1, 1);
        var result = CosmicProjectionCompositionResults.Materialize(query, packet);
        if (malformed)
            Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode);
        else
        {
            Assert.False(result.IsError, result.Message);
            Assert.Equal("abc", result.Result.Single().Name);
            Assert.Equal(3, result.Result.Single().Units);
        }
    }
}

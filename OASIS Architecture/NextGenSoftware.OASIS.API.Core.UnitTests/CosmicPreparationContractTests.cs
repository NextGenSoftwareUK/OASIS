using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Security;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicPreparationContractTests
{
    private static ProtectedHolonPreparation Record(Guid? id = null, string? envelope = null)
        => new(id ?? Guid.NewGuid(), envelope ?? HolonPreparationCipher.Prefix + "opaque");
    [Fact]
    public void CandidateOwnsItsOrderedRecordSnapshot()
    {
        var first = Record(); var second = Record(); var records = new[] { first, second };
        var value = new HolonWritePreparation(Guid.NewGuid(), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS, records);
        records[0] = Record(); Assert.Same(first, value.Records[0]); Assert.Same(second, value.Records[1]);
        Assert.Throws<NotSupportedException>(() => ((IList<ProtectedHolonPreparation>)value.Records).Clear());
    }
    [Fact]
    public void CandidateRejectsDuplicateOrNullNodesAndEmptyIdentities()
    {
        var record = Record();
        Assert.Throws<ArgumentException>(() => new HolonWritePreparation(Guid.NewGuid(), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS, new[] { record, record }));
        Assert.Throws<ArgumentException>(() => new HolonWritePreparation(Guid.NewGuid(), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS, new ProtectedHolonPreparation[] { null! }));
        Assert.Throws<ArgumentException>(() => new HolonWritePreparation(Guid.Empty, Guid.NewGuid(), ProviderType.SQLLiteDBOASIS, new[] { record }));
        Assert.Throws<ArgumentException>(() => new HolonWritePreparation(Guid.NewGuid(), Guid.Empty, ProviderType.SQLLiteDBOASIS, new[] { record }));
        Assert.Throws<ArgumentException>(() => Record(Guid.Empty));
    }
    [Fact]
    public void CandidateBoundsCountAndTotalPayload()
    {
        Assert.Throws<ArgumentException>(() => new HolonWritePreparation(Guid.NewGuid(), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS, Array.Empty<ProtectedHolonPreparation>()));
        Assert.Throws<ArgumentException>(() => new HolonWritePreparation(Guid.NewGuid(), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS,
            new ProtectedHolonPreparation[HolonWritePreparation.MaximumRecords + 1]));
        var envelope = HolonPreparationCipher.Prefix + new string('x', ProtectedHolonPreparation.MaximumEnvelopeCharacters - HolonPreparationCipher.Prefix.Length);
        var records = Enumerable.Range(0, 9).Select(_ => Record(envelope: envelope)).ToArray();
        Assert.Throws<ArgumentException>(() => new HolonWritePreparation(Guid.NewGuid(), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS, records));
        Assert.Throws<ArgumentException>(() => Record(envelope: envelope + "x"));
        Assert.Throws<ArgumentException>(() => Record(envelope: "oasis2:{}"));
        Assert.Throws<ArgumentException>(() => Record(envelope: HolonPreparationCipher.Prefix + "\ud800"));
    }
}

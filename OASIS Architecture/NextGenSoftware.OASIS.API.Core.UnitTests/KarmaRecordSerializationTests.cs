using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class KarmaRecordSerializationTests
{
    [Fact]
    public void AvatarDetailCanonicalProjectionPreservesKarmaHistory()
    {
        var source=new AvatarDetail {Id=Guid.NewGuid(),Karma=500};
        source.KarmaAkashicRecords.Add(new KarmaAkashicRecord {AvatarId=source.Id,Karma=500,TotalKarma=500});
        var projected=StoredRecordJson.Project<AvatarDetail>(source);
        Assert.Single(projected.KarmaAkashicRecords);
        var loaded=StoredRecordJson.DeserializeStored<AvatarDetail>(StoredRecordJson.Serialize(projected));
        Assert.Single(loaded.KarmaAkashicRecords);
        Assert.Equal(source.Id,loaded.KarmaAkashicRecords[0].AvatarId);
        Assert.Equal(500,loaded.KarmaAkashicRecords[0].TotalKarma);
    }
}

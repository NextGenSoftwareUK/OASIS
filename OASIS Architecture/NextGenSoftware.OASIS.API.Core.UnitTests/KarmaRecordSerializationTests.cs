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
        using var metadata=System.Text.Json.JsonDocument.Parse("[{\"Amount\":500}]");
        var history=Assert.IsType<Newtonsoft.Json.Linq.JArray>(StoredRecordJson.CaptureRuntimeToken(metadata.RootElement));
        Assert.Single(history);
        Assert.Equal(500,(int)history[0]["Amount"]!);
        var typedHistory=new List<Dictionary<string,object>> {new() {{"amount",500L}}};
        using var storedMetadata=System.Text.Json.JsonDocument.Parse(StoredRecordJson.Serialize<object>(typedHistory));
        var decoded=StoredRecordJson.Deserialize<List<Dictionary<string,object>>>(StoredRecordJson.Serialize(storedMetadata.RootElement));
        Assert.Single(decoded);
        Assert.Equal(500L,Convert.ToInt64(decoded[0]["amount"]));
    }
}

using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Newtonsoft.Json.Linq;

public class CosmicAuditDateSentinelTests
{
    public static IEnumerable<object[]> RecordKinds()
    {
        foreach (var record in new[] { 0, 1, 2 })
            foreach (var kind in new[] { DateTimeKind.Unspecified, DateTimeKind.Utc, DateTimeKind.Local })
                yield return new object[] { record, kind };
    }

    [Theory]
    [MemberData(nameof(RecordKinds))]
    public void CanonicalAuditAbsenceSurvivesEveryCaptureRoute(int record, DateTimeKind kind)
    {
        var absent = DateTime.SpecifyKind(DateTime.MinValue, kind);
        IHolonBase source = record switch { 0 => new Holon(), 1 => new Avatar(), _ => new AvatarDetail() };
        source.CreatedDate = absent; source.ModifiedDate = absent; source.DeletedDate = absent;
        var json = StoredRecordJson.Serialize<IHolonBase>(source);
        var input = StoredRecordJson.SerializeInput<IHolonBase>(source);
        var runtime = StoredRecordJson.CaptureRuntimeToken(source).ToString();
        foreach (var representation in new[] { json, input, runtime })
        {
            var loaded = (IHolonBase)StoredRecordJson.Deserialize(representation, source.GetType());
            Assert.Equal(0, loaded.CreatedDate.Ticks);
            Assert.Equal(0, loaded.ModifiedDate.Ticks);
            Assert.Equal(0, loaded.DeletedDate.Ticks);
        }
        Assert.Equal(kind, source.DeletedDate.Kind);
    }

    [Theory]
    [MemberData(nameof(RecordKinds))]
    public void RealAuditDatesAndMetadataKeepExistingUtcPolicy(int record, DateTimeKind kind)
    {
        var real = new DateTime(2026, 10, 20, 12, 34, 56, kind).AddTicks(1234567);
        var expected = kind == DateTimeKind.Local ? real.ToUniversalTime() : DateTime.SpecifyKind(real, DateTimeKind.Utc);
        IHolonBase source = record switch { 0 => new Holon(), 1 => new Avatar(), _ => new AvatarDetail() };
        source.CreatedDate = real; source.ModifiedDate = real; source.DeletedDate = real;
        var minimum = DateTime.SpecifyKind(DateTime.MinValue, kind);
        var metadataMinimum = kind == DateTimeKind.Local ? minimum.ToUniversalTime() : DateTime.SpecifyKind(minimum, DateTimeKind.Utc);
        source.MetaData = new() { ["date"] = real, ["minimum"] = minimum };
        foreach (var token in new[] { StoredRecordJson.CaptureInputToken<IHolonBase>(source), StoredRecordJson.CaptureRuntimeToken(source) })
        {
            foreach (var name in new[] { "CreatedDate", "ModifiedDate", "DeletedDate" })
                Assert.Equal(expected.Ticks, token[name]!.Value<DateTime>().Ticks);
            Assert.Equal(expected.Ticks, token["MetaData"]!["date"]!.Value<DateTime>().Ticks);
            Assert.Equal(metadataMinimum.Ticks, token["MetaData"]!["minimum"]!.Value<DateTime>().Ticks);
        }
        Assert.Equal(real.Ticks, source.DeletedDate.Ticks);
        Assert.Equal(kind, source.DeletedDate.Kind);
    }
}

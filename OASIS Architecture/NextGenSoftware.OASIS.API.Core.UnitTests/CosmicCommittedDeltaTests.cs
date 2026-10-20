using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicCommittedDeltaTests
{
    [Fact]
    public void StoredTypedIntegerArrayCanBeProjectedWithoutRawTokenReaderFailure()
    {
        var type = typeof(int[]).AssemblyQualifiedName!;
        using var json = System.Text.Json.JsonDocument.Parse(new JObject { ["$type"] = type, ["$values"] = new JArray(1, 2) }.ToString());
        var source = new Holon { MetaData = new() { ["array"] = json.RootElement.Clone() } };
        var projected = StoredRecordJson.Project<Holon>(source);
        Assert.Equal(new[] { 1, 2 }, Assert.IsType<int[]>(projected.MetaData["array"]));
    }
    private static Holon Row(Guid id, int version, Guid previous)
    {
        var row = new Holon { Id = id, Version = version, VersionId = Guid.NewGuid(), PreviousVersionId = previous, Name = "original" };
        CosmicConcurrency.SetProviderRevisionId(row, ProviderType.SQLLiteDBOASIS, Guid.NewGuid()); return row;
    }
    private static HolonCommittedDelta Capture(Holon previous, Holon current, bool retain = true, ConditionalHolonOperation operation = ConditionalHolonOperation.Save)
        => HolonCommittedDelta.Capture(Guid.NewGuid(), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS, operation,
            new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc), new[] { previous }, new[] { current }, retain);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CapturedValuesAreDetachedPreciseAndPublicSummaryIsRedacted(bool retain)
    {
        var previous = Row(Guid.NewGuid(), 1, Guid.Empty); var current = Row(previous.Id, 2, previous.VersionId);
        previous.MetaData["amount"] = 1.000000000000000000000000001m;
        current.MetaData["amount"] = 1.000000000000000000000000002m;
        previous.MetaData["removed"] = "private prior value"; current.MetaData["addedNull"] = null!;
        current.Name = "private current value";
        var captured = Capture(previous, current, retain); var stored = captured.StoredJson; var summary = captured.SummaryJson;
        Assert.Equal(retain, stored.Contains("private prior value"));
        Assert.False(summary.Contains("private prior value")); Assert.False(summary.Contains("private current value"));
        if (retain) { Assert.Contains("1.000000000000000000000000001", stored); Assert.Contains("1.000000000000000000000000002", stored); }
        previous.MetaData.Clear(); current.Name = "later unsaved edit";
        Assert.Equal(stored, captured.StoredJson); Assert.Equal(summary, captured.SummaryJson);
        Assert.Equal(summary, HolonCommittedDelta.ReadStored(stored).SummaryJson);
        Assert.Throws<System.IO.InvalidDataException>(() => HolonCommittedDelta.ReadStored(summary));
    }

    [Fact]
    public void MembershipRemovalIsSeparateFromPhysicalEntityDeletion()
    {
        var previous = Row(Guid.NewGuid(), 1, Guid.Empty); var current = Row(previous.Id, 2, previous.VersionId);
        var removed = Guid.NewGuid(); var retained = Guid.NewGuid();
        previous.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1, previous.Id,
            new[] { new HolonChildRole("Children", new[] { removed, retained }) }));
        current.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1, previous.Id,
            new[] { new HolonChildRole("Children", new[] { retained }) }));
        var packet = Capture(previous, current); var record = JObject.Parse(packet.StoredJson)["records"]!.Single();
        Assert.Equal("Updated", record.Value<string>("kind")); Assert.Equal(2, record.Value<int>("relationshipChangeCount"));
        Assert.Single((JArray)JObject.Parse(packet.StoredJson)["records"]!);
        Assert.Contains((JArray)record["relationships"]!, link => link.Value<string>("childId") == removed.ToString("D") && link.Value<string>("change") == "Unlinked");
        Assert.DoesNotContain(removed.ToString("D"), packet.SummaryJson);
        current.IsActive = false; current.DeletedDate = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);
        var deleted = Capture(previous, current, operation: ConditionalHolonOperation.HardDelete);
        var deletion = JObject.Parse(deleted.StoredJson)["records"]!.Single();
        Assert.Equal("HardDeleted", deletion.Value<string>("kind")); Assert.False(deletion.Value<bool>("currentExists"));
        Assert.All((JArray)deletion["changes"]!, change => Assert.Equal("Removed", change.Value<string>("change")));
    }

    [Fact]
    public void UnobservedMembershipIsNeverTreatedAsAnEmptyCollection()
    {
        var previous = Row(Guid.NewGuid(), 1, Guid.Empty); var current = Row(previous.Id, 2, previous.VersionId);
        current.ChildRoleManifestJson = HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1, current.Id,
            new[] { new HolonChildRole("Children", new[] { Guid.NewGuid() }) }));
        var record = JObject.Parse(Capture(previous, current).StoredJson)["records"]!.Single();
        Assert.False(record.Value<bool>("relationshipsObserved")); Assert.Equal(0, record.Value<int>("relationshipChangeCount"));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void InvalidStoredDeltaAndForeignAcknowledgementsAreRejected(int selection)
    {
        var previous = Row(Guid.NewGuid(), 1, Guid.Empty); var current = Row(previous.Id, 2, previous.VersionId);
        var packet = Capture(previous, current); var json = JObject.Parse(packet.StoredJson);
        // Keep the timestamp a literal string rather than the default JObject date inference.
        json["committedAtUtc"] = "2026-10-20T00:00:00.0000000Z";
        switch (selection)
        {
            case 0: json["format"] = 99; break;
            case 1: json["operation"] = "0"; break;
            case 2: json["committedAtUtc"] = "2026-10-20T00:00:00.0000000"; break;
            case 3: ((JArray)json["records"]!).Add(json["records"]!.First!.DeepClone()); break;
            default: json["records"]!.First!["changes"]!.First!["path"] = "/bad~escape"; break;
        }
        Assert.Throws<System.IO.InvalidDataException>(() => HolonCommittedDelta.ReadStored(json.ToString(Newtonsoft.Json.Formatting.None)));
        Assert.Throws<System.IO.InvalidDataException>(() => packet.ValidateAcknowledgement(Guid.NewGuid(), packet.ActorId,
            packet.Authority, packet.Operation, new[] { current }, new[] { new HolonWriteCondition(current.Id, CosmicConcurrency.GetProviderRevisionId(previous, packet.Authority)) }));
    }
}

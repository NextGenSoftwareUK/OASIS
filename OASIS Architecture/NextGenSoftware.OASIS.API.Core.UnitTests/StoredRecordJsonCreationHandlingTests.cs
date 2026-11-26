using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;

public class StoredRecordJsonCreationHandlingTests
{
    // Regression for 16d72a74: Replace on an interface-typed non-collection property (AvatarDetail.Chakras)
    // made Newtonsoft try to construct the interface; the constructor instance must be populated instead.
    [Fact]
    public void SystemTextJsonAvatarDetailRoundTripsThroughDeserializeStored()
    {
        var detail = new AvatarDetail { Id = Guid.NewGuid(), Karma = 17, XP = 31 };
        var json = System.Text.Json.JsonSerializer.Serialize(detail, new System.Text.Json.JsonSerializerOptions
            { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
        var loaded = StoredRecordJson.DeserializeStored<AvatarDetail>(json);
        Assert.Equal(17, loaded.Karma);
        Assert.Equal(31, loaded.XP);
        Assert.NotNull(loaded.Chakras);
    }

    // The collection half of the same invariant: a snapshot replaces, never appends to, an initializer list.
    [Fact]
    public void CustomCollectionSnapshotDoesNotDuplicateInitializerDefaults()
    {
        var source = new TaggedHolon { Id = Guid.NewGuid() };
        source.Tags.Clear(); source.Tags.Add("submitted");
        var loaded = StoredRecordJson.Deserialize<TaggedHolon>(StoredRecordJson.Serialize(source));
        Assert.Equal(new[] { "submitted" }, loaded.Tags);
    }

    public sealed class TaggedHolon : Holon
    {
        public List<string> Tags { get; set; } = new() { "default" };
    }
}

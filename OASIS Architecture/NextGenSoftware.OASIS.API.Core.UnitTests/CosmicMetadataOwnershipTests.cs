using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicMetadataOwnershipTests
{
    [Theory]
    [InlineData("Id")] [InlineData("nAmE")] [InlineData("CreatedByAvatarId")] [InlineData("IsPublic")]
    [InlineData("Version")] [InlineData("ProviderUniqueStorageKey")] [InlineData("MetaData")]
    public void V2MetadataCannotOverwriteAuthoritativeRecordOrRuntimeState(string field)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var manager = new HolonManager(null, dna, new ProviderManager(null, dna));
        var row = new Holon { Id = Guid.NewGuid(), Name = "canonical", CreatedByAvatarId = Guid.NewGuid(), Version = 7, IsPublic = false };
        var id = row.Id; var actor = row.CreatedByAvatarId; var metadata = row.MetaData; var keys = row.ProviderUniqueStorageKey;
        metadata[field] = field switch {
            "Id" or "CreatedByAvatarId" => Guid.NewGuid(), "nAmE" => "forged", "IsPublic" => true, "Version" => 999,
            "MetaData" => new Dictionary<string, object>(), _ => new Dictionary<ProviderType, string>()
        };
        manager.MapMetaData<Holon>(row);
        Assert.Equal(id, row.Id); Assert.Equal(actor, row.CreatedByAvatarId); Assert.Equal("canonical", row.Name);
        Assert.False(row.IsPublic); Assert.Equal(7, row.Version); Assert.Same(metadata, row.MetaData); Assert.Same(keys, row.ProviderUniqueStorageKey);
        Assert.True(row.MetaData.ContainsKey(field));
    }

    public sealed class CustomRow : Holon { [CustomOASISProperty] public int Rating { get; set; } }
    [Fact]
    public void V2CustomMetadataStillHydratesAlongsideProtectedCanonicalValues()
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var manager = new HolonManager(null, dna, new ProviderManager(null, dna));
        var row = new CustomRow { Id = Guid.NewGuid(), Name = "canonical", MetaData = new() { ["Rating"] = 7, ["Name"] = "forged" } };
        manager.MapMetaData<CustomRow>(row);
        Assert.Equal(7, row.Rating); Assert.Equal("canonical", row.Name);
    }
}

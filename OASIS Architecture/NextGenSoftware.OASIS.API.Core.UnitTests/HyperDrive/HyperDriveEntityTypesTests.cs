using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using Xunit;
using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public class HyperDriveEntityTypesTests
{
    [Theory]
    [InlineData(HyperDriveEntityTypes.Holon)]
    [InlineData(HyperDriveEntityTypes.Avatar)]
    [InlineData(HyperDriveEntityTypes.AvatarDetail)]
    [InlineData(HyperDriveEntityTypes.AvatarPreferences)]
    [InlineData(HyperDriveEntityTypes.Quest)]
    [InlineData(HyperDriveEntityTypes.QuestProgress)]
    [InlineData(HyperDriveEntityTypes.InventoryItem)]
    [InlineData(HyperDriveEntityTypes.Nft)]
    [InlineData(HyperDriveEntityTypes.NftCollection)]
    [InlineData(HyperDriveEntityTypes.GeoNft)]
    [InlineData(HyperDriveEntityTypes.GeoNftCollection)]
    [InlineData(HyperDriveEntityTypes.GeoHotSpot)]
    public void VersionedBuiltInTypeHasHostedDomainProjection(string entityType)
    {
        Assert.True(HyperDriveEntityTypes.IsHostedDomainType(entityType));
        Assert.EndsWith(".v1", entityType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("holon")]
    [InlineData("quest-progress")]
    [InlineData("custom.application.v1")]
    public void LegacyOrCustomTypeDoesNotImplicitlyClaimDomainProjection(string? entityType)
    {
        Assert.False(HyperDriveEntityTypes.IsHostedDomainType(entityType));
    }

    [Fact]
    public void EdgeAvatarProjectionCannotCarryAuthenticationOrProviderSecrets()
    {
        string[] forbidden =
        {
            "Password", "JwtToken", "RefreshToken", "RefreshTokens", "ResetToken",
            "VerificationToken", "ProviderWallets", "ProviderUsername", "ProviderUniqueStorageKey",
            "VoiceprintId", "HerzVoiceprintId"
        };
        string[] properties = typeof(HyperDriveAvatarProjection).GetProperties()
            .Select(x => x.Name).ToArray();
        Assert.All(forbidden, name => Assert.DoesNotContain(name, properties, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void PortableInventoryItemTypeWireContractMatchesFullCore()
    {
        var core = Enum.GetValues<InventoryItemType>().Select(value => (value.ToString(), (int)value));
        var portable = Enum.GetValues<HyperDriveInventoryItemType>().Select(value => (value.ToString(), (int)value));
        Assert.Equal(core, portable);
    }
}

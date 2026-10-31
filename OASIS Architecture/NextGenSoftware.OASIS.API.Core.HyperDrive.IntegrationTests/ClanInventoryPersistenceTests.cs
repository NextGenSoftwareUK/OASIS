using FluentAssertions;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class ClanInventoryPersistenceTests
{
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void PersistedClanItemRetainsPackageAndMediaUris(bool relative)
    {
        var item=new InventoryItem {Id=Guid.NewGuid(),Name="owned-clan-package",Quantity=3,
            PublishedSTARNETHolon=new byte[] {0,255,19},
            Image2DURI=new Uri(relative ? "images/seed.png":"https://example.test/seed.png",UriKind.RelativeOrAbsolute),
            Object3DURI=new Uri(relative ? "models/seed.glb":"https://example.test/seed.glb",UriKind.RelativeOrAbsolute)};
        var stored=JsonConvert.DeserializeObject<ClanManager.ClanPersistentInventoryItem>(
            JsonConvert.SerializeObject(ClanManager.ToPersistentInventoryItem(item)))!;
        var loaded=ClanManager.FromPersistentInventoryItem(stored);
        loaded.Id.Should().Be(item.Id);loaded.Quantity.Should().Be(3);
        loaded.PublishedSTARNETHolon.Should().Equal(item.PublishedSTARNETHolon);
        loaded.Image2DURI.OriginalString.Should().Be(item.Image2DURI.OriginalString);
        loaded.Object3DURI.OriginalString.Should().Be(item.Object3DURI.OriginalString);
    }
}

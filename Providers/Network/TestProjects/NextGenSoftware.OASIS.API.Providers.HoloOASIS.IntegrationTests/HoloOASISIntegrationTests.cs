using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.IntegrationTests;

public class HoloOASISIntegrationTests
{
    [Fact]
    public async Task AvatarAndHolonCrud_PersistOnOfficialHolochainConductor()
    {
        string adminUri = Environment.GetEnvironmentVariable("OASIS_HOLOCHAIN_ADMIN_URI")
            ?? throw new InvalidOperationException("Set OASIS_HOLOCHAIN_ADMIN_URI to run the official conductor test.");
        var provider = new HoloOASIS(adminUri, useHoloNetwork: false);
        var activation = await provider.ActivateProviderAsync();
        Assert.False(activation.IsError, activation.Message);
        Assert.True(activation.Result);

        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = $"holo-{Guid.NewGuid():N}",
            Email = $"holo-{Guid.NewGuid():N}@oasis.test",
            FirstName = "Before"
        };
        var createdAvatar = await provider.SaveAvatarAsync(avatar);
        Assert.False(createdAvatar.IsError, createdAvatar.Message);
        var loadedAvatar = await provider.LoadAvatarAsync(avatar.Id);
        Assert.False(loadedAvatar.IsError, loadedAvatar.Message);
        Assert.Equal("Before", loadedAvatar.Result.FirstName);
        avatar.FirstName = "After";
        var updatedAvatar = await provider.SaveAvatarAsync(avatar);
        Assert.False(updatedAvatar.IsError, updatedAvatar.Message);
        loadedAvatar = await provider.LoadAvatarAsync(avatar.Id);
        Assert.False(loadedAvatar.IsError, loadedAvatar.Message);
        Assert.Equal("After", loadedAvatar.Result.FirstName);

        var holon = new Holon { Id = Guid.NewGuid(), Name = "Before" };
        var createdHolon = await provider.SaveHolonAsync(holon);
        Assert.False(createdHolon.IsError, createdHolon.Message);
        var loadedHolon = await provider.LoadHolonAsync(holon.Id);
        Assert.False(loadedHolon.IsError, loadedHolon.Message);
        Assert.Equal("Before", loadedHolon.Result.Name);
        holon.Name = "After";
        var updatedHolon = await provider.SaveHolonAsync(holon);
        Assert.False(updatedHolon.IsError, updatedHolon.Message);
        loadedHolon = await provider.LoadHolonAsync(holon.Id);
        Assert.False(loadedHolon.IsError, loadedHolon.Message);
        Assert.Equal("After", loadedHolon.Result.Name);

        var deletedAvatar = await provider.DeleteAvatarAsync(avatar.Id, false);
        Assert.False(deletedAvatar.IsError, deletedAvatar.Message);
        var deletedHolon = await provider.DeleteHolonAsync(holon.Id);
        Assert.False(deletedHolon.IsError, deletedHolon.Message);
    }
}

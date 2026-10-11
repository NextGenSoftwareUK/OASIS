using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronStorageMigrationTests
{
    [Fact]
    public async Task InheritedStorageRejectsMissingConfigurationWithoutInventingAvatarData()
    {
        var provider = new TRONOASIS();
        Assert.IsAssignableFrom<KeyValueStorageProviderBase>(provider);
        Assert.True((await provider.LoadAvatarByEmailAsync("test@example.test")).IsError);
        Assert.True((await provider.LoadAvatarDetailByUsernameAsync("test")).IsError);
        Assert.True((await provider.SaveAvatarAsync(new Avatar { Username = "test" })).IsError);
        Assert.True((await provider.SaveAvatarDetailAsync(new AvatarDetail())).IsError);
        var batch = await provider.SaveHolonsAsync(new IHolon[] { new Holon(), new Holon() }, continueOnError: true);
        Assert.True(batch.IsError);
        Assert.Empty(batch.Result);
        var import = await provider.ImportAsync(new IHolon[] { new Holon() });
        Assert.True(import.IsError);
        Assert.False(import.Result);
        Assert.False(provider.IsProviderActivated);
    }
}

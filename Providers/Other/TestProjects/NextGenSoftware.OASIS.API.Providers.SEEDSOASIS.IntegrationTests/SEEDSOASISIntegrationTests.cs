using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using Xunit;
using TelosProvider = NextGenSoftware.OASIS.API.Providers.TelosOASIS.TelosOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.IntegrationTests;

public class SEEDSOASISIntegrationTests
{
    private static SEEDSOASIS CreateProvider()
    {
        string endpoint = Require("OASIS_ANTELOPE_ENDPOINT");
        string account = Require("OASIS_ANTELOPE_ACCOUNT");
        string chainId = Require("OASIS_ANTELOPE_CHAIN_ID");
        string privateKey = Require("OASIS_ANTELOPE_PRIVATE_KEY");
        return new SEEDSOASIS(new TelosProvider(endpoint, account, chainId, privateKey));
    }

    [Fact]
    public async Task AvatarCrud_UsesInjectedTelosStorageOnOfficialAntelopeNode()
    {
        var provider = CreateProvider();
        var activation = await provider.ActivateProviderAsync();
        Assert.False(activation.IsError, activation.Message);

        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = $"seeds-{Guid.NewGuid():N}",
            Email = $"seeds-{Guid.NewGuid():N}@oasis.test",
            FirstName = "Before"
        };
        Assert.False((await provider.SaveAvatarAsync(avatar)).IsError);
        var loaded = await provider.LoadAvatarAsync(avatar.Id);
        Assert.False(loaded.IsError, loaded.Message);
        Assert.Equal("Before", loaded.Result.FirstName);

        avatar.FirstName = "After";
        Assert.False((await provider.SaveAvatarAsync(avatar)).IsError);
        Assert.Equal("After", (await provider.LoadAvatarAsync(avatar.Id)).Result.FirstName);
        Assert.False((await provider.DeleteAvatarAsync(avatar.Id, false)).IsError);
    }

    [Fact]
    public async Task HolonCrud_UsesInjectedTelosStorageOnOfficialAntelopeNode()
    {
        var provider = CreateProvider();
        var activation = await provider.ActivateProviderAsync();
        Assert.False(activation.IsError, activation.Message);

        var holon = new Holon { Id = Guid.NewGuid(), Name = "Before" };
        Assert.False((await provider.SaveHolonAsync(holon)).IsError);
        var loaded = await provider.LoadHolonAsync(holon.Id);
        Assert.False(loaded.IsError, loaded.Message);
        Assert.Equal("Before", loaded.Result.Name);

        holon.Name = "After";
        Assert.False((await provider.SaveHolonAsync(holon)).IsError);
        Assert.Equal("After", (await provider.LoadHolonAsync(holon.Id)).Result.Name);
        Assert.False((await provider.DeleteHolonAsync(holon.Id)).IsError);
    }

    [Fact]
    public async Task Activation_PropagatesAnUnreachableTelosTransportFailure()
    {
        var provider = new SEEDSOASIS(new TelosProvider("http://127.0.0.1:1", "oasis",
            new string('0', 64), "5KQwrPbwdL6PhXujxW37FSSQ8hK1mXQ6hY7WZg1v9Y4YcVYhJ7x"));
        var activation = await provider.ActivateProviderAsync();
        Assert.True(activation.IsError || !activation.Result);
        Assert.False(provider.IsProviderActivated);
    }

    private static string Require(string name) =>
        Environment.GetEnvironmentVariable(name) ??
        throw new InvalidOperationException($"Set {name} to run the official Antelope integration tests.");
}

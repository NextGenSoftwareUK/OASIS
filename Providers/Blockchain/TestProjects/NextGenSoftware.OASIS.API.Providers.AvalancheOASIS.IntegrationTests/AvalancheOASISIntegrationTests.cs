using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.AvalancheOASIS.IntegrationTests;

public class AvalancheOASISIntegrationTests
{
    private const string DevelopmentPrivateKey = "0x0000000000000000000000000000000000000000000000000000000000000001";

    [Fact]
    public async Task ActivationAndHolonCrud_UseLiveAvalancheCompatibleChainAndContract()
    {
        string? rpcUrl = Environment.GetEnvironmentVariable("OASIS_AVALANCHE_RPC_URL");
        string? privateKey = Environment.GetEnvironmentVariable("OASIS_AVALANCHE_PRIVATE_KEY");
        string? contractAddress = Environment.GetEnvironmentVariable("OASIS_AVALANCHE_CONTRACT_ADDRESS");
        Assert.False(string.IsNullOrWhiteSpace(rpcUrl), "OASIS_AVALANCHE_RPC_URL must identify the owned test runtime.");
        Assert.False(string.IsNullOrWhiteSpace(privateKey), "OASIS_AVALANCHE_PRIVATE_KEY must identify its disposable funded account.");
        Assert.False(string.IsNullOrWhiteSpace(contractAddress), "OASIS_AVALANCHE_CONTRACT_ADDRESS must identify the deployed Web3Core contract.");

        var provider = new AvalancheOASIS(rpcUrl!, privateKey!, contractAddress!);
        var activation = await provider.ActivateProviderAsync();
        Assert.False(activation.IsError, activation.Message);
        Assert.True(activation.Result);

        var holon = new Holon { Id = Guid.NewGuid(), Name = $"avalanche-{Guid.NewGuid():N}" };
        var saved = await provider.SaveHolonAsync(holon);
        var loaded = await provider.LoadHolonAsync(holon.Id);
        Assert.False(saved.IsError, saved.Message);
        Assert.False(loaded.IsError, loaded.Message);
        Assert.Equal(holon.Name, loaded.Result.Name);

        holon.Name += "-updated";
        Assert.False((await provider.SaveHolonAsync(holon)).IsError);
        Assert.Equal(holon.Name, (await provider.LoadHolonAsync(holon.Id)).Result.Name);
        Assert.False((await provider.DeleteHolonAsync(holon.Id)).IsError);
        Assert.True((await provider.LoadHolonAsync(holon.Id)).IsError);
        Assert.True((await provider.DeActivateProviderAsync()).Result);
    }

    [Fact]
    public async Task Activation_RejectsUnreachableRpc()
    {
        var provider = new AvalancheOASIS("http://127.0.0.1:1", DevelopmentPrivateKey,
            "0x0000000000000000000000000000000000000000");
        var result = await provider.ActivateProviderAsync();
        Assert.True(result.IsError);
        Assert.False(provider.IsProviderActivated);
    }
}

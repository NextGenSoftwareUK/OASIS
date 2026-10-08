using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

[TestClass]
public sealed class SOLANAOASISIntegrationTests
{
    private SolanaOASIS _provider = null!;

    [TestInitialize]
    public async Task Setup()
    {
        string rpc = Environment.GetEnvironmentVariable("SOLANA_RPC_URL")
            ?? throw new InvalidOperationException("SOLANA_RPC_URL must identify a running Solana validator.");
        string privateKey = Environment.GetEnvironmentVariable("SOLANA_PRIVATE_KEY")
            ?? throw new InvalidOperationException("SOLANA_PRIVATE_KEY is required.");
        string publicKey = Environment.GetEnvironmentVariable("SOLANA_PUBLIC_KEY")
            ?? throw new InvalidOperationException("SOLANA_PUBLIC_KEY is required.");

        _provider = new SolanaOASIS(rpc, privateKey, publicKey);
        var activation = await _provider.ActivateProviderAsync();
        Assert.IsFalse(activation.IsError, activation.Message);
        Assert.IsTrue(activation.Result);
    }

    [TestMethod]
    public async Task MemoTransactionHolon_RoundTripsThroughRealValidator()
    {
        var holon = new Holon
        {
            Id = Guid.NewGuid(),
            Name = $"Solana holon {Guid.NewGuid():N}",
            Description = "Agave validator integration",
            HolonType = HolonType.Holon
        };

        var save = await _provider.SaveHolonAsync(holon, saveChildren: false);
        Assert.IsFalse(save.IsError, save.Message);
        Assert.IsNotNull(save.Result);
        Assert.IsTrue(save.Result.ProviderUniqueStorageKey.TryGetValue(ProviderType.SolanaOASIS, out string? signature));
        Assert.IsFalse(string.IsNullOrWhiteSpace(signature));

        var load = await _provider.LoadHolonAsync(signature!, loadChildren: false);
        Assert.IsFalse(load.IsError, load.Message);
        Assert.AreEqual(holon.Id, load.Result.Id);
        Assert.AreEqual(holon.Name, load.Result.Name);
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_provider.IsProviderActivated)
            await _provider.DeActivateProviderAsync();
    }
}

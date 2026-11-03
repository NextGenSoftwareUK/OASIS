using System.Text.Json;
using CosmicCqlDemo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass, DoNotParallelize]
public class Web3OwnedCqlNativeReadTests
{
    [TestMethod]
    public async Task ExistingArbitrumV2RecordsRunManagerLinqThreeToTwoWithoutWrites()
    {
        if (Environment.GetEnvironmentVariable("OASIS_EVM_V2_READ_ONLY") != "1")
            Assert.Inconclusive("Explicit retained V2 read-only environment required.");

        var state = Environment.GetEnvironmentVariable("OASIS_EVM_V2_READ_STATE")
            ?? throw new InvalidOperationException("Select the preserved Arbitrum seed directory.");
        Assert.IsFalse(File.Exists(Path.Combine(state, "demo-seed.pending.json")), "Reconcile original pending setup first.");
        var path = Path.Combine(state, "demo-seed.json");
        var original = await File.ReadAllBytesAsync(path);
        using var seed = JsonDocument.Parse(original);
        var actor = seed.RootElement.GetProperty("Actor").GetGuid();
        var ids = seed.RootElement.GetProperty("Ids").EnumerateArray().Select(x => x.GetGuid()).ToArray();
        var contract = seed.RootElement.GetProperty("Contract").GetString()!;

        // Retained public Ganache development identity; never accepts a funded
        // production signer. The fixture must already contain compatible V2 data.
        const string publicDevKey = "0x4f3edf983ac636a65a842ce7c78d9aa706d3b113bce9c46f30d7d21715b23b1d";
        var provider = (Web3CoreOASISBaseProvider)EvmProviderFactory.CreateArbitrum(
            new ArbitrumOASISProviderSettings
            {
                ConnectionString = "http://127.0.0.1:18547", ChainPrivateKey = publicDevKey,
                ContractAddress = contract, ChainId = 31338
            }, HyperDriveModes.V2, null!);
        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        try
        {
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2 } };
            var registry = new ProviderManager(null, dna);
            var manager = new HolonManager(null, dna, registry);
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            using var output = new StringWriter();
            var counts = await EvmV2OwnedWalkthrough.RunAsync(manager, provider, ids, actor, output, deadline.Token);
            Assert.AreEqual(3, counts.Full);
            Assert.AreEqual(2, counts.Subset);
            CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(path), "Original seed must remain unchanged.");
        }
        finally
        {
            var stopped = await provider.DeActivateProviderAsync();
            Assert.IsFalse(stopped.IsError, stopped.Message);
        }
    }
}

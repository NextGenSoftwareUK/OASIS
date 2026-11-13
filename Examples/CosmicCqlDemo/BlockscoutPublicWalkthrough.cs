using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.BlockscoutOASIS;

namespace CosmicCqlDemo;

/// <summary>Query three public explorer addresses, then filter to two through
/// HolonManager and deferred LINQ. This never submits an Ethereum transaction.</summary>
public static class BlockscoutPublicWalkthrough
{
    public static async Task<(int Full, int Subset)> RunAsync(
        string configurationPath,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // Reuse the retained explorer endpoint, addresses and projection owner.
        // No API key, account, wallet or local node is created by this demo.
        var json = await File.ReadAllTextAsync(configurationPath, cancellationToken);
        using var source = BlockscoutPublicAddressMetadataSource.FromJson(json);
        using var configuration = JsonDocument.Parse(json);
        var addresses = configuration.RootElement.GetProperty("addresses")
            .EnumerateArray()
            .Select(address => address.GetString()!)
            .ToArray();

        if (addresses.Length != 3)
            throw new ArgumentException("This walkthrough requires the retained three-address catalogue.");

        var activated = await source.ActivateQuerySourceAsync(cancellationToken);
        if (activated.IsError)
            throw new InvalidOperationException(activated.Message, activated.Exception);

        // The explicit read profile stays outside the default provider registry.
        // It cannot be selected for canonical writes or replication delivery.
        var dna = new OASISDNA
        {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = HyperDriveModes.V2
            }
        };
        var registry = new ProviderManager(null, dna);
        var manager = new HolonManager(null, dna, registry);
        var actor = Guid.NewGuid();

        // The shared walkthrough builds a deferred Where(...Contains(...)).
        // CQL plans bounded BlockscoutAddress membership; only selected native
        // addresses are read. It does not scan an explorer's address database.
        var full = await NativeKeySetWalkthrough.RunAsync(
            manager, source, addresses, actor, output, cancellationToken);

        var subset = await NativeKeySetWalkthrough.RunAsync(
            manager, source, addresses.Take(2), actor, output, cancellationToken);

        if (full.Count != 3 || subset.Count != 2)
            throw new InvalidOperationException("The configured selection did not return three/two addresses.");

        foreach (var row in full)
        {
            await output.WriteLineAsync($"Address: {row.MetaData["BlockscoutAddress"]}");

            if (row.MetaData.TryGetValue("BlockscoutIndexedName", out var name))
                await output.WriteLineAsync($"Explorer name: {name}");

            if (row.MetaData.TryGetValue("BlockscoutCoinBalance", out var balance))
                await output.WriteLineAsync($"Reported native-unit balance: {balance}");
            else
                await output.WriteLineAsync("Balance is unknown in this explorer observation.");

            if (row.MetaData.TryGetValue("BlockscoutBalanceUpdatedAtBlock", out var block))
                await output.WriteLineAsync($"Balance update block: {block}\n");
            else
                await output.WriteLineAsync("No balance update block was reported.\n");
        }

        await output.WriteLineAsync("Read-only Blockscout demo complete: 3 full / 2 filtered records.");
        await output.WriteLineAsync("No transaction, source write, account, signer or Mongo copy was created. " +
            "These are explorer-reported observations, not a shared finalized snapshot or independently verified consensus. " +
            "HTTP/Mongo/browser checks have separate acceptance gates.");

        return (full.Count, subset.Count);
    }
}

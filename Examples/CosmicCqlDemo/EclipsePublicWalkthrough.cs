using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.EclipseOASIS;

namespace CosmicCqlDemo;

/// <summary>Query three configured Eclipse accounts, then filter to two through
/// HolonManager and deferred LINQ. This never submits an Ethereum transaction.</summary>
public static class EclipsePublicWalkthrough
{
    public static async Task<(int Full, int Subset)> RunAsync(
        string configurationPath,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // Reuse the retained mainnet RPC endpoint, accounts, retained genesis pin and projection owner.
        // No API key, account, wallet or local node is created by this demo.
        var json = await File.ReadAllTextAsync(configurationPath, cancellationToken);
        using var source = EclipsePublicAccountMetadataSource.FromJson(json);
        using var configuration = JsonDocument.Parse(json);
        var addresses = configuration.RootElement.GetProperty("addresses")
            .EnumerateArray()
            .Select(address => address.GetString()!)
            .ToArray();

        if (addresses.Length != 3)
            throw new ArgumentException("This walkthrough requires the retained three-account catalogue.");

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
        // CQL plans bounded EclipseAccountAddress membership; only selected native
        // accounts are read. It does not scan unrelated accounts.
        var full = await NativeKeySetWalkthrough.RunAsync(
            manager, source, addresses, actor, output, cancellationToken);

        var subset = await NativeKeySetWalkthrough.RunAsync(
            manager, source, addresses.Take(2), actor, output, cancellationToken);

        if (full.Count != 3 || subset.Count != 2)
            throw new InvalidOperationException("The configured selection did not return three/two accounts.");

        // A batch shares one node-reported finalized slot. Separate queries
        // can observe different slots; the retained genesis pin is checked each time.
        foreach (var row in full)
        {
            await output.WriteLineAsync($"Account: {row.MetaData["EclipseAccountAddress"]}");
            await output.WriteLineAsync($"Native owner: {row.MetaData["EclipseAccountOwner"]}");
            await output.WriteLineAsync($"Exact balance units: {row.MetaData["EclipseBalanceUnits"]}");
            await output.WriteLineAsync($"Currency: ETH; decimals: {row.MetaData["EclipseBalanceDecimals"]}");
            await output.WriteLineAsync($"Finalized slot: {row.MetaData["EclipseSlot"]}\n");
        }

        await output.WriteLineAsync("Read-only Eclipse demo complete: 3 full / 2 filtered records.");
        await output.WriteLineAsync(
            "These are node-reported account observations, not independently verified consensus or Ethereum settlement. " +
            "Balance units use ETH with 9 decimals, not SOL or Ethereum wei with 18 decimals. Account data is excluded. " +
            "No transaction, source write, wallet or Mongo copy was created. HTTP/Mongo/browser gates are separate.");

        return (full.Count, subset.Count);
    }
}

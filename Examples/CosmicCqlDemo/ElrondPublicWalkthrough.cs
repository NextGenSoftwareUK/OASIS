using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.ElrondOASIS;

namespace CosmicCqlDemo;

/// <summary>Read-only console demo over three configured public MultiversX accounts.
/// Uses the same manager/LINQ walkthrough as the other native-key sources.</summary>
public static class ElrondPublicWalkthrough
{
    public static async Task<(int Full, int Subset)> RunAsync(
        string configurationPath,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // Reuse the existing stable catalogue. No account, keys, runtime or
        // synthetic source data is created by this walkthrough.
        var json = await File.ReadAllTextAsync(configurationPath, cancellationToken);
        using var source = ElrondPublicAccountMetadataSource.FromJson(json);
        using var configuration = JsonDocument.Parse(json);
        var keys = configuration.RootElement.GetProperty("addresses")
            .EnumerateArray()
            .Select(key => key.GetString()!)
            .ToArray();

        if (keys.Length != 3)
            throw new ArgumentException("This demo requires the existing three-account catalogue.");

        var activated = await source.ActivateQuerySourceAsync(cancellationToken);
        if (activated.IsError)
            throw new InvalidOperationException(activated.Message, activated.Exception);

        // This explicit read profile stays outside the canonical wallet
        // registry. It cannot become the default write or replication provider.
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

        // Run the full selection, then a real two-key predicate. Contains is
        // planned as bounded native metadata membership, never a chain scan.
        var full = await NativeKeySetWalkthrough.RunAsync(
            manager, source, keys, actor, output, cancellationToken);

        var subset = await NativeKeySetWalkthrough.RunAsync(
            manager, source, keys.Take(2), actor, output, cancellationToken);

        if (full.Count != 3 || subset.Count != 2)
            throw new InvalidOperationException("The configured public selection did not return three/two records.");

        // The public indexing service exposes latest observations. No pinned
        // account snapshot or shared Mongo/chain checkpoint is implied.
        foreach (var row in full)
        {
            await output.WriteLineAsync($"Account: {row.MetaData["ElrondAccountAddress"]}");
            await output.WriteLineAsync($"Balance (base units): {row.MetaData["ElrondBalanceBaseUnits"]}");
            await output.WriteLineAsync($"Nonce: {row.MetaData["ElrondNonce"]}");
            await output.WriteLineAsync($"Shard: {row.MetaData["ElrondShard"]}\n");
        }

        await output.WriteLineAsync("Read-only MultiversX demo complete: 3 full / 2 filtered records.");
        await output.WriteLineAsync("No chain transactions or Mongo records were written. HTTP federation is a separate gate.");

        return (full.Count, subset.Count);
    }
}

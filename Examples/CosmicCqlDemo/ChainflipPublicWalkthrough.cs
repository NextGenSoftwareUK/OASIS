using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.ChainflipOASIS;

namespace CosmicCqlDemo;

/// <summary>Read-only console demo over three configured public Chainflip swapIds.
/// Uses the same manager/LINQ walkthrough as the other native-key sources.</summary>
public static class ChainflipPublicWalkthrough
{
    public static async Task<(int Full, int Subset)> RunAsync(
        string configurationPath,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // Reuse the existing stable catalogue. No account, keys, runtime or
        // synthetic source data is created by this walkthrough.
        var json = await File.ReadAllTextAsync(configurationPath, cancellationToken);
        using var source = ChainflipPublicSwapMetadataSource.FromJson(json);
        using var configuration = JsonDocument.Parse(json);
        var keys = configuration.RootElement.GetProperty("swapIds")
            .EnumerateArray()
            .Select(key => key.GetString()!)
            .ToArray();

        if (keys.Length != 3)
            throw new ArgumentException("This demo requires the existing three-swap catalogue.");

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
        // planned as bounded native metadata membership, never a swap scan.
        var full = await NativeKeySetWalkthrough.RunAsync(
            manager, source, keys, actor, output, cancellationToken);

        var subset = await NativeKeySetWalkthrough.RunAsync(
            manager, source, keys.Take(2), actor, output, cancellationToken);

        if (full.Count != 3 || subset.Count != 2)
            throw new InvalidOperationException("The configured public selection did not return three/two records.");

        // Quantities remain exact base-unit strings. An indexed protocol swap
        // need not have an external deposit; absence is not a zero amount.
        foreach (var row in full)
        {
            await output.WriteLineAsync($"Swap: {row.Name}");
            await output.WriteLineAsync($"Original swap input: {row.MetaData["ChainflipOriginalInputAmount"]}");

            if (row.MetaData.TryGetValue("ChainflipDepositAmount", out var deposit))
                await output.WriteLineAsync($"External deposit amount: {deposit}\n");
            else
                await output.WriteLineAsync("No external deposit is present in this indexed record.\n");
        }

        await output.WriteLineAsync("Read-only Chainflip demo complete: 3 full / 2 filtered records.");
        await output.WriteLineAsync("No signer, source write, quote, swap, deposit address or Mongo record was created. " +
            "These are indexer-reported completed swap observations, not independent finality or destination proofs. " +
            "HTTP/Mongo/browser checks have separate acceptance gates.");

        return (full.Count, subset.Count);
    }
}

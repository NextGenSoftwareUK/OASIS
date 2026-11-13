using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS;

namespace CosmicCqlDemo;

/// <summary>Read-only console demo over three configured public ChainLink feed proxies.
/// Uses the same manager/LINQ walkthrough as the other native-key sources.</summary>
public static class ChainLinkPublicWalkthrough
{
    public static async Task<(int Full, int Subset)> RunAsync(
        string configurationPath,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // Reuse the existing stable catalogue. No account, keys, runtime or
        // synthetic source data is created by this walkthrough.
        var json = await File.ReadAllTextAsync(configurationPath, cancellationToken);
        using var source = ChainLinkPublicFeedMetadataSource.FromJson(json);
        using var configuration = JsonDocument.Parse(json);
        var keys = configuration.RootElement.GetProperty("feeds")
            .EnumerateArray()
            .Select(feed => feed.GetProperty("address").GetString()!)
            .ToArray();

        if (keys.Length != 3)
            throw new ArgumentException("This demo requires the existing three-feed catalogue.");

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
        // planned as bounded native metadata membership, never an account scan.
        var full = await NativeKeySetWalkthrough.RunAsync(
            manager, source, keys, actor, output, cancellationToken);

        var subset = await NativeKeySetWalkthrough.RunAsync(
            manager, source, keys.Take(2), actor, output, cancellationToken);

        if (full.Count != 3 || subset.Count != 2)
            throw new InvalidOperationException("The configured public selection did not return three/two records.");

        // Raw answers are signed integer strings. Apply the accompanying decimal
        // scale explicitly for display; never convert these values through double.
        // Round IDs can exceed JavaScript's exact integer range as well.
        foreach (var row in full)
        {
            await output.WriteLineAsync($"Feed: {row.MetaData["ChainLinkFeedName"]}");
            await output.WriteLineAsync($"Raw answer: {row.MetaData["ChainLinkAnswer"]}");
            await output.WriteLineAsync($"Decimals: {row.MetaData["ChainLinkDecimals"]}");
            await output.WriteLineAsync($"Round: {row.MetaData["ChainLinkRoundId"]}\n");
        }

        await output.WriteLineAsync("Read-only ChainLink demo complete: 3 full / 2 filtered records.");
        await output.WriteLineAsync("Each query uses its own RPC-reported finalized Ethereum block. " +
            "No independent consensus or current-price guarantee is claimed. " +
            "No source transaction or Mongo record was written; HTTP/Mongo/browser gates are separate.");

        return (full.Count, subset.Count);
    }
}

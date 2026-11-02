using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.CelestiaOASIS;

namespace CosmicCqlDemo;

/// <summary>
/// Query the retained three-height Celestia catalogue through HolonManager and LINQ.
/// A pinned node-reported header is not independently verified consensus or DA sampling.
/// </summary>
public static class CelestiaPublicWalkthrough
{
    public static async Task<(int Full, int Subset)> RunAsync(
        string configurationPath,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // Reuse the original heights, reported block IDs and projection owner.
        // This demo neither installs a node nor creates source or Mongo records.
        var json = await File.ReadAllTextAsync(configurationPath, cancellationToken);
        using var source = CelestiaPublicHeaderMetadataSource.FromJson(json);
        using var configuration = JsonDocument.Parse(json);
        var heights = configuration.RootElement.GetProperty("headers")
            .EnumerateObject()
            .Select(header => header.Name)
            .ToArray();

        if (heights.Length != 3)
            throw new ArgumentException("This walkthrough requires the retained three-header catalogue.");

        var activated = await source.ActivateQuerySourceAsync(cancellationToken);
        if (activated.IsError)
            throw new InvalidOperationException(activated.Message, activated.Exception);

        // Keep this read-only profile outside the default blob-write registry.
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

        // The shared walkthrough builds a deferred Where(...Contains(...))
        // expression. The planner admits bounded CelestiaHeight membership,
        // then the source requests exactly those heights through native RPC.
        var full = await NativeKeySetWalkthrough.RunAsync(
            manager, source, heights, actor, output, cancellationToken);

        var subset = await NativeKeySetWalkthrough.RunAsync(
            manager, source, heights.Take(2), actor, output, cancellationToken);

        if (full.Count != 3 || subset.Count != 2)
            throw new InvalidOperationException("The configured selection did not return three/two headers.");

        foreach (var row in full)
        {
            await output.WriteLineAsync($"Height: {row.MetaData["CelestiaHeight"]}");
            await output.WriteLineAsync($"Pinned reported block ID: {row.MetaData["CelestiaBlockHash"]}");
            await output.WriteLineAsync($"Reported data hash: {row.MetaData["CelestiaDataHash"]}");
            await output.WriteLineAsync($"Extended square width: {row.MetaData["CelestiaExtendedSquareWidth"]}\n");
        }

        await output.WriteLineAsync("Read-only Celestia demo complete: 3 full / 2 filtered records.");
        await output.WriteLineAsync("No blob, transaction, signer or Mongo copy was created. " +
            "These are node-reported pinned headers; consensus and data availability are not independently verified. " +
            "HTTP/Mongo/browser checks have separate acceptance gates.");

        return (full.Count, subset.Count);
    }
}

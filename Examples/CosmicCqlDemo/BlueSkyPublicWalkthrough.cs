using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.BlueSkyOASIS;

namespace CosmicCqlDemo;

/// <summary>Read-only console demo over three configured public BlueSky public profiles.
/// Uses the same manager/LINQ walkthrough as the other native-key sources.</summary>
public static class BlueSkyPublicWalkthrough
{
    public static async Task<(int Full, int Subset)> RunAsync(
        string configurationPath,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // Reuse the existing stable catalogue. No account, keys, runtime or
        // synthetic source data is created by this walkthrough.
        var json = await File.ReadAllTextAsync(configurationPath, cancellationToken);
        using var source = BlueSkyPublicProfileMetadataSource.FromJson(json);
        using var configuration = JsonDocument.Parse(json);
        var keys = configuration.RootElement.GetProperty("actors")
            .EnumerateArray()
            .Select(key => key.GetString()!)
            .ToArray();

        if (keys.Length != 3)
            throw new ArgumentException("This demo requires the existing three-DID catalogue.");

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

        // These are cached public AppView observations. Handles and profile text
        // may change; captured DIDs remain the explicit native selection keys.
        foreach (var row in full)
        {
            await output.WriteLineAsync($"Name: {row.Name}");
            await output.WriteLineAsync($"Handle: {row.MetaData["BlueSkyHandle"]}\n");
        }

        await output.WriteLineAsync("Read-only BlueSky demo complete: 3 full / 2 filtered records.");
        await output.WriteLineAsync("No PDS session, source write or Mongo record was created. " +
            "Cached AppView data is not signed repository, DID-document or shared snapshot proof. " +
            "HTTP/Mongo/browser checks have separate acceptance gates.");

        return (full.Count, subset.Count);
    }
}

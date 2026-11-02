using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.NostrOASIS;

namespace CosmicCqlDemo;

/// <summary>
/// Read three captured, signed Nostr events, then select two through CQL/LINQ.
/// This walkthrough never discovers new events or publishes to a relay.
/// </summary>
public static class NostrPublicWalkthrough
{
    public static async Task<(int Full, int Subset)> RunAsync(
        string configurationPath,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // Keep the retained catalogue and owner stable between runs.
        // An event ID identifies immutable signed content, not a current profile.
        var json = await File.ReadAllTextAsync(configurationPath, cancellationToken);

        using var source = NostrPublicEventMetadataSource.FromJson(json);
        using var configuration = JsonDocument.Parse(json);

        var keys = configuration.RootElement.GetProperty("eventIds")
            .EnumerateArray()
            .Select(value => value.GetString()!)
            .ToArray();

        if (keys.Length != 3)
            throw new ArgumentException("This demo requires three captured event IDs.");

        // Activation verifies the complete configured selection. Every event
        // must have the expected hash and a valid BIP-340 signature.
        var activated = await source.ActivateQuerySourceAsync(cancellationToken);

        if (activated.IsError)
            throw new InvalidOperationException(activated.Message, activated.Exception);

        // An explicit read-only adapter stays outside the canonical writer
        // registry. Choosing it never changes default routing or signs a write.
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

        // Contains becomes bounded native ID membership. The shared walkthrough
        // shows the actual LINQ expression and checks every OASISResult.
        var full = await NativeKeySetWalkthrough.RunAsync(
            manager, source, keys, actor, output, cancellationToken);

        var subset = await NativeKeySetWalkthrough.RunAsync(
            manager, source, keys.Take(2), actor, output, cancellationToken);

        if (full.Count != 3 || subset.Count != 2)
            throw new InvalidOperationException("Complete three/two event selections required.");

        // Show stable identities, not untrusted note bodies or remote links.
        foreach (var row in full)
        {
            await output.WriteLineAsync($"Event: {row.MetaData["NostrEventId"]}");
            await output.WriteLineAsync($"Public key: {row.MetaData["NostrPublicKey"]}");
            await output.WriteLineAsync($"Created (Unix seconds): {row.MetaData["NostrCreatedAt"]}\n");
        }

        await output.WriteLineAsync("Read-only Nostr demo: 3 full / 2 filtered events.");
        await output.WriteLineAsync(
            "Signatures prove event integrity for the stated public keys. " +
            "They do not prove real-world identity, freshness or relay completeness. " +
            "No login, source publication or private Mongo copy was created.");

        return (full.Count, subset.Count);
    }
}

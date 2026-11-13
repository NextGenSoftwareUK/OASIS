using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.AkashOASIS;

namespace CosmicCqlDemo;

/// <summary>
/// Read three configured Akash providers, then select two through the shared
/// HolonManager and deferred LINQ pipeline. No compute service is provisioned.
/// </summary>
public static class AkashPublicWalkthrough
{
    public static async Task<(int Full, int Subset)> RunAsync(
        string configurationPath,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // Reuse the acknowledged public sample and its stable projection owner.
        // Running a query never rediscovers providers or creates a wallet/account.
        var json = await File.ReadAllTextAsync(configurationPath, cancellationToken);
        using var source = AkashPublicProviderMetadataSource.FromJson(json);
        using var configuration = JsonDocument.Parse(json);
        var owners = configuration.RootElement.GetProperty("providerOwners")
            .EnumerateArray()
            .Select(value => value.GetString()!)
            .ToArray();

        if (owners.Length != 3)
            throw new ArgumentException("This walkthrough requires the retained three-provider sample.");

        var activated = await source.ActivateQuerySourceAsync(cancellationToken);
        if (activated.IsError)
            throw new InvalidOperationException(activated.Message, activated.Exception);

        // Supply the read-only adapter explicitly. It does not become the node's
        // default storage provider or a target for canonical writes/replication.
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

        // The shared example captures a Contains predicate on AkashProviderAddress.
        // CQL reads only these configured identities, with no network-wide scan.
        var full = await NativeKeySetWalkthrough.RunAsync(
            manager, source, owners, actor, output, cancellationToken);

        var subset = await NativeKeySetWalkthrough.RunAsync(
            manager, source, owners.Take(2), actor, output, cancellationToken);

        if (full.Count != 3 || subset.Count != 2)
            throw new InvalidOperationException("The configured selection did not return three/two providers.");

        foreach (var row in full)
        {
            await output.WriteLineAsync($"Provider owner: {row.MetaData["AkashProviderAddress"]}");
            await output.WriteLineAsync($"Display name: {row.Name}");
            await output.WriteLineAsync($"Indexer reports online: {row.MetaData["AkashProviderOnline"]}\n");
        }

        await output.WriteLineAsync("Read-only Akash demo complete: 3 full / 2 filtered records.");
        await output.WriteLineAsync(
            "These are indexer-reported observations of a configured sample, not the entire network, " +
            "a shared finalized snapshot or independently verified consensus. Online flags are not uptime guarantees. " +
            "No service URL was contacted, compute provisioned, source write or Mongo copy created. " +
            "HTTP/Mongo/browser gates are separate.");

        return (full.Count, subset.Count);
    }
}

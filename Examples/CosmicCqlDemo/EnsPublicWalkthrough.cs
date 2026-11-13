using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.ENSOASIS;

namespace CosmicCqlDemo;

/// <summary>Query three configured ENS names, then filter to two through
/// HolonManager and deferred LINQ. This never submits an Ethereum transaction.</summary>
public static class EnsPublicWalkthrough
{
    public static async Task<(int Full, int Subset)> RunAsync(
        string configurationPath,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // Reuse the retained mainnet RPC endpoint, names and projection owner.
        // No API key, account, wallet or local node is created by this demo.
        var json = await File.ReadAllTextAsync(configurationPath, cancellationToken);
        using var source = EnsPublicNameMetadataSource.FromJson(json);
        using var configuration = JsonDocument.Parse(json);
        var names = configuration.RootElement.GetProperty("names")
            .EnumerateArray()
            .Select(name => name.GetString()!)
            .ToArray();

        if (names.Length != 3)
            throw new ArgumentException("This walkthrough requires the retained three-name catalogue.");

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
        // CQL plans bounded EnsName membership; only selected native
        // names are read. It does not discover arbitrary registered names.
        var full = await NativeKeySetWalkthrough.RunAsync(
            manager, source, names, actor, output, cancellationToken);

        var subset = await NativeKeySetWalkthrough.RunAsync(
            manager, source, names.Take(2), actor, output, cancellationToken);

        if (full.Count != 3 || subset.Count != 2)
            throw new InvalidOperationException("The configured selection did not return three/two names.");

        // Each query uses one finalized canonical block. Separate queries can
        // observe different finalized blocks; this is not a distributed snapshot.
        foreach (var row in full)
        {
            await output.WriteLineAsync($"ENS name: {row.MetaData["EnsName"]}");
            await output.WriteLineAsync($"Resolved ETH address: {row.MetaData["EnsResolvedAddress"]}");
            await output.WriteLineAsync($"Resolver contract: {row.MetaData["EnsResolver"]}");
            await output.WriteLineAsync($"Finalized block: {row.MetaData["EnsBlockHash"]}\n");
        }

        await output.WriteLineAsync("Read-only ENS demo complete: 3 full / 2 filtered records.");
        await output.WriteLineAsync(
            "These are node-reported direct resolutions, not proof of wallet ownership or independently verified consensus. " +
            "Only the explicit pre-normalized ASCII catalogue is supported; CCIP Read and general Unicode names are separate work. " +
            "No transaction, source write, wallet or Mongo copy was created. HTTP/Mongo/browser gates are separate.");

        return (full.Count, subset.Count);
    }
}

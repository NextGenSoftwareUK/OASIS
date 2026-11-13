using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.BlockStackOASIS;

namespace CosmicCqlDemo;

/// <summary>Read-only console demo over three configured public Stacks principals.
/// Uses the same manager/LINQ walkthrough as the other native-key sources.</summary>
public static class StacksPublicWalkthrough
{
    public static async Task<(int Full, int Subset)> RunAsync(
        string configurationPath,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // Reuse the existing stable catalogue. No account, keys, runtime or
        // synthetic source data is created by this walkthrough.
        var json = await File.ReadAllTextAsync(configurationPath, cancellationToken);
        using var source = StacksPublicPrincipalMetadataSource.FromJson(json);
        using var configuration = JsonDocument.Parse(json);
        var keys = configuration.RootElement.GetProperty("principals")
            .EnumerateArray()
            .Select(key => key.GetString()!)
            .ToArray();

        if (keys.Length != 3)
            throw new ArgumentException("This demo requires the existing three-principal catalogue.");

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
        // planned as bounded native metadata membership, never a principal scan.
        var full = await NativeKeySetWalkthrough.RunAsync(
            manager, source, keys, actor, output, cancellationToken);

        var subset = await NativeKeySetWalkthrough.RunAsync(
            manager, source, keys.Take(2), actor, output, cancellationToken);

        if (full.Count != 3 || subset.Count != 2)
            throw new InvalidOperationException("The configured public selection did not return three/two records.");

        // These are node-reported explicit-tip observations. Principal states
        // may change; configured principals remain the explicit native selection keys.
        foreach (var row in full)
        {
            await output.WriteLineAsync($"Name: {row.Name}");
            await output.WriteLineAsync($"Spendable micro-STX: {row.MetaData["StacksBalanceMicroStx"]}\n");
        }

        await output.WriteLineAsync("Read-only Stacks demo complete: 3 full / 2 filtered records.");
        await output.WriteLineAsync("No signer, source write or Mongo record was created. " +
            "Each selection uses one node tip; separate queries can see different tips. " +
            "These are not independently verified consensus or Gaia storage proofs. " +
            "HTTP/Mongo/browser checks have separate acceptance gates.");

        return (full.Count, subset.Count);
    }
}

using NextGenSoftware.OASIS.API.Core.Enums;
using Boot = NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader;
using HoloProvider = NextGenSoftware.OASIS.API.Providers.HoloOASIS.HoloOASIS;

if (args.Length != 2 || !File.Exists(args[0])
    || !Enum.TryParse<ProviderType>(args[1], ignoreCase: false, out var providerType)
    || args[1] != providerType.ToString()
    || providerType is not (ProviderType.NeonOASIS or ProviderType.ConvexOASIS
        or ProviderType.SupabaseOASIS or ProviderType.SQLServerDBOASIS or ProviderType.BNBChainOASIS
        or ProviderType.AzureCosmosDBOASIS or ProviderType.PolkadotOASIS or ProviderType.TRONOASIS
        or ProviderType.HoloOASIS))
{
    Console.Error.WriteLine(
        "Usage: BootLoader.TestHarness <protected-fixture-dna-path> <NeonOASIS|ConvexOASIS|SupabaseOASIS|SQLServerDBOASIS|BNBChainOASIS|AzureCosmosDBOASIS|PolkadotOASIS|TRONOASIS|HoloOASIS>");
    return 2;
}

var boot = await Boot.BootOASISForAdmittedStorageAsync(Path.GetFullPath(args[0]));
if (boot.IsError || !boot.Result)
{
    Console.Error.WriteLine("Passive fixture boot failed.");
    return 1;
}

var activation = await Boot.GetAndActivateStorageProviderAsync(providerType);
var provider = activation.Result;
if (activation.IsError || provider is null || provider.ProviderType?.Value != providerType
    || !provider.IsProviderActivated)
{
    Console.Error.WriteLine($"Activation failed for {providerType}.");
    if (providerType == ProviderType.HoloOASIS)
        Console.Error.WriteLine(activation.Message);
    return 1;
}

var holo = provider as HoloProvider;
var agent = holo?.HoloNETClientAppAgent?.HoloNETDNA.AgentPubKey;
var adminUri = holo?.HoloNETClientAdmin?.EndPoint.AbsoluteUri;

var deactivation = await provider.DeActivateProviderAsync();
if (deactivation.IsError || !deactivation.Result || provider.IsProviderActivated)
{
    Console.Error.WriteLine($"Deactivation failed for {providerType}.");
    if (providerType == ProviderType.HoloOASIS)
        Console.Error.WriteLine(deactivation.Message);
    return 1;
}

Console.WriteLine($"Registered, activated and deactivated {providerType} through BootLoader.");

if (providerType == ProviderType.HoloOASIS)
{
    if (string.IsNullOrWhiteSpace(agent) || string.IsNullOrWhiteSpace(adminUri))
    {
        Console.Error.WriteLine("HoloOASIS did not expose the installed app identity.");
        return 1;
    }

    var reopened = new HoloProvider(adminUri, useHoloNetwork: false);
    var reopening = await reopened.ActivateProviderAsync();
    var sameAgent = StringComparer.Ordinal.Equals(agent, reopened.HoloNETClientAppAgent?.HoloNETDNA.AgentPubKey);
    var closing = await reopened.DeActivateProviderAsync();
    if (reopening.IsError || !reopening.Result || !sameAgent || closing.IsError || !closing.Result || reopened.IsProviderActivated)
    {
        Console.Error.WriteLine("HoloOASIS did not reopen and close the same installed app agent.");
        return 1;
    }

    Console.WriteLine("Reopened and closed the original HoloOASIS app agent.");
}

return 0;

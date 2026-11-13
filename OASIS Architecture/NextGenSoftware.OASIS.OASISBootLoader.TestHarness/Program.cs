using NextGenSoftware.OASIS.API.Core.Enums;
using Boot = NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader;

if (args.Length != 2 || !File.Exists(args[0])
    || !Enum.TryParse<ProviderType>(args[1], ignoreCase: false, out var providerType)
    || args[1] != providerType.ToString()
    || providerType is not (ProviderType.NeonOASIS or ProviderType.ConvexOASIS
        or ProviderType.SupabaseOASIS or ProviderType.SQLServerDBOASIS or ProviderType.BNBChainOASIS
        or ProviderType.AzureCosmosDBOASIS or ProviderType.PolkadotOASIS or ProviderType.TRONOASIS))
{
    Console.Error.WriteLine(
        "Usage: BootLoader.TestHarness <protected-fixture-dna-path> <NeonOASIS|ConvexOASIS|SupabaseOASIS|SQLServerDBOASIS|BNBChainOASIS|AzureCosmosDBOASIS|PolkadotOASIS|TRONOASIS>");
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
    return 1;
}

var deactivation = await provider.DeActivateProviderAsync();
if (deactivation.IsError || !deactivation.Result || provider.IsProviderActivated)
{
    Console.Error.WriteLine($"Deactivation failed for {providerType}.");
    return 1;
}

Console.WriteLine($"Registered, activated and deactivated {providerType} through BootLoader.");
return 0;

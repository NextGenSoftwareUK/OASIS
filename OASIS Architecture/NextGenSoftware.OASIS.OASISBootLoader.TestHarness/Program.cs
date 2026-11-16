using NextGenSoftware.OASIS.API.Core.Enums;
using Boot = NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader;
using HoloProvider = NextGenSoftware.OASIS.API.Providers.HoloOASIS.HoloOASIS;

if (args.Length != 2 || !File.Exists(args[0])
    || !Enum.TryParse<ProviderType>(args[1], ignoreCase: false, out var providerType)
    || args[1] != providerType.ToString()
    || providerType is not (ProviderType.NeonOASIS or ProviderType.ConvexOASIS
        or ProviderType.SupabaseOASIS or ProviderType.SQLServerDBOASIS or ProviderType.BNBChainOASIS
        or ProviderType.AzureCosmosDBOASIS or ProviderType.PolkadotOASIS or ProviderType.TRONOASIS
        or ProviderType.HoloOASIS or ProviderType.BitcoinOASIS))
{
    Console.Error.WriteLine(
        "Usage: BootLoader.TestHarness <protected-fixture-dna-path> <NeonOASIS|ConvexOASIS|SupabaseOASIS|SQLServerDBOASIS|BNBChainOASIS|AzureCosmosDBOASIS|PolkadotOASIS|TRONOASIS|HoloOASIS|BitcoinOASIS>");
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
var checkListenerReuse = holo != null
    && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OASIS_HOLO_LINUX_CI_FIXTURE"));
ushort? originalListener = null;
if (checkListenerReuse)
    originalListener = await GetOnlyOasisListenerPortAsync(holo!);

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
    if (string.IsNullOrWhiteSpace(agent))
    {
        Console.Error.WriteLine("HoloOASIS did not expose the installed app identity.");
        return 1;
    }

    var reopening = await holo!.ActivateProviderAsync();
    var sameAgent = StringComparer.Ordinal.Equals(agent, holo.HoloNETClientAppAgent?.HoloNETDNA.AgentPubKey);
    var reopenedListener = checkListenerReuse && !reopening.IsError
        ? await GetOnlyOasisListenerPortAsync(holo)
        : originalListener;
    var closing = await holo.DeActivateProviderAsync();
    if (reopening.IsError || !reopening.Result || !sameAgent || closing.IsError || !closing.Result || holo.IsProviderActivated)
    {
        Console.Error.WriteLine("HoloOASIS did not reopen and close the same installed app agent.");
        return 1;
    }

    if (checkListenerReuse && (!originalListener.HasValue || originalListener != reopenedListener))
    {
        Console.Error.WriteLine("HoloOASIS changed the app listener during reconnection.");
        return 1;
    }
    if (checkListenerReuse)
        Console.WriteLine("HoloOASIS reused the single app-bound listener after reconnection.");

    Console.WriteLine("Reactivated the same HoloOASIS provider instance and original app agent.");
}

return 0;

static async Task<ushort?> GetOnlyOasisListenerPortAsync(HoloProvider provider)
{
    var listed = await provider.HoloNETClientAdmin.ListInterfacesAsync();
    if (listed == null || listed.IsError || listed.AppInterfaces.Count != 1)
        return null;

    var listener = listed.AppInterfaces[0];
    if (!StringComparer.Ordinal.Equals(listener.InstalledAppId, "oasis") || listener.Port == 0)
        return null;

    return listener.Port;
}

using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Data.Admin.Requests.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

internal static class CqlHoloOasisReadSourceRegistration
{
    public static void AddHoloOasisReadSource(IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("CQL_HOLOOASIS_QUERY");
        if (!settings.Exists())
            return;

        if (!Uri.TryCreate(settings["Endpoint"], UriKind.Absolute, out var endpoint) ||
            (endpoint.Scheme != "wss" && !(endpoint.Scheme == "ws" && endpoint.IsLoopback)) ||
            endpoint.UserInfo.Length != 0 || endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0 ||
            string.IsNullOrWhiteSpace(settings["InstalledAppId"]) || string.IsNullOrWhiteSpace(settings["Zome"]) ||
            !Guid.TryParseExact(settings["ProbeId"], "D", out var probeId) || probeId == Guid.Empty)
        {
            throw new InvalidOperationException("HoloOASIS CQL requires a reviewed app endpoint, installed app, zome and probe.");
        }

        var appId = settings["InstalledAppId"];
        var zome = settings["Zome"];
        var dnaHash = ReadBytes(settings, "DnaHash", 39);
        var agentKey = ReadBytes(settings, "AgentKey", 39);
        var token = ReadAppToken(settings["AppToken"]);
        var credentials = new SigningCredentials
        {
            CapSecret = ReadBytes(settings, "CapabilitySecret", 64),
            SigningKey = ReadBytes(settings, "SigningKey", 39),
            KeyPair = new KeyPair { PrivateKey = ReadBytes(settings, "SigningPrivateKey", 64) }
        };

        services.AddSingleton<CqlReadSourceProfile>(provider =>
        {
            var catalogue = provider.GetServices<CqlPublicReadCatalogue>()
                .SingleOrDefault(entry => entry.Provider == ProviderType.HoloOASIS)
                ?? throw new InvalidOperationException("HoloOASIS CQL requires an explicit public catalogue.");
            var dna = new HoloNETDNA
            {
                HolochainConductorAppAgentURI = endpoint.AbsoluteUri,
                InstalledAppId = appId,
                AppAuthenticationToken = token,
                AutoStartHolochainConductor = false,
                AutoShutdownHolochainConductor = false
            };
            var client = new HoloNETClientAppAgent(appId, null, dna);
            var signer = new HoloOasisReadSigner(dnaHash, agentKey, zome, credentials);
            return new CqlReadSourceProfile(new HoloOasisReadSource(client, signer, endpoint, appId, probeId,
                catalogue.CopyRecordIds().ToHashSet()));
        });
    }

    private static byte[] ReadBytes(IConfiguration configuration, string name, int length)
    {
        Span<byte> decoded = stackalloc byte[length];
        if (Convert.TryFromBase64String(configuration[name] ?? string.Empty, decoded, out var count) && count == length)
            return decoded.ToArray();
        throw new InvalidOperationException($"HoloOASIS CQL setting {name} must be a base64 value of the required length.");
    }

    private static byte[] ReadAppToken(string value)
    {
        Span<byte> decoded = stackalloc byte[256];
        if (value is { Length: <= 344 } && Convert.TryFromBase64String(value, decoded, out var count) && count > 0)
            return decoded[..count].ToArray();
        throw new InvalidOperationException("HoloOASIS CQL requires a nonempty base64 app authentication token.");
    }
}

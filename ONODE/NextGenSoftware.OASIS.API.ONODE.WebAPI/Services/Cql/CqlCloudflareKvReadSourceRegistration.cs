using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.CloudflareOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlCloudflareKvReadSourceRegistration
{
    public static void AddCloudflareKvReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_CLOUDFLARE_KV_QUERY_";
        var account = configuration[prefix + "ACCOUNT_ID"];
        var nameSpace = configuration[prefix + "NAMESPACE_ID"];
        var token = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (account == null && nameSpace == null && token == null && probe == null) return;
        if (!IsCanonicalId(account) || !IsCanonicalId(nameSpace) ||
            token is not { Length: > 0 and <= 1024 } || token.Any(c => c <= ' ' || c > '~') ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty || id.ToString("D") != probe)
            throw new InvalidOperationException("Workers KV CQL requires a reviewed account, public teaching namespace, reader token and canonical probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(new CloudflareKvQuerySource(account, nameSpace, token, id)));
    }

    private static bool IsCanonicalId(string value) =>
        value is { Length: 32 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}

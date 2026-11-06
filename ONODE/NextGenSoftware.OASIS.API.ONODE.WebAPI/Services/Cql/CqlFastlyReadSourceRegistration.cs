using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.FastlyOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlFastlyReadSourceRegistration
{
    public static void AddFastlyReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_FASTLY_QUERY_";
        var store = configuration[prefix + "STORE_ID"];
        var token = configuration[prefix + "READER_TOKEN"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (store == null && token == null && probe == null) return;
        if (store is not { Length: > 0 and <= 64 } ||
            store.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')) ||
            token is not { Length: > 0 and <= 1024 } || token.Any(c => c <= ' ' || c > '~') ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty || probe != id.ToString("D"))
            throw new InvalidOperationException("Fastly CQL requires a reviewed store, reader token and canonical probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(new FastlyQuerySource(store, token, id)));
    }
}

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.LitestreamOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlLitestreamReadSourceRegistration
{
    private const string RestorePath = "/data/cql-litestream/restore.db";

    public static void AddLitestreamReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_LITESTREAM_QUERY_";
        var digest = configuration[prefix + "RESTORE_SHA256"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (digest == null && probe == null) return;
        if (digest is not { Length: 64 } || digest.Any(c => !char.IsAsciiHexDigitLower(c)) ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty || probe != id.ToString("D"))
            throw new InvalidOperationException("Litestream CQL requires an approved restore digest and canonical probe.");

        services.AddSingleton<CqlReadSourceProfile>(_ =>
            new CqlReadSourceProfile(new LitestreamQuerySource(Path.GetFullPath(RestorePath), digest, id)));
    }
}

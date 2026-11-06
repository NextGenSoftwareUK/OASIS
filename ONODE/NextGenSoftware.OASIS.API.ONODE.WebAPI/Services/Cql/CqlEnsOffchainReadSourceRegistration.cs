using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.ENSOffchainOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlEnsOffchainReadSourceRegistration
{
    public static void AddEnsOffchainReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var source = configuration["CQL_ENS_OFFCHAIN_QUERY_SOURCE_JSON"];
        if (source == null) return;
        if (Encoding.UTF8.GetByteCount(source) > 4096)
            throw new InvalidOperationException("A bounded ENS offchain catalogue is required.");
        try
        {
            using var document = JsonDocument.Parse(source, new JsonDocumentOptions { MaxDepth = 3 });
            var root = document.RootElement;
            var fields = new[] { "owner", "names", "probe" };
            var keys = root.EnumerateObject().Select(property => property.Name).ToArray();
            if (keys.Length != fields.Length || !keys.ToHashSet(StringComparer.Ordinal).SetEquals(fields))
                throw new ArgumentException("Unexpected ENS offchain catalogue fields.");
            var identity = root.GetProperty("owner").GetString();
            if (!Guid.TryParseExact(identity, "D", out var owner) || owner == Guid.Empty || owner.ToString("D") != identity)
                throw new ArgumentException("A canonical ENS offchain catalogue owner is required.");
            var names = root.GetProperty("names").EnumerateArray().Select(value => value.GetString()).ToArray();
            var probe = root.GetProperty("probe").GetString();
            if (names.Length is < 1 or > 8 || !names.Contains(probe, StringComparer.Ordinal) ||
                names.Select(name => EnsOffchainQuerySource.LogicalId(owner, name)).Distinct().Count() != names.Length)
                throw new ArgumentException("Unique canonical names and an enrolled probe are required.");
            services.AddSingleton<CqlReadSourceProfile>(_ =>
                new CqlReadSourceProfile(new EnsOffchainQuerySource(owner, names, probe)));
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException)
        {
            throw new InvalidOperationException("Use the reviewed public ENS offchain catalogue.", error);
        }
    }
}

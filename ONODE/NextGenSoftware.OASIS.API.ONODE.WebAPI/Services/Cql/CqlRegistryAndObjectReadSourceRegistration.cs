using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS;
using NextGenSoftware.OASIS.API.Providers.ProofOfHumanityOASIS;
using NextGenSoftware.OASIS.API.Providers.SafeOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlRegistryAndObjectReadSourceRegistration
{
    public static void AddRegistryAndObjectReadSources(this IServiceCollection services, IConfiguration configuration)
    {
        Add(services, configuration["CQL_PROOF_OF_HUMANITY_QUERY_SOURCE_JSON"], "addresses",
            ProofOfHumanityRegistryQuerySource.LogicalId,
            (owner, keys, probe) => new ProofOfHumanityRegistryQuerySource(owner, keys, probe));
        Add(services, configuration["CQL_ORACLE_OBJECT_STORAGE_QUERY_SOURCE_JSON"], "keys",
            OracleObjectStoragePublicMetadataQuerySource.LogicalId,
            (owner, keys, probe) => new OracleObjectStoragePublicMetadataQuerySource(owner, keys, probe));
        var safeCatalogue = configuration["CQL_SAFE_INDEXING_QUERY_SOURCE_JSON"];
        if (safeCatalogue is not null)
        {
            var apiKey = configuration["CQL_SAFE_INDEXING_QUERY_API_KEY"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("The Safe indexing profile requires a protected API key.");
            Add(services, safeCatalogue, "networks", SafeIndexingQuerySource.LogicalId,
                (owner, keys, probe) => new SafeIndexingQuerySource(owner, keys, probe, apiKey));
        }
    }

    private static void Add(IServiceCollection services, string json, string keyField, Func<Guid, string, Guid> logicalId,
        Func<Guid, string[], string, IOASISStorageProvider> create)
    {
        if (json == null) return;
        if (Encoding.UTF8.GetByteCount(json) > 4096)
            throw new InvalidOperationException("A bounded reviewed public catalogue is required.");
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 3 });
            var root = document.RootElement;
            var expected = new[] { "owner", keyField, "probe" };
            var fields = root.EnumerateObject().Select(field => field.Name).ToArray();
            if (fields.Length != expected.Length || !fields.ToHashSet(StringComparer.Ordinal).SetEquals(expected))
                throw new ArgumentException("Unexpected public catalogue fields.");
            var text = root.GetProperty("owner").GetString();
            if (!Guid.TryParseExact(text, "D", out var owner) || owner == Guid.Empty || owner.ToString("D") != text)
                throw new ArgumentException("A canonical public catalogue owner is required.");
            var keys = root.GetProperty(keyField).EnumerateArray().Select(value => value.GetString()).ToArray();
            var probe = root.GetProperty("probe").GetString();
            if (keys.Length is < 1 or > 8 || !keys.Contains(probe, StringComparer.Ordinal) ||
                keys.Select(key => logicalId(owner, key)).Distinct().Count() != keys.Length)
                throw new ArgumentException("Unique canonical keys and an enrolled probe are required.");
            services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(create(owner, keys, probe)));
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException)
        {
            throw new InvalidOperationException("Use the reviewed public catalogue for this source.", error);
        }
    }
}

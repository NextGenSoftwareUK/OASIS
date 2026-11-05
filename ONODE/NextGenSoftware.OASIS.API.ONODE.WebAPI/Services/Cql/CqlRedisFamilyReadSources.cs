using System;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using StackExchange.Redis;
using RedisProvider = NextGenSoftware.OASIS.API.Providers.RedisOASIS.RedisOASIS;
using ValkeyProvider = NextGenSoftware.OASIS.API.Providers.ValKeyOASIS.ValKeyOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlRedisFamilyReadSources
{
    private static readonly string[] Fields = { "ADDRESS", "PORT", "TLS_HOST", "USERNAME", "PASSWORD", "STORAGE_KEY", "PROBE_ID" };

    public static void AddRedisFamilyReadSources(this IServiceCollection services, IConfiguration configuration)
    {
        var redis = ReadSettings(configuration, "REDIS");
        var valkey = ReadSettings(configuration, "VALKEY");
        if (redis != null)
            services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(new RedisProvider(
                redis.Connection, redis.ResolveStorageKey, redis.Probe)));
        if (valkey != null)
            services.AddSingleton<CqlReadSourceProfile>(_ => new CqlReadSourceProfile(new ValkeyProvider(
                valkey.Connection, valkey.ResolveStorageKey, valkey.Probe)));
    }

    private sealed class Source(ConfigurationOptions connection, string encodedKey, Guid probe)
    {
        internal ConfigurationOptions Connection { get; } = connection;
        internal Guid Probe { get; } = probe;
        internal byte[] ResolveStorageKey() => Convert.FromBase64String(encodedKey);
    }

    private static Source ReadSettings(IConfiguration configuration, string provider)
    {
        var values = Fields.Select(name => configuration["CQL_" + provider + "_QUERY_" + name]).ToArray();
        if (values.All(value => value == null)) return null;
        if (values.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException(provider + " CQL requires all seven explicit query source settings.");
        if (!IPAddress.TryParse(values[0], out var address) || !NativeConnectionGuard.IsPublicAddress(address) ||
            !int.TryParse(values[1], out var port) || port is < 1 or > 65535 ||
            Uri.CheckHostName(values[2]) != UriHostNameType.Dns || !values[2].Contains('.') || values[2].Length > 253)
            throw new InvalidOperationException(provider + " CQL requires a public endpoint and its TLS certificate hostname.");
        if (values.Skip(3).Take(2).Any(value => value.Length > 1024 || value.Any(char.IsControl)))
            throw new InvalidOperationException(provider + " CQL requires explicit restricted connection credentials.");
        Span<byte> decoded = stackalloc byte[32];
        var validKey = values[5].Length == 44 && Convert.TryFromBase64String(values[5], decoded, out var written) &&
            written == 32 && Convert.ToBase64String(decoded) == values[5];
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(decoded);
        if (!validKey)
            throw new InvalidOperationException(provider + " CQL requires the source's existing canonical 32-byte encryption key.");
        if (!Guid.TryParseExact(values[6], "D", out var probe) || probe == Guid.Empty)
            throw new InvalidOperationException(provider + " CQL requires an existing public teaching probe.");
        var options = new ConfigurationOptions
        {
            Ssl = true, SslHost = values[2], User = values[3], Password = values[4],
            AllowAdmin = false, TieBreaker = "", DefaultDatabase = 0
        };
        options.EndPoints.Add(address, port);
        return new Source(options, values[5], probe);
    }
}

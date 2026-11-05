using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using SurrealProvider = NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.SurrealDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlSurrealDbReadSourceRegistration
{
    private const string TransportKey = "cql-surrealdb-transport";

    public static void AddSurrealDbReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_SURREALDB_QUERY_";
        var endpoint = configuration[prefix + "ORIGIN"];
        var username = configuration[prefix + "USERNAME"];
        var password = configuration[prefix + "PASSWORD"];
        var ns = configuration[prefix + "NAMESPACE"];
        var database = configuration[prefix + "DATABASE"];
        var probeValue = configuration[prefix + "PROBE_ID"];
        if (new[] { endpoint, username, password, ns, database, probeValue }.All(value => value == null)) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("SurrealDB CQL requires an explicit credential-free HTTPS origin.");
        if (!IsScopeName(ns) || !IsScopeName(database))
            throw new InvalidOperationException("SurrealDB CQL requires explicit database and namespace identifiers.");
        if (string.IsNullOrWhiteSpace(username) || username.Length > 256 || username.Contains(':') || username.Any(char.IsControl) ||
            string.IsNullOrWhiteSpace(password) || password.Length > 1024 || password.Any(char.IsControl))
            throw new InvalidOperationException("SurrealDB CQL requires an explicit database VIEWER account.");
        if (!Guid.TryParseExact(probeValue, "D", out var probe) || probe == Guid.Empty)
            throw new InvalidOperationException("SurrealDB CQL requires an existing teaching probe identity.");

        services.AddKeyedSingleton<HttpMessageHandler>(TransportKey, (_, _) => NativeConnectionGuard.CreateGuardedHttpHandler());
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new SurrealProvider(
            origin.AbsoluteUri, username, password, ns, database, probe,
            provider.GetRequiredKeyedService<HttpMessageHandler>(TransportKey))));
    }

    private static bool IsScopeName(string value) => !string.IsNullOrEmpty(value) && value.Length <= 64 &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-');
}

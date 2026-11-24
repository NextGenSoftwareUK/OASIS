using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.WeaviateOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Wires the shipping WeaviateOASIS provider as a hosted read-only CQL source; query logic stays in the provider.</summary>
public static class CqlWeaviateReadSourceRegistration
{
    private const string ClientKey = "cql-weaviate-source";

    public static void AddWeaviateReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_WEAVIATE_QUERY_";
        var endpoint = configuration[prefix + "ORIGIN"];
        var reader = configuration[prefix + "READER_API_KEY"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (endpoint == null && reader == null && probe == null) return;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.IsLoopback || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("Weaviate CQL requires an explicit credential-free HTTPS origin.");
        if (string.IsNullOrWhiteSpace(reader) || !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty)
            throw new InvalidOperationException("Weaviate CQL requires a restricted reader API key and an existing probe.");
        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) => new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler()));
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new WeaviateQuerySource(
            provider.GetRequiredKeyedService<HttpClient>(ClientKey), origin.AbsoluteUri.TrimEnd('/'), reader, id)));
    }
}

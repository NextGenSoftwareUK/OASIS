using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PouchProvider = NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.PouchDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Query-only access to the explicit sandbox-owned loopback runtime.</summary>
public static class CqlPouchDbReadSourceRegistration
{
    public const string Origin = "http://127.0.0.1:13800";
    private const string ClientKey = "cql-owned-pouchdb-source";

    public static void AddPouchDbReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var enabled = configuration["OASIS_SANDBOX_POUCHDB"] ?? "false";
        var digest = configuration["CQL_POUCHDB_QUERY_SOURCE_DIGEST"];
        if (enabled is not ("true" or "false"))
            throw new InvalidOperationException("PouchDB native sandbox flag must be explicit true or false.");
        if (enabled == "false")
        {
            if (digest != null) throw new InvalidOperationException("PouchDB CQL requires its owned native runtime to be enabled.");
            return;
        }
        var dna = configuration["OASIS_DNA_JSON"];
        bool sandbox;
        try
        {
            if (dna == null || dna.Length > 1024 * 1024) throw new InvalidOperationException("Bounded sandbox DNA required.");
            using var document = JsonDocument.Parse(dna);
            sandbox = document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("OASIS", out var oasis)
                && oasis.ValueKind == JsonValueKind.Object && oasis.TryGetProperty("Sandbox", out var settings)
                && settings.ValueKind == JsonValueKind.Object && settings.TryGetProperty("Enabled", out var flag) && flag.ValueKind == JsonValueKind.True;
        }
        catch (JsonException error) { throw new InvalidOperationException("PouchDB CQL requires valid authoritative sandbox DNA.", error); }
        if (!sandbox) throw new InvalidOperationException("PouchDB CQL requires OASIS.Sandbox.Enabled.");
        if (!CqlPouchDbSourceHttpHandler.IsDigest(digest))
            throw new InvalidOperationException("PouchDB CQL requires the qualified native source digest.");

        services.AddKeyedSingleton<HttpClient>(ClientKey, (_, _) => new HttpClient(new CqlPouchDbSourceHttpHandler(digest,
            new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, UseProxy = false })));
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new PouchProvider(
            Origin, null, null, provider.GetRequiredKeyedService<HttpClient>(ClientKey))));
    }
}

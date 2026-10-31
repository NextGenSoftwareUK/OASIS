using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>
/// Explicit host grant: every authenticated caller may read this complete bounded
/// catalogue on this authority. This is not inferred from a request or source flags.
/// </summary>
public sealed class CqlPublicReadCatalogue : ICqlReadAccessPolicy
{
    public const string ConfigurationKey = "CQL_PUBLIC_READ_CATALOGUES_JSON";
    private readonly ProviderType provider;
    private readonly Guid[] records;
    public ProviderType Provider => provider;

    public CqlPublicReadCatalogue(ProviderType provider, Guid[] records)
    {
        if (!CqlV1Compiler.SupportedReadProfile(provider) || records == null || records.Length > 128 ||
            records.Any(id => id == Guid.Empty) || records.Distinct().Count() != records.Length)
            throw new ArgumentException("A complete bounded catalogue for one explicit read provider is required.");
        this.provider = provider;
        this.records = (Guid[])records.Clone();
    }

    public OASISResult<CosmicQueryReadAccess> Authorize(Guid authenticatedActor, ProviderType authority)
        => authority == provider
            ? CosmicQueryReadAccess.AuthoritativeRecords(authenticatedActor, provider, records)
            : new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_ACCESS_FORBIDDEN",
                Message = "The catalogue belongs to another provider." };

    public static void Enroll(IServiceCollection services, IConfiguration configuration)
    {
        var json = configuration[ConfigurationKey];
        if (json == null) return;
        if (Encoding.UTF8.GetByteCount(json) > 32768)
            throw new ArgumentException("The public catalogue configuration exceeds its byte budget.");
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Public read catalogues must be a provider-to-ID-array object.");
        var entries = document.RootElement.EnumerateObject().ToArray();
        if (entries.Length > 16 || entries.Select(entry => entry.Name).Distinct(StringComparer.Ordinal).Count() != entries.Length)
            throw new ArgumentException("Public read catalogues require unique bounded provider names.");
        // Parse the entire configuration before adding registrations.
        var captured = entries.Select(entry =>
        {
            var provider = CqlV1Compiler.RequestedProvider(entry.Name, includeCloudReadProfile: true)!.Value;
            if (entry.Value.ValueKind != JsonValueKind.Array || entry.Value.GetArrayLength() > 128)
                throw new ArgumentException("Each public read catalogue must be a complete bounded ID array.");
            var ids = entry.Value.EnumerateArray().Select(value =>
                value.ValueKind == JsonValueKind.String && Guid.TryParseExact(value.GetString(), "D", out var id)
                    ? id : throw new ArgumentException("Catalogue identities require GUID D format.")).ToArray();
            return (Provider: provider, Policy: new CqlPublicReadCatalogue(provider, ids));
        }).ToArray();
        foreach (var entry in captured)
        {
            if (services.Any(descriptor => descriptor.ServiceType == typeof(ICqlReadAccessPolicy) &&
                descriptor.IsKeyedService && Equals(descriptor.ServiceKey, entry.Provider)))
                throw new ArgumentException("A provider read policy is already registered; do not replace it.");
        }
        foreach (var entry in captured)
        {
            services.AddKeyedSingleton<ICqlReadAccessPolicy>(entry.Provider, entry.Policy);
            services.AddSingleton(entry.Policy);
        }
    }
}

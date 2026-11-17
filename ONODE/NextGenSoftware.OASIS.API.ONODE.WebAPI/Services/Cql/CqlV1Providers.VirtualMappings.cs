using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public sealed partial class CqlV1Providers
{
    Task<OASISResult<IReadOnlyList<VirtualHolonRead>>> ICqlVirtualMappingReader.QueryVirtualMappingAsync(
        Guid mappingId, Guid actor, CqlVirtualProjection projection, CancellationToken cancellationToken)
    {
        var authority = Resolve(null);
        return QueryOwnedVirtualMappingAsync(CreateManager(authority), authority, mappingId, actor, null,
            projection.MatchField, projection.MatchValue, projection.ProjectedFields, CqlVirtualProjection.MaximumRows,
            cancellationToken);
    }

    internal static async Task<OASISResult<IReadOnlyList<VirtualHolonRead>>> QueryOwnedVirtualMappingAsync(
        HolonManager manager, IOASISProvider authority, Guid mappingId, Guid actor, ProviderType? requestedSource, string matchField, object matchValue,
        IReadOnlyList<string> selectedFields, int limit, CancellationToken cancellationToken)
    {
        if (authority is not ICosmicQueryStorageProvider adapter ||
            authority.ProviderType?.Value != adapter.QueryProviderType)
            return Refuse("VIRTUAL_HOLON_STORE_UNAVAILABLE", "The mapping's authoritative store cannot query its definition.");

        var provider = adapter.QueryProviderType;
        var query = CqlV1Compiler.Compile(new CqlV1Request
        {
            OrderBy = "Id",
            Take = 1,
            Filters = new[] { new CqlV1Filter
            {
                Field = "Id",
                Scalar = CqlV1Scalar.Guid,
                Operator = CqlV1Operator.Equal,
                Value = JsonSerializer.SerializeToElement(mappingId.ToString("D"))
            } }
        }, actor, provider);
        var plan = CosmicQueryPlanner.Plan(query, provider,
            new[] { new CosmicQueryTarget(provider, adapter.QueryCapabilities) });
        if (plan.IsError)
            return Refuse("VIRTUAL_HOLON_STORE_UNAVAILABLE", "The mapping's authoritative store cannot plan a definition lookup.");

        // This lookup belongs to the admitted CQL request. Use a measured native
        // query so the actor, visibility, provider tariff and execution bounds
        // stay attached to both reads.
        var loaded = await manager.ExecuteQueryAsync(plan.Result, new[] { adapter }, maximumResults: 1,
            cancellationToken: cancellationToken);
        if (loaded.IsError || loaded.Result == null)
            return Refuse("VIRTUAL_HOLON_STORE_UNAVAILABLE", "The mapping's authoritative store is unavailable.");
        var definition = loaded.Result.Providers.Single().Rows.SingleOrDefault();
        if (definition == null || definition.Id != mappingId || !definition.IsActive)
            return Refuse("VIRTUAL_MAPPING_NOT_FOUND", "The mapping is unavailable.");

        string connectionName;
        ProviderType source;
        try
        {
            source = VirtualHolonMapping.GetDefinitionProvider(actor, definition);
            if (requestedSource.HasValue && requestedSource.Value != source)
                return Refuse("VIRTUAL_MAPPING_NOT_FOUND", "The mapping is unavailable.");
            connectionName = VirtualHolonMapping.GetDefinitionConnectionName(actor, definition, source);
        }
        catch (ArgumentException)
        {
            return Refuse("VIRTUAL_MAPPING_NOT_FOUND", "The mapping is unavailable.");
        }

        try
        {
            var binding = await ResolveVirtualConnectionAsync(actor, source, definition.Name, connectionName);
            cancellationToken.ThrowIfCancellationRequested();
            if (binding == null)
                return Refuse("VIRTUAL_HOLON_SOURCE_UNAVAILABLE", "The mapping's native connection is unavailable.");

            var mapping = VirtualHolonMapping.FromDefinitionHolon(actor, definition, binding.Identity);
            if (binding.Connection == null && !IsSandboxVirtualMapping(source, mapping.SourceName, mapping.Fields))
                return Refuse("VIRTUAL_HOLON_QUERY_INVALID", "Use the approved sample collection fields for sandbox mappings.");

            return await mapping.QueryAsync(actor, binding.Reader, binding.Connection, matchField, matchValue,
                selectedFields, limit, cancellationToken);
        }
        catch (ArgumentException)
        {
            return Refuse("VIRTUAL_HOLON_CONNECTION_CHANGED", "The mapping no longer matches the named connection.");
        }
        catch (NativeConnectionRefusedException)
        {
            return Refuse("VIRTUAL_HOLON_SOURCE_UNAVAILABLE", "The mapping's native connection is unavailable.");
        }
    }

    internal sealed record VirtualConnectionBinding(INativeDataQueryProvider Reader, NativeConnection Connection, string Identity);

    internal static async Task<VirtualConnectionBinding> ResolveVirtualConnectionAsync(
        Guid actor, ProviderType provider, string sourceName, string connectionName)
    {
        if (connectionName == VirtualHolonMapping.SandboxConnectionName)
        {
            if (!ProviderFaultGate.IsEnabled || SandboxFields(provider, sourceName) == null)
                return null;
            var resolved = ProviderManager.Instance.ResolveStorageProvider(provider);
            return !resolved.IsError && resolved.Result is INativeDataQueryProvider configured
                ? new VirtualConnectionBinding(configured, null, configured.DescribeVirtualQueryConnection(null))
                : null;
        }

        if (OASISBootLoader.NativeImportReaders.Create(provider) is not INativeDataQueryProvider reader)
            return null;
        var connection = await Program.SettingsManager.GetNativeImportConnectionAsync(actor, provider, connectionName);
        return !connection.IsError && connection.Result != null
            ? new VirtualConnectionBinding(reader, connection.Result, reader.DescribeVirtualQueryConnection(connection.Result))
            : null;
    }

    internal static bool IsSandboxVirtualMapping(ProviderType provider, string sourceName,
        IReadOnlyDictionary<string, string> fields) =>
        ProviderFaultGate.IsEnabled && SandboxFields(provider, sourceName) is { } approved && fields != null &&
        fields.Values.All(field => approved.Contains(field, StringComparer.Ordinal));

    private static string[] SandboxFields(ProviderType provider, string sourceName) => (provider, sourceName) switch
    {
        (ProviderType.MongoDBOASIS, "customers") => new[] { "name", "city", "tier", "marketingOptIn" },
        (ProviderType.MongoDBOASIS, "products") => new[] { "title", "summary", "inStock" },
        (ProviderType.MongoDBOASIS, "orders") => new[] { "status" },
        (ProviderType.SQLLiteDBOASIS, "suppliers") => new[] { "name", "country", "description" },
        _ => null
    };

    private static OASISResult<IReadOnlyList<VirtualHolonRead>> Refuse(string code, string message) =>
        new OASISResult<IReadOnlyList<VirtualHolonRead>> { IsError = true, ErrorCode = code, Message = message };
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public sealed partial class CqlV1Providers
{
    Task<OASISResult<IReadOnlyList<VirtualHolonRead>>> ICqlVirtualMappingReader.QueryVirtualMappingAsync(
        Guid mappingId, Guid actor, CqlVirtualProjection projection, CancellationToken cancellationToken) =>
        QueryOwnedVirtualMappingAsync(CreateManager(Resolve(null)), mappingId, actor, null,
            projection.MatchField, projection.MatchValue, projection.ProjectedFields, CqlVirtualProjection.MaximumRows,
            cancellationToken);

    internal static async Task<OASISResult<IReadOnlyList<VirtualHolonRead>>> QueryOwnedVirtualMappingAsync(
        HolonManager manager, Guid mappingId, Guid actor, ProviderType? requestedSource, string matchField, object matchValue,
        IReadOnlyList<string> selectedFields, int limit, CancellationToken cancellationToken)
    {
        var context = manager.CreateRequestContext(new ProviderRoutingOverrides(
            autoFailoverEnabled: false, autoLoadBalancingEnabled: false));
        if (context.IsError)
            return Refuse("VIRTUAL_HOLON_STORE_UNAVAILABLE", "The mapping's authoritative store is unavailable.");

        cancellationToken.ThrowIfCancellationRequested();
        var loaded = await context.Result.Manager.LoadHolonAsync(mappingId,
            loadChildren: false, recursive: false, allowMissing: true);
        if (loaded.ErrorCode == OASISResultErrorCodes.RecordNotFound)
            return Refuse("VIRTUAL_MAPPING_NOT_FOUND", "The mapping is unavailable.");
        if (loaded.IsError)
            return Refuse("VIRTUAL_HOLON_STORE_UNAVAILABLE", "The mapping's authoritative store is unavailable.");
        if (loaded.Result == null || loaded.Result.Id != mappingId || !loaded.Result.IsActive)
            return Refuse("VIRTUAL_MAPPING_NOT_FOUND", "The mapping is unavailable.");

        string connectionName;
        ProviderType source;
        try
        {
            source = VirtualHolonMapping.GetDefinitionProvider(actor, loaded.Result);
            if (requestedSource.HasValue && requestedSource.Value != source)
                return Refuse("VIRTUAL_MAPPING_NOT_FOUND", "The mapping is unavailable.");
            connectionName = VirtualHolonMapping.GetDefinitionConnectionName(actor, loaded.Result, source);
        }
        catch (ArgumentException)
        {
            return Refuse("VIRTUAL_MAPPING_NOT_FOUND", "The mapping is unavailable.");
        }

        try
        {
            var binding = await ResolveVirtualConnectionAsync(actor, source, loaded.Result.Name, connectionName);
            cancellationToken.ThrowIfCancellationRequested();
            if (binding == null)
                return Refuse("VIRTUAL_HOLON_SOURCE_UNAVAILABLE", "The mapping's native connection is unavailable.");

            var mapping = VirtualHolonMapping.FromDefinitionHolon(actor, loaded.Result, binding.Identity);
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

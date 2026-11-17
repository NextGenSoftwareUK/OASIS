using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public sealed partial class CqlV1Providers
{
    async Task<OASISResult<IReadOnlyList<VirtualHolonRead>>> ICqlVirtualMappingReader.QueryVirtualMappingAsync(
        Guid mappingId, Guid actor, CqlVirtualProjection projection, CancellationToken cancellationToken)
    {
        var manager = CreateManager(Resolve(null));
        var context = manager.CreateRequestContext(new ProviderRoutingOverrides(
            autoFailoverEnabled: false, autoLoadBalancingEnabled: false));
        if (context.IsError)
            return Refuse( "VIRTUAL_HOLON_STORE_UNAVAILABLE", "The mapping's authoritative store is unavailable.");

        cancellationToken.ThrowIfCancellationRequested();
        var loaded = await context.Result.Manager.LoadHolonAsync(mappingId,
            loadChildren: false, recursive: false, allowMissing: true);
        if (loaded.ErrorCode == OASISResultErrorCodes.RecordNotFound)
            return Refuse( "VIRTUAL_MAPPING_NOT_FOUND", "The mapping is unavailable.");
        if (loaded.IsError)
            return Refuse( "VIRTUAL_HOLON_STORE_UNAVAILABLE", "The mapping's authoritative store is unavailable.");
        if (loaded.Result == null || loaded.Result.Id != mappingId || !loaded.Result.IsActive)
            return Refuse( "VIRTUAL_MAPPING_NOT_FOUND", "The mapping is unavailable.");

        // The first shipping virtual reader is the MongoDB native source. The
        // imported branch retains its independently admitted provider selection.
        const ProviderType source = ProviderType.MongoDBOASIS;
        string connectionName;
        try
        {
            connectionName = VirtualHolonMapping.GetDefinitionConnectionName(actor, loaded.Result, source);
        }
        catch (ArgumentException)
        {
            return Refuse( "VIRTUAL_MAPPING_NOT_FOUND", "The mapping is unavailable.");
        }

        var connection = await Program.SettingsManager.GetNativeImportConnectionAsync(actor, source, connectionName);
        cancellationToken.ThrowIfCancellationRequested();
        if (connection.IsError || connection.Result == null ||
            OASISBootLoader.NativeImportReaders.Create(source) is not INativeDataQueryProvider reader)
            return Refuse( "VIRTUAL_HOLON_SOURCE_UNAVAILABLE", "The mapping's native connection is unavailable.");

        VirtualHolonMapping mapping;
        try
        {
            mapping = VirtualHolonMapping.FromDefinitionHolon(actor, loaded.Result,
                reader.DescribeVirtualQueryConnection(connection.Result));
        }
        catch (ArgumentException)
        {
            return Refuse( "VIRTUAL_HOLON_CONNECTION_CHANGED", "The mapping no longer matches the named connection.");
        }
        catch (NativeConnectionRefusedException)
        {
            return Refuse( "VIRTUAL_HOLON_SOURCE_UNAVAILABLE", "The mapping's native connection is unavailable.");
        }

        return await mapping.QueryAsync(actor, reader, connection.Result, projection.MatchField,
            projection.MatchValue, projection.ProjectedFields, CqlVirtualProjection.MaximumRows, cancellationToken);
    }

    private static OASISResult<IReadOnlyList<VirtualHolonRead>> Refuse(string code, string message) =>
        new OASISResult<IReadOnlyList<VirtualHolonRead>> { IsError = true, ErrorCode = code, Message = message };
}

using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace CosmicCqlDemo;

public sealed record NativeImportCqlResult(NativeDataImportReport Import, IReadOnlyList<Holon> Rows);

/// <summary>Native data becomes ordinary holons, then uses the shared CQL path.</summary>
public static class NativeImportCqlWalkthrough
{
    public static async Task<NativeImportCqlResult> RunAsync(
        HolonManager manager,
        INativeDataImportProvider source,
        ICosmicQueryStorageProvider target,
        string sourceName,
        Guid actor,
        TextWriter output,
        CancellationToken cancellationToken = default)
    {
        // 1. Read the application's native records in bounded pages. The shared
        // manager maps stable per-avatar IDs and saves through HolonManager.
        var importer = new NativeDataImportManager(manager);
        var mapping = NativeDataMapping.ForSource(sourceName);
        var imported = Require(await importer.ImportSourceAsync(source, sourceName, actor,
            mapping, pageSize: 2, maximumRecords: 10, cancellationToken: cancellationToken));
        if (imported.RecordsFailed != 0 || imported.Truncated)
            throw new InvalidOperationException("The bounded demo import did not complete; preserve its fixture journal.");

        // 2. The source's native key remains provenance. CQL queries the saved
        // holons on the explicitly selected target, with actor visibility applied.
        // This synthetic demo does not enroll metadata encryption or replication.
        var sourceProvider = source.NativeDataProviderType.ToString();
        var scope = Require(CosmicQueryScope.Named(target.QueryProviderType));
        var access = Require(CosmicQueryReadAccess.OwnerOrPublic(actor));
        var query = Require(CosmicQuery<Holon>.Create(scope).WithReadAccess(access)
            .Where(row => CosmicQueryMetadata.Value<string>(row, NativeRecordHolonMapper.SourceMetaDataKey) == sourceName &&
                CosmicQueryMetadata.Value<string>(row, NativeRecordHolonMapper.ProviderMetaDataKey) == sourceProvider));
        var linq = Require(CosmicLinq.Create(manager, target, query, maximumResults: 10));
        var rows = Require(await linq.OrderBy(row => row.Name).ToCosmicListAsync(cancellationToken));

        await output.WriteLineAsync("\n=== Native records -> COSMIC holons -> CQL ===\n");
        await output.WriteLineAsync($"Source: {sourceProvider}/{sourceName}; target: {target.QueryProviderType}");
        await output.WriteLineAsync($"Read {imported.RecordsRead}; created {imported.HolonsCreated}; updated {imported.HolonsUpdated}; unchanged {imported.HolonsUnchanged}\n");
        foreach (var row in rows)
            await output.WriteLineAsync($"  {row.Name}: {row.Id}\n");

        return new NativeImportCqlResult(imported, rows);
    }

    private static T Require<T>(OASISResult<T> result)
    {
        if (result.IsError || result.Result == null)
            throw new InvalidOperationException(result.ErrorCode + ": " + result.Message, result.Exception);
        return result.Result;
    }
}

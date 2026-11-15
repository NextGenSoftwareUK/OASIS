using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private const string MetadataMatchFunction = "oasis_match_holon_metadata";

    public async Task<OASISResult<IEnumerable<IHolon>>> LoadMatchingHolonsAsync(
        Dictionary<string, string> filter, MetaKeyValuePairMatchMode mode, HolonType type,
        bool loadChildren, bool recursive, int maxChildDepth, int currentChildDepth,
        bool continueOnError, int version)
    {
        SqliteReadVersion.Validate(version);
        ValidateReadTraversal(maxChildDepth, currentChildDepth);
        if (filter.Count == 0)
            return await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, currentChildDepth, continueOnError, version);

        List<IHolon> matches;
        if (version > 0)
        {
            matches = ReadHolonRevisions(version, type)
                .Where(holon => StoredRecordSearch.MatchesMetadata(holon.MetaData, filter, mode))
                .ToList();
        }
        else
        {
            var connection = (SqliteConnection)_dbContext.Database.GetDbConnection();
            // Use the same comparison as historical reads for JSON scalars and legacy values.
            connection.CreateFunction<string, bool>(MetadataMatchFunction, json =>
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(json)
                    ?? throw new InvalidDataException("Stored holon metadata must be a JSON object.");
                return StoredRecordSearch.MatchesMetadata(metadata, filter, mode);
            }, isDeterministic: true);

            try
            {
                var rows = await _dbContext.Holons.FromSqlRaw(
                    "SELECT * FROM Holons WHERE CASE WHEN $allTypes OR HolonType = $type " +
                    "THEN " + MetadataMatchFunction + "(MetaDataJson) ELSE 0 END",
                    new SqliteParameter("$allTypes", type == HolonType.All),
                    new SqliteParameter("$type", (int)type))
                    .AsNoTracking().ToListAsync();
                matches = rows.Select(GetHolonFromEntity).ToList();
            }
            finally
            {
                connection.CreateFunction<string, bool>(MetadataMatchFunction, null);
            }
        }

        if (loadChildren && matches.Count > 0)
        {
            var graph = await _dbContext.Holons.AsNoTracking().ToListAsync();
            foreach (var match in matches)
                AttachLoadedChildren(match, graph, recursive, maxChildDepth, currentChildDepth);
        }

        return new OASISResult<IEnumerable<IHolon>>(matches)
        {
            IsLoaded = true,
            LoadedCount = matches.Count,
            Message = $"Successfully loaded {matches.Count} holons by metadata pairs from SQLite"
        };
    }
}

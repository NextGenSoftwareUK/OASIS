using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private async Task<IReadOnlyList<IHolon>> ReadCqlChildrenAsync(CosmicQueryChildSelection selection,
        SqliteCqlReadBudget budget, CancellationToken cancellationToken)
    {
        var rows = new List<IHolon>();
        long bytes = 0;
        var parentId = selection.ParentId;
        var childType = selection.ChildType;
        // Use the existing translator and exact domains; membership is batched
        // within its declared 128-literal limit, never evaluated on fetched rows.
        var count = selection.MemberIds?.Count ?? 1;
        for (var offset = 0; offset < count; offset += CosmicQueryExpression.MaximumMembershipValues)
        {
            budget.Check();
            var query = CosmicQuery<Holon>.Create().Where(row => row.ParentHolonId == parentId);
            if (selection.ChildType != HolonType.All)
                query = query.Where(row => row.HolonType == childType);
            if (selection.MemberIds != null)
            {
                var ids = selection.MemberIds.Skip(offset).Take(CosmicQueryExpression.MaximumMembershipValues).ToArray();
                query = query.Where(row => ids.Contains(row.Id));
            }
            if (query.IsError) throw new NotSupportedException(query.Message);
            var translator = new CqlSqlTranslator();
            var predicate = string.Join(" AND ", query.Result.Predicates.Select(translator.Translate));
            translator.Parameters.Add(new SqliteParameter("$graphLimit", selection.MaximumNodes - rows.Count + 1));
            var native = _dbContext.Holons.FromSqlRaw(
                "SELECT * FROM Holon WHERE " + predicate + " ORDER BY lower(Id) LIMIT $graphLimit",
                translator.Parameters.Cast<object>().ToArray()).AsNoTracking();
            await foreach (var row in native.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                budget.Check();
                if (rows.Count >= selection.MaximumNodes)
                    throw new SqliteCqlResourceException("SQLite CQL children exceed their graph node budget.");
                bytes += CqlRecordSize(row);
                if (bytes > selection.MaximumPayloadBytes)
                    throw new SqliteCqlResourceException("SQLite CQL children exceed their graph payload budget.");
                rows.Add(GetHolonFromEntity(row));
            }
        }
        budget.Check();
        return rows.AsReadOnly();
    }

    private static long CqlRecordSize(Entities.HolonModel row)
    {
        long bytes = 1024;
        foreach (var property in CqlStringColumns)
        {
            var value = (string)property.GetValue(row);
            if (value != null) bytes += Encoding.UTF8.GetByteCount(value);
        }
        return bytes;
    }
}

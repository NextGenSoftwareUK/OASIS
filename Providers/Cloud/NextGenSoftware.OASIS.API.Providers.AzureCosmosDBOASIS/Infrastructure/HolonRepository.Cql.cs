using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

public partial class HolonRepository
{
    internal async Task<IReadOnlyList<IHolon>> ExecuteCqlRowsAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults, CancellationToken cancellationToken) where T : IHolon
    {
        var command = CosmosCqlRowCommand.Compile(plan, maximumResults);
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await ReadBoundedCurrentQueryAsync(command, maximumResults, cancellationToken).ConfigureAwait(false);
        return ValidateCqlLeafRows(rows, plan.Query.ReadAccess, cancellationToken);
    }

    internal static IReadOnlyList<IHolon> ValidateCqlLeafRows(IReadOnlyList<IHolon> rows,
        CosmicQueryReadAccess access, CancellationToken cancellationToken)
    {
        // Defense on a complete canonical result, never managed filtering to
        // compensate for missing native visibility predicates.
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (access != null && !access.CanRead(row))
                throw new InvalidDataException("Cosmos native query violated its mandatory visibility restriction.");
        }
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            row.Children = new List<IHolon>();
        }
        cancellationToken.ThrowIfCancellationRequested();
        return rows;
    }
}

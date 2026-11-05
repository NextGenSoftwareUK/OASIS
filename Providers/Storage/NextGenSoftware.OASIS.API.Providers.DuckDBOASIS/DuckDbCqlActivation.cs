using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DuckDBOASIS;

public partial class DuckDBOASIS : ICosmicReadOnlyQueryActivation
{
    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var options = new DbConnectionStringBuilder { ConnectionString = _connectionString };
            if (!options.TryGetValue("access_mode", out var mode) ||
                !string.Equals(mode?.ToString(), "READ_ONLY", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("DuckDB query activation requires access_mode=READ_ONLY.");

            await using var connection = new DuckDBConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, doc FROM oasis_holons LIMIT 0";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (reader.GetFieldType(0) != typeof(string) || reader.GetFieldType(1) != typeof(string))
                throw new InvalidOperationException("DuckDB query source columns have incompatible types.");
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new()
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = error is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "DuckDB query activation did not verify an existing read-only source.",
                Exception = error
            };
        }
    }
}

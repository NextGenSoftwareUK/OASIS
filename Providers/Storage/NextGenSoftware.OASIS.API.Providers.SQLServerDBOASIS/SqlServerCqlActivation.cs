using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS;

public partial class SQLServerDBOASIS : ICosmicReadOnlyQueryActivation
{
    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var connection = OpenConnection();
            await connection.OpenAsync(cancellationToken);
            // Resolve the query columns and verify SELECT access without reading rows or provisioning tables.
            await using var command = new SqlCommand("SELECT TOP (0) Id, DataJson, IsDeleted FROM OASISHolons", connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (reader.GetFieldType(0) != typeof(Guid) || reader.GetFieldType(1) != typeof(string) ||
                reader.GetFieldType(2) != typeof(bool))
                throw new InvalidOperationException("SQL Server CQL source columns have incompatible types.");
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
                Message = "SQL Server query activation did not verify the existing source.",
                Exception = error
            };
        }
    }
}

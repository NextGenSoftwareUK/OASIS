using System;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS : ICosmicReadOnlyQueryActivation
{
    /// <summary>
    /// Verify the existing canonical provider schema without creating or repairing
    /// any objects. Hosted query enrollment must use this read-only contract.
    /// </summary>
    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Reuse the canonical schema validator. In particular, never call
            // ActivateProviderAsync: that explicit storage setup may provision DDL.
            await ValidateStorageSchemaAsync(connection, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true) { Message = "Existing Oracle query source verified without provisioning." };
        }
        catch (OperationCanceledException error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_QUERY_CANCELLED",
                Message = "Oracle query activation cancelled.", Exception = error };
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "Existing Oracle query source failed verification; no provisioning was attempted.", Exception = error };
        }
    }
}

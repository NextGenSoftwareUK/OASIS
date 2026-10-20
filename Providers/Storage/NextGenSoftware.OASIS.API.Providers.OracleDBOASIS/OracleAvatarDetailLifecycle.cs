using System;
using System.IO;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS;

public partial class OracleDBOASIS
{
    public Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id, bool softDelete = true)
        => DeleteDetailRecordAsync("ID", OracleDbType.Raw, id.ToByteArray(), softDelete);
    public OASISResult<bool> DeleteAvatarDetail(Guid id, bool softDelete = true)
        => DeleteAvatarDetailAsync(id, softDelete).GetAwaiter().GetResult();
    public Task<OASISResult<bool>> DeleteAvatarDetailByUsernameAsync(string username, bool softDelete = true)
        => DeleteDetailRecordAsync("USERNAME", OracleDbType.Varchar2, username, softDelete);
    public OASISResult<bool> DeleteAvatarDetailByUsername(string username, bool softDelete = true)
        => DeleteAvatarDetailByUsernameAsync(username, softDelete).GetAwaiter().GetResult();
    public Task<OASISResult<bool>> DeleteAvatarDetailByEmailAsync(string email, bool softDelete = true)
        => DeleteDetailRecordAsync("EMAIL", OracleDbType.Varchar2, email, softDelete);
    public OASISResult<bool> DeleteAvatarDetailByEmail(string email, bool softDelete = true)
        => DeleteAvatarDetailByEmailAsync(email, softDelete).GetAwaiter().GetResult();

    private async Task<OASISResult<bool>> DeleteDetailRecordAsync(string column, OracleDbType type, object value, bool softDelete)
    {
        var result = new OASISResult<bool>();
        var dispatched = false; var id = Guid.Empty;
        try
        {
            if (column == "ID" ? new Guid((byte[])value) == Guid.Empty : string.IsNullOrWhiteSpace(value as string))
                throw new ArgumentException("A nonempty AvatarDetail selector is required.");
            await using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = (OracleTransaction)await connection.BeginTransactionAsync();
            AvatarDetail row; string original;
            // Only fixed internal column names enter this query; selector values are bound.
            await using (var select = CreateCommand($"SELECT ID,DATA_JSON,USERNAME,EMAIL FROM OASIS_AVATAR_DETAILS WHERE {column}=:Selector AND ROWNUM<=2 FOR UPDATE", connection))
            {
                select.Transaction = transaction; select.Parameters.Add("Selector", type).Value = value;
                await using var reader = await select.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return new OASISResult<bool> { IsError = true, ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "Oracle AvatarDetail was not found." };
                id = new Guid((byte[])reader.GetValue(0)); original = reader.GetString(1);
                row = await ReadRecordVersionAsync<AvatarDetail>(id, original, 0,null,ReadNativeIdentityProjection(reader,2))
                    ?? throw new InvalidDataException("Oracle locked detail is missing its current version.");
                if (row.Id != id || column != "ID" && !CanonicalIdentityMatches(row, column, value))
                    throw new InvalidDataException("Oracle detail indexed identity differs from its canonical record.");
                if (await reader.ReadAsync()) throw new InvalidDataException("Oracle detail delete selector matched multiple records.");
            }
            if (!(softDelete && row.IsDeleted && !row.IsActive))
            {
                await using var command = CreateCommand(softDelete
                    ? "UPDATE OASIS_AVATAR_DETAILS SET DATA_JSON=:Json WHERE ID=:Id"
                    : "DELETE FROM OASIS_AVATAR_DETAILS WHERE ID=:Id", connection);
                command.Transaction = transaction; command.Parameters.Add("Id", OracleDbType.Raw).Value = id.ToByteArray();
                if (softDelete)
                {
                    row.IsActive = false; if (!row.IsDeleted) row.DeletedDate = DateTime.UtcNow;
                    command.Parameters.Add("Json", OracleDbType.Clob).Value = await StageRecordHistoryAsync(row, original);
                }
                dispatched = true;
                if (await command.ExecuteNonQueryAsync() != 1) throw new InvalidDataException("Oracle locked detail changed unexpectedly.");
                await CommitLifecycleTransactionAsync(transaction);
            }
            result.Result = true; result.IsSaved = true; result.IsDeleted = true; result.DeletedCount = 1;
        }
        catch (Exception cause)
        {
            var failure = dispatched ? new OracleLifecycleReconciliationException(id, cause) : cause;
            OASISErrorHandling.HandleError(ref result, "Oracle AvatarDetail lifecycle failed; inspect the original exception.", failure);
            if (!dispatched && cause is ArgumentException) result.ErrorCode = "HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }
}

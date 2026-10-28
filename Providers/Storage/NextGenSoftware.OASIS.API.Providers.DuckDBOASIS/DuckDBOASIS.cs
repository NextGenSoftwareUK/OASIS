using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.DuckDBOASIS;

/// <summary>Thin DuckDB adapter using native SQL and canonical shared record policy.
/// Existing legacy three-table files are preserved; automatic migration is not performed.</summary>
public class DuckDBOASIS : KeyValueStorageProviderBase
{
    public DuckDBOASIS(string connectionString = "Data Source=oasis.duckdb")
        : base(new DuckDbRecordBackend(Require(connectionString)))
    {
        ProviderName = "DuckDBOASIS";
        ProviderDescription = "DuckDB embedded SQL with shared OASIS record policy";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.DuckDBOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
    }
    private static string Require(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString); return connectionString;
    }
    public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => DeleteHolonRecordAsync(id, true);
    public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
    {
        var result = await DeleteHolonRecordAsync(id, softDelete);
        return new OASISResult<bool>(result.Result != null && result.IsDeleted) {
            IsError=result.IsError, Exception=result.Exception, ErrorCode=result.ErrorCode,
            Message=result.Message, IsDeleted=result.IsDeleted, DeletedCount=result.DeletedCount };
    }
    public OASISResult<bool> DeleteHolonSoft(Guid id, bool softDelete = true)
        => DeleteHolonSoftAsync(id, softDelete).GetAwaiter().GetResult();
}

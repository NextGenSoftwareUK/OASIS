using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;

public partial class PostgreSQLOASIS
{
    private static async Task ExecuteStoredWriteAsync(NpgsqlCommand command,Guid recordId)
    {
        try{
            if(await command.ExecuteNonQueryAsync()!=1)
                throw new PostgreSqlWriteReconciliationException(recordId,new InvalidDataException("PostgreSQL upsert did not acknowledge exactly one row."));
        }catch(NpgsqlException cause)when(cause is not PostgresException){
            throw new PostgreSqlWriteReconciliationException(recordId,cause);
        }
    }

    private void PublishAcknowledgedIdentity<T>(T source,T stored,StoredRecordPublicationSnapshot<T> publication) where T:class,IHolonBase
    {
        try{
            publication.ValidateUnchanged();
            source.Id=stored.Id;source.ProviderUniqueStorageKey??=new();
            source.ProviderUniqueStorageKey[ProviderType.Value]=stored.Id.ToString();source.IsNewHolon=false;
        }catch(Exception cause){throw new PostgreSqlWriteReconciliationException(stored.Id,cause);}
    }
}

public sealed class PostgreSqlWriteReconciliationException(Guid recordId,Exception cause)
    :Exception("PostgreSQL write outcome requires original-record reconciliation before replay.",cause),INativeMutationReconciliationRequired
{
    public Guid RecordId{get;}=recordId;
    public string OriginalTransactionId=>string.Empty;
}

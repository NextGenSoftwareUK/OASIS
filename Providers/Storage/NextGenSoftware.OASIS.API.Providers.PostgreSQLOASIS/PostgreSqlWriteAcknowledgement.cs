using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS;

public partial class PostgreSQLOASIS
{
    private void PublishAcknowledgedIdentity<T>(T source,T stored,StoredRecordPublicationSnapshot<T> publication) where T:class,IHolonBase
    {
        try{
            publication.ValidateUnchanged();
            source.Id=stored.Id;source.ProviderUniqueStorageKey??=new();
            source.ProviderUniqueStorageKey[ProviderType.Value]=stored.Id.ToString();source.IsNewHolon=false;
            source.Version=stored.Version;source.VersionId=stored.VersionId;source.PreviousVersionId=stored.PreviousVersionId;
        }catch(Exception cause){throw new PostgreSqlWriteReconciliationException(stored.Id,cause);}
    }
}

public sealed class PostgreSqlWriteReconciliationException(Guid recordId,Exception cause)
    :Exception("PostgreSQL write outcome requires original-record reconciliation before replay.",cause),INativeMutationReconciliationRequired
{
    public Guid RecordId{get;}=recordId;
    public string OriginalTransactionId=>string.Empty;
}

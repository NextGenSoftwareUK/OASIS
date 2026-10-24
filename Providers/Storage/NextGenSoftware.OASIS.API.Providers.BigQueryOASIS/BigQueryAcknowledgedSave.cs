using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS;

public partial class BigQueryOASIS
{
    private async Task<OASISResult<T>> SaveAcknowledgedRecordAsync<T>(T source,string table)where T:class,IHolonBase
    {
        var result=new OASISResult<T>();var acknowledged=false;var id=Guid.Empty;BigQueryWriteAcknowledgement? native=null;
        try{
            var snapshot=StoredRecordPublicationSnapshot<T>.Capture(source);var proposed=snapshot.Proposal;
            if(proposed.Id==Guid.Empty)proposed.Id=Guid.NewGuid();id=proposed.Id;
            proposed.ProviderUniqueStorageKey??=new();proposed.ProviderUniqueStorageKey[Core.Enums.ProviderType.BigQueryOASIS]=id.ToString();
            native=await UpsertDocAsync(table,id.ToString(),proposed);acknowledged=true;result.SavedCount=1;
            snapshot.ValidateUnchanged();
            source.Id=id;source.ProviderUniqueStorageKey??=new();source.ProviderUniqueStorageKey[Core.Enums.ProviderType.BigQueryOASIS]=id.ToString();
            result.Result=source;result.IsSaved=true;
        }catch(Exception cause){
            if(acknowledged)cause=new BigQueryRecordReconciliationException(id,_projectId,native!.JobId,native.Location,true,cause);
            if(cause is BigQueryRecordReconciliationException outcome&&outcome.WriteAcknowledged)result.SavedCount=1;
            OASISErrorHandling.HandleError(ref result,"BigQuery record save failed; inspect its original outcome before replay.",cause);
        }
        return result;
    }
}

public sealed class BigQueryRecordReconciliationException:Exception,INativeMutationReconciliationRequired
{
    public Guid RecordId{get;}
    public string ProjectId{get;}
    public string OriginalTransactionId{get;}
    public string JobLocation{get;}
    public bool WriteAcknowledged{get;}
    public BigQueryRecordReconciliationException(Guid id,string project,string job,string location,bool acknowledged,Exception cause)
        :base("BigQuery record mutation requires reconciliation of the original job/record before replay.",cause)
    {RecordId=id;ProjectId=project;OriginalTransactionId=job;JobLocation=location;WriteAcknowledged=acknowledged;}
}

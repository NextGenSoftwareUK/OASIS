using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public sealed class NearRecordMutationUncertainException:Exception,INativeMutationReconciliationRequired
{
    public string OriginalTransactionId {get;}
    internal NearRecordMutationUncertainException(string id,Exception cause):base("NEAR conditional record outcome requires reconciliation before replay.",cause)
        =>OriginalTransactionId=id??string.Empty;
}
public sealed class NearRecordWriteRejectedException:Exception
{
    public string OriginalTransactionId {get;}
    public string NativeStatusJson {get;}
    public bool IsRecordConflict {get;}
    internal NearRecordWriteRejectedException(string id,string status):base("The original NEAR conditional write finalized with native Failure.")
    {
        OriginalTransactionId=id;NativeStatusJson=status;
        using var document=System.Text.Json.JsonDocument.Parse(status);
        var node=document.RootElement;
        foreach(var name in new[]{"Failure","ActionError","kind","FunctionCallError","ExecutionError"}){
            if(node.ValueKind!=System.Text.Json.JsonValueKind.Object||!node.TryGetProperty(name,out node))return;
        }
        if(node.ValueKind==System.Text.Json.JsonValueKind.String){
            var firstLine=node.GetString().Split('\n')[0];
            IsRecordConflict=firstLine is "Smart contract panicked: OASIS_RECORD_CONFLICT" or "Smart contract panicked: OASIS_ARCHIVE_CONFLICT";
        }
    }
}
internal sealed class NearRecordAdmissionException:InvalidOperationException
{
    internal NearRecordAdmissionException(string message):base(message){}
}

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
    internal NearRecordWriteRejectedException(string id,string status):base("The original NEAR conditional write finalized with native Failure.")
    {OriginalTransactionId=id;NativeStatusJson=status;}
}
internal sealed class NearRecordAdmissionException:InvalidOperationException
{
    internal NearRecordAdmissionException(string message):base(message){}
}

using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public sealed class NearMutationUncertainException:Exception,INativeMutationReconciliationRequired
{
    public string OriginalTransactionId {get;}
    internal NearMutationUncertainException(string id,Exception cause)
        :base("The original NEAR mutation outcome requires reconciliation before replay.",cause)
        =>OriginalTransactionId=id??string.Empty;
}

public sealed class NearMutationRejectedException:Exception
{
    public string OriginalTransactionId {get;}
    public string NativeStatusJson {get;}
    internal NearMutationRejectedException(string id,string status)
        :base("The original NEAR mutation finalized with native Failure.")
    {OriginalTransactionId=id;NativeStatusJson=status;}
}

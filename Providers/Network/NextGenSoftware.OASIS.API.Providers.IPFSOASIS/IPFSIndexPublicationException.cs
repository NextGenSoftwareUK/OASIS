using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

public sealed class IPFSIndexPublicationException:Exception,INativeMutationReconciliationRequired
{
    public string OriginalTransactionId => ProposedIndexCid;
    public string ProposedIndexCid {get;}
    public string PreviousIndexCid {get;}
    public IPFSIndexPublicationException(string proposedIndexCid,string previousIndexCid,Exception cause)
        :base("IPFS acknowledged a new index CID, but root-pointer persistence failed; reconcile before replay.",cause)
    {
        if(string.IsNullOrWhiteSpace(proposedIndexCid)) throw new ArgumentException("Acknowledged index CID is required.",nameof(proposedIndexCid));
        ProposedIndexCid=proposedIndexCid; PreviousIndexCid=previousIndexCid;
    }
}

using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

public sealed class IPFSRecordPublicationException : Exception, INativeMutationReconciliationRequired
{
    public string RecordCid { get; }
    public string OriginalTransactionId => RecordCid;

    public IPFSRecordPublicationException(string recordCid, Exception cause)
        : base("IPFS acknowledged a record CID, but publishing its index failed; reconcile before replay.", cause)
    {
        if (string.IsNullOrWhiteSpace(recordCid)) throw new ArgumentException("Acknowledged record CID required.", nameof(recordCid));
        RecordCid = recordCid;
    }
}

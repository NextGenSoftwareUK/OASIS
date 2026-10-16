using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

public sealed class IPFSRecordPublicationException : Exception, INativeMutationReconciliationRequired
{
    public string RecordCid { get; }
    public IReadOnlyList<string> AcknowledgedRecordCids { get; }
    public string OriginalTransactionId => RecordCid;

    public IPFSRecordPublicationException(string recordCid, Exception cause)
        : this(new[] {recordCid},cause) { }

    public IPFSRecordPublicationException(IEnumerable<string> recordCids, Exception cause)
        : base("IPFS acknowledged record CIDs before the operation failed; reconcile before replay.", cause)
    {
        var receipts=recordCids?.ToArray();
        if (receipts==null || receipts.Length==0 || receipts.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Acknowledged record CIDs required.", nameof(recordCids));
        AcknowledgedRecordCids=Array.AsReadOnly(receipts);
        RecordCid = receipts[0];
    }
}

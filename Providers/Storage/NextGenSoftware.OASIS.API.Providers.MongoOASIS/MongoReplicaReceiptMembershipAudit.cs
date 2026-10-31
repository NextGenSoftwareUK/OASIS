using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

/// <summary>Reviewable migration intent containing identities and hashes only, never receipt payloads or credentials.</summary>
public sealed class MongoReplicaReceiptMembershipAudit
{
    public sealed class Entry
    {
        public Guid OperationId { get; }
        public string ReceiptHash { get; }
        public IReadOnlyList<Guid> HolonIds { get; }
        public Entry(Guid operationId,string receiptHash,IEnumerable<Guid> holonIds)
        {
            var ids=holonIds?.OrderBy(id=>id.ToString("D"),StringComparer.Ordinal).ToArray() ?? throw new ArgumentNullException(nameof(holonIds));
            if(operationId==Guid.Empty || ids.Length==0 || ids.Any(id=>id==Guid.Empty) || ids.Distinct().Count()!=ids.Length)
                throw new ArgumentException("Nonempty unique receipt and holon identities are required.");
            OperationId=operationId;ReceiptHash=Hash(receiptHash);HolonIds=Array.AsReadOnly(ids);
        }
    }
    public string StoreHash { get; }
    public Guid OperatorId { get; }
    public IReadOnlyList<Entry> Entries { get; }
    public string Fingerprint { get; }
    public MongoReplicaReceiptMembershipAudit(string storeHash,Guid operatorId,IEnumerable<Entry> entries)
    {
        var rows=entries?.Take(129).ToArray() ?? throw new ArgumentNullException(nameof(entries));
        if(operatorId==Guid.Empty || rows.Length is <1 or >128 || rows.Any(row=>row==null) || rows.Select(row=>row.OperationId).Distinct().Count()!=rows.Length)
            throw new ArgumentException("An explicit batch of 1–128 unique audited receipts is required.");
        rows=rows.OrderBy(row=>row.OperationId.ToString("D"),StringComparer.Ordinal).ToArray();
        StoreHash=Hash(storeHash);OperatorId=operatorId;Entries=Array.AsReadOnly(rows);
        Fingerprint=Digest(JsonConvert.SerializeObject(new{format=1,store=StoreHash,operatorId=OperatorId.ToString("D"),rows=rows.Select(row=>new{
            operation=row.OperationId.ToString("D"),hash=row.ReceiptHash,ids=row.HolonIds.Select(id=>id.ToString("D"))})}));
    }
    internal static string Digest(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string Hash(string value)=>value!=null && value.Length==64 && value.All(Uri.IsHexDigit)
        ? value.ToUpperInvariant():throw new ArgumentException("An exact SHA256 hash is required.");
}

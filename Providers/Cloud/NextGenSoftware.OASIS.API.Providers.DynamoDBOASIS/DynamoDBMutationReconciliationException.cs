using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

/// <summary>An SDK mutation attempt failed without authoritative no-execution evidence. Not a commit receipt.</summary>
public sealed class DynamoDBMutationReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string TableName { get; }
    public string RecordKey { get; }
    // DynamoDB has no transaction hash for ordinary item writes; do not invent one.
    public string OriginalTransactionId => string.Empty;
    public string SdkRequestId { get; }
    public DynamoDBMutationReconciliationException(string tableName,string recordKey,Exception cause)
        :base("DynamoDB mutation outcome requires reconciliation before cross-provider replay.",cause)
    {
        TableName=tableName; RecordKey=recordKey;
        SdkRequestId=(cause as Amazon.Runtime.AmazonServiceException)?.RequestId ?? string.Empty;
    }
}

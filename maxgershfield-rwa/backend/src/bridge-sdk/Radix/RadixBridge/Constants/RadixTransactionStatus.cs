namespace RadixBridge.Constants;

/// <summary>
/// Defines constants representing the possible statuses of a Radix transaction.
/// These statuses are used to track the state of a transaction on the Radix network.
/// </summary>
public static class RadixTransactionStatus
{
    /// <summary>
    /// The transaction was successfully committed to the ledger.
    /// </summary>
    public const string CommittedSuccess = "CommittedSuccess";

    /// <summary>
    /// The transaction was committed to the ledger but failed during execution.
    /// </summary>
    public const string CommittedFailure = "CommittedFailure";

    /// <summary>
    /// The transaction was permanently rejected and will not be included in the ledger.
    /// </summary>
    public const string PermanentRejection = "PermanentRejection";

    /// <summary>
    /// The transaction is currently in the mempool, waiting to be included in a block.
    /// </summary>
    public const string InMemPool = "InMempool";

    /// <summary>
    /// The transaction has not been seen by the network.
    /// </summary>
    public const string NotSeen = "NotSeen";

    /// <summary>
    /// The transaction's outcome is uncertain, and its final status is unknown.
    /// </summary>
    public const string FateUncertain = "FateUncertain";

    /// <summary>
    /// The transaction's outcome is uncertain, but it is likely to be rejected.
    /// </summary>
    public const string FateUncertainButLikelyRejection = "FateUncertainButLikelyRejection";
}
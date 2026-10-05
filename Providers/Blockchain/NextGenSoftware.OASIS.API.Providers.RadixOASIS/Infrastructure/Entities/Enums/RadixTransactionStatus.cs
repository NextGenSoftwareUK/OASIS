namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities.Enums;

/// <summary>
/// Radix transaction status from the Core API
/// </summary>
public static class RadixTransactionStatus
{
    public const string CommittedSuccess = "CommittedSuccess";
    public const string CommittedFailure = "CommittedFailure";
    public const string NotSeen = "NotSeen";
    public const string InMemPool = "InMempool";
    public const string PermanentRejection = "PermanentRejection";
    public const string FateUncertainButLikelyRejection = "FateUncertainButLikelyRejection";
    public const string FateUncertain = "FateUncertain";
}


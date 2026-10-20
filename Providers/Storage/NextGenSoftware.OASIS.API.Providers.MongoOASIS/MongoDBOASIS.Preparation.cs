using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : IHolonWritePreparationStorageProvider
{
    public OASISResult<HolonWritePreparation> ClaimHolonWritePreparation(HolonWritePreparation candidate)
        => ClaimHolonWritePreparationAsync(candidate).GetAwaiter().GetResult();
    public Task<OASISResult<HolonWritePreparation>> ClaimHolonWritePreparationAsync(HolonWritePreparation candidate,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return Task.FromResult(PreparationAdmissionFailure("COSMIC_PREPARATION_CANCELLED", "Preparation was cancelled before repository access.", new OperationCanceledException(cancellationToken)));
        if (candidate == null) return Task.FromResult(PreparationAdmissionFailure("COSMIC_PREPARATION_INVALID", "A protected preparation candidate is required."));
        if (candidate.Authority != Core.Enums.ProviderType.MongoDBOASIS) return Task.FromResult(PreparationAdmissionFailure("COSMIC_AUTHORITY_MISMATCH", "Preparation belongs to another provider authority."));
        if (_holonRepository == null) return Task.FromResult(PreparationAdmissionFailure("COSMIC_PREPARATION_PROVIDER_NOT_READY", "Activate the MongoDB provider before claiming preparation."));
        return _holonRepository.ClaimHolonWritePreparationAsync(candidate, cancellationToken);
    }
    private static OASISResult<HolonWritePreparation> PreparationAdmissionFailure(string code, string message, Exception cause = null)
        => new OASISResult<HolonWritePreparation> { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}

public sealed class MongoPreparationReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public Guid OperationId { get; }
    public string SessionId { get; }
    public long? TransactionNumber { get; }
    public string OriginalTransactionId => SessionId != null && TransactionNumber.HasValue ? SessionId + ":" + TransactionNumber.Value : string.Empty;
    public MongoPreparationReconciliationException(Guid operationId, string sessionId, long? transactionNumber, Exception cause)
        : base("Recover the original MongoDB preparation before dispatching a holon mutation.", cause)
    { OperationId = operationId; SessionId = sessionId; TransactionNumber = transactionNumber; }
}

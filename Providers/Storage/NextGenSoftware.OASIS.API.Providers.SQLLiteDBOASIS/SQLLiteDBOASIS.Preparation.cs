using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : IHolonWritePreparationRecoveryStorageProvider
{
    public Task<OASISResult<HolonWritePreparation>> ReadHolonWritePreparationAsync(Guid operationId, Guid actorId,
        CancellationToken cancellationToken = default)
    {
        OASISResult<HolonWritePreparation> Invalid(string code, string message) => new()
            { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
        if (cancellationToken.IsCancellationRequested) return Task.FromResult(Invalid("COSMIC_PREPARATION_CANCELLED", "Preparation read was cancelled."));
        if (operationId == Guid.Empty || actorId == Guid.Empty) return Task.FromResult(Invalid("COSMIC_PREPARATION_INVALID", "Operation and authenticated actor are required."));
        if (!IsProviderActivated || _holonRepository == null) return Task.FromResult(Invalid("COSMIC_PREPARATION_PROVIDER_NOT_READY", "Activate SQLite before reading preparation."));
        return _holonRepository.ReadHolonWritePreparationAsync(operationId, actorId, cancellationToken);
    }
    public OASISResult<HolonWritePreparation> ClaimHolonWritePreparation(HolonWritePreparation candidate)
        => _holonRepository.ClaimHolonWritePreparation(candidate, CancellationToken.None);
    public Task<OASISResult<HolonWritePreparation>> ClaimHolonWritePreparationAsync(HolonWritePreparation candidate,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_holonRepository.ClaimHolonWritePreparation(candidate, cancellationToken));
    }
}

public sealed class SqlitePreparationReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public Guid OperationId { get; }
    public string OriginalTransactionId => string.Empty;
    public SqlitePreparationReconciliationException(Guid operationId, Exception cause)
        : base("Recover the original durable SQLite preparation before dispatching a holon mutation.", cause)
    { OperationId = operationId; }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : IHolonWritePreparationStorageProvider
{
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

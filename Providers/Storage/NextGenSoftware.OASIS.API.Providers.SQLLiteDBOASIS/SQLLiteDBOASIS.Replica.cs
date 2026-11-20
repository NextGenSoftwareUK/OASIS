using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public sealed class SqliteReplicaReconciliationException(Guid operationId,string sourceId,Exception cause)
    :Exception("Reconcile the original SQLite replica receipt before another mutation.",cause),INativeMutationReconciliationRequired
{
    public Guid OperationId {get;}=operationId;
    public string SourceId {get;}=sourceId;
    public string OriginalTransactionId=>string.Empty;
}

public partial class SQLLiteDBOASIS : ICommittedHolonReplicaStorageProvider, ICommittedHolonReplicaInitializationStorageProvider, IBoundCommittedHolonReplicaStorageProvider, IHolonReplicaHeadAuditStorageProvider
{
    public Task<OASISResult<CommittedHolonReplicaReceiptEvidence>> ReadCommittedHolonReplicaReceiptAsync(
        CommittedHolonReplicaReceiptReadRequest request, CancellationToken cancellationToken = default)
        => WithHolonRepositoryAsync(repository => repository.ReadCommittedHolonReplicaReceiptAsync(request, cancellationToken));

    public Task<OASISResult<HolonReplicaHeadReadResult>> ReadHolonReplicaHeadsAsync(
        HolonReplicaHeadReadRequest request, CancellationToken cancellationToken = default)
        => WithHolonRepositoryAsync(repository => repository.ReadHolonReplicaHeadsAsync(request, cancellationToken));

    public OASISResult<IEnumerable<IHolon>> ApplyBoundCommittedHolonReplica(CommittedHolonReplicaRequest request,
        CosmicStorageIdentity expectedStorage)
        => WithHolonRepository(repository => repository.ApplyBoundCommittedHolonReplica(request, expectedStorage, CancellationToken.None));

    public Task<OASISResult<IEnumerable<IHolon>>> ApplyBoundCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,
        CosmicStorageIdentity expectedStorage, CancellationToken cancellationToken = default)
        => Task.FromResult(WithHolonRepository(repository => repository.ApplyBoundCommittedHolonReplica(request, expectedStorage, cancellationToken)));

    public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
        =>WithHolonRepository(repository => repository.ApplyCommittedHolonReplica(request,CancellationToken.None));
    public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,
        CancellationToken cancellationToken=default)
        =>Task.FromResult(WithHolonRepository(repository => repository.ApplyCommittedHolonReplica(request,cancellationToken)));

    public OASISResult<IEnumerable<IHolon>> InitializeCommittedHolonReplica(CommittedHolonReplicaRequest request)
        =>WithHolonRepository(repository => repository.InitializeCommittedHolonReplica(request,CancellationToken.None));
    public Task<OASISResult<IEnumerable<IHolon>>> InitializeCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,
        CancellationToken cancellationToken=default)
        =>Task.FromResult(WithHolonRepository(repository => repository.InitializeCommittedHolonReplica(request,cancellationToken)));
}

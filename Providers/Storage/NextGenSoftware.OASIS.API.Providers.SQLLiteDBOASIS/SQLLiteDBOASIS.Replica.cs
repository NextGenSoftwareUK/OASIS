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

public partial class SQLLiteDBOASIS : ICommittedHolonReplicaStorageProvider, ICommittedHolonReplicaInitializationStorageProvider
{
    public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
        =>_holonRepository.ApplyCommittedHolonReplica(request,CancellationToken.None);
    public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,
        CancellationToken cancellationToken=default)
        =>Task.FromResult(_holonRepository.ApplyCommittedHolonReplica(request,cancellationToken));

    public OASISResult<IEnumerable<IHolon>> InitializeCommittedHolonReplica(CommittedHolonReplicaRequest request)
        =>_holonRepository.InitializeCommittedHolonReplica(request,CancellationToken.None);
    public Task<OASISResult<IEnumerable<IHolon>>> InitializeCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,
        CancellationToken cancellationToken=default)
        =>Task.FromResult(_holonRepository.InitializeCommittedHolonReplica(request,cancellationToken));
}

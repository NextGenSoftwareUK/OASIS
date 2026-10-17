using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS
{
    /// <summary>Stable transaction boundaries used by deterministic crash-safety verification.</summary>
    public enum EdgeSQLiteTransactionBoundary
    {
        LocalEntityWrittenBeforeOutbox = 0,
        LocalOutboxWrittenBeforeCommit = 1,
        ReplicatedEntityWrittenBeforeInbox = 2,
        ReplicationInboxWrittenBeforeCommit = 3,
        ExchangeOperationResultsWritten = 4,
        ExchangeRemoteChangesWritten = 5,
        ExchangeCheckpointWrittenBeforeCommit = 6
    }

    /// <summary>
    /// Test/diagnostic hook invoked inside the active SQLite transaction. Production composition omits it.
    /// Throwing represents process failure at that boundary and must roll back the whole transaction.
    /// </summary>
    public interface IEdgeSQLiteTransactionFaultInjector
    {
        Task OnBoundaryAsync(EdgeSQLiteTransactionBoundary boundary, CancellationToken cancellationToken);
    }
}

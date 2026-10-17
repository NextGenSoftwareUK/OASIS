using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public enum HostedMongoSyncTransactionBoundary
    {
        DomainMutationWritten = 0,
        EntityWritten = 1,
        ChangeFeedWritten = 2,
        FanOutWritten = 3,
        OperationResultWritten = 4,
        DeviceSequenceWritten = 5,
        BatchReadyToCommit = 6
    }

    /// <summary>Diagnostic hook invoked while the hosted Mongo transaction is active.</summary>
    public interface IHostedMongoSyncFaultInjector
    {
        Task OnBoundaryAsync(HostedMongoSyncTransactionBoundary boundary,
            CancellationToken cancellationToken);
    }
}

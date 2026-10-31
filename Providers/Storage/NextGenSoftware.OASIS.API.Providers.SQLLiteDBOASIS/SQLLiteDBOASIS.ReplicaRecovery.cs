using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS : ICommittedHolonReplicaInitializationRecoveryStorageProvider
{
    public Task<OASISResult<HolonReplicaInitializationOutcome>> ReadReplicaInitializationOutcomeAsync(CommittedHolonReplicaRequest original,CancellationToken cancellationToken=default)
        =>_holonRepository.ReadReplicaInitializationOutcomeAsync(original,cancellationToken);
}

using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : ICommittedHolonReplicaInitializationRecoveryStorageProvider
{
    public Task<OASISResult<HolonReplicaInitializationOutcome>> ReadReplicaInitializationOutcomeAsync(CommittedHolonReplicaRequest original,CancellationToken cancellationToken=default)
        =>IsProviderActivated && _holonRepository is HolonRepository repository
            ?repository.ReadReplicaInitializationOutcomeAsync(original,cancellationToken)
            :Task.FromResult(new OASISResult<HolonReplicaInitializationOutcome>{IsError=true,ErrorCount=1,ErrorCode="COSMIC_INITIALIZATION_STATUS_UNVERIFIED",Message="Activate the bound provider before receipt inspection."});
}

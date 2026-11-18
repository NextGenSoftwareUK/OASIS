using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : IHolonPurgeProvider
{
    public override Task<HolonPurgeSubject> GetPurgeSubjectAsync(Guid holonId, CancellationToken cancellationToken)
        => _holonRepository == null ? throw new InvalidOperationException("The Mongo provider is not activated.") : _holonRepository.GetPurgeSubjectAsync(holonId, cancellationToken);

    public override Task<HolonPurgeStoreOutcome> PurgeHolonAsync(Guid holonId, CancellationToken cancellationToken)
        => _holonRepository == null ? throw new InvalidOperationException("The Mongo provider is not activated.") : _holonRepository.PurgeHolonAsync(holonId, cancellationToken);
}

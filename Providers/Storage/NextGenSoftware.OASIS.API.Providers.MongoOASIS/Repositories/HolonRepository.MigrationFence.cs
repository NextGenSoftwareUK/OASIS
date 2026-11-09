using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal Task<OASISResult<HolonMigrationFenceObservation>> ExecuteMigrationFenceAsync(HolonMigrationFenceRequest request,
        MongoHolonMigrationFence.Action action, Guid owner, CancellationToken token, string mutationDigest = null)
        => MongoHolonMigrationFence.ExecuteAsync(_dbContext.MongoDB, request, action, owner, token, mutationDigest);
}

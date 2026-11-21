using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Purge
{
    /// <summary>Production backend: the HolonManager and the storage providers. Compiled, not unit tested (it needs real providers).</summary>
    public sealed class HolonManagerPurgeBackend : IHolonPurgeBackend
    {
        private readonly Func<HolonManager> _manager;
        public HolonManagerPurgeBackend(Func<HolonManager> manager) { _manager = manager; }

        public Task<HolonPurgeSubject> GetSubjectAsync(Guid holonId, CancellationToken c) => _manager().GetHolonPurgeSubjectAsync(holonId, c);

        public async Task<bool> CanDeleteAsync(HolonPurgeSubject subject, Guid avatarId, CancellationToken c)
        {
            if (subject?.Holon == null || avatarId == Guid.Empty) return false;
            if (subject.CreatedByAvatarId == avatarId) return true;
            return await _manager().AllowsAsync(subject.Holon, avatarId, HolonPermission.Delete).ConfigureAwait(false);
        }

        public Task<IReadOnlyList<HolonPurgeStoreOutcome>> PurgeAsync(Guid holonId, CancellationToken c) => _manager().PurgeHolonStoresAsync(holonId, c);
    }
}

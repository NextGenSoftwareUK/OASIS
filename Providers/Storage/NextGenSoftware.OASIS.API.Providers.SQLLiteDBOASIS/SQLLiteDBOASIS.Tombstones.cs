using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public partial class SQLLiteDBOASIS : IAvatarTombstoneStorageProvider
    {
        public OASISResult<bool> ApplyAvatarTombstone(AvatarTombstone tombstone) => _avatarRepository.ApplyAvatarTombstone(tombstone);
        public Task<OASISResult<bool>> ApplyAvatarTombstoneAsync(AvatarTombstone tombstone) => _avatarRepository.ApplyAvatarTombstoneAsync(tombstone);
    }
}

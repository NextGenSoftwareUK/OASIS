using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.Providers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS:IAvatarProfileStorageProvider
{
    public Task<OASISResult<AvatarProfileSnapshot>> ApplyAvatarProfileAsync(Guid authenticatedAvatarId,
        AvatarProfileMutation mutation,CancellationToken cancellationToken=default)
    {
        var repository=_avatarRepository;
        if(!IsProviderActivated || repository==null)
            return Task.FromResult(new OASISResult<AvatarProfileSnapshot>{IsError=true,ErrorCount=1,
                ErrorCode=AvatarProfileErrors.StorageUnavailable,Message="The profile storage provider is not active."});
        return repository.ApplyAvatarProfileAsync(authenticatedAvatarId,mutation,cancellationToken);
    }
}

using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS : IAvatarTombstoneStorageProvider
    {
        public OASISResult<bool> ApplyAvatarTombstone(AvatarTombstone tombstone)
            => ApplyAvatarTombstoneAsync(tombstone).GetAwaiter().GetResult();

        public async Task<OASISResult<bool>> ApplyAvatarTombstoneAsync(AvatarTombstone tombstone)
        {
            var result=new OASISResult<bool>();
            try
            {
                ArgumentNullException.ThrowIfNull(tombstone);
                if(!IsProviderActivated)
                {
                    var activation=await ActivateProviderAsync();
                    if(activation.IsError) throw new InvalidOperationException(activation.Message,activation.Exception);
                }
                var applied=await SoftDeleteStoredAvatarAsync(tombstone.AvatarId,tombstone);
                if(!applied.AlreadyApplied && !StorageReceiptSucceeded(applied.Receipt))
                    throw new InvalidOperationException("Ethereum tombstone transaction reverted.");
                result.Result=true; result.IsDeleted=true; result.DeletedCount=1; result.IsSaved=!applied.AlreadyApplied;
                AvatarTombstoneEvidence.Attach(result,applied.Tombstone);
            }
            catch(Exception cause)
            {
                OASISErrorHandling.HandleError(ref result,$"Ethereum committed tombstone application failed: {cause.Message}",cause);
            }
            return result;
        }
    }
}

using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

public partial class BitcoinOASIS
{
    private async Task<OASISResult<IAvatar>> ResolveWalletAvatarForDeleteAsync(string providerKey)
    {
        var referenced = await LoadAvatarByProviderKeyAsync(providerKey);
        if (referenced.IsError)
            return referenced;

        var current = await LoadAvatarAsync(referenced.Result.Id);
        if (!current.IsError &&
            !string.Equals(current.Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.BitcoinOASIS],
                providerKey, StringComparison.OrdinalIgnoreCase))
            OASISErrorHandling.HandleError(ref current, "Bitcoin avatar deletion requires the current wallet record key.");

        return current;
    }

    private async Task<OASISResult<bool>> DeleteWalletAvatarAsync(
        Func<Task<OASISResult<IAvatar>>> resolve, bool softDelete)
    {
        var result = new OASISResult<bool>();
        if (!softDelete)
        {
            OASISErrorHandling.HandleError(ref result, "Bitcoin transaction history supports avatar soft deletion only.");
            return result;
        }

        var current = await resolve();
        if (current.IsError)
        {
            result.ErrorCode = current.ErrorCode;
            OASISErrorHandling.HandleError(ref result, current.Message, current.Exception);
            return result;
        }

        if (current.Result.Version == int.MaxValue)
        {
            OASISErrorHandling.HandleError(ref result, "The Bitcoin avatar version has reached its limit.");
            return result;
        }

        var tombstone = new Avatar
        {
            Id = current.Result.Id,
            Version = current.Result.Version + 1,
            IsActive = false,
            DeletedDate = DateTime.UtcNow
        };
        var publication = await SaveAvatarAsync(tombstone);
        result.MetaData = publication.MetaData;
        result.ErrorCode = publication.ErrorCode;
        if (publication.IsError)
            OASISErrorHandling.HandleError(ref result, publication.Message, publication.Exception);
        else
            result.Result = true;

        return result;
    }
}

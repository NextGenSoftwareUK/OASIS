using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public partial class SQLLiteDBOASIS
{
    private async Task<OASISResult<IEnumerable<IAvatar>>> LoadWalletKeyAvatarsAsync(int version)
    {
        if(!IsProviderActivated)
        {
            var active=await ActivateProviderAsync();
            if(active.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,IEnumerable<IAvatar>>(active);
        }
        return await LoadAllAvatarsAsync(version);
    }

    public Task<OASISResult<IAvatar>> LoadAvatarByPublicKeyAsync(string publicKey,int version=0)
        => StoredAvatarWalletSelector.SelectAsync(publicKey,false,version,LoadWalletKeyAvatarsAsync);
    public OASISResult<IAvatar> LoadAvatarByPublicKey(string publicKey,int version=0)
        => LoadAvatarByPublicKeyAsync(publicKey,version).GetAwaiter().GetResult();
    public Task<OASISResult<IAvatar>> LoadAvatarByPrivateKeyAsync(string privateKey,int version=0)
        => StoredAvatarWalletSelector.SelectAsync(privateKey,true,version,LoadWalletKeyAvatarsAsync);
    public OASISResult<IAvatar> LoadAvatarByPrivateKey(string privateKey,int version=0)
        => LoadAvatarByPrivateKeyAsync(privateKey,version).GetAwaiter().GetResult();
}

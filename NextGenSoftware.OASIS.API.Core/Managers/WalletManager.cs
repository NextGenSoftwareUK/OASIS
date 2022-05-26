using System.Collections.Generic;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    //TODO: Add Async version of all methods and add IWalletManager Interface.
    public class WalletManager : OASISManager
    {
        public AvatarManager AvatarManager { get; set; }

        public List<IOASISStorageProvider> OASISStorageProviders { get; set; }

        public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        public WalletManager(IOASISStorageProvider OASISStorageProvider, AvatarManager avatarManager, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            AvatarManager = avatarManager;
        }

        public OASISResult<IProviderWallet> GetWalletThatPublicKeyBelongsTo(string providerKey, ProviderType providerType)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            OASISResult<IEnumerable<IAvatar>> avatarsResult = AvatarManager.LoadAllAvatars();

            if (!avatarsResult.IsError && avatarsResult.Result != null)
            {
                foreach (IAvatar avatar in avatarsResult.Result)
                {
                    result = GetWalletThatPublicKeyBelongsTo(providerKey, providerType, avatar);

                    if (result.Result != null)
                        break;
                }
            }
            else
                ErrorHandling.HandleError(ref result, $"Error occured in GetWalletThatPublicKeyBelongsTo whilst loading avatars. Reason:{avatarsResult.Message}");

            return result;
        }

        public OASISResult<IProviderWallet> GetWalletThatPublicKeyBelongsTo(string providerKey, ProviderType providerType, IAvatar avatar)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            foreach (IProviderWallet wallet in avatar.ProviderWallets[providerType])
            {
                if (wallet.PublicKey == providerKey)
                {
                    result.Result = wallet;
                    result.Message = "Wallet Found";
                    break;
                }
            }

            return result;
        }

        //TODO: Lots more coming soon! ;-)
    }
}
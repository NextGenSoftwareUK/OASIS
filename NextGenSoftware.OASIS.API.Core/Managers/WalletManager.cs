using System.Collections.Generic;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    //TODO: Add Async version of all methods and add IWalletManager Interface.
    public class WalletManager : OASISManager
    {
        private static WalletManager _instance = null;

        //public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        public static WalletManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new WalletManager(ProviderManager.CurrentStorageProvider, AvatarManager.Instance);

                return _instance;
            }
        }

        public AvatarManager AvatarManager { get; set; }

       // public List<IOASISStorageProvider> OASISStorageProviders { get; set; }

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

        public OASISResult<IProviderWallet> ImportWalletUsingSecretPhase(string phase)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            //TODO: Finish implementing... (allow user to import a wallet using the secret recovering phase (memonic words).
            //Can derive the public key and private key from the phase (need to look into how to do this...)

            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingJSONFile(string pathToJSONFile)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            //TODO: Finish implementing... (allow user to import a wallet using the JSON import file (standard wallet format).

            return result;
        }

        public OASISResult<Guid> ImportWalletUsingPrivateKeyById(Guid avatarId, string key, ProviderType providerToImportTo)
        {
            //OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            //TODO: Finish implementing... Can derive the public key from the private key  (need to look into how to do this and update Link methods with new logic...)


            return KeyManager.Instance.LinkProviderPrivateKeyToAvatarById(Guid.Empty, avatarId, providerToImportTo, key);
        }

        public OASISResult<Guid> ImportWalletUsingPrivateKeyByUsername(string username, string key, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPrivateKeyToAvatarByUsername(Guid.Empty, username, providerToImportTo, key);
        }

        public OASISResult<Guid> ImportWalletUsingPrivateKeyByEmail(string email, string key, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPrivateKeyToAvatarByUsername(Guid.Empty, email, providerToImportTo, key);
        }

        public OASISResult<Guid> ImportWalletUsingPublicKeyById(Guid avatarId, string key, ProviderType providerToImportTo)
        {
            //OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            //TODO: Finish implementing... The wallet will only be read-only without the private key.
            //This will be very similar to the LinkProviderPublicKeyToAvatarById/LinkProviderPublicKeyToAvatarByUsername/LinkProviderPublicKeyToAvatarByEmail methods in KeyManager.
            //Ideally this method will call into the Link methods above (probably best to just have this method call them direct, no additional logic needed.

            return KeyManager.Instance.LinkProviderPublicKeyToAvatarById(Guid.Empty, avatarId, providerToImportTo, key);
        }

        public OASISResult<Guid> ImportWalletUsingPublicKeyByUsername(string username, string key, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPublicKeyToAvatarByUsername(Guid.Empty, username, providerToImportTo, key);
        }

        public OASISResult<Guid> ImportWalletUsingPublicKeyByEmail(string email, string key, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPublicKeyToAvatarByEmail(Guid.Empty, email, providerToImportTo, key);
        }

        //TODO: Lots more coming soon! ;-)
    }
}
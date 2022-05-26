//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Cryptography.ECDSA;
//using BC = BCrypt.Net.BCrypt;
//using Rijndael256;
//using NextGenSoftware.OASIS.API.DNA;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Events;
//using NextGenSoftware.OASIS.API.Core.Helpers;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Objects;
//using NextGenSoftware.OASIS.API.Core.Security;
//using NextGenSoftware.OASIS.API.Core.Utilities;

//namespace NextGenSoftware.OASIS.API.Core.Managers
//{
//    //TODO: Add Async version of all methods and add IKeyManager Interface.
//    public class KeyManager : OASISManager
//    {
//        private static Dictionary<string, string> _avatarIdToProviderUniqueStorageKeyLookup = new Dictionary<string, string>();
//        private static Dictionary<string, List<string>> _avatarIdToProviderPublicKeysLookup = new Dictionary<string, List<string>>();
//        private static Dictionary<string, string> _avatarIdToProviderPrivateKeyLookup = new Dictionary<string, string>();
//        private static Dictionary<string, string> _avatarUsernameToProviderUniqueStorageKeyLookup = new Dictionary<string, string>();
//        private static Dictionary<string, List<string>> _avatarUsernameToProviderPublicKeysLookup = new Dictionary<string, List<string>>();
//        private static Dictionary<string, string> _avatarUsernameToProviderPrivateKeyLookup = new Dictionary<string, string>();
//        private static Dictionary<string, string> _avatarEmailToProviderUniqueStorageKeyLookup = new Dictionary<string, string>();
//        private static Dictionary<string, List<string>> _avatarEmailToProviderPublicKeysLookup = new Dictionary<string, List<string>>();
//        private static Dictionary<string, string> _avatarEmailToProviderPrivateKeyLookup = new Dictionary<string, string>();
//        private static Dictionary<string, Guid> _providerUniqueStorageKeyToAvatarIdLookup = new Dictionary<string, Guid>();
//        private static Dictionary<string, Guid> _providerPublicKeyToAvatarIdLookup = new Dictionary<string, Guid>();
//        private static Dictionary<string, Guid> _providerPrivateKeyToAvatarIdLookup = new Dictionary<string, Guid>();
//        private static Dictionary<string, string> _providerUniqueStorageKeyToAvatarUsernameLookup = new Dictionary<string, string>();
//        private static Dictionary<string, string> _providerPublicKeyToAvatarUsernameLookup = new Dictionary<string, string>();
//        private static Dictionary<string, string> _providerPrivateKeyToAvatarUsernameLookup = new Dictionary<string, string>();
//        private static Dictionary<string, string> _providerUniqueStorageKeyToAvatarEmailLookup = new Dictionary<string, string>();
//        private static Dictionary<string, string> _providerPublicKeyToAvatarEmailLookup = new Dictionary<string, string>();
//        private static Dictionary<string, string> _providerPrivateKeyToAvatarEmailLookup = new Dictionary<string, string>();
//        private static Dictionary<string, IAvatar> _providerUniqueStorageKeyToAvatarLookup = new Dictionary<string, IAvatar>();
//        private static Dictionary<string, IAvatar> _providerPublicKeyToAvatarLookup = new Dictionary<string, IAvatar>();
//        private static Dictionary<string, IAvatar> _providerPrivateKeyToAvatarLookup = new Dictionary<string, IAvatar>();

//        public WifUtility WifUtility { get; set; } = new WifUtility();
//        public AvatarManager AvatarManager { get; set; }

//        public List<IOASISStorageProvider> OASISStorageProviders { get; set; }

//        public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

//        public KeyManager(IOASISStorageProvider OASISStorageProvider, AvatarManager avatarManager, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
//        {
//            AvatarManager = avatarManager;
//        }

//        //public static void Init(IOASISStorageProvider OASISStorageProvider, AvatarManager avatarManager, OASISDNA OASISDNA = null) 
//        //{
//        //    AvatarManager = avatarManager;
//        //}

//        //TODO: Implement later (Cache Disabled).
//        //public bool IsCacheEnabled { get; set; } = true;

//        public OASISResult<KeyPair> GenerateKeyPair(ProviderType provider)
//        {
//            return GenerateKeyPair(Enum.GetName(typeof(ProviderType), provider));
//        }

//        public OASISResult<KeyPair> GenerateKeyPair(string prefix)
//        {
//            OASISResult<KeyPair> result = new OASISResult<KeyPair>(new KeyPair());
//            byte[] privateKey = Secp256K1Manager.GenerateRandomKey();

//            OASISResult<string> privateWifResult = GetPrivateWif(privateKey);

//            if (!privateWifResult.IsError && privateWifResult.Result != null)
//            {
//                result.Result.PrivateKey = privateWifResult.Result;

//                byte[] publicKey = Secp256K1Manager.GetPublicKey(privateKey, true);

//                OASISResult<string> publicWifResult = GetPublicWif(publicKey, prefix);

//                if (!publicWifResult.IsError && publicWifResult.Result != null)
//                    result.Result.PublicKey = publicWifResult.Result;
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GenerateKeyPair generating public WIF. Reason: {publicWifResult.Message}");
//            }
//            else
//                ErrorHandling.HandleError(ref result, $"Error occured in GenerateKeyPair generating private WIF. Reason: {privateWifResult.Message}");

//            return result;
//        }

//        public OASISResult<bool> ClearCache()
//        {
//            _avatarIdToProviderUniqueStorageKeyLookup.Clear();
//            _avatarIdToProviderPublicKeysLookup.Clear();
//            _avatarIdToProviderPrivateKeyLookup.Clear();
//            _avatarUsernameToProviderUniqueStorageKeyLookup.Clear();
//            _avatarUsernameToProviderPublicKeysLookup.Clear();
//            _avatarUsernameToProviderPrivateKeyLookup.Clear();
//            _avatarEmailToProviderUniqueStorageKeyLookup.Clear();
//            _avatarEmailToProviderPublicKeysLookup.Clear();
//            _avatarEmailToProviderPrivateKeyLookup.Clear();
//            _providerUniqueStorageKeyToAvatarIdLookup.Clear();
//            _providerPublicKeyToAvatarIdLookup.Clear();
//            _providerPrivateKeyToAvatarIdLookup.Clear();
//            _providerUniqueStorageKeyToAvatarUsernameLookup.Clear();
//            _providerPublicKeyToAvatarUsernameLookup.Clear();
//            _providerPrivateKeyToAvatarUsernameLookup.Clear();
//            _providerUniqueStorageKeyToAvatarEmailLookup.Clear();
//            _providerPublicKeyToAvatarEmailLookup.Clear();
//            _providerPrivateKeyToAvatarEmailLookup.Clear();
//            _providerUniqueStorageKeyToAvatarLookup.Clear();
//            _providerPublicKeyToAvatarLookup.Clear();
//            _providerPrivateKeyToAvatarLookup.Clear();

//            return new OASISResult<bool>(true) { Message = "Cache Cleared."};
//        }

//        //TODO: Finish Later.
//        //public OASISResult<bool> ClearCacheForAvatarById(Guid id)
//        //{
//        //    _avatarIdToProviderUniqueStorageKeyLookup[id.ToString()] = null;
//        //    _avatarIdToProviderPublicKeysLookup[id.ToString()] = null;
//        //    _avatarIdToProviderPrivateKeyLookup[id.ToString()] = null;
//        //    _avatarUsernameToProviderUniqueStorageKeyLookup.Clear();
//        //    _avatarUsernameToProviderPublicKeysLookup.Clear();
//        //    _avatarUsernameToProviderPrivateKeyLookup.Clear();
//        //    _avatarEmailToProviderUniqueStorageKeyLookup.Clear();
//        //    _avatarEmailToProviderPublicKeysLookup.Clear();
//        //    _avatarEmailToProviderPrivateKeyLookup.Clear();
//        //    _providerUniqueStorageKeyToAvatarIdLookup.Clear();
//        //    _providerPublicKeyToAvatarIdLookup.Clear();
//        //    _providerPrivateKeyToAvatarIdLookup.Clear();
//        //    _providerUniqueStorageKeyToAvatarUsernameLookup.Clear();
//        //    _providerPublicKeyToAvatarUsernameLookup.Clear();
//        //    _providerPrivateKeyToAvatarUsernameLookup.Clear();
//        //    _providerUniqueStorageKeyToAvatarEmailLookup.Clear();
//        //    _providerPublicKeyToAvatarEmailLookup.Clear();
//        //    _providerPrivateKeyToAvatarEmailLookup.Clear();
//        //    _providerUniqueStorageKeyToAvatarLookup.Clear();
//        //    _providerPublicKeyToAvatarLookup.Clear();
//        //    _providerPrivateKeyToAvatarLookup.Clear();

//        //    return new OASISResult<bool>(true);
//        //}

//        // Could be used as the public key for private/public key pairs. Could also be a username/accountname/unique id/etc, etc.
//        public OASISResult<bool> LinkProviderPublicKeyToAvatarById(Guid avatarId, ProviderType providerTypeToLinkTo, string providerKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();

//            try
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, providerToLoadAvatarFrom);

//                //TODO Apply same fix in ALL other methods.
//                if (!avatarResult.IsError && avatarResult.Result != null)
//                    result = LinkProviderPublicKeyToAvatar(avatarResult.Result, providerTypeToLinkTo, providerKey, providerToLoadAvatarFrom);
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPublicKeyToAvatarById loading avatar for id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPublicKeyToAvatarById for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerKey}: {ex.Message}");
//            }

//            return result;
//        }

//        // Could be used as the public key for private/public key pairs. Could also be a username/accountname/unique id/etc, etc.
//        public OASISResult<bool> LinkProviderPublicKeyToAvatarByUsername(string username, ProviderType providerTypeToLinkTo, string providerKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();

//            try
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, false, providerToLoadAvatarFrom);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                    result = LinkProviderPublicKeyToAvatar(avatarResult.Result, providerTypeToLinkTo, providerKey, providerToLoadAvatarFrom);
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPublicKeyToAvatarByUsername loading avatar for username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPublicKeyToAvatarByUsername for avatar {username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerKey}: {ex.Message}");
//            }

//            return result;
//        }

//        public OASISResult<bool> LinkProviderPublicKeyToAvatarByEmail(string email, ProviderType providerTypeToLinkTo, string providerKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();

//            try
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, providerToLoadAvatarFrom);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                    result = LinkProviderPublicKeyToAvatar(avatarResult.Result, providerTypeToLinkTo, providerKey, providerToLoadAvatarFrom);
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPublicKeyToAvatarByEmail loading avatar for email {email}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPublicKeyToAvatarByEmail for avatar {email} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerKey}: {ex.Message}");
//            }

//            return result;
//        }

//        public OASISResult<IProviderWallet> GetWalletThatPublicKeyBelongsTo(string providerKey, ProviderType providerType)
//        {
//            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
//            OASISResult<IEnumerable<IAvatar>> avatarsResult = AvatarManager.LoadAllAvatars();

//            if (!avatarsResult.IsError && avatarsResult.Result != null)
//            {
//                foreach (IAvatar avatar in avatarsResult.Result)
//                {
//                    result = GetWalletThatPublicKeyBelongsTo(providerKey, providerType, avatar);

//                    if (result.Result != null)
//                        break;
//                }
//            }
//            else
//                ErrorHandling.HandleError(ref result, $"Error occured in GetWalletThatPublicKeyBelongsTo whilst loading avatars. Reason:{avatarsResult.Message}");

//            return result;
//        }

//        public OASISResult<IProviderWallet> GetWalletThatPublicKeyBelongsTo(string providerKey, ProviderType providerType, IAvatar avatar)
//        {
//            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

//            foreach (IProviderWallet wallet in avatar.ProviderWallets[providerType])
//            {
//                if (wallet.PublicKey == providerKey)
//                {
//                    result.Result = wallet;
//                    result.Message = "Wallet Found";
//                    break;
//                }
//            }

//            return result;
//        }

//        public OASISResult<bool> LinkProviderPublicKeyToAvatar(IAvatar avatar, ProviderType providerTypeToLinkTo, string providerKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();

//            try
//            {
               

//                if (!avatar.ProviderPublicKey.ContainsKey(providerTypeToLinkTo))
//                    avatar.ProviderPublicKey.Add(providerTypeToLinkTo, new List<string>());

//                if (!avatar.ProviderPublicKey[providerTypeToLinkTo].Contains(providerKey))
//                    avatar.ProviderPublicKey[providerTypeToLinkTo].Add(providerKey);
//                else
//                {
//                    ErrorHandling.HandleError(ref result, $"The Public ProviderKey {providerKey} is already linked to the avatar {avatar.Id} {avatar.Username}. The ProviderKey must be unique per provider.");
//                    return result;
//                }

//                //TODO: Upgrade Avatar.Save() methods to return OASISResult ASAP.
//                result.Result = avatar.Save() != null;

//                //TODO Apply same fix in ALL other methods.
//                if (result.Result)
//                {
//                    result.IsSaved = true;
//                    result.Message = $"Public key {providerKey} successfully linked to avatar {avatar.Id} - {avatar.Username} for provider {Enum.GetName(typeof(ProviderType), providerTypeToLinkTo)}";
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPublicKeyToAvatar saving avatar {avatar.Id} - {avatar.Username} for providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerKey}");
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPublicKeyToAvatar for avatar {avatar.Id} {avatar.Username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerKey}: {ex.Message}");
//            }

//            return result;
//        }

//        public OASISResult<KeyPair> GenerateKeyPairAndLinkProviderKeysToAvatarById(Guid avatarId, ProviderType providerTypeToLinkTo, bool showPublicKey = true, bool showPrivateKey = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
//        {
//            OASISResult<KeyPair> result = new OASISResult<KeyPair>();

//            try
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, providerToLoadAvatarFrom);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                    result = GenerateKeyPairAndLinkProviderKeysToAvatar(avatarResult.Result, providerTypeToLinkTo, showPublicKey, showPrivateKey, providerToLoadAvatarFrom);
//                else
//                    ErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatarById loading avatar for id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"An unknown error occured in GenerateKeyPairAndLinkProviderKeysToAvatarById for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
//            }

//            return result;
//        }

//        // Could be used as the public key for private/public key pairs. Could also be a username/accountname/unique id/etc, etc.
//        public OASISResult<KeyPair> GenerateKeyPairAndLinkProviderKeysToAvatarByUsername(string username, ProviderType providerTypeToLinkTo, bool showPublicKey = true, bool showPrivateKey = false,  ProviderType providerToLoadAvatarFrom = ProviderType.Default)
//        {
//            OASISResult<KeyPair> result = new OASISResult<KeyPair>();

//            try
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, false, providerToLoadAvatarFrom);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                    result = GenerateKeyPairAndLinkProviderKeysToAvatar(avatarResult.Result, providerTypeToLinkTo, showPublicKey, showPrivateKey, providerToLoadAvatarFrom);
//                else
//                    ErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatarByUsername loading avatar for username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"An unknown error occured in GenerateKeyPairAndLinkProviderKeysToAvatarByUsername for username {username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
//            }

//            return result;
//        }

//        public OASISResult<KeyPair> GenerateKeyPairAndLinkProviderKeysToAvatarByEmail(string email, ProviderType providerTypeToLinkTo, bool showPublicKey = true, bool showPrivateKey = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
//        {
//            OASISResult<KeyPair> result = new OASISResult<KeyPair>();

//            try
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, providerToLoadAvatarFrom);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                    result = GenerateKeyPairAndLinkProviderKeysToAvatar(avatarResult.Result, providerTypeToLinkTo, showPublicKey, showPrivateKey, providerToLoadAvatarFrom);
//                else
//                    ErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatarByUsername loading avatar for email {email}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"An unknown error occured in GenerateKeyPairAndLinkProviderKeysToAvatarByUsername for email {email} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
//            }

//            return result;
//        }

//        public OASISResult<KeyPair> GenerateKeyPairAndLinkProviderKeysToAvatar(IAvatar avatar, ProviderType providerTypeToLinkTo, bool showPublicKey = true, bool showPrivateKey = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
//        {
//            OASISResult<KeyPair> result = new OASISResult<KeyPair>();

//            if (avatar == null)
//            {
//                ErrorHandling.HandleError(ref result, "An error occured in GenerateKeyPairAndLinkProviderKeysToAvatar. The avatar passed in is null.");
//                return result;
//            }

//            try
//            {
//                result = GenerateKeyPair(providerTypeToLinkTo);

//                if (!result.IsError && result.Result != null)
//                {
//                    OASISResult<bool> publicKeyResult = LinkProviderPublicKeyToAvatar(avatar, providerTypeToLinkTo, result.Result.PublicKey, providerToLoadAvatarFrom);

//                    if (!publicKeyResult.IsError && publicKeyResult.Result)
//                    {
//                        OASISResult<bool> privateKeyResult = LinkProviderPrivateKeyToAvatar(avatar, providerTypeToLinkTo, result.Result.PrivateKey, providerToLoadAvatarFrom);

//                        if (privateKeyResult.IsError || !privateKeyResult.Result)
//                            ErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatar whilst linking the generated private key to the avatar {avatar.Id} - {avatar.Username}. Reason: {privateKeyResult.Message}", privateKeyResult.DetailedMessage);
//                        else
//                        {
//                            result.Message = "KeyPair Generated & Linked To Avatar.";

//                            if (!showPublicKey)
//                                result.Result.PublicKey = null;

//                            if (!showPrivateKey)
//                                result.Result.PrivateKey = null;
//                        }
//                    }
//                    else
//                        ErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatar whilst linking the generated public key to the avatar {avatar.Id} - {avatar.Username}. Reason: {publicKeyResult.Message}", publicKeyResult.DetailedMessage);
//                }
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPublicKeyToAvatar for avatar {avatar.Id} {avatar.Username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
//            }

//            return result;
//        }

//        // Private key for a public/private keypair.
//        public OASISResult<bool> LinkProviderPrivateKeyToAvatarById(Guid avatarId, ProviderType providerTypeToLinkTo, string providerPrivateKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();

//            try
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, providerToLoadAvatarFrom);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                    result = LinkProviderPrivateKeyToAvatar(avatarResult.Result, providerTypeToLinkTo, providerPrivateKey, providerToLoadAvatarFrom);
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPrivateKeyToAvatar loading avatar for id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkPrivateProviderKeyToAvatar for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerPrivateKey}: {ex.Message}");
//            }

//            return result;
//        }

//        // Private key for a public/private keypair.
//        public OASISResult<bool> LinkProviderPrivateKeyToAvatarByUsername(string username, ProviderType providerTypeToLinkTo, string providerPrivateKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();

//            try
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, false, providerToLoadAvatarFrom);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                    result = LinkProviderPrivateKeyToAvatar(avatarResult.Result, providerTypeToLinkTo, providerPrivateKey, providerToLoadAvatarFrom);
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPrivateKeyToAvatar loading avatar for username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkPrivateProviderKeyToAvatar for avatar {username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerPrivateKey}: {ex.Message}");
//            }

//            return result;
//        }

//        // Private key for a public/private keypair.
//        public OASISResult<bool> LinkProviderPrivateKeyToAvatarByEmail(string email, ProviderType providerTypeToLinkTo, string providerPrivateKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();

//            try
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, providerToLoadAvatarFrom);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                    result = LinkProviderPrivateKeyToAvatar(avatarResult.Result, providerTypeToLinkTo, providerPrivateKey, providerToLoadAvatarFrom);
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPrivateKeyToAvatarByEmail loading avatar for email {email}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPrivateKeyToAvatarByEmail for avatar {email} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerPrivateKey}: {ex.Message}");
//            }

//            return result;
//        }

//        public OASISResult<bool> LinkProviderPrivateKeyToAvatar(IAvatar avatar, ProviderType providerTypeToLinkTo, string providerPrivateKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();

//            try
//            {
//                //TODO Apply same fix in ALL other methods.
//                //if (!avatar.ProviderPrivateKey.ContainsKey(providerTypeToLinkTo))
//                //    avatar.ProviderPrivateKey.Add(providerTypeToLinkTo, new List<string>());

//                if (!avatar.ProviderPrivateKey[providerTypeToLinkTo].Contains(providerPrivateKey))
//                    avatar.ProviderPrivateKey[providerTypeToLinkTo] = providerPrivateKey;
//                else
//                {
//                    ErrorHandling.HandleError(ref result, $"The Private ProviderKey {providerPrivateKey} is already linked to the avatar {avatar.Id} {avatar.Username}. The ProviderKey must be unique per provider.");
//                    return result;
//                }

//                //TODO: Upgrade Avatar.Save() methods to return OASISResult ASAP.
//                //TODO: Fix the StringCipher below or find the strongest encryption, maybe the Qunatum Encryption? :)
//                //avatar.ProviderPrivateKey[providerTypeToLinkTo] = StringCipher.Encrypt(providerPrivateKey);
//                //avatar.ProviderPrivateKey[providerTypeToLinkTo] = BC.HashPassword(providerPrivateKey);
//                avatar.ProviderPrivateKey[providerTypeToLinkTo] = Rijndael.Encrypt(providerPrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
//                result.Result = avatar.Save() != null;

//                //TODO Apply same fix in ALL other methods.
//                if (result.Result)
//                {
//                    result.IsSaved = true;
//                    result.Message = $"Private key successfully linked to avatar {avatar.Id} - {avatar.Username} for provider {Enum.GetName(typeof(ProviderType), providerTypeToLinkTo)}";
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPrivateKeyToAvatar saving avatar {avatar.Id} - {avatar.Username} for providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}");
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPrivateKeyToAvatar for avatar {avatar.Id} {avatar.Username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
//            }

//            return result;
//        }

//        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<string> result = new OASISResult<string>();
//            string key = string.Concat(Enum.GetName(providerType), avatarId);

//            if (!_avatarIdToProviderUniqueStorageKeyLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                    result = GetProviderUniqueStorageKeyForAvatar(avatarResult.Result, key, _avatarIdToProviderUniqueStorageKeyLookup, providerType);
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GetProviderUniqueStorageKeyForAvatarById loading the avatar with id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarByUsername(string avatarUsername, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<string> result = new OASISResult<string>();

//            string key = string.Concat(Enum.GetName(providerType), avatarUsername);

//            if (!_avatarUsernameToProviderUniqueStorageKeyLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarUsername, true, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                    result = GetProviderUniqueStorageKeyForAvatar(avatarResult.Result, key, _avatarUsernameToProviderUniqueStorageKeyLookup, providerType);
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GetProviderUniqueStorageKeyForAvatarByUsername loading avatar with username {avatarUsername}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarByEmail(string avatarEmail, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<string> result = new OASISResult<string>();

//            string key = string.Concat(Enum.GetName(providerType), avatarEmail);

//            if (!_avatarEmailToProviderUniqueStorageKeyLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(avatarEmail, true, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                    result = GetProviderUniqueStorageKeyForAvatar(avatarResult.Result, key, _avatarEmailToProviderUniqueStorageKeyLookup, providerType);
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GetProviderUniqueStorageKeyForAvatarByEmail loading avatar with avatarEmail {avatarEmail}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<List<string>> GetProviderPublicKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<List<string>> result = new OASISResult<List<string>>();
//            string key = string.Concat(Enum.GetName(providerType), avatarId);

//            if (!_avatarIdToProviderPublicKeysLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
//                    {
//                        _avatarIdToProviderPublicKeysLookup[key] = avatarResult.Result.ProviderPublicKey[providerType];
//                        result.Result = _avatarIdToProviderPublicKeysLookup[key];
//                    }
//                    else
//                        ErrorHandling.HandleError(ref result, string.Concat("Error occured in GetProviderPublicKeysForAvatarById. The avatar with id ", avatarId, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GetProviderPublicKeysForAvatarById loading avatar with id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<List<string>> GetProviderPublicKeysForAvatarByUsername(string avatarUsername, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<List<string>> result = new OASISResult<List<string>>();
//            string key = string.Concat(Enum.GetName(providerType), avatarUsername);

//            if (!_avatarUsernameToProviderPublicKeysLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarUsername, true, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
//                    {
//                        _avatarUsernameToProviderPublicKeysLookup[key] = avatarResult.Result.ProviderPublicKey[providerType];
//                        result.Result = _avatarUsernameToProviderPublicKeysLookup[key];
//                    }
//                    else
//                        ErrorHandling.HandleError(ref result, string.Concat("The avatar with username ", avatarUsername, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GetProviderPublicKeysForAvatarByUsername loading avatar with username {avatarUsername}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<List<string>> GetProviderPublicKeysForAvatarByEmail(string avatarEmail, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<List<string>> result = new OASISResult<List<string>>();
//            string key = string.Concat(Enum.GetName(providerType), avatarEmail);

//            if (!_avatarEmailToProviderPublicKeysLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(avatarEmail, true, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
//                    {
//                        _avatarEmailToProviderPublicKeysLookup[key] = avatarResult.Result.ProviderPublicKey[providerType];
//                        result.Result = _avatarEmailToProviderPublicKeysLookup[key];
//                    }
//                    else
//                        ErrorHandling.HandleError(ref result, string.Concat("The avatar with email ", avatarEmail, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GetProviderPublicKeysForAvatarByEmail loading avatar with email {avatarEmail}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }
            
//            return result;
//        }

//        public OASISResult<string> GetProviderPrivateKeyForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<string> result = new OASISResult<string>();
//            string key = string.Concat(Enum.GetName(providerType), avatarId);

//            if (AvatarManager.LoggedInAvatar.Id != avatarId)
//            {
//                result.IsError = true;
//                result.Message = "You cannot retreive the private key for another person's avatar. Please login to this account and try again.";
//            }

//            if (!_avatarIdToProviderPrivateKeyLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
//                    {
//                        _avatarIdToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderPrivateKey[providerType];
//                        //result.Result = StringCipher.Decrypt(_avatarIdToProviderPrivateKeyLookup[key]);
//                        result.Result = Rijndael.Decrypt(_avatarIdToProviderPrivateKeyLookup[key], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
//                    }
//                    else
//                        ErrorHandling.HandleError(ref result, string.Concat("The avatar with id ", avatarId, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPrivateKeyToAvatar method on the AvatarManager or avatar REST API."));
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeyForAvatarById loading avatar with id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<string> GetProviderPrivateKeyForAvatarByUsername(string avatarUsername, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<string> result = new OASISResult<string>();
//            string key = string.Concat(Enum.GetName(providerType), avatarUsername);

//            if (AvatarManager.LoggedInAvatar.Username != avatarUsername)
//                ErrorHandling.HandleError(ref result, "Error occured in GetProviderPrivateKeyForAvatarByUsername. You cannot retreive the private key for another person's avatar. Please login to this account and try again.");

//            if (!_avatarUsernameToProviderPrivateKeyLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarUsername, false, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
//                    {
//                        _avatarUsernameToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderPrivateKey[providerType];
//                        //result.Result = StringCipher.Decrypt(_avatarUsernameToProviderPrivateKeyLookup[key]);
//                        result.Result = Rijndael.Decrypt(_avatarUsernameToProviderPrivateKeyLookup[key], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
//                    }
//                    else
//                        ErrorHandling.HandleError(ref result, string.Concat("Error occured in GetProviderPrivateKeyForAvatarByUsername. The avatar with username ", avatarUsername, " was not found."));
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeyForAvatarByUsername loading avatar with username {avatarUsername}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        //public OASISResult<string> GetProviderPrivateKeyForAvatarByEmail(string avatarEmail, ProviderType providerType = ProviderType.Default)
//        //{
//        //    OASISResult<string> result = new OASISResult<string>();
//        //    string key = string.Concat(Enum.GetName(providerType), avatarEmail);

//        //    if (AvatarManager.LoggedInAvatar.Email != avatarEmail)
//        //        ErrorHandling.HandleError(ref result, "Error occured in GetProviderPrivateKeyForAvatarByEmail. You cannot retreive the private key for another person's avatar. Please login to this account and try again.");

//        //    if (!_avatarEmailToProviderPrivateKeyLookup.ContainsKey(key))
//        //    {
//        //        OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(avatarEmail, false, providerType);

//        //        if (!avatarResult.IsError && avatarResult.Result != null)
//        //        {
//        //            if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
//        //                _avatarEmailToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderPrivateKey[providerType];
//        //            else
//        //                ErrorHandling.HandleError(ref result, string.Concat("Error occured in GetProviderPrivateKeyForAvatarByEmail. The avatar with email ", avatarEmail, " was not found."));
//        //        }
//        //        else
//        //            ErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeyForAvatarByEmail loading avatar with email {avatarEmail}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//        //    }

//        //    //result.Result = StringCipher.Decrypt(_avatarEmailToProviderPrivateKeyLookup[key]);
//        //    result.Result = Rijndael.Decrypt(_avatarEmailToProviderPrivateKeyLookup[key], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
//        //    return result;
//        //}

//        //public OASISResult<string> GetProviderPrivateKeyForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
//        //{
//        //    OASISResult<string> result = new OASISResult<string>();
//        //    string key = string.Concat(Enum.GetName(providerType), avatarUsername);

//        //    if (AvatarManager.LoggedInAvatar.Email != email)
//        //        ErrorHandling.HandleError(ref result, "Error occured in GetProviderPrivateKeyForAvatar. You cannot retreive the private key for another person's avatar. Please login to this account and try again.");

//        //    if (!_avatarUsernameToProviderPrivateKeyLookup.ContainsKey(key))
//        //    {
//        //        OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, providerType);

//        //        if (!avatarResult.IsError && avatarResult.Result != null)
//        //        {
//        //            if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
//        //                _avatarIdToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderPrivateKey[providerType];
//        //            else
//        //                ErrorHandling.HandleError(ref result, string.Concat("Error occured in GetProviderPrivateKeyForAvatar. The avatar with username ", avatarUsername, " was not found."));
//        //        }
//        //        else
//        //            ErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeyForAvatar loading avatar with username {avatarUsername}. Reason: {avatarResult.Message}");
//        //    }

//        //    result.Result = StringCipher.Decrypt(_avatarUsernameToProviderPrivateKeyLookup[key]);
//        //    return result;
//        //}

//        public OASISResult<Guid> GetAvatarIdForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
//        {
//            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
//            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.
//            OASISResult<Guid> result = new OASISResult<Guid>();
//            string key = string.Concat(Enum.GetName(providerType), providerKey);

//            if (!_providerUniqueStorageKeyToAvatarIdLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = GetAvatarForProviderUniqueStorageKey(providerKey, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    _providerUniqueStorageKeyToAvatarIdLookup[key] = avatarResult.Result.Id;
//                    result.Result = _providerUniqueStorageKeyToAvatarIdLookup[key];
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GetAvatarIdForProviderUniqueStorageKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<string> GetAvatarUsernameForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<string> result = new OASISResult<string>();

//            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
//            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

//            string key = string.Concat(Enum.GetName(providerType), providerKey);

//            if (!_providerUniqueStorageKeyToAvatarUsernameLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = GetAvatarForProviderUniqueStorageKey(providerKey, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    _providerUniqueStorageKeyToAvatarUsernameLookup[key] = avatarResult.Result.Username;
//                    result.Result = _providerUniqueStorageKeyToAvatarUsernameLookup[key];
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GetAvatarUsernameForProviderUniqueStorageKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<string> GetAvatarEmailForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<string> result = new OASISResult<string>();

//            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
//            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

//            string key = string.Concat(Enum.GetName(providerType), providerKey);

//            if (!_providerUniqueStorageKeyToAvatarEmailLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = GetAvatarForProviderUniqueStorageKey(providerKey, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    _providerUniqueStorageKeyToAvatarEmailLookup[key] = avatarResult.Result.Email;
//                    result.Result = _providerUniqueStorageKeyToAvatarEmailLookup[key];
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GetAvatarEmailForProviderUniqueStorageKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<IAvatar> GetAvatarForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
//            string key = string.Concat(Enum.GetName(providerType), providerKey);

//            if (!_providerUniqueStorageKeyToAvatarLookup.ContainsKey(key))
//            {
//                //TODO: Ideally need a new overload for LoadAvatar that takes the provider key.
//                //TODO: In the meantime should we cache the full list of Avatars? Could take up a LOT of memory so probably not good idea?
//                OASISResult<IEnumerable<IAvatar>> avatarsResult = AvatarManager.LoadAllAvatars(true, providerType);

//                if (!avatarsResult.IsError && avatarsResult.Result != null)
//                {
//                    IAvatar avatar = avatarsResult.Result.FirstOrDefault(x => x.ProviderUniqueStorageKey.ContainsKey(providerType) && x.ProviderUniqueStorageKey[providerType] == providerKey);

//                    if (avatar != null)
//                    {
//                        _providerUniqueStorageKeyToAvatarIdLookup[key] = avatar.Id;
//                        _providerUniqueStorageKeyToAvatarUsernameLookup[key] = avatar.Username;
//                        _providerUniqueStorageKeyToAvatarEmailLookup[key] = avatar.Email;
//                        _providerUniqueStorageKeyToAvatarLookup[key] = avatar;

//                        result.Result = _providerUniqueStorageKeyToAvatarLookup[key];
//                    }
//                    else
//                        ErrorHandling.HandleError(ref result, string.Concat("The provider Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderKeyToAvatar method on the AvatarManager or avatar REST API."));

//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"Error in GetAvatarForProviderUniqueStorageKey loading all avatars. Reason: {avatarsResult.Message}", avatarsResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<Guid> GetAvatarIdForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<Guid> result = new OASISResult<Guid>();

//            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
//            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

//            string key = string.Concat(Enum.GetName(providerType), providerKey);

//            if (!_providerPublicKeyToAvatarIdLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPublicKey(providerKey, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    _providerPublicKeyToAvatarIdLookup[key] = avatarResult.Result.Id;
//                    result.Result = _providerPublicKeyToAvatarIdLookup[key];
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GetAvatarIdForProviderPublicKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<string> GetAvatarUsernameForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<string> result = new OASISResult<string>();
//            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
//            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

//            string key = string.Concat(Enum.GetName(providerType), providerKey);

//            if (!_providerPublicKeyToAvatarUsernameLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPublicKey(providerKey, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    _providerPublicKeyToAvatarUsernameLookup[key] = avatarResult.Result.Username;
//                    result.Result = _providerPublicKeyToAvatarUsernameLookup[key];
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GetAvatarUsernameForProviderPublicKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<string> GetAvatarEmailForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<string> result = new OASISResult<string>();
//            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
//            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

//            string key = string.Concat(Enum.GetName(providerType), providerKey);

//            if (!_providerPublicKeyToAvatarEmailLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPublicKey(providerKey, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    _providerPublicKeyToAvatarEmailLookup[key] = avatarResult.Result.Email;
//                    result.Result = _providerPublicKeyToAvatarEmailLookup[key];
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"Error occured in GetAvatarEmailForProviderPublicKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<IAvatar> GetAvatarForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
//            string key = string.Concat(Enum.GetName(providerType), providerKey);

//            if (!_providerPublicKeyToAvatarLookup.ContainsKey(key))
//            {
//                //TODO: Ideally need a new overload for LoadAvatarDetail that takes the public provider key.
//                //TODO: In the meantime should we cache the full list of AvatarDetails? Could take up a LOT of memory so probably not good idea?
//                OASISResult<IEnumerable<IAvatar>> avatarsResult = AvatarManager.LoadAllAvatars(true, providerType);

//                if (!avatarsResult.IsError && avatarsResult.Result != null)
//                {
//                    IAvatar avatar = avatarsResult.Result.FirstOrDefault(x => x.ProviderPublicKey.ContainsKey(providerType) && x.ProviderPublicKey[providerType].Contains(providerKey));

//                    if (avatar != null)
//                    {
//                        _providerPublicKeyToAvatarIdLookup[key] = avatar.Id;
//                        _providerPublicKeyToAvatarUsernameLookup[key] = avatar.Username;
//                        _providerPublicKeyToAvatarEmailLookup[key] = avatar.Email;
//                        _providerPublicKeyToAvatarLookup[key] = avatar;

//                        result.Result = _providerPublicKeyToAvatarLookup[key];
//                    }
//                    else
//                        ErrorHandling.HandleError(ref result, string.Concat("The provider public Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
//                }
//                else
//                    ErrorHandling.HandleError(ref result, string.Concat("Error in GetAvatarForProviderPublicKey for the provider public Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType. There was an error loading all avatars. Reason: ", avatarsResult.Message));
//            }

//            return result;
//        }

//        public OASISResult<Guid> GetAvatarIdForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<Guid> result = new OASISResult<Guid>();

//            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
//            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

//            string key = string.Concat(Enum.GetName(providerType), providerKey);

//            if (!_providerPrivateKeyToAvatarIdLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    _providerPrivateKeyToAvatarIdLookup[key] = avatarResult.Result.Id;
//                    result.Result = _providerPrivateKeyToAvatarIdLookup[key];
//                }
//                else
//                    ErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarIdForProviderPrivateKey. The provider public Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
//            }

//            return result;
//        }

//        public OASISResult<string> GetAvatarUsernameForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<string> result = new OASISResult<string>();
//            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
//            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

//            string key = string.Concat(Enum.GetName(providerType), providerKey);

//            if (!_providerPrivateKeyToAvatarUsernameLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    _providerPrivateKeyToAvatarUsernameLookup[key] = avatarResult.Result.Username;
//                    result.Result = _providerPrivateKeyToAvatarUsernameLookup[key];
//                }
//                else
//                    ErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarUsernameForProviderPrivateKey for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
//            }

//            return result;
//        }

//        public OASISResult<string> GetAvatarEmailForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<string> result = new OASISResult<string>();
//            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
//            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

//            string key = string.Concat(Enum.GetName(providerType), providerKey);

//            if (!_providerPrivateKeyToAvatarEmailLookup.ContainsKey(key))
//            {
//                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    _providerPrivateKeyToAvatarEmailLookup[key] = avatarResult.Result.Email;
//                    result.Result = _providerPrivateKeyToAvatarEmailLookup[key];
//                }
//                else
//                    ErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarEmailForProviderPrivateKey for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
//            }

//            return result;
//        }

//        public OASISResult<IAvatar> GetAvatarForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

//            //TODO: Fix the StringCipher below or find the strongest encryption, maybe the Qunatum Encryption? :)
//            //string key = string.Concat(Enum.GetName(providerType), StringCipher.Encrypt(providerKey));
//            //string key = string.Concat(Enum.GetName(providerType), BC.HashPassword(providerKey));
//            string key = string.Concat(Enum.GetName(providerType), Rijndael.Encrypt(providerKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256));

//            if (!_providerPrivateKeyToAvatarLookup.ContainsKey(key))
//            {
//                //TODO: Ideally need a new overload for LoadAvatarDetail that takes the public provider key.
//                //TODO: In the meantime should we cache the full list of AvatarDetails? Could take up a LOT of memory so probably not good idea?
//                OASISResult<IEnumerable<IAvatar>> avatarsResult = AvatarManager.LoadAllAvatars(true, providerType);

//                if (!avatarsResult.IsError && avatarsResult.Result != null)
//                {
//                    IAvatar avatar = avatarsResult.Result.FirstOrDefault(x => x.ProviderPrivateKey.ContainsKey(providerType) && x.ProviderPrivateKey[providerType] == providerKey);

//                    if (avatar != null)
//                    {
//                        _providerPublicKeyToAvatarIdLookup[key] = avatar.Id;
//                        _providerPublicKeyToAvatarUsernameLookup[key] = avatar.Username;
//                        _providerPublicKeyToAvatarEmailLookup[key] = avatar.Email;
//                        _providerPublicKeyToAvatarLookup[key] = avatar;

//                        result.Result = _providerPrivateKeyToAvatarLookup[key];
//                    }
//                    else
//                        ErrorHandling.HandleError(ref result, string.Concat("The provider private Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPrivateKeyToAvatar method on the AvatarManager or avatar REST API."));
//                }
//                else
//                    ErrorHandling.HandleError(ref result, string.Concat("Error in GetAvatarForProviderPrivateKey for the provider private Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType. There was an error loading all avatars. Reason: ", avatarsResult.Message));
//            }
            
//            return result;
//        }

//        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();
//            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, providerType);

//            if (!avatarResult.IsError && avatarResult.Result != null)
//                result.Result = avatarResult.Result.ProviderUniqueStorageKey;
//            else
//                ErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderUniqueStorageKeysForAvatarById loading avatar with avatarId {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

//            return result;
//        }

//        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarByUsername(string username, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();
//            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, true, providerType);

//            if (!avatarResult.IsError && avatarResult.Result != null)
//                result.Result = avatarResult.Result.ProviderUniqueStorageKey;
//            else
//                ErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderUniqueStorageKeysForAvatarByUsername loading avatar with username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

//            return result;
//        }

//        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();
//            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, true, providerType);

//            if (!avatarResult.IsError && avatarResult.Result != null)
//                result.Result = avatarResult.Result.ProviderUniqueStorageKey;
//            else
//                ErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderUniqueStorageKeysForAvatarByEmail loading avatar with email {email}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

//            return result;
//        }

//        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
//            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, providerType);

//            if (!avatarResult.IsError && avatarResult.Result != null)
//                result.Result = avatarResult.Result.ProviderPublicKey;
//            else
//                ErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderPublicKeysForAvatarById loading avatar with avatarId {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

//            return result;
//        }

//        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarByUsername(string username, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
//            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, true, providerType);

//            if (!avatarResult.IsError && avatarResult.Result != null)
//                result.Result = avatarResult.Result.ProviderPublicKey;
//            else
//                ErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderPublicKeysForAvatarByUsername loading avatar with username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

//            return result;
//        }

//        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
//            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, true, providerType);

//            if (!avatarResult.IsError && avatarResult.Result != null)
//                result.Result = avatarResult.Result.ProviderPublicKey;
//            else
//                ErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderPublicKeysForAvatarByEmail loading avatar with email {email}. Reason: {avatarResult.Message}");

//            return result;
//        }

//        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderPrivateKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();

//            if (AvatarManager.LoggedInAvatar.Id != avatarId)
//                ErrorHandling.HandleError(ref result, "An error occured in GetAllProviderPrivateKeysForAvatarById. You can only retreive your own private keys, not another persons avatar.");
//            else
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    result.Result = avatarResult.Result.ProviderPrivateKey;

//                    // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
//                    foreach (ProviderType privateKeyProviderType in result.Result.Keys)
//                        result.Result[privateKeyProviderType] = Rijndael.Decrypt(result.Result[privateKeyProviderType], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
//                        //result.Result[privateKeyProviderType] = StringCipher.Decrypt(result.Result[privateKeyProviderType]);
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"An error occured in GetAllProviderPrivateKeysForAvatarById, the avatar with id {avatarId} could not be loaded. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderPrivateKeysForAvatarByUsername(string username, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();

//            if (AvatarManager.LoggedInAvatar.Username != username)
//                ErrorHandling.HandleError(ref result, "Error occured in GetAllProviderPrivateKeysForAvatarByUsername, you can only retreive your own private keys, not another persons avatar.");
//            else
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, false, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    result.Result = avatarResult.Result.ProviderPrivateKey;

//                    // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
//                    foreach (ProviderType privateKeyProviderType in result.Result.Keys)
//                        result.Result[privateKeyProviderType] = Rijndael.Decrypt(result.Result[privateKeyProviderType], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
//                        //result.Result[privateKeyProviderType] = StringCipher.Decrypt(result.Result[privateKeyProviderType]);
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"An error occured in GetAllProviderPrivateKeysForAvatarByUsername, the avatar with username {username} could not be loaded. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderPrivateKeysForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();

//            if (AvatarManager.LoggedInAvatar.Email != email)
//                ErrorHandling.HandleError(ref result, "Error occured in GetAllProviderPrivateKeysForAvatarByEmail, you can only retreive your own private keys, not another persons avatar.");
//            else
//            {
//                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, providerType);

//                if (!avatarResult.IsError && avatarResult.Result != null)
//                {
//                    result.Result = avatarResult.Result.ProviderPrivateKey;

//                    // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
//                    foreach (ProviderType privateKeyProviderType in result.Result.Keys)
//                        result.Result[privateKeyProviderType] = Rijndael.Decrypt(result.Result[privateKeyProviderType], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
//                        //result.Result[privateKeyProviderType] = StringCipher.Decrypt(result.Result[privateKeyProviderType]);
//                }
//                else
//                    ErrorHandling.HandleError(ref result, $"An error occured in GetAllProviderPrivateKeysForAvatarByEmail, the avatar with email {email} could not be loaded. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
//            }

//            return result;
//        }

//        public OASISResult<string> GetPrivateWif(byte[] source)
//        {
//            OASISResult<string> result = new OASISResult<string>();

//            try
//            {
//                result.Result = WifUtility.GetPrivateWif(source);
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"Error occured in GetPrivateWif. Reason: {ex}", ex);
//            }

//            return result;
//        }

//        public OASISResult<string> GetPublicWif(byte[] publicKey, string prefix)
//        {
//            OASISResult<string> result = new OASISResult<string>();

//            try
//            {
//                result.Result = WifUtility.GetPublicWif(publicKey, prefix);
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"Error occured in GetPublicWif. Reason: {ex}", ex);
//            }

//            return result; ;
//        }

//        public OASISResult<byte[]> DecodePrivateWif(string data)
//        {
//            OASISResult<byte[]> result = new OASISResult<byte[]>();

//            try
//            {
//                result.Result = WifUtility.DecodePrivateWif(data);
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"Error occured in DecodePrivateWif. Reason: {ex}", ex);
//            }

//            return result;
//        }

//        public OASISResult<byte[]> Base58CheckDecode(string data)
//        {
//            OASISResult<byte[]> result = new OASISResult<byte[]>();

//            try
//            {
//                result.Result = WifUtility.Base58CheckDecode(data);
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"Error occured in Base58CheckDecode. Reason: {ex}", ex);
//            }

//            return result;
//        }

//        public OASISResult<string> EncodeSignature(byte[] source)
//        {
//            OASISResult<string> result = new OASISResult<string>();

//            try
//            {
//                result.Result = WifUtility.EncodeSignature(source);
//            }
//            catch (Exception ex)
//            {
//                ErrorHandling.HandleError(ref result, $"Error occured in EncodeSignature. Reason: {ex}", ex);
//            }

//            return result;
//        }

//        private OASISResult<string> GetProviderUniqueStorageKeyForAvatar(IAvatar avatar, string key, Dictionary<string, string> dictionaryCache, ProviderType providerType = ProviderType.Default)
//        {
//            OASISResult<string> result = new OASISResult<string>();

//            if (avatar != null)
//            {
//                if (avatar.ProviderUniqueStorageKey.ContainsKey(providerType))
//                {
//                    dictionaryCache[key] = avatar.ProviderUniqueStorageKey[providerType];
//                    result.Result = dictionaryCache[key];
//                }
//                else
//                    ErrorHandling.HandleError(ref result, string.Concat("The avatar with id ", avatar.Id, " and username ", avatar.Username, " has not been linked to the ", Enum.GetName(providerType), " provider."));
//            }
//            else
//                ErrorHandling.HandleError(ref result, string.Concat("The avatar with id ", avatar.Id, " and username ", avatar.Username, " was not found."));

//            //result.Result = dictionaryCache[key];
//            return result;
//        }
//    }
//}
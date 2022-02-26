using System;
using System.Collections.Generic;
using System.Linq;
//using BC = BCrypt.Net.BCrypt;
//using Cryptography.ECDSA; //TODO: Use NuGet to add this when have internet connection next! :)
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class KeyManager : OASISManager
    {
        private static Dictionary<string, string> _avatarIdToProviderUniqueStorageKeyLookup = new Dictionary<string, string>();
        private static Dictionary<string, List<string>> _avatarIdToProviderPublicKeysLookup = new Dictionary<string, List<string>>();
        private static Dictionary<string, string> _avatarIdToProviderPrivateKeyLookup = new Dictionary<string, string>();
        private static Dictionary<string, string> _avatarUsernameToProviderUniqueStorageKeyLookup = new Dictionary<string, string>();
        private static Dictionary<string, List<string>> _avatarUsernameToProviderPublicKeysLookup = new Dictionary<string, List<string>>();
        private static Dictionary<string, string> _avatarUsernameToProviderPrivateKeyLookup = new Dictionary<string, string>();
        private static Dictionary<string, string> _providerUniqueStorageKeyToAvatarUsernameLookup = new Dictionary<string, string>();
        private static Dictionary<string, string> _providerPublicKeyToAvatarUsernameLookup = new Dictionary<string, string>();
        private static Dictionary<string, string> _providerPrivateKeyToAvatarUsernameLookup = new Dictionary<string, string>();
        private static Dictionary<string, Guid> _providerUniqueStorageKeyToAvatarIdLookup = new Dictionary<string, Guid>();
        private static Dictionary<string, Guid> _providerPublicKeyToAvatarIdLookup = new Dictionary<string, Guid>();
        private static Dictionary<string, Guid> _providerPrivateKeyToAvatarIdLookup = new Dictionary<string, Guid>();
        private static Dictionary<string, IAvatar> _providerUniqueStorageKeyToAvatarLookup = new Dictionary<string, IAvatar>();
        private static Dictionary<string, IAvatar> _providerPublicKeyToAvatarLookup = new Dictionary<string, IAvatar>();
        private static Dictionary<string, IAvatar> _providerPrivateKeyToAvatarLookup = new Dictionary<string, IAvatar>();

        private static AvatarManager AvatarManager { get; set; }
        public List<IOASISStorageProvider> OASISStorageProviders { get; set; }

        public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        public KeyManager(IOASISStorageProvider OASISStorageProvider, AvatarManager avatarManager, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {
            AvatarManager = avatarManager;            
        }

        //TODO: Implement later (Cache Disabled).
        //public bool IsCacheEnabled { get; set; } = true;

        public static KeyPair GenerateKeyPair()
        {
            KeyPair keyPair = new KeyPair();
            //TODO: Add back in once added in Cryptography.ECDSA.
            //byte[] privateKey = Secp256K1Manager.GenerateRandomKey();
            //keyPair.PrivateKey = WifUtility.GetPrivateWif(privateKey);
            //byte[] publicKey = Secp256K1Manager.GetPublicKey(privateKey, true);
            //keyPair.PublicKey = WifUtility.GetPublicWif(publicKey, "EOS");
            return keyPair;
        }

        public static void ClearCache()
        {
            _avatarIdToProviderUniqueStorageKeyLookup.Clear();
            _avatarIdToProviderPublicKeysLookup.Clear();
            _avatarIdToProviderPrivateKeyLookup.Clear();
            _avatarUsernameToProviderUniqueStorageKeyLookup.Clear();
            _avatarUsernameToProviderPublicKeysLookup.Clear();
            _avatarUsernameToProviderPrivateKeyLookup.Clear();
            _providerUniqueStorageKeyToAvatarUsernameLookup.Clear();
            _providerPublicKeyToAvatarUsernameLookup.Clear();
            _providerPrivateKeyToAvatarUsernameLookup.Clear();
            _providerUniqueStorageKeyToAvatarIdLookup.Clear();
            _providerPublicKeyToAvatarIdLookup.Clear();
            _providerPrivateKeyToAvatarIdLookup.Clear();
            _providerUniqueStorageKeyToAvatarLookup.Clear();
            _providerPublicKeyToAvatarLookup.Clear();
            _providerPrivateKeyToAvatarLookup.Clear();
        }

        // Could be used as the public key for private/public key pairs. Could also be a username/accountname/unique id/etc, etc.
        public static OASISResult<bool> LinkProviderPublicKeyToAvatar(Guid avatarId, ProviderType providerTypeToLinkTo, string providerKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    LinkProviderPublicKeyToAvatar(avatarResult.Result, providerTypeToLinkTo, providerKey, providerToLoadAvatarFrom);
                else
                {
                    result.IsError = true;
                    result.Message = $"Error occured in LinkProviderPublicKeyToAvatar loading avatar for id {avatarId}. Reason: {avatarResult.Message}";
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPublicKeyToAvatar for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerKey}: {ex.Message}");
            }

            return result;
        }

        // Could be used as the public key for private/public key pairs. Could also be a username/accountname/unique id/etc, etc.
        public static OASISResult<bool> LinkProviderPublicKeyToAvatar(string username, ProviderType providerTypeToLinkTo, string providerKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    LinkProviderPublicKeyToAvatar(avatarResult.Result, providerTypeToLinkTo, providerKey, providerToLoadAvatarFrom);
                else
                {
                    result.IsError = true;
                    result.Message = $"Error occured in LinkProviderPublicKeyToAvatar loading avatar for username {username}. Reason: {avatarResult.Message}";
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPublicKeyToAvatar for avatar {username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerKey}: {ex.Message}");
            }

            return result;
        }

        public static OASISResult<bool> LinkProviderPublicKeyToAvatar(IAvatar avatar, ProviderType providerTypeToLinkTo, string providerKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (!avatar.ProviderPublicKey[providerTypeToLinkTo].Contains(providerKey))
                    avatar.ProviderPublicKey[providerTypeToLinkTo].Add(providerKey);
                else
                {
                    result.IsError = true;
                    result.Message = $"The ProviderKey {providerKey} is already linked to the avatar {avatar.Id} {avatar.Username}. The ProviderKey must be unique per provider.";
                    return result;
                }

                //TODO: Upgrade Avatar.Save() methods to return OASISResult ASAP.
                result.Result = avatar.Save() != null;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPublicKeyToAvatar for avatar {avatar.Id} {avatar.Username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerKey}: {ex.Message}");
            }

            return result;
        }

        // Private key for a public/private keypair.
        public static OASISResult<bool> LinkProviderPrivateKeyToAvatar(Guid avatarId, ProviderType providerTypeToLinkTo, string providerPrivateKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    LinkProviderPrivateKeyToAvatar(avatarResult.Result, providerTypeToLinkTo, providerPrivateKey, providerToLoadAvatarFrom);    
                else
                {
                    result.IsError = true;
                    result.Message = $"Error occured in LinkProviderPrivateKeyToAvatar loading avatar for id {avatarId}. Reason: {avatarResult.Message}";
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkPrivateProviderKeyToAvatar for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerPrivateKey}: {ex.Message}");
            }

            return result;
        }

        // Private key for a public/private keypair.
        public static OASISResult<bool> LinkProviderPrivateKeyToAvatar(string username, ProviderType providerTypeToLinkTo, string providerPrivateKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    LinkProviderPrivateKeyToAvatar(avatarResult.Result, providerTypeToLinkTo, providerPrivateKey, providerToLoadAvatarFrom);
                else
                {
                    result.IsError = true;
                    result.Message = $"Error occured in LinkProviderPrivateKeyToAvatar loading avatar for username {username}. Reason: {avatarResult.Message}";
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkPrivateProviderKeyToAvatar for avatar {username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerPrivateKey}: {ex.Message}");
            }

            return result;
        }

        public static OASISResult<bool> LinkProviderPrivateKeyToAvatar(IAvatar avatar, ProviderType providerTypeToLinkTo, string providerPrivateKey, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                avatar.ProviderPrivateKey[providerTypeToLinkTo] = StringCipher.Encrypt(providerPrivateKey);
                result.Result = avatar.Save() != null;
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"Unknown error occured in LinkPrivateProviderKeyToAvatar for avatar {avatar.Id} {avatar.Username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerPrivateKey}: {ex.Message}");
            }

            return result;
        }

        public static OASISResult<string> GetProviderUniqueStorageKeyForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            string key = string.Concat(Enum.GetName(providerType), avatarId);

            if (!_avatarIdToProviderUniqueStorageKeyLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    GetProviderUniqueStorageKeyForAvatar(avatarResult.Result, key, _avatarIdToProviderUniqueStorageKeyLookup, providerType);
                else
                {
                    result.IsError = true;
                    result.Message = avatarResult.Message;
                }
            }

            result.Result = _avatarIdToProviderUniqueStorageKeyLookup[key];
            return result;
        }

        public static OASISResult<string> GetProviderUniqueStorageKeyForAvatar(string avatarUsername, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            string key = string.Concat(Enum.GetName(providerType), avatarUsername);

            if (!_avatarUsernameToProviderUniqueStorageKeyLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarUsername, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null) 
                    GetProviderUniqueStorageKeyForAvatar(avatarResult.Result, key, _avatarUsernameToProviderUniqueStorageKeyLookup, providerType);
                else
                {
                    result.IsError = true;
                    result.Message = avatarResult.Message;
                }
            }

            result.Result = _avatarUsernameToProviderUniqueStorageKeyLookup[key];
            return result;
        }

        public static OASISResult<List<string>> GetProviderPublicKeysForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            string key = string.Concat(Enum.GetName(providerType), avatarId);

            if (!_avatarIdToProviderPublicKeysLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
                        _avatarIdToProviderPublicKeysLookup[key] = avatarResult.Result.ProviderPublicKey[providerType];
                    else
                        ErrorHandling.HandleError(ref result, string.Concat("The avatar with id ", avatarId, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
                }
                else
                {
                    result.IsError = true;
                    result.Message = avatarResult.Message;
                }
            }

            result.Result = _avatarIdToProviderPublicKeysLookup[key];
            return result;
        }

        public static OASISResult<List<string>> GetProviderPublicKeysForAvatar(string avatarUsername, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            string key = string.Concat(Enum.GetName(providerType), avatarUsername);

            if (!_avatarUsernameToProviderPublicKeysLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarUsername, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
                        _avatarIdToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderPrivateKey[providerType];
                    else
                        ErrorHandling.HandleError(ref result, string.Concat("The avatar with username ", avatarUsername, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
                }
                else
                {
                    result.IsError = true;
                    result.Message = avatarResult.Message;
                }
            }

            result.Result = _avatarUsernameToProviderPublicKeysLookup[key];
            return result;
        }

        public static OASISResult<string> GetProviderPrivateKeyForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            string key = string.Concat(Enum.GetName(providerType), avatarId);

            if (AvatarManager.LoggedInAvatar.Id != avatarId)
            {
                result.IsError = true;
                result.Message = "You cannot retreive the private key for another person's avatar. Please login to this account and try again.";
            }

            if (!_avatarIdToProviderPrivateKeyLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
                        _avatarIdToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderPrivateKey[providerType];
                    else
                        ErrorHandling.HandleError(ref result, string.Concat("The avatar with id ", avatarId, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPrivateKeyToAvatar method on the AvatarManager or avatar REST API."));
                }
                else
                {
                    result.IsError = true;
                    result.Message = avatarResult.Message;
                }
            }

            result.Result = StringCipher.Decrypt(_avatarIdToProviderPrivateKeyLookup[key]);
            return result;
        }

        public static OASISResult<string> GetProviderPrivateKeyForAvatar(string avatarUsername, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            string key = string.Concat(Enum.GetName(providerType), avatarUsername);

            if (AvatarManager.LoggedInAvatar.Username != avatarUsername)
            {
                result.IsError = true;
                result.Message = "You cannot retreive the private key for another person's avatar. Please login to this account and try again.";
            }

            if (!_avatarUsernameToProviderPrivateKeyLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarUsername, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
                        _avatarIdToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderPrivateKey[providerType];
                    else
                        ErrorHandling.HandleError(ref result, string.Concat("The avatar with username ", avatarUsername, " was not found."));
                }
                else
                {
                    result.IsError = true;
                    result.Message = avatarResult.Message;
                }
            }

            result.Result = StringCipher.Decrypt(_avatarUsernameToProviderPrivateKeyLookup[key]);
            return result;
        }

        public static OASISResult<Guid> GetAvatarIdForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.
            OASISResult<Guid> result = new OASISResult<Guid>();
            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerUniqueStorageKeyToAvatarIdLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderUniqueStorageKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    _providerUniqueStorageKeyToAvatarIdLookup[key] = avatarResult.Result.Id;
                else
                {
                    result.IsError = true;
                    result.Message = avatarResult.Message;
                }
            }

            result.Result = _providerUniqueStorageKeyToAvatarIdLookup[key];
            return result;
        }

        public static OASISResult<string> GetAvatarUsernameForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerUniqueStorageKeyToAvatarUsernameLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderUniqueStorageKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    _providerUniqueStorageKeyToAvatarUsernameLookup[key] = avatarResult.Result.Username;
                else
                {
                    result.IsError = true;
                    result.Message = avatarResult.Message;
                }
            }

            result.Result = _providerUniqueStorageKeyToAvatarUsernameLookup[key];
            return result;
        }

        public static OASISResult<IAvatar> GetAvatarForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerUniqueStorageKeyToAvatarLookup.ContainsKey(key))
            {
                //TODO: Ideally need a new overload for LoadAvatar that takes the provider key.
                //TODO: In the meantime should we cache the full list of Avatars? Could take up a LOT of memory so probably not good idea?
                IAvatar avatar = AvatarManager.LoadAllAvatars(providerType).FirstOrDefault(x => x.ProviderUniqueStorageKey.ContainsKey(providerType) && x.ProviderUniqueStorageKey[providerType] == providerKey);

                if (avatar != null)
                {
                    _providerUniqueStorageKeyToAvatarIdLookup[key] = avatar.Id;
                    _providerUniqueStorageKeyToAvatarUsernameLookup[key] = avatar.Username;
                    _providerUniqueStorageKeyToAvatarLookup[key] = avatar;
                }
                else
                    throw new InvalidOperationException(string.Concat("The provider Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderKeyToAvatar method on the AvatarManager or avatar REST API."));
            }

            result.Result = _providerUniqueStorageKeyToAvatarLookup[key];
            return result;
        }

        public static OASISResult<Guid> GetAvatarIdForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Guid> result = new OASISResult<Guid>();

            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPublicKeyToAvatarIdLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPublicKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    _providerPublicKeyToAvatarIdLookup[key] = avatarResult.Result.Id;
                else
                {
                    result.IsError = true;
                    result.Message = avatarResult.Message;
                }
            }

            result.Result = _providerPublicKeyToAvatarIdLookup[key];
            return result;
        }

        public static OASISResult<string> GetAvatarUsernameForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPublicKeyToAvatarUsernameLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPublicKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    _providerPublicKeyToAvatarUsernameLookup[key] = avatarResult.Result.Username;
                else
                {
                    result.IsError = true;
                    result.Message = avatarResult.Message;
                }
            }

            result.Result = _providerPublicKeyToAvatarUsernameLookup[key];
            return result;
        }

        public static OASISResult<IAvatar> GetAvatarForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPublicKeyToAvatarLookup.ContainsKey(key))
            {
                //TODO: Ideally need a new overload for LoadAvatarDetail that takes the public provider key.
                //TODO: In the meantime should we cache the full list of AvatarDetails? Could take up a LOT of memory so probably not good idea?
                IAvatar avatar = AvatarManager.LoadAllAvatars(providerType).FirstOrDefault(x => x.ProviderPublicKey.ContainsKey(providerType) && x.ProviderPublicKey[providerType].Contains(providerKey));

                if (avatar != null)
                {
                    _providerPublicKeyToAvatarIdLookup[key] = avatar.Id;
                    _providerPublicKeyToAvatarUsernameLookup[key] = avatar.Username;
                    _providerPublicKeyToAvatarLookup[key] = avatar;
                }
                else
                {
                    result.IsError = true;
                    result.Message = string.Concat("The provider public Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API.");
                }
            }

            result.Result = _providerPublicKeyToAvatarLookup[key];
            return result;
        }

        public static OASISResult<Guid> GetAvatarIdForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Guid> result = new OASISResult<Guid>();

            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPrivateKeyToAvatarIdLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    _providerPrivateKeyToAvatarIdLookup[key] = avatarResult.Result.Id;
                else
                {
                    result.IsError = true;
                    result.Message = avatarResult.Message;
                }
            }

            result.Result = _providerPrivateKeyToAvatarIdLookup[key];
            return result;
        }

        public static OASISResult<string> GetAvatarUsernameForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPrivateKeyToAvatarUsernameLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    _providerPrivateKeyToAvatarUsernameLookup[key] = avatarResult.Result.Username;
                else
                {
                    result.IsError = true;
                    result.Message = avatarResult.Message;
                }
            }
                
            result.Result = _providerPrivateKeyToAvatarUsernameLookup[key];
            return result;
        }

        public static OASISResult<IAvatar> GetAvatarForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            string key = string.Concat(Enum.GetName(providerType), StringCipher.Encrypt(providerKey));

            if (!_providerPrivateKeyToAvatarLookup.ContainsKey(key))
            {
                //TODO: Ideally need a new overload for LoadAvatarDetail that takes the public provider key.
                //TODO: In the meantime should we cache the full list of AvatarDetails? Could take up a LOT of memory so probably not good idea?
                IAvatar avatar = AvatarManager.LoadAllAvatars(providerType).FirstOrDefault(x => x.ProviderPrivateKey.ContainsKey(providerType) && x.ProviderPrivateKey[providerType] == providerKey);

                if (avatar != null)
                {
                    _providerPrivateKeyToAvatarIdLookup[key] = avatar.Id;
                    _providerPrivateKeyToAvatarUsernameLookup[key] = avatar.Username;
                    _providerPrivateKeyToAvatarLookup[key] = avatar;
                }
                else
                {
                    result.IsError = true;
                    result.Message = string.Concat("The provider private Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPrivateKeyToAvatar method on the AvatarManager or avatar REST API.");
                }
            }

            result.Result = _providerPrivateKeyToAvatarLookup[key];
            return result;
        }

        public static OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = avatarResult.Result.ProviderUniqueStorageKey;
            else
            {
                result.IsError = true;
                result.Message = avatarResult.Message;
            }

            return result;
        }

        public static OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = avatarResult.Result.ProviderPublicKey;
            else
            {
                result.IsError = true;
                result.Message = avatarResult.Message;
            }

            return result;
        }

        public static OASISResult<Dictionary<ProviderType, string>> GetAllProviderPrivateKeysForAvatar(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();

            if (AvatarManager.LoggedInAvatar.Id != avatarId)
            {
                result.IsError = true;
                result.Message = "ERROR: You can only retreive your own private keys, not another persons avatar.";
            }
            else
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    result.Result = avatarResult.Result.ProviderPrivateKey;

                    // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
                    foreach (ProviderType privateKeyProviderType in result.Result.Keys)
                        result.Result[privateKeyProviderType] = StringCipher.Decrypt(result.Result[privateKeyProviderType]);
                }
                else
                {
                    result.IsError = true;
                    result.Message = avatarResult.Message;
                }
            }

            return result;
        }

        private static OASISResult<string> GetProviderUniqueStorageKeyForAvatar(IAvatar avatar, string key, Dictionary<string, string> dictionaryCache, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            if (avatar != null)
            {
                if (avatar.ProviderUniqueStorageKey.ContainsKey(providerType))
                    dictionaryCache[key] = avatar.ProviderUniqueStorageKey[providerType];
                else
                {
                    result.IsError = true;
                    result.Message = string.Concat("The avatar with id ", avatar.Id, " and username ", avatar.Username, " has not been linked to the ", Enum.GetName(providerType), " provider.");
                }
            }
            else
            {
                result.IsError = true;
                result.Message = string.Concat("The avatar with id ", avatar.Id, " and username ", avatar.Username, " was not found.");
            }

            result.Result = dictionaryCache[key];
            return result;
        }
    }
}
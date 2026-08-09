using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Cryptography.ECDSA;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Rijndael256;
using static NextGenSoftware.Utilities.KeyHelper;
using KeyPair = NextGenSoftware.OASIS.API.Core.Objects.KeyPair;
using Rijndael = Rijndael256.Rijndael;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class KeyManager : OASISManager
    {
        public OASISResult<Guid> GetAvatarIdForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.
            OASISResult<Guid> result = new OASISResult<Guid>();
            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerUniqueStorageKeyToAvatarIdLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderUniqueStorageKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerUniqueStorageKeyToAvatarIdLookup[key] = avatarResult.Result.Id;
                    result.Result = _providerUniqueStorageKeyToAvatarIdLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarIdForProviderUniqueStorageKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<string> GetAvatarUsernameForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerUniqueStorageKeyToAvatarUsernameLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderUniqueStorageKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerUniqueStorageKeyToAvatarUsernameLookup[key] = avatarResult.Result.Username;
                    result.Result = _providerUniqueStorageKeyToAvatarUsernameLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarUsernameForProviderUniqueStorageKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<string> GetAvatarEmailForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerUniqueStorageKeyToAvatarEmailLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderUniqueStorageKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerUniqueStorageKeyToAvatarEmailLookup[key] = avatarResult.Result.Email;
                    result.Result = _providerUniqueStorageKeyToAvatarEmailLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarEmailForProviderUniqueStorageKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<IAvatar> GetAvatarForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerUniqueStorageKeyToAvatarLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByProviderKeyForProvider(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    IAvatar avatar = avatarResult.Result;
                    _providerUniqueStorageKeyToAvatarIdLookup[key] = avatar.Id;
                    _providerUniqueStorageKeyToAvatarUsernameLookup[key] = avatar.Username;
                    _providerUniqueStorageKeyToAvatarEmailLookup[key] = avatar.Email;
                    _providerUniqueStorageKeyToAvatarLookup[key] = avatar;

                    result.Result = _providerUniqueStorageKeyToAvatarLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error in GetAvatarForProviderUniqueStorageKey loading avatar by provider key. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<Guid> GetAvatarIdForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Guid> result = new OASISResult<Guid>();

            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPublicKeyToAvatarIdLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPublicKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerPublicKeyToAvatarIdLookup[key] = avatarResult.Result.Id;
                    result.Result = _providerPublicKeyToAvatarIdLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarIdForProviderPublicKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<string> GetAvatarUsernameForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPublicKeyToAvatarUsernameLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPublicKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerPublicKeyToAvatarUsernameLookup[key] = avatarResult.Result.Username;
                    result.Result = _providerPublicKeyToAvatarUsernameLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarUsernameForProviderPublicKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<string> GetAvatarEmailForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPublicKeyToAvatarEmailLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPublicKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerPublicKeyToAvatarEmailLookup[key] = avatarResult.Result.Email;
                    result.Result = _providerPublicKeyToAvatarEmailLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarEmailForProviderPublicKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<IAvatar> GetAvatarForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            string key = string.Concat(Enum.GetName(providerType), providerKey);

            //TODO: Ideally need a new overload for LoadAvatarDetail that takes the public provider key.
            //TODO: In the meantime should we cache the full list of AvatarDetails? Could take up a LOT of memory so probably not good idea?
            if (!_providerPublicKeyToAvatarLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByPublicKeyForProvider(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    IAvatar avatar = avatarResult.Result;
                    _providerPublicKeyToAvatarIdLookup[key] = avatar.Id;
                    _providerPublicKeyToAvatarUsernameLookup[key] = avatar.Username;
                    _providerPublicKeyToAvatarEmailLookup[key] = avatar.Email;
                    _providerPublicKeyToAvatarLookup[key] = avatar;

                    result.Result = _providerPublicKeyToAvatarLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("Error in GetAvatarForProviderPublicKey for the provider public Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType. Reason: ", avatarResult.Message), avatarResult.DetailedMessage);
            }

            return result;
        }

        /*
        public OASISResult<Guid> GetAvatarIdForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Guid> result = new OASISResult<Guid>();

            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPrivateKeyToAvatarIdLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerPrivateKeyToAvatarIdLookup[key] = avatarResult.Result.Id;
                    result.Result = _providerPrivateKeyToAvatarIdLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarIdForProviderPrivateKey. The provider public Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
            }

            return result;
        }

        public OASISResult<string> GetAvatarUsernameForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPrivateKeyToAvatarUsernameLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerPrivateKeyToAvatarUsernameLookup[key] = avatarResult.Result.Username;
                    result.Result = _providerPrivateKeyToAvatarUsernameLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarUsernameForProviderPrivateKey for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
            }

            return result;
        }

        public OASISResult<string> GetAvatarEmailForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPrivateKeyToAvatarEmailLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerPrivateKeyToAvatarEmailLookup[key] = avatarResult.Result.Email;
                    result.Result = _providerPrivateKeyToAvatarEmailLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarEmailForProviderPrivateKey for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
            }

            return result;
        }

        public OASISResult<IAvatar> GetAvatarForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            //TODO: Fix the StringCipher below or find the strongest encryption, maybe the Qunatum Encryption? :)
            //string key = string.Concat(Enum.GetName(providerType), StringCipher.Encrypt(providerKey));
            //string key = string.Concat(Enum.GetName(providerType), BC.HashPassword(providerKey));
            string key = string.Concat(Enum.GetName(providerType), Rijndael.Encrypt(providerKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256));

            if (!_providerPrivateKeyToAvatarLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByPrivateKeyForProvider(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    IAvatar avatar = avatarResult.Result;
                    _providerPublicKeyToAvatarIdLookup[key] = avatar.Id;
                    _providerPublicKeyToAvatarUsernameLookup[key] = avatar.Username;
                    _providerPublicKeyToAvatarEmailLookup[key] = avatar.Email;
                    _providerPublicKeyToAvatarLookup[key] = avatar;

                    result.Result = _providerPrivateKeyToAvatarLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("Error in GetAvatarForProviderPrivateKey for the provider private Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType. Reason: ", avatarResult.Message), avatarResult.DetailedMessage);
            }
            
            return result;
        }
        */

        /*
        public OASISResult<Guid> GetAvatarIdForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Guid> result = new OASISResult<Guid>();

            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

           // string key = string.Concat(Enum.GetName(providerType), providerKey);

            OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = avatarResult.Result.Id;
            else
                OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarIdForProviderPrivateKey. The provider public Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
            
            return result;
        }

        public OASISResult<string> GetAvatarUsernameForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

           // string key = string.Concat(Enum.GetName(providerType), providerKey);

            OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = avatarResult.Result.Username;
            else
                OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarUsernameForProviderPrivateKey for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));

            return result;
        }

        public OASISResult<string> GetAvatarEmailForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

           // string key = string.Concat(Enum.GetName(providerType), providerKey);

            OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = avatarResult.Result.Email;
            else
                OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarEmailForProviderPrivateKey for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
            
            return result;
        }

        
        public OASISResult<IAvatar> GetAvatarForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            //TODO: Fix the StringCipher below or find the strongest encryption, maybe the Qunatum Encryption? :)
            //string key = string.Concat(Enum.GetName(providerType), StringCipher.Encrypt(providerKey));
            //string key = string.Concat(Enum.GetName(providerType), BC.HashPassword(providerKey));
            //string key = string.Concat(Enum.GetName(providerType), Rijndael.Encrypt(providerKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256));


            //TODO: Ideally need a new overload for LoadAvatarDetail that takes the public provider key.
            //TODO: In the meantime should we cache the full list of AvatarDetails? Could take up a LOT of memory so probably not good idea?


            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = WalletManager.Instance.LoadAllProviderWallets(providerKey);

            if (!avatarsResult.IsError && avatarsResult.Result != null)
            {
                IAvatar avatar = avatarsResult.Result.FirstOrDefault(x => x.ProviderWallets.ContainsKey(providerType) && x.ProviderWallets[providerType].Any(x => x.PrivateKey == providerKey));

                if (avatar != null)
                {
                    result.Result = avatar;
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("The provider private Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPrivateKeyToAvatar method on the AvatarManager or avatar REST API."));
            }
            else
                OASISErrorHandling.HandleError(ref result, string.Concat("Error in GetAvatarForProviderPrivateKey for the provider private Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType. There was an error loading all avatars. Reason: ", avatarsResult.Message));

            return result;
        }*/


    }
}
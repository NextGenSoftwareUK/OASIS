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
        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            string key = string.Concat(Enum.GetName(providerType), avatarId);

            if (!_avatarIdToProviderUniqueStorageKeyLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = GetProviderUniqueStorageKeyForAvatar(avatarResult.Result, key, _avatarIdToProviderUniqueStorageKeyLookup, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderUniqueStorageKeyForAvatarById loading the avatar with id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarByUsername(string avatarUsername, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            string key = string.Concat(Enum.GetName(providerType), avatarUsername);

            if (!_avatarUsernameToProviderUniqueStorageKeyLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarUsername, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = GetProviderUniqueStorageKeyForAvatar(avatarResult.Result, key, _avatarUsernameToProviderUniqueStorageKeyLookup, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderUniqueStorageKeyForAvatarByUsername loading avatar with username {avatarUsername}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarByEmail(string avatarEmail, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            string key = string.Concat(Enum.GetName(providerType), avatarEmail);

            if (!_avatarEmailToProviderUniqueStorageKeyLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(avatarEmail, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = GetProviderUniqueStorageKeyForAvatar(avatarResult.Result, key, _avatarEmailToProviderUniqueStorageKeyLookup, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderUniqueStorageKeyForAvatarByEmail loading avatar with avatarEmail {avatarEmail}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<List<string>> GetProviderPublicKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            string key = string.Concat(Enum.GetName(providerType), avatarId);

            if (!_avatarIdToProviderPublicKeysLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.ProviderWallets.ContainsKey(providerType))
                    {
                        _avatarIdToProviderPublicKeysLookup[key] = avatarResult.Result.ProviderWallets[providerType].Select( x=> x.PublicKey).ToList();
                        result.Result = _avatarIdToProviderPublicKeysLookup[key];
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetProviderPublicKeysForAvatarById. The avatar with id ", avatarId, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPublicKeysForAvatarById loading avatar with id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<List<string>> GetProviderPublicKeysForAvatarByUsername(string avatarUsername, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            string key = string.Concat(Enum.GetName(providerType), avatarUsername);

            if (!_avatarUsernameToProviderPublicKeysLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarUsername, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.ProviderWallets.ContainsKey(providerType))
                    {
                        _avatarUsernameToProviderPublicKeysLookup[key] = avatarResult.Result.ProviderWallets[providerType].Select( x=> x.PublicKey).ToList();
                        result.Result = _avatarUsernameToProviderPublicKeysLookup[key];
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat("The avatar with username ", avatarUsername, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPublicKeysForAvatarByUsername loading avatar with username {avatarUsername}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<List<string>> GetProviderPublicKeysForAvatarByEmail(string avatarEmail, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            string key = string.Concat(Enum.GetName(providerType), avatarEmail);

            if (!_avatarEmailToProviderPublicKeysLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(avatarEmail, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.ProviderWallets.ContainsKey(providerType))
                    {
                        _avatarEmailToProviderPublicKeysLookup[key] = avatarResult.Result.ProviderWallets[providerType].Select(x => x.PublicKey).ToList();
                        result.Result = _avatarEmailToProviderPublicKeysLookup[key];
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat("The avatar with email ", avatarEmail, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPublicKeysForAvatarByEmail loading avatar with email {avatarEmail}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            
            return result;
        }

        /*
        public OASISResult<List<string>> GetProviderPrivateKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            string key = string.Concat(Enum.GetName(providerType), avatarId);

            if (AvatarManager.LoggedInAvatar.Id != avatarId)
            {
                result.IsError = true;
                result.Message = "You cannot retreive the private key for another person's avatar. Please login to this account and try again.";
            }

            if (!_avatarIdToProviderPrivateKeyLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, false, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.ProviderWallets.ContainsKey(providerType))
                    {
                        _avatarIdToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderWallets[providerType].Select(x => x.PrivateKey).ToList();

                        // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
                        for (int i = 0; i < _avatarIdToProviderPrivateKeyLookup[key].Count; i++)
                            _avatarIdToProviderPrivateKeyLookup[key][i] = Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(_avatarIdToProviderPrivateKeyLookup[key][i], OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat("The avatar with id ", avatarId, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPrivateKeyToAvatar method on the AvatarManager or avatar REST API."));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeysForAvatarById loading avatar with id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<List<string>> GetProviderPrivateKeysForAvatarByUsername(string avatarUsername, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            string key = string.Concat(Enum.GetName(providerType), avatarUsername);

            if (AvatarManager.LoggedInAvatar.Username != avatarUsername)
                OASISErrorHandling.HandleError(ref result, "Error occured in GetProviderPrivateKeysForAvatarByUsername. You cannot retreive the private key for another person's avatar. Please login to this account and try again.");

            if (!_avatarUsernameToProviderPrivateKeyLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarUsername, true, false, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.ProviderWallets.ContainsKey(providerType))
                    {
                        _avatarUsernameToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderWallets[providerType].Select(x => x.PrivateKey).ToList();
                        
                        // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
                        for (int i = 0; i < _avatarIdToProviderPrivateKeyLookup[key].Count; i++)
                            _avatarIdToProviderPrivateKeyLookup[key][i] = Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(_avatarIdToProviderPrivateKeyLookup[key][i], OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetProviderPrivateKeysForAvatarByUsername. The avatar with username ", avatarUsername, " was not found."));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeysForAvatarByUsername loading avatar with username {avatarUsername}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }*/

        public OASISResult<List<string>> GetProviderPrivateKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            //string key = string.Concat(Enum.GetName(providerType), avatarId);

            if (AvatarManager.LoggedInAvatar.Id != avatarId)
            {
                result.IsError = true;
                result.Message = "You cannot retreive the private key for another person's avatar. Please login to this account and try again.";
            }

            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = WalletManager.Instance.LoadProviderWalletsForAvatarById(avatarId);

            //We use to cache the private keys but for security we no longer do this and instead load them from local storage each time they are needed (since they are local storage not much need to cache anyway).
            if (!walletsResult.IsError && walletsResult.Result != null)
            {
                if (walletsResult.Result.ContainsKey(providerType))
                {
                    result.Result = walletsResult.Result[providerType].Select(x => x.PrivateKey).ToList();

                    for (int i = 0; i < result.Result.Count; i++)
                    {
                        if (result.Result[i] != null)
                            result.Result[i] = Rijndael256.Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(result.Result[i], OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("The avatar with id ", avatarId, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPrivateKeyToAvatar method on the AvatarManager or avatar REST API."));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeysForAvatarById loading avatar wallets with id {avatarId}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);

            return result;
        }

        public OASISResult<List<string>> GetProviderPrivateKeysForAvatarByUsername(string avatarUsername, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            //string key = string.Concat(Enum.GetName(providerType), avatarUsername);

            if (AvatarManager.LoggedInAvatar.Username != avatarUsername)
                OASISErrorHandling.HandleError(ref result, "Error occured in GetProviderPrivateKeysForAvatarByUsername. You cannot retreive the private key for another person's avatar. Please login to this account and try again.");

            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = WalletManager.Instance.LoadProviderWalletsForAvatarByUsername(avatarUsername);

            //We use to cache the private keys but for security we no longer do this and instead load them from local storage each time they are needed (since they are local storage not much need to cache anyway).
            if (!walletsResult.IsError && walletsResult.Result != null)
            {
                if (walletsResult.Result.ContainsKey(providerType))
                {
                    result.Result = walletsResult.Result[providerType].Select(x => x.PrivateKey).ToList();

                    for (int i = 0; i < result.Result.Count; i++)
                    {
                        if (result.Result[i] != null)
                            result.Result[i] = Rijndael256.Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(result.Result[i], OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("The avatar with username ", avatarUsername, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPrivateKeyToAvatar method on the AvatarManager or avatar REST API."));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeysForAvatarByUsername loading avatar wallets with username {avatarUsername}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);

            return result;
        }

        //public OASISResult<string> GetProviderPrivateKeyForAvatarByEmail(string avatarEmail, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<string> result = new OASISResult<string>();
        //    string key = string.Concat(Enum.GetName(providerType), avatarEmail);

        //    if (AvatarManager.LoggedInAvatar.Email != avatarEmail)
        //        OASISErrorHandling.HandleError(ref result, "Error occured in GetProviderPrivateKeyForAvatarByEmail. You cannot retreive the private key for another person's avatar. Please login to this account and try again.");

        //    if (!_avatarEmailToProviderPrivateKeyLookup.ContainsKey(key))
        //    {
        //        OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(avatarEmail, false, providerType);

        //        if (!avatarResult.IsError && avatarResult.Result != null)
        //        {
        //            if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
        //                _avatarEmailToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderPrivateKey[providerType];
        //            else
        //                OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetProviderPrivateKeyForAvatarByEmail. The avatar with email ", avatarEmail, " was not found."));
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeyForAvatarByEmail loading avatar with email {avatarEmail}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
        //    }

        //    //result.Result = StringCipher.Decrypt(_avatarEmailToProviderPrivateKeyLookup[key]);
        //    result.Result = Rijndael.Decrypt(_avatarEmailToProviderPrivateKeyLookup[key], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
        //    return result;
        //}

        //public OASISResult<string> GetProviderPrivateKeyForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<string> result = new OASISResult<string>();
        //    string key = string.Concat(Enum.GetName(providerType), avatarUsername);

        //    if (AvatarManager.LoggedInAvatar.Email != email)
        //        OASISErrorHandling.HandleError(ref result, "Error occured in GetProviderPrivateKeyForAvatar. You cannot retreive the private key for another person's avatar. Please login to this account and try again.");

        //    if (!_avatarUsernameToProviderPrivateKeyLookup.ContainsKey(key))
        //    {
        //        OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, providerType);

        //        if (!avatarResult.IsError && avatarResult.Result != null)
        //        {
        //            if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
        //                _avatarIdToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderPrivateKey[providerType];
        //            else
        //                OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetProviderPrivateKeyForAvatar. The avatar with username ", avatarUsername, " was not found."));
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeyForAvatar loading avatar with username {avatarUsername}. Reason: {avatarResult.Message}");
        //    }

        //    result.Result = StringCipher.Decrypt(_avatarUsernameToProviderPrivateKeyLookup[key]);
        //    return result;
        //}

    }
}
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
        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = avatarResult.Result.ProviderUniqueStorageKey;
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderUniqueStorageKeysForAvatarById loading avatar with avatarId {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarByUsername(string username, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = avatarResult.Result.ProviderUniqueStorageKey;
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderUniqueStorageKeysForAvatarByUsername loading avatar with username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = avatarResult.Result.ProviderUniqueStorageKey;
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderUniqueStorageKeysForAvatarByEmail loading avatar with email {email}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        

        public OASISResult<Dictionary<ProviderType, List<KeyPair>>> GetAllProviderKeyPairsForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<KeyPair>>> result = new OASISResult<Dictionary<ProviderType, List<KeyPair>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, false, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<KeyPair>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<KeyPair>();

                        if (wallet.PublicKey != null || wallet.PrivateKey != null)
                        {
                            result.Result[provider].Add(new KeyPair()
                            {
                                PrivateKey = wallet.PrivateKey != null ? Rijndael256.Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(wallet.PrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256) : null,
                                PublicKey = wallet.PublicKey
                            });
                        }
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderKeyPairsForAvatarById loading avatar with avatarId {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderWalletAddressesForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.WalletAddress != null)
                            result.Result[provider].Add(wallet.WalletAddress);
                    }

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderWalletAddressesForAvatarById loading avatar with avatarId {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderWalletAddressesForAvatarByUsername(string username, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.WalletAddress != null)
                            result.Result[provider].Add(wallet.WalletAddress);
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderWalletAddressesForAvatarByUsername loading avatar with username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderWalletAddressesForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.WalletAddress != null)
                            result.Result[provider].Add(wallet.WalletAddress);
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderWalletAddressesForAvatarByEmail loading avatar with email {email}. Reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.PublicKey != null)
                            result.Result[provider].Add(wallet.PublicKey);
                    }

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderPublicKeysForAvatarById loading avatar with avatarId {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarByUsername(string username, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.PublicKey != null)
                            result.Result[provider].Add(wallet.PublicKey);
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderPublicKeysForAvatarByUsername loading avatar with username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.PublicKey != null)
                            result.Result[provider].Add(wallet.PublicKey);
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderPublicKeysForAvatarByEmail loading avatar with email {email}. Reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPrivateKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();

            if (AvatarManager.LoggedInAvatar.Id != avatarId)
                OASISErrorHandling.HandleError(ref result, "An error occured in GetAllProviderPrivateKeysForAvatarById. You can only retreive your own private keys, not another persons avatar.");
            else
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, false, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    result.Result = new Dictionary<ProviderType, List<string>>();

                    foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                    {
                        //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PrivateKey).ToList();

                        //for (int i = 0; i < result.Result[provider].Count; i++)
                        //{
                        //    if (result.Result[provider][i] == null)
                        //        result.Result[provider].Remove(result.Result[provider][i]);
                        //}

                        foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                        {
                            if (!result.Result.ContainsKey(provider))
                                result.Result[provider] = new List<string>();

                            if (wallet.PrivateKey != null)
                                result.Result[provider].Add(wallet.PrivateKey);
                        }

                        // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
                        for (int i = 0; i < result.Result[provider].Count; i++)
                        {
                            if (result.Result[provider][i] != null)
                                result.Result[provider][i] = Rijndael256.Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(result.Result[provider][i], OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"An error occured in GetAllProviderPrivateKeysForAvatarById, the avatar with id {avatarId} could not be loaded. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPrivateKeysForAvatarByUsername(string username, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();

            if (AvatarManager.LoggedInAvatar == null)
                OASISErrorHandling.HandleError(ref result, "Error occured in GetAllProviderPrivateKeysForAvatarByUsername, AvatarManager.LoggedInAvatar is null!");
            
            else if (AvatarManager.LoggedInAvatar.Username != username)
                OASISErrorHandling.HandleError(ref result, "Error occured in GetAllProviderPrivateKeysForAvatarByUsername, you can only retreive your own private keys, not another persons avatar.");
            else
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, true, false, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    result.Result = new Dictionary<ProviderType, List<string>>();

                    foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                    {
                        //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PrivateKey).ToList();

                        //for (int i = 0; i < result.Result[provider].Count; i++)
                        //{
                        //    if (result.Result[provider][i] == null)
                        //        result.Result[provider].Remove(result.Result[provider][i]);
                        //}

                        foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                        {
                            if (!result.Result.ContainsKey(provider))
                                result.Result[provider] = new List<string>();

                            if (wallet.PrivateKey != null)
                                result.Result[provider].Add(wallet.PrivateKey);
                        }

                        // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
                        for (int i = 0; i < result.Result[provider].Count; i++)
                        {
                            if (result.Result[provider][i] != null)
                                result.Result[provider][i] = Rijndael256.Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(result.Result[provider][i], OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"An error occured in GetAllProviderPrivateKeysForAvatarByUsername, the avatar with username {username} could not be loaded. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        //public OASISResult<Dictionary<ProviderType, string>> GetAllProviderPrivateKeysForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();

        //    if (AvatarManager.LoggedInAvatar.Email != email)
        //        OASISErrorHandling.HandleError(ref result, "Error occured in GetAllProviderPrivateKeysForAvatarByEmail, you can only retreive your own private keys, not another persons avatar.");
        //    else
        //    {
        //        OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, providerType);

        //        if (!avatarResult.IsError && avatarResult.Result != null)
        //        {
        //            result.Result = avatarResult.Result.ProviderPrivateKey;

        //            // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
        //            foreach (ProviderType privateKeyProviderType in result.Result.Keys)
        //                result.Result[privateKeyProviderType] = Rijndael.Decrypt(result.Result[privateKeyProviderType], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
        //                //result.Result[privateKeyProviderType] = StringCipher.Decrypt(result.Result[privateKeyProviderType]);
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"An error occured in GetAllProviderPrivateKeysForAvatarByEmail, the avatar with email {email} could not be loaded. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
        //    }

        //    return result;
        //}

    }
}
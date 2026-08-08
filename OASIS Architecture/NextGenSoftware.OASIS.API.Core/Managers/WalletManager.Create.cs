using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using Newtonsoft.Json;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Rijndael256;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class WalletManager : OASISManager
    {
        public OASISResult<IProviderWallet> CreateWalletWithoutSaving(Guid avatarId, string name, string description, ProviderType walletProviderType, bool generateKeyPair = true, bool isDefaultWallet = false)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            ProviderWallet newWallet = new ProviderWallet()
            {
                WalletId = Guid.NewGuid(),
                AvatarId = avatarId,
                Name = name,
                Description = description,
                CreatedByAvatarId = avatarId,
                CreatedDate = DateTime.Now,
                //WalletAddress = walletAddress,
                ProviderType = walletProviderType,
                SecretRecoveryPhrase = PasswordEncryptionHelper.WrapQuantumLayer(Rijndael.Encrypt(string.Join(" ", new Mnemonic(Wordlist.English, WordCount.Twelve).Words), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256), OASISDNA.OASIS.Security.OASISProviderPrivateKeys),
                //PrivateKey = privateKey,
                IsDefaultWallet = isDefaultWallet
            };

            if (generateKeyPair)
            {
                OASISResult<IKeyPairAndWallet> keyPair = KeyManager.Instance.GenerateKeyPairWithWalletAddress(walletProviderType);

                if (keyPair != null && keyPair.Result != null && !keyPair.IsError)
                {
                    newWallet.PrivateKey = PasswordEncryptionHelper.WrapQuantumLayer(Rijndael.Encrypt(keyPair.Result.PrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256), OASISDNA.OASIS.Security.OASISProviderPrivateKeys);
                    newWallet.PublicKey = keyPair.Result.PublicKey;
                    newWallet.WalletAddress = keyPair.Result.WalletAddressLegacy;
                }
            }

            result.Result = newWallet;
            return result;
        }

        public async Task<OASISResult<IProviderWallet>> CreateWalletForAvatarByIdAsync(Guid avatarId, string name, string description, ProviderType walletProviderType, bool generateKeyPair = true, bool isDefaultWallet = false, bool showSecretRecoveryPhase = false, bool showPrivateKey = false, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.CreateWalletForAvatarByIdAsync. Reason: ";

            try
            {
                OASISResult<IProviderWallet> createResult = CreateWalletWithoutSaving(avatarId, name, description, walletProviderType, generateKeyPair, isDefaultWallet);

                if (createResult != null && createResult.Result != null && !createResult.IsError)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByIdAsync(avatarId, providerTypeToLoadFrom: providerTypeToLoadSave);

                    if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                    {
                        if (!providerWallets.Result.ContainsKey(walletProviderType))
                            providerWallets.Result[walletProviderType] = new List<IProviderWallet>();
                        
                        else if (providerWallets.Result[walletProviderType] == null)
                            providerWallets.Result[walletProviderType] = new List<IProviderWallet>();

                        if (isDefaultWallet)
                        {
                            foreach (IProviderWallet wallet in providerWallets.Result[walletProviderType])
                                wallet.IsDefaultWallet = false;
                        }

                        providerWallets.Result[walletProviderType].Add(createResult.Result);

                        OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByIdAsync(avatarId, providerWallets.Result, providerTypeToLoadSave);

                        if (saveResult != null && saveResult.Result && !saveResult.IsError)
                        {
                            OASISResult<IProviderWallet> walletResult = ProcessDecryption(createResult.Result, showPrivateKey, showSecretRecoveryPhase, avatarId);

                            if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                                result.Result = walletResult.Result;
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured whilst processing decryption for avatar {avatarId} and wallet {createResult.Result.Id}. Reason: {walletResult.Message}", walletResult.DetailedMessage);

                            result.Message = "Wallet Created Successfully";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByIdAsync. Reason: {saveResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling LoadProviderWalletsForAvatarByIdAsync. Reason: {providerWallets.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured creating wallet calling CreateWallet. Reason: {createResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<IProviderWallet> CreateWalletForAvatarById(Guid avatarId, string name, string description, ProviderType walletProviderType, bool generateKeyPair = true, bool isDefaultWallet = false, bool showSecretRecoveryPhase = false, bool showPrivateKey = false, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.CreateWalletForAvatarById. Reason: ";

            try
            {
                OASISResult<IProviderWallet> createResult = CreateWalletWithoutSaving(avatarId, name, description, walletProviderType, generateKeyPair, isDefaultWallet);

                if (createResult != null && createResult.Result != null && !createResult.IsError)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarById(avatarId, providerTypeToLoadFrom: providerTypeToLoadSave);

                    if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                    {
                        if (!providerWallets.Result.ContainsKey(walletProviderType))
                            providerWallets.Result[walletProviderType] = new List<IProviderWallet>();

                        else if (providerWallets.Result[walletProviderType] == null)
                            providerWallets.Result[walletProviderType] = new List<IProviderWallet>();

                        if (isDefaultWallet)
                        {
                            foreach (IProviderWallet wallet in providerWallets.Result[walletProviderType])
                                wallet.IsDefaultWallet = false;
                        }

                        providerWallets.Result[walletProviderType].Add(createResult.Result);

                        OASISResult<bool> saveResult = SaveProviderWalletsForAvatarById(avatarId, providerWallets.Result, providerTypeToLoadSave);

                        if (saveResult != null && saveResult.Result && !saveResult.IsError)
                        {
                            OASISResult<IProviderWallet> walletResult = ProcessDecryption(createResult.Result, showPrivateKey, showSecretRecoveryPhase, avatarId);

                            if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                                result.Result = walletResult.Result;
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured whilst processing decryption for avatar {avatarId} and wallet {createResult.Result.Id}. Reason: {walletResult.Message}", walletResult.DetailedMessage);

                            result.Message = "Wallet Created Successfully";
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByIdAsync. Reason: {saveResult.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling LoadProviderWalletsForAvatarByIdAsync. Reason: {providerWallets.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured creating wallet calling CreateWallet. Reason: {createResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> CreateWalletForAvatarByUsernameAsync(string username, string name, string description, ProviderType walletProviderType, bool generateKeyPair = true, bool isDefaultWallet = false, bool showSecretRecoveryPhase = false, bool showPrivateKey = false, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.CreateWalletForAvatarByUsernameAsync. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(username, providerType: providerTypeToLoadSave);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    OASISResult<IProviderWallet> createResult = CreateWalletWithoutSaving(avatarResult.Result.Id, name, description, walletProviderType, generateKeyPair, isDefaultWallet);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByUsername(username, providerTypeToLoadFrom: providerTypeToLoadSave);

                        if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                        {
                            if (providerWallets.Result[walletProviderType] == null)
                                providerWallets.Result[walletProviderType] = new List<IProviderWallet>();

                            if (isDefaultWallet)
                            {
                                foreach (IProviderWallet wallet in providerWallets.Result[walletProviderType])
                                    wallet.IsDefaultWallet = false;
                            }

                            providerWallets.Result[walletProviderType].Add(createResult.Result);

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByUsername(username, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                OASISResult<IProviderWallet> walletResult = ProcessDecryption(createResult.Result, showPrivateKey, showSecretRecoveryPhase, avatarResult.Result.Id);

                                if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                                    result.Result = walletResult.Result;
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured whilst processing decryption for avatar {avatarResult.Result.Id} and wallet {createResult.Result.Id}. Reason: {walletResult.Message}", walletResult.DetailedMessage);

                                result.Message = "Wallet Created Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByUsername. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling LoadProviderWalletsForAvatarByUsername. Reason: {providerWallets.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured creating wallet calling CreateWallet. Reason: {createResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading avatar calling LoadAvatarAsync. Reason: {avatarResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<IProviderWallet> CreateWalletForAvatarByUsername(string username, string name, string description, ProviderType walletProviderType, bool generateKeyPair = true, bool isDefaultWallet = false, bool showSecretRecoveryPhase = false, bool showPrivateKey = false, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.CreateWalletForAvatarByUsername. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(username, providerType: providerTypeToLoadSave);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    OASISResult<IProviderWallet> createResult = CreateWalletWithoutSaving(avatarResult.Result.Id, name, description, walletProviderType, generateKeyPair, isDefaultWallet);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByUsername(username, providerTypeToLoadFrom: providerTypeToLoadSave);

                        if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                        {
                            if (providerWallets.Result[walletProviderType] == null)
                                providerWallets.Result[walletProviderType] = new List<IProviderWallet>();

                            if (isDefaultWallet)
                            {
                                foreach (IProviderWallet wallet in providerWallets.Result[walletProviderType])
                                    wallet.IsDefaultWallet = false;
                            }

                            providerWallets.Result[walletProviderType].Add(createResult.Result);

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByUsername(username, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                OASISResult<IProviderWallet> walletResult = ProcessDecryption(createResult.Result, showPrivateKey, showSecretRecoveryPhase, avatarResult.Result.Id);

                                if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                                    result.Result = walletResult.Result;
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured whilst processing decryption for avatar {avatarResult.Result.Id} and wallet {createResult.Result.Id}. Reason: {walletResult.Message}", walletResult.DetailedMessage);

                                result.Message = "Wallet Created Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByUsername. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling LoadProviderWalletsForAvatarByUsername. Reason: {providerWallets.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured creating wallet calling CreateWallet. Reason: {createResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading avatar calling LoadAvatar. Reason: {avatarResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> CreateWalletForAvatarByEmailAsync(string email, string name, string description, ProviderType walletProviderType, bool generateKeyPair = true, bool isDefaultWallet = false, bool showSecretRecoveryPhase = false, bool showPrivateKey = false, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.CreateWalletForAvatarByEmailAsync. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarByEmailAsync(email, providerType: providerTypeToLoadSave);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    OASISResult<IProviderWallet> createResult = CreateWalletWithoutSaving(avatarResult.Result.Id, name, description, walletProviderType, generateKeyPair, isDefaultWallet);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByEmailAsync(email, providerTypeToLoadFrom: providerTypeToLoadSave);

                        if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                        {
                            if (providerWallets.Result[walletProviderType] == null)
                                providerWallets.Result[walletProviderType] = new List<IProviderWallet>();

                            if (isDefaultWallet)
                            {
                                foreach (IProviderWallet wallet in providerWallets.Result[walletProviderType])
                                    wallet.IsDefaultWallet = false;
                            }

                            providerWallets.Result[walletProviderType].Add(createResult.Result);

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByEmailAsync(email, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                OASISResult<IProviderWallet> walletResult = ProcessDecryption(createResult.Result, showPrivateKey, showSecretRecoveryPhase, avatarResult.Result.Id);

                                if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                                    result.Result = walletResult.Result;
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured whilst processing decryption for avatar {avatarResult.Result.Id} and wallet {createResult.Result.Id}. Reason: {walletResult.Message}", walletResult.DetailedMessage);

                                result.Message = "Wallet Created Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByEmailAsync. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling LoadProviderWalletsForAvatarByEmailAsync. Reason: {providerWallets.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured creating wallet calling CreateWallet. Reason: {createResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading avatar calling LoadAvatarByEmailAsync. Reason: {avatarResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<IProviderWallet> CreateWalletForAvatarByEmail(string email, string name, string description, ProviderType walletProviderType, bool generateKeyPair = true, bool isDefaultWallet = false, bool showSecretRecoveryPhase = false, bool showPrivateKey = false, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.CreateWalletForAvatarByEmail. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByEmail(email, providerType: providerTypeToLoadSave);

                if (avatarResult != null && avatarResult.Result != null && !avatarResult.IsError)
                {
                    OASISResult<IProviderWallet> createResult = CreateWalletWithoutSaving(avatarResult.Result.Id, name, description, walletProviderType, generateKeyPair, isDefaultWallet);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByUsername(email, providerTypeToLoadFrom: providerTypeToLoadSave);

                        if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                        {
                            if (providerWallets.Result[walletProviderType] == null)
                                providerWallets.Result[walletProviderType] = new List<IProviderWallet>();

                            if (isDefaultWallet)
                            {
                                foreach (IProviderWallet wallet in providerWallets.Result[walletProviderType])
                                    wallet.IsDefaultWallet = false;
                            }

                            providerWallets.Result[walletProviderType].Add(createResult.Result);

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByUsername(email, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                OASISResult<IProviderWallet> walletResult = ProcessDecryption(createResult.Result, showPrivateKey, showSecretRecoveryPhase, avatarResult.Result.Id);

                                if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                                    result.Result = walletResult.Result;
                                else
                                    OASISErrorHandling.HandleError(ref result, $"Error occured in CreateWalletForAvatarByEmail whilst processing decryption for avatar {avatarResult.Result.Id} and wallet {createResult.Result.Id}. Reason: {walletResult.Message}", walletResult.DetailedMessage);

                                result.Message = "Wallet Created Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByUsername. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling LoadProviderWalletsForAvatarByUsername. Reason: {providerWallets.Message}");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured creating wallet calling CreateWallet. Reason: {createResult.Message}");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured loading avatar calling LoadAvatarByEmail. Reason: {avatarResult.Message}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

    }
}
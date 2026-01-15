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
    //TODO: Add Async version of all methods and add IWalletManager Interface.
    public class WalletManager : OASISManager
    {
        private static WalletManager _instance = null;

        public static WalletManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new WalletManager(ProviderManager.Instance.CurrentStorageProvider);

                return _instance;
            }
        }

        public WalletManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        public OASISResult<IProviderWallet> CreateWalletWithoutSaving(Guid avatarId, string name, string description, ProviderType walletProviderType, bool generateKeyPair = true, bool isDefaultWallet = false)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            ProviderWallet newWallet = new ProviderWallet()
            {
                WalletId = Guid.NewGuid(),
                Name = name,
                Description = description,
                CreatedByAvatarId = avatarId,
                CreatedDate = DateTime.Now,
                //WalletAddress = walletAddress,
                ProviderType = walletProviderType,
                SecretRecoveryPhrase = Rijndael.Encrypt(string.Join(" ", new Mnemonic(Wordlist.English, WordCount.Twelve).Words), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256),
                //PrivateKey = privateKey,
                IsDefaultWallet = isDefaultWallet
            };

            if (generateKeyPair)
            {
                OASISResult<IKeyPairAndWallet> keyPair = KeyManager.Instance.GenerateKeyPairWithWalletAddress(walletProviderType);

                if (keyPair != null && keyPair.Result != null && !keyPair.IsError)
                {
                    newWallet.PrivateKey = Rijndael.Encrypt(keyPair.Result.PrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
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

        public async Task<OASISResult<IProviderWallet>> UpdateWalletForAvatarByIdAsync(Guid avatarId, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarByIdAsync. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByIdAsync(avatarId, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByIdAsync(avatarId, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByIdAsync. Reason: {saveResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<IProviderWallet> UpdateWalletForAvatarById(Guid avatarId, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarById. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarById(avatarId, false, false, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarById(avatarId, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarById. Reason: {saveResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> UpdateWalletForAvatarByUsernameAsync(string username, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarByUsernameAsync. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByUsernameAsync(username, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByUsernameAsync(username, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByIdAsync. Reason: {saveResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<IProviderWallet> UpdateWalletForAvatarByUsername(string username, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarByUsername. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByUsername(username, false, false, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByUsername(username, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarById. Reason: {saveResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> UpdateWalletForAvatarByEmailAsync(string email, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarByEmailAsync. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByEmailAsync(email, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByEmailAsync(email, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByEmailAsync. Reason: {saveResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<IProviderWallet> UpdateWalletForAvatarByEmail(string email, Guid walletId, string name, string description, ProviderType walletProviderType, ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in WalletManager.UpdateWalletForAvatarByEmail. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByEmail(email, false, false, providerTypeToLoadFrom: providerTypeToLoadSave);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        IProviderWallet wallet = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.Name = name;
                            wallet.Description = description;
                            wallet.ProviderType = walletProviderType;

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByEmail(email, providerWallets.Result, providerTypeToLoadSave);

                            if (saveResult != null && saveResult.Result && !saveResult.IsError)
                            {
                                result.Result = wallet;
                                result.Message = "Wallet Saved Successfully";
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving wallets calling SaveProviderWalletsForAvatarByEmail. Reason: {saveResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<ISendWeb4TokenResponse>> SendTokenAsync(Guid avatarId, ISendWeb4TokenRequest request)
        {
            OASISResult<ISendWeb4TokenResponse> result = new OASISResult<ISendWeb4TokenResponse>(new SendWeb4TokenResponse());
            OASISResult<ITransactionResponse> blockchainResult = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error Occured in SendTokenAsync function. Reason: ";

            if (string.IsNullOrEmpty(request.FromWalletAddress))
            {
                //Try and lookup the wallet address from the avatar id/username/email if one of those is provided.
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

                if (avatarId != Guid.Empty)
                    walletsResult = await LoadProviderWalletsForAvatarByIdAsync(avatarId, false, false, false, request.FromProvider.Value);


                //if (request.FromAvatarId != Guid.Empty)
                //    walletsResult = await LoadProviderWalletsForAvatarByIdAsync(request.FromAvatarId, false, false, request.FromProvider.Value);

                //else if (!string.IsNullOrEmpty(request.FromAvatarUsername))
                //    walletsResult = await LoadProviderWalletsForAvatarByUsernameAsync(request.FromAvatarUsername, false, false, request.FromProvider.Value);

                //else if (!string.IsNullOrEmpty(request.FromAvatarEmail))
                //    walletsResult = await LoadProviderWalletsForAvatarByEmailAsync(request.FromAvatarEmail, false, false, request.FromProvider.Value);

                //else
                //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} You must provide at least one of the following to identify the sender: FromWalletAddress, FromAvatarId, FromAvatarUsername or FromAvatarEmail.");

                if (!walletsResult.IsError && walletsResult.Result != null && walletsResult.Result.ContainsKey(request.FromProvider.Value) && walletsResult.Result[request.FromProvider.Value] != null)
                {
                    IProviderWallet wallet = walletsResult.Result[request.FromProvider.Value].FirstOrDefault();

                    if (wallet != null)
                        request.FromWalletAddress = wallet.WalletAddress;
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.FromProvider.Name} so the transaction cannot be sent.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.FromProvider.Name} so the transaction cannot be sent. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }

            if (string.IsNullOrEmpty(request.ToWalletAddress))
            {
                //Try and lookup the wallet address from the avatar id/username/email if one of those is provided.
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
                if (request.ToAvatarId != Guid.Empty)

                    walletsResult = await LoadProviderWalletsForAvatarByIdAsync(request.ToAvatarId, false, false, false, request.ToProvider.Value);

                else if (!string.IsNullOrEmpty(request.ToAvatarUsername))
                    walletsResult = await LoadProviderWalletsForAvatarByUsernameAsync(request.ToAvatarUsername, false, false, false, request.ToProvider.Value);

                else if (!string.IsNullOrEmpty(request.ToAvatarEmail))
                    walletsResult = await LoadProviderWalletsForAvatarByEmailAsync(request.ToAvatarEmail, false, false, false, request.ToProvider.Value);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} You must provide at least one of the following to identify the receiver: ToWalletAddress, ToAvatarId, ToAvatarUsername or ToAvatarEmail.");

                if (!walletsResult.IsError && walletsResult.Result != null && walletsResult.Result.ContainsKey(request.ToProvider.Value) && walletsResult.Result[request.ToProvider.Value] != null)
                {
                    IProviderWallet wallet = walletsResult.Result[request.ToProvider.Value].FirstOrDefault();

                    if (wallet != null)
                        request.ToWalletAddress = wallet.WalletAddress;
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.ToProvider.Name} so the transaction cannot be sent.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.ToProvider.Name} so the transaction cannot be sent. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The FromProviderType {Enum.GetName(typeof(ProviderType), request.FromProvider)} is not a OASIS Blockchain  Provider. Please make sure you sepcify a OASIS Blockchain Provider.");


            if (result.IsError)
                return result;

            if (request.FromProvider.Name == request.ToProvider.Name)
            {
                blockchainResult = await SendTokenInternalAsync(request);

                if (blockchainResult != null && blockchainResult.Result != null && !blockchainResult.IsError)
                {
                    result.Message = "Token Sent Successfully";
                    result.Result.SendTransactionResult = blockchainResult.Result.TransactionResult;
                }
            }
            else
            {
                // Cross-chain transfer: Use BridgeManager for atomic swaps
                try
                {
                    // Get token symbols from provider types
                    var fromToken = GetTokenSymbolForProvider(request.FromProvider.Value);
                    var toToken = GetTokenSymbolForProvider(request.ToProvider.Value);

                    if (string.IsNullOrEmpty(fromToken) || string.IsNullOrEmpty(toToken))
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            $"{errorMessage} Unable to determine token symbols for providers {request.FromProvider.Name} and {request.ToProvider.Name}. Cross-chain transfers require valid blockchain providers.");
                        return result;
                    }

                    // Get BridgeManager instance
                    var bridgeManager = BridgeManager.Instance;

                    // Create bridge order request
                    var bridgeOrderRequest = new NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs.CreateBridgeOrderRequest
                    {
                        FromToken = fromToken,
                        ToToken = toToken,
                        Amount = request.Amount,
                        FromAddress = request.FromWalletAddress,
                        DestinationAddress = request.ToWalletAddress,
                        UserId = avatarId,
                        ExpiresInMinutes = 30
                    };

                    // Execute cross-chain bridge order (atomic swap)
                    var bridgeResult = await bridgeManager.CreateBridgeOrderAsync(bridgeOrderRequest);

                    if (bridgeResult != null && !bridgeResult.IsError && bridgeResult.Result != null)
                    {
                        result.Message = $"Cross-chain token transfer initiated successfully. Bridge Order ID: {bridgeResult.Result.OrderId}";
                        result.Result.SendTransactionResult = bridgeResult.Result.OrderId.ToString();
                        result.IsError = false;

                        // Store bridge order ID for tracking
                        result.Result.BridgeOrderId = bridgeResult.Result.OrderId.ToString();
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            $"{errorMessage} Cross-chain bridge operation failed. Reason: {bridgeResult?.Message ?? "Unknown error"}");
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"{errorMessage} Exception during cross-chain transfer: {ex.Message}", ex);
                }
            }

            return result;
        }

        public async Task<OASISResult<ISendWeb4NFTResponse>> SendNFTAsync(Guid avatarId, ISendWeb4NFTRequest request)
        {
            OASISResult<ISendWeb4NFTResponse> result = new OASISResult<ISendWeb4NFTResponse>(new SendWeb4NFTResponse());
            OASISResult<IWeb3NFTTransactionResponse> blockchainResult = new OASISResult<IWeb3NFTTransactionResponse>();
            string errorMessage = "Error Occured in SendNFTAsync function. Reason: ";

            // Resolve wallet addresses if not provided
            if (string.IsNullOrEmpty(request.FromWalletAddress))
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

                if (avatarId != Guid.Empty)
                    walletsResult = await LoadProviderWalletsForAvatarByIdAsync(avatarId, false, false, false, request.FromProvider.Value);

                if (!walletsResult.IsError && walletsResult.Result != null && walletsResult.Result.ContainsKey(request.FromProvider.Value) && walletsResult.Result[request.FromProvider.Value] != null)
                {
                    IProviderWallet wallet = walletsResult.Result[request.FromProvider.Value].FirstOrDefault();
                    if (wallet != null)
                        request.FromWalletAddress = wallet.WalletAddress;
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.FromProvider.Name}.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.FromProvider.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }

            if (string.IsNullOrEmpty(request.ToWalletAddress))
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
                if (request.ToAvatarId != Guid.Empty)
                    walletsResult = await LoadProviderWalletsForAvatarByIdAsync(request.ToAvatarId, false, false, false, request.ToProvider.Value);
                else if (!string.IsNullOrEmpty(request.ToAvatarUsername))
                    walletsResult = await LoadProviderWalletsForAvatarByUsernameAsync(request.ToAvatarUsername, false, false, false, request.ToProvider.Value);
                else if (!string.IsNullOrEmpty(request.ToAvatarEmail))
                    walletsResult = await LoadProviderWalletsForAvatarByEmailAsync(request.ToAvatarEmail, false, false, false, request.ToProvider.Value);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} You must provide at least one of the following to identify the receiver: ToWalletAddress, ToAvatarId, ToAvatarUsername or ToAvatarEmail.");

                if (!walletsResult.IsError && walletsResult.Result != null && walletsResult.Result.ContainsKey(request.ToProvider.Value) && walletsResult.Result[request.ToProvider.Value] != null)
                {
                    IProviderWallet wallet = walletsResult.Result[request.ToProvider.Value].FirstOrDefault();
                    if (wallet != null)
                        request.ToWalletAddress = wallet.WalletAddress;
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.ToProvider.Name}.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.ToProvider.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }

            if (result.IsError)
                return result;

            // Check if same-chain or cross-chain
            if (request.FromProvider.Name == request.ToProvider.Name)
            {
                // Same-chain: Direct NFT transfer
                IOASISNFTProvider nftProvider = ProviderManager.Instance.GetProvider(request.FromProvider.Value) as IOASISNFTProvider;
                if (nftProvider != null)
                {
                    var sendRequest = new SendWeb3NFTRequest
                    {
                        FromNFTTokenAddress = request.FromNFTTokenAddress,
                        FromWalletAddress = request.FromWalletAddress,
                        ToWalletAddress = request.ToWalletAddress,
                        TokenAddress = request.TokenAddress,
                        TokenId = request.TokenId,
                        Amount = request.Amount,
                        MemoText = request.MemoText
                    };

                    blockchainResult = await nftProvider.SendNFTAsync(sendRequest);
                    if (blockchainResult != null && blockchainResult.Result != null && !blockchainResult.IsError)
                    {
                        result.Message = "NFT Sent Successfully";
                        result.Result.SendTransactionResult = blockchainResult.Result.TransactionResult;
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Provider {request.FromProvider.Name} does not support NFT operations.");
                }
            }
            else
            {
                // Cross-chain NFT transfer: Use bridge
                try
                {
                    // Get NFT providers for both chains
                    IOASISNFTProvider fromNFTProvider = ProviderManager.Instance.GetProvider(request.FromProvider.Value) as IOASISNFTProvider;
                    IOASISNFTProvider toNFTProvider = ProviderManager.Instance.GetProvider(request.ToProvider.Value) as IOASISNFTProvider;

                    if (fromNFTProvider == null || toNFTProvider == null)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            $"{errorMessage} One or both providers do not support NFT operations.");
                        return result;
                    }

                    // Step 1: Withdraw NFT from source chain (locks it)
                    var withdrawResult = await fromNFTProvider.WithdrawNFTAsync(
                        request.TokenAddress ?? request.FromNFTTokenAddress,
                        request.TokenId,
                        request.FromWalletAddress,
                        string.Empty // Private key would be retrieved securely in production
                    );

                    if (withdrawResult.IsError || withdrawResult.Result == null)
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            $"{errorMessage} Failed to withdraw/lock NFT on source chain: {withdrawResult.Message}");
                        return result;
                    }

                    result.Result.LockTransactionResult = withdrawResult.Result.TransactionId;

                    // Step 2: Deposit NFT to destination chain (mints wrapped NFT)
                    var depositResult = await toNFTProvider.DepositNFTAsync(
                        request.TokenAddress ?? request.FromNFTTokenAddress, // Would be destination chain NFT contract
                        request.TokenId, // May be different on destination if wrapped
                        request.ToWalletAddress,
                        withdrawResult.Result.TransactionId // Source transaction hash for verification
                    );

                    if (depositResult.IsError || depositResult.Result == null)
                    {
                        // Rollback: Unlock NFT on source chain
                        var unlockRequest = new UnlockWeb3NFTRequest
                        {
                            NFTTokenAddress = request.TokenAddress ?? request.FromNFTTokenAddress,
                            Web3NFTId = Guid.TryParse(request.TokenId, out var guid) ? guid : Guid.NewGuid(),
                            UnlockedByAvatarId = avatarId
                        };

                        var unlockResult = await fromNFTProvider.UnlockNFTAsync(unlockRequest);
                        if (unlockResult != null && !unlockResult.IsError)
                        {
                            result.Result.UnlockTransactionResult = unlockResult.Result.TransactionResult;
                            result.Message = $"NFT deposit failed, but NFT was successfully unlocked on source chain. Deposit error: {depositResult.Message}";
                        }
                        else
                        {
                            result.Message = $"CRITICAL: NFT deposit failed AND unlock failed. Deposit error: {depositResult.Message}. Unlock error: {unlockResult?.Message}";
                        }
                        result.IsError = true;
                        return result;
                    }

                    result.Result.SendTransactionResult = depositResult.Result.TransactionId;
                    result.Message = "Cross-chain NFT transfer completed successfully";
                    result.IsError = false;
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"{errorMessage} Exception during cross-chain NFT transfer: {ex.Message}", ex);
                }
            }

            return result;
        }

        public OASISResult<ITransactionResponse> SendToken(Guid avatarId, ISendWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error Occured in SendToken function. Reason: ";

            if (string.IsNullOrEmpty(request.FromWalletAddress))
            {
                //Try and lookup the wallet address from the avatar id/username/email if one of those is provided.
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

                if (avatarId != Guid.Empty)
                    walletsResult = LoadProviderWalletsForAvatarById(avatarId, false, false, false, request.FromProvider.Value);

                //if (request.FromAvatarId != Guid.Empty)
                //    walletsResult = LoadProviderWalletsForAvatarById(request.FromAvatarId, providerTypeToLoadFrom: request.FromProvider.Value);

                //else if (!string.IsNullOrEmpty(request.FromAvatarUsername))
                //    walletsResult = LoadProviderWalletsForAvatarByUsername(request.FromAvatarUsername, providerTypeToLoadFrom: request.FromProvider.Value);

                //else if (!string.IsNullOrEmpty(request.FromAvatarEmail))
                //    walletsResult = LoadProviderWalletsForAvatarByEmail(request.FromAvatarEmail, providerTypeToLoadFrom: request.FromProvider.Value);

                //else
                //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} You must provide at least one of the following to identify the sender: FromWalletAddress, FromAvatarId, FromAvatarUsername or FromAvatarEmail.");

                if (!walletsResult.IsError && walletsResult.Result != null && walletsResult.Result.ContainsKey(request.FromProvider.Value) && walletsResult.Result[request.FromProvider.Value] != null)
                {
                    IProviderWallet wallet = walletsResult.Result[request.FromProvider.Value].FirstOrDefault();

                    if (wallet != null)
                        request.FromWalletAddress = wallet.WalletAddress;
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.FromProvider.Name} so the transaction cannot be sent.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.FromProvider.Name} so the transaction cannot be sent. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }

            if (string.IsNullOrEmpty(request.ToWalletAddress))
            {
                //Try and lookup the wallet address from the avatar id/username/email if one of those is provided.
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
                if (request.ToAvatarId != Guid.Empty)

                    walletsResult = LoadProviderWalletsForAvatarById(request.ToAvatarId, providerTypeToLoadFrom: request.ToProvider.Value);

                else if (!string.IsNullOrEmpty(request.ToAvatarUsername))
                    walletsResult = LoadProviderWalletsForAvatarByUsername(request.ToAvatarUsername, providerTypeToLoadFrom: request.ToProvider.Value);

                else if (!string.IsNullOrEmpty(request.ToAvatarEmail))
                    walletsResult = LoadProviderWalletsForAvatarByEmail(request.ToAvatarEmail, providerTypeToLoadFrom: request.ToProvider.Value);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} You must provide at least one of the following to identify the receiver: ToWalletAddress, ToAvatarId, ToAvatarUsername or ToAvatarEmail.");

                if (!walletsResult.IsError && walletsResult.Result != null && walletsResult.Result.ContainsKey(request.ToProvider.Value) && walletsResult.Result[request.ToProvider.Value] != null)
                {
                    IProviderWallet wallet = walletsResult.Result[request.ToProvider.Value].FirstOrDefault();

                    if (wallet != null)
                        request.ToWalletAddress = wallet.WalletAddress;
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.ToProvider.Name} so the transaction cannot be sent.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar could not be found or does not have a wallet for provider {request.ToProvider.Name} so the transaction cannot be sent. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} The FromProviderType {Enum.GetName(typeof(ProviderType), request.FromProvider)} is not a OASIS Blockchain  Provider. Please make sure you sepcify a OASIS Blockchain Provider.");


            if (result.IsError)
                return result;

            if (request.FromProvider.Name == request.ToProvider.Name)
            {
                IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.FromProvider.Value) as IOASISBlockchainStorageProvider;

                if (oasisBlockchainProvider != null)
                {
                    bool attemptingToSend = true;
                    DateTime startTime = DateTime.Now;

                    SendWeb3TokenRequest web3Request = new SendWeb3TokenRequest()
                    {
                         Amount = request.Amount,
                         //FromProvider = request.FromProvider,
                         FromWalletAddress = request.FromWalletAddress,
                         MemoText = request.MemoText,
                         //ToProvider = request.ToProvider,
                         ToWalletAddress = request.ToWalletAddress
                    };

                    do
                    {
                        result = oasisBlockchainProvider.SendToken(web3Request);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            attemptingToSend = false;
                            result.Message = "Token Sent Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenSent)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to send the token & WaitTillTokenSent is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToSendTokenEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.WaitForTokenToSendInSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to send the token. Reason: Timeout expired, WaitForTokenToSendInSeconds ({request.WaitForTokenToSendInSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }

                    } while (attemptingToSend);
                }
            }
            else
            {
                // Cross-chain transfer: Use BridgeManager for atomic swaps (synchronous wrapper)
                try
                {
                    // Get token symbols from provider types
                    var fromToken = GetTokenSymbolForProvider(request.FromProvider.Value);
                    var toToken = GetTokenSymbolForProvider(request.ToProvider.Value);

                    if (string.IsNullOrEmpty(fromToken) || string.IsNullOrEmpty(toToken))
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            $"{errorMessage} Unable to determine token symbols for providers {request.FromProvider.Name} and {request.ToProvider.Name}. Cross-chain transfers require valid blockchain providers.");
                        return result;
                    }

                    // Get BridgeManager instance
                    var bridgeManager = BridgeManager.Instance;

                    // Create bridge order request
                    var bridgeOrderRequest = new NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs.CreateBridgeOrderRequest
                    {
                        FromToken = fromToken,
                        ToToken = toToken,
                        Amount = request.Amount,
                        FromAddress = request.FromWalletAddress,
                        DestinationAddress = request.ToWalletAddress,
                        UserId = avatarId,
                        ExpiresInMinutes = 30
                    };

                    // Execute cross-chain bridge order (atomic swap) - synchronous wrapper
                    var bridgeResult = bridgeManager.CreateBridgeOrderAsync(bridgeOrderRequest).Result;

                    if (bridgeResult != null && !bridgeResult.IsError && bridgeResult.Result != null)
                    {
                        result.Message = $"Cross-chain token transfer initiated successfully. Bridge Order ID: {bridgeResult.Result.OrderId}";
                        result.Result.TransactionResult = bridgeResult.Result.OrderId.ToString();
                        result.IsError = false;
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, 
                            $"{errorMessage} Cross-chain bridge operation failed. Reason: {bridgeResult?.Message ?? "Unknown error"}");
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        $"{errorMessage} Exception during cross-chain transfer: {ex.Message}", ex);
                }
            }

            return result;
        }


        private async Task<OASISResult<ITransactionResponse>> SendTokenInternalAsync(ISendWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error occured in SendTokenInternalAsync. Reason: ";

            SendWeb3TokenRequest web3Request = new SendWeb3TokenRequest()
            {
                Amount = request.Amount,
                //FromProvider = request.FromProvider,
                FromWalletAddress = request.FromWalletAddress,
                MemoText = request.MemoText,
                //ToProvider = request.ToProvider,
                ToWalletAddress = request.ToWalletAddress
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.FromProvider.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = await oasisBlockchainProvider.SendTokenAsync(web3Request);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Sent Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenSent)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to send the token & WaitTillTokenSent is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToSendTokenEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToSendTokenEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to send the token. Reason: Timeout expired, AttemptToSendTokenEveryXSeconds ({request.AttemptToSendTokenEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.FromProvider.Name} calling ProviderManager.Instance.GetProvider.");

            return result;
        }

        private OASISResult<ITransactionResponse> SendTokenInternal(ISendWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error occured in SendTokenInternal. Reason: ";

            SendWeb3TokenRequest web3Request = new SendWeb3TokenRequest()
            {
                Amount = request.Amount,
                //FromProvider = request.FromProvider,
                FromWalletAddress = request.FromWalletAddress,
                MemoText = request.MemoText,
                //ToProvider = request.ToProvider,
                ToWalletAddress = request.ToWalletAddress
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.FromProvider.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = oasisBlockchainProvider.SendToken(web3Request);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Sent Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenSent)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to send the token & WaitTillTokenSent is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToSendTokenEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToSendTokenEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to send the token. Reason: Timeout expired, AttemptToSendTokenEveryXSeconds ({request.AttemptToSendTokenEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.FromProvider.Name} calling ProviderManager.Instance.GetProvider.");

            return result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error occured in BurnTokenAsync. Reason: ";

            BurnWeb3TokenRequest burnWeb3TokenRequest = new BurnWeb3TokenRequest()
            {
                TokenAddress = request.TokenAddress,
                Web3TokenId = request.Web3TokenId,
                OwnerPrivateKey = request.OwnerPrivateKey,
                OwnerPublicKey = request.OwnerPublicKey,
                OwnerSeedPhrase = request.OwnerSeedPhrase
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.ProviderType.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = await oasisBlockchainProvider.BurnTokenAsync(burnWeb3TokenRequest);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Burnt Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenBurnt)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to burn the token & WaitTillTokenBurnt is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToBurnEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToBurnEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to burn the token. Reason: Timeout expired, AttemptToBurnEveryXSeconds ({request.AttemptToBurnEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.ProviderType.Name} calling ProviderManager.Instance.GetProvider.");

            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error occured in BurnToken. Reason: ";

            BurnWeb3TokenRequest burnWeb3TokenRequest = new BurnWeb3TokenRequest()
            {
                TokenAddress = request.TokenAddress,
                Web3TokenId = request.Web3TokenId,
                OwnerPrivateKey = request.OwnerPrivateKey,
                OwnerPublicKey = request.OwnerPublicKey,
                OwnerSeedPhrase = request.OwnerSeedPhrase
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.ProviderType.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = oasisBlockchainProvider.BurnToken(burnWeb3TokenRequest);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Burnt Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenBurnt)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to burn the token & WaitTillTokenBurnt is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToBurnEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToBurnEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to burn the token. Reason: Timeout expired, AttemptToBurnEveryXSeconds ({request.AttemptToBurnEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.ProviderType.Name} calling ProviderManager.Instance.GetProvider.");

            return result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error occured in LockTokenAsync. Reason: ";

            LockWeb3TokenRequest lockWeb3TokenRequest = new LockWeb3TokenRequest()
            {
                TokenAddress = request.TokenAddress,
                Web3TokenId = request.Web3TokenId
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.ProviderType.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = await oasisBlockchainProvider.LockTokenAsync(lockWeb3TokenRequest);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Locked Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenLocked)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to lock the token & WaitTillTokenLocked is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToLockEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToLockEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to lock the token. Reason: Timeout expired, AttemptToLockEveryXSeconds ({request.AttemptToLockEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.ProviderType.Name} calling ProviderManager.Instance.GetProvider.");

            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error occured in LockToken. Reason: ";

            LockWeb3TokenRequest lockWeb3TokenRequest = new LockWeb3TokenRequest()
            {
                TokenAddress = request.TokenAddress,
                Web3TokenId = request.Web3TokenId
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.ProviderType.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = oasisBlockchainProvider.LockToken(lockWeb3TokenRequest);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Locked Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenLocked)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to lock the token & WaitTillTokenLocked is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToLockEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToLockEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to lock the token. Reason: Timeout expired, AttemptToLockEveryXSeconds ({request.AttemptToLockEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.ProviderType.Name} calling ProviderManager.Instance.GetProvider.");

            return result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error occured in UnlockTokenAsync. Reason: ";

            UnlockWeb3TokenRequest unlockWeb3TokenRequest = new UnlockWeb3TokenRequest()
            {
                TokenAddress = request.TokenAddress,
                Web3TokenId = request.Web3TokenId
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.ProviderType.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = await oasisBlockchainProvider.UnlockTokenAsync(unlockWeb3TokenRequest);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Unlocked Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenUnlocked)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to unlock the token & WaitTillTokenUnlocked is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToUnlockEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToUnlockEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to unlock the token. Reason: Timeout expired, AttemptToUnlockEveryXSeconds ({request.AttemptToUnlockEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.ProviderType.Name} callingProviderManager.Instance.GetProvider.");

            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb4TokenRequest request)
        {
            OASISResult<ITransactionResponse> result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error occured in UnlockToken. Reason: ";

            UnlockWeb3TokenRequest unlockWeb3TokenRequest = new UnlockWeb3TokenRequest()
            {
                TokenAddress = request.TokenAddress,
                Web3TokenId = request.Web3TokenId
            };

            IOASISBlockchainStorageProvider oasisBlockchainProvider = ProviderManager.Instance.GetProvider(request.ProviderType.Value) as IOASISBlockchainStorageProvider;

            if (oasisBlockchainProvider != null)
            {
                DateTime startTime = DateTime.Now;

                do
                {
                    try
                    {
                        result = oasisBlockchainProvider.UnlockToken(unlockWeb3TokenRequest);

                        if (result != null && result.Result != null && !result.IsError)
                        {
                            result.Message = "Token Unlocked Successfully";
                            break;
                        }
                        else if (!request.WaitTillTokenUnlocked)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to unlock the token & WaitTillTokenUnlocked is false. Reason: {result.Message}");
                            break;
                        }

                        Thread.Sleep(request.AttemptToUnlockEveryXSeconds * 1000);

                        if (startTime.AddSeconds(request.AttemptToUnlockEveryXSeconds).Ticks < DateTime.Now.Ticks)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to unlock the token. Reason: Timeout expired, AttemptToUnlockEveryXSeconds ({request.AttemptToUnlockEveryXSeconds}) exceeded, try increasing and trying again!");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown error occured. Reason: {e}");
                        break;
                    }
                } while (true);
            }
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured getting provider {request.ProviderType.Name} calling ProviderManager.Instance.GetProvider.");

            return result;
        }

        public async Task<OASISResult<double>> GetTotalBalanceForAllProviderWalletsForAvatarByIdAsync(Guid avatarId)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessage = "Error occured in GetTotalBalanceForAllProviderWalletsForAvatarByIdAsync method in WalletManager. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByIdAsync(avatarId, false, false);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        foreach (IProviderWallet providerWallet in providerWallets.Result[provider])
                            result.Result += providerWallet.Balance;
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<double> GetTotalBalanceForAllProviderWalletsForAvatarById(Guid avatarId)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessage = "Error occured in GetTotalBalanceForAllProviderWalletsForAvatarById method in WalletManager. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarById(avatarId);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        foreach (IProviderWallet providerWallet in providerWallets.Result[provider])
                            result.Result += providerWallet.Balance;
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<double>> GetTotalBalanceForAllProviderWalletsForAvatarByUsernameAsync(string username)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessage = "Error occured in GetTotalBalanceForAllProviderWalletsForAvatarByUsernameAsync method in WalletManager. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByUsernameAsync(username);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        foreach (IProviderWallet providerWallet in providerWallets.Result[provider])
                            result.Result += providerWallet.Balance;
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<double> GetTotalBalanceForAllProviderWalletsForAvatarByUsername(string username)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessage = "Error occured in GetTotalBalanceForAllProviderWalletsForAvatarByUsername method in WalletManager. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByUsername(username);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        foreach (IProviderWallet providerWallet in providerWallets.Result[provider])
                            result.Result += providerWallet.Balance;
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<double>> GetTotalBalanceForAllProviderWalletsForAvatarByEmailAsync(string email)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessage = "Error occured in GetTotalBalanceForAllProviderWalletsForAvatarByEmailAsync method in WalletManager. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByEmailAsync(email);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        foreach (IProviderWallet providerWallet in providerWallets.Result[provider])
                            result.Result += providerWallet.Balance;
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<double> GetTotalBalanceForAllProviderWalletsForAvatarByEmail(string email)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessage = "Error occured in GetTotalBalanceForAllProviderWalletsForAvatarByEmail method in WalletManager. Reason: ";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByEmail(email);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        foreach (IProviderWallet providerWallet in providerWallets.Result[provider])
                            result.Result += providerWallet.Balance;
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<double>> GetTotalBalanceForProviderWalletsForAvatarByIdAsync(Guid avatarId, ProviderType walletProviderType)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetTotalBalanceForProviderWalletsForAvatarByIdAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, walletProviderType);

            try
            {
                OASISResult<List<IProviderWallet>> providerWallets = await LoadProviderWalletsForProviderByAvatarIdAsync(avatarId, walletProviderType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (IProviderWallet providerWallet in providerWallets.Result)
                        result.Result += providerWallet.Balance;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<double> GetTotalBalanceForProviderWalletsForAvatarById(Guid avatarId, ProviderType walletProviderType)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetTotalBalanceForProviderWalletsForAvatarById method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, walletProviderType);

            try
            {
                OASISResult<List<IProviderWallet>> providerWallets = LoadProviderWalletsForProviderByAvatarId(avatarId, walletProviderType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (IProviderWallet providerWallet in providerWallets.Result)
                        result.Result += providerWallet.Balance;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<double>> GetTotalBalanceForProviderWalletsForAvatarByUsernameAsync(string username, ProviderType walletProviderType)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetTotalBalanceForProviderWalletsForAvatarByUsernameAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, walletProviderType);

            try
            {
                OASISResult<List<IProviderWallet>> providerWallets = await LoadProviderWalletsForProviderByAvatarUsernameAsync(username, walletProviderType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (IProviderWallet providerWallet in providerWallets.Result)
                        result.Result += providerWallet.Balance;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<double> GetTotalBalanceForProviderWalletsForAvatarByUsername(string username, ProviderType walletProviderType)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetTotalBalanceForProviderWalletsForAvatarByUsername method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, walletProviderType);

            try
            {
                OASISResult<List<IProviderWallet>> providerWallets = LoadProviderWalletsForProviderByAvatarUsername(username, walletProviderType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (IProviderWallet providerWallet in providerWallets.Result)
                        result.Result += providerWallet.Balance;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<double>> GetTotalBalanceForProviderWalletsForAvatarByEmailAsync(string email, ProviderType walletProviderType)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetTotalBalanceForProviderWalletsForAvatarByEmailAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, walletProviderType);

            try
            {
                OASISResult<List<IProviderWallet>> providerWallets = await LoadProviderWalletsForProviderByAvatarEmailAsync(email, walletProviderType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (IProviderWallet providerWallet in providerWallets.Result)
                        result.Result += providerWallet.Balance;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<double> GetTotalBalanceForProviderWalletsForAvatarByEmail(string email, ProviderType walletProviderType)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetTotalBalanceForProviderWalletsForAvatarByEmail method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, walletProviderType);

            try
            {
                OASISResult<List<IProviderWallet>> providerWallets = LoadProviderWalletsForProviderByAvatarEmail(email, walletProviderType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (IProviderWallet providerWallet in providerWallets.Result)
                        result.Result += providerWallet.Balance;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<double>> GetBalanceForWalletForAvatarByIdAsync(Guid avatarId, Guid walletId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetBalanceForWalletForAvatarByIdAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IProviderWallet> providerWallet = await LoadProviderWalletForAvatarByIdAsync(avatarId, walletId, false, false, providerType);

                if (providerWallet != null && providerWallet.Result != null && !providerWallet.IsError)
                    result.Result = providerWallet.Result.Balance;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallet.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<double>> GetBalanceForWalletForAvatarById(Guid avatarId, Guid walletId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetBalanceForWalletForAvatarById method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IProviderWallet> providerWallet = LoadProviderWalletForAvatarById(avatarId, walletId, false, false, providerType);

                if (providerWallet != null && providerWallet.Result != null && !providerWallet.IsError)
                    result.Result = providerWallet.Result.Balance;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallet.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<double>> GetBalanceForWalletForAvatarByUsernameAsync(string username, Guid walletId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetBalanceForWalletForAvatarByUsernameAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IProviderWallet> providerWallet = await LoadProviderWalletForAvatarByUsernameAsync(username, walletId, providerType: providerType);

                if (providerWallet != null && providerWallet.Result != null && !providerWallet.IsError)
                    result.Result = providerWallet.Result.Balance;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallet.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<double> GetBalanceForWalletForAvatarByUsername(string username, Guid walletId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetBalanceForWalletForAvatarByUsernameAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IProviderWallet> providerWallet = LoadProviderWalletForAvatarByUsername(username, walletId, false, false, providerType);

                if (providerWallet != null && providerWallet.Result != null && !providerWallet.IsError)
                    result.Result = providerWallet.Result.Balance;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallet.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<double>> GetBalanceForWalletForAvatarByEmailAsync(string email, Guid walletId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetBalanceForWalletForAvatarByEmailAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IProviderWallet> providerWallet = await LoadProviderWalletForAvatarByEmailAsync(email, walletId, providerType: providerType);

                if (providerWallet != null && providerWallet.Result != null && !providerWallet.IsError)
                    result.Result = providerWallet.Result.Balance;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallet.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<double>> GetBalanceForWalletForAvatarByEmail(string email, Guid walletId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<double> result = new OASISResult<double>();
            string errorMessageTemplate = "Error occured in GetBalanceForWalletForAvatarByEmailAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IProviderWallet> providerWallet = LoadProviderWalletForAvatarByEmail(email, walletId, false, false, providerType);

                if (providerWallet != null && providerWallet.Result != null && !providerWallet.IsError)
                    result.Result = providerWallet.Result.Balance;
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallet.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> LoadProviderWalletForAvatarByIdAsync(Guid avatarId, Guid walletId, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByIdAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByIdAsync(avatarId, false, showPrivateKeys, showSecretWords, providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                        {
                            OASISResult<IProviderWallet> walletResult = ProcessDecryption(result.Result, showPrivateKeys, showSecretWords, avatarId, providerType);

                            if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                            {
                                result.Result = walletResult.Result;
                                result.IsLoaded = true;
                                result.IsError = false;
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<IProviderWallet> LoadProviderWalletForAvatarById(Guid avatarId, Guid walletId, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByIdAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarById(avatarId, providerTypeToLoadFrom: providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                        {
                            OASISResult<IProviderWallet> walletResult = ProcessDecryption(result.Result, showPrivateKeys, showSecretWords, avatarId, providerType);

                            if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                            {
                                result.Result = walletResult.Result;
                                result.IsLoaded = true;
                                result.IsError = false;
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> LoadProviderWalletForAvatarByUsernameAsync(string username, Guid walletId, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByUsernameAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByUsernameAsync(username, false, showPrivateKeys, showSecretWords, providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                        {
                            //TODO: Check that avatarId isnt needed here (hopefully privatekey should already be loaded from the local provider!)
                            OASISResult<IProviderWallet> walletResult = ProcessDecryption(result.Result, showPrivateKeys, showSecretWords, Guid.Empty, providerType);

                            if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                            {
                                result.Result = walletResult.Result;
                                result.IsLoaded = true;
                                result.IsError = false;
                            }
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<IProviderWallet> LoadProviderWalletForAvatarByUsername(string username, Guid walletId, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByUsernameAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByUsername(username, providerTypeToLoadFrom: providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                        {
                            ////TODO: Check that avatarId isnt needed here (hopefully privatekey should already be loaded from the local provider!)
                            //OASISResult<IProviderWallet> walletResult = ProcessDecryption(result.Result, showPrivateKeys, showSecretWords, Guid.Empty, providerType);

                            //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                            //{
                            //    result.Result = walletResult.Result;
                            //    result.IsLoaded = true;
                            //    result.IsError = false;
                            //}
                            //else
                            //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> LoadProviderWalletForAvatarByEmailAsync(string email, Guid walletId, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByEmailAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByEmailAsync(email, false, showPrivateKeys, showSecretWords, providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                        {
                            //TODO: Check that avatarId isnt needed here (hopefully privatekey should already be loaded from the local provider!)
                            //OASISResult<IProviderWallet> walletResult = ProcessDecryption(result.Result, showPrivateKeys, showSecretWords, Guid.Empty, providerType);

                            //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                            //{
                            //    result.Result = walletResult.Result;
                            //    result.IsLoaded = true;
                            //    result.IsError = false;
                            //}
                            //else
                            //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<IProviderWallet> LoadProviderWalletForAvatarByEmail(string email, Guid walletId, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByEmail method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = LoadProviderWalletsForAvatarByEmail(email, providerTypeToLoadFrom: providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                        {
                            //TODO: Check that avatarId isnt needed here (hopefully privatekey should already be loaded from the local provider!)
                            //OASISResult<IProviderWallet> walletResult = ProcessDecryption(result.Result, showPrivateKeys, showSecretWords, Guid.Empty, providerType);

                            //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                            //{
                            //    result.Result = walletResult.Result;
                            //    result.IsLoaded = true;
                            //    result.IsError = false;
                            //}
                            //else
                            //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");

                            break;
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}{providerWallets.Message}");

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByIdAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                providerTypeToLoadFrom = ProviderType.LocalFileOASIS; //TODO: Temp!

                CLIEngine.SupressConsoleLogging = true;
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerTypeToLoadFrom);
                CLIEngine.SupressConsoleLogging = false;

                errorMessage = string.Format(errorMessageTemplate, ProviderManager.Instance.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    //if (providerResult.Result.ProviderCategory.Value == ProviderCategory.StorageLocal || providerResult.Result.ProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork)
                    result = ((IOASISLocalStorageProvider)providerResult.Result).LoadProviderWalletsForAvatarById(id);
                    //else
                    //    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}The providerType ProviderCategory must be either StorageLocal or StorageLocalAndNetwork.");

                    if (result != null && result.Result != null && !result.IsError)
                        result.Result = FilterWallets(result.Result, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor);
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Error occured loading wallets calling LoadProviderWalletsForAvatarById. Reason: "), result.Message);
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Error occured setting the provider. Reason: ", providerResult.Message), providerResult.Message);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarById method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                providerTypeToLoadFrom = ProviderType.LocalFileOASIS; //TODO: Temp!
                CLIEngine.SupressConsoleLogging = true;
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerTypeToLoadFrom);
                errorMessage = string.Format(errorMessageTemplate, ProviderManager.Instance.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    //if (providerResult.Result.ProviderCategory.Value == ProviderCategory.StorageLocal || providerResult.Result.ProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork)
                    result = ((IOASISLocalStorageProvider)providerResult.Result).LoadProviderWalletsForAvatarById(id);
                    //else
                    //    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}The providerType ProviderCategory must be either StorageLocal or StorageLocalAndNetwork.");

                    if (result != null && result.Result != null && !result.IsError)
                        result.Result = FilterWallets(result.Result, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor);
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Error occured loading wallets calling LoadProviderWalletsForAvatarById. Reason: "), result.Message);
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Error occured setting the provider. Reason: ", providerResult.Message), providerResult.Message);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameAsync(string username, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByUsernameAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(username, false, true, providerTypeToLoadFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = await LoadProviderWalletsForAvatarByIdAsync(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor, providerTypeToLoadFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load for provider {providerTypeToLoadFrom}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsername(string username, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByUsername method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(username, false, true, providerTypeToLoadFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor, providerTypeToLoadFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load for provider {providerTypeToLoadFrom}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailAsync(string email, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByEmailAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarByEmailAsync(email, false, true, providerTypeToLoadFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = await LoadProviderWalletsForAvatarByIdAsync(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor, providerTypeToLoadFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load for provider {providerTypeToLoadFrom}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByEmail(string email, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByEmail method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByEmail(email, false, true, providerTypeToLoadFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor, providerTypeToLoadFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load for provider {providerTypeToLoadFrom}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }


        public async Task<OASISResult<List<IProviderWallet>>> LoadProviderWalletsForProviderByAvatarIdAsync(Guid avatarId, ProviderType walletProviderType, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = await LoadProviderWalletsForAvatarByIdAsync(avatarId, showOnlyDefault, showPrivateKeys);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForProviderByAvatarIdAsync. Reason: {result.Result}");

            return result;
        }

        public OASISResult<List<IProviderWallet>> LoadProviderWalletsForProviderByAvatarId(Guid avatarId, ProviderType walletProviderType, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = LoadProviderWalletsForAvatarById(avatarId);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForProviderByAvatarId. Reason: {result.Result}");

            return result;
        }

        public async Task<OASISResult<List<IProviderWallet>>> LoadProviderWalletsForProviderByAvatarUsernameAsync(string username, ProviderType walletProviderType, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = await LoadProviderWalletsForAvatarByUsernameAsync(username);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForProviderByAvatarUsernameAsync. Reason: {result.Result}");

            return result;
        }

        public OASISResult<List<IProviderWallet>> LoadProviderWalletsForProviderByAvatarUsername(string username, ProviderType walletProviderType, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = LoadProviderWalletsForAvatarByUsername(username);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForProviderByAvatarUsername. Reason: {result.Result}");

            return result;
        }

        public async Task<OASISResult<List<IProviderWallet>>> LoadProviderWalletsForProviderByAvatarEmailAsync(string email, ProviderType walletProviderType, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = await LoadProviderWalletsForAvatarByEmailAsync(email);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForAvatarByEmailAsync. Reason: {result.Result}");

            return result;
        }

        public OASISResult<List<IProviderWallet>> LoadProviderWalletsForProviderByAvatarEmail(string email, ProviderType walletProviderType, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = LoadProviderWalletsForAvatarByEmail(email);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForAvatarByEmail. Reason: {result.Result}");

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdUsingHyperDriveAsync(Guid id, bool showOnlyDefault = false, bool showPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result =
                new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByIdAsync(id, showOnlyDefault, showPrivateKeys, providerTypeToLoadFrom: type.Value);
                result.Result = walletsResult.Result;

                if (!walletsResult.IsError && walletsResult.Result != null)
                    break;
                else
                    OASISErrorHandling.HandleWarning(ref result, $"Error occured in LoadProviderWalletsForAvatarByIdUsingHyperDriveAsync in WalletManager loading wallets for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }

            if (result.Result == null || result.IsError)
                OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load wallets for avatar with id ", id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            else
            {
                result.IsLoaded = true;

                if (result.WarningCount > 0)
                    OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with id ", id, " loaded it's wallets successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsingHyperDriveId(Guid id, bool showOnlyDefault = false, bool showPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result =
                new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarById(id, showOnlyDefault, showPrivateKeys, providerTypeToLoadFrom: type.Value);
                result.Result = walletsResult.Result;

                if (!walletsResult.IsError && walletsResult.Result != null)
                    break;
                else
                    OASISErrorHandling.HandleWarning(ref result, $"Error occured in LoadProviderWalletsForAvatarByUsingHyperDriveId in WalletManager loading wallets for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }

            if (result.Result == null || result.IsError)
                OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to load wallets for avatar with id ", id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            else
            {
                result.IsLoaded = true;

                if (result.WarningCount > 0)
                    OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar with id ", id, " loaded it's wallets successfully for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to load for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsernameUsingHyperDrive(string username, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in LoadProviderWalletsForAvatarByUsernameUsingHyperDrive method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(username, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameUsingHyperDriveAsync(string username, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false,  ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in LoadProviderWalletsForAvatarByUsernameUsingHyperDriveAsync method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(username, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByEmailUsingHyperDrive(string email, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in LoadProviderWalletsForAvatarByEmailUsingHyperDrive method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByEmail(email, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailUsingHyperDriveAsync(string email, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in LoadProviderWalletsForAvatarByEmailUsingHyperDriveAsync method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarByEmailAsync(email, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

  
        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessageTemplate = "Error in SaveProviderWalletsForAvatarById method in WalletManager saving wallets for provider {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                providerType = ProviderType.LocalFileOASIS; //TODO: TEMP!
                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerType);
                errorMessage = string.Format(errorMessageTemplate, ProviderManager.Instance.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    //Make sure private keys are ONLY stored locally.
                    if (ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocal || ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork)
                    {
                        //TODO: Was going to load the private keys from the local storage and then restore any missing private keys before saving (in case they had been removed before saving to a non-local storage provider) but then there will be no way of knowing if the keys have been removed by the user (if they were then this would then incorrectly restore them again!).
                        //Commented out code was an alternative to saving the private keys seperatley as the next block below does...
                        //(result, IAvatar originalAvatar) = OASISResultHelper<IAvatar, IAvatar>.UnWrapOASISResult(ref result, LoadAvatar(avatar.Id, true, providerType), String.Concat(errorMessage, "Error loading avatar. Reason: {0}"));

                        //if (!result.IsError)
                        //{

                        //}


                        //We need to save the wallets (with private keys) seperatley to the local storage provider otherwise the next time a non local provider replicates to local it will overwrite the wallets and private keys (will be blank).
                        //TODO: The PrivateKeys are already encrypted but I want to add an extra layer of protection to encrypt the full wallet! ;-)
                        //TODO: Soon will also add a 3rd level of protection by quantum encrypting the keys/wallets... :)

                        var walletsTask = Task.Run(() => ((IOASISLocalStorageProvider)providerResult.Result).SaveProviderWalletsForAvatarById(id, wallets));

                        if (walletsTask.Wait(TimeSpan.FromSeconds(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)))
                        {
                            if (walletsTask.Result.IsError || !walletsTask.Result.Result)
                            {
                                if (string.IsNullOrEmpty(walletsTask.Result.Message))
                                    walletsTask.Result.Message = "Unknown error occured saving provider wallets.";

                                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, walletsTask.Result.Message), walletsTask.Result.DetailedMessage);
                            }
                            else
                            {
                                result.Result = true;
                                result.IsSaved = true;
                            }
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "timeout occured saving provider wallets."));
                    }
                    //else
                    //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The providerType ProviderCategory must be either StorageLocal or StorageLocalAndNetwork.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"An error occured setting the provider {providerType}. Reason: ", providerResult.Message), providerResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessageTemplate = "Error in SaveProviderWalletsForAvatarByIdAsync method in WalletManager saving wallets for provider {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                providerType = ProviderType.LocalFileOASIS; //TODO:Temp!

                CLIEngine.SupressConsoleLogging = true;
                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
                CLIEngine.SupressConsoleLogging = false;

                errorMessage = string.Format(errorMessageTemplate, ProviderManager.Instance.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    //Make sure private keys are ONLY stored locally.
                    //if (ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocal || ProviderManager.Instance.CurrentStorageProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork)
                    //{
                        //TODO: Was going to load the private keys from the local storage and then restore any missing private keys before saving (in case they had been removed before saving to a non-local storage provider) but then there will be no way of knowing if the keys have been removed by the user (if they were then this would then incorrectly restore them again!).
                        //Commented out code was an alternative to saving the private keys seperatley as the next block below does...
                        //(result, IAvatar originalAvatar) = OASISResultHelper<IAvatar, IAvatar>.UnWrapOASISResult(ref result, LoadAvatar(avatar.Id, true, providerType), String.Concat(errorMessage, "Error loading avatar. Reason: {0}"));

                        //if (!result.IsError)
                        //{

                        //}


                        //We need to save the wallets (with private keys) seperatley to the local storage provider otherwise the next time a non local provider replicates to local it will overwrite the wallets and private keys (will be blank).
                        //TODO: The PrivateKeys are already encrypted but I want to add an extra layer of protection to encrypt the full wallet! ;-)
                        //TODO: Soon will also add a 3rd level of protection by quantum encrypting the keys/wallets... :)

                        var walletsTask = ((IOASISLocalStorageProvider)providerResult.Result).SaveProviderWalletsForAvatarByIdAsync(id, wallets);

                        if (await Task.WhenAny(walletsTask, Task.Delay(OASISDNA.OASIS.StorageProviders.ProviderMethodCallTimeOutSeconds * 1000)) == walletsTask)
                        {
                            if (walletsTask.Result.IsError || !walletsTask.Result.Result)
                            {
                                if (string.IsNullOrEmpty(walletsTask.Result.Message))
                                    walletsTask.Result.Message = "Unknown error occured saving provider wallets.";

                                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, walletsTask.Result.Message), walletsTask.Result.DetailedMessage);
                            }
                            else
                            {
                                result.Result = true;
                                result.IsSaved = true;
                            }
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "timeout occured saving provider wallets."));
                    //}
                   // else
                    //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The providerType ProviderCategory must be either StorageLocal or StorageLocalAndNetwork.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"An error occured setting the provider {providerType}. Reason: ", providerResult.Message), providerResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarByUsername(string username, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessageTemplate = "Error in SaveProviderWalletsForAvatarByUsername method in WalletManager saving wallets for provider {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(username, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = SaveProviderWalletsForAvatarById(avatarResult.Result.Id, wallets, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load for provider {providerType}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByUsernameAsync(string username, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessageTemplate = "Error in SaveProviderWalletsForAvatarByUsernameAsync method in WalletManager saving wallets for provider {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(username, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = SaveProviderWalletsForAvatarById(avatarResult.Result.Id, wallets, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load for provider {providerType}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarByEmail(string email, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessageTemplate = "Error in SaveProviderWalletsForAvatarByEmail method in WalletManager saving wallets for provider {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByEmail(email, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = SaveProviderWalletsForAvatarById(avatarResult.Result.Id, wallets, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load for provider {providerType}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByEmailAsync(string email, Dictionary<ProviderType, List<IProviderWallet>> wallets, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessageTemplate = "Error in SaveProviderWalletsForAvatarByEmailAsync method in WalletManager saving wallets for provider {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarByEmailAsync(email, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = SaveProviderWalletsForAvatarById(avatarResult.Result.Id, wallets, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load for provider {providerType}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }


        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> wallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            //TODO: TEMP!
            OASISResult<bool> walletsResult = SaveProviderWalletsForAvatarById(id, wallets, ProviderType.LocalFileOASIS);
            
            if (walletsResult != null && walletsResult.Result != null)
                result.Result = walletsResult.Result;


            //TODO: May add local storage providers to their own list? To save looping through lots of non-local ones or is this not really needed? :)
            //foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
            //{
            //    OASISResult<bool> walletsResult = SaveProviderWalletsForAvatarById(id, wallets, type.Value);
            //    result.Result = walletsResult.Result;

            //    if (!walletsResult.IsError && walletsResult.Result)
            //    {
            //        previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            //        break;
            //    }
            //    else
            //        OASISErrorHandling.HandleWarning(ref result, $"Error occured in SaveProviderWalletsForAvatarById in WalletManager saving wallets for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            //}

            if (!result.Result || result.IsError)
                OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save wallets for avatar with id ", id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            else
            {
                result.IsSaved = true;

                if (result.WarningCount > 0)
                    OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar wallets ", id, " successfully saved for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                    result.Message = "Avatar Wallets Successfully Saved.";

                //TODO: Need to move into background thread ASAP!
                //TODO: Even if all providers failed above, we should still attempt again in a background thread for a fixed number of attempts (default 3) every X seconds (default 5) configured in OASISDNA.json.
                //TODO: Auto-Failover should also re-try in a background thread after reporting the intial error above and then report after the retries either failed or succeeded later...
                //if (ProviderManager.Instance.IsAutoReplicationEnabled)
                //{
                //    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                //    {
                //        if (type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                //        {
                //            OASISResult<bool> walletsResult = SaveProviderWalletsForAvatarById(id, wallets, type.Value);
                //            result.Result = walletsResult.Result;

                //            if (walletsResult.IsError || !walletsResult.Result)
                //                OASISErrorHandling.HandleWarning(ref result, $"Error occured in LoadProviderWalletsForAvatarById in WalletManager saving wallets for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
                //        }
                //    }

                //    if (result.WarningCount > 0)
                //        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar wallets ", id, " successfully saved for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                //    else
                //        LoggingManager.Log("Avatar Wallets Successfully Saved/Replicated", LogType.Info, ref result, true, false);
                //}
            }

            ProviderManager.Instance.SetAndActivateCurrentStorageProvider(currentProviderType);
            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> wallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            ProviderType currentProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
            ProviderType previousProviderType = ProviderType.Default;

            //TODO: May add local storage providers to their own list? To save looping through lots of non-local ones or is this not really needed? :)
            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
            {
                OASISResult<bool> walletsResult = await SaveProviderWalletsForAvatarByIdAsync(id, wallets, type.Value);
                result.Result = walletsResult.Result;

                if (!walletsResult.IsError && walletsResult.Result)
                {
                    previousProviderType = ProviderManager.Instance.CurrentStorageProviderType.Value;
                    break;
                }
                else
                    OASISErrorHandling.HandleWarning(ref result, $"Error occured in SaveProviderWalletsForAvatarById in WalletManager saving wallets for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }

            if (!result.Result || result.IsError)
                OASISErrorHandling.HandleError(ref result, String.Concat("All registered OASIS Providers in the AutoFailOverList failed to save wallets for avatar with id ", id, ". Please view the logs or DetailedMessage property for more information. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Details: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
            else
            {
                result.IsSaved = true;

                if (result.WarningCount > 0)
                    OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar wallets ", id, " successfully saved for the provider ", ProviderManager.Instance.CurrentStorageProviderType.Value, " but failed to save for some of the other providers in the AutoFailOverList. Providers in the list are: ", ProviderManager.Instance.GetProviderAutoFailOverListAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)));
                else
                    result.Message = "Avatar Wallets Successfully Saved.";

                //TODO: Need to move into background thread ASAP!
                //TODO: Even if all providers failed above, we should still attempt again in a background thread for a fixed number of attempts (default 3) every X seconds (default 5) configured in OASISDNA.json.
                //TODO: Auto-Failover should also re-try in a background thread after reporting the intial error above and then report after the retries either failed or succeeded later...
                if (ProviderManager.Instance.IsAutoReplicationEnabled)
                {
                    foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProvidersThatAreAutoReplicating())
                    {
                        if (type.Value != ProviderManager.Instance.CurrentStorageProviderType.Value)
                        {
                            OASISResult<bool> walletsResult = await SaveProviderWalletsForAvatarByIdAsync(id, wallets, type.Value);
                            result.Result = walletsResult.Result;

                            if (walletsResult.IsError || !walletsResult.Result)
                                OASISErrorHandling.HandleWarning(ref result, $"Error occured in SaveProviderWalletsForAvatarByIdAsync in WalletManager saving wallets for provider {type.Name}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
                        }
                    }

                    if (result.WarningCount > 0)
                        OASISErrorHandling.HandleWarning(ref result, string.Concat("The avatar wallets ", id, " successfully saved for the provider ", previousProviderType, " but failed to auto-replicate for some of the other providers in the Auto-Replicate List. Providers in the list are: ", ProviderManager.Instance.GetProvidersThatAreAutoReplicatingAsString()), string.Concat("Error Message: ", OASISResultHelper.BuildInnerMessageError(result.InnerMessages)), true);
                    else
                        LoggingManager.Log("Avatar Wallets Successfully Saved/Replicated", LogType.Info, ref result, true, false);
                }
            }

            CLIEngine.SupressConsoleLogging = true;
            await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
            CLIEngine.SupressConsoleLogging = false;

            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarByUsername(string username, Dictionary<ProviderType, List<IProviderWallet>> wallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in SaveProviderWalletsForAvatarByUsername method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(username, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = SaveProviderWalletsForAvatarById(avatarResult.Result.Id, wallets);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByUsernameAsync(string username, Dictionary<ProviderType, List<IProviderWallet>> wallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in SaveProviderWalletsForAvatarByUsernameAsync method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(username, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = await SaveProviderWalletsForAvatarByIdAsync(avatarResult.Result.Id, wallets);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarByEmail(string email, Dictionary<ProviderType, List<IProviderWallet>> wallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in SaveProviderWalletsForAvatarByEmail method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(email, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = SaveProviderWalletsForAvatarById(avatarResult.Result.Id, wallets);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByEmailAsync(string email, Dictionary<ProviderType, List<IProviderWallet>> wallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in SaveProviderWalletsForAvatarByEmail method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(email, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = await SaveProviderWalletsForAvatarByIdAsync(avatarResult.Result.Id, wallets);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public Dictionary<ProviderType, List<IProviderWallet>> CopyProviderWallets(Dictionary<ProviderType, List<IProviderWallet>> wallets)
        {
            Dictionary<ProviderType, List<IProviderWallet>> walletsCopy = new Dictionary<ProviderType, List<IProviderWallet>>();

            foreach (ProviderType pType in wallets.Keys)
            {
                foreach (IProviderWallet wallet in wallets[pType])
                {
                    if (!walletsCopy.ContainsKey(pType))
                        walletsCopy[pType] = new List<IProviderWallet>();

                    walletsCopy[pType].Add(new ProviderWallet()
                    {
                        PublicKey = wallet.PublicKey,
                        PrivateKey = wallet.PrivateKey,
                        WalletAddress = wallet.WalletAddress,
                        Id = wallet.Id,
                        CreatedByAvatarId = wallet.CreatedByAvatarId,
                        CreatedDate = wallet.CreatedDate,
                        ModifiedByAvatarId = wallet.ModifiedByAvatarId,
                        ModifiedDate = wallet.ModifiedDate,
                        Version = wallet.Version
                    });
                }
            }

            return walletsCopy;
        }


        public OASISResult<IProviderWallet> GetWalletThatPublicKeyBelongsTo(string providerKey, bool showPrivateKey = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            OASISResult<IEnumerable<IAvatar>> avatarsResult = AvatarManager.Instance.LoadAllAvatars();

            if (!avatarsResult.IsError && avatarsResult.Result != null)
            {
                foreach (IAvatar avatar in avatarsResult.Result)
                {
                    result = GetWalletThatPublicKeyBelongsTo(providerKey, providerType, avatar, showPrivateKey, showSecretWords);

                    if (result.Result != null)
                        break;
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetWalletThatPublicKeyBelongsTo whilst loading avatars. Reason:{avatarsResult.Message}", avatarsResult.DetailedMessage);

            return result;
        }

        public OASISResult<IProviderWallet> GetWalletThatPublicKeyBelongsTo(string providerKey, bool showPrivateKey = false, bool showSecretWords = false)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            OASISResult<IEnumerable<IAvatar>> avatarsResult = AvatarManager.Instance.LoadAllAvatars();

            if (!avatarsResult.IsError && avatarsResult.Result != null)
            {
                foreach (IAvatar avatar in avatarsResult.Result)
                {
                    result = GetWalletThatPublicKeyBelongsTo(providerKey, avatar);

                    if (result.Result != null)
                        break;
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetWalletThatPublicKeyBelongsTo whilst loading avatars. Reason:{avatarsResult.Message}", avatarsResult.DetailedMessage);

            return result;
        }

        public OASISResult<IProviderWallet> GetWalletThatPublicKeyBelongsTo(string providerKey, ProviderType providerType, IAvatar avatar, bool showPrivateKey = false, bool showSecretWords = false)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            foreach (IProviderWallet wallet in avatar.ProviderWallets[providerType])
            {
                if (wallet.PublicKey == providerKey)
                {
                    OASISResult<IProviderWallet> walletResult = ProcessDecryption(wallet, showPrivateKey, showSecretWords, avatar.Id);

                    if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                        result.Result = walletResult.Result;
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in GetWalletThatPublicKeyBelongsTo whilst processing decryption for avatar {avatar.Id} and wallet {wallet.Id}. Reason: {walletResult.Message}", walletResult.DetailedMessage);

                    result.Message = "Wallet Found";
                    break;
                }
            }

            return result;
        }

        public OASISResult<IProviderWallet> GetWalletThatPublicKeyBelongsTo(string providerKey, IAvatar avatar, bool showPrivateKey = false, bool showSecretWords = false)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            foreach (ProviderType providerType in avatar.ProviderWallets.Keys)
            {
                foreach (IProviderWallet wallet in avatar.ProviderWallets[providerType])
                {
                    if (wallet.PublicKey == providerKey)
                    {
                        OASISResult<IProviderWallet> walletResult = ProcessDecryption(wallet, showPrivateKey, showSecretWords, avatar.Id);

                        if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                            result.Result = walletResult.Result;
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured in GetWalletThatPublicKeyBelongsTo whilst processing decryption for avatar {avatar.Id} and wallet {wallet.Id}. Reason: {walletResult.Message}", walletResult.DetailedMessage);

                        result.Message = "Wallet Found";
                        return result;
                    }
                }
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ExportWalletByIdAsync(Guid avatarId, Guid walletId, string fullPathToExportTo, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = await LoadProviderWalletForAvatarByIdAsync(avatarId, walletId, showPrivateKeys, showSecretWords, providerTypeToLoadFrom);
            string errorMessage = "Error occured in ExportWalletByIdAsync. Reason:";

            try
            {
                if (result != null && result.Result != null && !result.IsError)
                    File.WriteAllText(fullPathToExportTo, JsonConvert.SerializeObject(result.Result));
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> ExportWalletById(Guid avatarId, Guid walletId, string fullPathToExportTo, bool showPrivateKeys = false, bool showSecretWords = false,  ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = LoadProviderWalletForAvatarById(avatarId, walletId, showPrivateKeys, showSecretWords, providerTypeToLoadFrom);
            string errorMessage = "Error occured in ExportWalletById. Reason:";

            try
            {
                if (result != null && result.Result != null && !result.IsError)
                    File.WriteAllText(fullPathToExportTo, JsonConvert.SerializeObject(result.Result));
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ExportWalletByUsernameAsync(string username, Guid walletId, string fullPathToExportTo, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = await LoadProviderWalletForAvatarByUsernameAsync(username, walletId, showPrivateKeys, showSecretWords, providerTypeToLoadFrom);
            string errorMessage = "Error occured in ExportWalletByUsernameAsync. Reason:";

            try
            {
                if (result != null && result.Result != null && !result.IsError)
                    File.WriteAllText(fullPathToExportTo, JsonConvert.SerializeObject(result.Result));
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> ExportWalletByUsername(string username, Guid walletId, string fullPathToExportTo, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = LoadProviderWalletForAvatarByUsername(username, walletId, showPrivateKeys, showSecretWords, providerTypeToLoadFrom);
            string errorMessage = "Error occured in ExportWalletByUsername. Reason:";

            try
            {
                if (result != null && result.Result != null && !result.IsError)
                    File.WriteAllText(fullPathToExportTo, JsonConvert.SerializeObject(result.Result));
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ExportWalletByEmailAsync(string email, Guid walletId, string fullPathToExportTo, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = await LoadProviderWalletForAvatarByUsernameAsync(email, walletId, showPrivateKeys, showSecretWords, providerTypeToLoadFrom);
            string errorMessage = "Error occured in ExportWalletByEmailAsync. Reason:";

            try
            {
                if (result != null && result.Result != null && !result.IsError)
                    File.WriteAllText(fullPathToExportTo, JsonConvert.SerializeObject(result.Result));
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> ExportWalletByEmail(string email, Guid walletId, string fullPathToExportTo, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = LoadProviderWalletForAvatarByEmail(email, walletId, showPrivateKeys, showSecretWords, providerTypeToLoadFrom);
            string errorMessage = "Error occured in ExportWalletByEmail. Reason:";

            try
            {
                if (result != null && result.Result != null && !result.IsError)
                    File.WriteAllText(fullPathToExportTo, JsonConvert.SerializeObject(result.Result));
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> ExportAllWalletsByIdAsync(Guid avatarId, string fullPathToExportTo, bool exportOnlyDefault = false, bool showPrivateKeys = false, ProviderType providerTypeToExportWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = await LoadProviderWalletsForAvatarByIdAsync(avatarId, showPrivateKeys: showPrivateKeys, showOnlyDefault: exportOnlyDefault, providerTypeToShowWalletsFor: providerTypeToExportWalletsFor, providerTypeToLoadFrom: providerTypeToLoadFrom);
            string errorMessage = "Error occured in ExportAllWalletsByIdAsync. Reason:";

            try
            {
                if (result != null && result.Result != null && !result.IsError)
                    File.WriteAllText(fullPathToExportTo, JsonConvert.SerializeObject(result.Result));
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> ExportAllWalletsById(Guid avatarId, string fullPathToExportTo, bool exportOnlyDefault = false, bool showPrivateKeys = false, ProviderType providerTypeToExportWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = LoadProviderWalletsForAvatarById(avatarId, showPrivateKeys: showPrivateKeys, showOnlyDefault: exportOnlyDefault, providerTypeToShowWalletsFor: providerTypeToExportWalletsFor, providerTypeToLoadFrom: providerTypeToLoadFrom);
            string errorMessage = "Error occured in ExportAllWalletsById. Reason:";

            try
            {
                if (result != null && result.Result != null && !result.IsError)
                    File.WriteAllText(fullPathToExportTo, JsonConvert.SerializeObject(result.Result));
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> ExportAllWalletsByUsernameAsync(string username, string fullPathToExportTo, bool exportOnlyDefault = false, bool showPrivateKeys = false, ProviderType providerTypeToExportWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = await LoadProviderWalletsForAvatarByUsernameAsync(username, showPrivateKeys: showPrivateKeys, showOnlyDefault: exportOnlyDefault, providerTypeToShowWalletsFor: providerTypeToExportWalletsFor, providerTypeToLoadFrom: providerTypeToLoadFrom);
            string errorMessage = "Error occured in ExportAllWalletsByUsernameAsync. Reason:";

            try
            {
                if (result != null && result.Result != null && !result.IsError)
                    File.WriteAllText(fullPathToExportTo, JsonConvert.SerializeObject(result.Result));
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> ExportAllWalletsByUsername(string username, string fullPathToExportTo, bool exportOnlyDefault = false, bool showPrivateKeys = false, ProviderType providerTypeToExportWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = LoadProviderWalletsForAvatarByUsername(username, showPrivateKeys: showPrivateKeys, showOnlyDefault: exportOnlyDefault, providerTypeToShowWalletsFor: providerTypeToExportWalletsFor, providerTypeToLoadFrom: providerTypeToLoadFrom);
            string errorMessage = "Error occured in ExportAllWalletsByUsername. Reason:";

            try
            {
                if (result != null && result.Result != null && !result.IsError)
                    File.WriteAllText(fullPathToExportTo, JsonConvert.SerializeObject(result.Result));
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {result.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingSecretPhaseByIdAsync(Guid avatarId, string phase, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingSecretPhaseByIdAsync. Reason:";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByIdAsync(avatarId, providerTypeToLoadFrom: providerTypeToLoadFrom);

                if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                {
                    foreach (ProviderType providerType in walletsResult.Result.Keys)
                    {
                        result.Result = walletsResult.Result[providerType].FirstOrDefault(x => x.SecretRecoveryPhrase == phase);

                        if (result.Result != null)
                            break;
                    }

                    if (result.Result == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for the secrert recovery phase.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            //TODO: Finish implementing... (allow user to import a wallet using the secret recovering phase (memonic words).
            //Can derive the public key and private key from the phase (need to look into how to do this...)

            //TODO: Need to look into how others do this... because the code above just finds the existing wallet matching the phase!
            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingSecretPhaseById(Guid avatarId, string phase, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingSecretPhaseById. Reason:";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarById(avatarId, providerTypeToLoadFrom: providerTypeToLoadFrom);

                if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                {
                    foreach (ProviderType providerType in walletsResult.Result.Keys)
                    {
                        result.Result = walletsResult.Result[providerType].FirstOrDefault(x => x.SecretRecoveryPhrase == phase);

                        if (result.Result != null)
                            break;
                    }

                    if (result.Result == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for the secrert recovery phase.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            //TODO: Finish implementing... (allow user to import a wallet using the secret recovering phase (memonic words).
            //Can derive the public key and private key from the phase (need to look into how to do this...)

            //TODO: Need to look into how others do this... because the code above just finds the existing wallet matching the phase!
            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingSecretPhaseByUsernameAsync(string username, string phase, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingSecretPhaseByUsernameAsync. Reason:";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByUsernameAsync(username, providerTypeToLoadFrom: providerTypeToLoadFrom);

                if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                {
                    foreach (ProviderType providerType in walletsResult.Result.Keys)
                    {
                        result.Result = walletsResult.Result[providerType].FirstOrDefault(x => x.SecretRecoveryPhrase == phase);

                        if (result.Result != null)
                            break;
                    }

                    if (result.Result == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for the secrert recovery phase.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            //TODO: Finish implementing... (allow user to import a wallet using the secret recovering phase (memonic words).
            //Can derive the public key and private key from the phase (need to look into how to do this...)

            //TODO: Need to look into how others do this... because the code above just finds the existing wallet matching the phase!
            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingSecretPhaseByUsername(string username, string phase, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingSecretPhaseByUsername. Reason:";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarByUsername(username, providerTypeToLoadFrom: providerTypeToLoadFrom);

                if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                {
                    foreach (ProviderType providerType in walletsResult.Result.Keys)
                    {
                        result.Result = walletsResult.Result[providerType].FirstOrDefault(x => x.SecretRecoveryPhrase == phase);

                        if (result.Result != null)
                            break;
                    }

                    if (result.Result == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for the secrert recovery phase.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            //TODO: Finish implementing... (allow user to import a wallet using the secret recovering phase (memonic words).
            //Can derive the public key and private key from the phase (need to look into how to do this...)

            //TODO: Need to look into how others do this... because the code above just finds the existing wallet matching the phase!
            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingSecretPhaseByEmailAsync(string email, string phase, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingSecretPhaseByEmailAsync. Reason:";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByEmailAsync(email, providerTypeToLoadFrom: providerTypeToLoadFrom);

                if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                {
                    foreach (ProviderType providerType in walletsResult.Result.Keys)
                    {
                        result.Result = walletsResult.Result[providerType].FirstOrDefault(x => x.SecretRecoveryPhrase == phase);

                        if (result.Result != null)
                            break;
                    }

                    if (result.Result == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for the secrert recovery phase.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            //TODO: Finish implementing... (allow user to import a wallet using the secret recovering phase (memonic words).
            //Can derive the public key and private key from the phase (need to look into how to do this...)

            //TODO: Need to look into how others do this... because the code above just finds the existing wallet matching the phase!
            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingSecretPhaseByEmail(string email, string phase, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingSecretPhaseByEmail. Reason:";

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarByEmail(email, providerTypeToLoadFrom: providerTypeToLoadFrom);

                if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                {
                    foreach (ProviderType providerType in walletsResult.Result.Keys)
                    {
                        result.Result = walletsResult.Result[providerType].FirstOrDefault(x => x.SecretRecoveryPhrase == phase);

                        if (result.Result != null)
                            break;
                    }

                    if (result.Result == null)
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} No wallet was found for the secrert recovery phase.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {walletsResult.Message}");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e.Message}");
            }

            //TODO: Finish implementing... (allow user to import a wallet using the secret recovering phase (memonic words).
            //Can derive the public key and private key from the phase (need to look into how to do this...)

            //TODO: Need to look into how others do this... because the code above just finds the existing wallet matching the phase!
            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingJSONFileByIdAsync(Guid avatarId, string pathToJSONFile)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingJSONFile. Reason: ";

            try
            {
                result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByIdAsync(avatarId);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
                            walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

                        if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
                        {
                            walletsResult.Result[result.Result.ProviderType].Add(result.Result);

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByIdAsync(avatarId, walletsResult.Result);

                            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                result.Message = "Wallet Imported Successfully";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByIdAsync. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByIdAsync. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingJSONFileById(Guid avatarId, string pathToJSONFile)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingJSONFileById. Reason: ";

            try
            {
                result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarById(avatarId);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
                            walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

                        if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
                        {
                            walletsResult.Result[result.Result.ProviderType].Add(result.Result);

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarById(avatarId, walletsResult.Result);

                            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                result.Message = "Wallet Imported Successfully";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarById. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarById. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingJSONFileByUsernameAsync(string username, string pathToJSONFile)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingJSONFileByUsernameAsync. Reason: ";

            try
            {
                result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByUsernameAsync(username);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
                            walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

                        if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
                        {
                            walletsResult.Result[result.Result.ProviderType].Add(result.Result);

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByUsernameAsync(username, walletsResult.Result);

                            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                result.Message = "Wallet Imported Successfully";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByUsernameAsync. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByUsernameAsync. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingJSONFileByUsername(string username, string pathToJSONFile)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingJSONFileByUsername. Reason: ";

            try
            {
                result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarByUsername(username);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
                            walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

                        if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
                        {
                            walletsResult.Result[result.Result.ProviderType].Add(result.Result);

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByUsername(username, walletsResult.Result);

                            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                result.Message = "Wallet Imported Successfully";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByUsername. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByUsername. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingJSONFileByEmailAsync(string email, string pathToJSONFile)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingJSONFileByEmailAsync. Reason: ";

            try
            {
                result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByEmailAsync(email);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
                            walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

                        if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
                        {
                            walletsResult.Result[result.Result.ProviderType].Add(result.Result);

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByEmailAsync(email, walletsResult.Result);

                            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                result.Message = "Wallet Imported Successfully";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByEmailAsync. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByEmailAsync. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingJSONFileByEmail(string email, string pathToJSONFile)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ImportWalletUsingJSONFileByEmail. Reason: ";

            try
            {
                result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));

                if (result.Result != null)
                {
                    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarByEmail(email);

                    if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                    {
                        if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
                            walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

                        if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
                        {
                            walletsResult.Result[result.Result.ProviderType].Add(result.Result);

                            OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByEmail(email, walletsResult.Result);

                            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                                result.Message = "Wallet Imported Successfully";
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByEmail. Reason: {saveResult.Message}");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByEmail. Reason: {walletsResult.Message}");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
            }

            return result;
        }

        //TODO: Finish later! ;-)
        //public async Task<OASISResult<IProviderWallet>> ImportWalletUsingJSONFileByIdAsync(Guid avatarId, string pathToJSONFile)
        //{
        //    OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
        //    string errorMessage = "Error occured in ImportWalletUsingJSONFile. Reason: ";

        //    try
        //    {
        //        result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));

        //        if (result.Result != null)
        //        {
        //            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByIdAsync(avatarId);

        //            if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
        //            {
        //                if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
        //                    walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

        //                if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
        //                {
        //                    walletsResult.Result[result.Result.ProviderType].Add(result.Result);

        //                    OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByIdAsync(avatarId, walletsResult.Result);

        //                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
        //                        result.Message = "Wallet Imported Successfully";
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByIdAsync. Reason: {saveResult.Message}");
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByIdAsync. Reason: {walletsResult.Message}");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
        //    }

        //    return result;
        //}

        //public OASISResult<IProviderWallet> ImportWalletUsingJSONFileById(Guid avatarId, string pathToJSONFile)
        //{
        //    OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
        //    string errorMessage = "Error occured in ImportWalletUsingJSONFileById. Reason: ";

        //    try
        //    {
        //        result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));

        //        if (result.Result != null)
        //        {
        //            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarById(avatarId);

        //            if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
        //            {
        //                if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
        //                    walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

        //                if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
        //                {
        //                    walletsResult.Result[result.Result.ProviderType].Add(result.Result);

        //                    OASISResult<bool> saveResult = SaveProviderWalletsForAvatarById(avatarId, walletsResult.Result);

        //                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
        //                        result.Message = "Wallet Imported Successfully";
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarById. Reason: {saveResult.Message}");
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarById. Reason: {walletsResult.Message}");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IProviderWallet>> ImportWalletUsingJSONFileByUsernameAsync(string username, string pathToJSONFile)
        //{
        //    OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
        //    string errorMessage = "Error occured in ImportWalletUsingJSONFileByUsernameAsync. Reason: ";

        //    try
        //    {
        //        result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));

        //        if (result.Result != null)
        //        {
        //            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByUsernameAsync(username);

        //            if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
        //            {
        //                if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
        //                    walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

        //                if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
        //                {
        //                    walletsResult.Result[result.Result.ProviderType].Add(result.Result);

        //                    OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByUsernameAsync(username, walletsResult.Result);

        //                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
        //                        result.Message = "Wallet Imported Successfully";
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByUsernameAsync. Reason: {saveResult.Message}");
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByUsernameAsync. Reason: {walletsResult.Message}");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
        //    }

        //    return result;
        //}

        //public OASISResult<IProviderWallet> ImportWalletUsingJSONFileByUsername(string username, string pathToJSONFile)
        //{
        //    OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
        //    string errorMessage = "Error occured in ImportWalletUsingJSONFileByUsername. Reason: ";

        //    try
        //    {
        //        result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));

        //        if (result.Result != null)
        //        {
        //            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarByUsername(username);

        //            if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
        //            {
        //                if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
        //                    walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

        //                if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
        //                {
        //                    walletsResult.Result[result.Result.ProviderType].Add(result.Result);

        //                    OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByUsername(username, walletsResult.Result);

        //                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
        //                        result.Message = "Wallet Imported Successfully";
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByUsername. Reason: {saveResult.Message}");
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByUsername. Reason: {walletsResult.Message}");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IProviderWallet>> ImportAllWalletsUsingJSONFileByEmailAsync(string email, string pathToJSONFile)
        //{
        //    OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
        //    string errorMessage = "Error occured in ImportWalletUsingJSONFileByEmailAsync. Reason: ";

        //    try
        //    {
        //        result.Result = JsonConvert.DeserializeObject<IProviderWallet>(File.ReadAllText(pathToJSONFile));

        //        if (result.Result != null)
        //        {
        //            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByEmailAsync(email);

        //            if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
        //            {
        //                if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
        //                    walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

        //                if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
        //                {
        //                    walletsResult.Result[result.Result.ProviderType].Add(result.Result);

        //                    OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByEmailAsync(email, walletsResult.Result);

        //                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
        //                        result.Message = "Wallet Imported Successfully";
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByEmailAsync. Reason: {saveResult.Message}");
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByEmailAsync. Reason: {walletsResult.Message}");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
        //    }

        //    return result;
        //}

        //public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> ImportAllWalletsUsingJSONFileByEmail(string email, string pathToJSONFile)
        //{
        //    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
        //    string errorMessage = "Error occured in ImportAllWalletsUsingJSONFileByEmail. Reason: ";

        //    try
        //    {
        //        result.Result = JsonConvert.DeserializeObject<Dictionary<ProviderType, List<IProviderWallet>>>(File.ReadAllText(pathToJSONFile));

        //        if (result.Result != null)
        //        {
        //            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarByEmail(email);

        //            if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
        //            {
        //                if (!walletsResult.Result.ContainsKey(result.Result.ProviderType))
        //                    walletsResult.Result[result.Result.ProviderType] = new List<IProviderWallet>();

        //                if (!walletsResult.Result[result.Result.ProviderType].Any(x => x.Id == result.Result.Id))
        //                {
        //                    walletsResult.Result[result.Result.ProviderType].Add(result.Result);

        //                    OASISResult<bool> saveResult = SaveProviderWalletsForAvatarByEmail(email, walletsResult.Result);

        //                    if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
        //                        result.Message = "Wallet Imported Successfully";
        //                    else
        //                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured saving the wallets calling SaveProviderWalletsForAvatarByEmail. Reason: {saveResult.Message}");
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The wallet with id {result.Result.Id} and name '{result.Result.Name}' for provider type {Enum.GetName(typeof(ProviderType), result.Result.ProviderType)} already exists so it cannot be imported again!");
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error loading wallets calling LoadProviderWalletsForAvatarByEmail. Reason: {walletsResult.Message}");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} {e}");
        //    }

        //    return result;
        //}

        public OASISResult<IProviderWallet> ImportWalletUsingPrivateKeyById(Guid avatarId, string key, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPrivateKeyToAvatarById(Guid.Empty, avatarId, providerToImportTo, key);
        }

        public OASISResult<IProviderWallet> ImportWalletUsingPrivateKeyByUsername(string username, string key, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPrivateKeyToAvatarByUsername(Guid.Empty, username, providerToImportTo, key);
        }

        public OASISResult<IProviderWallet> ImportWalletUsingPrivateKeyByEmail(string email, string key, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPrivateKeyToAvatarByUsername(Guid.Empty, email, providerToImportTo, key);
        }

        public OASISResult<IProviderWallet> ImportWalletUsingPublicKeyById(Guid avatarId, string key, string walletAddress, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPublicKeyToAvatarById(Guid.Empty, avatarId, providerToImportTo, key, walletAddress);
        }

        public OASISResult<IProviderWallet> ImportWalletUsingPublicKeyByUsername(string username, string key, string walletAddress, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPublicKeyToAvatarByUsername(Guid.Empty, username, providerToImportTo, key, walletAddress);
        }

        public OASISResult<IProviderWallet> ImportWalletUsingPublicKeyByEmail(string email, string key, string walletAddress, ProviderType providerToImportTo)
        {
            return KeyManager.Instance.LinkProviderPublicKeyToAvatarByEmail(Guid.Empty, email, providerToImportTo, key, walletAddress);
        }

        public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByIdAsync(Guid avatarId, ProviderType providerType, bool showOnlyDefaultWallet = false, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in GetAvatarDefaultWalletById method in WalletManager. Reason: ";

            try
            {
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByIdAsync(avatarId, showOnlyDefaultWallet, showPrivateKeys, showSecretWords, providerType);
                if (allAvatarWalletsByProvider.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallets failed to load. Reason: {allAvatarWalletsByProvider.Message}", allAvatarWalletsByProvider.DetailedMessage);
                }
                else
                {
                    var defaultAvatarWallet = allAvatarWalletsByProvider.Result[providerType].FirstOrDefault(x => x.IsDefaultWallet);
                    if (defaultAvatarWallet == null)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}Avatar doesn't have a default wallet!");
                    }
                    else
                    {
                        result.Result = defaultAvatarWallet;
                        result.IsLoaded = true;
                        result.IsError = false;

                        //OASISResult<IProviderWallet> walletResult = ProcessDecryption(defaultAvatarWallet, showPrivateKeys, showSecretWords, avatarId, providerType);

                        //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                        //{
                        //    result.Result = walletResult.Result;
                        //    result.IsLoaded = true;
                        //    result.IsError = false;
                        //}
                        //else
                        //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByUsernameAsync(string avatarUsername, bool showOnlyDefaultWallet = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in GetAvatarDefaultWalletByUsername method in WalletManager. Reason: ";

            try
            {
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByUsernameAsync(avatarUsername, showOnlyDefaultWallet, showPrivateKeys, showSecretWords, providerType);
                if (allAvatarWalletsByProvider.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallets failed to load. Reason: {allAvatarWalletsByProvider.Message}", allAvatarWalletsByProvider.DetailedMessage);
                }
                else
                {
                    var defaultAvatarWallet = allAvatarWalletsByProvider.Result[providerType].FirstOrDefault(x => x.IsDefaultWallet);
                    if (defaultAvatarWallet == null)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}Avatar doesn't have a default wallet!");
                    }
                    else
                    {
                        result.Result = defaultAvatarWallet;
                        result.IsLoaded = true;
                        result.IsError = false;

                        ////TODO: Check that avatarId isnt needed here (hopefully privatekey should already be loaded from the local provider!)
                        //OASISResult<IProviderWallet> walletResult = ProcessDecryption(defaultAvatarWallet, showPrivateKeys, showSecretWords, Guid.Empty, providerType);

                        //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                        //{
                        //    result.Result = walletResult.Result;
                        //    result.IsLoaded = true;
                        //    result.IsError = false;
                        //}
                        //else
                        //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");
                    }   
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByEmailAsync(string email, ProviderType providerType, bool showOnlyDefaultWallet = false, bool showPrivateKeys = false, bool showSecretWords = false)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in GetAvatarDefaultWalletByEmail method in WalletManager. Reason: ";

            try
            {
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByEmailAsync(email, showOnlyDefaultWallet, showPrivateKeys, showSecretWords, providerType);
                if (allAvatarWalletsByProvider.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallets failed to load. Reason: {allAvatarWalletsByProvider.Message}", allAvatarWalletsByProvider.DetailedMessage);
                }
                else
                {
                    var defaultAvatarWallet = allAvatarWalletsByProvider.Result[providerType].FirstOrDefault(x => x.IsDefaultWallet);
                    if (defaultAvatarWallet == null)
                    {
                        OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}Avatar doesn't have a default wallet!");
                    }
                    else
                    {
                        result.Result = defaultAvatarWallet;
                        result.IsLoaded = true;
                        result.IsError = false;

                        //TODO: Check that avatarId isnt needed here (hopefully privatekey should already be loaded from the local provider!)
                        //OASISResult<IProviderWallet> walletResult = ProcessDecryption(defaultAvatarWallet, showPrivateKeys, showSecretWords, Guid.Empty, providerType);

                        //if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                        //{
                        //    result.Result = walletResult.Result;
                        //    result.IsLoaded = true;
                        //    result.IsError = false;
                        //}
                        //else
                        //    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Error occured calling ProcessDecryption. Reason: {walletResult.Message}");
                    }   
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> SetAvatarDefaultWalletByIdAsync(Guid avatarId, Guid walletId, ProviderType providerType)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in SetAvatarDefaultWalletById method in WalletManager. Reason: ";

            try
            {
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByIdAsync(avatarId);

                if (allAvatarWalletsByProvider.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallets failed to load. Reason: {allAvatarWalletsByProvider.Message}", allAvatarWalletsByProvider.DetailedMessage);
                }
                else
                {
                    var avatarWallet = allAvatarWalletsByProvider.Result[providerType].FirstOrDefault(x => x.WalletId == walletId);

                    if (avatarWallet == null)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallet with id {walletId} Not found!");
                    }
                    else
                    {
                        if (avatarWallet.IsDefaultWallet)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallet with id {walletId} is already the Default Wallet!");
                        else
                        {
                            foreach (IProviderWallet wallet in allAvatarWalletsByProvider.Result[providerType])
                                wallet.IsDefaultWallet = false;

                            avatarWallet.IsDefaultWallet = true;

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByIdAsync(avatarId, allAvatarWalletsByProvider.Result, providerType);

                            if (saveResult != null && saveResult.Result)
                                result.Result = avatarWallet;

                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> SetAvatarDefaultWalletByUsernameAsync(string avatarUsername, Guid walletId, ProviderType providerType)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in SetAvatarDefaultWalletByUsername method in WalletManager. Reason: ";

            try
            {
                //var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByUsernameAsync(avatarUsername, false, false, providerType);
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByUsernameAsync(avatarUsername, false, false);
                if (allAvatarWalletsByProvider.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallets failed to load. Reason: {allAvatarWalletsByProvider.Message}", allAvatarWalletsByProvider.DetailedMessage);
                }
                else
                {
                    var avatarWallet = allAvatarWalletsByProvider.Result[providerType].FirstOrDefault(x => x.WalletId == walletId);

                    if (avatarWallet == null)
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallet with id {walletId} Not found!");
                    }
                    else
                    {
                        if (avatarWallet.IsDefaultWallet)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallet with id {walletId} is already the Default Wallet!");
                        else
                        {
                            foreach (IProviderWallet wallet in allAvatarWalletsByProvider.Result[providerType])
                                wallet.IsDefaultWallet = false;

                            avatarWallet.IsDefaultWallet = true;

                            OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByUsernameAsync(avatarUsername, allAvatarWalletsByProvider.Result, providerType);

                            if (saveResult != null && saveResult.Result)
                                result.Result = avatarWallet;

                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> SetAvatarDefaultWalletByEmailAsync(string email, Guid walletId, ProviderType providerType)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in SetAvatarDefaultWalletByEmail method in WalletManager. Reason: ";

            try
            {
                //var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByEmailAsync(email, false, false, providerType);
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByEmailAsync(email, false, false);

                if (allAvatarWalletsByProvider.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallets failed to load. Reason: {allAvatarWalletsByProvider.Message}", allAvatarWalletsByProvider.DetailedMessage);
                }
                else
                {
                    if (allAvatarWalletsByProvider.Result[providerType].Any(x => x.IsDefaultWallet))
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar already have default wallet!");
                    }
                    else
                    {
                        var avatarWallet = allAvatarWalletsByProvider.Result[providerType].FirstOrDefault(x => x.WalletId == walletId);

                        if (avatarWallet == null)
                        {
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallet with id {walletId} Not found!");
                        }
                        else
                        {
                            if (avatarWallet.IsDefaultWallet)
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage}Avatar wallet with id {walletId} is already the Default Wallet!");
                            else
                            {
                                foreach (IProviderWallet wallet in allAvatarWalletsByProvider.Result[providerType])
                                    wallet.IsDefaultWallet = false;

                                avatarWallet.IsDefaultWallet = true;

                                OASISResult<bool> saveResult = await SaveProviderWalletsForAvatarByEmailAsync(email, allAvatarWalletsByProvider.Result, providerType);

                                if (saveResult != null && saveResult.Result)
                                    result.Result = avatarWallet;

                                OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(saveResult, result);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        private Dictionary<ProviderType, List<IProviderWallet>> FilterWallets(Dictionary<ProviderType, List<IProviderWallet>> wallets, bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            if (providerTypeToShowWalletsFor != ProviderType.All)
            {
                Dictionary<ProviderType, List<IProviderWallet>> newWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                newWallets[providerTypeToShowWalletsFor] = wallets[providerTypeToShowWalletsFor];
                wallets = newWallets;
            }

            if (showPrivateKeys)
            {
                foreach (ProviderType provider in wallets.Keys)
                {
                    foreach (IProviderWallet wallet in wallets[provider])
                    {
                        if (wallet.PrivateKey != null)
                            wallet.PrivateKey = Rijndael.Decrypt(wallet.PrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                    }
                }
            }

            if (showSecretWords)
            {
                foreach (ProviderType provider in wallets.Keys)
                {
                    foreach (IProviderWallet wallet in wallets[provider])
                    {
                        if (wallet.SecretRecoveryPhrase != null)
                            wallet.SecretRecoveryPhrase = Rijndael.Decrypt(wallet.SecretRecoveryPhrase, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                    }
                }
            }

            if (showOnlyDefault)
            {
                Dictionary<ProviderType, List<IProviderWallet>> newWallets = new Dictionary<ProviderType, List<IProviderWallet>>();

                foreach (ProviderType provider in wallets.Keys)
                {
                    if (!newWallets.ContainsKey(provider))
                        newWallets[provider] = new List<IProviderWallet>();

                    foreach (IProviderWallet wallet in wallets[provider])
                    {
                        if (wallet.IsDefaultWallet)
                            newWallets[provider].Add(wallet);
                    }
                }

                wallets = newWallets;
            }

            return wallets;
        }

        /// <summary>
        /// Maps ProviderType to token symbol for bridge operations
        /// </summary>
        private string GetTokenSymbolForProvider(ProviderType providerType)
        {
            return providerType switch
            {
                ProviderType.SolanaOASIS => "SOL",
                ProviderType.EthereumOASIS => "ETH",
                ProviderType.RadixOASIS => "XRD",
                ProviderType.ZcashOASIS => "ZEC",
                ProviderType.AztecOASIS => "AZTEC",
                ProviderType.MidenOASIS => "MIDEN",
                ProviderType.StarknetOASIS => "STARKNET",
                ProviderType.PolygonOASIS => "MATIC",
                ProviderType.ArbitrumOASIS => "ARB",
                ProviderType.OptimismOASIS => "OP",
                ProviderType.BNBChainOASIS => "BNB",
                ProviderType.AvalancheOASIS => "AVAX",
                ProviderType.NEAROASIS => "NEAR",
                ProviderType.SuiOASIS => "SUI",
                ProviderType.AptosOASIS => "APT",
                ProviderType.CardanoOASIS => "ADA",
                ProviderType.PolkadotOASIS => "DOT",
                ProviderType.BitcoinOASIS => "BTC",
                ProviderType.BaseOASIS => "ETH", // Base uses ETH
                ProviderType.FantomOASIS => "FTM",
                ProviderType.ChainLinkOASIS => "LINK",
                ProviderType.EOSIOOASIS => "EOS",
                ProviderType.HashgraphOASIS => "HBAR",
                ProviderType.ElrondOASIS => "EGLD",
                ProviderType.BlockStackOASIS => "STX",
                _ => null
            };
        }

        private IProviderWallet CloneWallet(IProviderWallet providerWallet)
        {
            return new ProviderWallet()
            {
                PublicKey = providerWallet.PublicKey,
                PrivateKey = providerWallet.PrivateKey,
                WalletAddress = providerWallet.WalletAddress,
                Id = providerWallet.Id,
                CreatedByAvatarId = providerWallet.CreatedByAvatarId,
                CreatedDate = providerWallet.CreatedDate,
                ModifiedByAvatarId = providerWallet.ModifiedByAvatarId,
                ModifiedDate = providerWallet.ModifiedDate,
                Version = providerWallet.Version,
                Name = providerWallet.Name,
                Description = providerWallet.Description,
                SecretRecoveryPhrase = providerWallet.SecretRecoveryPhrase,
                ProviderType = providerWallet.ProviderType,
                Balance = providerWallet.Balance,
                IsDefaultWallet = providerWallet.IsDefaultWallet,
                HolonType = providerWallet.HolonType,
                DeletedByAvatar = providerWallet.DeletedByAvatar,
                DeletedDate = providerWallet.DeletedDate,
                DeletedByAvatarId = providerWallet.DeletedByAvatarId,
                PreviousVersionId = providerWallet.PreviousVersionId,
                CreatedOASISType = providerWallet.CreatedOASISType,
                InstanceSavedOnProviderType = providerWallet.InstanceSavedOnProviderType,
                IsActive = providerWallet.IsActive,
                VersionId = providerWallet.VersionId,
                WalletAddressSegwitP2SH = providerWallet.WalletAddressSegwitP2SH,
                Transactions = providerWallet.Transactions,
                ProviderUniqueStorageKey = providerWallet.ProviderUniqueStorageKey,
                ProviderMetaData = providerWallet.ProviderMetaData,
                PreviousVersionProviderUniqueStorageKey = providerWallet.PreviousVersionProviderUniqueStorageKey,
                Original = providerWallet.Original
            };
        }

        private OASISResult<IProviderWallet> ProcessDecryption(IProviderWallet providerWallet, bool showPrivateKey = false, bool showSecretWords = false, Guid avatarId = default, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in ProcessDecryption, reason: ";

            try
            {
                //Need to clone so original wallets are not left decrypted on the avatar object!
                providerWallet = CloneWallet(providerWallet);

                if (showSecretWords)
                    providerWallet.SecretRecoveryPhrase = Rijndael.Decrypt(providerWallet.SecretRecoveryPhrase, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);

                if (showPrivateKey)
                {
                    if (string.IsNullOrEmpty(providerWallet.PrivateKey))
                    {
                        if (avatarId != Guid.Empty)
                        {
                            //Need to load private key from local storage provider.
                            OASISResult<IProviderWallet> walletResult = LoadProviderWalletForAvatarById(avatarId, providerWallet.Id, true, showSecretWords, providerType);

                            if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                                providerWallet.PrivateKey = walletResult.Result.PrivateKey;
                            else
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured whilst loading private key from local storage provider for avatar {avatarId} and wallet {providerWallet.Id}. Reason: {walletResult.Message}", walletResult.DetailedMessage);
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatarId passed in is null or empty guid! Please pass in a valid avatar id (GUID).");
                    }
                    else
                    //if (!string.IsNullOrEmpty(providerWallet.PrivateKey))
                        providerWallet.PrivateKey = Rijndael.Decrypt(providerWallet.PrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}Unknown error occured! Reason: {e.Message}");
            }

            result.Result = providerWallet;
            return result;
        }

        //TODO: Lots more coming soon! ;-)
    }
}
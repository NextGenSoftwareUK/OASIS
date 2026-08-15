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
    public partial class WalletManager
    {
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

                if (wallets.ContainsKey(providerTypeToShowWalletsFor))
                {
                    newWallets[providerTypeToShowWalletsFor] = wallets[providerTypeToShowWalletsFor];
                    wallets = newWallets;
                }
            }

            if (showPrivateKeys)
            {
                foreach (ProviderType provider in wallets.Keys)
                {
                    foreach (IProviderWallet wallet in wallets[provider])
                    {
                        if (wallet.PrivateKey != null)
                        {
                            try
                            {
                                wallet.PrivateKey = Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(wallet.PrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleError($"Error decrypting private key for wallet {wallet.Name} (ID: {wallet.Id}) of provider {provider}. Exception: {e}");
                            }
                        }
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
                        {
                            try
                            {
                                wallet.SecretRecoveryPhrase = Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(wallet.SecretRecoveryPhrase, OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                            }
                            catch (Exception e)
                            {
                                OASISErrorHandling.HandleError($"Error decrypting secret recovery phase for wallet {wallet.Name} (ID: {wallet.Id}) of provider {provider}. Exception: {e}");
                            }
                        }
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
                    providerWallet.SecretRecoveryPhrase = Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(providerWallet.SecretRecoveryPhrase, OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);

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
                        providerWallet.PrivateKey = Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(providerWallet.PrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
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

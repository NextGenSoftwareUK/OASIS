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
            if (wallets == null)
            {
                result.IsError = true;
                result.Message = "The wallets dictionary is required. Please provide a valid dictionary (can be empty).";
                return result;
            }
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
            if (wallets == null)
            {
                result.IsError = true;
                result.Message = "The wallets dictionary is required. Please provide a valid dictionary (can be empty).";
                return result;
            }
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
            if (wallets == null)
            {
                result.IsError = true;
                result.Message = "The wallets dictionary is required. Please provide a valid dictionary (can be empty).";
                return result;
            }
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
            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByPublicKeyForProvider(providerKey, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result = GetWalletThatPublicKeyBelongsTo(providerKey, providerType, avatarResult.Result, showPrivateKey, showSecretWords);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetWalletThatPublicKeyBelongsTo whilst loading avatar by public key. Reason:{avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<IProviderWallet> GetWalletThatPublicKeyBelongsTo(string providerKey, bool showPrivateKey = false, bool showSecretWords = false)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByPublicKeyForProvider(providerKey);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result = GetWalletThatPublicKeyBelongsTo(providerKey, avatarResult.Result);
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetWalletThatPublicKeyBelongsTo whilst loading avatar by public key. Reason:{avatarResult.Message}", avatarResult.DetailedMessage);

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

    }
}

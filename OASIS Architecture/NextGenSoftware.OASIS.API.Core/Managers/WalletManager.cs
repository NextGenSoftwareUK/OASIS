using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects;
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

        //public async Task<OASISResult<IProviderWallet>> AddWallet(Guid avatarId, string name, string description, ProviderType providerType, string privateKey = "", string publicKey = "", string walletAddress = "")
        //public async Task<OASISResult<IProviderWallet>> AddWallet(Guid avatarId, string name, string description, ProviderType providerType)
        //{
        //    OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

        //    ProviderWallet newWallet = new ProviderWallet()
        //    {
        //        WalletId = Guid.NewGuid(),
        //        CreatedByAvatarId = avatarId,
        //        CreatedDate = DateTime.Now,
        //        //WalletAddress = walletAddress,
        //        ProviderType = providerType,
        //        SecretRecoveryPhrase = Rijndael.Encrypt(string.Join(" ", new Mnemonic(Wordlist.English, WordCount.Twelve).Words), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256),
        //        //PrivateKey = privateKey,
        //    };
        //}

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

        public async Task<OASISResult<ITransactionRespone>> SendTokenAsync(IWalletTransactionRequest request)
        {
            OASISResult<ITransactionRespone> result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error Occured in SendTokenAsync function. Reason: ";

            if (string.IsNullOrEmpty(request.FromWalletAddress))
            {
                //Try and lookup the wallet address from the avatar id/username/email if one of those is provided.
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

                if (request.FromAvatarId != Guid.Empty)
                    walletsResult = await LoadProviderWalletsForAvatarByIdAsync(request.FromAvatarId, false, false, request.FromProvider.Value);

                else if (!string.IsNullOrEmpty(request.FromAvatarUsername))
                    walletsResult = await LoadProviderWalletsForAvatarByUsernameAsync(request.FromAvatarUsername, false, false, request.FromProvider.Value);

                else if (!string.IsNullOrEmpty(request.FromAvatarEmail))
                    walletsResult = await LoadProviderWalletsForAvatarByEmailAsync(request.FromAvatarEmail, false, false, request.FromProvider.Value);

                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} You must provide at least one of the following to identify the sender: FromWalletAddress, FromAvatarId, FromAvatarUsername or FromAvatarEmail.");

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

                    walletsResult = await LoadProviderWalletsForAvatarByIdAsync(request.ToAvatarId, false, false, request.ToProvider.Value);

                else if (!string.IsNullOrEmpty(request.ToAvatarUsername))
                    walletsResult = await LoadProviderWalletsForAvatarByUsernameAsync(request.ToAvatarUsername, false, false, request.ToProvider.Value);

                else if (!string.IsNullOrEmpty(request.ToAvatarEmail))
                    walletsResult = await LoadProviderWalletsForAvatarByEmailAsync(request.ToAvatarEmail, false, false, request.ToProvider.Value);
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
                    result = await oasisBlockchainProvider.SendTransactionAsync(request.FromWalletAddress, request.ToWalletAddress, request.Amount, request.MemoText);

                    if (result == null || (result != null && result.IsError || result.Result == null))
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error whilst calling the SendTransactionAsync function. Reason: {result.Message}");
                }
            }
            else
            {
                //TODO: Implement cross chain transfer logic here.
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Cross-chain sending is coming soon!");
            }

            return result;
        }

        public OASISResult<ITransactionRespone> SendToken(IWalletTransactionRequest request)
        {
            OASISResult<ITransactionRespone> result = new OASISResult<ITransactionRespone>();
            string errorMessage = "Error Occured in SendToken function. Reason: ";

            if (string.IsNullOrEmpty(request.FromWalletAddress))
            {
                //Try and lookup the wallet address from the avatar id/username/email if one of those is provided.
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

                if (request.FromAvatarId != Guid.Empty)
                    walletsResult = LoadProviderWalletsForAvatarById(request.FromAvatarId, providerTypeToLoadFrom: request.FromProvider.Value);

                else if (!string.IsNullOrEmpty(request.FromAvatarUsername))
                    walletsResult = LoadProviderWalletsForAvatarByUsername(request.FromAvatarUsername, providerTypeToLoadFrom: request.FromProvider.Value);

                else if (!string.IsNullOrEmpty(request.FromAvatarEmail))
                    walletsResult = LoadProviderWalletsForAvatarByEmail(request.FromAvatarEmail, providerTypeToLoadFrom: request.FromProvider.Value);

                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} You must provide at least one of the following to identify the sender: FromWalletAddress, FromAvatarId, FromAvatarUsername or FromAvatarEmail.");

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
                    result = oasisBlockchainProvider.SendTransaction(request.FromWalletAddress, request.ToWalletAddress, request.Amount, request.MemoText);

                    if (result == null || (result != null && result.IsError || result.Result == null))
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} There was an error whilst calling the SendTransactionAsync function. Reason: {result.Message}");
                }
            }
            else
            {
                //TODO: Implement cross chain transfer logic here.
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Cross-chain sending is coming soon!");
            }

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
                OASISResult<IProviderWallet> providerWallet = await LoadProviderWalletForAvatarByIdAsync(avatarId, walletId, providerType);

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
                OASISResult<IProviderWallet> providerWallet = LoadProviderWalletForAvatarById(avatarId, walletId, providerType);

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
                OASISResult<IProviderWallet> providerWallet = LoadProviderWalletForAvatarByUsername(username, walletId, providerType);

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
                OASISResult<IProviderWallet> providerWallet = LoadProviderWalletForAvatarByEmail(email, walletId, providerType);

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

        public async Task<OASISResult<IProviderWallet>> LoadProviderWalletForAvatarByIdAsync(Guid avatarId, Guid walletId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByIdAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByIdAsync(avatarId, false, false, providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                            break;
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

        public OASISResult<IProviderWallet> LoadProviderWalletForAvatarById(Guid avatarId, Guid walletId, ProviderType providerType = ProviderType.Default)
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
                            break;
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

        public async Task<OASISResult<IProviderWallet>> LoadProviderWalletForAvatarByUsernameAsync(string username, Guid walletId, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByUsernameAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByUsernameAsync(username, showOnlyDefault, decryptPrivateKeys, providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                            break;
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

        public OASISResult<IProviderWallet> LoadProviderWalletForAvatarByUsername(string username, Guid walletId, ProviderType providerType = ProviderType.Default)
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
                            break;
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

        public async Task<OASISResult<IProviderWallet>> LoadProviderWalletForAvatarByEmailAsync(string email, Guid walletId, bool showOnlyDefaultWallet = false, bool decryptPrivateKeys = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletForAvatarByEmailAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerType);

            try
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> providerWallets = await LoadProviderWalletsForAvatarByEmailAsync(email, showOnlyDefaultWallet, decryptPrivateKeys, providerType);

                if (providerWallets != null && providerWallets.Result != null && !providerWallets.IsError)
                {
                    foreach (ProviderType provider in providerWallets.Result.Keys)
                    {
                        result.Result = providerWallets.Result[provider].FirstOrDefault(x => x.Id == walletId);

                        if (result.Result != null)
                            break;
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

        public OASISResult<IProviderWallet> LoadProviderWalletForAvatarByEmail(string email, Guid walletId, ProviderType providerType = ProviderType.Default)
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
                            break;
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

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByIdAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                providerTypeToLoadFrom = ProviderType.LocalFileOASIS; //TODO: Temp!

                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerTypeToLoadFrom);
                errorMessage = string.Format(errorMessageTemplate, ProviderManager.Instance.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    //if (providerResult.Result.ProviderCategory.Value == ProviderCategory.StorageLocal || providerResult.Result.ProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork)
                    result = ((IOASISLocalStorageProvider)providerResult.Result).LoadProviderWalletsForAvatarById(id);
                    //else
                    //    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}The providerType ProviderCategory must be either StorageLocal or StorageLocalAndNetwork.");

                    if (result != null && result.Result != null && !result.IsError)
                        result.Result = FilterWallets(result.Result, showOnlyDefault, decryptPrivateKeys, providerTypeToShowWalletsFor);
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

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarById method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                providerTypeToLoadFrom = ProviderType.LocalFileOASIS; //TODO: Temp!

                OASISResult<IOASISStorageProvider> providerResult = ProviderManager.Instance.SetAndActivateCurrentStorageProvider(providerTypeToLoadFrom);
                errorMessage = string.Format(errorMessageTemplate, ProviderManager.Instance.CurrentStorageProviderType.Name);

                if (!providerResult.IsError && providerResult.Result != null)
                {
                    //if (providerResult.Result.ProviderCategory.Value == ProviderCategory.StorageLocal || providerResult.Result.ProviderCategory.Value == ProviderCategory.StorageLocalAndNetwork)
                    result = ((IOASISLocalStorageProvider)providerResult.Result).LoadProviderWalletsForAvatarById(id);
                    //else
                    //    OASISErrorHandling.HandleWarning(ref result, $"{errorMessage}The providerType ProviderCategory must be either StorageLocal or StorageLocalAndNetwork.");

                    if (result != null && result.Result != null && !result.IsError)
                        result.Result = FilterWallets(result.Result, showOnlyDefault, decryptPrivateKeys, providerTypeToShowWalletsFor);
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

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameAsync(string username, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByUsernameAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(username, false, true, providerTypeToLoadFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = await LoadProviderWalletsForAvatarByIdAsync(avatarResult.Result.Id, showOnlyDefault, decryptPrivateKeys, providerTypeToShowWalletsFor, providerTypeToLoadFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load for provider {providerTypeToLoadFrom}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsername(string username, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByUsername method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(username, false, true, providerTypeToLoadFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, decryptPrivateKeys, providerTypeToShowWalletsFor, providerTypeToLoadFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load for provider {providerTypeToLoadFrom}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailAsync(string email, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByEmailAsync method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarByEmailAsync(email, false, true, providerTypeToLoadFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = await LoadProviderWalletsForAvatarByIdAsync(avatarResult.Result.Id, showOnlyDefault, decryptPrivateKeys, providerTypeToShowWalletsFor, providerTypeToLoadFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load for provider {providerTypeToLoadFrom}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByEmail(string email, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessageTemplate = "Error occured in LoadProviderWalletsForAvatarByEmail method in WalletManager for providerType {0}. Reason: ";
            string errorMessage = string.Format(errorMessageTemplate, providerTypeToLoadFrom);

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByEmail(email, false, true, providerTypeToLoadFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, decryptPrivateKeys, providerTypeToShowWalletsFor, providerTypeToLoadFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load for provider {providerTypeToLoadFrom}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }


        public async Task<OASISResult<List<IProviderWallet>>> LoadProviderWalletsForProviderByAvatarIdAsync(Guid avatarId, ProviderType walletProviderType, bool showOnlyDefault = false, bool decryptPrivateKeys = false)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = await LoadProviderWalletsForAvatarByIdAsync(avatarId, showOnlyDefault, decryptPrivateKeys);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForProviderByAvatarIdAsync. Reason: {result.Result}");

            return result;
        }

        public OASISResult<List<IProviderWallet>> LoadProviderWalletsForProviderByAvatarId(Guid avatarId, ProviderType walletProviderType)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = LoadProviderWalletsForAvatarById(avatarId);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForProviderByAvatarId. Reason: {result.Result}");

            return result;
        }

        public async Task<OASISResult<List<IProviderWallet>>> LoadProviderWalletsForProviderByAvatarUsernameAsync(string username, ProviderType walletProviderType)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = await LoadProviderWalletsForAvatarByUsernameAsync(username);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForProviderByAvatarUsernameAsync. Reason: {result.Result}");

            return result;
        }

        public OASISResult<List<IProviderWallet>> LoadProviderWalletsForProviderByAvatarUsername(string username, ProviderType walletProviderType)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = LoadProviderWalletsForAvatarByUsername(username);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForProviderByAvatarUsername. Reason: {result.Result}");

            return result;
        }

        public async Task<OASISResult<List<IProviderWallet>>> LoadProviderWalletsForProviderByAvatarEmailAsync(string email, ProviderType walletProviderType)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = await LoadProviderWalletsForAvatarByEmailAsync(email);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForAvatarByEmailAsync. Reason: {result.Result}");

            return result;
        }

        public OASISResult<List<IProviderWallet>> LoadProviderWalletsForProviderByAvatarEmail(string email, ProviderType walletProviderType)
        {
            OASISResult<List<IProviderWallet>> result = new OASISResult<List<IProviderWallet>>();
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> wallets = LoadProviderWalletsForAvatarByEmail(email);

            if (wallets != null && wallets.Result != null && !wallets.IsError)
                result.Result = wallets.Result[walletProviderType];
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForAvatarByEmail. Reason: {result.Result}");

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdUsingHyperDriveAsync(Guid id, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result =
                new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await LoadProviderWalletsForAvatarByIdAsync(id, showOnlyDefault, decryptPrivateKeys, providerTypeToLoadFrom: type.Value);
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

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsingHyperDriveId(Guid id, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result =
                new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            foreach (EnumValue<ProviderType> type in ProviderManager.Instance.GetProviderAutoFailOverList())
            {
                OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = LoadProviderWalletsForAvatarById(id, showOnlyDefault, decryptPrivateKeys, providerTypeToLoadFrom: type.Value);
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

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsernameUsingHyperDrive(string username, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in LoadProviderWalletsForAvatarByUsernameUsingHyperDrive method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatar(username, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, decryptPrivateKeys, providerTypeToShowWalletsFor);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameUsingHyperDriveAsync(string username, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in LoadProviderWalletsForAvatarByUsernameUsingHyperDriveAsync method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarAsync(username, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, decryptPrivateKeys, providerTypeToShowWalletsFor);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with username {username} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByEmailUsingHyperDrive(string email, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in LoadProviderWalletsForAvatarByEmailUsingHyperDrive method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.Instance.LoadAvatarByEmail(email, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, decryptPrivateKeys, providerTypeToShowWalletsFor);
                else
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage}The avatar with email {email} failed to load. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailUsingHyperDriveAsync(string email, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            string errorMessage = "Error occured in LoadProviderWalletsForAvatarByEmailUsingHyperDriveAsync method in WalletManager. Reason: ";

            try
            {
                OASISResult<IAvatar> avatarResult = await AvatarManager.Instance.LoadAvatarByEmailAsync(email, false, true);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LoadProviderWalletsForAvatarById(avatarResult.Result.Id, showOnlyDefault, decryptPrivateKeys, providerTypeToShowWalletsFor);
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

                OASISResult<IOASISStorageProvider> providerResult = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(providerType);
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

            await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(currentProviderType);
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


        public OASISResult<IProviderWallet> GetWalletThatPublicKeyBelongsTo(string providerKey, ProviderType providerType)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            OASISResult<IEnumerable<IAvatar>> avatarsResult = AvatarManager.Instance.LoadAllAvatars();

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
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetWalletThatPublicKeyBelongsTo whilst loading avatars. Reason:{avatarsResult.Message}", avatarsResult.DetailedMessage);

            return result;
        }

        public OASISResult<IProviderWallet> GetWalletThatPublicKeyBelongsTo(string providerKey)
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

        public OASISResult<IProviderWallet> GetWalletThatPublicKeyBelongsTo(string providerKey, IAvatar avatar)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            foreach (ProviderType providerType in avatar.ProviderWallets.Keys)
            {
                foreach (IProviderWallet wallet in avatar.ProviderWallets[providerType])
                {
                    if (wallet.PublicKey == providerKey)
                    {
                        result.Result = wallet;
                        result.Message = "Wallet Found";
                        return result;
                    }
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

        public OASISResult<IProviderWallet> ImportWalletUsingPrivateKeyById(Guid avatarId, string key, ProviderType providerToImportTo)
        {
            //OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            //TODO: Finish implementing... Can derive the public key from the private key  (need to look into how to do this and update Link methods with new logic...)


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
            //OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            //TODO: Finish implementing... The wallet will only be read-only without the private key.
            //This will be very similar to the LinkProviderPublicKeyToAvatarById/LinkProviderPublicKeyToAvatarByUsername/LinkProviderPublicKeyToAvatarByEmail methods in KeyManager.
            //Ideally this method will call into the Link methods above (probably best to just have this method call them direct, no additional logic needed.

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

        public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByIdAsync(Guid avatarId, ProviderType providerType, bool showOnlyDefaultWallet = false, bool decryptPrivateKeys = false)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in GetAvatarDefaultWalletById method in WalletManager. Reason: ";

            try
            {
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByIdAsync(avatarId, showOnlyDefaultWallet, decryptPrivateKeys, providerType);
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
                    }   
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByUsernameAsync(string avatarUsername, bool showOnlyDefaultWallet = false, bool decryptPrivateKeys = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in GetAvatarDefaultWalletByUsername method in WalletManager. Reason: ";

            try
            {
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByUsernameAsync(avatarUsername, showOnlyDefaultWallet, decryptPrivateKeys, providerType);
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
                    }   
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> GetAvatarDefaultWalletByEmailAsync(string email, ProviderType providerType, bool showOnlyDefaultWallet = false, bool decryptPrivateKeys = false)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            string errorMessage = "Error occured in GetAvatarDefaultWalletByEmail method in WalletManager. Reason: ";

            try
            {
                var allAvatarWalletsByProvider = await LoadProviderWalletsForAvatarByEmailAsync(email, showOnlyDefaultWallet, decryptPrivateKeys, providerType);
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

        private Dictionary<ProviderType, List<IProviderWallet>> FilterWallets(Dictionary<ProviderType, List<IProviderWallet>> wallets, bool showOnlyDefault = false, bool decryptPrivateKeys = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All)
        {
            if (providerTypeToShowWalletsFor != ProviderType.All)
            {
                Dictionary<ProviderType, List<IProviderWallet>> newWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
                newWallets[providerTypeToShowWalletsFor] = wallets[providerTypeToShowWalletsFor];
                wallets = newWallets;
            }

            if (decryptPrivateKeys)
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

            if (showOnlyDefault)
            {
                Dictionary<ProviderType, List<IProviderWallet>> newWallets = new Dictionary<ProviderType, List<IProviderWallet>>();

                foreach (ProviderType provider in wallets.Keys)
                {
                    if (!wallets.ContainsKey(provider))
                        wallets[provider] = new List<IProviderWallet>();

                    foreach (IProviderWallet wallet in wallets[provider])
                    {
                        if (wallet.IsDefaultWallet)
                            wallets[provider].Add(wallet);
                    }
                }

                wallets = newWallets;
            }

            return wallets;
        }

        //TODO: Lots more coming soon! ;-)
    }
}
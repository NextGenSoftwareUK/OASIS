using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class Wallets
    {
        //public async Task<OASISResult<IProviderWallet>> AddWallet()
        //{
        //    OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

        //    STAR.OASISAPI.Wallets.Add
        //}

        public async Task<OASISResult<IProviderWallet>> CreateWalletAsync(ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            string name = CLIEngine.GetValidInput($"Please enter the new wallet name:", addLineBefore: false);
            string desc = CLIEngine.GetValidInput($"Please enter the new wallet description:", addLineBefore: false);

            object objProviderType = CLIEngine.GetValidInputForEnum($"Please enter the new wallet provider type:", typeof(ProviderType), addLineBefore: false);

            if (objProviderType != null)
            {
                if (objProviderType.ToString() == "exit")
                {
                    result.Message = "User Exited";
                    return result;
                }

                ProviderType walletProviderType = (ProviderType)objProviderType;
                bool isDefault = CLIEngine.GetConfirmation($"Will this be the new default wallet?", addLineBefore: false);
                bool showSecretPhase = CLIEngine.GetConfirmation($"Do you wish to show/decrypt the secret recovery phase after the wallet has been created?", addLineBefore: true);
                bool showPrivateKey = CLIEngine.GetConfirmation($"Do you wish to show/decrypt the private key after the wallet has been created?", addLineBefore: true);

                CLIEngine.ShowWorkingMessage("Creating Wallet...", addLineBefore: true);
                result = await STAR.OASISAPI.Wallets.CreateWalletForAvatarByIdAsync(STAR.BeamedInAvatar.Id, name, desc, walletProviderType, true, isDefault, showSecretPhase, showPrivateKey, providerTypeToLoadSave);

                if (result != null && result.Result != null && !result.IsError)
                {
                    CLIEngine.ShowSuccessMessage("Wallet Successfully Created", addLineBefore: true);

                    Console.WriteLine("");
                    ShowWallet(result.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error Occured Creating Wallet. Reason: {result.Message}", addLineBefore: true);
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> UpdateWallet(ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            OASISResult<IProviderWallet> walletResult = await FindWalletAsync("Select the wallet you wish to edit");

            if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
            {
                if (CLIEngine.GetConfirmation($"Do you wish to edit the name? (currently is: {walletResult.Result.Name})"))
                    walletResult.Result.Name = CLIEngine.GetValidInput($"Please enter the new wallet name:", addLineBefore: true);
                else
                    Console.WriteLine("");

                if (CLIEngine.GetConfirmation($"Do you wish to edit the description? (currently is: {walletResult.Result.Description})"))
                    walletResult.Result.Description = CLIEngine.GetValidInput($"Please enter the new wallet description:", addLineBefore: true);
                else
                    Console.WriteLine("");

                if (CLIEngine.GetConfirmation($"Do you wish to edit the provider type? (currently is: {Enum.GetName(typeof(ProviderType), walletResult.Result.ProviderType)})"))
                {
                    object objProviderType = CLIEngine.GetValidInputForEnum($"Please enter the new wallet provider type:", typeof(ProviderType), addLineBefore: true);

                    if (objProviderType != null)
                    {
                        if (objProviderType.ToString() == "exit")
                        {
                            result.Message = "User Exited";
                            return result;
                        }

                        walletResult.Result.ProviderType = (ProviderType)objProviderType;
                    }
                }
                else
                    Console.WriteLine("");

                CLIEngine.ShowWorkingMessage("Updating Wallet...");
                result = await STAR.OASISAPI.Wallets.UpdateWalletForAvatarByIdAsync(STAR.BeamedInAvatar.Id, walletResult.Result.Id, walletResult.Result.Name, walletResult.Result.Description, walletResult.Result.ProviderType, providerTypeToLoadSave);

                if (result != null && result.Result != null && !result.IsError)
                    CLIEngine.ShowSuccessMessage("Wallet Successfully Updated", addLineBefore: true);
                else
                    CLIEngine.ShowErrorMessage($"Error Occured Updating Wallet. Reason: {result.Message}", addLineBefore: true);
            }

            return result;
        }

        public async Task<OASISResult<ISendWeb4TokenResponse>> SendToken(ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<ISendWeb4TokenResponse> result = new OASISResult<ISendWeb4TokenResponse>();
            ISendWeb4TokenRequest request = new SendWeb4TokenRequest()
            {
                FromAvatarId = STAR.BeamedInAvatar.Id,
                FromAvatarEmail = STAR.BeamedInAvatar.Email,
                FromAvatarUsername = STAR.BeamedInAvatar.Username
            };

            object objProviderType = CLIEngine.GetValidInputForEnum($"Please enter the provider type/chain you wish to send from:", typeof(ProviderType), addLineBefore: true);

            if (objProviderType != null)
            {
                if (objProviderType.ToString() == "exit")
                {
                    result.Message = "User Exited";
                    return result;
                }

                request.FromProvider = new Utilities.EnumValue<ProviderType>((ProviderType)objProviderType);
            }

            objProviderType = CLIEngine.GetValidInputForEnum($"Please enter the provider type/chain you wish to send to:", typeof(ProviderType), addLineBefore: true);

            if (objProviderType != null)
            {
                if (objProviderType.ToString() == "exit")
                {
                    result.Message = "User Exited";
                    return result;
                }

                request.ToProvider = new Utilities.EnumValue<ProviderType>((ProviderType)objProviderType);
            }

            int selection = CLIEngine.GetValidInputForInt("Do you wish to send the token using the users (1) Wallet Address, (2) Avatar Id, (3) Username or (4) Email? (Please enter 1, 2, 3 or 4)", true, 1, 4);

            switch (selection)
            {
                case 1:
                    request.ToWalletAddress = CLIEngine.GetValidInput("What is the wallet address you want to send the token to?");
                    break;

                case 2:
                    request.ToAvatarId = CLIEngine.GetValidInputForGuid("What is the Id of the Avatar you want to send the token to?");
                    break;

                case 3:
                    request.ToAvatarUsername = CLIEngine.GetValidInput("What is the Username of the Avatar you want to send the token to?");
                    break;

                case 4:
                    request.ToAvatarEmail = CLIEngine.GetValidInputForEmail("What is the Email of the Avatar you want to send the token to?");
                    break;
            }

            request.MemoText = CLIEngine.GetValidInput("What is the memo text for this transaction?");

            CLIEngine.ShowWorkingMessage("Sending Token..");
            result = await STAR.OASISAPI.Wallets.SendTokenAsync(STAR.BeamedInAvatar.Id, request);

            if (result != null && result.Result != null && !result.IsError)
                CLIEngine.ShowSuccessMessage("Token Successfully Sent", addLineBefore: true);
            else
                CLIEngine.ShowErrorMessage($"Error Occured Sending Token. Reason: {result.Message}", addLineBefore: true);
            
            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ImportWalletUsingSecretRecoveryPhase(ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            string phase = CLIEngine.GetValidInput("What is the secret recovery phase?");
            CLIEngine.ShowWorkingMessage("Importing Wallet..");
            result = await STAR.OASISAPI.Wallets.ImportWalletUsingSecretPhaseByIdAsync(STAR.BeamedInAvatar.Id, phase);

            if (result != null && result.Result != null && !result.IsError)
            {
                CLIEngine.ShowSuccessMessage("Wallet Successfully Imported", addLineBefore: true);
                ShowWallet(result.Result);
            }
            else
                CLIEngine.ShowErrorMessage($"Error Occured Importing Wallet. Reason: {result.Message}", addLineBefore: true);

            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletUsingJSONFile(ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            string path = CLIEngine.GetValidFile("What is the full path to the JSON file?");
            CLIEngine.ShowWorkingMessage("Importing Wallet..");
            result = STAR.OASISAPI.Wallets.ImportWalletUsingJSONFileById(STAR.BeamedInAvatar.Id, path);

            if (result != null && result.Result != null && !result.IsError)
            {
                CLIEngine.ShowSuccessMessage("Wallet Successfully Imported", addLineBefore: true);
                ShowWallet(result.Result);
            }
            else
                CLIEngine.ShowErrorMessage($"Error Occured Importing Wallet. Reason: {result.Message}", addLineBefore: true);

            return result;
        }

        public OASISResult<IProviderWallet> ImportWalletsUsingJSONFile(ProviderType providerTypeToSaveTo = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            string path = CLIEngine.GetValidFile("What is the full path to the JSON file?");
            CLIEngine.ShowWorkingMessage("Importing Wallets..");
            result = STAR.OASISAPI.Wallets.ImportWalletUsingJSONFileById(STAR.BeamedInAvatar.Id, path);

            if (result != null && result.Result != null && !result.IsError)
            {
                CLIEngine.ShowSuccessMessage("Wallets Successfully Imported", addLineBefore: true);
                ShowWallet(result.Result);
            }
            else
                CLIEngine.ShowErrorMessage($"Error Occured Importing Wallets. Reason: {result.Message}", addLineBefore: true);

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> ExportWallet(ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            result = await FindWalletAsync("Select the wallet you wish to export: ");

            if (result != null && result.Result != null && !result.IsError)
            {
                string path = CLIEngine.GetValidInput("What is the full path you wish to export to?");
                bool decryptPrivateKeys = CLIEngine.GetConfirmation("Do you wish to decrypt/show the private keys?");
                bool showSecretWords = CLIEngine.GetConfirmation("Do you wish to decrypt/show the secrert recovery words?", addLineBefore: true);

                CLIEngine.ShowWorkingMessage("Exporting Wallet..", addLineBefore: true);
                result = await STAR.OASISAPI.Wallets.ExportWalletByIdAsync(STAR.BeamedInAvatar.Id, result.Result.Id, path, decryptPrivateKeys, showSecretWords, providerTypeToLoadSave);

                if (result != null && result.Result != null && !result.IsError)
                    CLIEngine.ShowSuccessMessage("Wallet Successfully Exported", addLineBefore: true);
                else
                    CLIEngine.ShowErrorMessage($"Error Occured Exporting Wallet. Reason: {result.Message}", addLineBefore: true);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> ExportAllWalletsAsync(ProviderType providerTypeToLoadSave = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();
            ProviderType providerTypeToExportFor = ProviderType.All;

            string path = CLIEngine.GetValidInput("What is the full path you wish to export to?");
            bool exportDefaultWalletsOnly = CLIEngine.GetConfirmation("Do you wish to only export the default wallets?");
            bool decryptPrivateKeys = CLIEngine.GetConfirmation("Do you wish to decrypt the private keys?", addLineBefore: true);

            if (!CLIEngine.GetConfirmation("Do you wish to export ALL wallets? If you enter 'N' then you will be asked which to export for next.", addLineBefore: true))
            {
                object providerTypeObj = CLIEngine.GetValidInputForEnum("Which provider/chain do you wisth to export for?", typeof(ProviderType));

                if (providerTypeObj != null)
                {
                    if (providerTypeObj.ToString() == "exit")
                    {
                        result.IsWarning = true;
                        result.Message = "User Exited";
                        return result;
                    }

                    providerTypeToExportFor = (ProviderType)providerTypeObj;
                }
            }

            CLIEngine.ShowWorkingMessage("Exporting Wallets..");
            result = await STAR.OASISAPI.Wallets.ExportAllWalletsByIdAsync(STAR.BeamedInAvatar.Id, path, exportDefaultWalletsOnly, decryptPrivateKeys, providerTypeToExportFor, providerTypeToLoadSave);

            if (result != null && result.Result != null && !result.IsError)
                CLIEngine.ShowSuccessMessage("Wallets Successfully Exported", addLineBefore: true);
            else
                CLIEngine.ShowErrorMessage($"Error Occured Exporting Wallets. Reason: {result.Message}", addLineBefore: true);
            
            return result;
        }

        public async Task<OASISResult<double>> GetTotalBalance()
        {
            CLIEngine.ShowWorkingMessage("Getting Balance...");
            OASISResult<double> balance = await STAR.OASISAPI.Wallets.GetTotalBalanceForAllProviderWalletsForAvatarByIdAsync(STAR.BeamedInAvatar.Id);
            CLIEngine.ShowMessage($"Total Blanace: {balance.Result}");
            return balance;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(Guid walletId)
        {
            CLIEngine.ShowWorkingMessage("Getting Balance...");
            OASISResult<double> balance = await STAR.OASISAPI.Wallets.GetBalanceForWalletForAvatarByIdAsync(STAR.BeamedInAvatar.Id, walletId);
            CLIEngine.ShowMessage($"Blanace: {balance.Result}");
            return balance;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(ProviderType providerType)
        {
            CLIEngine.ShowWorkingMessage("Getting Balance...");
            OASISResult<double> balance = await STAR.OASISAPI.Wallets.GetTotalBalanceForProviderWalletsForAvatarByIdAsync(STAR.BeamedInAvatar.Id, providerType);
            CLIEngine.ShowMessage($"Blanace: {balance.Result}");
            return balance;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(string walletIdOrProviderType)
        {
            OASISResult<double> balance = new OASISResult<double>();
            Guid id = Guid.Empty;
            object providerTypeObj = null;
            CLIEngine.ShowWorkingMessage("Getting Balance...");

            if (Guid.TryParse(walletIdOrProviderType, out id))
                balance = await STAR.OASISAPI.Wallets.GetBalanceForWalletForAvatarByIdAsync(STAR.BeamedInAvatar.Id, id);
            
            else if (Enum.TryParse(typeof(ProviderType), walletIdOrProviderType, out providerTypeObj))
                balance = await STAR.OASISAPI.Wallets.GetTotalBalanceForProviderWalletsForAvatarByIdAsync(STAR.BeamedInAvatar.Id, (ProviderType)providerTypeObj);

            CLIEngine.ShowMessage($"Blanace: {balance.Result}");
            return balance;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> ListProviderWalletsForBeamedInAvatarAsync(bool showOnlyDefault = false, bool showPrivateKeys = false, bool showSecretWords = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default, bool showNumbers = false)
        {
            CLIEngine.ShowWorkingMessage("Loading Wallets...");
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await STAR.OASISAPI.Wallets.LoadProviderWalletsForAvatarByIdAsync(STAR.BeamedInAvatar.Id, showOnlyDefault, showPrivateKeys, showSecretWords, providerTypeToShowWalletsFor, providerTypeToLoadFrom);
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = false;

            if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
                ShowWallets(walletsResult.Result, showNumbers: showNumbers);

            return walletsResult;
        }


        //public async Task<OASISResult<IProviderWallet>> ShowDefaultWalletForBeamedInAvatarAsync()
        //{
        //    return await ShowDefaultWalletForBeamedInAvatarAsync(null, null);
        //}

        public async Task<OASISResult<IProviderWallet>> ShowDefaultWalletForBeamedInAvatarAsync(bool? showPrivateKeys, bool? showSecretWords)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            object providerObj = CLIEngine.GetValidInputForEnum("Enter the provider (chain) you wish to get the default wallet for: ", typeof(ProviderType));

            if (providerObj != null)
            {
                if (providerObj.ToString() == "exit")
                {
                    result.Message = "User Exited";
                    return result;
                }

                if (!showPrivateKeys.HasValue)
                    showPrivateKeys = CLIEngine.GetConfirmation("Do you wish to decrypt/show the private keys?");

                if (!showSecretWords.HasValue)
                    showSecretWords = CLIEngine.GetConfirmation("Do you wish to decrypt/show the secrert recovery words?", addLineBefore: true);

                CLIEngine.ShowWorkingMessage("Loading Default Wallet...", addLineBefore: true);
                Console.WriteLine("");
                ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
                result = await STAR.OASISAPI.Wallets.GetAvatarDefaultWalletByIdAsync(STAR.BeamedInAvatar.Id, (ProviderType)providerObj, true, showPrivateKeys.Value, showSecretWords.Value);
                ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = false;

                if (result != null && result.Result != null && !result.IsError)
                    ShowWallet(result.Result);
            }

            return result;
        }

        public OASISResult<IProviderWallet> ShowWalletThatPublicKeyBelongsTo(string? publicKey, bool? showPrivateKeys, bool? showSecretWords)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
                publicKey = CLIEngine.GetValidInput("Enter the public key you wish to find the wallet for: ");

            if (!showPrivateKeys.HasValue)
                showPrivateKeys = CLIEngine.GetConfirmation("Do you wish to decrypt/show the private keys?");
            
            if (!showSecretWords.HasValue)
                showSecretWords = CLIEngine.GetConfirmation("Do you wish to decrypt/show the secrert recovery words?", addLineBefore: true);

            CLIEngine.ShowWorkingMessage("Loading Wallet...", addLineBefore: true);
            Console.WriteLine("");

            OASISResult<IProviderWallet> walletResult = STAR.OASISAPI.Wallets.GetWalletThatPublicKeyBelongsTo(publicKey, STAR.BeamedInAvatar, showPrivateKeys.Value, showSecretWords.Value);

            if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                ShowWallet(walletResult.Result);
            else
                CLIEngine.ShowWarningMessage("No Wallets Found!");

            return walletResult;
        }

        public void ShowWallets(Dictionary<ProviderType, List<IProviderWallet>> wallets, int displayFieldLength = 40, bool showNumbers = false)
        {
            int number = 1;

            foreach (ProviderType provider in wallets.Keys)
            {
                CLIEngine.ShowMessage($"Provider: {Enum.GetName(typeof(ProviderType), provider)}");
                Console.WriteLine("");

                foreach (IProviderWallet wallet in wallets[provider])
                {
                    ShowWallet(wallet, showNumbers: showNumbers, number: number);
                    number++;
                }
            }
        }

        public void ShowWallet(IProviderWallet wallet, int displayFieldLength = 40, bool showNumbers = false, int number = 0)
        {
            if (showNumbers)
                CLIEngine.DisplayProperty("Number", number.ToString(), displayFieldLength);

            CLIEngine.DisplayProperty("Id", wallet.WalletId.ToString(), displayFieldLength);
            CLIEngine.DisplayProperty("Provider Type", Enum.GetName(typeof(ProviderType), wallet.ProviderType), displayFieldLength);
            CLIEngine.DisplayProperty("Name", wallet.Name, displayFieldLength);
            CLIEngine.DisplayProperty("Description", wallet.Description, displayFieldLength);
            CLIEngine.DisplayProperty("Wallet Address", wallet.WalletAddress, displayFieldLength);
            CLIEngine.DisplayProperty("Wallet Address SeqwitP2SH", wallet.WalletAddressSegwitP2SH, displayFieldLength);
            CLIEngine.DisplayProperty("Public Key", wallet.PublicKey, displayFieldLength);
            CLIEngine.DisplayProperty("Private Key", wallet.PrivateKey, displayFieldLength);
            CLIEngine.DisplayProperty("Secret Recovery Phrase", wallet.SecretRecoveryPhrase, displayFieldLength);
            CLIEngine.DisplayProperty("Default", wallet.IsDefaultWallet == true ? "Yes" : "No", displayFieldLength);
            CLIEngine.DisplayProperty("Created Date", wallet.CreatedDate.ToShortDateString(), displayFieldLength);
            CLIEngine.DisplayProperty("Created By", wallet.CreatedByAvatarId.ToString(), displayFieldLength);
            CLIEngine.DisplayProperty("Balance", wallet.Balance.ToString(), displayFieldLength);
            CLIEngine.ShowDivider();
        }

        public async Task<OASISResult<IProviderWallet>> SetDefaultWalletAsync()
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            object providerObj = CLIEngine.GetValidInputForEnum("Enter the provider (chain) you wish to set the default wallet for: ", typeof(ProviderType));

            if (providerObj != null)
            {
                if (providerObj.ToString() == "exit")
                {
                    result.Message = "User Exited";
                    return result;
                }

                OASISResult<IProviderWallet> selectedWallet = await FindWalletAsync("Enter the number of the wallet you wish to set as the default:", (ProviderType)providerObj);

                if (selectedWallet != null && selectedWallet.Result != null && !selectedWallet.IsError)
                {
                    CLIEngine.ShowWorkingMessage("Setting Default Wallet...");
                    OASISResult<IProviderWallet> walletResult = await STAR.OASISAPI.Wallets.SetAvatarDefaultWalletByIdAsync(STAR.BeamedInAvatar.Id, selectedWallet.Result.Id, (ProviderType)providerObj);

                    if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                    {
                        CLIEngine.ShowSuccessMessage("Default Wallet Saved Successfully");
                        Console.WriteLine("");
                        ShowWallet(walletResult.Result);
                    }
                    else
                        OASISErrorHandling.HandleError(ref selectedWallet, $"Error saving default wallet. Reason: {walletResult.Message}");
                }
            }

            return result;
        }

        //TODO: Improve later so can search by title, desc, public key, wallet address etc (may be a SearchWallet function)
        public async Task<OASISResult<IProviderWallet>> FindWalletAsync(string message, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await ListProviderWalletsForBeamedInAvatarAsync(false, false, false, providerTypeToShowWalletsFor, providerTypeToLoadFrom, true);
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = false;

            Dictionary<int, IProviderWallet> lookup = new Dictionary<int, IProviderWallet>();
            int number = 0;

            if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
            {
                foreach (ProviderType provider in walletsResult.Result.Keys)
                {
                    foreach (IProviderWallet wallet in walletsResult.Result[provider])
                    {
                        number++;
                        lookup[number] = wallet;
                    }
                }
            }

            int selectedWallet = CLIEngine.GetValidInputForInt(message, true, 1, number);
            result.Result = lookup[selectedWallet];
            return result;
        }

        public void SaveWallet()
        {

        }
    }
}
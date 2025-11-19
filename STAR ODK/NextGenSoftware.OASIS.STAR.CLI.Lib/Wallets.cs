using Grpc.Core;
using Nethereum.RPC.Eth;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
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

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> ListProviderWalletsForBeamedInAvatarAsync(bool showOnlyDefault = false, ProviderType providerTypeToShowWalletsFor = ProviderType.All, ProviderType providerTypeToLoadFrom = ProviderType.Default, bool showNumbers = false)
        {
            CLIEngine.ShowWorkingMessage("Loading Wallets...");
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await STAR.OASISAPI.Wallets.LoadProviderWalletsForAvatarByIdAsync(STAR.BeamedInAvatar.Id, showOnlyDefault, true, providerTypeToShowWalletsFor, providerTypeToLoadFrom);
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = false;

            if (walletsResult != null && walletsResult.Result != null && !walletsResult.IsError)
            {
                int number = 1;

                foreach (ProviderType provider in walletsResult.Result.Keys)
                {
                    CLIEngine.ShowMessage($"Provider: {Enum.GetName(typeof(ProviderType), provider)}");
                    Console.WriteLine("");    

                    foreach (IProviderWallet wallet in walletsResult.Result[provider])
                    {
                        ShowWallet(wallet, showNumbers: showNumbers, number: number);
                        number++;
                    }
                }
            }

            return walletsResult;
        }

        public async Task<OASISResult<IProviderWallet>> ShowDefaultWalletForBeamedInAvatarAsync()
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

                CLIEngine.ShowWorkingMessage("Loading Default Wallet...");
                Console.WriteLine("");
                ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
                result = await STAR.OASISAPI.Wallets.GetAvatarDefaultWalletByIdAsync(STAR.BeamedInAvatar.Id, (ProviderType)providerObj, true);
                ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = false;

                if (result != null && result.Result != null && !result.IsError)
                    ShowWallet(result.Result);
            }

            return result;
        }

        public OASISResult<IProviderWallet> ShowWalletThatPublicKeyBelongsTo()
        {
            string publicKey = CLIEngine.GetValidInput("Enter the provider key you wish to find the wallet for: ");

            CLIEngine.ShowWorkingMessage("Loading Wallet...");
            Console.WriteLine("");
            OASISResult<IProviderWallet> walletResult = STAR.OASISAPI.Wallets.GetWalletThatPublicKeyBelongsTo(publicKey, STAR.BeamedInAvatar);

            if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                ShowWallet(walletResult.Result);
            else
                CLIEngine.ShowWarningMessage("No Wallets Found!");

            return walletResult;
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
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await ListProviderWalletsForBeamedInAvatarAsync(false,providerTypeToShowWalletsFor, providerTypeToLoadFrom, true);
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
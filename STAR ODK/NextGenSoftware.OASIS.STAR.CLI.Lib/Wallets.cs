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

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> ListProviderWalletsForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default, bool showNumbers = false)
        {
            CLIEngine.ShowWorkingMessage("Loading Wallets...");
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await STAR.OASISAPI.Wallets.LoadProviderWalletsForAvatarByIdAsync(STAR.BeamedInAvatar.Id, true, providerType);

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

        public async Task<OASISResult<IProviderWallet>> ShowDefaultWalletForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading Default Wallet...");
            OASISResult<IProviderWallet> walletResult = await STAR.OASISAPI.Wallets.GetAvatarDefaultWalletByIdAsync(STAR.BeamedInAvatar.Id, providerType, true);

            if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                ShowWallet(walletResult.Result);

            return walletResult;
        }

        public OASISResult<IProviderWallet> ShowWalletThatPublicKeyBelongsTo(ProviderType providerType = ProviderType.Default)
        {
            string publicKey = CLIEngine.GetValidInput("Enter the provider key you wish to find the wallet for: ");

            CLIEngine.ShowWorkingMessage("Loading Wallet...");
            OASISResult<IProviderWallet> walletResult = STAR.OASISAPI.Wallets.GetWalletThatPublicKeyBelongsTo(publicKey, providerType, STAR.BeamedInAvatar);

            if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                ShowWallet(walletResult.Result);

            return walletResult;
        }

        public void ShowWallet(IProviderWallet wallet, int displayFieldLength = 40, bool showNumbers = false, int number = 0)
        {
            if (showNumbers)
                CLIEngine.DisplayProperty("Number", number.ToString(), displayFieldLength);

            CLIEngine.DisplayProperty("Id", wallet.WalletId.ToString(), displayFieldLength);
            CLIEngine.DisplayProperty("Name", wallet.Name, displayFieldLength);
            CLIEngine.DisplayProperty("Description", wallet.Description, displayFieldLength);
            CLIEngine.DisplayProperty("WalletAddress", wallet.WalletAddress, displayFieldLength);
            CLIEngine.DisplayProperty("Public Key", wallet.PublicKey, displayFieldLength);
            CLIEngine.DisplayProperty("Private Key", wallet.PrivateKey, displayFieldLength);
            CLIEngine.DisplayProperty("Secret Recovery Phrase", wallet.SecretRecoveryPhrase, displayFieldLength);
            CLIEngine.DisplayProperty("Default", wallet.IsDefaultWallet == true ? "Yes" : "No", displayFieldLength);
            CLIEngine.DisplayProperty("Created Date", wallet.CreatedDate.ToShortDateString(), displayFieldLength);
            CLIEngine.DisplayProperty("Created By", wallet.CreatedByAvatarId.ToString(), displayFieldLength);
            CLIEngine.DisplayProperty("Balance", wallet.Balance.ToString(), displayFieldLength);
            CLIEngine.ShowDivider();
        }

        public async Task<OASISResult<IProviderWallet>> SetDefaultWalletAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> selectedWallet = await FindWalletAsync("Enter the number of the wallet you wish to set as the default:", providerType);

            if (selectedWallet != null && selectedWallet.Result != null && !selectedWallet.IsError)
            {
                CLIEngine.ShowWorkingMessage("Setting Default Wallet...");
                OASISResult<IProviderWallet> walletResult = await STAR.OASISAPI.Wallets.SetAvatarDefaultWalletByIdAsync(STAR.BeamedInAvatar.Id, selectedWallet.Result.Id, providerType);

                if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                {
                    CLIEngine.ShowSuccessMessage("Default Wallet Saved Successfully");
                    ShowWallet(walletResult.Result);
                }
                else
                    OASISErrorHandling.HandleError(ref selectedWallet, $"Error saving default wallet. Reason: {walletResult.Message}");
            }

            return selectedWallet;
        }

        //TODO: Improve later so can search by title, desc, public key, wallet address etc (may be a SearchWallet function)
        public async Task<OASISResult<IProviderWallet>> FindWalletAsync(string message, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = await ListProviderWalletsForBeamedInAvatarAsync(providerType, true);
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
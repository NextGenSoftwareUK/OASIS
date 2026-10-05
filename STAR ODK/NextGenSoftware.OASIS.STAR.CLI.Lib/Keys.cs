using System.Collections.Generic;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class Keys
    {
        public async Task<OASISResult<IProviderWallet>> LinkProviderKeyToBeamedInAvatarWalletAsync(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            int selection = CLIEngine.GetValidInputForInt("Do you wish to link a private key, public key, wallet address or generate a keyvalue pair (and wallet address) and then link? Press 1 for private key, 2 for public key, 3 for wallet address or 4 to generate and link.", true, 1, 4);
            
            switch (selection)
            {
                case 1:
                    return await LinkProviderPrivateKeyToBeamedInAvatarWalletAsync(providerToLoadAvatarFrom);

                case 2:
                    return await LinkProviderPublicKeyToBeamedInAvatarWalletAsync(providerToLoadAvatarFrom);

                case 3:
                    return await LinkProviderWalletAddressToBeamedInAvatarWalletAsync(providerToLoadAvatarFrom);

                case 4:
                    GenerateKeyPairWithWalletAddressAndLinkProviderKeysToBeamedInAvatarWallet();
                    break;
            }

            return null;
        }

        public async Task<OASISResult<IProviderWallet>> LinkProviderWalletAddressToBeamedInAvatarWalletAsync(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            Guid walletId = Guid.Empty;
            ProviderType walletProvider = ProviderType.None;

            if (CLIEngine.GetConfirmation("Do you have an existing wallet you wish to link to? (if you select 'N' a new wallet will be created for you)"))
            {
                Console.WriteLine("");
                OASISResult<IProviderWallet> walletResult = await STARCLI.Wallets.FindWalletAsync("Enter the number of the wallet you wish to add the wallet address to:");

                if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                {
                    walletId = walletResult.Result.Id;
                    walletProvider = walletResult.Result.ProviderType;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured finding wallet. Reason: {walletResult.Message}");
            }
            else
                Console.WriteLine("");

            string walletAddress = CLIEngine.GetValidInput("Enter the wallet address you wish to link to your wallet: ");

            if (walletProvider == ProviderType.None)
            {
                object providerObj = CLIEngine.GetValidInputForEnum("Enter the provider (chain) you wish to link the wallet address to in your wallet: ", typeof(ProviderType));

                if (providerObj != null)
                {
                    if (providerObj.ToString() == "exit")
                    {
                        result.Message = "User Exited";
                        return result;
                    }

                    walletProvider = (ProviderType)providerObj;
                }
            }

            if (walletProvider != ProviderType.None)
            {
                CLIEngine.ShowWorkingMessage("Linking Wallet Address...");
                ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
                result = STAR.OASISAPI.Keys.LinkProviderWalletAddressToAvatarById(walletId, STAR.BeamedInAvatar.Id, walletProvider, walletAddress, providerToLoadAvatarFrom);
                ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = false;

                if (result != null && result.Result != null && !result.IsError)
                {
                    CLIEngine.ShowSuccessMessage($"Wallet Address Linked Successfully.");
                    Console.WriteLine("");
                    STARCLI.Wallets.ShowWallet(result.Result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured linking wallet address: Reason: {result.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, "ProviderType is None!");

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> LinkProviderPublicKeyToBeamedInAvatarWalletAsync(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            Guid walletId = Guid.Empty;
            ProviderType walletProvider = ProviderType.None;

            if (CLIEngine.GetConfirmation("Do you have an existing wallet you wish to link to? (if you select 'N' a new wallet will be created for you)"))
            {
                Console.WriteLine("");
                OASISResult<IProviderWallet> walletResult = await STARCLI.Wallets.FindWalletAsync("Enter the number of the wallet you wish to add the public key to:");

                if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                {
                    walletId = walletResult.Result.Id;
                    walletProvider = walletResult.Result.ProviderType;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured finding wallet. Reason: {walletResult.Message}");
            }
            else
                Console.WriteLine("");

            string publicKey = CLIEngine.GetValidInput("Enter the public key you wish to link to your wallet: ");
            string walletAddress = "";

            if (CLIEngine.GetConfirmation("Do you know the wallet address? If not then we will attempt to calculate it from the public key"))
                walletAddress = CLIEngine.GetValidInput("Enter the wallet address you wish to link to your wallet: ");

            if (walletProvider == ProviderType.None)
            {
                object providerObj = CLIEngine.GetValidInputForEnum("Enter the provider (chain) you wish to link the public key to in your wallet: ", typeof(ProviderType));

                if (providerObj != null)
                {
                    if (providerObj.ToString() == "exit")
                    {
                        result.Message = "User Exited";
                        return result;
                    }

                    walletProvider = (ProviderType)providerObj;
                }
            }

            if (walletProvider != ProviderType.None)
            {
                CLIEngine.ShowWorkingMessage("Linking Public Key...");
                ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
                result = STAR.OASISAPI.Keys.LinkProviderPublicKeyToAvatarById(walletId, STAR.BeamedInAvatar.Id, walletProvider, publicKey, walletAddress, null, true, providerToLoadAvatarFrom);
                ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = false;

                if (result != null && result.Result != null && !result.IsError)
                {
                    CLIEngine.ShowSuccessMessage($"Public Key Linked Successfully.");
                    Console.WriteLine("");
                    STARCLI.Wallets.ShowWallet(result.Result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured linking public key: Reason: {result.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, "ProviderType is None!");

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> LinkProviderPrivateKeyToBeamedInAvatarWalletAsync(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            Guid walletId = Guid.Empty;
            ProviderType walletProvider = ProviderType.None;

            if (CLIEngine.GetConfirmation("Do you have an existing wallet you wish to link to? (if you select 'N' a new wallet will be created for you)"))
            {
                OASISResult<IProviderWallet> walletResult = await STARCLI.Wallets.FindWalletAsync("Enter the number of the wallet you wish to add the public key to:");

                if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                {
                    walletId = walletResult.Result.Id;
                    walletProvider = walletResult.Result.ProviderType;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured finding wallet. Reason: {walletResult.Message}");
            }
            else
                Console.WriteLine("");

            string publicKey = CLIEngine.GetValidInput("Enter the private key you wish to link to your wallet: ", addLineBefore: true);

            if (walletProvider == ProviderType.None)
            {
                object providerObj = CLIEngine.GetValidInputForEnum("Enter the provider (chain) you wish to link the private key to in your wallet: ", typeof(ProviderType));

                if (providerObj != null)
                {
                    if (providerObj.ToString() == "exit")
                    {
                        result.Message = "User Exited";
                        return result;
                    }

                    walletProvider = (ProviderType)providerObj;
                }
            }

            if (walletProvider != ProviderType.None)
            {
                ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
                CLIEngine.ShowWorkingMessage("Linking Private Key...");
                result = STAR.OASISAPI.Keys.LinkProviderPrivateKeyToAvatarById(walletId, STAR.BeamedInAvatar.Id, walletProvider, publicKey, true, true, providerToLoadAvatarFrom);
                ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = false;

                if (result != null && result.Result != null && !result.IsError)
                {
                    CLIEngine.ShowSuccessMessage($"Private Key Linked Successfully.");
                    Console.WriteLine("");
                    STARCLI.Wallets.ShowWallet(result.Result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured linking Private Key: Reason: {result.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, "ProviderType is None!");

            return result;
        }

        //public async Task<OASISResult<List<Key>> ListAllKeysForBeamedInAvatar(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        //{
        //    OASISResult<List<Key>> keys = await STAR.OASISAPI.Keys.GetAllKeysAsync(STAR.BeamedInAvatar.Id);
        //}

        public void ListAllProviderKeysForBeamedInAvatar(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            int selection = CLIEngine.GetValidInputForInt("Do you wish to list all private, public, wallet address, keypair (private & public) or unique storage keys? Press 1 for private, 2 for public, 3 for wallet address, 4 for keypair or 5 for storage.", true, 1, 5);
            
            switch (selection)
            {
                case 1:
                    ListAllProviderPrivateKeysForBeamedInAvatar(providerToLoadAvatarFrom);
                    break;

                case 2:
                    ListAllProviderPublicKeysForBeamedInAvatar(providerToLoadAvatarFrom);
                    break;

                case 3:
                    ListAllProviderWalletAddressesForBeamedInAvatar(providerToLoadAvatarFrom);
                    break;

                case 4:
                    ListAllProviderKeyPairsForBeamedInAvatar(providerToLoadAvatarFrom);
                    break;

                case 5:
                    ListAllProviderUniqueStorageKeysForBeamedInAvatar(providerToLoadAvatarFrom);
                    break;
            }
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> ListAllProviderWalletAddressesForBeamedInAvatar(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading Wallet Addresses...");
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
            OASISResult<Dictionary<ProviderType, List<string>>> keysResult = STAR.OASISAPI.Keys.GetAllProviderWalletAddressesForAvatarById(STAR.BeamedInAvatar.Id, providerToLoadAvatarFrom);
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = false;

            if (keysResult != null && keysResult.Result != null && !keysResult.IsError)
                ShowKeys(keysResult.Result);
            else
                OASISErrorHandling.HandleError(ref keysResult, $"Error occured loading wallet addresses. Reason: {keysResult.Message}");

            return keysResult;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> ListAllProviderPublicKeysForBeamedInAvatar(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading Public Keys...");
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
            OASISResult<Dictionary<ProviderType, List<string>>> keysResult = STAR.OASISAPI.Keys.GetAllProviderPublicKeysForAvatarById(STAR.BeamedInAvatar.Id, providerToLoadAvatarFrom);
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = false;

            if (keysResult != null && keysResult.Result != null && !keysResult.IsError)
                ShowKeys(keysResult.Result);
            else
                OASISErrorHandling.HandleError(ref keysResult, $"Error occured loading public keys. Reason: {keysResult.Message}");

            return keysResult;
        }

        public OASISResult<Dictionary<ProviderType, List<KeyPair>>> ListAllProviderKeyPairsForBeamedInAvatar(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading KeyPairs...");
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
            OASISResult<Dictionary<ProviderType, List<KeyPair>>> keysResult = STAR.OASISAPI.Keys.GetAllProviderKeyPairsForAvatarById(STAR.BeamedInAvatar.Id, providerToLoadAvatarFrom);
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = false;

            if (keysResult != null && keysResult.Result != null && !keysResult.IsError)
                ShowKeys(keysResult.Result);
            else
                OASISErrorHandling.HandleError(ref keysResult, $"Error occured loading public & private keys. Reason: {keysResult.Message}");

            return keysResult;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> ListAllProviderPrivateKeysForBeamedInAvatar(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading Private Keys...");
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
            OASISResult<Dictionary<ProviderType, List<string>>> keysResult = STAR.OASISAPI.Keys.GetAllProviderPrivateKeysForAvatarById(STAR.BeamedInAvatar.Id, providerToLoadAvatarFrom);
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = false;

            if (keysResult != null && keysResult.Result != null && !keysResult.IsError)
                ShowKeys(keysResult.Result);
            else
                OASISErrorHandling.HandleError(ref keysResult, $"Error occured loading private keys. Reason: {keysResult.Message}");

            return keysResult;
        }

        public OASISResult<Dictionary<ProviderType, string>> ListAllProviderUniqueStorageKeysForBeamedInAvatar(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading Provider Unique Storage Keys...");
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
            OASISResult<Dictionary<ProviderType, string>> keysResult = STAR.OASISAPI.Keys.GetAllProviderUniqueStorageKeysForAvatarById(STAR.BeamedInAvatar.Id, providerToLoadAvatarFrom);
            ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = false;

            if (keysResult != null && keysResult.Result != null && !keysResult.IsError)
            {
                foreach (ProviderType providerType in keysResult.Result.Keys)
                    CLIEngine.ShowMessage(string.Concat("Provider ", Enum.GetName(typeof(ProviderType), providerType).PadRight(20), ": ", keysResult.Result[providerType]));
            }
            else
                OASISErrorHandling.HandleError(ref keysResult, $"Error occured loading private keys. Reason: {keysResult.Message}");

            return keysResult;
        }

        public OASISResult<Guid> GetAvatarIdForProviderPublicKey(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            string publicKey = CLIEngine.GetValidInput("Enter the public key: ");

            CLIEngine.ShowWorkingMessage("Getting Avatar Id...");
            OASISResult<Guid> avatarId = STAR.OASISAPI.Keys.GetAvatarIdForProviderPublicKey(publicKey, providerToLoadAvatarFrom);

            if (avatarId != null && avatarId.Result != null && !avatarId.IsError)
                CLIEngine.ShowMessage($"Avatar Id: {avatarId.Result}");
            else
                OASISErrorHandling.HandleError(ref avatarId, $"Error occured loading avatar id. Reason: {avatarId.Message}");

            return avatarId;
        }

        public OASISResult<string> GetAvatarUsernameForProviderPublicKey(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            string publicKey = CLIEngine.GetValidInput("Enter the public key: ");

            CLIEngine.ShowWorkingMessage("Getting Avatar Username...");
            OASISResult<string> avatarUsername = STAR.OASISAPI.Keys.GetAvatarUsernameForProviderPublicKey(publicKey, providerToLoadAvatarFrom);

            if (avatarUsername != null && avatarUsername.Result != null && !avatarUsername.IsError)
                CLIEngine.ShowMessage($"Avatar Username: {avatarUsername.Result}");
            else
                OASISErrorHandling.HandleError(ref avatarUsername, $"Error occured loading avatar username. Reason: {avatarUsername.Message}");

            return avatarUsername;
        }

        public OASISResult<KeyPair> GenerateKeyPair(ProviderType providerType = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Generating KeyPair...");
            OASISResult<KeyPair> keyPair = STAR.OASISAPI.Keys.GenerateKeyPair(providerType);

            if (keyPair != null && keyPair.Result != null && !keyPair.IsError)
            {
                CLIEngine.ShowMessage($"Public Key: {keyPair.Result.PublicKey}");
                CLIEngine.ShowMessage($"Private Key: {keyPair.Result.PrivateKey}");
            }
            else
                OASISErrorHandling.HandleError(ref keyPair, $"Error occured generating keypair. Reason: {keyPair.Message}");

            return keyPair;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPairWithWallet(ProviderType providerType = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Generating KeyPair With Wallet Address...");
            OASISResult<IKeyPairAndWallet> keyPairResult = STAR.OASISAPI.Keys.GenerateKeyPairWithWalletAddress(providerType);

            if (keyPairResult != null && keyPairResult.Result != null && !keyPairResult.IsError)
            {
                CLIEngine.ShowMessage($"Wallet Address Legacy: {keyPairResult.Result.WalletAddressLegacy}");
                CLIEngine.ShowMessage($"Wallet Address: SegwitP2SH {keyPairResult.Result.WalletAddressSegwitP2SH}");
                CLIEngine.ShowMessage($"Public Key: {keyPairResult.Result.PublicKey}");
                CLIEngine.ShowMessage($"Private Key: {keyPairResult.Result.PrivateKey}");
            }
            else
                OASISErrorHandling.HandleError(ref keyPairResult, $"Error occured generating keypair. Reason: {keyPairResult.Message}");

            return keyPairResult;
        }

        public OASISResult<IProviderWallet> GenerateKeyPairWithWalletAddressAndLinkProviderKeysToBeamedInAvatarWallet(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            object providerObj = CLIEngine.GetValidInputForEnum("Enter the provider (chain) you wish to link the keypair and wallet address to in your wallet: ", typeof(ProviderType));

            if (providerObj != null)
            {
                if (providerObj.ToString() == "exit")
                {
                    result.Message = "User Exited";
                    return result;
                }

                CLIEngine.ShowWorkingMessage("Generating KeyPair With Wallet Address...");
                ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = true;
                result = STAR.OASISAPI.Keys.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarById(STAR.BeamedInAvatar.Id, (ProviderType)providerObj, true, true, true, true, providerToLoadAvatarFrom);
                ProviderManager.Instance.SupressConsoleLoggingWhenSwitchingProviders = false;

                if (result != null && result.Result != null && !result.IsError)
                {
                    CLIEngine.ShowSuccessMessage($"KeyPair & Wallet Address Successfully Generated.");
                    Console.WriteLine("");
                    STARCLI.Wallets.ShowWallet(result.Result);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured generating keypair. Reason: {result.Message}");
            }

            return result;
        }

        private void ShowKeys(Dictionary<ProviderType, List<string>> keys)
        {
            foreach (ProviderType providerType in keys.Keys)
            {
                CLIEngine.ShowMessage($"{Enum.GetName(typeof(ProviderType), providerType)}:");

                foreach (string key in keys[providerType])
                    CLIEngine.ShowMessage(key, false);
            }
        }

        private void ShowKeys(Dictionary<ProviderType, List<KeyPair>> keys)
        {
            foreach (ProviderType providerType in keys.Keys)
            {
                CLIEngine.ShowMessage($"{Enum.GetName(typeof(ProviderType), providerType)}");
                Console.WriteLine("");

                foreach (KeyPair keyPair in keys[providerType])
                    CLIEngine.ShowMessage(string.Concat("Public: ", keyPair.PublicKey != null ? keyPair.PublicKey.PadRight(65) : "".PadRight(65), "Private: ", keyPair.PrivateKey), false);
            }
        }
    }
}
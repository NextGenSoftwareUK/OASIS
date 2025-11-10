using System.Collections.Generic;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class Keys
    {
        public async Task<OASISResult<IProviderWallet>> LinkProviderPublicKeyToBeamedInAvatarWalletAsync(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            Guid walletId = Guid.Empty;

            if (CLIEngine.GetConfirmation("Do you have an existing wallet you wish to link to? (if you select 'N' a new wallet will be created for you)"))
            {
                OASISResult<IProviderWallet> walletResult = await STARCLI.Wallets.FindWalletAsync("Enter the number of the wallet you wish to add the public key to:");

                if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                    walletId = walletResult.Result.Id;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured finding wallet. Reason: {walletResult.Message}");
            }

            string publicKey = CLIEngine.GetValidInput("Enter the public key you wish to link to your wallet: ");
            object providerObj = CLIEngine.GetValidInputForEnum("Enter the provider (chain) you wish to link the public key to in your wallet: ", typeof(ProviderType));

            if (providerObj != null)
            {
                if (providerObj.ToString() == "exit")
                {
                    result.Message = "User Exited";
                    return result;
                }

                CLIEngine.ShowWorkingMessage("Linking Public Key...");
                result = STAR.OASISAPI.Keys.LinkProviderPublicKeyToAvatarById(walletId, STAR.BeamedInAvatar.Id, (ProviderType)providerObj, publicKey, providerToLoadAvatarFrom);

                if (result != null && result.Result != null && !result.IsError)
                {
                    CLIEngine.ShowSuccessMessage("Public Key Linked Successfully");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured linking public key: Reason: {result.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IProviderWallet>> LinkProviderPrivateKeyToBeamedInAvatarWalletAsync(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            Guid walletId = Guid.Empty;

            if (CLIEngine.GetConfirmation("Do you have an existing wallet you wish to link to? (if you select 'N' a new wallet will be created for you)"))
            {
                OASISResult<IProviderWallet> walletResult = await STARCLI.Wallets.FindWalletAsync("Enter the number of the wallet you wish to add the public key to:");

                if (walletResult != null && walletResult.Result != null && !walletResult.IsError)
                    walletId = walletResult.Result.Id;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured finding wallet. Reason: {walletResult.Message}");
            }

            string publicKey = CLIEngine.GetValidInput("Enter the private key you wish to link to your wallet: ");
            object providerObj = CLIEngine.GetValidInputForEnum("Enter the provider (chain) you wish to link the private key to in your wallet: ", typeof(ProviderType));

            if (providerObj != null)
            {
                if (providerObj.ToString() == "exit")
                {
                    result.Message = "User Exited";
                    return result;
                }

                CLIEngine.ShowWorkingMessage("Linking Private Key...");
                result = STAR.OASISAPI.Keys.LinkProviderPrivateKeyToAvatarById(walletId, STAR.BeamedInAvatar.Id, (ProviderType)providerObj, publicKey, providerToLoadAvatarFrom);

                if (result != null && result.Result != null && !result.IsError)
                {
                    CLIEngine.ShowSuccessMessage("Private Key Linked Successfully");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured linking Private Key: Reason: {result.Message}");
            }

            return result;
        }

        //public async Task<OASISResult<List<Key>> ListAllKeysForBeamedInAvatar(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        //{
        //    OASISResult<List<Key>> keys = await STAR.OASISAPI.Keys.GetAllKeysAsync(STAR.BeamedInAvatar.Id);
        //}

        public OASISResult<Dictionary<ProviderType, List<string>>> ListAllProviderPublicKeysForBeamedInAvatar(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading Public Keys...");
            OASISResult<Dictionary<ProviderType, List<string>>> keysResult = STAR.OASISAPI.Keys.GetAllProviderPublicKeysForAvatarById(STAR.BeamedInAvatar.Id, providerToLoadAvatarFrom);

            if (keysResult != null && keysResult.Result != null && !keysResult.IsError)
                ShowKeys(keysResult.Result);
            else
                OASISErrorHandling.HandleError(ref keysResult, $"Error occured loading public keys. Reason: {keysResult.Message}");

            return keysResult;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> ListAllProviderPrivateKeysForBeamedInAvatar(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading Private Keys...");
            OASISResult<Dictionary<ProviderType, List<string>>> keysResult = STAR.OASISAPI.Keys.GetAllProviderPrivateKeysForAvatarById(STAR.BeamedInAvatar.Id, providerToLoadAvatarFrom);

            if (keysResult != null && keysResult.Result != null && !keysResult.IsError)
                ShowKeys(keysResult.Result);
            else
                OASISErrorHandling.HandleError(ref keysResult, $"Error occured loading private keys. Reason: {keysResult.Message}");

            return keysResult;
        }

        public OASISResult<Dictionary<ProviderType, string>> ListAllProviderUniqueStorageKeysForBeamedInAvatar(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            CLIEngine.ShowWorkingMessage("Loading Provider Unique Storage Keys...");
            OASISResult<Dictionary<ProviderType, string>> keysResult = STAR.OASISAPI.Keys.GetAllProviderUniqueStorageKeysForAvatarById(STAR.BeamedInAvatar.Id, providerToLoadAvatarFrom);

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

        public OASISResult<KeyPair> GenerateKeyPairAndLinkProviderKeysToBeamedInAvatarWallet(ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<KeyPair> result = new OASISResult<KeyPair>();
            object providerObj = CLIEngine.GetValidInputForEnum("Enter the provider (chain) you wish to link the public key to in your wallet: ", typeof(ProviderType));

            if (providerObj != null)
            {
                if (providerObj.ToString() == "exit")
                {
                    result.Message = "User Exited";
                    return result;
                }

                CLIEngine.ShowWorkingMessage("Generating KeyPair...");
                result = STAR.OASISAPI.Keys.GenerateKeyPairAndLinkProviderKeysToAvatarById(STAR.BeamedInAvatar.Id, (ProviderType)providerObj, true, true, providerToLoadAvatarFrom);

                if (result != null && result.Result != null && !result.IsError)
                {
                    CLIEngine.ShowMessage($"Public Key: {result.Result.PublicKey}");
                    CLIEngine.ShowMessage($"Private Key: {result.Result.PrivateKey}");
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
                CLIEngine.ShowMessage($"Provider {Enum.GetName(typeof(ProviderType), providerType)}:");

                foreach (string key in keys[providerType])
                    CLIEngine.ShowMessage(key, false);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Cryptography.ECDSA;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Rijndael256;
using static NextGenSoftware.Utilities.KeyHelper;
using KeyPair = NextGenSoftware.OASIS.API.Core.Objects.KeyPair;
using Rijndael = Rijndael256.Rijndael;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class KeyManager : OASISManager
    {
        public OASISResult<IProviderWallet> GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarById(Guid avatarId, ProviderType providerTypeToLinkTo, bool showWalletAddress = true, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, false, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatar(avatarResult.Result, providerTypeToLinkTo, showWalletAddress, showPublicKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarById loading avatar for id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarById for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
            }

            return result;
        }

        // Could be used as the public key for private/public key pairs. Could also be a username/accountname/unique id/etc, etc.
        public OASISResult<IProviderWallet> GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByUsername(string username, ProviderType providerTypeToLinkTo, bool showWalletAddress = true, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, true, false, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatar(avatarResult.Result, providerTypeToLinkTo, showWalletAddress, showPublicKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByUsername loading avatar for username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in GenerateKeyPairAndLinkProviderKeysToAvatarByUsername for username {username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByEmail(string email, ProviderType providerTypeToLinkTo, bool showWalletAddress = true, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, true, false, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatar(avatarResult.Result, providerTypeToLinkTo, showWalletAddress, showPublicKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByEmail loading avatar for email {email}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByEmail for email {email} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatar(IAvatar avatar, ProviderType providerTypeToLinkTo, bool showWalletAddress = true, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            if (avatar == null)
            {
                OASISErrorHandling.HandleError(ref result, "An error occured in GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatar. The avatar passed in is null.");
                return result;
            }

            try
            {
                IKeyPairAndWallet keyPair =  GenerateKeyValuePairAndWalletAddress();

                if (keyPair != null)
                {
                    //Backup the wallets before the private keys get blanked out in LinkProviderPublicKeyToAvatar.
                    Dictionary<ProviderType, List<IProviderWallet>> wallets = WalletManager.Instance.CopyProviderWallets(avatar.ProviderWallets);
                    OASISResult<IProviderWallet> publicKeyResult = LinkProviderPublicKeyToAvatar(Guid.Empty, avatar, providerTypeToLinkTo, keyPair.PublicKey, keyPair.WalletAddressLegacy, keyPair.WalletAddressSegwitP2SH, showSecretRecoveryWords, providerToLoadAvatarFrom);

                    if (!publicKeyResult.IsError)
                    {
                        //Need to restore wallet private keys because the LinkProviderPublicKeyToAvatar calls Save() on the avatar object, which then blanks all private keys for extra security.
                        foreach (ProviderType pType in avatar.ProviderWallets.Keys)
                        {
                            foreach (IProviderWallet wallet in avatar.ProviderWallets[pType])
                            {
                                //if (wallets.ContainsKey(pType) && wallets[pType].Any(x => x.WalletId == wallet.Id))
                                if (wallets.ContainsKey(pType))
                                {
                                    IProviderWallet backedUpWallet = wallets[pType].FirstOrDefault(x => x.WalletId == wallet.Id);

                                    if (backedUpWallet != null)
                                        wallet.PrivateKey = backedUpWallet.PrivateKey;
                                }
                            }
                        }

                        //avatar.ProviderWallets = wallets;

                        OASISResult<IProviderWallet> privateKeyResult = LinkProviderPrivateKeyToAvatar(publicKeyResult.Result.Id, avatar, providerTypeToLinkTo, keyPair.PrivateKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);

                        if (!privateKeyResult.IsError)
                        {
                            result.Message = "KeyPair & Wallet Address Generated & Linked To Avatar.";
                            result.Result = privateKeyResult.Result;

                            if (!showWalletAddress)
                                result.Result.WalletAddress = null;

                            if (!showPublicKey)
                                result.Result.PublicKey = null;

                            if (!showPrivateKey)
                                result.Result.PrivateKey = null;
                            else
                                result.Result.PrivateKey = keyPair.PrivateKey; //Need to do this because save blanks private keys when saving for extra security.
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatar whilst linking the generated private key to the avatar {avatar.Id} - {avatar.Username}. Reason: {privateKeyResult.Message}", privateKeyResult.DetailedMessage);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatar whilst linking the generated public key to the avatar {avatar.Id} - {avatar.Username}. Reason: {publicKeyResult.Message}", publicKeyResult.DetailedMessage);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatar for avatar {avatar.Id} {avatar.Username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
            }

            return result;
        }

        // Private key for a public/private keypair.
        public OASISResult<IProviderWallet> LinkProviderPrivateKeyToAvatarById(Guid walletId, Guid avatarId, ProviderType providerTypeToLinkTo, string providerPrivateKey, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, false, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LinkProviderPrivateKeyToAvatar(walletId, avatarResult.Result, providerTypeToLinkTo, providerPrivateKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPrivateKeyToAvatar loading avatar for id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in LinkPrivateProviderKeyToAvatar for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerPrivateKey}: {ex.Message}");
            }

            return result;
        }

        // Private key for a public/private keypair.
        public OASISResult<IProviderWallet> LinkProviderPrivateKeyToAvatarByUsername(Guid walletId, string username, ProviderType providerTypeToLinkTo, string providerPrivateKey, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, true, false, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    //OASISResult<IAvatar> walletsResult = WalletManager.Instance.LoadProviderWalletsForAvatarByUsername(username, )
                    result = LinkProviderPrivateKeyToAvatar(walletId, avatarResult.Result, providerTypeToLinkTo, providerPrivateKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPrivateKeyToAvatar loading avatar for username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in LinkPrivateProviderKeyToAvatar for avatar {username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerPrivateKey}: {ex.Message}");
            }

            return result;
        }

        // Private key for a public/private keypair.
        public OASISResult<IProviderWallet> LinkProviderPrivateKeyToAvatarByEmail(Guid walletId, string email, ProviderType providerTypeToLinkTo, string providerPrivateKey, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, true, false, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LinkProviderPrivateKeyToAvatar(walletId, avatarResult.Result, providerTypeToLinkTo, providerPrivateKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPrivateKeyToAvatarByEmail loading avatar for email {email}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPrivateKeyToAvatarByEmail for avatar {email} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerPrivateKey}: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> LinkProviderPrivateKeyToAvatar(Guid walletId, IAvatar avatar, ProviderType providerTypeToLinkTo, string providerPrivateKey, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            if (avatar == null)
            {
                OASISErrorHandling.HandleError(ref result, "The avatar is required. Please provide a valid avatar object.");
                return result;
            }
            try
            {
                if (!avatar.ProviderWallets.ContainsKey(providerTypeToLinkTo))
                    avatar.ProviderWallets.Add(providerTypeToLinkTo, new List<IProviderWallet>());

                foreach (ProviderType proType in avatar.ProviderWallets.Keys)
                {
                    foreach (IProviderWallet proWallet in avatar.ProviderWallets[proType])
                    {
                        if (proWallet != null && !string.IsNullOrEmpty(proWallet.PrivateKey))
                            proWallet.PrivateKey = Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(proWallet.PrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                    }
                }

                IProviderWallet wallet = avatar.ProviderWallets[providerTypeToLinkTo].FirstOrDefault(x => x.PrivateKey == providerPrivateKey);

                foreach (ProviderType proType in avatar.ProviderWallets.Keys)
                {
                    foreach (IProviderWallet proWallet in avatar.ProviderWallets[proType])
                    {
                        if (proWallet != null && !string.IsNullOrEmpty(proWallet.PrivateKey))
                            proWallet.PrivateKey = PasswordEncryptionHelper.WrapQuantumLayer(Rijndael.Encrypt(proWallet.PrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256), OASISDNA.OASIS.Security.OASISProviderPrivateKeys);
                    }
                }

                if (wallet == null)
                {
                    if (walletId == Guid.Empty)
                    {
                        ProviderWallet newWallet = new ProviderWallet()
                        {
                            WalletId = Guid.NewGuid(),
                            //AvatarId = avatar.Id,
                            CreatedByAvatarId = avatar.Id,
                            CreatedDate = DateTime.Now,
                            WalletAddress = WalletAddressHelper.PrivateKeyToAddress(providerPrivateKey), //TODO: Need to calucalte the walletAddress from the PublicKey!
                            ProviderType = providerTypeToLinkTo,
                            SecretRecoveryPhrase = PasswordEncryptionHelper.WrapQuantumLayer(Rijndael.Encrypt(string.Join(" ", new Mnemonic(Wordlist.English, WordCount.Twelve).Words), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256), OASISDNA.OASIS.Security.OASISProviderPrivateKeys),
                            PrivateKey = PasswordEncryptionHelper.WrapQuantumLayer(Rijndael.Encrypt(providerPrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256), OASISDNA.OASIS.Security.OASISProviderPrivateKeys)
                        };

                        result.Result = newWallet;

                        // If any default wallet exist in avatar provider wallet? if not, make current/first one wallet as default
                        if (!avatar.ProviderWallets[providerTypeToLinkTo].Any(x => x.IsDefaultWallet) && avatar.ProviderWallets[providerTypeToLinkTo].Count == 0)
                        {
                            newWallet.IsDefaultWallet = true;
                        }
                        avatar.ProviderWallets[providerTypeToLinkTo].Add(newWallet);
                    }
                    else
                    {
                        wallet = avatar.ProviderWallets[providerTypeToLinkTo].FirstOrDefault(x => x.Id == walletId);

                        if (wallet != null)
                        {
                            wallet.PrivateKey = PasswordEncryptionHelper.WrapQuantumLayer(Rijndael.Encrypt(providerPrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256), OASISDNA.OASIS.Security.OASISProviderPrivateKeys);
                            wallet.ModifiedByAvatarId = avatar.Id;
                            wallet.ModifiedDate = DateTime.Now;
                            result.Result = wallet;
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, $"The Wallet with ID {walletId} was not found. Please pass in a valid ID or leave empty if you wish to create a new wallet for this provider key.");
                            return result;
                        }
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"The Private ProviderKey is already linked to the wallet {wallet.Id} belonging to avatar {avatar.Id} {avatar.Username}. The ProviderKey must be unique per provider.");
                    return result;
                }

                string privateKey = result.Result.PrivateKey;
                string secret = result.Result.SecretRecoveryPhrase;

                // Could save the wallets without having to save the full avatar but then would need to add additional looping code to go through all providers looking for only local storage ones.
                // But we STILL need to save the wallets (without private keys) for all non-local storage providers so not much point doing it seperatley and just call the Save method above... ;-)
                // UPDATE: BUT the Save method above currently does not save private keys to local storage if Auto-Replicate is switched off so better to manually save them below just in case... :)
                // Was considering moving this method into the Save above but then it would do extra un-necessary processing/logic EVERY time the avatar is saved even when the keys have not changed so best to just manually save when we KNOW they have changed (here). ;-)
                // UPDATE: Auto-replicate in Save above NO longer saves to local storage in case the private kets get blanked out by a avatar object loaded from a non local provider.
                // TODO: So now we need to add auto-replicate to all local storage providers for method below... DONE ;-)
                OASISResult<bool> walletsResult = WalletManager.Instance.SaveProviderWalletsForAvatarById(avatar.Id, avatar.ProviderWallets);

                //The only issue is when a avatar is loaded from a non local storage provider how it will know the difference between that and if the user had deleted the private keys?
                //TODO: COME BACK TO THE LINE ABOVE... AS I RECALL I WORKED OUT THERE WAS NO WAY IT WOULD WORK WITHOUT SAVING THE WALLETS (WITH PRIVATE KEYS) TO A LOCAL STORAGE PROVIDER OUTSIDE OF THE AVATAR OBJECT AS IT CURRENTLY DOES...
                //The Wallet Save/Load needs to be de-coupled from the Avatar Save/Load as it currently is. Well actually the Save will save wallets locally during auto-replication only BUT will load wallets from localStorage on Avatar load if loadPrivateKeys param is set to true.

                if (!walletsResult.IsError && walletsResult.Result)
                {
                    //Will save private keys (along with the rest of the wallet) to local storage providers only and wallets minus the private keys to the other non local storage providers.
                    //This way the private keys (and rest of the wallet) can be auto-replicated to other local storage providers and the wallets minus the private keys will be auto-replicated to other non storage providers.
                    OASISResult<IAvatar> avatarResult = avatar.Save();

                    if (!avatarResult.IsError && avatarResult.Result != null)
                    {
                        try
                        {
                            if (showPrivateKey)
                                result.Result.PrivateKey = Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(privateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                        }
                        catch (Exception e)
                        {

                        }

                        try
                        {
                            if (showSecretRecoveryWords)
                                result.Result.SecretRecoveryPhrase = Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(secret, OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                        }
                        catch (Exception e)
                        {

                        }
                        
                        result.IsSaved = true;
                        result.Message = $"Private key was successfully linked to wallet {result.Result} and avatar {avatar.Id} - {avatar.Username} for provider {Enum.GetName(typeof(ProviderType), providerTypeToLinkTo)}";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPrivateKeyToAvatar saving avatar {avatar.Id} - {avatar.Username} for providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPrivateKeyToAvatar saving wallets to local storage for avatar {avatar.Id} - {avatar.Username}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPrivateKeyToAvatar for avatar {avatar.Id} {avatar.Username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
            }

            return result;
        }

    }
}
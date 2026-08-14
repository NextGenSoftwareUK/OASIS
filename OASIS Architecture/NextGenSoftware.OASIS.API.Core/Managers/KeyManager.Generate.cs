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
    public partial class KeyManager
    {
        public OASISResult<IKeyPairAndWallet> GenerateKeyPairWithWalletAddress(ProviderType providerType)
        {
            OASISResult<IKeyPairAndWallet> result = new OASISResult<IKeyPairAndWallet>();

            try
            {
                IOASISProvider provider = ProviderManager.Instance.GetProvider(providerType);
                IOASISBlockchainStorageProvider blockchainStorageProvider = provider as IOASISBlockchainStorageProvider;

                if (blockchainStorageProvider != null)
                {
                    try
                    {
                        result = blockchainStorageProvider.GenerateKeyPair();

                        if (result.IsError || result.Result == null || (result.Result != null && (string.IsNullOrEmpty(result.Result.PublicKey) || string.IsNullOrEmpty(result.Result.PrivateKey) || string.IsNullOrEmpty(result.Result.WalletAddressLegacy))))
                        {
                            result.Result = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                            result.IsError = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error occured in GenerateKeyPairWithWalletAddress calling provider {Enum.GetName(typeof(ProviderType), providerType)} GenerateKeyPair(): {ex.Message}", ex);
                        result.Result = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                    }
                    return result;
                }
                else
                    result.Result = KeyHelper.GenerateKeyValuePairAndWalletAddress();
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in GenerateKeyPairWithWalletAddress for providerType {Enum.GetName(typeof(ProviderType), providerType)}: {e.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairWithWalletAddressAsync(ProviderType providerType)
        {
            OASISResult<IKeyPairAndWallet> result = new OASISResult<IKeyPairAndWallet>();

            try
            {
                IOASISProvider provider = ProviderManager.Instance.GetProvider(providerType);
                IOASISBlockchainStorageProvider blockchainStorageProvider = provider as IOASISBlockchainStorageProvider;

                if (blockchainStorageProvider != null)
                {
                    try
                    {
                        result = await blockchainStorageProvider.GenerateKeyPairAsync();

                        if (result.IsError || result.Result == null || (result.Result != null && (string.IsNullOrEmpty(result.Result.PublicKey) || string.IsNullOrEmpty(result.Result.PrivateKey) || string.IsNullOrEmpty(result.Result.WalletAddressLegacy))))
                        {
                            result.Result = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                            result.IsError = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error occured in GenerateKeyPairWithWalletAddressAsync calling provider {Enum.GetName(typeof(ProviderType), providerType)} GenerateKeyPair(): {ex.Message}", ex);
                        result.Result = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                    }
                    return result;
                }
                else
                    result.Result = KeyHelper.GenerateKeyValuePairAndWalletAddress();
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in GenerateKeyPairWithWalletAddressAsync for providerType {Enum.GetName(typeof(ProviderType), providerType)}: {e.Message}");
            }

            return result;
        }

        //public OASISResult<KeyPair> GenerateKeyPair(ProviderType providerType)
        //{
        //    string prefix = "";

        //    //TODO: Need to look up and add all prefixes here!
        //    switch (providerType)
        //    {
        //        case ProviderType.EthereumOASIS:
        //            prefix = "1";
        //            break;

        //        case ProviderType.SolanaOASIS: 
        //            prefix = "2"; 
        //            break;
        //    }

        //    return GenerateKeyPair(prefix);
        //}

        //public OASISResult<KeyPair> GenerateKeyPair(string prefix)
        //{
        //    OASISResult<KeyPair> result = new OASISResult<KeyPair>(new KeyPair());

        //    //// Create RSA instance
        //    //RSA rsa = RSA.Create();

        //    //// Export keys
        //    //string publicKeyXml = rsa.ToXmlString(false);
        //    //string privateKeyXml = rsa.ToXmlString(true);



        //    byte[] privateKey = Secp256K1Manager.GenerateRandomKey();

        //    OASISResult<string> privateWifResult = GetPrivateWif(privateKey);

        //    if (!privateWifResult.IsError && privateWifResult.Result != null)
        //    {
        //        result.Result.PrivateKey = privateWifResult.Result;

        //        byte[] publicKey = Secp256K1Manager.GetPublicKey(privateKey, true);

        //        OASISResult<string> publicWifResult = GetPublicWif(publicKey, prefix);

        //        if (!publicWifResult.IsError && publicWifResult.Result != null)
        //            result.Result.PublicKey = publicWifResult.Result;
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"Error occured in GenerateKeyPair generating public WIF. Reason: {publicWifResult.Message}");
        //    }
        //    else
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in GenerateKeyPair generating private WIF. Reason: {privateWifResult.Message}");

        //    return result;
        //}

        public OASISResult<bool> ClearCache()
        {
            _avatarIdToProviderUniqueStorageKeyLookup.Clear();
            _avatarIdToProviderPublicKeysLookup.Clear();
            //_avatarIdToProviderPrivateKeyLookup.Clear();
            _avatarUsernameToProviderUniqueStorageKeyLookup.Clear();
            _avatarUsernameToProviderPublicKeysLookup.Clear();
           // _avatarUsernameToProviderPrivateKeyLookup.Clear();
            _avatarEmailToProviderUniqueStorageKeyLookup.Clear();
            _avatarEmailToProviderPublicKeysLookup.Clear();
           // _avatarEmailToProviderPrivateKeyLookup.Clear();
            _providerUniqueStorageKeyToAvatarIdLookup.Clear();
            _providerPublicKeyToAvatarIdLookup.Clear();
            //_providerPrivateKeyToAvatarIdLookup.Clear();
            _providerUniqueStorageKeyToAvatarUsernameLookup.Clear();
            _providerPublicKeyToAvatarUsernameLookup.Clear();
           // _providerPrivateKeyToAvatarUsernameLookup.Clear();
            _providerUniqueStorageKeyToAvatarEmailLookup.Clear();
            _providerPublicKeyToAvatarEmailLookup.Clear();
            //_providerPrivateKeyToAvatarEmailLookup.Clear();
            _providerUniqueStorageKeyToAvatarLookup.Clear();
            _providerPublicKeyToAvatarLookup.Clear();
            //_providerPrivateKeyToAvatarLookup.Clear();

            return new OASISResult<bool>(true) { Message = "Cache Cleared."};
        }

        //TODO: Finish Later.
        //public OASISResult<bool> ClearCacheForAvatarById(Guid id)
        //{
        //    _avatarIdToProviderUniqueStorageKeyLookup[id.ToString()] = null;
        //    _avatarIdToProviderPublicKeysLookup[id.ToString()] = null;
        //    _avatarIdToProviderPrivateKeyLookup[id.ToString()] = null;
        //    _avatarUsernameToProviderUniqueStorageKeyLookup.Clear();
        //    _avatarUsernameToProviderPublicKeysLookup.Clear();
        //    _avatarUsernameToProviderPrivateKeyLookup.Clear();
        //    _avatarEmailToProviderUniqueStorageKeyLookup.Clear();
        //    _avatarEmailToProviderPublicKeysLookup.Clear();
        //    _avatarEmailToProviderPrivateKeyLookup.Clear();
        //    _providerUniqueStorageKeyToAvatarIdLookup.Clear();
        //    _providerPublicKeyToAvatarIdLookup.Clear();
        //    _providerPrivateKeyToAvatarIdLookup.Clear();
        //    _providerUniqueStorageKeyToAvatarUsernameLookup.Clear();
        //    _providerPublicKeyToAvatarUsernameLookup.Clear();
        //    _providerPrivateKeyToAvatarUsernameLookup.Clear();
        //    _providerUniqueStorageKeyToAvatarEmailLookup.Clear();
        //    _providerPublicKeyToAvatarEmailLookup.Clear();
        //    _providerPrivateKeyToAvatarEmailLookup.Clear();
        //    _providerUniqueStorageKeyToAvatarLookup.Clear();
        //    _providerPublicKeyToAvatarLookup.Clear();
        //    _providerPrivateKeyToAvatarLookup.Clear();

        //    return new OASISResult<bool>(true);
        //}







        public OASISResult<IProviderWallet> LinkProviderWalletAddressToAvatarById(Guid walletId, Guid avatarId, ProviderType providerTypeToLinkTo, string walletAddress, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, false, providerToLoadAvatarFrom);

                //TODO Apply same fix in ALL other methods.
                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LinkProviderWalletAddressToAvatar(walletId, avatarResult.Result, providerTypeToLinkTo, walletAddress, providerToLoadAvatarFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderWalletAddressToAvatarById loading avatar for id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderWalletAddressToAvatarById for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and wallet address {walletAddress}: {ex.Message}");
            }

            return result;
        }

        // Could be used as the public key for private/public key pairs. Could also be a username/accountname/unique id/etc, etc.
        public OASISResult<IProviderWallet> LinkProviderWalletAddressToAvatarByUsername(Guid walletId, string username, ProviderType providerTypeToLinkTo, string walletAddress, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, true, false, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LinkProviderWalletAddressToAvatar(walletId, avatarResult.Result, providerTypeToLinkTo, walletAddress, providerToLoadAvatarFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderWalletAddressToAvatarByUsername loading avatar for username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderWalletAddressToAvatarByUsername for avatar {username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and wallet address {walletAddress}: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> LinkProviderWalletAddressToAvatarByEmail(Guid walletId, string email, ProviderType providerTypeToLinkTo, string walletAddress, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, true, false, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LinkProviderWalletAddressToAvatar(walletId, avatarResult.Result, providerTypeToLinkTo, walletAddress, providerToLoadAvatarFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderWalletAddressToAvatarByEmail loading avatar for email {email}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderWalletAddressToAvatarByEmail for avatar {email} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and wallet address {walletAddress}: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> LinkProviderWalletAddressToAvatar(Guid walletId, IAvatar avatar, ProviderType providerTypeToLinkTo, string walletAddress, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
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

                IProviderWallet wallet = avatar.ProviderWallets[providerTypeToLinkTo].FirstOrDefault(x => x.WalletAddress == walletAddress);

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
                            WalletAddress = walletAddress,
                            ProviderType = providerTypeToLinkTo,
                            SecretRecoveryPhrase = PasswordEncryptionHelper.WrapQuantumLayer(Rijndael.Encrypt(string.Join(" ", new Mnemonic(Wordlist.English, WordCount.Twelve).Words), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256), OASISDNA.OASIS.Security.OASISProviderPrivateKeys)
                            //SecretRecoveryPhrase = string.Join(" ", new Mnemonic(Wordlist.English, WordCount.Twelve).Words)
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
                            wallet.WalletAddress = walletAddress;
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
                    OASISErrorHandling.HandleError(ref result, $"The Wallet Address {walletAddress} is already linked to the wallet {wallet.Id} belonging to avatar {avatar.Id} {avatar.Username}. The Wallet Address must be unique per provider.");
                    return result;
                }

                OASISResult<bool> walletsResult = WalletManager.Instance.SaveProviderWalletsForAvatarById(avatar.Id, avatar.ProviderWallets);

                if (!walletsResult.IsError && walletsResult.Result)
                {
                    OASISResult<IAvatar> avatarResult = avatar.Save();

                    if (!avatarResult.IsError && avatarResult.Result != null)
                    {
                        result.IsSaved = true;
                        result.Message = $"Wallet Address {walletAddress} was successfully linked to wallet {result.Result} and avatar {avatar.Id} - {avatar.Username} for provider {Enum.GetName(typeof(ProviderType), providerTypeToLinkTo)}";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderWalletAddressToAvatar saving avatar {avatar.Id} - {avatar.Username} for providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and wallet address {walletAddress}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderWalletAddressToAvatar saving avatar wallets for avatar {avatar.Id} - {avatar.Username} and wallet address {walletAddress}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderWalletAddressToAvatar for avatar {avatar.Id} {avatar.Username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and wallet address {walletAddress}: {ex.Message}");
            }

            return result;
        }

        // Could be used as the public key for private/public key pairs. Could also be a username/accountname/unique id/etc, etc.
        public OASISResult<IProviderWallet> LinkProviderPublicKeyToAvatarById(Guid walletId, Guid avatarId, ProviderType providerTypeToLinkTo, string providerKey, string walletAddress, string walletAddressSegwitP2SH = null, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, false, providerToLoadAvatarFrom);

                //TODO Apply same fix in ALL other methods.
                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LinkProviderPublicKeyToAvatar(walletId, avatarResult.Result, providerTypeToLinkTo, providerKey, walletAddress, walletAddressSegwitP2SH, showSecretRecoveryWords, providerToLoadAvatarFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPublicKeyToAvatarById loading avatar for id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPublicKeyToAvatarById for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerKey}: {ex.Message}");
            }

            return result;
        }

        // Could be used as the public key for private/public key pairs. Could also be a username/accountname/unique id/etc, etc.
        public OASISResult<IProviderWallet> LinkProviderPublicKeyToAvatarByUsername(Guid walletId, string username, ProviderType providerTypeToLinkTo, string providerKey, string walletAddress, string walletAddressSegwitP2SH = null, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, true, false, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LinkProviderPublicKeyToAvatar(walletId, avatarResult.Result, providerTypeToLinkTo, providerKey, walletAddress, walletAddressSegwitP2SH, showSecretRecoveryWords, providerToLoadAvatarFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPublicKeyToAvatarByUsername loading avatar for username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPublicKeyToAvatarByUsername for avatar {username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerKey}: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> LinkProviderPublicKeyToAvatarByEmail(Guid walletId, string email, ProviderType providerTypeToLinkTo, string providerKey, string walletAddress, string walletAddressSegwitP2SH = null, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, true, false, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = LinkProviderPublicKeyToAvatar(walletId, avatarResult.Result, providerTypeToLinkTo, providerKey, walletAddress, walletAddressSegwitP2SH, showSecretRecoveryWords, providerToLoadAvatarFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPublicKeyToAvatarByEmail loading avatar for email {email}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPublicKeyToAvatarByEmail for avatar {email} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerKey}: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> LinkProviderPublicKeyToAvatar(Guid walletId, IAvatar avatar, ProviderType providerTypeToLinkTo, string providerKey, string walletAddress, string walletAddressSegwitP2SH = null, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();
            if (avatar == null)
            {
                OASISErrorHandling.HandleError(ref result, "The avatar is required. Please provide a valid avatar object.");
                return result;
            }
            string secret = "";

            try
            {
                if (!avatar.ProviderWallets.ContainsKey(providerTypeToLinkTo))
                    avatar.ProviderWallets.Add(providerTypeToLinkTo, new List<IProviderWallet>());

                IProviderWallet wallet = avatar.ProviderWallets[providerTypeToLinkTo].FirstOrDefault(x => x.PublicKey == providerKey);

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
                            PublicKey = providerKey,
                            WalletAddress = !string.IsNullOrEmpty(walletAddress) ? walletAddress : WalletAddressHelper.PublicKeyToAddress(providerKey),
                            WalletAddressSegwitP2SH = walletAddressSegwitP2SH,
                            ProviderType = providerTypeToLinkTo,
                            SecretRecoveryPhrase = PasswordEncryptionHelper.WrapQuantumLayer(Rijndael.Encrypt(string.Join(" ", new Mnemonic(Wordlist.English, WordCount.Twelve).Words), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256), OASISDNA.OASIS.Security.OASISProviderPrivateKeys)
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
                            wallet.WalletAddress = !string.IsNullOrEmpty(walletAddress) ? walletAddress : WalletAddressHelper.PublicKeyToAddress(providerKey);
                            wallet.WalletAddressSegwitP2SH = walletAddressSegwitP2SH;
                            wallet.PublicKey = providerKey;
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
                    OASISErrorHandling.HandleError(ref result, $"The Public ProviderKey {providerKey} is already linked to the wallet {wallet.Id} belonging to avatar {avatar.Id} {avatar.Username}. The ProviderKey must be unique per provider.");
                    return result;
                }

                secret = result.Result.SecretRecoveryPhrase;
                OASISResult<bool> walletsResult = WalletManager.Instance.SaveProviderWalletsForAvatarById(avatar.Id, avatar.ProviderWallets);

                if (!walletsResult.IsError && walletsResult.Result)
                {
                    OASISResult<IAvatar> avatarResult = avatar.Save();

                    if (!avatarResult.IsError && avatarResult.Result != null)
                    {
                        try
                        {
                            if (showSecretRecoveryWords)
                                result.Result.SecretRecoveryPhrase = Rijndael.Decrypt(PasswordEncryptionHelper.UnwrapQuantumLayer(secret, OASISDNA.OASIS.Security.OASISProviderPrivateKeys), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                        }
                        catch (Exception e)
                        {

                        }

                        result.IsSaved = true;
                        result.Message = $"Public key {providerKey} was successfully linked to wallet {result.Result} and avatar {avatar.Id} - {avatar.Username} for provider {Enum.GetName(typeof(ProviderType), providerTypeToLinkTo)}";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPublicKeyToAvatar saving avatar {avatar.Id} - {avatar.Username} for providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in LinkProviderPublicKeyToAvatar saving avatar wallets for avatar {avatar.Id} - {avatar.Username} and key {providerKey}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPublicKeyToAvatar for avatar {avatar.Id} {avatar.Username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)} and key {providerKey}: {ex.Message}");
            }

            return result;
        }

        //public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarById(Guid avatarId, ProviderType providerTypeToLinkTo, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        //{
        //    OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

        //    try
        //    {
        //        OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, false, providerToLoadAvatarFrom);

        //        if (!avatarResult.IsError && avatarResult.Result != null)
        //            result = GenerateKeyPairAndLinkProviderKeysToAvatar(avatarResult.Result, providerTypeToLinkTo, showPublicKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatarById loading avatar for id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An unknown error occured in GenerateKeyPairAndLinkProviderKeysToAvatarById for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
        //    }

        //    return result;
        //}

        //// Could be used as the public key for private/public key pairs. Could also be a username/accountname/unique id/etc, etc.
        //public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarByUsername(string username, ProviderType providerTypeToLinkTo, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        //{
        //    OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

        //    try
        //    {
        //        OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, true, false, providerToLoadAvatarFrom);

        //        if (!avatarResult.IsError && avatarResult.Result != null)
        //            result = GenerateKeyPairAndLinkProviderKeysToAvatar(avatarResult.Result, providerTypeToLinkTo, showPublicKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatarByUsername loading avatar for username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An unknown error occured in GenerateKeyPairAndLinkProviderKeysToAvatarByUsername for username {username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
        //    }

        //    return result;
        //}

        //public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarByEmail(string email, ProviderType providerTypeToLinkTo, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        //{
        //    OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

        //    try
        //    {
        //        OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, true, false, providerToLoadAvatarFrom);

        //        if (!avatarResult.IsError && avatarResult.Result != null)
        //            result = GenerateKeyPairAndLinkProviderKeysToAvatar(avatarResult.Result, providerTypeToLinkTo, showPublicKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatarByUsername loading avatar for email {email}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"An unknown error occured in GenerateKeyPairAndLinkProviderKeysToAvatarByUsername for email {email} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
        //    }

        //    return result;
        //}

        //public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatar(IAvatar avatar, ProviderType providerTypeToLinkTo, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        //{
        //    OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

        //    if (avatar == null)
        //    {
        //        OASISErrorHandling.HandleError(ref result, "An error occured in GenerateKeyPairAndLinkProviderKeysToAvatar. The avatar passed in is null.");
        //        return result;
        //    }

        //    try
        //    {
        //        OASISResult<KeyPair> keyPairResult = GenerateKeyPair(providerTypeToLinkTo);

        //        if (!keyPairResult.IsError && keyPairResult.Result != null)
        //        {
        //            //Backup the wallets before the private keys get blanked out in LinkProviderPublicKeyToAvatar.
        //            Dictionary<ProviderType, List<IProviderWallet>> wallets = WalletManager.Instance.CopyProviderWallets(avatar.ProviderWallets);
        //            OASISResult<IProviderWallet> publicKeyResult = LinkProviderPublicKeyToAvatar(Guid.Empty, avatar, providerTypeToLinkTo, keyPairResult.Result.PublicKey, null, null, showSecretRecoveryWords, providerToLoadAvatarFrom);

        //            if (!publicKeyResult.IsError)
        //            {
        //                //Need to restore wallet private keys because the LinkProviderPublicKeyToAvatar calls Save() on the avatar object, which then blanks all private keys for extra security.
        //                foreach (ProviderType pType in avatar.ProviderWallets.Keys)
        //                {
        //                    foreach (IProviderWallet wallet in avatar.ProviderWallets[pType])
        //                    {
        //                        //if (wallets.ContainsKey(pType) && wallets[pType].Any(x => x.WalletId == wallet.Id))
        //                        if (wallets.ContainsKey(pType))
        //                        {
        //                            IProviderWallet backedUpWallet = wallets[pType].FirstOrDefault(x => x.WalletId == wallet.Id);

        //                            if (backedUpWallet != null)
        //                                wallet.PrivateKey = backedUpWallet.PrivateKey;
        //                        }
        //                    }
        //                }

        //                //avatar.ProviderWallets = wallets;
                        
        //                OASISResult<IProviderWallet> privateKeyResult = LinkProviderPrivateKeyToAvatar(publicKeyResult.Result.Id, avatar, providerTypeToLinkTo, keyPairResult.Result.PrivateKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);

        //                if (!privateKeyResult.IsError)
        //                {
        //                    result.Message = "KeyPair Generated & Linked To Avatar.";
        //                    result.Result = privateKeyResult.Result;

        //                    if (!showPublicKey)
        //                        result.Result.PublicKey = null;

        //                    if (!showPrivateKey)
        //                        result.Result.PrivateKey = null;
        //                }
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatar whilst linking the generated private key to the avatar {avatar.Id} - {avatar.Username}. Reason: {privateKeyResult.Message}", privateKeyResult.DetailedMessage);
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatar whilst linking the generated public key to the avatar {avatar.Id} - {avatar.Username}. Reason: {publicKeyResult.Message}", publicKeyResult.DetailedMessage);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPublicKeyToAvatar for avatar {avatar.Id} {avatar.Username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
        //    }

        //    return result;
        //}

    }
}

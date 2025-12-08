using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Cryptography.ECDSA;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Enums;
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
    //TODO: Add Async version of all methods and add IKeyManager Interface.
    public class KeyManager : OASISManager
    {
        private readonly object _lockObject = new object();
        private readonly Dictionary<Guid, List<Key>> _avatarKeys;
        private readonly Dictionary<Guid, List<KeyUsage>> _keyUsage;

        private static Dictionary<string, string> _avatarIdToProviderUniqueStorageKeyLookup = new Dictionary<string, string>();
        private static Dictionary<string, List<string>> _avatarIdToProviderPublicKeysLookup = new Dictionary<string, List<string>>();
        //private static Dictionary<string, List<string>> _avatarIdToProviderPrivateKeyLookup = new Dictionary<string, List<string>>();
        private static Dictionary<string, string> _avatarUsernameToProviderUniqueStorageKeyLookup = new Dictionary<string, string>();
        private static Dictionary<string, List<string>> _avatarUsernameToProviderPublicKeysLookup = new Dictionary<string, List<string>>();
        //private static Dictionary<string, List<string>> _avatarUsernameToProviderPrivateKeyLookup = new Dictionary<string, List<string>>();
        private static Dictionary<string, string> _avatarEmailToProviderUniqueStorageKeyLookup = new Dictionary<string, string>();
        private static Dictionary<string, List<string>> _avatarEmailToProviderPublicKeysLookup = new Dictionary<string, List<string>>();
        //private static Dictionary<string, List<string>> _avatarEmailToProviderPrivateKeyLookup = new Dictionary<string, List<string>>();
        private static Dictionary<string, Guid> _providerUniqueStorageKeyToAvatarIdLookup = new Dictionary<string, Guid>();
        private static Dictionary<string, Guid> _providerPublicKeyToAvatarIdLookup = new Dictionary<string, Guid>();
        //private static Dictionary<string, Guid> _providerPrivateKeyToAvatarIdLookup = new Dictionary<string, Guid>();
        private static Dictionary<string, string> _providerUniqueStorageKeyToAvatarUsernameLookup = new Dictionary<string, string>();
        private static Dictionary<string, string> _providerPublicKeyToAvatarUsernameLookup = new Dictionary<string, string>();
        //private static Dictionary<string, string> _providerPrivateKeyToAvatarUsernameLookup = new Dictionary<string, string>();
        private static Dictionary<string, string> _providerUniqueStorageKeyToAvatarEmailLookup = new Dictionary<string, string>();
        private static Dictionary<string, string> _providerPublicKeyToAvatarEmailLookup = new Dictionary<string, string>();
        //private static Dictionary<string, string> _providerPrivateKeyToAvatarEmailLookup = new Dictionary<string, string>();
        private static Dictionary<string, IAvatar> _providerUniqueStorageKeyToAvatarLookup = new Dictionary<string, IAvatar>();
        private static Dictionary<string, IAvatar> _providerPublicKeyToAvatarLookup = new Dictionary<string, IAvatar>();
        //private static Dictionary<string, IAvatar> _providerPrivateKeyToAvatarLookup = new Dictionary<string, IAvatar>();
        private static KeyManager _instance = null;

        public static KeyManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new KeyManager(ProviderManager.Instance.CurrentStorageProvider);
                    //_instance = new KeyManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.Instance);

                return _instance;
            }
        }

        public WifUtility WifUtility { get; set; } = new WifUtility();
        //public AvatarManager AvatarManager { get; set; }

        public AvatarManager AvatarManager
        {
            get
            {
                return AvatarManager.Instance;
            }
        }


        //public KeyManager(IOASISStorageProvider OASISStorageProvider, AvatarManager avatarManager, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        //{
        //    AvatarManager = avatarManager;
        //}

        public KeyManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        //public static void Init(IOASISStorageProvider OASISStorageProvider, AvatarManager avatarManager, OASISDNA OASISDNA = null) 
        //{
        //    AvatarManager = avatarManager;
        //}

        //TODO: Implement later (Cache Disabled).
        //public bool IsCacheEnabled { get; set; } = true;

        public OASISResult<IKeyPairAndWallet> GenerateKeyPairWithWalletAddress(ProviderType providerType)
        {
            OASISResult<IKeyPairAndWallet> result = new OASISResult<IKeyPairAndWallet>();
            result.Result = KeyHelper.GenerateKeyValuePairAndWalletAddress();
            return result;
        }

        public OASISResult<KeyPair> GenerateKeyPair(ProviderType providerType)
        {
            string prefix = "";

            //TODO: Need to look up and add all prefixes here!
            switch (providerType)
            {
                case ProviderType.EthereumOASIS:
                    prefix = "1";
                    break;

                case ProviderType.SolanaOASIS: 
                    prefix = "2"; 
                    break;
            }

            return GenerateKeyPair(prefix);
        }

        public OASISResult<KeyPair> GenerateKeyPair(string prefix)
        {
            OASISResult<KeyPair> result = new OASISResult<KeyPair>(new KeyPair());

            //// Create RSA instance
            //RSA rsa = RSA.Create();

            //// Export keys
            //string publicKeyXml = rsa.ToXmlString(false);
            //string privateKeyXml = rsa.ToXmlString(true);



            byte[] privateKey = Secp256K1Manager.GenerateRandomKey();

            OASISResult<string> privateWifResult = GetPrivateWif(privateKey);

            if (!privateWifResult.IsError && privateWifResult.Result != null)
            {
                result.Result.PrivateKey = privateWifResult.Result;

                byte[] publicKey = Secp256K1Manager.GetPublicKey(privateKey, true);

                OASISResult<string> publicWifResult = GetPublicWif(publicKey, prefix);

                if (!publicWifResult.IsError && publicWifResult.Result != null)
                    result.Result.PublicKey = publicWifResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GenerateKeyPair generating public WIF. Reason: {publicWifResult.Message}");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GenerateKeyPair generating private WIF. Reason: {privateWifResult.Message}");

            return result;
        }

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
                            SecretRecoveryPhrase = string.Join(" ", new Mnemonic(Wordlist.English, WordCount.Twelve).Words)
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
                            SecretRecoveryPhrase = Rijndael.Encrypt(string.Join(" ", new Mnemonic(Wordlist.English, WordCount.Twelve).Words), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256)
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
                                result.Result.SecretRecoveryPhrase = Rijndael.Decrypt(secret, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
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

        public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarById(Guid avatarId, ProviderType providerTypeToLinkTo, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, false, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = GenerateKeyPairAndLinkProviderKeysToAvatar(avatarResult.Result, providerTypeToLinkTo, showPublicKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatarById loading avatar for id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in GenerateKeyPairAndLinkProviderKeysToAvatarById for avatar {avatarId} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
            }

            return result;
        }

        // Could be used as the public key for private/public key pairs. Could also be a username/accountname/unique id/etc, etc.
        public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarByUsername(string username, ProviderType providerTypeToLinkTo, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, true, false, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = GenerateKeyPairAndLinkProviderKeysToAvatar(avatarResult.Result, providerTypeToLinkTo, showPublicKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatarByUsername loading avatar for username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in GenerateKeyPairAndLinkProviderKeysToAvatarByUsername for username {username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatarByEmail(string email, ProviderType providerTypeToLinkTo, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            try
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, true, false, providerToLoadAvatarFrom);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = GenerateKeyPairAndLinkProviderKeysToAvatar(avatarResult.Result, providerTypeToLinkTo, showPublicKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);
                else
                    OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatarByUsername loading avatar for email {email}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"An unknown error occured in GenerateKeyPairAndLinkProviderKeysToAvatarByUsername for email {email} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
            }

            return result;
        }

        public OASISResult<IProviderWallet> GenerateKeyPairAndLinkProviderKeysToAvatar(IAvatar avatar, ProviderType providerTypeToLinkTo, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false, ProviderType providerToLoadAvatarFrom = ProviderType.Default)
        {
            OASISResult<IProviderWallet> result = new OASISResult<IProviderWallet>();

            if (avatar == null)
            {
                OASISErrorHandling.HandleError(ref result, "An error occured in GenerateKeyPairAndLinkProviderKeysToAvatar. The avatar passed in is null.");
                return result;
            }

            try
            {
                OASISResult<KeyPair> keyPairResult = GenerateKeyPair(providerTypeToLinkTo);

                if (!keyPairResult.IsError && keyPairResult.Result != null)
                {
                    //Backup the wallets before the private keys get blanked out in LinkProviderPublicKeyToAvatar.
                    Dictionary<ProviderType, List<IProviderWallet>> wallets = WalletManager.Instance.CopyProviderWallets(avatar.ProviderWallets);
                    OASISResult<IProviderWallet> publicKeyResult = LinkProviderPublicKeyToAvatar(Guid.Empty, avatar, providerTypeToLinkTo, keyPairResult.Result.PublicKey, null, null, showSecretRecoveryWords, providerToLoadAvatarFrom);

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
                        
                        OASISResult<IProviderWallet> privateKeyResult = LinkProviderPrivateKeyToAvatar(publicKeyResult.Result.Id, avatar, providerTypeToLinkTo, keyPairResult.Result.PrivateKey, showPrivateKey, showSecretRecoveryWords, providerToLoadAvatarFrom);

                        if (!privateKeyResult.IsError)
                        {
                            result.Message = "KeyPair Generated & Linked To Avatar.";
                            result.Result = privateKeyResult.Result;

                            if (!showPublicKey)
                                result.Result.PublicKey = null;

                            if (!showPrivateKey)
                                result.Result.PrivateKey = null;
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatar whilst linking the generated private key to the avatar {avatar.Id} - {avatar.Username}. Reason: {privateKeyResult.Message}", privateKeyResult.DetailedMessage);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"An error occured in GenerateKeyPairAndLinkProviderKeysToAvatar whilst linking the generated public key to the avatar {avatar.Id} - {avatar.Username}. Reason: {publicKeyResult.Message}", publicKeyResult.DetailedMessage);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occured in LinkProviderPublicKeyToAvatar for avatar {avatar.Id} {avatar.Username} and providerType {Enum.GetName(typeof(ProviderType), providerToLoadAvatarFrom)}: {ex.Message}");
            }

            return result;
        }

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

            try
            {
                if (!avatar.ProviderWallets.ContainsKey(providerTypeToLinkTo))
                    avatar.ProviderWallets.Add(providerTypeToLinkTo, new List<IProviderWallet>());

                foreach (ProviderType proType in avatar.ProviderWallets.Keys)
                {
                    foreach (IProviderWallet proWallet in avatar.ProviderWallets[proType])
                    {
                        if (proWallet != null && !string.IsNullOrEmpty(proWallet.PrivateKey))
                            proWallet.PrivateKey = Rijndael.Decrypt(proWallet.PrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                    }
                }

                IProviderWallet wallet = avatar.ProviderWallets[providerTypeToLinkTo].FirstOrDefault(x => x.PrivateKey == providerPrivateKey);

                foreach (ProviderType proType in avatar.ProviderWallets.Keys)
                {
                    foreach (IProviderWallet proWallet in avatar.ProviderWallets[proType])
                    {
                        if (proWallet != null && !string.IsNullOrEmpty(proWallet.PrivateKey))
                            proWallet.PrivateKey = Rijndael.Encrypt(proWallet.PrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
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
                            SecretRecoveryPhrase = Rijndael.Encrypt(string.Join(" ", new Mnemonic(Wordlist.English, WordCount.Twelve).Words), OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256),
                            PrivateKey = Rijndael.Encrypt(providerPrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256)
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
                            wallet.PrivateKey = Rijndael.Encrypt(providerPrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
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
                                result.Result.PrivateKey = Rijndael.Decrypt(privateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                        }
                        catch (Exception e)
                        {

                        }

                        try
                        {
                            if (showSecretRecoveryWords)
                                result.Result.SecretRecoveryPhrase = Rijndael.Decrypt(secret, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
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

        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            string key = string.Concat(Enum.GetName(providerType), avatarId);

            if (!_avatarIdToProviderUniqueStorageKeyLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = GetProviderUniqueStorageKeyForAvatar(avatarResult.Result, key, _avatarIdToProviderUniqueStorageKeyLookup, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderUniqueStorageKeyForAvatarById loading the avatar with id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarByUsername(string avatarUsername, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            string key = string.Concat(Enum.GetName(providerType), avatarUsername);

            if (!_avatarUsernameToProviderUniqueStorageKeyLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarUsername, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = GetProviderUniqueStorageKeyForAvatar(avatarResult.Result, key, _avatarUsernameToProviderUniqueStorageKeyLookup, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderUniqueStorageKeyForAvatarByUsername loading avatar with username {avatarUsername}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<string> GetProviderUniqueStorageKeyForAvatarByEmail(string avatarEmail, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            string key = string.Concat(Enum.GetName(providerType), avatarEmail);

            if (!_avatarEmailToProviderUniqueStorageKeyLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(avatarEmail, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                    result = GetProviderUniqueStorageKeyForAvatar(avatarResult.Result, key, _avatarEmailToProviderUniqueStorageKeyLookup, providerType);
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderUniqueStorageKeyForAvatarByEmail loading avatar with avatarEmail {avatarEmail}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<List<string>> GetProviderPublicKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            string key = string.Concat(Enum.GetName(providerType), avatarId);

            if (!_avatarIdToProviderPublicKeysLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.ProviderWallets.ContainsKey(providerType))
                    {
                        _avatarIdToProviderPublicKeysLookup[key] = avatarResult.Result.ProviderWallets[providerType].Select( x=> x.PublicKey).ToList();
                        result.Result = _avatarIdToProviderPublicKeysLookup[key];
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetProviderPublicKeysForAvatarById. The avatar with id ", avatarId, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPublicKeysForAvatarById loading avatar with id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<List<string>> GetProviderPublicKeysForAvatarByUsername(string avatarUsername, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            string key = string.Concat(Enum.GetName(providerType), avatarUsername);

            if (!_avatarUsernameToProviderPublicKeysLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarUsername, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.ProviderWallets.ContainsKey(providerType))
                    {
                        _avatarUsernameToProviderPublicKeysLookup[key] = avatarResult.Result.ProviderWallets[providerType].Select( x=> x.PublicKey).ToList();
                        result.Result = _avatarUsernameToProviderPublicKeysLookup[key];
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat("The avatar with username ", avatarUsername, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPublicKeysForAvatarByUsername loading avatar with username {avatarUsername}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<List<string>> GetProviderPublicKeysForAvatarByEmail(string avatarEmail, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            string key = string.Concat(Enum.GetName(providerType), avatarEmail);

            if (!_avatarEmailToProviderPublicKeysLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(avatarEmail, false, true, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.ProviderWallets.ContainsKey(providerType))
                    {
                        _avatarEmailToProviderPublicKeysLookup[key] = avatarResult.Result.ProviderWallets[providerType].Select(x => x.PublicKey).ToList();
                        result.Result = _avatarEmailToProviderPublicKeysLookup[key];
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat("The avatar with email ", avatarEmail, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPublicKeysForAvatarByEmail loading avatar with email {avatarEmail}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }
            
            return result;
        }

        /*
        public OASISResult<List<string>> GetProviderPrivateKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            string key = string.Concat(Enum.GetName(providerType), avatarId);

            if (AvatarManager.LoggedInAvatar.Id != avatarId)
            {
                result.IsError = true;
                result.Message = "You cannot retreive the private key for another person's avatar. Please login to this account and try again.";
            }

            if (!_avatarIdToProviderPrivateKeyLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, false, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.ProviderWallets.ContainsKey(providerType))
                    {
                        _avatarIdToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderWallets[providerType].Select(x => x.PrivateKey).ToList();

                        // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
                        for (int i = 0; i < _avatarIdToProviderPrivateKeyLookup[key].Count; i++)
                            _avatarIdToProviderPrivateKeyLookup[key][i] = Rijndael.Decrypt(_avatarIdToProviderPrivateKeyLookup[key][i], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat("The avatar with id ", avatarId, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPrivateKeyToAvatar method on the AvatarManager or avatar REST API."));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeysForAvatarById loading avatar with id {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<List<string>> GetProviderPrivateKeysForAvatarByUsername(string avatarUsername, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            string key = string.Concat(Enum.GetName(providerType), avatarUsername);

            if (AvatarManager.LoggedInAvatar.Username != avatarUsername)
                OASISErrorHandling.HandleError(ref result, "Error occured in GetProviderPrivateKeysForAvatarByUsername. You cannot retreive the private key for another person's avatar. Please login to this account and try again.");

            if (!_avatarUsernameToProviderPrivateKeyLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarUsername, true, false, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    if (avatarResult.Result.ProviderWallets.ContainsKey(providerType))
                    {
                        _avatarUsernameToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderWallets[providerType].Select(x => x.PrivateKey).ToList();
                        
                        // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
                        for (int i = 0; i < _avatarIdToProviderPrivateKeyLookup[key].Count; i++)
                            _avatarIdToProviderPrivateKeyLookup[key][i] = Rijndael.Decrypt(_avatarIdToProviderPrivateKeyLookup[key][i], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetProviderPrivateKeysForAvatarByUsername. The avatar with username ", avatarUsername, " was not found."));
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeysForAvatarByUsername loading avatar with username {avatarUsername}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }*/

        public OASISResult<List<string>> GetProviderPrivateKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            //string key = string.Concat(Enum.GetName(providerType), avatarId);

            if (AvatarManager.LoggedInAvatar.Id != avatarId)
            {
                result.IsError = true;
                result.Message = "You cannot retreive the private key for another person's avatar. Please login to this account and try again.";
            }

            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = WalletManager.Instance.LoadProviderWalletsForAvatarById(avatarId);

            //We use to cache the private keys but for security we no longer do this and instead load them from local storage each time they are needed (since they are local storage not much need to cache anyway).
            if (!walletsResult.IsError && walletsResult.Result != null)
            {
                if (walletsResult.Result.ContainsKey(providerType))
                {
                    result.Result = walletsResult.Result[providerType].Select(x => x.PrivateKey).ToList();

                    for (int i = 0; i < result.Result.Count; i++)
                    {
                        if (result.Result[i] != null)
                            result.Result[i] = Rijndael256.Rijndael.Decrypt(result.Result[i], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("The avatar with id ", avatarId, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPrivateKeyToAvatar method on the AvatarManager or avatar REST API."));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeysForAvatarById loading avatar wallets with id {avatarId}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);

            return result;
        }

        public OASISResult<List<string>> GetProviderPrivateKeysForAvatarByUsername(string avatarUsername, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();
            //string key = string.Concat(Enum.GetName(providerType), avatarUsername);

            if (AvatarManager.LoggedInAvatar.Username != avatarUsername)
                OASISErrorHandling.HandleError(ref result, "Error occured in GetProviderPrivateKeysForAvatarByUsername. You cannot retreive the private key for another person's avatar. Please login to this account and try again.");

            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = WalletManager.Instance.LoadProviderWalletsForAvatarByUsername(avatarUsername);

            //We use to cache the private keys but for security we no longer do this and instead load them from local storage each time they are needed (since they are local storage not much need to cache anyway).
            if (!walletsResult.IsError && walletsResult.Result != null)
            {
                if (walletsResult.Result.ContainsKey(providerType))
                {
                    result.Result = walletsResult.Result[providerType].Select(x => x.PrivateKey).ToList();

                    for (int i = 0; i < result.Result.Count; i++)
                    {
                        if (result.Result[i] != null)
                            result.Result[i] = Rijndael256.Rijndael.Decrypt(result.Result[i], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("The avatar with username ", avatarUsername, " has not been linked to the ", Enum.GetName(providerType), " provider. Please use the LinkProviderPrivateKeyToAvatar method on the AvatarManager or avatar REST API."));
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeysForAvatarByUsername loading avatar wallets with username {avatarUsername}. Reason: {walletsResult.Message}", walletsResult.DetailedMessage);

            return result;
        }

        //public OASISResult<string> GetProviderPrivateKeyForAvatarByEmail(string avatarEmail, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<string> result = new OASISResult<string>();
        //    string key = string.Concat(Enum.GetName(providerType), avatarEmail);

        //    if (AvatarManager.LoggedInAvatar.Email != avatarEmail)
        //        OASISErrorHandling.HandleError(ref result, "Error occured in GetProviderPrivateKeyForAvatarByEmail. You cannot retreive the private key for another person's avatar. Please login to this account and try again.");

        //    if (!_avatarEmailToProviderPrivateKeyLookup.ContainsKey(key))
        //    {
        //        OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(avatarEmail, false, providerType);

        //        if (!avatarResult.IsError && avatarResult.Result != null)
        //        {
        //            if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
        //                _avatarEmailToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderPrivateKey[providerType];
        //            else
        //                OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetProviderPrivateKeyForAvatarByEmail. The avatar with email ", avatarEmail, " was not found."));
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeyForAvatarByEmail loading avatar with email {avatarEmail}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
        //    }

        //    //result.Result = StringCipher.Decrypt(_avatarEmailToProviderPrivateKeyLookup[key]);
        //    result.Result = Rijndael.Decrypt(_avatarEmailToProviderPrivateKeyLookup[key], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
        //    return result;
        //}

        //public OASISResult<string> GetProviderPrivateKeyForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<string> result = new OASISResult<string>();
        //    string key = string.Concat(Enum.GetName(providerType), avatarUsername);

        //    if (AvatarManager.LoggedInAvatar.Email != email)
        //        OASISErrorHandling.HandleError(ref result, "Error occured in GetProviderPrivateKeyForAvatar. You cannot retreive the private key for another person's avatar. Please login to this account and try again.");

        //    if (!_avatarUsernameToProviderPrivateKeyLookup.ContainsKey(key))
        //    {
        //        OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, providerType);

        //        if (!avatarResult.IsError && avatarResult.Result != null)
        //        {
        //            if (avatarResult.Result.ProviderPublicKey.ContainsKey(providerType))
        //                _avatarIdToProviderPrivateKeyLookup[key] = avatarResult.Result.ProviderPrivateKey[providerType];
        //            else
        //                OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetProviderPrivateKeyForAvatar. The avatar with username ", avatarUsername, " was not found."));
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"Error occured in GetProviderPrivateKeyForAvatar loading avatar with username {avatarUsername}. Reason: {avatarResult.Message}");
        //    }

        //    result.Result = StringCipher.Decrypt(_avatarUsernameToProviderPrivateKeyLookup[key]);
        //    return result;
        //}

        public OASISResult<Guid> GetAvatarIdForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.
            OASISResult<Guid> result = new OASISResult<Guid>();
            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerUniqueStorageKeyToAvatarIdLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderUniqueStorageKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerUniqueStorageKeyToAvatarIdLookup[key] = avatarResult.Result.Id;
                    result.Result = _providerUniqueStorageKeyToAvatarIdLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarIdForProviderUniqueStorageKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<string> GetAvatarUsernameForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerUniqueStorageKeyToAvatarUsernameLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderUniqueStorageKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerUniqueStorageKeyToAvatarUsernameLookup[key] = avatarResult.Result.Username;
                    result.Result = _providerUniqueStorageKeyToAvatarUsernameLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarUsernameForProviderUniqueStorageKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<string> GetAvatarEmailForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerUniqueStorageKeyToAvatarEmailLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderUniqueStorageKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerUniqueStorageKeyToAvatarEmailLookup[key] = avatarResult.Result.Email;
                    result.Result = _providerUniqueStorageKeyToAvatarEmailLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarEmailForProviderUniqueStorageKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<IAvatar> GetAvatarForProviderUniqueStorageKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerUniqueStorageKeyToAvatarLookup.ContainsKey(key))
            {
                //TODO: Ideally need a new overload for LoadAvatar that takes the provider key.
                //TODO: In the meantime should we cache the full list of Avatars? Could take up a LOT of memory so probably not good idea?
                OASISResult<IEnumerable<IAvatar>> avatarsResult = AvatarManager.LoadAllAvatars(false, true, true, providerType);

                if (!avatarsResult.IsError && avatarsResult.Result != null)
                {
                    IAvatar avatar = avatarsResult.Result.FirstOrDefault(x => x.ProviderUniqueStorageKey.ContainsKey(providerType) && x.ProviderUniqueStorageKey[providerType] == providerKey);

                    if (avatar != null)
                    {
                        _providerUniqueStorageKeyToAvatarIdLookup[key] = avatar.Id;
                        _providerUniqueStorageKeyToAvatarUsernameLookup[key] = avatar.Username;
                        _providerUniqueStorageKeyToAvatarEmailLookup[key] = avatar.Email;
                        _providerUniqueStorageKeyToAvatarLookup[key] = avatar;

                        result.Result = _providerUniqueStorageKeyToAvatarLookup[key];
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat("The provider Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderKeyToAvatar method on the AvatarManager or avatar REST API."));

                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error in GetAvatarForProviderUniqueStorageKey loading all avatars. Reason: {avatarsResult.Message}", avatarsResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<Guid> GetAvatarIdForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Guid> result = new OASISResult<Guid>();

            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPublicKeyToAvatarIdLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPublicKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerPublicKeyToAvatarIdLookup[key] = avatarResult.Result.Id;
                    result.Result = _providerPublicKeyToAvatarIdLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarIdForProviderPublicKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<string> GetAvatarUsernameForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPublicKeyToAvatarUsernameLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPublicKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerPublicKeyToAvatarUsernameLookup[key] = avatarResult.Result.Username;
                    result.Result = _providerPublicKeyToAvatarUsernameLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarUsernameForProviderPublicKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<string> GetAvatarEmailForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPublicKeyToAvatarEmailLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPublicKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerPublicKeyToAvatarEmailLookup[key] = avatarResult.Result.Email;
                    result.Result = _providerPublicKeyToAvatarEmailLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in GetAvatarEmailForProviderPublicKey loading avatar for providerKey {providerKey}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<IAvatar> GetAvatarForProviderPublicKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            string key = string.Concat(Enum.GetName(providerType), providerKey);

            //TODO: Ideally need a new overload for LoadAvatarDetail that takes the public provider key.
            //TODO: In the meantime should we cache the full list of AvatarDetails? Could take up a LOT of memory so probably not good idea?
            if (!_providerPublicKeyToAvatarLookup.ContainsKey(key))
            {
                //TODO: Need to cache loading all avatars.
                OASISResult<IEnumerable<IAvatar>> avatarsResult = AvatarManager.LoadAllAvatars(false, true, true, providerType);

                if (!avatarsResult.IsError && avatarsResult.Result != null)
                {
                    IAvatar avatar = avatarsResult.Result.FirstOrDefault(x => x.ProviderWallets.ContainsKey(providerType) && x.ProviderWallets[providerType].Any(x => x.PublicKey == providerKey));

                    if (avatar != null)
                    {
                        _providerPublicKeyToAvatarIdLookup[key] = avatar.Id;
                        _providerPublicKeyToAvatarUsernameLookup[key] = avatar.Username;
                        _providerPublicKeyToAvatarEmailLookup[key] = avatar.Email;
                        _providerPublicKeyToAvatarLookup[key] = avatar;

                        result.Result = _providerPublicKeyToAvatarLookup[key];
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat("The provider public Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("Error in GetAvatarForProviderPublicKey for the provider public Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType. There was an error loading all avatars. Reason: ", avatarsResult.Message));
            }

            return result;
        }

        /*
        public OASISResult<Guid> GetAvatarIdForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Guid> result = new OASISResult<Guid>();

            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPrivateKeyToAvatarIdLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerPrivateKeyToAvatarIdLookup[key] = avatarResult.Result.Id;
                    result.Result = _providerPrivateKeyToAvatarIdLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarIdForProviderPrivateKey. The provider public Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
            }

            return result;
        }

        public OASISResult<string> GetAvatarUsernameForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPrivateKeyToAvatarUsernameLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerPrivateKeyToAvatarUsernameLookup[key] = avatarResult.Result.Username;
                    result.Result = _providerPrivateKeyToAvatarUsernameLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarUsernameForProviderPrivateKey for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
            }

            return result;
        }

        public OASISResult<string> GetAvatarEmailForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

            string key = string.Concat(Enum.GetName(providerType), providerKey);

            if (!_providerPrivateKeyToAvatarEmailLookup.ContainsKey(key))
            {
                OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    _providerPrivateKeyToAvatarEmailLookup[key] = avatarResult.Result.Email;
                    result.Result = _providerPrivateKeyToAvatarEmailLookup[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarEmailForProviderPrivateKey for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
            }

            return result;
        }

        public OASISResult<IAvatar> GetAvatarForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            //TODO: Fix the StringCipher below or find the strongest encryption, maybe the Qunatum Encryption? :)
            //string key = string.Concat(Enum.GetName(providerType), StringCipher.Encrypt(providerKey));
            //string key = string.Concat(Enum.GetName(providerType), BC.HashPassword(providerKey));
            string key = string.Concat(Enum.GetName(providerType), Rijndael.Encrypt(providerKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256));

            if (!_providerPrivateKeyToAvatarLookup.ContainsKey(key))
            {
                //TODO: Ideally need a new overload for LoadAvatarDetail that takes the public provider key.
                //TODO: In the meantime should we cache the full list of AvatarDetails? Could take up a LOT of memory so probably not good idea?
                OASISResult<IEnumerable<IAvatar>> avatarsResult = AvatarManager.LoadAllAvatars(true, false, providerType);

                if (!avatarsResult.IsError && avatarsResult.Result != null)
                {
                    IAvatar avatar = avatarsResult.Result.FirstOrDefault(x => x.ProviderWallets.ContainsKey(providerType) && x.ProviderWallets[providerType].Any(x => x.PrivateKey == providerKey));

                    if (avatar != null)
                    {
                        _providerPublicKeyToAvatarIdLookup[key] = avatar.Id;
                        _providerPublicKeyToAvatarUsernameLookup[key] = avatar.Username;
                        _providerPublicKeyToAvatarEmailLookup[key] = avatar.Email;
                        _providerPublicKeyToAvatarLookup[key] = avatar;

                        result.Result = _providerPrivateKeyToAvatarLookup[key];
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, string.Concat("The provider private Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPrivateKeyToAvatar method on the AvatarManager or avatar REST API."));
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("Error in GetAvatarForProviderPrivateKey for the provider private Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType. There was an error loading all avatars. Reason: ", avatarsResult.Message));
            }
            
            return result;
        }
        */

        /*
        public OASISResult<Guid> GetAvatarIdForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Guid> result = new OASISResult<Guid>();

            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

           // string key = string.Concat(Enum.GetName(providerType), providerKey);

            OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = avatarResult.Result.Id;
            else
                OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarIdForProviderPrivateKey. The provider public Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
            
            return result;
        }

        public OASISResult<string> GetAvatarUsernameForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

           // string key = string.Concat(Enum.GetName(providerType), providerKey);

            OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = avatarResult.Result.Username;
            else
                OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarUsernameForProviderPrivateKey for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));

            return result;
        }

        public OASISResult<string> GetAvatarEmailForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            // TODO: Do we need to store both the id and whole avatar in the cache? Think only need one? Just storing the id would use less memory and be faster but there may be use cases for when we need the whole avatar?
            // In future, if there is not a use case for the whole avatar we will just use the id cache and remove the other.

           // string key = string.Concat(Enum.GetName(providerType), providerKey);

            OASISResult<IAvatar> avatarResult = GetAvatarForProviderPrivateKey(providerKey, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = avatarResult.Result.Email;
            else
                OASISErrorHandling.HandleError(ref result, string.Concat("Error occured in GetAvatarEmailForProviderPrivateKey for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPublicKeyToAvatar method on the AvatarManager or avatar REST API."));
            
            return result;
        }

        
        public OASISResult<IAvatar> GetAvatarForProviderPrivateKey(string providerKey, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();

            //TODO: Fix the StringCipher below or find the strongest encryption, maybe the Qunatum Encryption? :)
            //string key = string.Concat(Enum.GetName(providerType), StringCipher.Encrypt(providerKey));
            //string key = string.Concat(Enum.GetName(providerType), BC.HashPassword(providerKey));
            //string key = string.Concat(Enum.GetName(providerType), Rijndael.Encrypt(providerKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256));


            //TODO: Ideally need a new overload for LoadAvatarDetail that takes the public provider key.
            //TODO: In the meantime should we cache the full list of AvatarDetails? Could take up a LOT of memory so probably not good idea?


            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> walletsResult = WalletManager.Instance.LoadAllProviderWallets(providerKey);

            if (!avatarsResult.IsError && avatarsResult.Result != null)
            {
                IAvatar avatar = avatarsResult.Result.FirstOrDefault(x => x.ProviderWallets.ContainsKey(providerType) && x.ProviderWallets[providerType].Any(x => x.PrivateKey == providerKey));

                if (avatar != null)
                {
                    result.Result = avatar;
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("The provider private Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType has not been linked to an avatar. Please use the LinkProviderPrivateKeyToAvatar method on the AvatarManager or avatar REST API."));
            }
            else
                OASISErrorHandling.HandleError(ref result, string.Concat("Error in GetAvatarForProviderPrivateKey for the provider private Key ", providerKey, " for the ", Enum.GetName(providerType), " providerType. There was an error loading all avatars. Reason: ", avatarsResult.Message));

            return result;
        }*/


        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = avatarResult.Result.ProviderUniqueStorageKey;
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderUniqueStorageKeysForAvatarById loading avatar with avatarId {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarByUsername(string username, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = avatarResult.Result.ProviderUniqueStorageKey;
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderUniqueStorageKeysForAvatarByUsername loading avatar with username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, string>> GetAllProviderUniqueStorageKeysForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
                result.Result = avatarResult.Result.ProviderUniqueStorageKey;
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderUniqueStorageKeysForAvatarByEmail loading avatar with email {email}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        

        public OASISResult<Dictionary<ProviderType, List<KeyPair>>> GetAllProviderKeyPairsForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<KeyPair>>> result = new OASISResult<Dictionary<ProviderType, List<KeyPair>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, false, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<KeyPair>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<KeyPair>();

                        if (wallet.PublicKey != null || wallet.PrivateKey != null)
                        {
                            result.Result[provider].Add(new KeyPair()
                            {
                                PrivateKey = wallet.PrivateKey != null ? Rijndael256.Rijndael.Decrypt(wallet.PrivateKey, OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256) : null,
                                PublicKey = wallet.PublicKey
                            });
                        }
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderKeyPairsForAvatarById loading avatar with avatarId {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderWalletAddressesForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.WalletAddress != null)
                            result.Result[provider].Add(wallet.WalletAddress);
                    }

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderWalletAddressesForAvatarById loading avatar with avatarId {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderWalletAddressesForAvatarByUsername(string username, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.WalletAddress != null)
                            result.Result[provider].Add(wallet.WalletAddress);
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderWalletAddressesForAvatarByUsername loading avatar with username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderWalletAddressesForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.WalletAddress != null)
                            result.Result[provider].Add(wallet.WalletAddress);
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderWalletAddressesForAvatarByEmail loading avatar with email {email}. Reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.PublicKey != null)
                            result.Result[provider].Add(wallet.PublicKey);
                    }

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderPublicKeysForAvatarById loading avatar with avatarId {avatarId}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarByUsername(string username, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.PublicKey != null)
                            result.Result[provider].Add(wallet.PublicKey);
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderPublicKeysForAvatarByUsername loading avatar with username {username}. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPublicKeysForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();
            OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, true, providerType);

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                result.Result = new Dictionary<ProviderType, List<string>>();

                foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                {
                    //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PublicKey).ToList();

                    //for (int i = 0; i < result.Result[provider].Count; i++)
                    //{
                    //    if (result.Result[provider][i] == null)
                    //        result.Result[provider].Remove(result.Result[provider][i]);
                    //}

                    foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                    {
                        if (!result.Result.ContainsKey(provider))
                            result.Result[provider] = new List<string>();

                        if (wallet.PublicKey != null)
                            result.Result[provider].Add(wallet.PublicKey);
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetAllProviderPublicKeysForAvatarByEmail loading avatar with email {email}. Reason: {avatarResult.Message}");

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPrivateKeysForAvatarById(Guid avatarId, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();

            if (AvatarManager.LoggedInAvatar.Id != avatarId)
                OASISErrorHandling.HandleError(ref result, "An error occured in GetAllProviderPrivateKeysForAvatarById. You can only retreive your own private keys, not another persons avatar.");
            else
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(avatarId, true, false, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    result.Result = new Dictionary<ProviderType, List<string>>();

                    foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                    {
                        //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PrivateKey).ToList();

                        //for (int i = 0; i < result.Result[provider].Count; i++)
                        //{
                        //    if (result.Result[provider][i] == null)
                        //        result.Result[provider].Remove(result.Result[provider][i]);
                        //}

                        foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                        {
                            if (!result.Result.ContainsKey(provider))
                                result.Result[provider] = new List<string>();

                            if (wallet.PrivateKey != null)
                                result.Result[provider].Add(wallet.PrivateKey);
                        }

                        // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
                        for (int i = 0; i < result.Result[provider].Count; i++)
                        {
                            if (result.Result[provider][i] != null)
                                result.Result[provider][i] = Rijndael256.Rijndael.Decrypt(result.Result[provider][i], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"An error occured in GetAllProviderPrivateKeysForAvatarById, the avatar with id {avatarId} could not be loaded. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<string>>> GetAllProviderPrivateKeysForAvatarByUsername(string username, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Dictionary<ProviderType, List<string>>> result = new OASISResult<Dictionary<ProviderType, List<string>>>();

            if (AvatarManager.LoggedInAvatar == null)
                OASISErrorHandling.HandleError(ref result, "Error occured in GetAllProviderPrivateKeysForAvatarByUsername, AvatarManager.LoggedInAvatar is null!");
            
            else if (AvatarManager.LoggedInAvatar.Username != username)
                OASISErrorHandling.HandleError(ref result, "Error occured in GetAllProviderPrivateKeysForAvatarByUsername, you can only retreive your own private keys, not another persons avatar.");
            else
            {
                OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatar(username, true, false, providerType);

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    result.Result = new Dictionary<ProviderType, List<string>>();

                    foreach (ProviderType provider in avatarResult.Result.ProviderWallets.Keys)
                    {
                        //result.Result[provider] = avatarResult.Result.ProviderWallets[provider].Select(x => x.PrivateKey).ToList();

                        //for (int i = 0; i < result.Result[provider].Count; i++)
                        //{
                        //    if (result.Result[provider][i] == null)
                        //        result.Result[provider].Remove(result.Result[provider][i]);
                        //}

                        foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[provider])
                        {
                            if (!result.Result.ContainsKey(provider))
                                result.Result[provider] = new List<string>();

                            if (wallet.PrivateKey != null)
                                result.Result[provider].Add(wallet.PrivateKey);
                        }

                        // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
                        for (int i = 0; i < result.Result[provider].Count; i++)
                        {
                            if (result.Result[provider][i] != null)
                                result.Result[provider][i] = Rijndael256.Rijndael.Decrypt(result.Result[provider][i], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
                        }
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"An error occured in GetAllProviderPrivateKeysForAvatarByUsername, the avatar with username {username} could not be loaded. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
            }

            return result;
        }

        //public OASISResult<Dictionary<ProviderType, string>> GetAllProviderPrivateKeysForAvatarByEmail(string email, ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<Dictionary<ProviderType, string>> result = new OASISResult<Dictionary<ProviderType, string>>();

        //    if (AvatarManager.LoggedInAvatar.Email != email)
        //        OASISErrorHandling.HandleError(ref result, "Error occured in GetAllProviderPrivateKeysForAvatarByEmail, you can only retreive your own private keys, not another persons avatar.");
        //    else
        //    {
        //        OASISResult<IAvatar> avatarResult = AvatarManager.LoadAvatarByEmail(email, false, providerType);

        //        if (!avatarResult.IsError && avatarResult.Result != null)
        //        {
        //            result.Result = avatarResult.Result.ProviderPrivateKey;

        //            // Decrypt the keys only for this return object (there are not stored in memory or storage unenrypted).
        //            foreach (ProviderType privateKeyProviderType in result.Result.Keys)
        //                result.Result[privateKeyProviderType] = Rijndael.Decrypt(result.Result[privateKeyProviderType], OASISDNA.OASIS.Security.OASISProviderPrivateKeys.Rijndael256Key, KeySize.Aes256);
        //                //result.Result[privateKeyProviderType] = StringCipher.Decrypt(result.Result[privateKeyProviderType]);
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"An error occured in GetAllProviderPrivateKeysForAvatarByEmail, the avatar with email {email} could not be loaded. Reason: {avatarResult.Message}", avatarResult.DetailedMessage);
        //    }

        //    return result;
        //}

        public OASISResult<string> GetPrivateWif(byte[] source)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                result.Result = WifUtility.GetPrivateWif(source);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetPrivateWif. Reason: {ex}", ex);
            }

            return result;
        }

        public OASISResult<string> GetPublicWif(byte[] publicKey, string prefix)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                result.Result = WifUtility.GetPublicWif(publicKey, prefix);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in GetPublicWif. Reason: {ex}", ex);
            }

            return result; ;
        }

        public OASISResult<byte[]> DecodePrivateWif(string data)
        {
            OASISResult<byte[]> result = new OASISResult<byte[]>();

            try
            {
                result.Result = WifUtility.DecodePrivateWif(data);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in DecodePrivateWif. Reason: {ex}", ex);
            }

            return result;
        }

        public OASISResult<byte[]> Base58CheckDecode(string data)
        {
            OASISResult<byte[]> result = new OASISResult<byte[]>();

            try
            {
                result.Result = WifUtility.Base58CheckDecode(data);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in Base58CheckDecode. Reason: {ex}", ex);
            }

            return result;
        }

        public OASISResult<string> EncodeSignature(byte[] source)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                result.Result = WifUtility.EncodeSignature(source);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in EncodeSignature. Reason: {ex}", ex);
            }

            return result;
        }

        public async Task<OASISResult<List<Key>>> GetAllKeysAsync(Guid avatarId)
        {
            var result = new OASISResult<List<Key>>();
            try
            {
                if (_avatarKeys.TryGetValue(avatarId, out var keys))
                {
                    result.Result = keys.ToList();
                    result.Message = "Keys retrieved successfully.";
                }
                else
                {
                    result.Result = new List<Key>();
                    result.Message = "No keys found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving keys: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<Key>> GenerateKeyAsync(Guid avatarId, KeyType keyType, string name = null, Dictionary<string, object> metadata = null)
        {
            var result = new OASISResult<Key>();
            try
            {
                var key = new Key
                {
                    Id = Guid.NewGuid(),
                    AvatarId = avatarId,
                    KeyType = keyType,
                    Name = name ?? $"{keyType} Key {DateTime.UtcNow:yyyy-MM-dd}",
                    PublicKey = GeneratePublicKey(),
                    PrivateKey = GeneratePrivateKey(),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    UsageCount = 0,
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                lock (_lockObject)
                {
                    if (!_avatarKeys.ContainsKey(avatarId))
                    {
                        _avatarKeys[avatarId] = new List<Key>();
                    }
                    _avatarKeys[avatarId].Add(key);
                }

                result.Result = key;
                result.Message = "Key generated successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = null;
                result.Message = $"Error generating key: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<bool>> UseKeyAsync(Guid avatarId, Guid keyId, string purpose = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_avatarKeys.TryGetValue(avatarId, out var keys))
                {
                    var key = keys.FirstOrDefault(k => k.Id == keyId);
                    if (key != null && key.IsActive)
                    {
                        key.UsageCount++;
                        key.LastUsedAt = DateTime.UtcNow;

                        // Record usage
                        var usage = new KeyUsage
                        {
                            Id = Guid.NewGuid(),
                            KeyId = keyId,
                            AvatarId = avatarId,
                            Purpose = purpose,
                            UsedAt = DateTime.UtcNow
                        };

                        lock (_lockObject)
                        {
                            if (!_keyUsage.ContainsKey(avatarId))
                            {
                                _keyUsage[avatarId] = new List<KeyUsage>();
                            }
                            _keyUsage[avatarId].Add(usage);
                        }

                        result.Result = true;
                        result.Message = "Key used successfully.";
                    }
                    else
                    {
                        result.IsError = true;
                        result.Result = false;
                        result.Message = "Key not found or inactive.";
                    }
                }
                else
                {
                    result.IsError = true;
                    result.Result = false;
                    result.Message = "No keys found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error using key: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<bool>> DeactivateKeyAsync(Guid avatarId, Guid keyId)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_avatarKeys.TryGetValue(avatarId, out var keys))
                {
                    var key = keys.FirstOrDefault(k => k.Id == keyId);
                    if (key != null)
                    {
                        key.IsActive = false;
                        key.DeactivatedAt = DateTime.UtcNow;

                        result.Result = true;
                        result.Message = "Key deactivated successfully.";
                    }
                    else
                    {
                        result.IsError = true;
                        result.Result = false;
                        result.Message = "Key not found.";
                    }
                }
                else
                {
                    result.IsError = true;
                    result.Result = false;
                    result.Message = "No keys found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Result = false;
                result.Message = $"Error deactivating key: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<List<KeyUsage>>> GetKeyUsageHistoryAsync(Guid avatarId, int limit = 50, int offset = 0)
        {
            var result = new OASISResult<List<KeyUsage>>();
            try
            {
                if (_keyUsage.TryGetValue(avatarId, out var usage))
                {
                    result.Result = usage
                        .OrderByDescending(u => u.UsedAt)
                        .Skip(offset)
                        .Take(limit)
                        .ToList();
                    result.Message = "Key usage history retrieved successfully.";
                }
                else
                {
                    result.Result = new List<KeyUsage>();
                    result.Message = "No key usage history found for this avatar.";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving key usage history: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        #region Competition Tracking

        private async Task UpdateKeyCompetitionScoresAsync(Guid avatarId, KeyType keyType)
        {
            try
            {
                var competitionManager = CompetitionManager.Instance;

                // Calculate score based on key type and usage
                var score = CalculateKeyScore(keyType);

                // Update social activity competition scores
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Daily, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Weekly, score);
                await competitionManager.UpdateAvatarScoreAsync(avatarId, CompetitionType.SocialActivity, SeasonType.Monthly, score);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating key competition scores: {ex.Message}");
            }
        }

        private long CalculateKeyScore(KeyType keyType)
        {
            return keyType switch
            {
                KeyType.Authentication => 5,
                KeyType.Encryption => 10,
                KeyType.Signing => 15,
                KeyType.Access => 20,
                KeyType.Master => 50,
                KeyType.System => 25,
                _ => 1
            };
        }

        public async Task<OASISResult<Dictionary<string, object>>> GetKeyStatsAsync(Guid avatarId)
        {
            var result = new OASISResult<Dictionary<string, object>>();
            try
            {
                var keys = _avatarKeys.GetValueOrDefault(avatarId, new List<Key>());
                var usage = _keyUsage.GetValueOrDefault(avatarId, new List<KeyUsage>());

                var totalKeys = keys.Count;
                var activeKeys = keys.Count(k => k.IsActive);
                var totalUsage = usage.Count;
                var keyTypeDistribution = keys.GroupBy(k => k.KeyType).ToDictionary(g => g.Key.ToString(), g => g.Count());

                var stats = new Dictionary<string, object>
                {
                    ["totalKeys"] = totalKeys,
                    ["activeKeys"] = activeKeys,
                    ["inactiveKeys"] = totalKeys - activeKeys,
                    ["totalUsage"] = totalUsage,
                    ["keyTypeDistribution"] = keyTypeDistribution,
                    ["averageUsagePerKey"] = totalKeys > 0 ? (double)totalUsage / totalKeys : 0,
                    ["mostUsedKeyType"] = keyTypeDistribution.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "None",
                    ["totalScore"] = keys.Sum(k => CalculateKeyScore(k.KeyType) * k.UsageCount)
                };

                result.Result = stats;
                result.Message = "Key statistics retrieved successfully.";
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error retrieving key statistics: {ex.Message}";
                result.Exception = ex;
            }
            return await Task.FromResult(result);
        }

        #endregion

        #region Helper Methods

        private string GeneratePublicKey()
        {
            // In a real implementation, this would generate a proper cryptographic key
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        private string GeneratePrivateKey()
        {
            // In a real implementation, this would generate a proper cryptographic key
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        #endregion

        private OASISResult<string> GetProviderUniqueStorageKeyForAvatar(IAvatar avatar, string key, Dictionary<string, string> dictionaryCache, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            if (avatar != null)
            {
                if (avatar.ProviderUniqueStorageKey.ContainsKey(providerType))
                {
                    dictionaryCache[key] = avatar.ProviderUniqueStorageKey[providerType];
                    result.Result = dictionaryCache[key];
                }
                else
                    OASISErrorHandling.HandleError(ref result, string.Concat("The avatar with id ", avatar.Id, " and username ", avatar.Username, " has not been linked to the ", Enum.GetName(providerType), " provider."));
            }
            else
                OASISErrorHandling.HandleError(ref result, string.Concat("The avatar with id ", avatar.Id, " and username ", avatar.Username, " was not found."));

            //result.Result = dictionaryCache[key];
            return result;
        }
    }

    public class Key
    {
        public Guid Id { get; set; }
        public Guid AvatarId { get; set; }
        public KeyType KeyType { get; set; }
        public string Name { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public DateTime? DeactivatedAt { get; set; }
        public bool IsActive { get; set; }
        public int UsageCount { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class KeyUsage
    {
        public Guid Id { get; set; }
        public Guid KeyId { get; set; }
        public Guid AvatarId { get; set; }
        public string Purpose { get; set; }
        public DateTime UsedAt { get; set; }
    }

    public enum KeyType
    {
        PrivateKey,
        PublicKey,
        Authentication,
        Encryption,
        Signing,
        Access,
        Master,
        System,
        Custom
    }
}
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







    }
}
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.IO;
using System.Reflection;
using System.Text;
using Nethereum.RPC.Accounts;
// using Nethereum.StandardTokenEIP20; // Commented out - type doesn't exist

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        // Custom keys are exactly the stored CustomKey metadata field, never provider keys.
        private static string StoredCustomKey(IHolon record)
            => record.MetaData != null && record.MetaData.TryGetValue("CustomKey",out var value)
                ? Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture) : null;

        public async Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await FindStoredIdentityAsync(customKey,StoredCustomKey,
                ()=>LoadAllHolonsAsync(HolonType.All,loadChildren,recursive,maxChildDepth,0,continueOnError,loadChildrenFromProvider,version),
                StringComparison.Ordinal);

        public OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonByCustomKeyAsync(customKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> snapshot=null;
            var parent=await FindStoredIdentityAsync(customKey,StoredCustomKey,
                async ()=>snapshot=await LoadAllHolonsAsync(HolonType.All,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version),StringComparison.Ordinal);
            if(parent.IsError)
            {
                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IHolon,IEnumerable<IHolon>>(parent);
            }
            return ProjectParentSnapshot(snapshot,parent.Result.Id,type,loadChildren,recursive,maxChildDepth,curentChildDepth);
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentByCustomKeyAsync(customKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await LoadHolonsByMetaDataAsync(new Dictionary<string,string> { [metaKey ?? string.Empty]=metaValue },
                MetaKeyValuePairMatchMode.All,type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result=new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if(metaKeyValuePairs == null || metaKeyValuePairs.Count == 0)
                    throw new ArgumentException("At least one metadata key/value pair is required.",nameof(metaKeyValuePairs));
                if(metaKeyValuePairs.Any(pair=>string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null))
                    throw new ArgumentException("Metadata keys must be nonempty and query values nonnull.",nameof(metaKeyValuePairs));
                if(metaKeyValuePairMatchMode != MetaKeyValuePairMatchMode.All && metaKeyValuePairMatchMode != MetaKeyValuePairMatchMode.Any)
                    throw new ArgumentOutOfRangeException(nameof(metaKeyValuePairMatchMode));
                var query=metaKeyValuePairs.ToArray();
                var stored=await LoadAllHolonsAsync(type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version);
                if(stored.IsError) return stored;
                bool Matches(IHolon row,KeyValuePair<string,string> pair)
                    => row.MetaData != null && row.MetaData.TryGetValue(pair.Key,out var value) && value != null
                        && string.Equals(Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture),pair.Value,StringComparison.Ordinal);
                result.Result=stored.Result.Where(row=>metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All
                    ? query.All(pair=>Matches(row,pair)) : query.Any(pair=>Matches(row,pair))).ToList();
                result.IsLoaded=true;
            }
            catch(Exception cause)
            {
                OASISErrorHandling.HandleError(ref result,$"Ethereum native metadata query failed: {cause.Message}",cause);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }



        private async Task<OASISResult<ITransactionResponse>> SendTransactionByIdInternalAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTransactionByIdAsync (token) in EthereumOASIS. Reason: ";

            try
            {
                var senderPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
                if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderPrivateKeysResult.Message), senderPrivateKeysResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, toAvatarId);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                var senderPrivateKey = senderPrivateKeysResult.Result[0];
                var toAddress = toWalletResult.Result;

                if (!string.IsNullOrWhiteSpace(token))
                    return await SendEthereumErc20Transaction(senderPrivateKey, token, toAddress, amount);
                else
                    return await SendEthereumTransaction(senderPrivateKey, toAddress, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
                return result;
            }
        }

        private async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameInternalAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTransactionByUsernameAsync (token) in EthereumOASIS. Reason: ";

            try
            {
                var senderPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarByUsername(fromAvatarUsername, Core.Enums.ProviderType.EthereumOASIS);
                if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderPrivateKeysResult.Message), senderPrivateKeysResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, toAvatarUsername);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                var senderPrivateKey = senderPrivateKeysResult.Result[0];
                var toAddress = toWalletResult.Result;

                if (!string.IsNullOrWhiteSpace(token))
                    return await SendEthereumErc20Transaction(senderPrivateKey, token, toAddress, amount);
                else
                    return await SendEthereumTransaction(senderPrivateKey, toAddress, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
                return result;
            }
        }

        private async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailInternalAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTransactionByEmailAsync (token) in EthereumOASIS. Reason: ";

            try
            {
                var senderPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarByUsername(fromAvatarEmail, Core.Enums.ProviderType.EthereumOASIS);
                if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderPrivateKeysResult.Message), senderPrivateKeysResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, toAvatarEmail);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                var senderPrivateKey = senderPrivateKeysResult.Result[0];
                var toAddress = toWalletResult.Result;

                if (!string.IsNullOrWhiteSpace(token))
                    return await SendEthereumErc20Transaction(senderPrivateKey, token, toAddress, amount);
                else
                    return await SendEthereumTransaction(senderPrivateKey, toAddress, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
                return result;
            }
        }

        private async Task<OASISResult<ITransactionResponse>> SendEthereumErc20Transaction(string senderAccountPrivateKey, string tokenContractAddress, string receiverAccountAddress, decimal amount)
        {
            return await ExecuteErc20MutationAsync(senderAccountPrivateKey, tokenContractAddress, receiverAccountAddress, amount, false);
        }



    }
}

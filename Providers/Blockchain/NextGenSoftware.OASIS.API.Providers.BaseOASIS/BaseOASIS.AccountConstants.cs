using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Util;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NextGenSoftware.OASIS.API.Providers.BaseOASIS;

public sealed partial class BaseOASIS
{
    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!_isActivated || _web3Client == null || _oasisAccount == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(receiverAccountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Receiver account address is required");
                return result;
            }

            if (amount <= 0)
            {
                OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                return result;
            }

            var transactionReceipt = await _web3Client.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, amount, 2);

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = transactionReceipt.TransactionHash,
                IsSuccessful = transactionReceipt.Status.Value == 1,
                Status = transactionReceipt.Status.Value == 1 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
    {
        var result = new OASISResult<BridgeTransactionStatus>();
        try
        {
            if (!_isActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Base provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(transactionHash))
            {
                OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                return result;
            }

            var transactionReceipt = await _web3Client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

            if (transactionReceipt == null)
            {
                result.Result = BridgeTransactionStatus.NotFound;
                result.IsError = true;
                result.Message = "Transaction not found.";
            }
            else if (transactionReceipt.Status.Value == 1)
            {
                result.Result = BridgeTransactionStatus.Completed;
                result.IsError = false;
            }
            else
            {
                result.Result = BridgeTransactionStatus.Canceled;
                result.IsError = true;
                result.Message = "Transaction failed on chain.";
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting Base transaction status: {ex.Message}", ex);
            result.Result = BridgeTransactionStatus.NotFound;
        }
        return result;
    }



    /// <summary>
    /// Parse Base blockchain response to list of Holon objects
    /// </summary>
    private IEnumerable<IHolon> ParseBaseToHolons(JsonElement jsonElement)
    {
        try
        {
            var holons = new List<IHolon>();

            if (jsonElement.TryGetProperty("result", out var result) &&
                result.TryGetProperty("rows", out var rows) &&
                rows.ValueKind == JsonValueKind.Array)
            {
                foreach (var row in rows.EnumerateArray())
                {
                    var holon = ParseBaseToHolon(row);
                    if (holon != null)
                        holons.Add(holon);
                }
            }
            else if (jsonElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in jsonElement.EnumerateArray())
                {
                    var holon = ParseBaseToHolon(element);
                    if (holon != null)
                        holons.Add(holon);
                }
            }

            return holons;
        }
        catch (Exception)
        {
            return new List<IHolon>();
        }
    }

    /// <summary>
    /// Parse Base blockchain response to Holon object
    /// </summary>
    private IHolon ParseBaseToHolon(JsonElement baseData)
    {
        try
        {
            var holon = new Holon();

            if (baseData.TryGetProperty("id", out var id))
                holon.Id = Guid.TryParse(id.GetString(), out var guid) ? guid : Guid.NewGuid();

            if (baseData.TryGetProperty("name", out var name))
                holon.Name = name.GetString();

            if (baseData.TryGetProperty("description", out var description))
                holon.Description = description.GetString();

            if (baseData.TryGetProperty("holon_type", out var holonType) || baseData.TryGetProperty("holonType", out holonType))
            {
                if (Enum.TryParse<HolonType>(holonType.GetString(), out var type))
                    holon.HolonType = type;
            }

            return holon;
        }
        catch (Exception)
        {
            return new Holon();
        }
    }

    /// <summary>
    /// Parse Base transaction response to TransactionResponse object
    /// </summary>
    private TransactionResponse ParseBaseToTransactionResponse(string content)
    {
        try
        {
            var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);

            return new TransactionResponse
            {
                TransactionResult = jsonElement.TryGetProperty("transactionHash", out var hashElement) ? hashElement.GetString() : ""
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing Base transaction response: {ex.Message}");
            return new TransactionResponse
            {
                TransactionResult = ""
            };
        }
    }

    private static IWeb3NFT ParseBaseToNFT(string content)
    {
        try
        {
            var jsonElement = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(content);
            
            var tokenAddress = jsonElement.TryGetProperty("tokenAddress", out var ta) ? ta.GetString() : jsonElement.TryGetProperty("address", out var addr) ? addr.GetString() : "unknown";
            return new Web3NFT
            {
                Id = BaseContractHelper.CreateDeterministicGuid($"{NextGenSoftware.OASIS.API.Core.Enums.ProviderType.BaseOASIS}:nft:{tokenAddress}"),
                Title = jsonElement.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : "Base NFT",
                Description = jsonElement.TryGetProperty("description", out var descElement) ? descElement.GetString() : "Base NFT Description",
                ImageUrl = jsonElement.TryGetProperty("imageUrl", out var imageElement) ? imageElement.GetString() : "",
                JSONMetaDataURL = jsonElement.TryGetProperty("metadataUrl", out var metadataElement) ? metadataElement.GetString() : "",
                NFTTokenAddress = jsonElement.TryGetProperty("contractAddress", out var contractElement) ? contractElement.GetString() : "",
                MintedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                MetaData = new Dictionary<string, string>
                {
                    { "BaseContent", content },
                    { "ProviderType", "BaseOASIS" }
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing Base NFT: {ex.Message}");
            return new Web3NFT
            {
                Id = BaseContractHelper.CreateDeterministicGuid($"{NextGenSoftware.OASIS.API.Core.Enums.ProviderType.BaseOASIS}:nft:error"),
                Title = "Base NFT",
                Description = "Base NFT Description",
                ImageUrl = "",
                JSONMetaDataURL = "",
                NFTTokenAddress = "",
                MintedOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                MetaData = new Dictionary<string, string>
                {
                    { "BaseContent", content },
                    { "ProviderType", "BaseOASIS" }
                }
            };
        }
    }

}

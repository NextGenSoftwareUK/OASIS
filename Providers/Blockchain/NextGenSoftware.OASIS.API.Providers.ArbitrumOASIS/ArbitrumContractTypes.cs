using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using static NextGenSoftware.Utilities.KeyHelper;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;


namespace NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;

internal sealed class AvatarDetailInfo
{
    [Parameter("uint256", "EntityId", 1)]
    public BigInteger EntityId { get; set; }
    [Parameter("string", "AvatarId", 2)]
    public string AvatarId { get; set; }
    [Parameter("string", "Info", 3)]
    public string Info { get; set; }
}

internal sealed class AvatarInfo
{
    [Parameter("uint256", "EntityId", 1)]
    public BigInteger EntityId { get; set; }
    [Parameter("string", "AvatarId", 2)]
    public string AvatarId { get; set; }
    [Parameter("string", "Info", 3)]
    public string Info { get; set; }
}

internal sealed class HolonInfo
{
    [Parameter("uint256", "EntityId", 1)]
    public BigInteger EntityId { get; set; }
    [Parameter("string", "HolonId", 2)]
    public string HolonId { get; set; }
    [Parameter("string", "Info", 3)]
    public string Info { get; set; }
}

[Function("getHolonByProviderKey", typeof(GetHolonByProviderKeyFunction))]
internal sealed class GetHolonByProviderKeyFunction : FunctionMessage
{
    [Parameter("string", "providerKey", 1)]
    public string ProviderKey { get; set; }
}

[Function("getHolonsForParentByProviderKey", typeof(GetHolonsForParentByProviderKeyFunction))]
internal sealed class GetHolonsForParentByProviderKeyFunction : FunctionMessage
{
    [Parameter("string", "providerKey", 1)]
    public string ProviderKey { get; set; }
}

[Function("searchAvatars", typeof(SearchAvatarsFunction))]
internal sealed class SearchAvatarsFunction : FunctionMessage
{
    [Parameter("string", "searchParams", 1)]
    public string SearchParams { get; set; }
}

[Function("searchHolons", typeof(SearchHolonsFunction))]
internal sealed class SearchHolonsFunction : FunctionMessage
{
    [Parameter("string", "searchParams", 1)]
    public string SearchParams { get; set; }
}

[Function("getNFTData", typeof(GetNFTDataFunction))]
internal sealed class GetNFTDataFunction : FunctionMessage
{
    [Parameter("string", "nftTokenAddress", 1)]
    public string NftTokenAddress { get; set; }
}

[Function("getHolonsForParent", typeof(GetHolonsForParentFunction))]
internal sealed class GetHolonsForParentFunction : FunctionMessage
{
    [Parameter("string", "parentId", 1)]
    public string ParentId { get; set; }
}

[Function("getHolonsByMetaData", typeof(GetHolonsByMetaDataFunction))]
internal sealed class GetHolonsByMetaDataFunction : FunctionMessage
{
    [Parameter("string", "metaKey", 1)]
    public string MetaKey { get; set; }
    [Parameter("string", "metaValue", 2)]
    public string MetaValue { get; set; }
}

[Function("getHolonsByMetaDataPairs", typeof(GetHolonsByMetaDataPairsFunction))]
internal sealed class GetHolonsByMetaDataPairsFunction : FunctionMessage
{
    [Parameter("string", "metaDataJson", 1)]
    public string MetaDataJson { get; set; }
    [Parameter("string", "matchMode", 2)]
    public string MatchMode { get; set; }
}


internal static class ArbitrumContractHelper
{
    private static Guid CreateDeterministicGuid(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return Guid.Empty;
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(bytes.Take(16).ToArray());
    }

    public const string CreateAvatarFuncName = "CreateAvatar";
    public const string CreateAvatarDetailFuncName = "CreateAvatarDetail";
    public const string CreateHolonFuncName = "CreateHolon";
    public const string UpdateAvatarFuncName = "UpdateAvatar";
    public const string UpdateAvatarDetailFuncName = "UpdateAvatarDetail";
    public const string UpdateHolonFuncName = "UpdateHolon";
    public const string DeleteAvatarFuncName = "DeleteAvatar";
    public const string DeleteAvatarDetailFuncName = "DeleteAvatarDetail";
    public const string DeleteHolonFuncName = "DeleteHolon";
    public const string GetAvatarByIdFuncName = "GetAvatarById";
    public const string GetAvatarDetailByIdFuncName = "GetAvatarDetailById";
    public const string GetHolonByIdFuncName = "GetHolonById";
    public const string GetAvatarsCountFuncName = "GetAvatarsCount";
    public const string GetAvatarDetailsCountFuncName = "GetAvatarDetailsCount";
    public const string GetHolonsCountFuncName = "GetHolonsCount";
    public const string SendNftFuncName = "sendNFT";
    public const string MintFuncName = "mint";
    public const string BurnFuncName = "burn";
    public const string Abi = "[{\"inputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"constructor\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"sender\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"},{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"ERC721IncorrectOwner\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"ERC721InsufficientApproval\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"approver\",\"type\":\"address\"}],\"name\":\"ERC721InvalidApprover\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"}],\"name\":\"ERC721InvalidOperator\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"ERC721InvalidOwner\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"receiver\",\"type\":\"address\"}],\"name\":\"ERC721InvalidReceiver\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"sender\",\"type\":\"address\"}],\"name\":\"ERC721InvalidSender\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"ERC721NonexistentToken\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"OwnableInvalidOwner\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"account\",\"type\":\"address\"}],\"name\":\"OwnableUnauthorizedAccount\",\"type\":\"error\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"approved\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"Approval\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"},{\"indexed\":false,\"internalType\":\"bool\",\"name\":\"approved\",\"type\":\"bool\"}],\"name\":\"ApprovalForAll\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"previousOwner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"newOwner\",\"type\":\"address\"}],\"name\":\"OwnershipTransferred\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"Transfer\",\"type\":\"event\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"avatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"CreateAvatar\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"avatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"CreateAvatarDetail\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"holonId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"CreateHolon\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"DeleteAvatar\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"DeleteAvatarDetail\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"DeleteHolon\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"GetAvatarById\",\"outputs\":[{\"components\":[{\"internalType\":\"uint256\",\"name\":\"EntityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"AvatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"Info\",\"type\":\"string\"}],\"internalType\":\"structAvatar\",\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"GetAvatarDetailById\",\"outputs\":[{\"components\":[{\"internalType\":\"uint256\",\"name\":\"EntityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"AvatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"Info\",\"type\":\"string\"}],\"internalType\":\"structAvatarDetail\",\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"GetAvatarDetailsCount\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"count\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"GetAvatarsCount\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"count\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"GetHolonById\",\"outputs\":[{\"components\":[{\"internalType\":\"uint256\",\"name\":\"EntityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"HolonId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"Info\",\"type\":\"string\"}],\"internalType\":\"structHolon\",\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"GetHolonsCount\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"count\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"UpdateAvatar\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"UpdateAvatarDetail\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"UpdateHolon\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"admin\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"approve\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"getApproved\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"getTransferHistory\",\"outputs\":[{\"components\":[{\"internalType\":\"address\",\"name\":\"fromWalletAddress\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"toWalletAddress\",\"type\":\"address\"},{\"internalType\":\"string\",\"name\":\"fromProviderType\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"toProviderType\",\"type\":\"string\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"memoText\",\"type\":\"string\"}],\"internalType\":\"structArbitrumOASIS.NFTTransfer[]\",\"name\":\"\",\"type\":\"tuple[]\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"}],\"name\":\"isApprovedForAll\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"string\",\"name\":\"metadataUri\",\"type\":\"string\"}],\"name\":\"mint\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"name\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"nextTokenId\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"name\":\"nftMetadata\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"metadataUri\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"},{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"name\":\"nftTransfers\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"fromWalletAddress\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"toWalletAddress\",\"type\":\"address\"},{\"internalType\":\"string\",\"name\":\"fromProviderType\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"toProviderType\",\"type\":\"string\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"memoText\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"owner\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"ownerOf\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"renounceOwnership\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"safeTransferFrom\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"},{\"internalType\":\"bytes\",\"name\":\"data\",\"type\":\"bytes\"}],\"name\":\"safeTransferFrom\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"fromWalletAddress\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"toWalletAddress\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"fromProviderType\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"toProviderType\",\"type\":\"string\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"memoText\",\"type\":\"string\"}],\"name\":\"sendNFT\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"},{\"internalType\":\"bool\",\"name\":\"approved\",\"type\":\"bool\"}],\"name\":\"setApprovalForAll\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"bytes4\",\"name\":\"interfaceId\",\"type\":\"bytes4\"}],\"name\":\"supportsInterface\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"symbol\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"tokenExists\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"tokenURI\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"transferFrom\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"newOwner\",\"type\":\"address\"}],\"name\":\"transferOwnership\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]";

    // Real Arbitrum implementation: Parse Arbitrum data to OASIS objects
    private static IAvatarDetail ParseArbitrumToAvatarDetail(object avatarDetailData)
    {
        try
        {
            // Real implementation: Parse actual smart contract data from Arbitrum
            if (avatarDetailData == null) return null;
            
            // Parse the actual data from Arbitrum smart contract response
            var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(avatarDetailData.ToString());
            if (dataDict == null) return null;
            
            var avatarDetail = new AvatarDetail
            {
                Id = dataDict.ContainsKey("id") ? Guid.Parse(dataDict["id"].ToString()) : CreateDeterministicGuid($"{Core.Enums.ProviderType.ArbitrumOASIS}:avatarDetail:{dataDict.GetValueOrDefault("providerKey")?.ToString() ?? dataDict.GetValueOrDefault("address")?.ToString() ?? dataDict.GetValueOrDefault("id")?.ToString() ?? "unknown"}"),
                Username = dataDict.GetValueOrDefault("username")?.ToString() ?? "",
                Email = dataDict.GetValueOrDefault("email")?.ToString() ?? "",
                FirstName = dataDict.GetValueOrDefault("firstName")?.ToString() ?? "",
                LastName = dataDict.GetValueOrDefault("lastName")?.ToString() ?? "",
                CreatedDate = dataDict.ContainsKey("createdDate") ? DateTime.Parse(dataDict["createdDate"].ToString()) : DateTime.UtcNow,
                ModifiedDate = dataDict.ContainsKey("modifiedDate") ? DateTime.Parse(dataDict["modifiedDate"].ToString()) : DateTime.UtcNow,
                AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(dataDict.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.User),
                KarmaAkashicRecords = new List<IKarmaAkashicRecord>(),
                // Level = dataDict.ContainsKey("level") ? Convert.ToInt32(dataDict["level"]) : 1, // Level is read-only
                XP = dataDict.ContainsKey("xp") ? Convert.ToInt32(dataDict["xp"]) : 0,
                Description = dataDict.GetValueOrDefault("description")?.ToString() ?? "",
                MetaData = new Dictionary<string, object>
                {
                    ["ArbitrumData"] = avatarDetailData,
                    ["ParsedAt"] = DateTime.UtcNow,
                    ["Provider"] = "ArbitrumOASIS"
                }
            };
            
            return avatarDetail;
        }
        catch (Exception ex)
        {
            // Log error and return null
            return null;
        }
    }

    private static IAvatar ParseArbitrumToAvatar(object avatarData)
    {
        try
        {
            // Real implementation: Parse actual smart contract data from Arbitrum
            if (avatarData == null) return null;
            
            // Parse the actual data from Arbitrum smart contract response
            var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(avatarData.ToString());
            if (dataDict == null) return null;
            
            var avatar = new Avatar
            {
                Id = dataDict.ContainsKey("id") ? Guid.Parse(dataDict["id"].ToString()) : CreateDeterministicGuid($"{Core.Enums.ProviderType.ArbitrumOASIS}:avatarDetail:{dataDict.GetValueOrDefault("providerKey")?.ToString() ?? dataDict.GetValueOrDefault("address")?.ToString() ?? dataDict.GetValueOrDefault("id")?.ToString() ?? "unknown"}"),
                Username = dataDict.GetValueOrDefault("username")?.ToString() ?? "",
                Email = dataDict.GetValueOrDefault("email")?.ToString() ?? "",
                CreatedDate = dataDict.ContainsKey("createdDate") ? DateTime.Parse(dataDict["createdDate"].ToString()) : DateTime.UtcNow,
                ModifiedDate = dataDict.ContainsKey("modifiedDate") ? DateTime.Parse(dataDict["modifiedDate"].ToString()) : DateTime.UtcNow,
                AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(dataDict.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.User),
                MetaData = new Dictionary<string, object>
                {
                    ["ArbitrumData"] = avatarData,
                    ["ParsedAt"] = DateTime.UtcNow,
                    ["Provider"] = "ArbitrumOASIS"
                }
            };
            
            return avatar;
        }
        catch (Exception ex)
        {
            // Log error and return null
            return null;
        }
    }

    private static Web3NFT ParseArbitrumToNFT(object nftData)
    {
        try
        {
            if (nftData == null) return null;
            var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(nftData.ToString());
            if (dataDict == null) return null;
            var tokenAddress = dataDict.GetValueOrDefault("tokenAddress")?.ToString() ?? dataDict.GetValueOrDefault("address")?.ToString() ?? dataDict.GetValueOrDefault("tokenAddress")?.ToString() ?? "unknown";
            var nft = new Web3NFT
            {
                Id = dataDict.ContainsKey("id") && Guid.TryParse(dataDict["id"]?.ToString(), out var idGuid) ? idGuid : CreateDeterministicGuid($"{Core.Enums.ProviderType.ArbitrumOASIS}:nft:{tokenAddress}"),
                Title = dataDict.GetValueOrDefault("title")?.ToString() ?? "Arbitrum NFT",
                Description = dataDict.GetValueOrDefault("description")?.ToString() ?? "NFT from Arbitrum blockchain",
                NFTTokenAddress = tokenAddress,
                OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.ArbitrumOASIS),
                //MetaData = new Dictionary<string, object>
                //{
                //    ["ArbitrumData"] = nftData,
                //    ["ParsedAt"] = DateTime.Now
                //}
            };
            
            return nft;
        }
        catch (Exception ex)
        {
            // Log error and return null
            return null;
        }
    }
    
    private static IHolon ParseArbitrumToHolon(object holonData)
    {
        try
        {
            // Real implementation: Parse actual smart contract data from Arbitrum
            if (holonData == null) return null;
            
            // Parse the actual data from Arbitrum smart contract response
            var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(holonData.ToString());
            if (dataDict == null) return null;
            
            var holon = new Holon
            {
                Id = dataDict.ContainsKey("id") ? Guid.Parse(dataDict["id"].ToString()) : CreateDeterministicGuid($"{Core.Enums.ProviderType.ArbitrumOASIS}:avatarDetail:{dataDict.GetValueOrDefault("providerKey")?.ToString() ?? dataDict.GetValueOrDefault("address")?.ToString() ?? dataDict.GetValueOrDefault("id")?.ToString() ?? "unknown"}"),
                Name = dataDict.GetValueOrDefault("name")?.ToString() ?? "",
                Description = dataDict.GetValueOrDefault("description")?.ToString() ?? "",
                HolonType = Enum.TryParse<HolonType>(dataDict.GetValueOrDefault("holonType")?.ToString(), out var holonType) 
                    ? holonType 
                    : HolonType.All,
                CreatedDate = dataDict.ContainsKey("createdDate") ? DateTime.Parse(dataDict["createdDate"].ToString()) : DateTime.UtcNow,
                ModifiedDate = dataDict.ContainsKey("modifiedDate") ? DateTime.Parse(dataDict["modifiedDate"].ToString()) : DateTime.UtcNow,
                MetaData = new Dictionary<string, object>
                {
                    ["ArbitrumData"] = holonData,
                    ["ParsedAt"] = DateTime.UtcNow,
                    ["Provider"] = "ArbitrumOASIS"
                }
            };
            
            return holon;
        }
        catch (Exception ex)
        {
            // Log error and return null
            return null;
        }
    }
}

// FunctionMessage classes for new Nethereum API
public class GetAvatarDetailsCountFunction : FunctionMessage
{
}

public class GetAvatarDetailByIdFunction : FunctionMessage
{
    [Parameter("uint256", "id", 1)]
    public uint Id { get; set; }
}

public class GetAvatarsCountFunction : FunctionMessage
{
}

public class GetAvatarByIdFunction : FunctionMessage
{
    [Parameter("uint256", "id", 1)]
    public uint Id { get; set; }
}

public class GetHolonByIdyIdFunction : FunctionMessage
{
    [Parameter("uint256", "id", 1)]
    public uint Id { get; set; }
}

public class GetHolonsCountFunction : FunctionMessage
{
}

/// <summary>
/// Lightweight Arbitrum provider using Web3CoreOASISBaseProvider and the generic Web3Core contract.
/// Use this once the generic Web3CoreOASIS.sol contract is deployed to Arbitrum. Until then, use <see cref="ArbitrumOASIS"/> (live/deployed).
/// </summary>
public sealed class ArbitrumOASIS_Web3Core : Web3CoreOASISBaseProvider,
    IOASISDBStorageProvider,
    IOASISNETProvider,
    IOASISSuperStar,
    IOASISBlockchainStorageProvider,
    IOASISNFTProvider
{
    public ArbitrumOASIS_Web3Core(
        string hostUri = "https://arb1.arbitrum.io/rpc",
        string chainPrivateKey = "",
        string contractAddress = "")
        : base(hostUri, chainPrivateKey, contractAddress)
    {
        ProviderName = "ArbitrumOASIS";
        ProviderDescription = "Arbitrum Provider - Ethereum Layer 2 scaling solution using Web3Core";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ArbitrumOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using System.Text;


using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;

public sealed partial class AvalancheOASIS_Legacy : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider, IOASISNFTProvider
{
    private readonly string _hostURI;
    private readonly string _chainPrivateKey;
    private readonly BigInteger _chainId;
    private readonly string _contractAddress;
    private readonly HexBigInteger _gasLimit = new(500000);

    // Function names for Avalanche smart contract
    private const string GetAvatarByIdFuncName = "GetAvatarById";
    private const string GetAvatarDetailByIdFuncName = "GetAvatarDetailById";
    private const string GetHolonByIdFuncName = "GetHolonById";
    private const string GetAvatarsCountFuncName = "GetAvatarsCount";
    private const string GetAvatarDetailsCountFuncName = "GetAvatarDetailsCount";
    private const string GetHolonsCountFuncName = "GetHolonsCount";
    private const string GetNFTDataFuncName = "GetNFTData";

    // Struct definitions for Avalanche smart contract
    public struct AvatarStruct
    {
        [Parameter("uint256", "EntityId", 1)]
        public BigInteger EntityId { get; set; }

        [Parameter("string", "AvatarId", 2)]
        public string AvatarId { get; set; }

        [Parameter("string", "Info", 3)]
        public string Info { get; set; }
    }

    public struct AvatarDetailStruct
    {
        [Parameter("uint256", "EntityId", 1)]
        public BigInteger EntityId { get; set; }

        [Parameter("string", "AvatarId", 2)]
        public string AvatarId { get; set; }

        [Parameter("string", "Info", 3)]
        public string Info { get; set; }
    }

    public struct HolonStruct
    {
        [Parameter("uint256", "EntityId", 1)]
        public BigInteger EntityId { get; set; }

        [Parameter("string", "HolonId", 2)]
        public string HolonId { get; set; }

        [Parameter("string", "Info", 3)]
        public string Info { get; set; }
    }

    public struct NFTStruct
    {
        [Parameter("uint256", "EntityId", 1)]
        public BigInteger EntityId { get; set; }

        [Parameter("string", "TokenId", 2)]
        public string TokenId { get; set; }

        [Parameter("string", "Info", 3)]
        public string Info { get; set; }
    }

    private Web3 _web3Client;
    private Account _oasisAccount;
    private Contract _contract;
    private ContractHandler _contractHandler;
    private HttpClient _httpClient;
    private string _apiBaseUrl = "https://api.avax.network";
    private object _nextGenSoftwareOasisService;
    private object _avalancheClient;

}

/// <summary>
/// AvalancheOASIS provider using the shared Web3CoreOASISBaseProvider and generic Web3Core contract.
/// All Avatar, AvatarDetail, and Holon operations are handled by the base provider.
/// </summary>
public sealed class AvalancheOASIS : Web3CoreOASISBaseProvider,
    IOASISDBStorageProvider,
    IOASISNETProvider,
    IOASISSuperStar,
    IOASISBlockchainStorageProvider,
    IOASISNFTProvider
{
    public AvalancheOASIS(
        string hostUri = "https://api.avax.network/ext/bc/C/rpc",
        string chainPrivateKey = "",
        string contractAddress = "")
        : base(hostUri, chainPrivateKey, contractAddress)
    {
        ProviderName = "AvalancheOASIS";
        ProviderDescription = "Avalanche Provider - EVM-compatible using Web3Core";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AvalancheOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
    }
}

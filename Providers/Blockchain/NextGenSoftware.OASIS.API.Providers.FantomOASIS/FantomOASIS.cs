using System;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.FantomOASIS
{
    public class FantomTransactionResponse : ITransactionResponse
    {
        public string TransactionResult { get; set; }
        public string MemoText { get; set; }
    }

    // Struct definitions for Fantom smart contract
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

    /// <summary>
    /// DTO for GetHolon function output from Fantom smart contract
    /// </summary>
    public class GetHolonOutputDTO
    {
        [Parameter("string", "name", 1)]
        public string Name { get; set; }

        [Parameter("string", "description", 2)]
        public string Description { get; set; }

        [Parameter("string", "holonType", 3)]
        public string HolonType { get; set; }

        [Parameter("string", "metadata", 4)]
        public string Metadata { get; set; }

        [Parameter("string", "parentId", 5)]
        public string ParentId { get; set; }
    }

    /// <summary>
    /// DTO for GetAvatar function output from Fantom smart contract
    /// </summary>
    public class GetAvatarOutputDTO
    {
        [Parameter("string", "username", 1)]
        public string Username { get; set; }

        [Parameter("string", "email", 2)]
        public string Email { get; set; }

        [Parameter("string", "firstName", 3)]
        public string FirstName { get; set; }

        [Parameter("string", "lastName", 4)]
        public string LastName { get; set; }

        [Parameter("string", "avatarType", 5)]
        public string AvatarType { get; set; }

        [Parameter("string", "metadata", 6)]
        public string Metadata { get; set; }
    }

    /// <summary>
    /// Legacy Fantom provider implementation using a chain-specific contract and custom Nethereum logic.
    /// This class is kept only for reference and backward compatibility and is no longer used by OASIS at runtime.
    /// The new FantomOASIS provider below delegates all logic to the shared Web3CoreOASISBaseProvider and generic Web3Core contract.
    /// </summary>
    public partial class FantomOASIS_Legacy : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _rpcEndpoint;
        private readonly string _chainId;
        private readonly string _privateKey;
        private readonly string _contractAddress;
        private bool _isActivated;
        private WalletManager _walletManager;
        private Web3 _web3Client;
        private Account _account;
        private Contract _contract;

        public WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = new WalletManager(this, OASISDNA);
                return _walletManager;
            }
            set => _walletManager = value;
        }

        /// <summary>
        /// Initializes a new instance of the legacy FantomOASIS provider.
        /// </summary>
        /// <param name="rpcEndpoint">Fantom RPC endpoint URL</param>
        /// <param name="chainId">Fantom chain ID (250 for mainnet, 4002 for testnet)</param>
        /// <param name="privateKey">Private key for signing transactions</param>
        public FantomOASIS_Legacy(string rpcEndpoint = "https://rpc.ftm.tools", string chainId = "250", string privateKey = "", string contractAddress = "0x0000000000000000000000000000000000000000")
        {
            this.ProviderName = "FantomOASIS";
            this.ProviderDescription = "Fantom Provider - High-performance EVM-compatible blockchain";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.FantomOASIS);
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            _rpcEndpoint = rpcEndpoint ?? throw new ArgumentNullException(nameof(rpcEndpoint));
            _chainId = chainId ?? throw new ArgumentNullException(nameof(chainId));
            _privateKey = privateKey;
            _contractAddress = contractAddress;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_rpcEndpoint)
            };
        }
    }

    public sealed class FantomOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        public FantomOASIS(
            string hostUri = "https://rpc.ftm.tools",
            string chainPrivateKey = "",
            string contractAddress = "")
            : base(hostUri, chainPrivateKey, contractAddress)
        {
            ProviderName = "FantomOASIS";
            ProviderDescription = "Fantom Provider - High-performance EVM-compatible blockchain using Web3Core";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.FantomOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }
}

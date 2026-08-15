using System;
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
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.OptimismOASIS
{
    public class OptimismTransactionResponse : ITransactionResponse
    {
        public string TransactionResult { get; set; }
        public string MemoText { get; set; }
    }

    /// <summary>
    /// DTO for GetAvatar function output from Optimism smart contract
    /// </summary>
    public class GetAvatarOutputDTO
    {
        [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("string", "username", 1)]
        public string Username { get; set; }

        [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("string", "email", 2)]
        public string Email { get; set; }

        [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("string", "firstName", 3)]
        public string FirstName { get; set; }

        [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("string", "lastName", 4)]
        public string LastName { get; set; }

        [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("string", "avatarType", 5)]
        public string AvatarType { get; set; }

        [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("string", "metadata", 6)]
        public string Metadata { get; set; }
    }

    /// <summary>
    /// DTO for GetHolon function output from Optimism smart contract
    /// </summary>
    public class GetHolonOutputDTO
    {
        [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("string", "name", 1)]
        public string Name { get; set; }

        [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("string", "description", 2)]
        public string Description { get; set; }

        [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("string", "holonType", 3)]
        public string HolonType { get; set; }

        [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("string", "metadata", 4)]
        public string Metadata { get; set; }

        [Nethereum.ABI.FunctionEncoding.Attributes.Parameter("string", "parentId", 5)]
        public string ParentId { get; set; }
    }
    /// <summary>
    /// Legacy Optimism provider implementation using a chain-specific contract and custom Nethereum logic.
    /// This class is kept only for reference and backward compatibility and is no longer used by OASIS at runtime.
    /// The new OptimismOASIS provider below delegates all logic to the shared Web3CoreOASISBaseProvider and generic Web3Core contract.
    /// </summary>
    public partial class OptimismOASIS_Legacy : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider
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
        private KeyManager _keyManager;

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

        public KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                    _keyManager = new KeyManager(this, OASISDNA);
                return _keyManager;
            }
            set => _keyManager = value;
        }

        /// <summary>
        /// Initializes a new instance of the OptimismOASIS provider
        /// </summary>
        /// <param name="rpcEndpoint">Optimism RPC endpoint URL</param>
        /// <param name="chainId">Optimism chain ID</param>
        /// <param name="privateKey">Private key for signing transactions</param>
        public OptimismOASIS_Legacy(string rpcEndpoint = "https://mainnet.optimism.io", string chainId = "10", string privateKey = "", string contractAddress = "")
        {
            this.ProviderName = "OptimismOASIS";
            this.ProviderDescription = "Optimism Provider - Ethereum Layer 2 scaling solution";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.OptimismOASIS);
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

    public sealed class OptimismOASIS : Web3CoreOASISBaseProvider,
        IOASISDBStorageProvider,
        IOASISNETProvider,
        IOASISSuperStar,
        IOASISBlockchainStorageProvider,
        IOASISNFTProvider
    {
        public OptimismOASIS(
            string hostUri = "https://mainnet.optimism.io",
            string chainPrivateKey = "",
            string contractAddress = "")
            : base(hostUri, chainPrivateKey, contractAddress)
        {
            ProviderName = "OptimismOASIS";
            ProviderDescription = "Optimism Provider - Ethereum Layer 2 using Web3Core";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.OptimismOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }
    }
}

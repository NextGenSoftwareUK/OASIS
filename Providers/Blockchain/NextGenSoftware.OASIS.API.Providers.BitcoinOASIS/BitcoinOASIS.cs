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
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS
{
    public partial class BitcoinOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _rpcEndpoint;
        private readonly string _network;
        private readonly string _privateKey;
        private readonly decimal? _paymentFeeRateSatPerVByte;
        private bool _isActivated;
        private WalletManager _walletManager;

        private Network SigningNetwork => _network.ToLowerInvariant() switch
        {
            "mainnet" => Network.Main,
            "testnet" => Network.TestNet,
            "regtest" => Network.RegTest,
            _ => throw new InvalidOperationException("The configured Bitcoin network is unsupported.")
        };

        private async Task<Transaction> ReadVerifiedTransactionAsync(uint256 transactionId)
        {
            using var response = await _httpClient.GetAsync($"tx/{transactionId}/hex");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new BitcoinTransactionNotFoundException(transactionId);

            response.EnsureSuccessStatusCode();
            var hex = await response.Content.ReadAsStringAsync();
            // The Bitcoin wire format and transaction hash are the same across Bitcoin networks.
            var transaction = Transaction.Parse(hex.Trim(), Network.Main);
            if (transaction.GetHash() != transactionId)
                throw new InvalidOperationException("Esplora returned a transaction with a different ID.");
            return transaction;
        }

        private sealed class BitcoinTransactionNotFoundException(uint256 transactionId)
            : Exception($"Esplora has no Bitcoin transaction with ID {transactionId}.")
        {
        }

        public WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = WalletManager.Instance;
                return _walletManager;
            }
            set => _walletManager = value;
        }

        /// <summary>
        /// Initializes a new instance of the BitcoinOASIS provider
        /// </summary>
        /// <param name="rpcEndpoint">Bitcoin RPC endpoint URL</param>
        /// <param name="network">Bitcoin network (mainnet, testnet, regtest)</param>
        /// <param name="privateKey">Private key for signing transactions</param>
        /// <param name="walletManager">WalletManager for wallet operations</param>
        public BitcoinOASIS(string rpcEndpoint = "https://blockstream.info/api", string network = "mainnet", string privateKey = "", WalletManager walletManager = null)
            : this(rpcEndpoint, network, privateKey, walletManager, null)
        {
        }

        public BitcoinOASIS(string rpcEndpoint, string network, string privateKey, WalletManager walletManager, decimal? paymentFeeRateSatPerVByte)
            : this(rpcEndpoint, network, privateKey, walletManager, paymentFeeRateSatPerVByte, null)
        {
        }

        public BitcoinOASIS(string rpcEndpoint, string network, string privateKey, WalletManager walletManager,
            decimal? paymentFeeRateSatPerVByte, HolonManager escrowReceipts)
        {
            _walletManager = walletManager;
            _bitcoinEscrowReceipts = escrowReceipts;
            this.ProviderName = "BitcoinOASIS";
            this.ProviderDescription = "Bitcoin Provider - First and largest cryptocurrency";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.BitcoinOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.Blockchain);
            this.ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            _rpcEndpoint = rpcEndpoint ?? throw new ArgumentNullException(nameof(rpcEndpoint));
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _privateKey = privateKey;
            if (paymentFeeRateSatPerVByte is <= 0)
                throw new ArgumentOutOfRangeException(nameof(paymentFeeRateSatPerVByte), "The fee rate must be positive.");
            _paymentFeeRateSatPerVByte = paymentFeeRateSatPerVByte;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_rpcEndpoint.TrimEnd('/') + "/")
            };
        }


    }

    public class BitcoinTransactionResponse : ITransactionResponse
    {
        public string TransactionResult { get; set; }
        public string MemoText { get; set; }
    }
}

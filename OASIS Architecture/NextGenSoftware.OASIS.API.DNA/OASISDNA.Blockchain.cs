using System;
using System.Collections.Generic;
using NextGenSoftware.ErrorHandling;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.Core.Configuration;

namespace NextGenSoftware.OASIS.API.DNA
{
    public class BitcoinOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://blockstream.info/api";
        public string Network { get; set; } = "mainnet";
    }

    public class CardanoOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://cardano-mainnet.blockfrost.io/api/v0";
        public string NetworkId { get; set; } = "mainnet";
        public string ProjectId { get; set; }
    }

    public class PolkadotOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "wss://rpc.polkadot.io";
        public string Network { get; set; } = "polkadot";
    }

    public class BNBChainOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://bsc-dataseed.binance.org";
        public string NetworkId { get; set; } = "56";
        public string ChainId { get; set; } = "0x38";
    }

    public class FantomOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://rpc.ftm.tools";
        public string NetworkId { get; set; } = "250";
        public string ChainId { get; set; } = "0xfa";
    }

    public class OptimismOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://mainnet.optimism.io";
        public string NetworkId { get; set; } = "10";
        public string ChainId { get; set; } = "0xa";
    }

    public class ChainLinkOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://mainnet.infura.io/v3/YOUR_PROJECT_ID";
        public string NetworkId { get; set; } = "1";
        public string ChainId { get; set; } = "0x1";
    }

    public class ElrondOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://api.elrond.com";
        public string Network { get; set; } = "mainnet";
        public string ChainId { get; set; } = "1";
    }

    public class AptosOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://api.mainnet.aptoslabs.com/v1";
        public string Network { get; set; } = "mainnet";
        public string ChainId { get; set; } = "1";
        public string PrivateKey { get; set; } = "";
        public string ContractAddress { get; set; } = "0x1";
    }

    public class TRONOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://api.trongrid.io";
        public string Network { get; set; } = "mainnet";
        public string ChainId { get; set; } = "0x2b6653dc";
    }

    public class HashgraphOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://mainnet-public.mirrornode.hedera.com/api/v1";
        public string Network { get; set; } = "mainnet";
        public string ChainId { get; set; } = "295";
    }

    public class AvalancheOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://api.avax.network/ext/bc/C/rpc";
        public string NetworkId { get; set; } = "43114";
        public string ChainId { get; set; } = "0xa86a";
        public string ChainPrivateKey { get; set; } = "";
        public string ContractAddress { get; set; } = "";
    }

    public class CosmosBlockChainOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://cosmos-rpc.polkachu.com";
        public string Network { get; set; } = "cosmos";
        public string ChainId { get; set; } = "cosmoshub-4";
    }

    public class NEAROASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://rpc.mainnet.near.org";
        public string Network { get; set; } = "mainnet";
        public string ChainId { get; set; } = "mainnet";
    }

    public class BaseOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://mainnet.base.org";
        public string NetworkId { get; set; } = "8453";
        public string ChainId { get; set; } = "0x2105";
        public string ChainPrivateKey { get; set; } = "";
        public string ContractAddress { get; set; } = "";
    }

    public class SuiOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://fullnode.mainnet.sui.io:443";
        public string Network { get; set; } = "mainnet";
        public string ChainId { get; set; } = "mainnet";
        public string ContractAddress { get; set; } = "";
    }

    public class MoralisOASISProviderSettings : ProviderSettingsBase
    {
        public string ApiKey { get; set; }
        public string RpcEndpoint { get; set; } = "https://speedy-nodes-nyc.moralis.io/YOUR_API_KEY/eth/mainnet";
        public string Network { get; set; } = "mainnet";
    }

    public class AztecOASISProviderSettings : ProviderSettingsBase
    {
        /// <summary>Base URL of the Aztec node API (e.g. http://localhost:8080 for sandbox or a testnet/mainnet endpoint).</summary>
        public string ApiBaseUrl { get; set; } = "http://localhost:8080";
        /// <summary>Optional API key for the Aztec node.</summary>
        public string ApiKey { get; set; } = "";
        /// <summary>Deployed Aztec bridge contract address. Required for Deposit/Withdraw operations.</summary>
        public string BridgeContractAddress { get; set; } = "";
        /// <summary>Alias of the OASIS operator account registered in the aztec-wallet CLI. Used for deposit transactions.</summary>
        public string OperatorAccountAlias { get; set; } = "oasis_operator";
        /// <summary>Network name passed to aztec-wallet CLI (e.g. "sandbox", "testnet", "mainnet").</summary>
        public string Network { get; set; } = "sandbox";
    }

    public class TelosOASISProviderSettings : ProviderSettingsBase
    {
        public string RpcEndpoint { get; set; } = "https://api.telos.net";
        public string Network { get; set; } = "mainnet";
        public string ChainId { get; set; } = "4667b205c6838ef70ff7988f6e8257e8be0e1284a2f59699054a018f743b1d11";
    }

    public class ActivityPubOASISProviderSettings : ProviderSettingsBase
    {
        public string BaseUrl { get; set; } = "https://mastodon.social/api/v1";
        public string UserAgent { get; set; } = "OASIS-ActivityPub-Provider/1.0";
        public string AcceptHeader { get; set; } = "application/json";
        public int TimeoutSeconds { get; set; } = 30;
        public bool EnableCaching { get; set; } = true;
        public int CacheExpirationMinutes { get; set; } = 15;
    }

    public class GoogleCloudOASISProviderSettings : ProviderSettingsBase
    {
        public string ProjectId { get; set; } = "oasis-project";
        public string BucketName { get; set; } = "oasis-storage";
        public string CredentialsPath { get; set; }
        public string FirestoreDatabaseId { get; set; } = "(default)";
        public string BigQueryDatasetId { get; set; } = "oasis_data";
        public bool EnableStorage { get; set; } = true;
        public bool EnableFirestore { get; set; } = true;
        public bool EnableBigQuery { get; set; } = true;
    }

    /// <summary>
    /// ONET (OASIS Network) P2P configuration. Controls how this node discovers peers, identifies
    /// itself on the network, and which P2P transport layer to use.
    ///
    /// NetworkType options:
    ///   "Internal"  — custom Kademlia DHT + mDNS + TCP :38470 (default, no extra deps)
    ///   "HoloNET"   — delegates P2P to Holochain/HoloNET; requires HoloOASIS as storage provider
    ///                 and a running Holochain conductor (conductor URL comes from StorageProviders.HoloOASIS)
    ///
    /// NodeId / NodePublicKey / NodePrivateKey are generated automatically on first ONODE start if blank.
    /// For User DNA the private key is DPAPI-protected on disk; for System DNA it lives in the encrypted blob.
    /// </summary>
    public class ONETConfig
    {
        /// <summary>
        /// Servers used to bootstrap peer discovery. ONETDiscovery calls GET {server}/api/v1/onet/network/nodes
        /// on each entry to retrieve the initial peer list. At least one entry is required for non-LAN deployments.
        /// </summary>
        public List<string> BootstrapServers { get; set; } = new List<string>
        {
            "https://api.web4.oasisomniverse.one"
        };

        /// <summary>
        /// P2P transport layer: "Internal" (Kademlia/mDNS/TCP) or "HoloNET" (Holochain conductor).
        /// HoloNET mode requires StorageProviders.HoloOASIS to be configured and a conductor running.
        /// </summary>
        public string NetworkType { get; set; } = "Internal";

        /// <summary>Stable node identifier — SHA-256 of NodePublicKey. Auto-generated on first run if blank.</summary>
        public string NodeId { get; set; } = "";

        /// <summary>Base-64 encoded ECDSA-P256 public key for this node. Auto-generated on first run if blank.</summary>
        public string NodePublicKey { get; set; } = "";

        /// <summary>
        /// Base-64 encoded ECDSA-P256 private key. Auto-generated on first run if blank.
        /// In User DNA this field is encrypted by DPAPI before being written to disk.
        /// In System DNA it is protected by the AES key embedded in DNALoader.
        /// Never store this in plaintext in source control.
        /// </summary>
        public string NodePrivateKey { get; set; } = "";

        /// <summary>TCP port ONETProtocol listens on for peer connections. Default matches the hardcoded constant in ONETProtocol.</summary>
        public int TcpPort { get; set; } = 38470;

        /// <summary>Enable mDNS local-network peer discovery (Internal mode only).</summary>
        public bool EnableMDNS { get; set; } = true;

        /// <summary>
        /// Register this node with each bootstrap server on startup via POST /api/v1/onet/nodes/register,
        /// supplying NodeId and NodePublicKey so the server can verify future ECDSA-signed requests.
        /// </summary>
        public bool AutoRegisterOnBootstrap { get; set; } = true;

        /// <summary>
        /// Shared secret required in the X-ONET-API-Key header for POST /onet/nodes/register.
        /// If empty, the check is skipped (open registration). Set a strong random value in production.
        /// </summary>
        public string ONETApiKey { get; set; } = "";
    }
}

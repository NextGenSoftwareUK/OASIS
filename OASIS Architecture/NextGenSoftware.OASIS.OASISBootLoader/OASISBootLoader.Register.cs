//using NextGenSoftware.OASIS.API.Providers.TONOASIS; // Not referenced in Core Only solution
//using NextGenSoftware.OASIS.API.Providers.ZkSyncOASIS;
//using NextGenSoftware.OASIS.API.Providers.LineaOASIS;
//using NextGenSoftware.OASIS.API.Providers.ScrollOASIS;
//using NextGenSoftware.OASIS.API.Providers.XRPLOASIS;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.Logging;
using NextGenSoftware.Logging.NLogger;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;
using NextGenSoftware.OASIS.API.Providers.AptosOASIS;
using NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;
using NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;
using NextGenSoftware.OASIS.API.Providers.BaseOASIS;
using NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;
using NextGenSoftware.OASIS.API.Providers.BNBChainOASIS;
using NextGenSoftware.OASIS.API.Providers.CardanoOASIS;
using NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
using NextGenSoftware.OASIS.API.Providers.FantomOASIS;
using NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura;
using NextGenSoftware.OASIS.API.Providers.OptimismOASIS;
using NextGenSoftware.OASIS.API.Providers.ArweaveOASIS;
using NextGenSoftware.OASIS.API.Providers.PinataOASIS;
using NextGenSoftware.OASIS.API.Providers.PolygonOASIS;
using NextGenSoftware.OASIS.API.Providers.RootstockOASIS;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SuiOASIS;
using NextGenSoftware.OASIS.API.Providers.TelosOASIS;
using NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;
using NextGenSoftware.OASIS.API.Providers.NEAROASIS;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS; // TODO: Fix TRONOASIS build errors
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Providers.ElrondOASIS;
//using NextGenSoftware.OASIS.API.Providers.PolkaDotOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader
{
    public static partial class OASISBootLoader
    {
        private static OASISResult<IOASISStorageProvider> RegisterProviderInternal(ProviderType providerType, string overrideConnectionString = null, bool forceRegister = false)
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();

            try
            {
                // If they wish to forceRegister then if it is already registered then unregister it first (when connectionstring changes for example).
                if (forceRegister && ProviderManager.Instance.IsProviderRegistered(providerType))
                    ProviderManager.Instance.UnRegisterProvider(providerType);

                if (!ProviderManager.Instance.IsProviderRegistered(providerType))
                {
                    switch (providerType)
                    {
                        case ProviderType.HoloOASIS:
                            {
                                HoloOASIS holoOASIS = new HoloOASIS(
                                            overrideConnectionString == null
                                                ? OASISDNA.OASIS.StorageProviders.HoloOASIS.LocalNodeURI
                                                : overrideConnectionString,
                                            OASISDNA, // Inject OASISDNA into constructor
                                            OASISDNA.OASIS.StorageProviders.HoloOASIS.HoloNetworkURI, 
                                            OASISDNA.OASIS.StorageProviders.HoloOASIS.UseLocalNode, 
                                            OASISDNA.OASIS.StorageProviders.HoloOASIS.UseHoloNetwork, 
                                            OASISDNA.OASIS.StorageProviders.HoloOASIS.HoloNETORMUseReflection);

                                holoOASIS.OnStorageProviderError += HoloOASIS_StorageProviderError;
                                result.Result = holoOASIS;
                            }
                            break;

                        case ProviderType.SQLLiteDBOASIS:
                            {
                                //TODO: need to fix or re-write SQLLiteDBOASIS Provider ASAP!

                                SQLLiteDBOASIS SQLLiteDBOASIS = new SQLLiteDBOASIS(overrideConnectionString == null
                                    ? OASISDNA.OASIS.StorageProviders.SQLLiteDBOASIS.ConnectionString
                                    : overrideConnectionString);
                                SQLLiteDBOASIS.OnStorageProviderError += SQLLiteDBOASIS_StorageProviderError;
                                result.Result = SQLLiteDBOASIS;
                            }
                            break;

                        case ProviderType.MongoDBOASIS:
                            {
                                MongoDBOASIS mongoOASIS =
                                    new MongoDBOASIS(
                                        overrideConnectionString == null
                                            ? OASISDNA.OASIS.StorageProviders.MongoDBOASIS.ConnectionString
                                            : overrideConnectionString, OASISDNA.OASIS.StorageProviders.MongoDBOASIS.DBName, OASISDNA);
                                mongoOASIS.OnStorageProviderError += MongoOASIS_StorageProviderError;
                                result.Result = mongoOASIS;
                            }
                            break;

                        case ProviderType.SolanaOASIS:
                            {
                                SolanaOASIS solanaOasis = new(
                                    OASISDNA.OASIS.StorageProviders.SolanaOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.SolanaOASIS.PrivateKey,
                                    OASISDNA.OASIS.StorageProviders.SolanaOASIS.PublicKey);
                                solanaOasis.OnStorageProviderError += SolanaOASIS_StorageProviderError;
                                result.Result = solanaOasis;
                            }
                            break;

                        case ProviderType.EOSIOOASIS:
                            {
                                var eosioProvider = new EOSIOOASIS(
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountName,
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ChainId,
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountPrivateKey);
                                eosioProvider.OnStorageProviderError += EOSIOOASIS_StorageProviderError;
                                result.Result = eosioProvider;
                            }
                            break;

                        case ProviderType.TelosOASIS:
                            {
                                var telosProvider = new TelosOASIS(
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountName,
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ChainId,
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountPrivateKey);
                                telosProvider.OnStorageProviderError += TelosOASIS_StorageProviderError;
                                result.Result = telosProvider;
                            }
                            break;

                        case ProviderType.SEEDSOASIS:
                            {
                                var seedsProvider = new SEEDSOASIS(new TelosOASIS(
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountName,
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ChainId,
                                    OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountPrivateKey));
                                seedsProvider.OnStorageProviderError += SEEDSOASIS_StorageProviderError;
                                result.Result = seedsProvider;
                            }
                            break;

                        case ProviderType.Neo4jOASIS:
                            {
                                Neo4jOASIS Neo4jOASIS = new Neo4jOASIS(
                                    overrideConnectionString == null
                                        ? OASISDNA.OASIS.StorageProviders.Neo4jOASIS.ConnectionString
                                        : overrideConnectionString, OASISDNA.OASIS.StorageProviders.Neo4jOASIS.Username,
                                    OASISDNA.OASIS.StorageProviders.Neo4jOASIS.Password);
                                Neo4jOASIS.OnStorageProviderError += Neo4jOASIS_StorageProviderError;
                                result.Result = Neo4jOASIS;
                            }
                            break;

                        case ProviderType.IPFSOASIS:
                            {
                                IPFSOASIS IPFSOASIS = null;

                                //Example of how to pass in OASISDNA if the Provider needs to update the DNA.
                                if (overrideConnectionString != null)
                                {
                                    OASISDNA overrideDNA = OASISDNA;
                                    overrideDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString = overrideConnectionString;
                                    IPFSOASIS = new IPFSOASIS(overrideDNA, OASISDNAPath);
                                }
                                else
                                    IPFSOASIS = new IPFSOASIS(OASISDNA, OASISDNAPath);

                                IPFSOASIS.OnStorageProviderError += IPFSOASIS_StorageProviderError;
                                result.Result = IPFSOASIS;
                            }
                            break;

                        case ProviderType.PinataOASIS:
                            {
                                PinataOASIS PinataOASIS = null;

                                //Example of how to pass in OASISDNA if the Provider needs to update the DNA.
                                if (overrideConnectionString != null)
                                {
                                    OASISDNA overrideDNA = OASISDNA;
                                    overrideDNA.OASIS.StorageProviders.PinataOASIS.ConnectionString = overrideConnectionString;
                                    PinataOASIS = new PinataOASIS(overrideDNA, OASISDNAPath);
                                }
                                else
                                    PinataOASIS = new PinataOASIS(OASISDNA, OASISDNAPath);

                                PinataOASIS.OnStorageProviderError += PinataOASIS_StorageProviderError;
                                result.Result = PinataOASIS;
                            }
                            break;

                        case ProviderType.ArweaveOASIS:
                            {
                                ArweaveOASIS ArweaveOASIS = null;

                                if (overrideConnectionString != null)
                                {
                                    OASISDNA overrideDNA = OASISDNA;
                                    overrideDNA.OASIS.StorageProviders.ArweaveOASIS.ConnectionString = overrideConnectionString;
                                    ArweaveOASIS = new ArweaveOASIS(overrideDNA, OASISDNAPath);
                                }
                                else
                                    ArweaveOASIS = new ArweaveOASIS(OASISDNA, OASISDNAPath);

                                ArweaveOASIS.OnStorageProviderError += ArweaveOASIS_StorageProviderError;
                                result.Result = ArweaveOASIS;
                            }
                            break;

                        case ProviderType.EthereumOASIS:
                            {
                                var ethereumProvider = new EthereumOASIS(
                                    OASISDNA.OASIS.StorageProviders.EthereumOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.EthereumOASIS.ChainPrivateKey,
                                    OASISDNA.OASIS.StorageProviders.EthereumOASIS.ChainId,
                                    OASISDNA.OASIS.StorageProviders.EthereumOASIS.ContractAddress);
                                ethereumProvider.OnStorageProviderError += EthereumOASIS_StorageProviderError;
                                result.Result = ethereumProvider;
                            }
                            break;

                        case ProviderType.ArbitrumOASIS:
                            {
                                ArbitrumOASIS ArbitrumOASIS = new(
                                    OASISDNA.OASIS.StorageProviders.ArbitrumOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.ArbitrumOASIS.ChainPrivateKey,
                                    OASISDNA.OASIS.StorageProviders.ArbitrumOASIS.ChainId,
                                    OASISDNA.OASIS.StorageProviders.ArbitrumOASIS.ContractAddress);
                                ArbitrumOASIS.OnStorageProviderError += ArbitrumOASIS_StorageProviderError;
                                result.Result = ArbitrumOASIS;
                            }
                            break;
                        case ProviderType.RootstockOASIS:
                            {
                                RootstockOASIS RootstockOASIS = new(
                                    OASISDNA.OASIS.StorageProviders.RootstockOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.RootstockOASIS.ChainPrivateKey,
                                    OASISDNA.OASIS.StorageProviders.RootstockOASIS.ContractAddress);
                                RootstockOASIS.OnStorageProviderError += RootstockOASIS_StorageProviderError;
                                result.Result = RootstockOASIS;
                            }
                            break;
                        case ProviderType.PolygonOASIS:
                            {
                                PolygonOASIS PolygonOASIS = new(
                                    OASISDNA.OASIS.StorageProviders.PolygonOASIS.ConnectionString,
                                    OASISDNA.OASIS.StorageProviders.PolygonOASIS.ChainPrivateKey,
                                    OASISDNA.OASIS.StorageProviders.PolygonOASIS.ContractAddress);
                                PolygonOASIS.OnStorageProviderError += PolygonOASIS_StorageProviderError;
                                result.Result = PolygonOASIS;
                            }
                            break;

                        case ProviderType.ThreeFoldOASIS:
                            {
                                var threeFoldProvider = new ThreeFoldOASIS(overrideConnectionString == null
                                    ? OASISDNA.OASIS.StorageProviders.ThreeFoldOASIS.ConnectionString
                                    : overrideConnectionString);
                                threeFoldProvider.OnStorageProviderError += ThreeFoldOASIS_StorageProviderError;
                                result.Result = threeFoldProvider;
                            }
                            break;

                        case ProviderType.LocalFileOASIS:
                            {
                                LocalFileOASIS localFileOASIS = new LocalFileOASIS(OASISDNA.OASIS.StorageProviders.LocalFileOASIS.FilePath);
                                localFileOASIS.OnStorageProviderError += LocalFileOASIS_StorageProviderError;
                                result.Result = localFileOASIS;
                            }
                            break;

                        case ProviderType.AzureCosmosDBOASIS:
                            {
                                AzureCosmosDBOASIS azureCosmosDBOASIS = new AzureCosmosDBOASIS(
                                    new Uri(OASISDNA.OASIS.StorageProviders.AzureCosmosDBOASIS.ServiceEndpoint),
                                    OASISDNA.OASIS.StorageProviders.AzureCosmosDBOASIS.AuthKey,
                                    OASISDNA.OASIS.StorageProviders.AzureCosmosDBOASIS.DBName,
                                    ListHelper.ConvertToList(OASISDNA.OASIS.StorageProviders.AzureCosmosDBOASIS.CollectionNames));

                                azureCosmosDBOASIS.OnStorageProviderError += AzureCosmosDBOASIS_StorageProviderError;
                                result.Result = azureCosmosDBOASIS;
                            }
                            break;

                        case ProviderType.BitcoinOASIS:
                        {
                            var bitcoinProvider = new BitcoinOASIS(
                                OASISDNA.OASIS.StorageProviders.BitcoinOASIS.RpcEndpoint ?? "https://blockstream.info/api",
                                OASISDNA.OASIS.StorageProviders.BitcoinOASIS.Network ?? "mainnet");
                            bitcoinProvider.OnStorageProviderError += BitcoinOASIS_StorageProviderError;
                            result.Result = bitcoinProvider;
                        }
                        break;

                        //case ProviderType.CardanoOASIS:
                        //    {
                        //        CardanoOASIS cardanoOASIS = new CardanoOASIS(
                        //            OASISDNA.OASIS.StorageProviders.CardanoOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.CardanoOASIS.NetworkId);
                        //        result.Result = cardanoOASIS;
                        //    }
                        //    break;

                        //case ProviderType.PolkadotOASIS:
                        //    {
                        //        PolkadotOASIS polkadotOASIS = new PolkadotOASIS(
                        //            OASISDNA.OASIS.StorageProviders.PolkadotOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.PolkadotOASIS.Network);
                        //        result.Result = polkadotOASIS;
                        //    }
                        //    break;

                        //case ProviderType.BNBChainOASIS:
                        //    {
                        //        BNBChainOASIS bnbChainOASIS = new BNBChainOASIS(
                        //            OASISDNA.OASIS.StorageProviders.BNBChainOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.BNBChainOASIS.NetworkId,
                        //            OASISDNA.OASIS.StorageProviders.BNBChainOASIS.ChainId);
                        //        result.Result = bnbChainOASIS;
                        //    }
                        //    break;

                        //case ProviderType.FantomOASIS:
                        //    {
                        //        FantomOASIS fantomOASIS = new FantomOASIS(
                        //            OASISDNA.OASIS.StorageProviders.FantomOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.FantomOASIS.NetworkId,
                        //            OASISDNA.OASIS.StorageProviders.FantomOASIS.ChainId);
                        //        result.Result = fantomOASIS;
                        //    }
                        //    break;

                        //case ProviderType.OptimismOASIS:
                        //    {
                        //        OptimismOASIS optimismOASIS = new OptimismOASIS(
                        //            OASISDNA.OASIS.StorageProviders.OptimismOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.OptimismOASIS.NetworkId,
                        //            OASISDNA.OASIS.StorageProviders.OptimismOASIS.ChainId);
                        //        result.Result = optimismOASIS;
                        //    }
                        //    break;

                        //case ProviderType.ChainLinkOASIS:
                        //    {
                        //        ChainLinkOASIS chainLinkOASIS = new ChainLinkOASIS(
                        //            OASISDNA.OASIS.StorageProviders.ChainLinkOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.ChainLinkOASIS.NetworkId,
                        //            OASISDNA.OASIS.StorageProviders.ChainLinkOASIS.ChainId);
                        //        result.Result = chainLinkOASIS;
                        //    }
                        //    break;

                        //case ProviderType.ElrondOASIS:
                        //    {
                        //        ElrondOASIS elrondOASIS = new ElrondOASIS(
                        //            OASISDNA.OASIS.StorageProviders.ElrondOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.ElrondOASIS.Network,
                        //            OASISDNA.OASIS.StorageProviders.ElrondOASIS.ChainId);
                        //        result.Result = elrondOASIS;
                        //    }
                        //    break;

                        case ProviderType.AptosOASIS:
                        {
                            var aptosProvider = new AptosOASIS(
                                OASISDNA.OASIS.StorageProviders.AptosOASIS.RpcEndpoint ?? "https://api.mainnet.aptoslabs.com/v1",
                                OASISDNA.OASIS.StorageProviders.AptosOASIS.Network ?? "mainnet",
                                OASISDNA.OASIS.StorageProviders.AptosOASIS.PrivateKey,
                                OASISDNA.OASIS.StorageProviders.AptosOASIS.ContractAddress ?? "0x1");
                            aptosProvider.OnStorageProviderError += AptosOASIS_StorageProviderError;
                            result.Result = aptosProvider;
                        }
                        break;

                        case ProviderType.TRONOASIS:
                        {
                            //TODO: Fix TRONOASIS build errors
                             var tronProvider = new TRONOASIS(
                                 OASISDNA.OASIS.StorageProviders.TRONOASIS.RpcEndpoint ?? "https://api.trongrid.io",
                                 OASISDNA.OASIS.StorageProviders.TRONOASIS.Network ?? "mainnet",
                                 OASISDNA.OASIS.StorageProviders.TRONOASIS.ChainId ?? "728126428");
                                tronProvider.OnStorageProviderError += TRONOASIS_StorageProviderError;
                                result.Result = tronProvider;
                                break;
                        }

                        case ProviderType.HashgraphOASIS:
                        {
                            var hashgraphProvider = new HashgraphOASIS(
                                OASISDNA.OASIS.StorageProviders.HashgraphOASIS.RpcEndpoint ?? "https://mainnet-public.mirrornode.hedera.com",
                                OASISDNA.OASIS.StorageProviders.HashgraphOASIS.Network ?? "mainnet",
                                OASISDNA.OASIS.StorageProviders.HashgraphOASIS.ChainId ?? "295");
                            hashgraphProvider.OnStorageProviderError += HashgraphOASIS_StorageProviderError;
                            result.Result = hashgraphProvider;
                        }
                        break;

                        case ProviderType.AvalancheOASIS:
                        {
                            var avalancheProvider = new AvalancheOASIS(
                                OASISDNA.OASIS.StorageProviders.AvalancheOASIS.RpcEndpoint ?? "https://api.avax.network/ext/bc/C/rpc",
                                OASISDNA.OASIS.StorageProviders.AvalancheOASIS.ChainPrivateKey ?? "",
                                OASISDNA.OASIS.StorageProviders.AvalancheOASIS.ContractAddress ?? "");
                            avalancheProvider.OnStorageProviderError += AvalancheOASIS_StorageProviderError;
                            result.Result = avalancheProvider;
                        }
                        break;

                        case ProviderType.CosmosBlockChainOASIS:
                        {
                            var cosmosProvider = new CosmosBlockChainOASIS(
                                OASISDNA.OASIS.StorageProviders.CosmosBlockChainOASIS.RpcEndpoint ?? "https://cosmos-rpc.polkachu.com",
                                OASISDNA.OASIS.StorageProviders.CosmosBlockChainOASIS.Network ?? "cosmos",
                                OASISDNA.OASIS.StorageProviders.CosmosBlockChainOASIS.ChainId ?? "cosmoshub-4");
                            cosmosProvider.OnStorageProviderError += CosmosBlockChainOASIS_StorageProviderError;
                            result.Result = cosmosProvider;
                        }
                        break;

                        case ProviderType.NEAROASIS:
                        {
                            var nearProvider = new NEAROASIS(
                                OASISDNA.OASIS.StorageProviders.NEAROASIS.RpcEndpoint ?? "https://rpc.mainnet.near.org",
                                OASISDNA.OASIS.StorageProviders.NEAROASIS.Network ?? "mainnet",
                                OASISDNA.OASIS.StorageProviders.NEAROASIS.ChainId ?? "mainnet");
                            nearProvider.OnStorageProviderError += NEAROASIS_StorageProviderError;
                            result.Result = nearProvider;
                            break;
                        }

                        case ProviderType.BaseOASIS:
                        {
                            var chainIdHex = OASISDNA.OASIS.StorageProviders.BaseOASIS.ChainId ?? "0x2105";
                            var chainId = chainIdHex.StartsWith("0x") 
                                ? BigInteger.Parse(chainIdHex.Substring(2), System.Globalization.NumberStyles.HexNumber)
                                : BigInteger.Parse(chainIdHex);
                            var baseProvider = new BaseOASIS(
                                OASISDNA.OASIS.StorageProviders.BaseOASIS.RpcEndpoint ?? "https://mainnet.base.org",
                                OASISDNA.OASIS.StorageProviders.BaseOASIS.ChainPrivateKey ?? "",
                                chainId,
                                OASISDNA.OASIS.StorageProviders.BaseOASIS.ContractAddress ?? "");
                            baseProvider.OnStorageProviderError += BaseOASIS_StorageProviderError;
                            result.Result = baseProvider;
                        }
                        break;

                        case ProviderType.SuiOASIS:
                        {
                            var suiProvider = new SuiOASIS(
                                OASISDNA.OASIS.StorageProviders.SuiOASIS.RpcEndpoint ?? "https://fullnode.mainnet.sui.io:443",
                                OASISDNA.OASIS.StorageProviders.SuiOASIS.Network ?? "mainnet",
                                OASISDNA.OASIS.StorageProviders.SuiOASIS.ChainId ?? "",
                                OASISDNA.OASIS.StorageProviders.SuiOASIS.ContractAddress ?? "");
                            suiProvider.OnStorageProviderError += SuiOASIS_StorageProviderError;
                            result.Result = suiProvider;
                        }
                        break;

                        case ProviderType.MoralisOASIS:
                        {
                            // TODO: Fix MoralisOASIS build errors
                            // var moralisProvider = new MoralisOASIS(
                            //     OASISDNA.OASIS.StorageProviders.MoralisOASIS.ApiKey ?? "",
                            //     OASISDNA.OASIS.StorageProviders.MoralisOASIS.RpcEndpoint ?? "https://speedy-nodes-nyc.moralis.io",
                            //     OASISDNA.OASIS.StorageProviders.MoralisOASIS.Network ?? "mainnet");
                            // moralisProvider.OnStorageProviderError += MoralisOASIS_StorageProviderError;
                            // result.Result = moralisProvider;
                            break;
                        }

                        //case ProviderType.TelosOASIS:
                        //    {
                        //        TelosOASIS telosOASIS = new TelosOASIS(
                        //            OASISDNA.OASIS.StorageProviders.TelosOASIS.RpcEndpoint,
                        //            OASISDNA.OASIS.StorageProviders.TelosOASIS.Network,
                        //            OASISDNA.OASIS.StorageProviders.TelosOASIS.ChainId,
                        //            "");
                        //        result.Result = telosOASIS;
                        //    }
                        //    break;

                        case ProviderType.ActivityPubOASIS:
                            {
                                var baseUrl = OASISDNA.OASIS.StorageProviders.ActivityPubOASIS?.BaseUrl ?? "https://mastodon.social";
                                // Remove /api/v1 if present since ActivityPubOASIS constructor expects instance URL
                                if (baseUrl.EndsWith("/api/v1"))
                                    baseUrl = baseUrl.Replace("/api/v1", "");
                                var activityPubProvider = new ActivityPubOASIS(baseUrl, "");
                                activityPubProvider.OnStorageProviderError += ActivityPubOASIS_StorageProviderError;
                                result.Result = activityPubProvider;
                            }
                            break;

                        case ProviderType.GoogleCloudOASIS:
                            {
                                GoogleCloudOASIS googleCloudOASIS = new GoogleCloudOASIS(
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.ProjectId,
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.BucketName,
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.CredentialsPath,
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.FirestoreDatabaseId,
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.BigQueryDatasetId,
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.EnableStorage,
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.EnableFirestore,
                                    OASISDNA.OASIS.StorageProviders.GoogleCloudOASIS.EnableBigQuery);
                                result.Result = googleCloudOASIS;
                            }
                            break;
                    }

                    if (result.Result != null)
                        ProviderManager.Instance.RegisterProvider(result.Result);
                }
                else
                    result.Result = (IOASISStorageProvider)ProviderManager.Instance.GetProvider(providerType);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown Error Occured In OASISBootLoader In Method RegisterProviderInternal. Reason: {e}");
            }

            return result;
        }
    }
}

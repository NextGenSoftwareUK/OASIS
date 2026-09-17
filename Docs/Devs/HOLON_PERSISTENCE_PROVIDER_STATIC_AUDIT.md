| Status | SaveHolonAsync implementation | Static result |
|---|---|---|
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.AlgoliaOASIS/AlgoliaOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.ArangoDBOASIS/ArangoDBOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.ArcadeDBOASIS/ArcadeDBOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| append-only | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.ArweaveOASIS/ArweaveOASIS.SaveLoad.cs` | writes an immutable remote record; requires provider-specific versioning contract |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.BigQueryOASIS/BigQueryOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.CassandraOASIS/CassandraOASIS.cs` | branches on a lifecycle/provider-key field |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.ChromaOASIS/ChromaOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| review | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS/ClickHouseOASIS.cs` | Id is present but write primitive needs manual integration review |
| review | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.CloudinaryOASIS/CloudinaryOASIS.cs` | Id is present but write primitive needs manual integration review |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.CockroachDBOASIS/CockroachDBOASIS.cs` | branches on a lifecycle/provider-key field |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS/CouchbaseOASIS.cs` | branches on a lifecycle/provider-key field |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.CouchDBOASIS/CouchDBOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.DatabricksOASIS/DatabricksOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| review | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.DenoDeployOASIS/DenoDeployOASIS.cs` | Id is present but write primitive needs manual integration review |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.DragonflyOASIS/DragonflyOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.DruidOASIS/DruidOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.DuckDBOASIS/DuckDBOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS/ElasticsearchOASIS.cs` | branches on a lifecycle/provider-key field |
| review | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.FastlyOASIS/FastlyOASIS.cs` | Id is present but write primitive needs manual integration review |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.GarnetOASIS/GarnetOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS/InfluxDBOASIS.cs` | branches on a lifecycle/provider-key field |
| review | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.KeyDBOASIS/KeyDBOASIS.cs` | Id is present but write primitive needs manual integration review |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.LanceDBOASIS/LanceDBOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.LitestreamOASIS/LitestreamOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS/LocalFileOASIS.LoadHolons.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MarqoOASIS/MarqoOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS/MeilisearchOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| review | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MemcachedOASIS/MemcachedOASIS.cs` | Id is present but write primitive needs manual integration review |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MilvusOASIS/MilvusOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| review | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MinIOOASIS/MinIOOASIS.cs` | Id is present but write primitive needs manual integration review |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/MongoDBOASIS.Part2.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.MotherDuckOASIS/MotherDuckOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS/Neo4jOASIS.LoadHolons.cs` | source uses Id as, or passes Id into, the storage write |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura/Neo4jOASIS.NFTToken.cs` | does not reference public IHolon.Id |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS/OpenSearchOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS/OracleDBOASIS.cs` | branches on a lifecycle/provider-key field |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.PgVectorOASIS/PgVectorOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.PineconeOASIS/PineconeOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.PinotOASIS/PinotOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.PlanetScaleOASIS/PlanetScaleOASIS.cs` | branches on a lifecycle/provider-key field |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS/PostgreSQLOASIS.cs` | branches on a lifecycle/provider-key field |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.PouchDBOASIS/PouchDBOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.QdrantOASIS/QdrantOASIS.cs` | branches on a lifecycle/provider-key field |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.QuestDBOASIS/QuestDBOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.RavenDBOASIS/RavenDBOASIS.cs` | branches on a lifecycle/provider-key field |
| review | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.RedisOASIS/RedisOASIS.cs` | Id is present but write primitive needs manual integration review |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.RedshiftOASIS/RedshiftOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| review | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.ScyllaDBOASIS/ScyllaDBOASIS.cs` | Id is present but write primitive needs manual integration review |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.SnowflakeOASIS/SnowflakeOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.SolrOASIS/SolrOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/Persistence/Repositories/HolonRepository.cs` | does not reference public IHolon.Id |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS/SQLServerDBOASIS.cs` | branches on a lifecycle/provider-key field |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS/SurrealDBOASIS.cs` | branches on a lifecycle/provider-key field |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.TimescaleDBOASIS/TimescaleDBOASIS.cs` | branches on a lifecycle/provider-key field |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.TypesenseOASIS/TypesenseOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.ValKeyOASIS/ValKeyOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| divergent | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.WeaviateOASIS/WeaviateOASIS.cs` | branches on a lifecycle/provider-key field |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.XataOASIS/XataOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| public-id write | `Providers/Storage/NextGenSoftware.OASIS.API.Providers.ZillizOASIS/ZillizOASIS.cs` | source uses Id as, or passes Id into, the storage write |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AlgorandOASIS/AlgorandOASIS.cs` | does not reference public IHolon.Id |
| review | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AptosOASIS/AptosOASIS.SaveHolonTrans.cs` | Id is present but write primitive needs manual integration review |
| append-only | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS/ArbitrumOASIS.HolonSave.Part1.cs` | writes an immutable remote record; requires provider-specific versioning contract |
| append-only | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AvalancheOASIS/AvalancheOASIS_Legacy.SaveSearch.cs` | writes an immutable remote record; requires provider-specific versioning contract |
| review | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AxelarOASIS/AxelarOASIS.cs` | Id is present but write primitive needs manual integration review |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AztecOASIS/AztecOASIS.SaveHolons.cs` | does not reference public IHolon.Id |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BasechainOASIS/BasechainOASIS.cs` | does not reference public IHolon.Id |
| append-only | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BaseOASIS/BaseOASIS.SaveDeleteSearch.cs` | writes an immutable remote record; requires provider-specific versioning contract |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BitcoinOASIS/BitcoinOASIS.AvatarDetail.cs` | does not reference public IHolon.Id |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BlockStackOASIS/BlockStackOASIS.SaveHolonExport.cs` | branches on a lifecycle/provider-key field |
| append-only | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.BNBChainOASIS/BNBChainOASIS_Legacy.SaveHolon.cs` | writes an immutable remote record; requires provider-specific versioning contract |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CardanoOASIS/CardanoOASIS.SaveHolonDelete.cs` | branches on a lifecycle/provider-key field |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CeramicOASIS/CeramicOASIS.cs` | does not reference public IHolon.Id |
| review | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS/ChainLinkOASIS.HolonOps.cs` | Id is present but write primitive needs manual integration review |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/CosmosBlockChainOASIS.ActivateDeactivateLoad.cs` | does not reference public IHolon.Id |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ElrondOASIS/ElrondOASIS.AvatarDetail.cs` | does not reference public IHolon.Id |
| public-id write | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EOSIOOASIS/EOSIOOASIS.LoadHolons.cs` | source uses Id as, or passes Id into, the storage write |
| review | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EthereumOASIS/EthereumOASIS.SaveAvatarLoad.cs` | Id is present but write primitive needs manual integration review |
| append-only | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.FantomOASIS/FantomOASIS_Legacy.SaveDeleteHolon.cs` | writes an immutable remote record; requires provider-specific versioning contract |
| review | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.FhenixOASIS/FhenixOASIS.cs` | Id is present but write primitive needs manual integration review |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.FilecoinOASIS/FilecoinOASIS.cs` | does not reference public IHolon.Id |
| review | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/HashgraphOASIS.LoadHolons.cs` | Id is present but write primitive needs manual integration review |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.MidenOASIS/MidenOASIS.SaveDelete.cs` | branches on a lifecycle/provider-key field |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.MoralisOASIS/MoralisOASIS.AvatarDetail.cs` | branches on a lifecycle/provider-key field |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/NEAROASIS.AvatarDetailHolon.cs` | does not reference public IHolon.Id |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/NEAROASIS.AvatarDetailHolon.cs` | does not reference public IHolon.Id |
| append-only | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.OptimismOASIS/OptimismOASIS_Legacy.SearchExport.cs` | writes an immutable remote record; requires provider-specific versioning contract |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/PolkadotOASIS.HolonLoad.cs` | branches on a lifecycle/provider-key field |
| review | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.RadixOASIS/RadixOASIS.NFTToken.cs` | Id is present but write primitive needs manual integration review |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/SolanaOasis.HolonLoad.cs` | does not reference public IHolon.Id |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.StarknetOASIS/StarknetOASIS.SaveHolonToken.cs` | branches on a lifecycle/provider-key field |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.StellarOASIS/StellarOASIS.cs` | does not reference public IHolon.Id |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SuiOASIS.SendTrans.cs` | branches on a lifecycle/provider-key field |
| public-id write | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TelosOASIS/TelosOASIS.HolonSave.cs` | source uses Id as, or passes Id into, the storage write |
| review | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TRONOASIS/TRONOASIS.LoadHolon.cs` | Id is present but write primitive needs manual integration review |
| review | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/src/Web3CoreOASISBaseProvider.SaveSearch.cs` | Id is present but write primitive needs manual integration review |
| review | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.WormholeOASIS/WormholeOASIS.cs` | Id is present but write primitive needs manual integration review |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.XRPLOASIS/XRPLOASIS.cs` | does not reference public IHolon.Id |
| divergent | `Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ZcashOASIS/ZcashOASIS.LoadHolons.cs` | does not reference public IHolon.Id |

append-only: 7, divergent: 37, public-id write: 35, review: 19

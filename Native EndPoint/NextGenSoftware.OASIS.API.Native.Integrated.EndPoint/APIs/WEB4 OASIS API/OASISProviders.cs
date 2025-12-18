using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Providers.AcitvityPubOASIS;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
//using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
//using NextGenSoftware.OASIS.API.Providers.TelosOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;
using NextGenSoftware.OASIS.API.Providers.RootstockOASIS;
using NextGenSoftware.OASIS.API.Providers.PolygonOASIS;

namespace NextGenSoftware.OASIS.API.Native.EndPoint
{
    public class OASISProviders
    {
        //EthereumOASIS _ethereum;
        ArbitrumOASIS _arbitrum;
        RootstockOASIS _rootstock;
        PolygonOASIS _polygon;
        SolanaOASIS _solana;
        //EOSIOOASIS _EOSIO;
        //TelosOASIS _telos;
        //SEEDSOASIS _SEEDS;
        IPFSOASIS _IPFS;
        HoloOASIS _holochain;
        MongoDBOASIS _mongoDB;
        Neo4jOASIS _neo4j;
        SQLLiteDBOASIS _sqlLiteDb;
        ThreeFoldOASIS _threeFold;
        //AcitvityPubOASIS _activityPub;
        OASISDNA _OASISDNA;

        //public SEEDSOASIS SEEDS
        //{
        //    get
        //    {
        //        if (_SEEDS == null)
        //        {
        //            if (ProviderManager.Instance.IsProviderRegistered(ProviderType.SEEDSOASIS))
        //                _SEEDS = (SEEDSOASIS)ProviderManager.Instance.GetStorageProvider(ProviderType.SEEDSOASIS);
        //            else
        //            {
        //                // We could re-use the TelosOASIS Provider but it could have a different connection string to SEEDSOASIS so they need to be seperate.
        //                // TODO: The only other way is to share it and have to keep disconnecting and re-connecting with the different connections (SEEDS or EOSIO may even work with any EOSIO node end point? NEED TO TEST... if so then we can use the commented out line below).
        //                //_SEEDS = new SEEDSOASIS(Telos); 
        //                _SEEDS = new SEEDSOASIS(new TelosOASIS(
        //                    _OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ConnectionString,
        //                    _OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountName,
        //                    _OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ChainId,
        //                    _OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountPrivateKey
        //                    ));
        //                ProviderManager.Instance.RegisterProvider(_SEEDS);
        //            }
        //        }

        //        return _SEEDS;
        //    }
        //}

        public IPFSOASIS IPFS
        {
            get
            {
                if (_IPFS == null)
                {
                    Task.Run(async () =>
                    {
                        OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.IPFSOASIS);

                        if (result != null && !result.IsError)
                            _IPFS = (IPFSOASIS)result.Result;
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISAPIProviders In IPFS Property Getter. Reason: {result.Message}");
                    });
                }

                return _IPFS;
            }
        }

        //public EOSIOOASIS EOSIO
        //{
        //    get
        //    {
        //        if (_EOSIO == null)
        //        {
        //            Task.Run(async () =>
        //            {
        //                OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.EOSIOOASIS);

        //                if (result != null && !result.IsError)
        //                    _EOSIO = (EOSIOOASIS)result.Result;
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISAPIProviders In EOSIO Property Getter. Reason: {result.Message}");
        //            });
        //        }

        //        return _EOSIO;
        //    }
        //}

        public SolanaOASIS Solana
        {
            get
            {
                if (_solana == null)
                {
                    Task.Run(async () =>
                    {
                        OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.SolanaOASIS);

                        if (result != null && !result.IsError)
                            _solana = (SolanaOASIS)result.Result;
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISAPIProviders In Solana Property Getter. Reason: {result.Message}");
                    });
                }

                return _solana;
            }
        }

        //public EthereumOASIS Ethereum
        //{
        //    get
        //    {
        //        if (_ethereum == null)
        //        {
        //            Task.Run(async () =>
        //            {
        //                OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.EthereumOASIS);

        //                if (result != null && !result.IsError)
        //                    _ethereum = (EthereumOASIS)result.Result;
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISAPIProviders In Ethereum Property Getter. Reason: {result.Message}");
        //            });
        //        }

        //        return _ethereum;
        //    }
        //}

        public ArbitrumOASIS Arbitrum
        {
            get
            {
                if (_arbitrum == null)
                {
                    Task.Run(async () =>
                    {
                        OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.ArbitrumOASIS);

                        if (result != null && !result.IsError)
                            _arbitrum = (ArbitrumOASIS)result.Result;
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISAPIProviders In Arbitrum Property Getter. Reason: {result.Message}");
                    });
                }

                return _arbitrum;
            }
        }

        public PolygonOASIS Polygon
        {
            get
            {
                if (_polygon == null)
                {
                    Task.Run(async () =>
                    {
                        OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.PolygonOASIS);

                        if (result != null && !result.IsError)
                            _polygon = (PolygonOASIS)result.Result;
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISAPIProviders In Polygon Property Getter. Reason: {result.Message}");
                    });
                }

                return _polygon;
            }
        }

        public RootstockOASIS Rootstock
        {
            get
            {
                if (_rootstock == null)
                {
                    Task.Run(async () =>
                    {
                        OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.RootstockOASIS);

                        if (result != null && !result.IsError)
                            _rootstock = (RootstockOASIS)result.Result;
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISAPIProviders In Rootstock Property Getter. Reason: {result.Message}");
                    });
                }

                return _rootstock;
            }
        }

        //public TelosOASIS Telos
        //{
        //    get
        //    {
        //        if (_telos == null)
        //        {
        //            Task.Run(async () =>
        //            {
        //                OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.TelosOASIS);

        //                if (result != null && !result.IsError)
        //                    _telos = (TelosOASIS)result.Result;
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISAPIProviders In Telos Property Getter. Reason: {result.Message}");
        //            });
        //        }

        //        return _telos;
        //    }
        //}

        public HoloOASIS Holochain
        {
            get
            {
                if (_holochain == null)
                {
                    Task.Run(async () =>
                    {
                        OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.HoloOASIS);

                        if (result != null && !result.IsError)
                            _holochain = (HoloOASIS)result.Result;
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISAPIProviders In Holochain Property Getter. Reason: {result.Message}");
                    });
                }

                return _holochain;
            }
        }

        public MongoDBOASIS MongoDB
        {
            get
            {
                if (_mongoDB == null)
                {
                    Task.Run(async () =>
                    {
                        OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.MongoDBOASIS);

                        if (result != null && !result.IsError)
                            _mongoDB = (MongoDBOASIS)result.Result;
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISAPIProviders In MongoDB Property Getter. Reason: {result.Message}");
                    });
                }

                return _mongoDB;
            }
        }

        public Neo4jOASIS Neo4j
        {
            get
            {
                if (_neo4j == null)
                {
                    Task.Run(async () =>
                    {
                        OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.Neo4jOASIS);

                        if (result != null && !result.IsError)
                            _neo4j = (Neo4jOASIS)result.Result;
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISAPIProviders In Neo4j Property Getter. Reason: {result.Message}");
                    });
                }

                return _neo4j;
            }
        }

        public SQLLiteDBOASIS SQLLiteDB
        {
            get
            {
                if (_sqlLiteDb == null)
                {
                    Task.Run(async () =>
                    {
                        OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.SQLLiteDBOASIS);

                        if (result != null && !result.IsError)
                            _sqlLiteDb = (SQLLiteDBOASIS)result.Result;
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISAPIProviders In SQLLiteDB Property Getter. Reason: {result.Message}");
                    });
                }

                return _sqlLiteDb;
            }
        }

        public ThreeFoldOASIS ThreeFold
        {
            get
            {
                if (_threeFold == null)
                {
                    Task.Run(async () =>
                    {
                        OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.ThreeFoldOASIS);

                        if (result != null && !result.IsError)
                            _threeFold = (ThreeFoldOASIS)result.Result;
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISAPIProviders In ThreeFold Property Getter. Reason: {result.Message}");
                    });
                }

                return _threeFold;
            }
        }

        //public AcitvityPubOASIS ActivityPub
        //{
        //    get
        //    {
        //        if (_activityPub == null)
        //        {
        //            Task.Run(async () =>
        //            {
        //                OASISResult<IOASISStorageProvider> result = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.ActivityPubOASIS);

        //                if (result != null && !result.IsError)
        //                    _activityPub = (AcitvityPubOASIS)result.Result;
        //                else
        //                    OASISErrorHandling.HandleError(ref result, $"Error Occured In OASISAPIProviders In ActivityPub Property Getter. Reason: {result.Message}");
        //            });
        //        }

        //        return _activityPub;
        //    }
        //}

        public OASISProviders(OASISDNA OASISDNA)
        {
            _OASISDNA = OASISDNA;
        }
    }
}
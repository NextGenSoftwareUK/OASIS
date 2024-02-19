using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.AcitvityPubOASIS;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using NextGenSoftware.OASIS.API.Providers.TelosOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Native.EndPoint
{
    public class OASISProviders
    {
        EthereumOASIS _Ethereum;
        SolanaOASIS _Solana;
        EOSIOOASIS _EOSIO;
        TelosOASIS _Telos;
        SEEDSOASIS _SEEDS;
        IPFSOASIS _IPFS;
        HoloOASIS _Holochain;
        MongoDBOASIS _MongoDB;
        Neo4jOASIS _Neo4j;
        SQLLiteDBOASIS _sqlLiteDb;
        ThreeFoldOASIS _ThreeFold;
        AcitvityPubOASIS _ActivityPub;
        OASISDNA _OASISDNA;

        public SEEDSOASIS SEEDS
        {
            get
            {
                if (_SEEDS == null)
                {
                    if (ProviderManager.IsProviderRegistered(ProviderType.SEEDSOASIS))
                        _SEEDS = (SEEDSOASIS)ProviderManager.GetStorageProvider(ProviderType.SEEDSOASIS);
                    else
                    {
                        // We could re-use the TelosOASIS Provider but it could have a different connection string to SEEDSOASIS so they need to be seperate.
                        // TODO: The only other way is to share it and have to keep disconnecting and re-connecting with the different connections (SEEDS or EOSIO may even work with any EOSIO node end point? NEED TO TEST... if so then we can use the commented out line below).
                        //_SEEDS = new SEEDSOASIS(Telos); 
                        _SEEDS = new SEEDSOASIS(new TelosOASIS(
                            _OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ConnectionString,
                            _OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountName,
                            _OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ChainId,
                            _OASISDNA.OASIS.StorageProviders.EOSIOOASIS.AccountPrivateKey
                            ));
                        
                        ProviderManager.RegisterProvider(_SEEDS);
                    }
                }

                return _SEEDS;
            }
        }

        public IPFSOASIS IPFS
        {
            get
            {
                if (_IPFS == null)
                    _IPFS = (IPFSOASIS)Task.Run(async () => await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.IPFSOASIS)).Result;

                return _IPFS;
            }
        }

        public EOSIOOASIS EOSIO
        {
            get
            {
                if (_EOSIO == null)
                    _EOSIO = (EOSIOOASIS)Task.Run(async () => await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.EOSIOOASIS)).Result;

                return _EOSIO;
            }
        }

        public SolanaOASIS Solana
        {
            get
            {
                if (_Solana == null)
                    _Solana = (SolanaOASIS)Task.Run(async () => await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.SolanaOASIS)).Result;

                return _Solana;
            }
        }

        public EthereumOASIS Ethereum
        {
            get
            {
                if (_Ethereum == null)
                    _Ethereum = (EthereumOASIS)Task.Run(async () => await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.EthereumOASIS)).Result;

                return _Ethereum;
            }
        }

        public TelosOASIS Telos
        {
            get
            {
                if (_Telos == null)
                    _Telos = (TelosOASIS)Task.Run(async () => await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.TelosOASIS)).Result;

                return _Telos;
            }
        }

        public HoloOASIS Holochain
        {
            get
            {
                if (_Holochain == null)
                    _Holochain = (HoloOASIS)Task.Run(async () => await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.HoloOASIS)).Result;

                return _Holochain;
            }
        }

        public MongoDBOASIS MongoDB
        {
            get
            {
                if (_MongoDB == null)
                    _MongoDB = (MongoDBOASIS)Task.Run(async () => await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.MongoDBOASIS)).Result;

                return _MongoDB;
            }
        }

        public Neo4jOASIS Neo4j
        {
            get
            {
                if (_Neo4j == null)
                    _Neo4j = (Neo4jOASIS)Task.Run(async () => await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.Neo4jOASIS)).Result;

                return _Neo4j;
            }
        }

        public SQLLiteDBOASIS SQLLiteDB
        {
            get
            {
                if (_sqlLiteDb == null)
                    _sqlLiteDb = (SQLLiteDBOASIS)Task.Run(async () => await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.SQLLiteDBOASIS)).Result;

                return _sqlLiteDb;
            }
        }

        public ThreeFoldOASIS ThreeFold
        {
            get
            {
                if (_ThreeFold == null)
                    _ThreeFold = (ThreeFoldOASIS)Task.Run(async () => await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.ThreeFoldOASIS)).Result;

                return _ThreeFold;
            }
        }

        public AcitvityPubOASIS ActivityPub
        {
            get
            {
                if (_ActivityPub == null)
                    _ActivityPub = (AcitvityPubOASIS)Task.Run(async () => await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(ProviderType.ActivityPubOASIS)).Result;

                return _ActivityPub;
            }
        }

        public OASISProviders(OASISDNA OASISDNA)
        {
            _OASISDNA = OASISDNA;
        }
    }
}
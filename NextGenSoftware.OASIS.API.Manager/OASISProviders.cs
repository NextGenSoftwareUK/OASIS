﻿using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.AcitvityPubOASIS;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using NextGenSoftware.OASIS.API.Providers.TelosOASIS;

namespace NextGenSoftware.OASIS.API.Manager
{
    public class OASISProviders
    {
        SEEDSOASIS _SEEDS;
        IPFSOASIS _IPFS;
        EOSIOOASIS _EOSIO;
        TelosOASIS _Telos;
        HoloOASIS _Holochain;
        MongoDBOASIS _MongoDB;
        Neo4jOASIS _Neo4j;
        EthereumOASIS _Ethereum;
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
                        _SEEDS = new SEEDSOASIS(new TelosOASIS(_OASISDNA.OASIS.StorageProviders.SEEDSOASIS.ConnectionString));
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
                    _IPFS = (IPFSOASIS)OASISBootLoader.OASISBootLoader.RegisterProvider(ProviderType.IPFSOASIS);

                return _IPFS;
            }
        }

        public EOSIOOASIS EOSIO
        {
            get
            {
                if (_EOSIO == null)
                    _EOSIO = (EOSIOOASIS)OASISBootLoader.OASISBootLoader.RegisterProvider(ProviderType.EOSIOOASIS);

                return _EOSIO;
            }
        }

        public TelosOASIS Telos
        {
            get
            {
                if (_Telos == null)
                    _Telos = (TelosOASIS)OASISBootLoader.OASISBootLoader.RegisterProvider(ProviderType.TelosOASIS);

                return _Telos;
            }
        }

        public HoloOASIS Holochain
        {
            get
            {
                if (_Holochain == null)
                    _Holochain = (HoloOASIS)OASISBootLoader.OASISBootLoader.RegisterProvider(ProviderType.HoloOASIS);

                return _Holochain;
            }
        }

        public MongoDBOASIS MongoDB
        {
            get
            {
                if (_MongoDB == null)
                    _MongoDB = (MongoDBOASIS)OASISBootLoader.OASISBootLoader.RegisterProvider(ProviderType.MongoDBOASIS);

                return _MongoDB;
            }
        }

        public Neo4jOASIS Neo4j
        {
            get
            {
                if (_Neo4j == null)
                    _Neo4j = (Neo4jOASIS)OASISBootLoader.OASISBootLoader.RegisterProvider(ProviderType.Neo4jOASIS);

                return _Neo4j;
            }
        }

        public EthereumOASIS Ethereum
        {
            get
            {
                if (_Ethereum == null)
                    _Ethereum = (EthereumOASIS)OASISBootLoader.OASISBootLoader.RegisterProvider(ProviderType.EthereumOASIS);

                return _Ethereum;
            }
        }

        public ThreeFoldOASIS ThreeFold
        {
            get
            {
                if (_ThreeFold == null)
                    _ThreeFold = (ThreeFoldOASIS)OASISBootLoader.OASISBootLoader.RegisterProvider(ProviderType.ThreeFoldOASIS);

                return _ThreeFold;
            }
        }

        public AcitvityPubOASIS ActivityPub
        {
            get
            {
                if (_ActivityPub == null)
                    _ActivityPub = (AcitvityPubOASIS)OASISBootLoader.OASISBootLoader.RegisterProvider(ProviderType.ActivityPubOASIS);

                return _ActivityPub;
            }
        }

        public OASISProviders(OASISDNA OASISDNA)
        {
            _OASISDNA = OASISDNA;
        }
    }
}
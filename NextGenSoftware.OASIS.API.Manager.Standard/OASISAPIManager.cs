//using System.Collections.Generic;
//using NextGenSoftware.OASIS.API.DNA;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Managers;
//using NextGenSoftware.OASIS.API.Core.Apollo.Server;
//using NextGenSoftware.OASIS.API.Providers.AcitvityPubOASIS;
//using NextGenSoftware.OASIS.API.Providers.BlockStackOASIS;
//using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
//using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
//using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
//using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
//using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
//using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
//using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
//using NextGenSoftware.OASIS.API.Providers.TelosOASIS;

//namespace NextGenSoftware.OASIS.API.Manager.Standard
//{
//    public class OASISProviders
//    {
//        public SEEDSOASIS SEEDS { get; set; }
//        public IPFSOASIS IPFS { get; set; }
//        public EOSIOOASIS EOSIO { get; set; }
//        public HoloOASIS Holochain { get; set; }
//        public MongoDBOASIS MongoDB { get; set; }
//        public Neo4jOASIS Neo4j { get; set; }
//        public EthereumOASIS Ethereum { get; set; }
//        public ThreeFoldOASIS ThreeFold { get; set; }
//        public AcitvityPubOASIS ActivityPub { get; set; }
//    }

//    public static class OASISAPI
//    {
//        public static AvatarManager Avatar { get; set; }
//        public static HolonManager Data { get; set; }
//        public static MapManager Map { get; set; }
//        public static OASISProviders Providers { get; } = new OASISProviders();

//        public static void Init(InitOptions options, OASISDNA OASISDNA, bool startApolloServer = true)
//        {
//            switch (options)
//            {
//                case InitOptions.InitWithAllProviders:
//                    Init(ProviderManager.GetAllRegisteredProviders(), OASISDNA, startApolloServer);
//                    break;

//                case InitOptions.InitWithCurrentDefaultProvider:
//                    Init(new List<IOASISProvider>() { ProviderManager.CurrentStorageProvider }, OASISDNA, startApolloServer);
//                    break;
//            }
//        }

//        public static void Init(List<IOASISProvider> OASISProviders, OASISDNA OASISDNA, bool startApolloServer = true)
//        {
//            ProviderManager.RegisterProviders(OASISProviders); //TODO: Soon you will not need to pass these in since MEF will taKe care of this for us.

//            // TODO: Soon you will not need to inject in a provider because the mappings below will be used instead...
//            Map = new MapManager((IOASISStorage)OASISProviders[0]); 
//            Avatar = new AvatarManager((IOASISStorage)OASISProviders[0], OASISDNA);
//            Data = new HolonManager((IOASISStorage)OASISProviders[0]);
//            Providers.IPFS = new IPFSOASIS(OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
//            Providers.EOSIO = new EOSIOOASIS(OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ConnectionString);
//            Providers.Holochain = new HoloOASIS(OASISDNA.OASIS.StorageProviders.HoloOASIS.ConnectionString, Holochain.HoloNET.Client.Core.HolochainVersion.RSM);
//            Providers.MongoDB = new MongoDBOASIS(OASISDNA.OASIS.StorageProviders.MongoDBOASIS.ConnectionString, OASISDNA.OASIS.StorageProviders.MongoDBOASIS.DBName);
//            Providers.Neo4j = new Neo4jOASIS(OASISDNA.OASIS.StorageProviders.Neo4jOASIS.ConnectionString, OASISDNA.OASIS.StorageProviders.Neo4jOASIS.Username,  OASISDNA.OASIS.StorageProviders.Neo4jOASIS.Password);
//            Providers.ThreeFold = new ThreeFoldOASIS();
//            Providers.ActivityPub = new AcitvityPubOASIS();

//            // We could re-use the TelosOASIS Provider but it could have a different connection string to SEEDSOASIS so they need to be seperate.
//            // TODO: The only other way is to share it and have to keep disconnecting and re-connecting with the different connections (SEEDS or EOSIO may even work with any EOSIO node end point? NEED TO TEST... if so then we can use the commented out line below).
//            //Providers.SEEDS = new SEEDSOASIS(Providers.EOSIO); 
//            Providers.SEEDS = new SEEDSOASIS(new TelosOASIS(OASISDNA.OASIS.StorageProviders.SEEDSOASIS.ConnectionString));

//            ProviderManager.RegisterProvider(Providers.IPFS);
//            ProviderManager.RegisterProvider(Providers.EOSIO);
//            ProviderManager.RegisterProvider(Providers.Holochain);
//            ProviderManager.RegisterProvider(Providers.MongoDB);
//            ProviderManager.RegisterProvider(Providers.Neo4j);
//            ProviderManager.RegisterProvider(Providers.ThreeFold);
//            ProviderManager.RegisterProvider(Providers.ActivityPub);

//            ////TODO: Move the mappings to an external config wrapper than is injected into the OASISAPIManager constructor above...
//            //// Give HoloOASIS Store permission for the Name field (the field will only be stored on Holochain).
//            //Avatar.Config.FieldToProviderMappings.Name.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.Store, Provider = ProviderType.HoloOASIS });

//            //// Give all providers read/write access to the Karma field (will allow them to read and write to the field but it will only be stored on Holochain).
//            //// You could choose to store it on more than one provider if you wanted the extra redundancy (but not normally needed since Holochain has a lot of redundancy built in).
//            //Avatar.Config.FieldToProviderMappings.Karma.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.All });
//            ////this.AvatarManager.Config.FieldToProviderMappings.Name.Add(new AvatarManagerConfig.FieldToProviderMappingAccess { Access = AvatarManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.EthereumOASIS });
//            ////this.AvatarManager.Config.FieldToProviderMappings.Name.Add(new AvatarManagerConfig.FieldToProviderMappingAccess { Access = AvatarManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.IPFSOASIS });
//            ////this.AvatarManager.Config.FieldToProviderMappings.DOB.Add(new AvatarManagerConfig.FieldToProviderMappingAccess { Access = AvatarManagerConfig.ProviderAccess.Store, Provider = ProviderType.HoloOASIS });

//            ////Give Ethereum read-only access to the DOB field.
//            //Avatar.Config.FieldToProviderMappings.DOB.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.ReadOnly, Provider = ProviderType.EthereumOASIS });

//            if (startApolloServer)
//                ApolloServer.StartServer();
//        }
//    }
//}

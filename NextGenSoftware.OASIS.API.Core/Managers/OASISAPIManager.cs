
//using NextGenSoftware.OASIS.API.Core.Apollo.Server;
//using System.Collections.Generic;

//namespace NextGenSoftware.OASIS.API.Core
//{
//    public class OASISAPIManager
//    {
//        public AvatarManager AvatarManager { get; set; }
//        public MapManager MapManager { get; set; }

//    //    public KarmaManager MapManager { get; set; }

//        public OASISAPIManager(List<IOASISProvider> OASISProviders, bool startApolloServer = true)
//        //public OASISAPIManager()
//        {
//            ProviderManager.RegisterProviders(OASISProviders); //TODO: Soon you will not need to pass these in since MEF will taKe care of this for us.
//            this.MapManager = new MapManager((IOASISStorageProvider)OASISProviders[0]); //TODO: Fix this.
            
//            // TODO: Soon you will not need to inject in a provider because the mappings below will be used instead...
//            this.AvatarManager = new AvatarManager(ProviderManager.GetStorageProvider(ProviderType.HoloOASIS));

//            //TODO: Move the mappings to an external config wrapper than is injected into the OASISAPIManager constructor above...
//            // Give HoloOASIS Store permission for the Name field (the field will only be stored on Holochain).
//            this.AvatarManager.Config.FieldToProviderMappings.Name.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.Store, Provider = ProviderType.HoloOASIS });

//            // Give all providers read/write access to the Name field (will allow them to read and write to the field but it will only be stored on Holochain).
//            // You could choose to store it on more than one provider if you wanted the extra redundancy (but not normally needed since Holochain has a lot of redundancy built in).
//            this.AvatarManager.Config.FieldToProviderMappings.Name.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.All });
//            //this.AvatarManager.Config.FieldToProviderMappings.Name.Add(new AvatarManagerConfig.FieldToProviderMappingAccess { Access = AvatarManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.EthereumOASIS });
//            //this.AvatarManager.Config.FieldToProviderMappings.Name.Add(new AvatarManagerConfig.FieldToProviderMappingAccess { Access = AvatarManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.IPFSOASIS });
//            //this.AvatarManager.Config.FieldToProviderMappings.DOB.Add(new AvatarManagerConfig.FieldToProviderMappingAccess { Access = AvatarManagerConfig.ProviderAccess.Store, Provider = ProviderType.HoloOASIS });

//            //Give Ethereum read-only access to the DOB field.
//            this.AvatarManager.Config.FieldToProviderMappings.DOB.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.ReadOnly, Provider = ProviderType.EthereumOASIS });

//            if (startApolloServer)
//            {
//                ApolloServer.StartServer();
//            }
//        }
//    }
//}

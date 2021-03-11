using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Apollo.Server;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.OASISAPIManager
{
    public static class OASISAPIManager
    {
        public static AvatarManager AvatarManager { get; set; }
        public static MapManager MapManager { get; set; }
        public static SEEDSManager SEEDAPI { get; set; } = new SEEDSManager();
     

        public static void Init(List<IOASISProvider> OASISProviders, bool startApolloServer = true)
        //public OASISAPIManager()
        {
            ProviderManager.RegisterProviders(OASISProviders); //TODO: Soon you will not need to pass these in since MEF will taKe care of this for us.
            MapManager = new MapManager((IOASISStorage)OASISProviders[0]); //TODO: Fix this.

            // TODO: Soon you will not need to inject in a provider because the mappings below will be used instead...
            AvatarManager = new AvatarManager(ProviderManager.GetStorageProvider(ProviderType.HoloOASIS));

            //TODO: Move the mappings to an external config wrapper than is injected into the OASISAPIManager constructor above...
            // Give HoloOASIS Store permission for the Name field (the field will only be stored on Holochain).
            AvatarManager.Config.FieldToProviderMappings.Name.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.Store, Provider = ProviderType.HoloOASIS });

            // Give all providers read/write access to the Name field (will allow them to read and write to the field but it will only be stored on Holochain).
            // You could choose to store it on more than one provider if you wanted the extra redundancy (but not normally needed since Holochain has a lot of redundancy built in).
            AvatarManager.Config.FieldToProviderMappings.Name.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.All });
            //this.AvatarManager.Config.FieldToProviderMappings.Name.Add(new AvatarManagerConfig.FieldToProviderMappingAccess { Access = AvatarManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.EthereumOASIS });
            //this.AvatarManager.Config.FieldToProviderMappings.Name.Add(new AvatarManagerConfig.FieldToProviderMappingAccess { Access = AvatarManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.IPFSOASIS });
            //this.AvatarManager.Config.FieldToProviderMappings.DOB.Add(new AvatarManagerConfig.FieldToProviderMappingAccess { Access = AvatarManagerConfig.ProviderAccess.Store, Provider = ProviderType.HoloOASIS });

            //Give Ethereum read-only access to the DOB field.
            AvatarManager.Config.FieldToProviderMappings.DOB.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.ReadOnly, Provider = ProviderType.EthereumOASIS });

            if (startApolloServer)
            {
                ApolloServer.StartServer();
            }
        }
    }
}

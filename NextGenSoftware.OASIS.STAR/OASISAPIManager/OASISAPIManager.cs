using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Apollo.Server;

namespace NextGenSoftware.OASIS.STAR.OASISAPIManager
{
    public class OASISAPI
    {
        public AvatarManager Avatar { get; set; }
        public HolonManager Data { get; set; }
        public MapManager Map { get; set; }
        public OASISProviders Providers { get; private set; }

        //TODO: Not sure we need any of these methods? Because OASISDNAManager does all this for you! ;-) 
        /*
        public void Ignite(IgniteOptions options, OASISDNA OASISDNA, bool startApolloServer = true)
        {
            switch (options)
            {
                case IgniteOptions.IgniteWithAllProviders:
                    Ignite(ProviderManager.GetAllRegisteredProviders(), OASISDNA, startApolloServer);
                    break;

                case IgniteOptions.IgniteWithCurrentDefaultProvider:
                    Ignite(new List<IOASISProvider>() { ProviderManager.CurrentStorageProvider }, OASISDNA, startApolloServer);
                    break;
            }
        }

        public OASISResult<bool> Ignite(List<ProviderType> OASISProviderTypes, OASISDNA OASISDNA, bool startApolloServer = true)
        {
            if (!OASISDNAManager.IsInitialized)
                OASISDNAManager.Initialize(OASISDNA);

            OASISResult<bool> result = OASISDNAManager.RegisterProviders(OASISProviderTypes);

            // Set and Activate the first provider in the list. (may change in future how default provider is set but for now it will always be the first item in any list throughout the OASIS).
            if (!result.IsError)
                Ignite(ProviderManager.SetAndActivateCurrentStorageProvider(OASISProviderTypes[0]), OASISDNA, startApolloServer);

            return result;
        }

        public void Ignite(List<IOASISProvider> OASISProviders, OASISDNA OASISDNA, bool startApolloServer = true)
        {
            //TODO: Soon you will not need to pass these in since MEF will taKe care of this for us.
            if (ProviderManager.RegisterProviders(OASISProviders))
                Ignite(ProviderManager.SetAndActivateCurrentStorageProvider(OASISProviders[0]), OASISDNA, startApolloServer);
        }
        */

        public void Ignite(IOASISProvider currentStorageProvider, OASISDNA OASISDNA, bool startApolloServer = true)
        {
            // TODO: Soon you will not need to inject in a provider because the mappings below will be used instead...
            Map = new MapManager((IOASISStorage)currentStorageProvider);
            Avatar = new AvatarManager((IOASISStorage)currentStorageProvider, OASISDNA);
            Data = new HolonManager((IOASISStorage)currentStorageProvider);
            Providers = new OASISProviders(OASISDNA);

            if (startApolloServer)
                ApolloServer.StartServer();

            ////TODO: Move the mappings to an external config wrapper than is injected into the OASISAPIManager constructor above...
            //// Give HoloOASIS Store permission for the Name field (the field will only be stored on Holochain).
            //Avatar.Config.FieldToProviderMappings.Name.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.Store, Provider = ProviderType.HoloOASIS });

            //// Give all providers read/write access to the Karma field (will allow them to read and write to the field but it will only be stored on Holochain).
            //// You could choose to store it on more than one provider if you wanted the extra redundancy (but not normally needed since Holochain has a lot of redundancy built in).
            //Avatar.Config.FieldToProviderMappings.Karma.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.All });
            ////this.AvatarManager.Config.FieldToProviderMappings.Name.Add(new AvatarManagerConfig.FieldToProviderMappingAccess { Access = AvatarManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.EthereumOASIS });
            ////this.AvatarManager.Config.FieldToProviderMappings.Name.Add(new AvatarManagerConfig.FieldToProviderMappingAccess { Access = AvatarManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.IPFSOASIS });
            ////this.AvatarManager.Config.FieldToProviderMappings.DOB.Add(new AvatarManagerConfig.FieldToProviderMappingAccess { Access = AvatarManagerConfig.ProviderAccess.Store, Provider = ProviderType.HoloOASIS });

            ////Give Ethereum read-only access to the DOB field.
            //Avatar.Config.FieldToProviderMappings.DOB.Add(new ProviderManagerConfig.FieldToProviderMappingAccess { Access = ProviderManagerConfig.ProviderAccess.ReadOnly, Provider = ProviderType.EthereumOASIS });
        }
    }
}
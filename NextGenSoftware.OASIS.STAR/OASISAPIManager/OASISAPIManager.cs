using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Apollo.Server;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.STAR.OASISAPIManager
{
    public class OASISAPI
    {
        public bool IsOASISBooted { get; set; }
        public AvatarManager Avatar { get; set; }
        public HolonManager Data { get; set; }
        public MapManager Map { get; set; }
        public OASISProviders Providers { get; private set; }

        public OASISResult<bool> BootOASIS(OASISDNA OASISDNA, bool startApolloServer = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            // TODO: Soon you will not need to inject in a provider because the mappings below will be used instead...
            if (!OASISBootLoader.OASISBootLoader.IsOASISBooted)
                result = OASISBootLoader.OASISBootLoader.BootOASIS(OASISDNA);

            if (!result.IsError && result.Result)
                BootOASIS(startApolloServer);

            return result;
        }

        public OASISResult<bool> BootOASIS(string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            // TODO: Soon you will not need to inject in a provider because the mappings below will be used instead...
            if (!OASISBootLoader.OASISBootLoader.IsOASISBooted)
                result = OASISBootLoader.OASISBootLoader.BootOASIS(OASISDNAPath);

            if (!result.IsError && result.Result)
            {
                OASISResult<IOASISStorage> bootLoaderResult = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

                if (bootLoaderResult.IsError)
                {
                    result.IsError = true;
                    result.Message = bootLoaderResult.Message;
                }
                else
                    BootOASIS(startApolloServer);
            }

            return result;
        }

        public OASISResult<bool> ShutdownOASIS()
        {
            return OASISBootLoader.OASISBootLoader.ShutdownOASIS();
        }

        private void BootOASIS(bool startApolloServer = true)
        {
            Map = new MapManager(ProviderManager.CurrentStorageProvider);
            Avatar = new AvatarManager(ProviderManager.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
            Data = new HolonManager(ProviderManager.CurrentStorageProvider);
            Providers = new OASISProviders(OASISBootLoader.OASISBootLoader.OASISDNA);

            if (startApolloServer)
                ApolloServer.StartServer();

            IsOASISBooted = true;

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
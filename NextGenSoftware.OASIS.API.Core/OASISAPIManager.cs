using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public class OASISAPIManager
    {
        public ProfileManager ProfileManager { get; set; }
        public MapManager MapManager { get; set; }
        public ProviderManager ProviderManager { get; set; }

        //public OASISAPIManager(IOASISStorage OASISStorageProvider, IOASISNET OASISNETProvider)
        //public OASISAPIManager(IOASISStorage OASISStorageProvider)
        public OASISAPIManager(List<IOASISProvider> OASISProviders)
        {
            this.MapManager = new MapManager();
            this.ProviderManager = new ProviderManager();
            this.ProviderManager.RegisteredProviders.AddRange(OASISProviders);

            // TODO: Soon you will not need to inject in a provider because the mappings below will be used instead...
            //this.ProfileManager = new ProfileManager(OASISStorageProvider);
            this.ProfileManager = new ProfileManager(ProviderManager.GetStorageProviders());


            //TODO: Move the mappings to an external config wrapper than is injected into the OASISAPIManager constructor above...

            // Give HoloOASIS Store permission for the Name field (the field will only be stored on Holochain).
            this.ProfileManager.Config.FieldToProviderMappings.Name.Add(new ProfileManagerConfig.FieldToProviderMappingAccess { Access = ProfileManagerConfig.ProviderAccess.Store, Provider = ProviderType.HoloOASIS });

            // Give all providers read/write access to the Name field (will allow them to read and write to the field but it will only be stored on Holochain).
            // You could choose to store it on more than one provider if you wanted the extra redundancy (but not normally needed since Holochain has a lot of redundancy built in).
            this.ProfileManager.Config.FieldToProviderMappings.Name.Add(new ProfileManagerConfig.FieldToProviderMappingAccess { Access = ProfileManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.All });
            //this.ProfileManager.Config.FieldToProviderMappings.Name.Add(new ProfileManagerConfig.FieldToProviderMappingAccess { Access = ProfileManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.EthereumOASIS });
            //this.ProfileManager.Config.FieldToProviderMappings.Name.Add(new ProfileManagerConfig.FieldToProviderMappingAccess { Access = ProfileManagerConfig.ProviderAccess.ReadWrite, Provider = ProviderType.IPFSOASIS });
            //this.ProfileManager.Config.FieldToProviderMappings.DOB.Add(new ProfileManagerConfig.FieldToProviderMappingAccess { Access = ProfileManagerConfig.ProviderAccess.Store, Provider = ProviderType.HoloOASIS });

            //Give Ethereum read-only access to the DOB field.
            this.ProfileManager.Config.FieldToProviderMappings.DOB.Add(new ProfileManagerConfig.FieldToProviderMappingAccess { Access = ProfileManagerConfig.ProviderAccess.ReadOnly, Provider = ProviderType.EthereumOASIS });

        }

        /*
        public async Task<IProfile> LoadProfileAsync(string providerKey)
        {
            return await ProfileManager.LoadProfileAsync(providerKey);
        }

        public async Task<IProfile> LoadProfileAsync(Guid id)
        {
            return await ProfileManager.LoadProfileAsync(id);
        }

        public async Task<IProfile> LoadProfileAsync(string username, string password)
        {
            return await ProfileManager.LoadProfileAsync(username, password);
        }

        public async Task<IProfile> SaveProfileAsync(IProfile profile)
        {
            return await ProfileManager.SaveProfileAsync(profile);
        }

        public async Task<bool> AddKarmaToProfileAsync(IProfile profile, int karma)
        {
            return await ProfileManager.AddKarmaToProfileAsync(profile, karma);
        }

        public async Task<bool> RemoveKarmaToProfileAsync(IProfile profile, int karma)
        {
            return await ProfileManager.RemoveKarmaFromProfileAsync(profile, karma);
        }*/
    }
}

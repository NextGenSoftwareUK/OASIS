//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.DNA;
//using NextGenSoftware.OASIS.API.Core.Managers;
//using NextGenSoftware.OASIS.API.Core.Apollo.Server;
//using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
//using NextGenSoftware.OASIS.Common;

//namespace NextGenSoftware.OASIS.API.Native.EndPoint
//{
//    public class OASISAPI
//    {
//        public static bool IsOASISBooted { get; set; }
//        public static string OASISVersion { get; set; }
//        public static OASISDNA OASISDNA { get; set; }
//        public static AvatarManager Avatar { get; set; }
//        public static HolonManager Data { get; set; }
//        public static KeyManager Keys { get; set; }
//        public static WalletManager Wallets { get; set; }
//        public static NFTManager NFTs { get; set; }
//        public static OASISProviders Providers { get; private set; }
//        public static SearchManager Search { get; set; }
//        public static MapManager Map { get; set; }
//        public static MissionManager Missions { get; set; }
//        public static QuestManager Quests { get; set; }
//        public static ParkManager Parks { get; set; }
//        public static OLandManager OLAND { get; set; }

//        public static OASISResult<bool> BootOASIS(OASISDNA OASISDNA, bool startApolloServer = true)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();

//            if (!OASISBootLoader.OASISBootLoader.IsOASISBooted)
//                result = OASISBootLoader.OASISBootLoader.BootOASIS(OASISDNA);

//            if (!result.IsError && result.Result)
//                InitOASIS(startApolloServer);

//            return result;
//        }

//        public static async Task<OASISResult<bool>> BootOASISAsync(OASISDNA OASISDNA, bool startApolloServer = true)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();

//            if (!OASISBootLoader.OASISBootLoader.IsOASISBooted)
//                result = await OASISBootLoader.OASISBootLoader.BootOASISAsync(OASISDNA);

//            if (!result.IsError && result.Result)
//                InitOASIS(startApolloServer);

//            return result;
//        }

//        public static OASISResult<bool> BootOASIS(string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();

//            if (!OASISBootLoader.OASISBootLoader.IsOASISBooted)
//                result = OASISBootLoader.OASISBootLoader.BootOASIS(OASISDNAPath);

//            if (!result.IsError && result.Result)
//                InitOASIS(startApolloServer);

//            return result;
//        }

//        public static async Task<OASISResult<bool>> BootOASISAsync(string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();

//            if (!OASISBootLoader.OASISBootLoader.IsOASISBooted)
//                result = await OASISBootLoader.OASISBootLoader.BootOASISAsync(OASISDNAPath);

//            if (!result.IsError && result.Result)
//                InitOASIS(startApolloServer);

//            return result;
//        }

//        public static OASISResult<bool> ShutdownOASIS()
//        {
//            return OASISBootLoader.OASISBootLoader.ShutdownOASIS();
//        }

//        public static async Task<OASISResult<bool>> ShutdownOASISAsync()
//        {
//            return await OASISBootLoader.OASISBootLoader.ShutdownOASISAsync();
//        }

//        private static void InitOASIS(bool startApolloServer = true)
//        {
//            //OASISVersion = OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.OASISVersion == "LIVE" ? OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.CurrentLiveVersion : OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.CurrentStagingVersion;
//            OASISVersion = OASISBootLoader.OASISBootLoader.OASISVersion;
//            OASISDNA = OASISBootLoader.OASISBootLoader.OASISDNA;
//            Avatar = new AvatarManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
//            Data = new HolonManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
//            Keys = new KeyManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
//            Wallets = new WalletManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
//            NFTs = new NFTManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
//            Map = new MapManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
//            Missions = new MissionManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
//            Quests = new QuestManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
//            Parks = new ParkManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
//            OLAND = new OLandManager(NFTs, ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
//            Search = new SearchManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);

//            Providers = new OASISProviders(OASISBootLoader.OASISBootLoader.OASISDNA);

//            if (startApolloServer)
//                ApolloServer.StartServer();

//            IsOASISBooted = true;

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
//        }
//    }
//}
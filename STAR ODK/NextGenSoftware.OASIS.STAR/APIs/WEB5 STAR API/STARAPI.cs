//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.OASIS.API.DNA;
//using NextGenSoftware.OASIS.API.Core.Managers;
//using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

//namespace NextGenSoftware.OASIS.STAR.OASISAPIManager
//{
//    public class STARAPI
//    {
//        OASISAPI OASISAPI { get; set; } = new OASISAPI();
//        public bool IsOASISBooted { get; set; }
//        public string OASISVersion { get; set; }
//        public OASISDNA OASISDNA { get; set; } 
//        public AvatarManager Avatar { get; set; }
//        public MissionManager Missions { get; set; }
//        public ChapterManager Chapters { get; set; }
//        public QuestManager Quests { get; set; }
//        public InventoryItemManager InventoryItems { get; set; }
//        public GeoHotSpotManager GeoHotSpots { get; set; }
//        public MapManager Map { get; set; }
//        public ParkManager Parks { get; set; }
//        public OLandManager OLAND { get; set; }
//        public OAPPManager OAPPs { get; set; }
//        public OAPPTemplateManager OAPPTemplates { get; set; }
//        public RuntimeManager Runtimes { get; set; }

//        public OASISResult<bool> BootSTARAPI(string userName, string password, OASISDNA OASISDNA, bool startApolloServer = true)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();
//            result = OASISAPI.BootOASIS(userName, password, OASISDNA, startApolloServer);

//            if (result != null && result.Result != null && !result.IsError)
//                InitManagers();

//            return result;
//        }

//        public async Task<OASISResult<bool>> BootSTARAPIAsync(string userName, string password, OASISDNA OASISDNA, bool startApolloServer = true)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();
//            result = await OASISAPI.BootOASISAsync(userName, password, OASISDNA, startApolloServer);

//            if (result != null && result.Result != null && !result.IsError)
//                InitManagers();

//            return result;
//        }

//        public OASISResult<bool> BootSTARAPI(string userName, string password, string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();
//            result = OASISAPI.BootOASIS(userName, password, OASISDNAPath, startApolloServer);

//            if (result != null && result.Result != null && !result.IsError)
//                InitManagers();

//            return result;
//        }

//        public async Task<OASISResult<bool>> BootSTARAPIAsync(string userName, string password, string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
//        {
//            OASISResult<bool> result = new OASISResult<bool>();
//            result = await OASISAPI.BootOASISAsync(userName, password, OASISDNAPath, startApolloServer);

//            if (result != null && result.Result != null && !result.IsError)
//                InitManagers();

//            return result;
//        }

//        public static OASISResult<bool> ShutdownSTARAPI()
//        {
//            return OASISBootLoader.OASISBootLoader.ShutdownOASIS();
//        }

//        public static async Task<OASISResult<bool>> ShutdownSTARAPIAsync()
//        {
//            return await OASISBootLoader.OASISBootLoader.ShutdownOASISAsync();
//        }

//        private void InitManagers()
//        {
//            //These are the OASIS.API.ONODE.Core Managers.
//            //NFTs = new NFTManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
//            GeoHotSpots = new GeoHotSpotManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
//            Map = new MapManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
//            Chapters = new ChapterManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
//            Missions = new MissionManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
//            //Quests = new QuestManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, NFTs, OASISBootLoader.OASISBootLoader.OASISDNA);
//            Parks = new ParkManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
//            //OLAND = new OLandManager(NFTs, ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
//            OAPPs = new OAPPManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
//            OAPPTemplates = new OAPPTemplateManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
//            InventoryItems = new InventoryItemManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
//            Runtimes = new RuntimeManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
//        }
//    }
//}
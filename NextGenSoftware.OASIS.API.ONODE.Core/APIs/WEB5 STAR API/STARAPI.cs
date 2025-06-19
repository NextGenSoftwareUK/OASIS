using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Exceptions;

namespace NextGenSoftware.OASIS.API.ONODE.Core
{
    public class STARAPI
    {
        private MissionManager _missions = null;
        private ChapterManager _chapters = null;
        private QuestManager _quests = null;
        private InventoryItemManager _inventoryItems = null;
        private GeoHotSpotManager _geoHotSpots = null;
        private MapManager _map = null;
        private ParkManager _parks = null;
        //private OLandManager _olands = null;
        private OAPPManager _oapps = null;
        private OAPPTemplateManager _oappTemplates = null;
        private RuntimeManager _runtimes = null;

        private STARAPI() { }

        //public static async Task<STARAPI> CreateAsync(string userName, string password, OASISDNA OASISDNA, bool startApolloServer = true)
        //{
        //    var instance = new STARAPI();
        //    await instance.BootSTARAPIAsync(userName, password, OASISDNA, startApolloServer);
        //    return instance;
        //}

        //public static STARAPI Create(string userName, string password, OASISDNA OASISDNA, bool startApolloServer = true)
        //{
        //    var instance = new STARAPI();
        //    instance.BootSTARAPI(userName, password, OASISDNA, startApolloServer);
        //    return instance;
        //}

        //public static async Task<STARAPI> CreateAsync(string userName, string password, string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
        //{
        //    var instance = new STARAPI();
        //    await instance.BootSTARAPIAsync(userName, password, OASISDNAPath, startApolloServer);
        //    return instance;
        //}

        //public static STARAPI Create(string userName, string password, string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
        //{
        //    var instance = new STARAPI();
        //    instance.BootSTARAPI(userName, password, OASISDNAPath, startApolloServer);
        //    return instance;
        //}

        OASISAPI OASISAPI { get; set; } = new OASISAPI();
        public bool IsOASISBooted { get; set; }
        public string OASISVersion { get; set; } //TODO: NEED TO FIX LATER!
        public OASISDNA OASISDNA { get; set; } //TODO: NEED TO FIX LATER!
        public AvatarManager Avatar { get; set; } //TODO: NEED TO FIX LATER!
        //public MissionManager Missions { get; set; } //TODO: NEED TO FIX LATER!

        public MissionManager Missions
        {
            get
            {
                if (_missions == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _missions = new MissionManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Missions property!");
                }

                return _missions;
            }
        }

        public ChapterManager Chapters
        {
            get
            {
                if (_chapters == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _chapters = new ChapterManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Chapters property!");
                }

                return _chapters;
            }
        }

        public QuestManager Quests
        {
            get
            {
                if (_quests == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _quests = new QuestManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Quests property!");
                }

                return _quests;
            }
        }

        public InventoryItemManager InventoryItems
        {
            get
            {
                if (_inventoryItems == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _inventoryItems = new InventoryItemManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the InventoryItems propertyv!");
                }

                return _inventoryItems;
            }
        }

        public GeoHotSpotManager GeoHotSpots
        {
            get
            {
                if (_geoHotSpots == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _geoHotSpots = new GeoHotSpotManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the GeoHotSpots property!");
                }

                return _geoHotSpots;
            }
        }

        public STARNFTManager NFTs
        {
            get
            {
                if (_geoHotSpots == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _geoHotSpots = new GeoHotSpotManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the GeoHotSpots property!");
                }

                return _geoHotSpots;
            }
        }


        public STARGeoNFTManager GeoNFTs
        {
            get
            {
                if (_geoHotSpots == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _geoHotSpots = new GeoHotSpotManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the GeoHotSpots property!");
                }

                return _geoHotSpots;
            }
        }

        public MapManager Map
        {
            get
            {
                if (_map == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _map = new MapManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Map property!");
                }

                return _map;
            }
        }

        public ParkManager Parks
        {
            get
            {
                if (_parks == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _parks = new ParkManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Parks property!");
                }

                return _parks;
            }
        }

        //public OLandManager OLAND
        //{
        //    get
        //    {
        //        if (_olands == null)
        //        {
        //            if (OASISAPI.IsOASISBooted)
        //                _olands = new OLandManager(NFTs, ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //            else
        //                throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the OLAND property!");
        //        }

        //        return _olands;
        //    }
        //}

        public OAPPManager OAPPs
        {
            get
            {
                if (_oapps == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _oapps = new OAPPManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the OAPPs property!");
                }

                return _oapps;
            }
        }

        public OAPPTemplateManager OAPPTemplates
        {
            get
            {
                if (_oappTemplates == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _oappTemplates = new OAPPTemplateManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the OAPPTemplates property!");
                }

                return _oappTemplates;
            }
        }

        public RuntimeManager Runtimes
        {
            get
            {
                if (_runtimes == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _runtimes = new RuntimeManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Runtimes property!");
                }

                return _runtimes;
            }
        }

        public CelestialBodyManager CelestialBodies
        {
            get
            {
                if (_runtimes == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _runtimes = new RuntimeManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Runtimes property!");
                }

                return _runtimes;
            }
        }

        public CelestialSpaceManager CelestialSpaces
        {
            get
            {
                if (_runtimes == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _runtimes = new RuntimeManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Runtimes property!");
                }

                return _runtimes;
            }
        }

        public STARZomeManager Zomes
        {
            get
            {
                if (_runtimes == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _runtimes = new RuntimeManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Runtimes property!");
                }

                return _runtimes;
            }
        }

        public STARHolonManager Holons
        {
            get
            {
                if (_runtimes == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _runtime  s = new RuntimeManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Runtimes property!");
                }

                return _runtimes;
            }
        }

        public OASISResult<bool> BootSTARAPI(string userName, string password, OASISDNA OASISDNA, bool startApolloServer = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            result = OASISAPI.BootOASIS(OASISDNA, userName, password, startApolloServer);

            if (result != null && result.Result != null && !result.IsError)
                InitManagers();

            return result;
        }

        public async Task<OASISResult<bool>> BootSTARAPIAsync(string userName, string password, OASISDNA OASISDNA, bool startApolloServer = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            result = await OASISAPI.BootOASISAsync(OASISDNA, userName, password, startApolloServer);

            if (result != null && result.Result != null && !result.IsError)
                InitManagers();

            return result;
        }

        public OASISResult<bool> BootSTARAPI(string userName, string password, string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            result = OASISAPI.BootOASIS(userName, password, OASISDNAPath, startApolloServer);

            if (result != null && result.Result != null && !result.IsError)
                InitManagers();

            return result;
        }

        public async Task<OASISResult<bool>> BootSTARAPIAsync(string userName, string password, string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            result = await OASISAPI.BootOASISAsync(userName, password, OASISDNAPath, startApolloServer);

            if (result != null && result.Result != null && !result.IsError)
                InitManagers();

            return result;
        }

        public static OASISResult<bool> ShutdownSTARAPI()
        {
            return OASISBootLoader.OASISBootLoader.ShutdownOASIS();
        }

        public static async Task<OASISResult<bool>> ShutdownSTARAPIAsync()
        {
            return await OASISBootLoader.OASISBootLoader.ShutdownOASISAsync();
        }

        private void InitManagers() 
        {
            //These are the OASIS.API.ONODE.Core Managers.
            ////NFTs = new NFTManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
            //GeoHotSpots = new GeoHotSpotManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
            //Map = new MapManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
            //Chapters = new ChapterManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
            //Missions = new MissionManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
            //Quests = new QuestManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, NFTs, OASISBootLoader.OASISBootLoader.OASISDNA);
            //Parks = new ParkManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
            //OLAND = new OLandManager(NFTs, ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
            //OAPPs = new OAPPManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
            //OAPPTemplates = new OAPPTemplateManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
            //InventoryItems = new InventoryItemManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
            //Runtimes = new RuntimeManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        }
    }
}
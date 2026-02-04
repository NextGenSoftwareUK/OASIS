using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.Native.EndPoint
{
    public class STARAPI
    {
        //private IOmiverse _omiverse = null;
        //private COSMICManager _cosmicManager = null;
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
        private LibraryManager _libs = null;
        private STARNFTManager _nfts = null;
        private STARGeoNFTManager _geoNFTs = null;
        private CelestialBodyManager _celestialBodies = null;
        private CelestialSpaceManager _celestialSpaces = null;
        private STARZomeManager _zomes = null;
        private STARHolonManager _holons = null;
        private CelestialBodyMetaDataDNAManager _celestialBodiesDNA = null;
        private ZomeMetaDataDNAManager _zomesDNA = null;
        private HolonMetaDataDNAManager _holonsDNA = null;
        private PluginManager _plugins = null;
        private GameManager _game = null;
        //private COSMICManager _cosmic = null;

        public STARAPI(STARDNA STARDNA, OASISAPI OASISAPI = null) 
        {
            this.STARDNA = STARDNA;

            if (OASISAPI != null)
                this.OASISAPI = OASISAPI;
        }

        public STARDNA STARDNA { get; set; }

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
        public string OASISVersion { get; set; }
        public OASISDNA OASISDNA { get; set; }

        //public AvatarManager Avatar
        //{
        //    get
        //    {
        //        if (_avatar == null)
        //        {
        //            if (OASISAPI.IsOASISBooted)
        //                _avatar = new AvatarManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
        //            else
        //                throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Avatar property!");
        //        }
        //        return _avatar;
        //    }
        //}

        //public AvatarManager Avatars
        //{
        //    get
        //    {
        //        return OASISAPI.Avatars;
        //    }
        //}

        //public COSMICManager COSMIC
        //{
        //    get
        //    {
        //        if (_cosmicManager == null)
        //        {
        //            if (!OASISAPI.IsOASISBooted)
        //                throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the COSMIC property!");

        //            else if (AvatarManager.LoggedInAvatar == null || (AvatarManager.LoggedInAvatar != null && AvatarManager.LoggedInAvatar.Id.ToString() == OASISDNA.OASIS.OASISSystemAccountId))
        //                throw new OASISException("No avatar is beamed in. Please beam in before accessing the COSMIC property!");

        //            else
        //                _cosmicManager = new COSMICManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //        }

        //        return _cosmicManager;
        //    }
        //}


        //public IOmiverse Omniverse
        //{
        //    get
        //    {
        //        if (_omiverse == null)
        //        {
        //            if (!OASISAPI.IsOASISBooted)
        //                throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Missions property!");

        //            else if (AvatarManager.LoggedInAvatar == null || (AvatarManager.LoggedInAvatar != null && AvatarManager.LoggedInAvatar.Id.ToString() == OASISDNA.OASIS.OASISSystemAccountId))
        //                throw new OASISException("No avatar is beamed in. Please beam in before accessing the Missions property!");

        //            else
        //                _omiverse = AvatarManager.LoggedInAvatar.O
        //        }

        //        return _omiverse;
        //    }
        //}
        public MissionManager Missions
        {
            get
            {
                if (_missions == null)
                {
                    if (!OASISAPI.IsOASISBooted)
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Missions property!");

                    else if (AvatarManager.LoggedInAvatar == null || (AvatarManager.LoggedInAvatar != null && AvatarManager.LoggedInAvatar.Id.ToString() == OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.OASISSystemAccountId))
                        throw new OASISException("No avatar is beamed in. Please beam in before accessing the Missions property!");

                    else
                        _missions = new MissionManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                }

                return _missions;
            }
        }

        //TODO: Make all other properties like Missions ASAP! :)
        public ChapterManager Chapters
        {
            get
            {
                if (_chapters == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _chapters = new ChapterManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
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
                        _quests = new QuestManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Quests property!");
                }

                return _quests;
            }
        }

        public GameManager Game
        {
            get
            {
                if (_game == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _game = new GameManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Game property!");
                }

                return _game;
            }
        }

        public InventoryItemManager InventoryItems
        {
            get
            {
                if (_inventoryItems == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _inventoryItems = new InventoryItemManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
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
                        _geoHotSpots = new GeoHotSpotManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
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
                if (_nfts == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _nfts = new STARNFTManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the NFTs property!");
                }

                return _nfts;
            }
        }


        public STARGeoNFTManager GeoNFTs
        {
            get
            {
                if (_geoNFTs == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _geoNFTs = new STARGeoNFTManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the GeoNFTs property!");
                }

                return _geoNFTs;
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
                        _oapps = new OAPPManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
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
                        _oappTemplates = new OAPPTemplateManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
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
                        _runtimes = new RuntimeManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Runtimes property!");
                }

                return _runtimes;
            }
        }

        public LibraryManager Libraries
        {
            get
            {
                if (_libs == null)
                {
                    if (!OASISAPI.IsOASISBooted)
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Libraries property!");

                    else if (AvatarManager.LoggedInAvatar == null || (AvatarManager.LoggedInAvatar != null && AvatarManager.LoggedInAvatar.Id.ToString() == OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.OASISSystemAccountId))
                        throw new OASISException("No avatar is beamed in. Please beam in before accessing the Libraries property!");

                    else
                        _libs = new LibraryManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                }

                return _libs;
            }
        }

        public CelestialBodyManager CelestialBodies
        {
            get
            {
                if (_celestialBodies == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _celestialBodies = new CelestialBodyManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the CelestialBodies property!");
                }

                return _celestialBodies;
            }
        }

        public CelestialSpaceManager CelestialSpaces
        {
            get
            {
                if (_celestialSpaces == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _celestialSpaces = new CelestialSpaceManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the CelestialSpaces property!");
                }

                return _celestialSpaces;
            }
        }

        public STARZomeManager Zomes
        {
            get
            {
                if (_zomes == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _zomes = new STARZomeManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Zomes property!");
                }

                return _zomes;
            }
        }

        public STARHolonManager Holons
        {
            get
            {
                if (_holons == null)
                {
                    if (OASISAPI.IsOASISBooted)
                        _holons = new STARHolonManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Holons property!");
                }

                return _holons;
            }
        }

        public CelestialBodyMetaDataDNAManager CelestialBodiesMetaDataDNA
        {
            get
            {
                if (_celestialBodiesDNA == null)
                {
                    if (!OASISAPI.IsOASISBooted)
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the CelestialBodiesMetaDataDNA property!");

                    else if (AvatarManager.LoggedInAvatar == null || (AvatarManager.LoggedInAvatar != null && AvatarManager.LoggedInAvatar.Id.ToString() == OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.OASISSystemAccountId))
                        throw new OASISException("No avatar is beamed in. Please beam in before accessing the CelestialBodiesMetaDataDNA property!");

                    else
                        _celestialBodiesDNA = new CelestialBodyMetaDataDNAManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                }

                return _celestialBodiesDNA;
            }
        }

        public ZomeMetaDataDNAManager ZomesMetaDataDNA
        {
            get
            {
                if (_zomesDNA == null)
                {
                    if (!OASISAPI.IsOASISBooted)
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the ZomesMetaDataDNA property!");

                    else if (AvatarManager.LoggedInAvatar == null || (AvatarManager.LoggedInAvatar != null && AvatarManager.LoggedInAvatar.Id.ToString() == OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.OASISSystemAccountId))
                        throw new OASISException("No avatar is beamed in. Please beam in before accessing the ZomesMetaDataDNA property!");

                    else
                        _zomesDNA = new ZomeMetaDataDNAManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                }

                return _zomesDNA;
            }
        }

        public HolonMetaDataDNAManager HolonsMetaDataDNA
        {
            get
            {
                if (_holonsDNA == null)
                {
                    if (!OASISAPI.IsOASISBooted)
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the HolonsMetaDataDNA property!");

                    else if (AvatarManager.LoggedInAvatar == null || (AvatarManager.LoggedInAvatar != null && AvatarManager.LoggedInAvatar.Id.ToString() == OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.OASISSystemAccountId))
                        throw new OASISException("No avatar is beamed in. Please beam in before accessing the HolonsMetaDataDNA property!");

                    else
                        _holonsDNA = new HolonMetaDataDNAManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                }

                return _holonsDNA;
            }
        }

        public PluginManager Plugins
        {
            get
            {
                if (_plugins == null)
                {
                    if (!OASISAPI.IsOASISBooted)
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Plugins property!");

                    else if (AvatarManager.LoggedInAvatar == null || (AvatarManager.LoggedInAvatar != null && AvatarManager.LoggedInAvatar.Id.ToString() == OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.OASISSystemAccountId))
                        throw new OASISException("No avatar is beamed in. Please beam in before accessing the Plugins property!");

                    else
                        _plugins = new PluginManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, STARDNA, OASISBootLoader.OASISBootLoader.OASISDNA);
                }

                return _plugins;
            }
        }

        //public COSMICManager COSMIC
        //{
        //    get
        //    {
        //        if (_cosmic == null)
        //        {
        //            if (IsOASISBooted)
        //            {
        //                if (AvatarManager.LoggedInAvatar != null && AvatarManager.LoggedInAvatar.Id != Guid.Empty)
        //                    _cosmic = new COSMICManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.Id, OASISBootLoader.OASISBootLoader.OASISDNA);
        //                else
        //                    _cosmic = new COSMICManager(ProviderManager.Instance.CurrentStorageProvider, Guid.NewGuid(), OASISBootLoader.OASISBootLoader.OASISDNA);
        //            }
        //            else
        //                throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the COSMIC property!");
        //        }

        //        return _cosmic;
        //    }
        //}

        public OASISResult<bool> BootOASISAPI(string userName, string password, OASISDNA OASISDNA, bool startApolloServer = true)
        {
           return OASISAPI.BootOASIS(OASISDNA, userName, password, startApolloServer);
        }

        public async Task<OASISResult<bool>> BootSTARAPIAsync(string userName, string password, OASISDNA OASISDNA, bool startApolloServer = true)
        {
            return await OASISAPI.BootOASISAsync(OASISDNA, userName, password, startApolloServer);
        }

        public OASISResult<bool> BootOASISAPI(string userName, string password, string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
        {
            return OASISAPI.BootOASIS(userName, password, OASISDNAPath, startApolloServer);
        }

        public async Task<OASISResult<bool>> BootOASISAsync(string userName, string password, string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
        {
            return await OASISAPI.BootOASISAsync(userName, password, OASISDNAPath, startApolloServer);
        }

        public static OASISResult<bool> ShutdownOASIS()
        {
            return OASISBootLoader.OASISBootLoader.ShutdownOASIS();
        }

        public static async Task<OASISResult<bool>> ShutdownOASISAsync()
        {
            return await OASISBootLoader.OASISBootLoader.ShutdownOASISAsync();
        }

        //public OASISResult<bool> BootSTARAPI(string userName, string password, OASISDNA OASISDNA, bool startApolloServer = true)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();
        //    result = OASISAPI.BootOASIS(OASISDNA, userName, password, startApolloServer);

        //    if (result != null && result.Result != null && !result.IsError)
        //        InitManagers();

        //    return result;
        //}

        //public async Task<OASISResult<bool>> BootSTARAPIAsync(string userName, string password, OASISDNA OASISDNA, bool startApolloServer = true)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();
        //    result = await OASISAPI.BootOASISAsync(OASISDNA, userName, password, startApolloServer);

        //    if (result != null && result.Result != null && !result.IsError)
        //        InitManagers();

        //    return result;
        //}

        //public OASISResult<bool> BootSTARAPI(string userName, string password, string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();
        //    result = OASISAPI.BootOASIS(userName, password, OASISDNAPath, startApolloServer);

        //    if (result != null && result.Result != null && !result.IsError)
        //        InitManagers();

        //    return result;
        //}

        //public async Task<OASISResult<bool>> BootSTARAPIAsync(string userName, string password, string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();
        //    result = await OASISAPI.BootOASISAsync(userName, password, OASISDNAPath, startApolloServer);

        //    if (result != null && result.Result != null && !result.IsError)
        //        InitManagers();

        //    return result;
        //}

        //public static OASISResult<bool> ShutdownSTARAPI()
        //{
        //    return OASISBootLoader.OASISBootLoader.ShutdownOASIS();
        //}

        //public static async Task<OASISResult<bool>> ShutdownSTARAPIAsync()
        //{
        //    return await OASISBootLoader.OASISBootLoader.ShutdownOASISAsync();
        //}

        //private void InitManagers() 
        //{
        //    //These are the OASIS.API.ONODE.Core Managers.
        //    ////NFTs = new NFTManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //GeoHotSpots = new GeoHotSpotManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //Map = new MapManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //Chapters = new ChapterManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //Missions = new MissionManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //Quests = new QuestManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, NFTs, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //Parks = new ParkManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //OLAND = new OLandManager(NFTs, ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //OAPPs = new OAPPManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //OAPPTemplates = new OAPPTemplateManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //InventoryItems = new InventoryItemManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //Runtimes = new RuntimeManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //}
    }
}
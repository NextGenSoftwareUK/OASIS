using System.Net;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Native.EndPoint
{
    public class OASISAPI
    {
        private AvatarManager _avatar = null;
        private NFTManager _nfts = null;
        private OLandManager _olands = null;
        private HolonManager _data = null;
        private KeyManager _keys = null;
        private WalletManager _wallets = null;
        private OASISProviders _providers = null;
        private SearchManager _search = null;
        private ONODEManager _onode = null;
        private ONETManager _onet = null;
        private SettingsManager _settings = null;
        private KarmaManager _karma = null;
        private MapManager _map = null;
        private ChatManager _chat = null;
        private MessagingManager _messaging = null;
        private CompetitionManager _competition = null;
        private GiftsManager _gifts = null;
        private FilesManager _files = null;
        private SocialManager _social = null;
        private VideoManager _video = null;
        //private ShareManager _share = null;
        private SeedsManager _seeds = null;
        //private TelosManager _telos = null;
        private StatsManager _stats = null;
        private ProviderManager _provider = null;
        private COSMICManager _cosmic = null;

        public bool IsOASISBooted { get; set; }
        //public string OASISRunVersion { get; set; }
        public OASISDNA OASISDNA { get; set; } 

        public AvatarManager Avatars
        {
            get
            {
                if (_avatar == null)
                {
                    if (IsOASISBooted)
                        _avatar = new AvatarManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Avatar property!");
                }

                return _avatar;
            }
        }

        public OLandManager OLAND
        {
            get
            {
                if (_olands == null)
                {
                    if (IsOASISBooted)
                        _olands = new OLandManager(NFTs, ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the OLAND property!");
                }

                return _olands;
            }
        }

        public NFTManager NFTs
        {
            get
            {
                if (_nfts == null)
                {
                    if (IsOASISBooted)
                        _nfts = new NFTManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the NFTs property!");
                }

                return _nfts;
            }
        }

        public HolonManager Data
        {
            get
            {
                if (_data == null)
                {
                    if (IsOASISBooted)
                        _data = new HolonManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Data property!");
                }

                return _data;
            }
        }

        public KeyManager Keys
        {
            get
            {
                if (_keys == null)
                {
                    if (IsOASISBooted)
                        _keys = new KeyManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Keys property!");
                }

                return _keys;
            }
        }

        public WalletManager Wallets
        {
            get
            {
                if (_wallets == null)
                {
                    if (IsOASISBooted)
                        _wallets = new WalletManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Wallets property!");
                }

                return _wallets;
            }
        }

        public OASISProviders Providers
        {
            get
            {
                if (_providers == null)
                {
                    if (IsOASISBooted)
                        _providers = new OASISProviders(OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Providers property!");
                }

                return _providers;
            }
        }

        public SearchManager Search
        {
            get
            {
                if (_search == null)
                {
                    if (IsOASISBooted)
                        _search = new SearchManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Search property!");
                }

                return _search;
            }
        }

        public ONODEManager ONODE
        {
            get
            {
                if (_onode == null)
                {
                    if (IsOASISBooted)
                        _onode = new ONODEManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the ONODE property!");
                }

                return _onode;
            }
        }

        public ONETManager ONET
        {
            get
            {
                if (_onet == null)
                {
                    if (IsOASISBooted)
                        _onet = new ONETManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the ONET property!");
                }

                return _onet;
            }
        }

        public SettingsManager Settings
        {
            get
            {
                if (_settings == null)
                {
                    if (IsOASISBooted)
                        //_settings = new SettingsManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                        _settings = SettingsManager.Instance;
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Settings property!");
                }

                return _settings;
            }
        }

        public KarmaManager Karma
        {
            get
            {
                if (_karma == null)
                {
                    if (IsOASISBooted)
                        _karma = new KarmaManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Karma property!");
                }

                return _karma;
            }
        }

        public MapManager Map
        {
            get
            {
                if (_map == null)
                {
                    if (IsOASISBooted)
                        _map = new MapManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Map property!");
                }

                return _map;
            }
        }

        public ChatManager Chat
        {
            get
            {
                if (_chat == null)
                {
                    if (IsOASISBooted)
                        _chat = new ChatManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Chat property!");
                }

                return _chat;
            }
        }

        public MessagingManager Messaging
        {
            get
            {
                if (_messaging == null)
                {
                    if (IsOASISBooted)
                        _messaging = new MessagingManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Messaging property!");
                }

                return _messaging;
            }
        }

        public CompetitionManager Competition
        {
            get
            {
                if (_competition == null)
                {
                    if (IsOASISBooted)
                        _competition = new CompetitionManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Competition property!");
                }

                return _competition;
            }
        }

        public GiftsManager Gifts
        {
            get
            {
                if (_gifts == null)
                {
                    if (IsOASISBooted)
                        _gifts = new GiftsManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Gifts property!");
                }

                return _gifts;
            }
        }

        public FilesManager Files
        {
            get
            {
                if (_files == null)
                {
                    if (IsOASISBooted)
                        _files = new FilesManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Files property!");
                }

                return _files;
            }
        }

        public SocialManager Social
        {
            get
            {
                if (_social == null)
                {
                    if (IsOASISBooted)
                        _social = new SocialManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Social property!");
                }

                return _social;
            }
        }

        public VideoManager Video
        {
            get
            {
                if (_video == null)
                {
                    if (IsOASISBooted)
                        _video = new VideoManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Video property!");
                }

                return _video;
            }
        }

        //public ShareManager Share
        //{
        //    get
        //    {
        //        if (_share == null)
        //        {
        //            if (IsOASISBooted)
        //                _share = new ShareManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
        //            else
        //                throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Share property!");
        //        }

        //        return _share;
        //    }
        //}

        public SeedsManager Seeds
        {
            get
            {
                if (_seeds == null)
                {
                    if (IsOASISBooted)
                        _seeds = new SeedsManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.Id, OASISBootLoader.OASISBootLoader.OASISDNA);
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Seeds property!");
                }

                return _seeds;
            }
        }

        //public TelosManager Telos
        //{
        //    get
        //    {
        //        if (_telos == null)
        //        {
        //            if (IsOASISBooted)
        //                _telos = new TelosManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
        //            else
        //                throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Telos property!");
        //        }

        //        return _telos;
        //    }
        //}

        public StatsManager Stats
        {
            get
            {
                if (_stats == null)
                {
                    if (IsOASISBooted)
                        //_stats = new StatsManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
                        _stats = StatsManager.Instance;
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Stats property!");
                }

                return _stats;
            }
        }

        public ProviderManager Provider
        {
            get
            {
                if (_provider == null)
                {
                    if (IsOASISBooted)
                        _provider = ProviderManager.Instance;
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the Provider property!");
                }

                return _provider;
            }
        }

        public COSMICManager COSMIC
        {
            get
            {
                if (_cosmic == null)
                {
                    if (IsOASISBooted)
                    {
                        if (AvatarManager.LoggedInAvatar != null && AvatarManager.LoggedInAvatar.Id != Guid.Empty)
                            _cosmic = new COSMICManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.Id, OASISBootLoader.OASISBootLoader.OASISDNA);
                        else
                            _cosmic = new COSMICManager(ProviderManager.Instance.CurrentStorageProvider, Guid.NewGuid(), OASISBootLoader.OASISBootLoader.OASISDNA);
                    }
                    else
                        throw new OASISException("OASIS is not booted. Please boot the OASIS before accessing the COSMIC property!");
                }

                return _cosmic;
            }
        }

        public OASISResult<bool> BootOASIS(OASISDNA OASISDNA, string userName = "", string password = "", bool startApolloServer = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            if (!OASISBootLoader.OASISBootLoader.IsOASISBooted)
                result = OASISBootLoader.OASISBootLoader.BootOASIS(OASISDNA);

            if (!result.IsError && result.Result)
                InitOASIS(userName, password, startApolloServer);

            return result;
        }

        public async Task<OASISResult<bool>> BootOASISAsync(OASISDNA OASISDNA, string userName = "", string password = "", bool startApolloServer = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            if (!OASISBootLoader.OASISBootLoader.IsOASISBooted)
                result = await OASISBootLoader.OASISBootLoader.BootOASISAsync(OASISDNA);

            if (!result.IsError && result.Result)
                InitOASIS(userName, password, startApolloServer);

            return result;
        }

        public OASISResult<bool> BootOASIS(string userName = "", string password = "", string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            if (!OASISBootLoader.OASISBootLoader.IsOASISBooted)
                result = OASISBootLoader.OASISBootLoader.BootOASIS(OASISDNAPath);

            if (!result.IsError && result.Result)
                InitOASIS(userName, password, startApolloServer);

            return result;
        }

        public async Task<OASISResult<bool>> BootOASISAsync(string userName = "", string password = "", string OASISDNAPath = "OASIS_DNA.json", bool startApolloServer = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            if (!OASISBootLoader.OASISBootLoader.IsOASISBooted)
                result = await OASISBootLoader.OASISBootLoader.BootOASISAsync(OASISDNAPath);

            if (!result.IsError && result.Result)
                InitOASIS(userName, password, startApolloServer);

            return result;
        }

        public static OASISResult<bool> ShutdownOASIS()
        {
            return OASISBootLoader.OASISBootLoader.ShutdownOASIS();
        }

        public static async Task<OASISResult<bool>> ShutdownOASISAsync()
        {
            return await OASISBootLoader.OASISBootLoader.ShutdownOASISAsync();
        }

        public OASISResult<IAvatar> LogAvatarIntoOASISManagers(string userName, string password)
        {
            string hostName = Dns.GetHostName();
            string IPAddress = Dns.GetHostEntry(hostName).AddressList[0].ToString();
            OASISResult<IAvatar> result = Avatars.Authenticate(userName, password, IPAddress);

            //if (result != null && result.Result != null && !result.IsError)
            //    InitManagers();

            return result;
        }

        public async Task<OASISResult<IAvatar>> LogAvatarIntoOASISManagersAsync(string userName, string password)
        {
            string hostName = Dns.GetHostName();
            string IPAddress = Dns.GetHostEntry(hostName).AddressList[0].ToString();
            OASISResult<IAvatar> result = await Avatars.AuthenticateAsync(userName, password, IPAddress);

            //if (result != null && result.Result != null && !result.IsError)
            //    InitManagers();

            return result;
        }

        //private void InitManagers()
        //{
        //    //These are the OASIS.API.ONODE.Core Managers.
        //    //NFTs = new NFTManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //GeoHotSpots = new GeoHotSpotManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //Map = new MapManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //Chapters = new ChapterManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //Missions = new MissionManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //Quests = new QuestManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, NFTs, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //Parks = new ParkManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //OLAND = new OLandManager(NFTs, ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //OAPPs = new OAPPManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //OAPPTemplates = new OAPPTemplateManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //Inventory = new InventoryItemManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //    //Runtimes = new RuntimeManager(ProviderManager.Instance.CurrentStorageProvider, AvatarManager.LoggedInAvatar.AvatarId, OASISBootLoader.OASISBootLoader.OASISDNA);
        //}

        private void InitOASIS(string userName = "", string password = "", bool startApolloServer = true)
        {
            // Set OASIS DNA and boot status
            OASISDNA = OASISBootLoader.OASISBootLoader.OASISDNA;
            IsOASISBooted = true;

            // All managers are now lazily initialized when accessed
            // This provides better performance and memory usage

            // Avatar authentication is not required for STAR ignition
            // OASIS is now booted and ready to use

            //if (startApolloServer)
            //    ApolloServer.StartServer();

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
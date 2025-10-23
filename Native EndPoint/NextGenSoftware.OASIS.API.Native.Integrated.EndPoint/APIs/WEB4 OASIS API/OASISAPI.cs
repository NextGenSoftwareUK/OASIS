using System.Net;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

namespace NextGenSoftware.OASIS.API.Native.EndPoint
{
    public class OASISAPI
    {
        private AvatarManager _avatar = null;
        private NFTManager _nfts = null;
        private OLandManager _olands = null;

        public bool IsOASISBooted { get; set; }
        //public string OASISRunVersion { get; set; }
        public OASISDNA OASISDNA { get; set; } 

        //TODO: Convert below to work how Avatars does.
        public HolonManager Data { get; set; } //TODO: FIX TOMORROW!
        //public HolonManager HolonicGraph { get; set; }
        public KeyManager Keys { get; set; }  //TODO: FIX TOMORROW!
        public WalletManager Wallets { get; set; }  //TODO: FIX TOMORROW!
        public OASISProviders Providers { get; private set; }  //TODO: FIX TOMORROW!
        public SearchManager Search { get; set; }  //TODO: FIX TOMORROW!

        

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

        //Add any other missing Managers here that are used in the WEB OASIS API Web API...

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
            //OASISVersion = OASISBootLoader.OASISBootLoader.OASISVersion;
            OASISDNA = OASISBootLoader.OASISBootLoader.OASISDNA;
            //Avatar = new AvatarManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
            Data = new HolonManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
            Keys = new KeyManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
            Wallets = new WalletManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
            Search = new SearchManager(ProviderManager.Instance.CurrentStorageProvider, OASISBootLoader.OASISBootLoader.OASISDNA);
            Providers = new OASISProviders(OASISBootLoader.OASISBootLoader.OASISDNA);

            // Set IsOASISBooted to true before accessing Avatars property
            IsOASISBooted = true;

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
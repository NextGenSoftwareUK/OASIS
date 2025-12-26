using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.Enums;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.EventArgs;
using NextGenSoftware.OASIS.STAR.ErrorEventArgs;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

using NextGenSoftware.OASIS.STAR.Interfaces;
using SevenZip.Buffer;
using System.Linq;
using System.Text.Json;


namespace NextGenSoftware.OASIS.STAR
{
    public static class STAR
    {
        const string STAR_DNA_DEFAULT_PATH = "DNA\\STAR_DNA.json";
        const string OASIS_DNA_DEFAULT_PATH = "DNA\\OASIS_DNA.json";

        private static StarStatus _status;
        private static Guid _starId = Guid.Empty;
        private static OASISAPI _OASISAPI = null;
        private static STARAPI _STARAPI = null;
        private static IPlanet _defaultPlanet = null;
        private static ISuperStar _defaultSuperStar = null;
        private static IGrandSuperStar _defaultGrandSuperStar = null;
        private static IGreatGrandSuperStar _defaultGreatGrandSuperStar = null;

        public static string STARDNAPath { get; set; } = STAR_DNA_DEFAULT_PATH;
        public static string OASISDNAPath { get; set; } = OASIS_DNA_DEFAULT_PATH;

        public static STARDNA STARDNA
        {
            get
            {
                return STARDNAManager.STARDNA;
            }
            set
            {
                STARDNAManager.STARDNA = value;
            }
        }

        public static OASISDNA OASISDNA
        {
            get
            {
                return OASISBootLoader.OASISBootLoader.OASISDNA;
            }
        }

        public static StarStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { Status = value });
            }
        }

        public static bool IsStarIgnited { get; private set; }
        public static bool IsDetailedCOSMICOutputsEnabled { get; set; } = false;
        public static bool IsDetailedStatusUpdatesEnabled { get; set; }

        //public static GreatGrandSuperStar InnerStar { get; set; } //Only ONE of these can ever exist and is at the centre of the Omniverse (also only ONE).

        //Will default to the GreatGrandSuperStar at the centre of our Omniverse.
        public static IGreatGrandSuperStar DefaultGreatGrandSuperStar
        {
            get
            {
                //return _defaultGreatGrandSuperStar;

                if (_defaultGreatGrandSuperStar == null)
                {
                    if (STARDNA != null && !string.IsNullOrEmpty(STARDNA.DefaultGreatGrandSuperStarId) && Guid.TryParse(STARDNA.DefaultGreatGrandSuperStarId, out _))
                        _defaultGreatGrandSuperStar = new GreatGrandSuperStar(new Guid(STARDNA.DefaultGreatGrandSuperStarId));
                }

                return _defaultGreatGrandSuperStar;
            }
            set
            {
                _defaultGreatGrandSuperStar = value;

                //if (_defaultGreatGrandSuperStar == null)
                //{
                //    if (STARDNA != null && !string.IsNullOrEmpty(STARDNA.DefaultGreatGrandSuperStarId) && Guid.TryParse(STARDNA.DefaultGreatGrandSuperStarId, out _))
                //        _defaultGreatGrandSuperStar = new GreatGrandSuperStar(new Guid(STARDNA.DefaultGreatGrandSuperStarId));
                //}
            }
        }

        //public static IGreatGrandSuperStar DefaultGreatGrandSuperStar { get; set; } //Will default to the GreatGrandSuperStar at the centre of our Omniverse.

        //Will default to the GrandSuperStar at the centre of our Universe.
        public static IGrandSuperStar DefaultGrandSuperStar
        {
            get
            {
                if (_defaultGrandSuperStar == null)
                {
                    if (STARDNA != null && !string.IsNullOrEmpty(STARDNA.DefaultGrandSuperStarId) && Guid.TryParse(STARDNA.DefaultGrandSuperStarId, out _))
                        _defaultGrandSuperStar = new GrandSuperStar(new Guid(STARDNA.DefaultGrandSuperStarId));
                }

                return _defaultGrandSuperStar;
            }
            set
            {
                _defaultGrandSuperStar = value;
            }
        }

        //public static IGrandSuperStar DefaultGrandSuperStar { get; set; } //Will default to the GrandSuperStar at the centre of our Universe.

        //Will default to the SuperStar at the centre of our Galaxy.
        public static ISuperStar DefaultSuperStar
        {
            get
            {
                if (_defaultSuperStar == null)
                {
                    if (STARDNA != null && !string.IsNullOrEmpty(STARDNA.DefaultSuperStarId) && Guid.TryParse(STARDNA.DefaultSuperStarId, out _))
                        _defaultSuperStar = new SuperStar(new Guid(STARDNA.DefaultSuperStarId));
                }

                return _defaultSuperStar;
            }
            set
            {
                _defaultSuperStar = value;
            }
        }

        //public static ISuperStar DefaultSuperStar { get; set; } 

        public static IStar DefaultStar { get; set; } //Will default to our Sun.

        //Will default to Our World.
        public static IPlanet DefaultPlanet
        {
            get
            {
                if (_defaultPlanet == null)
                {
                    if (STARDNA != null && !string.IsNullOrEmpty(STARDNA.DefaultPlanetId) && Guid.TryParse(STARDNA.DefaultPlanetId, out _))
                        _defaultPlanet = new Planet(new Guid(STARDNA.DefaultPlanetId));
                }

                return _defaultPlanet;
            }
            set
            {
                _defaultPlanet = value;
            }
        }
        // public static CelestialBodies.Star InnerStar { get; set; }
        //public static SuperStarCore SuperStarCore { get; set; }
        //public static List<CelestialBodies.Star> Stars { get; set; } = new List<CelestialBodies.Star>();
        //public static List<IPlanet> Planets
        //{
        //    get
        //    {
        //        return InnerStar.Planets;
        //    }
        //}

        public static IAvatar BeamedInAvatar { get; set; }
        public static IAvatarDetail BeamedInAvatarDetail { get; set; }

        //public static OASISAPI OASISAPI
        //{
        //    get
        //    {
        //        if (_OASISAPI == null)
        //            _OASISAPI = new OASISAPI();

        //        return _OASISAPI;
        //    }
        //}

        public static OASISAPI OASISAPI
        {
            get
            {
                if (_OASISAPI == null)
                    _OASISAPI = new OASISAPI();

                return _OASISAPI;
            }
        }

        public static STARAPI STARAPI
        {
            get
            {
                if (_STARAPI == null)
                    _STARAPI = new STARAPI(STARDNA, OASISAPI);

                return _STARAPI;
            }
        }

        //public static IMapper Mapper { get; set; }

        //public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        //public static event HolonsLoaded OnHolonsLoaded;

        //public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        //public static event ZomesLoaded OnZomesLoaded;

        //public delegate void HolonSaved(object sender, HolonSavedEventArgs e);
        //public static event HolonSaved OnHolonSaved;

        //public delegate void HolonLoaded(object sender, HolonLoadedEventArgs e);
        //public static event HolonLoaded OnHolonLoaded;

        //public delegate void ZomeError(object sender, ZomeErrorEventArgs e);
        //public static event ZomeError OnZomeError;

        public static event CelestialSpaceLoaded OnCelestialSpaceLoaded;
        public static event CelestialSpaceSaved OnCelestialSpaceSaved;
        public static event CelestialSpaceError OnCelestialSpaceError;
        public static event CelestialSpacesLoaded OnCelestialSpacesLoaded;
        public static event CelestialSpacesSaved OnCelestialSpacesSaved;
        public static event CelestialSpacesError OnCelestialSpacesError;
        public static event CelestialBodyLoaded OnCelestialBodyLoaded;
        public static event CelestialBodySaved OnCelestialBodySaved;
        public static event CelestialBodyError OnCelestialBodyError;
        public static event CelestialBodiesLoaded OnCelestialBodiesLoaded;
        public static event CelestialBodiesSaved OnCelestialBodiesSaved;
        public static event CelestialBodiesError OnCelestialBodiesError;
        public static event ZomeLoaded OnZomeLoaded;
        public static event ZomeSaved OnZomeSaved;
        public static event ZomeError OnZomeError;
        public static event ZomesLoaded OnZomesLoaded;
        public static event ZomesSaved OnZomesSaved;
        public static event ZomesError OnZomesError;
        public static event HolonLoaded OnHolonLoaded;
        public static event HolonSaved OnHolonSaved;
        public static event HolonError OnHolonError;
        public static event HolonsLoaded OnHolonsLoaded;
        public static event HolonsSaved OnHolonsSaved;
        public static event HolonsError OnHolonsError;

        public delegate void DefaultCeletialBodyInit(object sender, DefaultCelestialBodyInitEventArgs e);
        public static event DefaultCeletialBodyInit OnDefaultCeletialBodyInit;

        public delegate void StarIgnited(object sender, StarIgnitedEventArgs e);
        public static event StarIgnited OnStarIgnited;

        public delegate void StarCoreIgnited(object sender, System.EventArgs e);
        public static event StarCoreIgnited OnStarCoreIgnited;

        public delegate void StarStatusChanged(object sender, StarStatusChangedEventArgs e);
        public static event StarStatusChanged OnStarStatusChanged;

        public delegate void StarError(object sender, StarErrorEventArgs e);
        public static event StarError OnStarError;

        public delegate void OASISBooted(object sender, OASISBootedEventArgs e);
        public static event OASISBooted OnOASISBooted;

        public delegate void OASISBootError(object sender, OASISBootErrorEventArgs e);
        public static event OASISBootError OnOASISBootError;

        //TODO: Not sure if we want to expose the HoloNETClient events at this level? They can subscribe to them through the HoloNETClient property below...
        //public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        //public static event Disconnected OnDisconnected;

        //public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        //public static event DataReceived OnDataReceived;

        public static async Task<OASISResult<IOmiverse>> IgniteStarAsync(string userName = "", string password = "", string STARDNAPath = STAR_DNA_DEFAULT_PATH, string OASISDNAPath = OASIS_DNA_DEFAULT_PATH, string starId = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();
            Status = StarStatus.Igniting;

            // If you wish to change the logging framework from the default (NLog) then set it below (or just change in OASIS_DNA - prefered way)
            //LoggingManager.CurrentLoggingFramework = LoggingFramework.NLog;

            /*
            var config = new MapperConfiguration(cfg => {
                //cfg.AddProfile<AppProfile>();
                cfg.CreateMap<IHolon, CelestialBody>();
                cfg.CreateMap<IHolon, Zome>();
            });

            Mapper = config.CreateMapper();
            */

            if (File.Exists(STARDNAPath))
                await STARDNAManager.LoadDNAAsync(STARDNAPath);
            else
            {
                STARDNA = new STARDNA();
                await STARDNAManager.SaveDNAAsync(STARDNAPath, STARDNA);
            }

            ValidateSTARDNA(STARDNA);
            Status = StarStatus.BootingOASIS;
            OASISResult<bool> oasisResult = await BootOASISAsync(userName, password, OASISDNAPath);

            if (oasisResult.IsError)
            {
                string errorMessage = string.Concat("Error whilst booting OASIS. Reason: ", oasisResult.Message);
                OnOASISBootError?.Invoke(null, new OASISBootErrorEventArgs() { ErrorReason = errorMessage });
                OnStarError?.Invoke(null, new StarErrorEventArgs() { Reason = errorMessage });
                result.IsError = true;
                result.Message = errorMessage;
                return result;
            }
            else
                OnOASISBooted?.Invoke(null, new OASISBootedEventArgs() { Message = result.Message });

            Status = StarStatus.OASISBooted;

            // If the starId is passed in and is valid then convert to Guid, otherwise get it from the STARDNA file.
            if (!string.IsNullOrEmpty(starId) && !string.IsNullOrWhiteSpace(starId))
            {
                if (!Guid.TryParse(starId, out _starId))
                {
                    //TODO: Need to apply this error handling across the entire OASIS eventually...
                    HandleErrorMessage(ref result, "StarID passed in is invalid. It needs to be a valid Guid.");
                    return result;
                }
            }
            else if (!string.IsNullOrEmpty(STARDNA.DefaultStarId) && !string.IsNullOrWhiteSpace(STARDNA.DefaultStarId) && !Guid.TryParse(STARDNA.DefaultStarId, out _starId))
            {
                HandleErrorMessage(ref result, "StarID defined in the STARDNA file in is invalid. It needs to be a valid Guid.");
                return result;
            }

            result = await IgniteInnerStarAsync(result);

            if (result.IsError)
                Status = StarStatus.Error;
            else
            {
                Status = StarStatus.Ignited;
                OnStarIgnited.Invoke(null, new StarIgnitedEventArgs() { Message = result.Message });
                IsStarIgnited = true;
            }

            return result;
        }

        public static OASISResult<IOmiverse> IgniteStar(string userName = "", string password = "", string STARDNAPath = STAR_DNA_DEFAULT_PATH, string OASISDNAPath = OASIS_DNA_DEFAULT_PATH, string starId = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();
            Status = StarStatus.Igniting;

            // If you wish to change the logging framework from the default (NLog) then set it below (or just change in OASIS_DNA - prefered way)
            //LoggingManager.CurrentLoggingFramework = LoggingFramework.NLog;

            /*
            var config = new MapperConfiguration(cfg => {
                //cfg.AddProfile<AppProfile>();
                cfg.CreateMap<IHolon, CelestialBody>();
                cfg.CreateMap<IHolon, Zome>();
            });

            Mapper = config.CreateMapper();
            */

            if (File.Exists(STARDNAPath))
                STARDNAManager.LoadDNA(STARDNAPath);
            else
            {
                STARDNA = new STARDNA();
                STARDNAManager.SaveDNA(STARDNAPath, STARDNA);
            }

            ValidateSTARDNA(STARDNA);

            IsDetailedCOSMICOutputsEnabled = STARDNA.DetailedCOSMICOutputEnabled;
            IsDetailedStatusUpdatesEnabled = STARDNA.DetailedSTARStatusOutputEnabled;

            Status = StarStatus.BootingOASIS;
            OASISResult<bool> oasisResult = BootOASIS(userName, password, OASISDNAPath);

            if (oasisResult.IsError)
            {
                string errorMessage = string.Concat("Error whilst booting OASIS. Reason: ", oasisResult.Message);
                OnOASISBootError?.Invoke(null, new OASISBootErrorEventArgs() { ErrorReason = errorMessage });
                OnStarError?.Invoke(null, new StarErrorEventArgs() { Reason = errorMessage });
                result.IsError = true;
                result.Message = errorMessage;
                return result;
            }
            else
                OnOASISBooted?.Invoke(null, new OASISBootedEventArgs() { Message = result.Message });

            Status = StarStatus.OASISBooted;
            BeamedInAvatar = AvatarManager.LoggedInAvatar;

            // If the starId is passed in and is valid then convert to Guid, otherwise get it from the STARDNA file.
            if (!string.IsNullOrEmpty(starId) && !string.IsNullOrWhiteSpace(starId))
            {
                if (!Guid.TryParse(starId, out _starId))
                {
                    //TODO: Need to apply this error handling across the entire OASIS eventually...
                    HandleErrorMessage(ref result, "StarID passed in is invalid. It needs to be a valid Guid.");
                    return result;
                }
            }
            else if (!string.IsNullOrEmpty(STARDNA.DefaultStarId) && !string.IsNullOrWhiteSpace(STARDNA.DefaultStarId) && !Guid.TryParse(STARDNA.DefaultStarId, out _starId))
            {
                HandleErrorMessage(ref result, "StarID defined in the STARDNA file in is invalid. It needs to be a valid Guid.");
                return result;
            }

            result = IgniteInnerStar(result);

            if (result.IsError)
                Status = StarStatus.Error;
            else
            {
                Status = StarStatus.Ignited;
                OnStarIgnited?.Invoke(null, new StarIgnitedEventArgs() { Message = result.Message });
                IsStarIgnited = true;
            }

            return result;
        }

        public static OASISResult<bool> ExtinguishStar()
        {
            return OASISAPI.ShutdownOASIS();
        }

        public static async Task<OASISResult<bool>> ExtinguishStarAsync()
        {
            return await OASISAPI.ShutdownOASISAsync();
        }

        private static void WireUpEvents()
        {
            if (DefaultStar != null)
            {
                DefaultStar.OnHolonLoaded += InnerStar_OnHolonLoaded;
                DefaultStar.OnHolonSaved += InnerStar_OnHolonSaved;
                DefaultStar.OnHolonsLoaded += InnerStar_OnHolonsLoaded;
                DefaultStar.OnZomeError += InnerStar_OnZomeError;
                DefaultStar.OnInitialized += InnerStar_OnInitialized;
            }
        }

        private static void InnerStar_OnInitialized(object sender, System.EventArgs e)
        {
            OnStarCoreIgnited?.Invoke(sender, e);
        }

        private static void InnerStar_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private static void InnerStar_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            OnHolonLoaded?.Invoke(sender, e);
        }

        private static void InnerStar_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);
        }

        private static void InnerStar_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            OnHolonsLoaded?.Invoke(sender, e);
        }

        public static OASISResult<IAvatar> CreateAvatar(string title, string firstName, string lastName, string email, string username, string password, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green, ProviderType providerType = ProviderType.Default)
        {
            if (!IsStarIgnited)
                IgniteStar();

            return OASISAPI.Avatars.Register(title, firstName, lastName, email, password, username, AvatarType.User, OASISType.STARCLI, cliColour, favColour);
        }

        public static async Task<OASISResult<IAvatar>> CreateAvatarAsync(string title, string firstName, string lastName, string email, string username, string password, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green, ProviderType providerType = ProviderType.Default)
        {
            if (!IsStarIgnited)
                await IgniteStarAsync();

            return await OASISAPI.Avatars.RegisterAsync(title, firstName, lastName, email, password, username, AvatarType.User, OASISType.STARCLI, cliColour, favColour);
        }

        public static async Task<OASISResult<IAvatar>> BeamInAsync(string username, string password, ProviderType providerType = ProviderType.Default)
        {
            string hostName = Dns.GetHostName();
            string IPAddress = Dns.GetHostEntry(hostName).AddressList[0].ToString();

            if (!IsStarIgnited)
                await IgniteStarAsync();

            OASISResult<IAvatar> result = await OASISAPI.Avatars.AuthenticateAsync(username, password, IPAddress);

            if (!result.IsError)
            {
                BeamedInAvatar = (Avatar)result.Result;
                //OASISAPI.LogAvatarIntoOASISManagers(); //TODO: Is there a better way of doing this?

                //BeamedInAvatarDetail = new AvatarDetail()
                //{
                //    Karma = 777
                //};

                //TODO: Fix later! Gifts property de-serialiazed issue in MongoDBOASIS
                OASISResult<IAvatarDetail> loggedInAvatarDetailResult = await OASISAPI.Avatars.LoadAvatarDetailAsync(BeamedInAvatar.Id);

                if (!loggedInAvatarDetailResult.IsError && loggedInAvatarDetailResult.Result != null)
                    BeamedInAvatarDetail = loggedInAvatarDetailResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error Occured In BeamInAsync Calling LoadAvatarDetailAsync. Reason: {loggedInAvatarDetailResult.Message}");

                //TODO: NEED TO FIX LATER!
                //await STARAPI.BootSTARAPIAsync(username, password);
                //await STARAPI.InitManagers(username, password);
            }

            return result;
        }

        public static OASISResult<IAvatar> BeamIn(string username, string password, ProviderType providerType = ProviderType.Default)
        {
            string IPAddress = "";
            string hostName = Dns.GetHostName();
            IPHostEntry entry = Dns.GetHostEntry(hostName);

            if (entry != null && entry.AddressList.Length > 1)
                IPAddress = Dns.GetHostEntry(hostName).AddressList[1].ToString();

            if (!IsStarIgnited)
                IgniteStar();

            OASISResult<IAvatar> result = OASISAPI.Avatars.Authenticate(username, password, IPAddress);

            if (!result.IsError)
            {
                BeamedInAvatar = (Avatar)result.Result;

                OASISResult<IAvatarDetail> loggedInAvatarDetailResult = OASISAPI.Avatars.LoadAvatarDetail(BeamedInAvatar.Id);

                if (!loggedInAvatarDetailResult.IsError && loggedInAvatarDetailResult.Result != null)
                    BeamedInAvatarDetail = loggedInAvatarDetailResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error Occured In BeamIn Calling LoadAvatarDetail. Reason: {loggedInAvatarDetailResult.Message}");
            }
            return result;
        }

        public static OASISResult<CoronalEjection> Light(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, ProviderType providerType = ProviderType.Default)
        {
            return Light(OAPPName, OAPPDescription, OAPPType, OAPPTemplateId, OAPPTemplateVersion, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, metaHolonTagMappings, metaTagMappings, null, providerType);
        }

        //public static OASISResult<CoronalEjection> Light(string OAPPName, string OAPPDescription, OAPPType OAPPType, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", IStar starToAddPlanetTo = null, ProviderType providerType = ProviderType.Default)
        //{
        //    return Light(OAPPName, OAPPDescription, OAPPType, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, (ICelestialBody)starToAddPlanetTo, providerType);
        //}

        //public static OASISResult<CoronalEjection> Light(string OAPPName, string OAPPDescription, OAPPType OAPPType, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", IPlanet planetToAddMoonTo = null, ProviderType providerType = ProviderType.Default)
        //{
        //    return Light(OAPPName, OAPPDescription, OAPPType, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, (ICelestialBody)planetToAddMoonTo, providerType);
        //}

        public static OASISResult<CoronalEjection> Light(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, ICelestialBody celestialBodyParent = null, ProviderType providerType = ProviderType.Default)
        {
            return LightAsync(OAPPName, OAPPDescription, OAPPType, OAPPTemplateId, OAPPTemplateVersion, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, metaHolonTagMappings, metaTagMappings, celestialBodyParent, providerType).Result;
        }

        public static OASISResult<CoronalEjection> Light(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, Guid celestialBodyParentId = new Guid(), ProviderType providerType = ProviderType.Default)
        {
            return LightInternalAsync(OAPPName, OAPPDescription, OAPPType, OAPPTemplateId, OAPPTemplateVersion, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, metaHolonTagMappings, metaTagMappings, null, celestialBodyParentId, providerType).Result;
        }

        public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, ProviderType providerType = ProviderType.Default)
        {
            return await LightAsync(OAPPName, OAPPDescription, OAPPType, OAPPTemplateId, OAPPTemplateVersion, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, metaHolonTagMappings, metaTagMappings, (ICelestialBody)null, providerType);
        }

        //public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", IStar starToAddPlanetTo = null, ProviderType providerType = ProviderType.Default)
        //{
        //    return await LightAsync(OAPPName, OAPPDescription, OAPPType, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, (ICelestialBody)starToAddPlanetTo, providerType);
        //}

        //public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, GenesisType genesisType,  string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", IPlanet planetToAddMoonTo = null, ProviderType providerType = ProviderType.Default)
        //{
        //    return await LightAsync(OAPPName, OAPPDescription, OAPPType, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, (ICelestialBody)planetToAddMoonTo, providerType);
        //}

        public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, string zomeAndHolonDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, ProviderType providerType = ProviderType.Default)
        {
            return await LightAsync(OAPPName, OAPPDescription, OAPPType, OAPPTemplateId, OAPPTemplateVersion, GenesisType.ZomesAndHolonsOnly, zomeAndHolonDNAFolder, genesisFolder, genesisNameSpace, metaHolonTagMappings, metaTagMappings, providerType);
        }

        public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, Guid celestialBodyParentId = new Guid(), ProviderType providerType = ProviderType.Default)
        {
            return await LightInternalAsync(OAPPName, OAPPDescription, OAPPType, OAPPTemplateId, OAPPTemplateVersion, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, metaHolonTagMappings, metaTagMappings, null, celestialBodyParentId, providerType);
        }

        //public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, OAPPTemplateType OAPPTemplateType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", ICelestialBody celestialBodyParent = null, ProviderType providerType = ProviderType.Default)
        public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, ICelestialBody celestialBodyParent = null, ProviderType providerType = ProviderType.Default)
        {
            return await LightInternalAsync(OAPPName, OAPPDescription, OAPPType, OAPPTemplateId, OAPPTemplateVersion, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, metaHolonTagMappings, metaTagMappings, celestialBodyParent, Guid.Empty, providerType);
        }

        //private static async Task<OASISResult<CoronalEjection>> LightInternalAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, OAPPTemplateType OAPPTemplateType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "",  string genesisNameSpace = "", ICelestialBody celestialBodyParent = null, Guid celestialBodyParentId = new Guid(), ProviderType providerType = ProviderType.Default)
        private static async Task<OASISResult<CoronalEjection>> LightInternalAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, ICelestialBody celestialBodyParent = null, Guid celestialBodyParentId = new Guid(), ProviderType providerType = ProviderType.Default)
        {
            OASISResult<CoronalEjection> result = new OASISResult<CoronalEjection>(new CoronalEjection());
            ICelestialBody newBody = null;
            bool holonReached = false;
            string zomeBufferCsharp = "";
            string izomeBufferCsharp = "";
            string holonBufferCsharp = "";
            string iholonBufferCsharp = "";
            string holonName = "";
            string zomeName = "";
            bool firstField = true;
            bool secondField = false;
            string celestialBodyBufferCsharp = "";
            bool firstHolon = true;
            string OAPPFolder = "";
            List<string> holonNames = new List<string>();
            string firstStringProperty = "";
            string errorMessage = "Error Occured In STAR.LightInternalAsync. Reason:";

            if (BeamedInAvatarDetail == null)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar is not logged in. Please log in before calling this command.");
                return result;
            }

            if (BeamedInAvatarDetail.Level < 77 && genesisType == GenesisType.Star)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar must have reached level 77 before they can create stars. Please create a planet or moon instead...");
                return result;
            }

            if (BeamedInAvatarDetail.Level < 33 && genesisType == GenesisType.Planet)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar must have reached level 33 before they can create planets. Please create a moon instead...");
                return result;
            }

            if (!IsStarIgnited)
                await IgniteStarAsync();

            //If folder is not passed in via command line args then use default in config file.
            if (string.IsNullOrEmpty(celestialBodyDNAFolder))
            {
                if (Path.IsPathRooted(STARDNA.OAPPMetaDataDNAFolder))
                    celestialBodyDNAFolder = Path.Combine(STARDNA.OAPPMetaDataDNAFolder, OAPPName, "CelestialBodyDNA");
                else
                    celestialBodyDNAFolder = Path.Combine(STARDNA.BaseSTARPath, STARDNA.OAPPMetaDataDNAFolder, OAPPName, "CelestialBodyDNA");
            }

            if (string.IsNullOrEmpty(genesisFolder))
                genesisFolder = STARDNA.DefaultOAPPsSourcePath;
                //genesisFolder = STARDNA.GenesisFolder;

            if (string.IsNullOrEmpty(genesisNameSpace))
                genesisNameSpace = STARDNA.DefaultGenesisNamespace;

            if (DefaultStar == null)
            {
                OASISResult<IOmiverse> igniteResult = new OASISResult<IOmiverse>();
                igniteResult = await IgniteInnerStarAsync(igniteResult);

                if (result.IsError)
                    return new OASISResult<CoronalEjection>() { IsError = true, Message = string.Concat("Error Igniting Inner Star. Reason: ", result.Message) };
            }

            ValidateLightDNA(celestialBodyDNAFolder, genesisFolder);

            string iHolonTemplate = new FileInfo(string.Concat(STARDNA.BaseSTARPath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateIHolonDNA)).OpenText().ReadToEnd();
            string holonTemplateCsharp = new FileInfo(string.Concat(STARDNA.BaseSTARPath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateHolonDNA)).OpenText().ReadToEnd();
            string iZomeTemplate = new FileInfo(string.Concat(STARDNA.BaseSTARPath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateIZomeDNA)).OpenText().ReadToEnd();
            string zomeTemplateCsharp = new FileInfo(string.Concat(STARDNA.BaseSTARPath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateZomeDNA)).OpenText().ReadToEnd();
            string iCelestialBodyTemplateCsharp = new FileInfo(string.Concat(STARDNA.BaseSTARPath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateICelestialBodyDNA)).OpenText().ReadToEnd();
            string celestialBodyTemplateCsharp = new FileInfo(string.Concat(STARDNA.BaseSTARPath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateCelestialBodyDNA)).OpenText().ReadToEnd();
            string loadHolonTemplateCsharp = new FileInfo(string.Concat(STARDNA.BaseSTARPath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateLoadHolonDNA)).OpenText().ReadToEnd();
            string saveHolonTemplateCsharp = new FileInfo(string.Concat(STARDNA.BaseSTARPath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateSaveHolonDNA)).OpenText().ReadToEnd();
            string iloadHolonTemplateCsharp = new FileInfo(string.Concat(STARDNA.BaseSTARPath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateILoadHolonDNA)).OpenText().ReadToEnd();
            string isaveHolonTemplateCsharp = new FileInfo(string.Concat(STARDNA.BaseSTARPath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateISaveHolonDNA)).OpenText().ReadToEnd();

            string IntTemplateCsharp = new FileInfo(string.Concat(STARDNA.BaseSTARPath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateInt)).OpenText().ReadToEnd();
            string StringTemplateCSharp = new FileInfo(string.Concat(STARDNA.BaseSTARPath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateString)).OpenText().ReadToEnd();
            string BoolTemplateCsharp = new FileInfo(string.Concat(STARDNA.BaseSTARPath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateBool)).OpenText().ReadToEnd();

            
            if (string.IsNullOrEmpty(genesisFolder))
                genesisFolder = $"{STARDNA.BaseSTARNETPath}\\{STARDNA.DefaultOAPPsSourcePath}";
                //genesisFolder = $"{STARDNA.BaseSTARPath}\\{STARDNA.GenesisFolder}";

            if (string.IsNullOrEmpty(genesisNameSpace))
                genesisNameSpace = $"{STARDNA.DefaultGenesisNamespace}";
                //genesisNameSpace = $"{STARDNA.BaseSTARPath}\\{STARDNA.DefaultGenesisNamespace}";

            if (string.IsNullOrEmpty(genesisNameSpace))
                genesisNameSpace = string.Concat(OAPPName, "OAPP");

            OASISResult<string> initOASISFolderResult = await InitOAPPFolderAsync(OAPPType, OAPPName, genesisFolder, genesisNameSpace, OAPPTemplateId, OAPPTemplateVersion, providerType);

            if (initOASISFolderResult != null && !string.IsNullOrEmpty(initOASISFolderResult.Result) && !initOASISFolderResult.IsError)
                OAPPFolder = initOASISFolderResult.Result;
            else
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured in InitOAPPFolderAsync. Reason: {initOASISFolderResult.Message}");
                return result;
            }

            genesisFolder = string.Concat(OAPPFolder, "\\", STARDNA.OAPPGeneratedCodeFolder);

            if (!Directory.Exists(string.Concat(genesisFolder, "\\CSharp")))
                Directory.CreateDirectory(string.Concat(genesisFolder, "\\CSharp"));

            if (!Directory.Exists(string.Concat(genesisFolder, "\\CSharp\\Zomes")))
                Directory.CreateDirectory(string.Concat(genesisFolder, "\\CSharp\\Zomes"));

            if (!Directory.Exists(string.Concat(genesisFolder, "\\CSharp\\Holons")))
                Directory.CreateDirectory(string.Concat(genesisFolder, "\\CSharp\\Holons"));

            if (!Directory.Exists(string.Concat(genesisFolder, "\\CSharp\\Interfaces")))
                Directory.CreateDirectory(string.Concat(genesisFolder, "\\CSharp\\Interfaces"));

            if (!Directory.Exists(string.Concat(genesisFolder, "\\CSharp\\Interfaces\\Zomes")))
                Directory.CreateDirectory(string.Concat(genesisFolder, "\\CSharp\\Interfaces\\Zomes"));

            if (!Directory.Exists(string.Concat(genesisFolder, "\\CSharp\\Interfaces\\Holons")))
                Directory.CreateDirectory(string.Concat(genesisFolder, "\\CSharp\\Interfaces\\Holons"));

            if (genesisType != GenesisType.ZomesAndHolonsOnly)
            {
                if (!Directory.Exists(string.Concat(genesisFolder, "\\CSharp\\CelestialBodies")))
                    Directory.CreateDirectory(string.Concat(genesisFolder, "\\CSharp\\CelestialBodies"));

                if (!Directory.Exists(string.Concat(genesisFolder, "\\CSharp\\Interfaces\\CelestialBodies")))
                    Directory.CreateDirectory(string.Concat(genesisFolder, "\\CSharp\\Interfaces\\CelestialBodies"));
            }

            // Rust folder creation removed - now handled by HoloOASIS.NativeCodeGenesis

            DirectoryInfo dirInfo = new DirectoryInfo(celestialBodyDNAFolder);
            FileInfo[] files = dirInfo.GetFiles();

            if (celestialBodyParent != null)
                celestialBodyParentId = celestialBodyParent.Id;

            switch (genesisType)
            {
                case GenesisType.Moon:
                    {
                        newBody = new Moon();

                        if (celestialBodyParent == null)
                            celestialBodyParent = DefaultPlanet;

                        Mapper<IPlanet, Moon>.MapParentCelestialBodyProperties((IPlanet)celestialBodyParent, (Moon)newBody);
                        newBody.ParentHolon = celestialBodyParent;
                        newBody.ParentHolonId = celestialBodyParentId;
                        newBody.ParentCelestialBody = celestialBodyParent;
                        newBody.ParentCelestialBodyId = celestialBodyParentId;
                        newBody.ParentPlanet = (IPlanet)celestialBodyParent;
                        newBody.ParentPlanetId = celestialBodyParentId;
                    }
                    break;

                case GenesisType.Planet:
                    {
                        newBody = new Planet();

                        //If no parent Star is passed in then set the parent star to our Sun.
                        if (celestialBodyParent == null)
                            celestialBodyParent = DefaultStar;

                        Mapper<IStar, Planet>.MapParentCelestialBodyProperties((IStar)celestialBodyParent, (Planet)newBody);
                        newBody.ParentHolon = celestialBodyParent;
                        newBody.ParentHolonId = celestialBodyParentId;
                        newBody.ParentCelestialBody = celestialBodyParent;
                        newBody.ParentCelestialBodyId = celestialBodyParentId;
                        newBody.ParentStar = (IStar)celestialBodyParent;
                        newBody.ParentStarId = celestialBodyParentId;
                    }
                break;

                case GenesisType.Star:
                    {
                        newBody = new Star();

                        if (celestialBodyParent == null)
                            celestialBodyParent = DefaultSuperStar;

                        Mapper<ISuperStar, Star>.MapParentCelestialBodyProperties((ISuperStar)celestialBodyParent, (Star)newBody);
                        newBody.ParentHolon = celestialBodyParent;
                        newBody.ParentHolonId = celestialBodyParentId;
                        newBody.ParentCelestialBody = celestialBodyParent;
                        newBody.ParentCelestialBodyId = celestialBodyParentId;
                        newBody.ParentSuperStar = (ISuperStar)celestialBodyParent;
                        newBody.ParentSuperStarId = celestialBodyParentId;
                    }
                break;

                //case GenesisType.Galaxy:
                //    {
                //        newBody = new SuperStar();

                //        if (celestialBodyParent == null)
                //            celestialBodyParent = DefaultGrandSuperStar;

                //        Mapper<IGrandSuperStar, SuperStar>.MapParentCelestialBodyProperties((IGrandSuperStar)celestialBodyParent, (SuperStar)newBody);
                //        newBody.ParentHolon = celestialBodyParent;
                //        newBody.ParentHolonId = celestialBodyParentId;
                //        newBody.ParentCelestialBody = celestialBodyParent;
                //        newBody.ParentCelestialBodyId = celestialBodyParentId;
                //        newBody.ParentGrandSuperStar = (IGrandSuperStar)celestialBodyParent;
                //        newBody.ParentGrandSuperStarId = celestialBodyParentId;
                //    }
                //    break;

                //case GenesisType.Universe:
                //    {
                //        newBody = new GrandSuperStar();

                //        if (celestialBodyParent == null)
                //            celestialBodyParent = DefaultGreatGrandSuperStar;

                //        Mapper<IGreatGrandSuperStar, GrandSuperStar>.MapParentCelestialBodyProperties((IGreatGrandSuperStar)celestialBodyParent, (GrandSuperStar)newBody);
                //        newBody.ParentHolon = celestialBodyParent;
                //        newBody.ParentHolonId = celestialBodyParentId;
                //        newBody.ParentCelestialBody = celestialBodyParent;
                //        newBody.ParentCelestialBodyId = celestialBodyParentId;
                //        newBody.ParentGreatGrandSuperStar = (IGreatGrandSuperStar)celestialBodyParent;
                //        newBody.ParentGreatGrandSuperStarId = celestialBodyParentId;
                //    }
                //    break;
            }

            if (genesisType != GenesisType.ZomesAndHolonsOnly)
            {
                newBody.Id = Guid.NewGuid();
                newBody.IsNewHolon = true; //This was commented out, not sure why?
                newBody.Name = OAPPName;
                newBody.Description = OAPPDescription;
                newBody.OnCelestialBodySaved += NewBody_OnCelestialBodySaved;
                newBody.OnCelestialBodyError += NewBody_OnCelestialBodyError;
                newBody.OnZomeSaved += NewBody_OnZomeSaved;
                newBody.OnZomeError += NewBody_OnZomeError;
                newBody.OnZomesSaved += NewBody_OnZomesSaved;
                newBody.OnZomesError += NewBody_OnZomesError;
                newBody.OnHolonSaved += NewBody_OnHolonSaved;
                newBody.OnHolonError += NewBody_OnHolonError;
                newBody.OnHolonsSaved += NewBody_OnHolonsSaved;
                newBody.OnHolonsError += NewBody_OnHolonsError;
            }
          
            // All Rust code generation has been moved to HoloOASIS.NativeCodeGenesis
            IZome currentZome = null;
            IHolon currentHolon = null;
            List<IZome> zomes = new List<IZome>();

            foreach (FileInfo file in files)
            {
                if (file != null)
                {
                    StreamReader reader = file.OpenText();

                    while (!reader.EndOfStream)
                    {
                        string buffer = reader.ReadLine();

                        if (buffer.Contains("namespace"))
                        {
                            string[] parts = buffer.Split(' ');

                            //If the new namespace name has not been passed in then default it to the proxy holon namespace.
                            if (string.IsNullOrEmpty(genesisNameSpace))
                                genesisNameSpace = parts[1];

                            zomeBufferCsharp = zomeTemplateCsharp.Replace(STARDNA.CSharpDNATemplateNamespace, genesisNameSpace);
                            izomeBufferCsharp = iZomeTemplate.Replace(STARDNA.CSharpDNATemplateNamespace, genesisNameSpace);
                            holonBufferCsharp = holonTemplateCsharp.Replace(STARDNA.CSharpDNATemplateNamespace, genesisNameSpace);
                            iholonBufferCsharp = iHolonTemplate.Replace(STARDNA.CSharpDNATemplateNamespace, genesisNameSpace);
                        }

                        if (buffer.Contains("ZomeDNA"))
                        {
                            string[] parts = buffer.Split(' ');
                            //libBuffer = libTemplate.Replace("zome_name", parts[6].ToSnakeCase());

                            zomeName = parts[6].ToPascalCase();
                            zomeBufferCsharp = zomeBufferCsharp.Replace("ZomeDNATemplate", zomeName);
                            zomeBufferCsharp = zomeBufferCsharp.Replace("IZome", $"I{zomeName}");
                            izomeBufferCsharp = izomeBufferCsharp.Replace("IZomeDNATemplate", $"I{zomeName}");

                            currentZome = new Zome()
                            {
                                Id = Guid.NewGuid(),
                                IsNewHolon = true,
                                Name = zomeName,
                                CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI),
                                HolonType = HolonType.Zome,
                                ParentHolonId = newBody != null ? newBody.Id : Guid.Empty,
                                ParentHolon = newBody,
                                ParentCelestialBodyId = newBody != null ? newBody.Id : Guid.Empty,
                                ParentCelestialBody = newBody,
                                ParentPlanetId = newBody != null && newBody.HolonType == HolonType.Planet ? newBody.Id : Guid.Empty,
                                ParentPlanet = newBody != null && newBody.HolonType == HolonType.Planet ? (IPlanet)newBody : null,
                                ParentMoonId = newBody != null && newBody.HolonType == HolonType.Moon ? newBody.Id : Guid.Empty,
                                ParentMoon = newBody != null && newBody.HolonType == HolonType.Moon ? (IMoon)newBody : null
                            };

                            zomeBufferCsharp = zomeBufferCsharp.Replace("ID", currentZome.Id.ToString());

                            if (newBody != null)
                            {
                                Mapper.MapParentCelestialBodyProperties(newBody, currentZome);
                                //await newBody.CelestialBodyCore.AddZomeAsync(currentZome);
                                await newBody.CelestialBodyCore.AddZomeAsync(currentZome, false); //Ideally wanted to save the zomes/holons all in one go when the celestialbody is saved (and it would have if we called .save() on the newBody below... but for some reason we implemented it differently! ;-) lol
                            }
                            //else
                                zomes.Add(currentZome); //used only for Zomes & Holons Only Genesis Type.
                        }

                        if (holonReached && buffer.Contains("string") || buffer.Contains("int") || buffer.Contains("bool"))
                        {
                            string[] parts = buffer.Split(' ');
                            string fieldName = parts[14].ToSnakeCase();

                            switch (parts[13].ToLower())
                            {
                                case "string":
                                    {
                                        if (string.IsNullOrEmpty(firstStringProperty))
                                            firstStringProperty = parts[14];

                                        GenerateCSharpField(parts[14], StringTemplateCSharp, ref holonBufferCsharp, ref iholonBufferCsharp, ref firstField, ref secondField);
                                        // Rust field generation moved to HoloOASIS.NativeCodeGenesis
                                    }
                                    break;

                                case "int":
                                    {
                                        GenerateCSharpField(parts[14], IntTemplateCsharp, ref holonBufferCsharp, ref iholonBufferCsharp, ref firstField, ref secondField);
                                        // Rust field generation moved to HoloOASIS.NativeCodeGenesis
                                    }
                                    break;

                                case "bool":
                                    {
                                        GenerateCSharpField(parts[14], BoolTemplateCsharp, ref holonBufferCsharp, ref iholonBufferCsharp, ref firstField, ref secondField);
                                        // Rust field generation moved to HoloOASIS.NativeCodeGenesis
                                    }
                                    break;
                            }
                        }

                        // Write the holon out.
                        if (holonReached && buffer.Length > 1 && buffer.Substring(buffer.Length - 1, 1) == "}" && !buffer.Contains("get;"))
                        {
                            holonName = holonName.ToPascalCase();

                            File.WriteAllText(string.Concat(genesisFolder, "\\CSharp\\Interfaces\\Holons\\I", holonName, ".cs"), iholonBufferCsharp);
                            File.WriteAllText(string.Concat(genesisFolder, "\\CSharp\\Holons\\", holonName, ".cs"), holonBufferCsharp);

                            holonBufferCsharp = "";
                            iholonBufferCsharp = "";
                            holonReached = false;
                            firstField = true;
                            firstHolon = false;
                            holonName = "";
                        }

                        if (buffer.Contains("HolonDNA"))
                        {
                            string[] parts = buffer.Split(' ');
                            holonName = parts[10].ToPascalCase();

                            // Rust holon template processing moved to HoloOASIS.NativeCodeGenesis

                            //Process the CSharp Templates.
                            if (string.IsNullOrEmpty(holonBufferCsharp))
                                holonBufferCsharp = holonTemplateCsharp;

                            if (string.IsNullOrEmpty(iholonBufferCsharp))
                                iholonBufferCsharp = iHolonTemplate;

                            holonBufferCsharp = holonBufferCsharp.Replace("HolonDNATemplate", holonName);
                            iholonBufferCsharp = iholonBufferCsharp.Replace("IHolonDNATemplate", string.Concat("I", holonName));
     
                            zomeBufferCsharp = zomeBufferCsharp.Insert(zomeBufferCsharp.Length - 7, string.Concat(loadHolonTemplateCsharp.Replace(".CelestialBodyCore", ""), "\n"));
                            zomeBufferCsharp = zomeBufferCsharp.Insert(zomeBufferCsharp.Length - 7, string.Concat(saveHolonTemplateCsharp.Replace(".CelestialBodyCore", ""), "\n"));
                            zomeBufferCsharp = zomeBufferCsharp.Replace("HOLON", holonName);
                            zomeBufferCsharp = zomeBufferCsharp.Replace("IHOLON", $"I{holonName}");
    
                            izomeBufferCsharp = izomeBufferCsharp.Insert(izomeBufferCsharp.Length - 10, string.Concat(iloadHolonTemplateCsharp.Replace(".CelestialBodyCore", ""), "\n"));
                            //izomeBufferCsharp = izomeBufferCsharp.Insert(izomeBufferCsharp.Length - 10, string.Concat(isaveHolonTemplateCsharp.Replace(".CelestialBodyCore", ""), "\n"));
                            izomeBufferCsharp = izomeBufferCsharp.Insert(izomeBufferCsharp.Length - 10, string.Concat(isaveHolonTemplateCsharp.Replace(".CelestialBodyCore", "")));
                            izomeBufferCsharp = izomeBufferCsharp.Replace("HOLON", holonName);
                            izomeBufferCsharp = izomeBufferCsharp.Replace("IHOLON", $"I{holonName}");

                            zomeBufferCsharp = zomeBufferCsharp.Replace(STARDNA.CSharpDNATemplateNamespace, genesisNameSpace);
                            izomeBufferCsharp = izomeBufferCsharp.Replace(STARDNA.CSharpDNATemplateNamespace, genesisNameSpace);
                            holonBufferCsharp = holonBufferCsharp.Replace(STARDNA.CSharpDNATemplateNamespace, genesisNameSpace);
                            iholonBufferCsharp = iholonBufferCsharp.Replace(STARDNA.CSharpDNATemplateNamespace, genesisNameSpace);

                            if (newBody != null)
                            {
                                if (string.IsNullOrEmpty(celestialBodyBufferCsharp))
                                    celestialBodyBufferCsharp = celestialBodyTemplateCsharp;

                                celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace(STARDNA.CSharpDNATemplateNamespace, genesisNameSpace);
                                celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("NAMESPACE", genesisNameSpace);
                                celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("ID", newBody.Id.ToString());
                                celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("CelestialBodyDNATemplate", OAPPName.ToPascalCase());
                                celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("CELESTIALBODY", Enum.GetName(typeof(GenesisType), genesisType));
                                celestialBodyBufferCsharp = celestialBodyBufferCsharp.Insert(celestialBodyBufferCsharp.Length - 7, string.Concat(loadHolonTemplateCsharp, "\n"));
                                celestialBodyBufferCsharp = celestialBodyBufferCsharp.Insert(celestialBodyBufferCsharp.Length - 7, string.Concat(saveHolonTemplateCsharp, "\n"));
                                celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("HOLON", parts[10].ToPascalCase());
                            }

                            // TODO: Current Zome Id will be empty here so need to save the zome before? (above when the zome is first created and added to the newBody zomes collection).
                            currentHolon = new Holon()
                            {
                                Id = Guid.NewGuid(),
                                IsNewHolon = true,
                                Name = holonName,
                                CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI),
                                HolonType = HolonType.Holon,
                                ParentHolonId = currentZome.Id,
                                ParentHolon = currentZome,
                                ParentZomeId = currentZome.Id,
                                ParentZome = currentZome,
                                ParentCelestialBodyId = newBody != null ? newBody.Id : Guid.Empty,
                                ParentCelestialBody = newBody,
                                ParentPlanetId = newBody != null && newBody.HolonType == HolonType.Planet ? newBody.Id : Guid.Empty,
                                ParentPlanet = newBody != null && newBody.HolonType == HolonType.Planet ? (IPlanet)newBody : null,
                                ParentMoonId = newBody != null && newBody.HolonType == HolonType.Moon ? newBody.Id : Guid.Empty,
                                ParentMoon = newBody != null && newBody.HolonType == HolonType.Moon ? (IMoon)newBody : null 
                            };

                            holonBufferCsharp = holonBufferCsharp.Replace("ID", currentHolon.Id.ToString());

                            if (newBody != null )
                                Mapper.MapParentCelestialBodyProperties(newBody, currentHolon);
                            
                            ((List<IHolon>)currentZome.Children).Add((Holon)currentHolon);

                            holonNames.Add(holonName);
                            holonName = holonName.ToSnakeCase();
                            holonReached = true;
                        }
                    }

                    reader.Close();

                    // Rust lib.rs generation moved to HoloOASIS.NativeCodeGenesis
                    File.WriteAllText(string.Concat(genesisFolder, "\\CSharp\\Interfaces\\Zomes\\I", zomeName, ".cs"), izomeBufferCsharp);
                    File.WriteAllText(string.Concat(genesisFolder, "\\CSharp\\Zomes\\", zomeName, ".cs"), zomeBufferCsharp);
                }
            }

            // Remove any white space from the name.
            if (genesisType != GenesisType.ZomesAndHolonsOnly)
                File.WriteAllText(string.Concat(genesisFolder, "\\CSharp\\CelestialBodies\\", OAPPName, Enum.GetName(typeof(GenesisType), genesisType), ".cs"), celestialBodyBufferCsharp);
                //File.WriteAllText(string.Concat(genesisFolder, "\\CSharp\\CelestialBodies\\", Regex.Replace(OAPPName, @"\s+", ""), Enum.GetName(typeof(GenesisType), genesisType), ".cs"), celestialBodyBufferCsharp);

            // Currently the OApp Name is the same as the CelestialBody name (each CelestialBody is a seperate OApp), but in future a OApp may be able to contain more than one celestialBody...
            // TODO: Currently the OApp templates only contain sample load/save for one holon... this may change in future... likely will... ;-) Want to show for every zome/holon inside the celestialbody...
            if (holonNames.Count > 0)
                ApplyOAPPTemplate(genesisType, OAPPFolder, genesisNameSpace, OAPPName, OAPPName, zomes[0].Name, holonNames[0], firstStringProperty, metaHolonTagMappings, metaTagMappings);
            else
                ApplyOAPPTemplate(genesisType, OAPPFolder, genesisNameSpace, OAPPName, OAPPName, zomes[0].Name, "", firstStringProperty, metaHolonTagMappings, metaTagMappings);

            //Generate any native code for the current provider.
            //TODO: Add option to pass into STAR which providers to generate native code for (can be more than one provider).
            if (ProviderManager.Instance.CurrentStorageProvider is IOASISSuperStar superStar)
            {
                // Pass celestialBodyDNAFolder to HoloOASIS for Rust generation
                // Rust template paths are now read from OASISDNA.HoloOASIS section
                var nativeParams = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "celestialBodyDNAFolder", celestialBodyDNAFolder }
                });
                superStar.NativeCodeGenesis(newBody, OAPPFolder, nativeParams);
            }

            switch (genesisType)
            {
                case GenesisType.ZomesAndHolonsOnly:
                    {
                        foreach (IZome zome in zomes)
                        {
                            OASISResult<IZome> saveZomeResult = await zome.SaveAsync();

                            if (!(saveZomeResult != null && saveZomeResult.Result != null && !saveZomeResult.IsError))
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured saving zome {LoggingHelper.GetHolonInfoForLogging(zome, "zome")}. Reason: {saveZomeResult.Message}.", true);
                        }

                        if (!result.IsError)
                            result.Message = "Zomes And Holons Successfully Created.";
                        else
                            result.Message = $"Some errors occured saving zomes and holons: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

                        result.Result.Zomes = new List<IZome>(zomes);
                    }break;

                case GenesisType.Moon:
                    {
                        //celestialBodyParent will be a Planet (Default is Our World).
                        //TODO: Soon need to add this code to Holon or somewhere so Parent's are lazy loaded when accessed for first time.
                        if (celestialBodyParent.ParentStar == null)
                            celestialBodyParent.ParentStar = new Star(celestialBodyParent.ParentStarId);

                        OASISResult<IMoon> addMoonresult =  await ((StarCore)celestialBodyParent.ParentStar.CelestialBodyCore).AddMoonAsync(newBody.ParentPlanet, (IMoon)newBody);

                        if (addMoonresult != null)
                        {
                            if (addMoonresult.IsError)
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error Occured Calling AddMoonAsync. Reason: {addMoonresult.Message}");
                            else
                            {
                                result.Result.CelestialBody = addMoonresult.Result;
                                result.Message = "Moon Successfully Created.";
                            }
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown Error Occured Creating Moon.");
                    }break;

                case GenesisType.Planet:
                    {                      
                        OASISResult<IPlanet> addPlanetResult = await ((StarCore)celestialBodyParent.CelestialBodyCore).AddPlanetAsync((IPlanet)newBody);

                        if (addPlanetResult != null)
                        {
                            if (addPlanetResult.IsError)
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error Occured Calling AddPlanetAsync. Reason: {addPlanetResult.Message}");
                            else
                            {
                                result.Result.CelestialBody = addPlanetResult.Result;
                                result.Message = "Planet Successfully Created.";
                            }
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown Error Occured Creating Planet.");
                    }break;

                case GenesisType.Star:
                    {
                        OASISResult<IStar> starResult = await ((ISuperStarCore)celestialBodyParent.CelestialBodyCore).AddStarAsync((IStar)newBody);

                        if (starResult != null)
                        {
                            if (starResult.IsError)
                                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error Occured Calling AddStarAsync. Reason: {starResult.Message}");
                            else
                            {
                                result.Result.CelestialBody = starResult.Result;
                                result.Message = "Star Successfully Created.";
                            }
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown Error Occured Creating Star.");
                    }break;

                //case GenesisType.SoloarSystem:
                //    {
                //        OASISResult<ISolarSystem> result = await ((StarCore)celestialBodyParent.CelestialBodyCore).AddSolarSystemAsync(new SolarSystem() { Star = (IStar)newBody });

                //        if (result != null)
                //        {
                //            if (result.IsError)
                //                return new OASISResult<CoronalEjection>() { IsError = true, Message = result.Message, Result = new CoronalEjection() { CelestialSpace = result.Result, CelestialBody = result.Result.Star } };
                //            else
                //                return new OASISResult<CoronalEjection>() { IsError = false, Message = "Star/SoloarSystem Successfully Created.", Result = new CoronalEjection() { CelestialSpace = result.Result, CelestialBody = result.Result.Star } };
                //        }
                //        else
                //            return new OASISResult<CoronalEjection>() { IsError = true, Message = "Unknown Error Occured Creating Star/SoloarSystem." };
                //    }

                //TODO: Come back to this! ;-)

                /*
                case GenesisType.Galaxy:
                    {
                        OASISResult<IGalaxy> result = await ((IGrandSuperStarCore)celestialBodyParent.CelestialBodyCore).AddGalaxyClusterToUniverse(new GalaxyCluster() );


                        OASISResult<IGalaxy> result = await ((IGrandSuperStarCore)celestialBodyParent.CelestialBodyCore).AddGalaxyAsync(new Galaxy() { SuperStar = (ISuperStar)newBody });

                        if (result != null)
                        {
                            if (result.IsError)
                                return new CoronalEjection() { ErrorOccured = true, Message = result.Message, CelestialSpace = result.Result, CelestialBody = result.Result.Star };
                            else
                                return new CoronalEjection() { ErrorOccured = false, Message = "SuperStar/Galaxy Successfully Successfully Created.", CelestialSpace = result.Result, CelestialBody = result.Result.Star };
                        }
                        else
                            return new CoronalEjection() { ErrorOccured = true, Message = "Unknown Error Occured Creating SuperStar/Galaxy." };
                    }

                case GenesisType.Universe:
                    {
                        await ((IGreatGrandSuperStarCore)celestialBodyParent.CelestialBodyCore).AddUniverseAsync(new Universe() { GrandSuperStar = (IGrandSuperStar)newBody });
                        return new CoronalEjection() { ErrorOccured = false, Message = "GrandSuperStar/Universe Successfully Created.", CelestialBody = newBody };
                    }*/

                // Cannot create a SuperStar on its own, you create a Galaxy which comes with a new SuperStar at the centre.

                //case GenesisType.SuperStar:
                //    {
                //        await ((IGrandSuperStarCore)celestialBodyParent.CelestialBodyCore).AddGalaxyAsync(new Galaxy() { SuperStar = (ISuperStar)newBody });
                //        return new CoronalEjection() { ErrorOccured = false, Message = "SuperStar/Galaxy Successfully Created.", CelestialBody = newBody };
                //    }

                default:
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Unknown Error Occured, GenesisType {Enum.GetName(typeof(GenesisType), genesisType)} Not Recognised!");
                    break;
            }

            return result;
        }

        public static OASISResult<Dictionary<string, IList<INode>>> ExtractNodesFromCelestialBodyMetaDataDNA(string celestialBodyDNAFolder)
        {
            OASISResult<Dictionary<string, IList<INode>>> result = new OASISResult<Dictionary<string, IList<INode>>>(new Dictionary<string, IList<INode>>());

            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(celestialBodyDNAFolder);
                FileInfo[] files = dirInfo.GetFiles();
                bool holonReached = false;
                string holonName = "";

                foreach (FileInfo file in files)
                {
                    if (file != null)
                    {
                        using (StreamReader reader = file.OpenText())
                        {
                            while (!reader.EndOfStream)
                            {
                                string buffer = reader.ReadLine();

                                if (holonReached && buffer.Length > 1 && buffer.Substring(buffer.Length - 1, 1) == "}" && !buffer.Contains("get;"))
                                {
                                    holonReached = false;
                                    holonName = "";
                                }

                                if (buffer.Contains("HolonDNA"))
                                {
                                    string[] parts = buffer.Split(' ');
                                    holonName = parts[10].ToPascalCase();
                                    holonReached = true;
                                }

                                if (buffer.Contains("string") || buffer.Contains("int") || buffer.Contains("bool"))
                                {
                                    string[] parts = buffer.Split(' ');
                                    string fieldName = parts[14].ToPascalCase();

                                    if (!result.Result.ContainsKey(holonName))
                                        result.Result[holonName] = new List<INode>();

                                    result.Result[holonName].Add(new Node()
                                    {
                                        NodeName = fieldName,
                                        NodeType = parts[13].ToLower() switch
                                        {
                                            "string" => NodeType.String,
                                            "int" => NodeType.Int,
                                            "bool" => NodeType.Bool,
                                            _ => NodeType.Unknown
                                        }
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Invalid CelesitalBodyMetaDataDNA! Please correct or use another!");
            }

            return result;
        }

        ////public static async Task<OASISResult<IGenerateMetaDataDNAResult>> GenerateMetaDataDNAAsync(List<IZome> zomes, string CelestialBodyMetaDataDNAName, string CelestialBodyMetaDataDNADesc, CelestialBodyType celestialBodyType, string ZomeMetaDataDNAName, string ZomeMetaDataDNADesc, ZomeType zomeType, string HolonMetaDataDNAName, string HolonMetaDataDNADesc, HolonType holonType, string fullPathToCelestialBodySourcePath = "", string fullPathToZomeSourcePath = "", string fullPathToHolonSourcePath = "", ProviderType providerType = ProviderType.Default)
        ////public static async Task<OASISResult<IGenerateMetaDataDNAResult>> GenerateMetaDataDNAAsync(List<IZome> zomes, string CelestialBodyMetaDataDNAName, string CelestialBodyMetaDataDNADesc, CelestialBodyType celestialBodyType, string fullPathToCelestialBodySourcePath = "", string fullPathToZomeSourcePath = "", string fullPathToHolonSourcePath = "", ProviderType providerType = ProviderType.Default)
        //public static async Task<OASISResult<IGenerateMetaDataDNAResult>> GenerateMetaDataDNAAsync(List<IZome> zomes, string OAPPName, string OAPPMetaDataDNAPath = "", ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IGenerateMetaDataDNAResult> result = new OASISResult<IGenerateMetaDataDNAResult>();
        //    string errorMessage = "Error occured in STAR.GenerateMetaDataDNAAsync. Reason:";

        //    if (string.IsNullOrEmpty(OAPPMetaDataDNAPath))
        //    {
        //        if (Path.IsPathRooted(STARDNA.OAPPMetaDataDNA))
        //            OAPPMetaDataDNAPath = STARDNA.OAPPMetaDataDNA;
        //        else
        //            OAPPMetaDataDNAPath = Path.Combine(STARDNA.BaseSTARPath, STARDNA.OAPPMetaDataDNA);
        //    }

        //    //if (string.IsNullOrEmpty(OAPPMetaDataDNAPath))
        //    //{
        //    //    if (Path.IsPathRooted(STARDNA.DefaultCelestialBodiesMetaDataDNASourcePath))
        //    //        fullPathToCelestialBodySourcePath = STARDNA.DefaultCelestialBodiesMetaDataDNASourcePath;

        //    //    else if (Path.IsPathRooted(STARDNA.BaseSTARNETPath))
        //    //        fullPathToCelestialBodySourcePath = Path.Combine(STARDNA.BaseSTARNETPath, STARDNA.DefaultCelestialBodiesMetaDataDNASourcePath);

        //    //    else
        //    //        fullPathToCelestialBodySourcePath = Path.Combine(STARDNA.BaseSTARPath, STARDNA.BaseSTARNETPath, STARDNA.DefaultCelestialBodiesMetaDataDNASourcePath);
        //    //}

        //    //if (string.IsNullOrEmpty(fullPathToZomeSourcePath))
        //    //{
        //    //    if (Path.IsPathRooted(STARDNA.DefaultZomesMetaDataDNASourcePath))
        //    //        fullPathToZomeSourcePath = STARDNA.DefaultZomesMetaDataDNASourcePath;

        //    //    else if (Path.IsPathRooted(STARDNA.BaseSTARNETPath))
        //    //        fullPathToZomeSourcePath = Path.Combine(STARDNA.BaseSTARNETPath, STARDNA.DefaultZomesMetaDataDNASourcePath);

        //    //    else
        //    //        fullPathToZomeSourcePath = Path.Combine(STARDNA.BaseSTARPath, STARDNA.BaseSTARNETPath, STARDNA.DefaultZomesMetaDataDNASourcePath);
        //    //}

        //    //if (string.IsNullOrEmpty(fullPathToHolonSourcePath))
        //    //{
        //    //    if (Path.IsPathRooted(STARDNA.DefaultHolonsMetaDataDNASourcePath))
        //    //        fullPathToHolonSourcePath = STARDNA.DefaultHolonsMetaDataDNASourcePath;

        //    //    else if (Path.IsPathRooted(STARDNA.BaseSTARNETPath))
        //    //        fullPathToHolonSourcePath = Path.Combine(STARDNA.BaseSTARNETPath, STARDNA.DefaultHolonsMetaDataDNASourcePath);

        //    //    else
        //    //        fullPathToHolonSourcePath = Path.Combine(STARDNA.BaseSTARPath, STARDNA.BaseSTARNETPath, STARDNA.DefaultHolonsMetaDataDNASourcePath);
        //    //}


        //OASISResult<STARNETHolon> createResult = await STARAPI.CelestialBodiesMetaDataDNA.CreateAsync(BeamedInAvatar.Id, CelestialBodyMetaDataDNAName, CelestialBodyMetaDataDNADesc, celestialBodyType, fullPathToCelestialBodySourcePath, providerType: providerType);

        //if (createResult != null && createResult.Result != null && !createResult.IsError)
        //    result.Result.CelestialBodyMetaDataDNA = createResult.Result;
        //else
        //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling STARAPI.CelestialBodiesMetaDataDNA.CreateAsync. Reason: {createResult.Message}");


        //createResult = await STARAPI.ZomesMetaDataDNA.CreateAsync(BeamedInAvatar.Id, ZomeMetaDataDNAName, ZomeMetaDataDNADesc, zomeType, fullPathToZomeSourcePath, providerType: providerType);

        //if (createResult != null && createResult.Result != null && !createResult.IsError)
        //    result.Result.ZomeMetaDataDNA = createResult.Result;
        //else
        //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling STARAPI.ZomesMetaDataDNA.CreateAsync. Reason: {createResult.Message}");


        //createResult = await STARAPI.HolonsMetaDataDNA.CreateAsync(BeamedInAvatar.Id, HolonMetaDataDNAName, HolonMetaDataDNADesc, holonType, fullPathToHolonSourcePath, providerType: providerType);

        //if (createResult != null && createResult.Result != null && !createResult.IsError)
        //    result.Result.HolonMetaDataDNA = createResult.Result;
        //else
        //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling STARAPI.HolonsMetaDataDNA.CreateAsync. Reason: {createResult.Message}");


        //    OASISResult<bool> generateResult = GenerateMetaDataDNA(zomes, fullPathToCelestialBodySourcePath, fullPathToZomeSourcePath, fullPathToHolonSourcePath);

        //    if (!(generateResult != null && generateResult.Result != null && !generateResult.IsError))
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling STAR.GenerateMetaDataDNA. Reason: {generateResult.Message}");

        //    return result;
        //}

        //public static OASISResult<bool> GenerateMetaDataDNA(List<IZome> zomes, string generatedCelstialBodyMetaDataDNAPath, string generatedZomeMetaDataDNAPath, string generatedHolonMetaDataDNAPath, ProviderType providerType = ProviderType.Default)
        public static OASISResult<IGenerateMetaDataDNAResult> GenerateMetaDataDNA(List<IZome> zomes, string OAPPName, string OAPPMetaDataDNAPath = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGenerateMetaDataDNAResult> result = new OASISResult<IGenerateMetaDataDNAResult>();
            string propBuffer = "";
            string holonBuffer = "";
            string holonsBuffer = "";
            string zomeBuffer = "";
            string zomeDNAPath = "";
            string holonDNAPath = "";
            //string propTemplate = "            public {TYPE} {PROPERTYNAME} {get; set;}";
            string propTemplate = "public {TYPE} {PROPERTYNAME} {get; set;}";
            bool firstProp = true;
            int iHolon = 0;
            int iProp = 0;

            try
            {
                if (string.IsNullOrEmpty(OAPPMetaDataDNAPath))
                {
                    if (Path.IsPathRooted(STARDNA.OAPPMetaDataDNAFolder))
                        OAPPMetaDataDNAPath = STARDNA.OAPPMetaDataDNAFolder;
                    else
                        OAPPMetaDataDNAPath = Path.Combine(STARDNA.BaseSTARPath, STARDNA.OAPPMetaDataDNAFolder);
                }

                result.Result = new GenerateMetaDataDNAResult()
                {
                    CelestialBodyMetaDataDNAPath = Path.Combine(OAPPMetaDataDNAPath, OAPPName, "CelestialBodyDNA"),
                    ZomeMetaDataDNAPath = Path.Combine(OAPPMetaDataDNAPath, OAPPName, "ZomeDNA"),
                    HolonMetaDataDNAPath = Path.Combine(OAPPMetaDataDNAPath, OAPPName, "HolonDNA")
                };

                if (Directory.Exists(result.Result.CelestialBodyMetaDataDNAPath))
                    Directory.Delete(result.Result.CelestialBodyMetaDataDNAPath, true);

                if (Directory.Exists(result.Result.ZomeMetaDataDNAPath))
                    Directory.Delete(result.Result.ZomeMetaDataDNAPath, true);

                if (Directory.Exists(result.Result.HolonMetaDataDNAPath))
                    Directory.Delete(result.Result.HolonMetaDataDNAPath, true);

                Directory.CreateDirectory(result.Result.CelestialBodyMetaDataDNAPath);
                Directory.CreateDirectory(result.Result.ZomeMetaDataDNAPath);
                Directory.CreateDirectory(result.Result.HolonMetaDataDNAPath);

                //TODO: Apply this pathing logic to ALL of STARDNA paths! ;-)
                if (!Path.IsPathRooted(STARDNA.ZomeMetaDataDNA))
                {
                    if (Path.IsPathRooted(STARDNA.MetaDataDNATemplateFolder))
                        zomeDNAPath = Path.Combine(STARDNA.MetaDataDNATemplateFolder, STARDNA.ZomeMetaDataDNA);
                    else
                        zomeDNAPath = Path.Combine(STARDNA.BaseSTARPath, STARDNA.MetaDataDNATemplateFolder, STARDNA.ZomeMetaDataDNA);
                }
                else
                    zomeDNAPath = STARDNA.ZomeMetaDataDNA;


                if (!Path.IsPathRooted(STARDNA.HolonMetaDataDNA))
                {
                    if (Path.IsPathRooted(STARDNA.MetaDataDNATemplateFolder))
                        holonDNAPath = Path.Combine(STARDNA.MetaDataDNATemplateFolder, STARDNA.HolonMetaDataDNA);
                    else
                        holonDNAPath = Path.Combine(STARDNA.BaseSTARPath, STARDNA.MetaDataDNATemplateFolder, STARDNA.HolonMetaDataDNA);
                }
                else
                    holonDNAPath = STARDNA.HolonMetaDataDNA;

                string zomeMetaDataDNA = File.ReadAllText(zomeDNAPath);
                string holonMetaDataDNA = File.ReadAllText(holonDNAPath);
                //string[] lines = File.ReadAllLines(holonDNAPath);

                //string holonMetaDataDNA = "";
                //for (int i = 0; i < lines.Length; i++)
                //{
                //    if (!lines[i].Contains("//"))
                //    {
                //        holonMetaDataDNA = string.Concat(holonMetaDataDNA, lines[i]);

                //        if (i < lines.Length - 1)
                //            holonMetaDataDNA = string.Concat(holonMetaDataDNA, "\n");
                //    }
                //}

                foreach (IZome zome in zomes)
                {
                    holonBuffer = "";
                    holonsBuffer = "";
                    iHolon = 0;

                    foreach (IHolon holon in zome.Children)
                    {
                        iHolon++;
                        propBuffer = "";
                        firstProp = true;
                        iProp = 0;

                        foreach (INode node in holon.Nodes)
                        {
                            iProp++;
                            if (!firstProp)
                                propBuffer = string.Concat(propBuffer, "".PadRight(12));
                                    
                            switch (node.NodeType)
                            {
                                case NodeType.Bool:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "bool").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.String:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "string").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.Int:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "int").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.Double:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "double").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.Float:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "float").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.Long:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "long").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.DateTime:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "DateTime").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.ByteArray:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "byte[]").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.Object:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "object").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;
                            }

                            if (iProp != holon.Nodes.Count)
                                propBuffer = string.Concat(propBuffer, "\n");

                            firstProp = false;
                        }

                        holonBuffer = string.Concat(holonMetaDataDNA.Replace("{HOLONNAME}", holon.Name).Replace("{PROPERTIES}", propBuffer));

                        if (iHolon != zome.Children.Count)
                            holonBuffer = string.Concat(holonBuffer, "\n\n");

                        holonsBuffer = string.Concat(holonsBuffer, holonBuffer);
                        File.WriteAllText(Path.Combine(result.Result.HolonMetaDataDNAPath, string.Concat(holon.Name, ".cs")), holonBuffer);
                    }

                    zomeBuffer = zomeMetaDataDNA.Replace("{ZOMENAME}", zome.Name).Replace("{HOLONS}", holonsBuffer);
                    File.WriteAllText(Path.Combine(result.Result.ZomeMetaDataDNAPath, string.Concat(zome.Name, ".cs")), zomeBuffer);
                    File.WriteAllText(Path.Combine(result.Result.CelestialBodyMetaDataDNAPath, string.Concat(zome.Name, ".cs")), zomeBuffer);
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STAR.GenerateMetaDataDNA generating the CelestialBody, Zome & Holon MetaData DNA. Reason: {e}");
            }

            return result;
        }

        public static void ShowStatusMessage(StarStatusMessageType messageType, string message)
        {
            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = messageType, Message = message });
        }

        //public static void ShowStatusMessage(StarStatusChangedEventArgs eventArgs)
        //{
        //    OnStarStatusChanged?.Invoke(null, eventArgs);
        //}

        public static void ShowStatusMessage<T>(OASISEventArgs<T> eventArgs)
        {
            if (eventArgs.Result != null && eventArgs.Result.Result != null)
            {
                if (!eventArgs.Result.IsError)
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"{((IHolon)eventArgs.Result.Result).Name} Created." });
                else
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating {((IHolon)eventArgs.Result.Result)}. Reason: {eventArgs.Result.Message}" });
            }
        }

        private static void NewBody_OnCelestialBodySaved(object sender, CelestialBodySavedEventArgs e)
        {
            if (IsDetailedStatusUpdatesEnabled && e.Result != null && e.Result.Result != null)
            {
                if (!e.Result.IsError)
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"{e.Result.Result.Name} Saved." });
                else
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating {e.Result.Result.Name}. Reason: {e.Result.Message}" });
            }

            /*
            switch (e.Result.Result.HolonType)
            {
                case HolonType.GreatGrandSuperStar:
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "GreatGrandSuperStar Created." });
                    break;

                case HolonType.GrandSuperStar:
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "GrandSuperStar Created." });
                    break;

                case HolonType.Multiverse:
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Default Multiverse Created." });
                    break;

                case HolonType.Dimension:
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"{e.Result.Result.Name} Created." });
                    break;

                case HolonType.Universe:
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Default Universe Created." });
                    break;
            }*/

            //switch (e.Result.Result.Name)
            //{
            //    case "ThirdDimenson"
            //}

            OnCelestialBodySaved?.Invoke(null, e);
        }

        private static void NewBody_OnCelestialBodyError(object sender, CelestialBodyErrorEventArgs e)
        {
            OnCelestialBodyError?.Invoke(null, e);
        }

        private static void NewBody_OnZomeSaved(object sender, ZomeSavedEventArgs e)
        {
            OnZomeSaved?.Invoke(null, e);
        }

        private static void NewBody_OnZomesSaved(object sender, ZomesSavedEventArgs e)
        {
            OnZomesSaved?.Invoke(null, e);
        }

        private static void NewBody_OnZomesError(object sender, ZomesErrorEventArgs e)
        {
            OnZomesError?.Invoke(null, e);
        }

        private static void NewBody_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(null, e);
        }

        private static void NewBody_OnHolonError(object sender, HolonErrorEventArgs e)
        {
            OnHolonError?.Invoke(null, e);
        }

        private static void NewBody_OnHolonsSaved(object sender, HolonsSavedEventArgs e)
        {
            OnHolonsSaved?.Invoke(null, e);
        }

        private static void NewBody_OnHolonsError(object sender, HolonsErrorEventArgs e)
        {
            OnHolonsError?.Invoke(null, e);
        }


        // Build
        public static CoronalEjection Flare(string bodyName)
        {
            //TODO: Build rust code using hc conductor and .net code using dotnet compiler.
            return new CoronalEjection();
        }

        public static CoronalEjection Flare(ICelestialBody body)
        {
            //TODO: Build rust code using hc conductor and .net code using dotnet compiler.
            return new CoronalEjection();
        }

        //Activate & Launch - Launch & activate a planet (OApp) by shining the star's light upon it...
        public static void Shine(ICelestialBody body)
        {

        }

        public static void Shine(string bodyName)
        {

        }

        //Dractivate
        public static void Dim(ICelestialBody body)
        {

        }

        public static void Dim(string bodyName)
        {

        }

        //Publish
        public static async Task<OASISResult<IOAPP>> SeedAsync(string fullPathToOAPP, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateOAPPSource = true, bool uploadOAPPSourceToSTARNET = true, bool makeOAPPSourcePublic = false, bool generateOAPPBinary = true, bool generateOAPPSelfContainedBinary = false, bool generateOAPPSelfContainedFullBinary = false, bool uploadOAPPToCloud = false, bool uploadOAPPSelfContainedToCloud = false, bool uploadOAPPSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS, ProviderType oappSelfContainedBinaryProviderType = ProviderType.None, ProviderType oappSelfContainedFullBinaryProviderType = ProviderType.None)
        {
            return await STARAPI.OAPPs.PublishOAPPAsync(BeamedInAvatar.AvatarId, fullPathToOAPP, launchTarget, fullPathToPublishTo, false, registerOnSTARNET, dotnetPublish, generateOAPPSource, uploadOAPPSourceToSTARNET, makeOAPPSourcePublic, generateOAPPBinary, generateOAPPSelfContainedBinary, generateOAPPSelfContainedFullBinary, uploadOAPPToCloud, uploadOAPPSelfContainedToCloud, uploadOAPPSelfContainedFullToCloud, providerType, oappBinaryProviderType, oappSelfContainedBinaryProviderType, oappSelfContainedFullBinaryProviderType);
        }

        public static OASISResult<IOAPP> Seed(string fullPathToOAPP, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateOAPPSource = true, bool uploadOAPPSourceToSTARNET = true, bool makeOAPPSourcePublic = false, bool generateOAPPBinary = true, bool generateOAPPSelfContainedBinary = false, bool generateOAPPSelfContainedFullBinary = false, bool uploadOAPPToCloud = false, bool uploadOAPPSelfContainedToCloud = false, bool uploadOAPPSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS, ProviderType oappSelfContainedBinaryProviderType = ProviderType.None, ProviderType oappSelfContainedFullBinaryProviderType = ProviderType.None)
        {
            return STARAPI.OAPPs.PublishOAPP(BeamedInAvatar.AvatarId, fullPathToOAPP, launchTarget, fullPathToPublishTo, false, registerOnSTARNET, dotnetPublish, generateOAPPSource, uploadOAPPSourceToSTARNET, makeOAPPSourcePublic, generateOAPPBinary, generateOAPPSelfContainedBinary, generateOAPPSelfContainedFullBinary, uploadOAPPToCloud, uploadOAPPSelfContainedToCloud, uploadOAPPSelfContainedFullToCloud, providerType, oappBinaryProviderType, oappSelfContainedBinaryProviderType, oappSelfContainedFullBinaryProviderType);
        }

        public static async Task<OASISResult<OAPP>> UnSeedAsync(Guid OAPPId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return await STARAPI.OAPPs.UnpublishAsync(BeamedInAvatar.Id, OAPPId, version, providerType);
        }

        public static OASISResult<OAPP> UnSeed(Guid OAPPId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return STARAPI.OAPPs.Unpublish(BeamedInAvatar.Id, OAPPId, version, providerType);
        }

        // Run Tests
        public static void Twinkle(ICelestialBody body)
        {

        }

        public static void Twinkle(string bodyName)
        {

        }

        // Delete Planet (OApp)
        public static void Dust(ICelestialBody body)
        {

        }

        // Delete Planet (OApp)
        public static void Dust(string bodyName)
        {

        }

        
        public static void Evolve(ICelestialBody body)
        {

        }

        public static void Evolve(string bodyName)
        {

        }

        public static void Mutate(ICelestialBody body)
        {

        }

        public static void Mutate(string bodyName)
        {

        }

        // Highlight the Planet (OApp) in the OApp Store (StarNET)
        public static void Radiate(ICelestialBody body)
        {

        }

        public static void Radiate(string bodyName)
        {

        }

        // Show how much light the planet (OApp) is emitting into the solar system (StarNET/HoloNET)
        public static void Emit(ICelestialBody body)
        {

        }

        public static void Emit(string bodyName)
        {

        }

        // Show stats of the Planet (OApp)
        public static void Reflect(ICelestialBody body)
        {

        }

        public static void Reflect(string bodyName)
        {

        }

        // Send/Receive Love
        public static void Love(ICelestialBody body)
        {

        }

        public static void Love(string body)
        {

        }

        // Show network stats/management/settings
        public static void Burst(ICelestialBody body)
        {

        }

        public static void Burst(string body)
        {

        }

        // ????
        public static void Pulse(ICelestialBody body)
        {

        }

        public static void Pulse(string body)
        {

        }

        // Reserved For Future Use...
        public static void Super(ICelestialBody body)
        {

        }

        public static void Super(string planetName)
        {

        }

        private static void ValidateSTARDNA(STARDNA starDNA)
        {
            if (starDNA != null)
            {
                ValidateFolder("", starDNA.BaseSTARPath, "STARDNA.BaseSTARPath");
                ValidateFolder(starDNA.BaseSTARPath, starDNA.OAPPMetaDataDNAFolder, "STARDNA.OAPPMetaDataDNAFolder");
                //ValidateFolder(starDNA.BaseSTARPath, starDNA.GenesisFolder, "STARDNA.GenesisFolder", false, true);
                //ValidateFolder(starDNA.BaseSTARPath, starDNA.GenesisRustFolder, "STARDNA.GenesisRustFolder", false, true);
                ValidateFolder(starDNA.BaseSTARPath, starDNA.CSharpDNATemplateFolder, "STARDNA.CSharpDNATemplateFolder");
                ValidateFile(starDNA.BaseSTARPath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateHolonDNA, "STARDNA.CSharpTemplateHolonDNA");
                ValidateFile(starDNA.BaseSTARPath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateZomeDNA, "STARDNA.CSharpTemplateZomeDNA");
                ValidateFile(starDNA.BaseSTARPath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateCelestialBodyDNA, "STARDNA.CSharpTemplateCelestialBodyDNA");
                ValidateFile(starDNA.BaseSTARPath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateLoadHolonDNA, "STARDNA.CSharpTemplateLoadHolonDNA");
                ValidateFile(starDNA.BaseSTARPath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateSaveHolonDNA, "STARDNA.CSharpTemplateSaveHolonDNA");
                ValidateFile(starDNA.BaseSTARPath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateILoadHolonDNA, "STARDNA.CSharpTemplateILoadHolonDNA");
                ValidateFile(starDNA.BaseSTARPath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateISaveHolonDNA, "STARDNA.CSharpTemplateISaveHolonDNA");
                ValidateFile(starDNA.BaseSTARPath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateInt, "STARDNA.CSharpTemplateInt");
                ValidateFile(starDNA.BaseSTARPath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateString, "STARDNA.CSharpTemplateString");
                ValidateFile(starDNA.BaseSTARPath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateBool, "STARDNA.CSharpTemplateBool");

                //ValidateFolder(starDNA.BaseSTARPath, starDNA.OAPPBlazorTemplateDNA, "STARDNA.OAPPBlazorTemplateDNA", true);
                //ValidateFolder(starDNA.BaseSTARPath, starDNA.OAPPConsoleTemplateDNA, "STARDNA.OAPPConsoleTemplateDNA", true);
                //ValidateFolder(starDNA.BaseSTARPath, starDNA.OAPPCustomTemplateDNA, "STARDNA.OAPPCustomTemplateDNA", true);
                //ValidateFolder(starDNA.BaseSTARPath, starDNA.OAPPGraphQLServiceTemplateDNA, "STARDNA.OAPPGraphQLServiceTemplateDNA", true);
                //ValidateFolder(starDNA.BaseSTARPath, starDNA.OAPPgRPCServiceTemplateDNA, "STARDNA.OAPPgRPCServiceTemplateDNA", true);
                //ValidateFolder(starDNA.BaseSTARPath, starDNA.OAPPMAUITemplateDNA, "STARDNA.OAPPMAUITemplateDNA", true);
                //ValidateFolder(starDNA.BaseSTARPath, starDNA.OAPPRESTServiceTemplateDNA, "STARDNA.OAPPRESTServiceTemplateDNA", true);
                //ValidateFolder(starDNA.BaseSTARPath, starDNA.OAPPUnityTemplateDNA, "STARDNA.OAPPUnityTemplateDNA", true);
                //ValidateFolder(starDNA.BaseSTARPath, starDNA.OAPPWebMVCTemplateDNA, "STARDNA.OAPPWebMVCTemplateDNA", true);
                //ValidateFolder(starDNA.BaseSTARPath, starDNA.OAPPWindowsServiceTemplateDNA, "STARDNA.OAPPWindowsServiceTemplateDNA", true);
                //ValidateFolder(starDNA.BaseSTARPath, starDNA.OAPPWinFormsTemplateDNA, "STARDNA.OAPPWinFormsTemplateDNA", true);
                //ValidateFolder(starDNA.BaseSTARPath, starDNA.OAPPWPFTemplateDNA, "STARDNA.OAPPWPFTemplateDNA", true);


                // Rust template validation moved to HoloOASIS - commented out for rollback purposes:
                //ValidateFolder(starDNA.BaseSTARPath, starDNA.RustDNARSMTemplateFolder, "STARDNA.RustDNARSMTemplateFolder");
                //ValidateFile(starDNA.BaseSTARPath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateLib, "STARDNA.RustTemplateLib");
                //ValidateFile(starDNA.BaseSTARPath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateCreate, "STARDNA.RustTemplateCreate");
                //ValidateFile(starDNA.BaseSTARPath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateDelete, "STARDNA.RustTemplateDelete");
                //ValidateFile(starDNA.BaseSTARPath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateRead, "STARDNA.RustTemplateRead");
                //ValidateFile(starDNA.BaseSTARPath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateUpdate, "STARDNA.RustTemplateUpdate");
                //ValidateFile(starDNA.BaseSTARPath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateList, "STARDNA.RustTemplateList");
                //ValidateFile(starDNA.BaseSTARPath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateValidation, "STARDNA.RustTemplateValidation");
                //ValidateFile(starDNA.BaseSTARPath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateInt, "STARDNA.RustTemplateInt");
                //ValidateFile(starDNA.BaseSTARPath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateString, "STARDNA.RustTemplateString");
                //ValidateFile(starDNA.BaseSTARPath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateBool, "STARDNA.RustTemplateBool");
                //ValidateFile(starDNA.BaseSTARPath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateHolon, "STARDNA.RustTemplateHolon");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPsSourcePath))
                    starDNA.DefaultOAPPsSourcePath = "OAPPs\\Source";

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPsPublishedPath))
                    starDNA.DefaultOAPPsPublishedPath = "OAPPs\\Published";

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPsDownloadedPath))
                    starDNA.DefaultOAPPsDownloadedPath = "OAPPs\\Downloaded";

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPsInstalledPath))
                    starDNA.DefaultOAPPsInstalledPath = "OAPPs\\Installed";


                if (string.IsNullOrEmpty(starDNA.DefaultOAPPTemplatesSourcePath))
                    starDNA.DefaultOAPPTemplatesSourcePath = "OAPPTemplates\\Source";

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPTemplatesPublishedPath))
                    starDNA.DefaultOAPPTemplatesPublishedPath = "OAPPTemplates\\Published";

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPTemplatesDownloadedPath))
                    starDNA.DefaultOAPPTemplatesDownloadedPath = "OAPPTemplates\\Downloaded";

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPTemplatesInstalledPath))
                    starDNA.DefaultOAPPTemplatesInstalledPath = "OAPPTemplates\\Installed";


                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesSourcePath))
                    starDNA.DefaultRuntimesSourcePath = "Runtimes\\Source";

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesPublishedPath))
                    starDNA.DefaultRuntimesPublishedPath = "Runtimes\\Published";

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesDownloadedPath))
                    starDNA.DefaultRuntimesDownloadedPath = "Runtimes\\Downloaded";

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesInstalledPath))
                    starDNA.DefaultRuntimesInstalledPath = "Runtimes\\Installed\\Other";

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesInstalledOASISPath))
                    starDNA.DefaultRuntimesInstalledOASISPath = "Runtimes\\Installed\\OASIS";

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesInstalledSTARPath))
                    starDNA.DefaultRuntimesInstalledSTARPath = "Runtimes\\Installed\\STAR";


                STARDNAManager.SaveDNA(STARDNAPath, STARDNA);

                ValidateFolder(starDNA.BaseSTARPath, starDNA.DefaultOAPPsSourcePath, "STARDNA.DefaultOAPPsSourcePath", false, true);
                ValidateFolder(starDNA.BaseSTARPath, starDNA.DefaultOAPPsPublishedPath, "STARDNA.DefaultOAPPsPublishedPath", false, true);
                ValidateFolder(starDNA.BaseSTARPath, starDNA.DefaultOAPPsDownloadedPath, "STARDNA.DefaultOAPPsDownloadedPath", false, true);
                ValidateFolder(starDNA.BaseSTARPath, starDNA.DefaultOAPPsInstalledPath, "STARDNA.DefaultOAPPsInstalledPath", false, true);
                ValidateFolder(starDNA.BaseSTARPath, starDNA.DefaultOAPPTemplatesSourcePath, "STARDNA.DefaultOAPPTemplatesSourcePath", false, true);
                ValidateFolder(starDNA.BaseSTARPath, starDNA.DefaultOAPPTemplatesPublishedPath, "STARDNA.DefaultOAPPTemplatesPublishedPath", false, true);
                ValidateFolder(starDNA.BaseSTARPath, starDNA.DefaultOAPPTemplatesDownloadedPath, "STARDNA.DefaultOAPPTemplatesDownloadedPath", false, true);
                ValidateFolder(starDNA.BaseSTARPath, starDNA.DefaultOAPPTemplatesInstalledPath, "STARDNA.DefaultOAPPTemplatesInstalledPath", false, true);
                ValidateFolder(starDNA.BaseSTARPath, starDNA.DefaultRuntimesSourcePath, "STARDNA.DefaultRuntimesSourcePath", false, true);
                ValidateFolder(starDNA.BaseSTARPath, starDNA.DefaultRuntimesPublishedPath, "STARDNA.DefaultRuntimesPublishedPath", false, true);
                ValidateFolder(starDNA.BaseSTARPath, starDNA.DefaultRuntimesDownloadedPath, "STARDNA.DefaultRuntimesDownloadedPath", false, true);
                ValidateFolder(starDNA.BaseSTARPath, starDNA.DefaultRuntimesInstalledPath, "STARDNA.DefaultRuntimesInstalledPath", false, true);
                ValidateFolder(starDNA.BaseSTARPath, starDNA.DefaultRuntimesInstalledOASISPath, "STARDNA.DefaultRuntimesInstalledOASISPath", false, true);
                ValidateFolder(starDNA.BaseSTARPath, starDNA.DefaultRuntimesInstalledSTARPath, "STARDNA.DefaultRuntimesInstalledSTARPath", false, true);
            }
            else
                throw new ArgumentNullException("STARDNA is null, please check and try again.");
        }

        private static void ValidateLightDNA(string celestialBodyDNAFolder, string genesisFolder)
        {
            ValidateFolder("", celestialBodyDNAFolder, "celestialBodyDNAFolder");
            ValidateFolder("", genesisFolder, "genesisFolder", false, true);
            //ValidateFolder("", genesisRustFolder, "genesisRustFolder", false, true);
        }

        private static void ValidateFolder(string basePath, string folder, string folderParam, bool checkIfContainsFilesOrFolder = false, bool createIfDoesNotExist = false)
        {
            string path = string.IsNullOrEmpty(basePath) ? folder : $"{basePath}\\{folder}";

            if (Path.IsPathRooted(folder))
                path = folder; //If the folder is rooted, use it as is.

            if (string.IsNullOrEmpty(folder))
                throw new ArgumentNullException(folderParam, string.Concat("The ", folderParam, " param in the STARDNA is null, please double check and try again."));

            if (checkIfContainsFilesOrFolder && Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)
                throw new InvalidOperationException(string.Concat("The ", folderParam, " folder (", path, ") in the STARDNA is empty."));

            if (!Directory.Exists(path))
            {
                if (createIfDoesNotExist)
                    Directory.CreateDirectory(path);
                else
                    throw new InvalidOperationException(string.Concat("The ", folderParam, " was not found (", path, "), please double check and try again."));
            }
        }

        private static void ValidateFile(string basePath, string folder, string file, string fileParam)
        {
            string path = $"{basePath}\\{folder}";

            if (string.IsNullOrEmpty(file))
                throw new ArgumentNullException(fileParam, string.Concat("The ", fileParam, " param in the STARDNA is null, please double check and try again."));

            if (!File.Exists(string.Concat(path, "\\", file)))
                throw new FileNotFoundException(string.Concat("The ", fileParam, " file is not valid, the file does not exist, please double check and try again."), string.Concat(path, "\\", file));
        }

        //private static STARDNA LoadDNA()
        //{
        //    using (StreamReader r = new StreamReader(STARDNAPath))
        //    {
        //        string json = r.ReadToEnd();
        //        STARDNA = JsonConvert.DeserializeObject<STARDNA> (json);
        //        return STARDNA;
        //    }
        //}
        //private static bool SaveDNA()
        //{
        //    try
        //    {
        //        string json = JsonConvert.SerializeObject(STARDNA);

        //        if (!Directory.Exists(Path.GetDirectoryName(STARDNAPath)))
        //            Directory.CreateDirectory(Path.GetDirectoryName(STARDNAPath));

        //        StreamWriter writer = new StreamWriter(STARDNAPath);
        //        writer.Write(json);
        //        writer.Close();
        //    }
        //    catch (Exception e)
        //    {
                
        //    }

        //    return true;
        //}

        private static void NewBody_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            //OnZomeError?.Invoke(sender, new ZomeErrorEventArgs() { EndPoint = StarBody.HoloNETClient.EndPoint, Reason = e.Reason, ErrorDetails = e.ErrorDetails, HoloNETErrorDetails = e.HoloNETErrorDetails });
            // OnStarError?.Invoke(sender, new StarErrorEventArgs() { EndPoint = StarBody.HoloNETClient.EndPoint, Reason = e.Reason, ErrorDetails = e.ErrorDetails, HoloNETErrorDetails = e.HoloNETErrorDetails });
        }

        //TODO: Get this working... :) // Is this working now?! lol hmmmm... need to check...
        private static string GenerateDynamicZomeFunc(string funcName, string zomeTemplateCsharp, string holonName, string zomeBufferCsharp, int funcLength)
        {
            int funcHolonIndex = zomeTemplateCsharp.IndexOf(funcName);
            string funct = zomeTemplateCsharp.Substring(funcHolonIndex - 26, funcLength); //170
            funct = funct.Replace("{holon}", holonName.ToSnakeCase()).Replace("HOLON", holonName.ToPascalCase());
            zomeBufferCsharp = zomeBufferCsharp.Insert(zomeBufferCsharp.Length - 6, funct);
            return zomeBufferCsharp;
        }

        // GenerateRustField method moved to HoloOASIS.NativeCodeGenesis

        private static void GenerateCSharpField(string fieldName, string fieldTemplate, ref string holonBufferCsharp, ref string iHolonBufferCsharp, ref bool firstField, ref bool secondField)
        {
            int fieldsEnd = holonBufferCsharp.LastIndexOf("}") - 7;
            holonBufferCsharp = holonBufferCsharp.Insert(fieldsEnd, string.Concat("\n", fieldTemplate.Replace("variableName", fieldName), "\n"));

            //fieldsEnd = iHolonBufferCsharp.LastIndexOf("}") - 7;

            if (firstField)
            {
                fieldsEnd = iHolonBufferCsharp.LastIndexOf("}") - 10;
                iHolonBufferCsharp = iHolonBufferCsharp.Insert(fieldsEnd, string.Concat(fieldTemplate.Replace("variableName", fieldName)));
                secondField = true;
            }
            else if (secondField)
            {
                secondField = false;
                fieldsEnd = iHolonBufferCsharp.LastIndexOf("}") - 7;
                //iHolonBufferCsharp = iHolonBufferCsharp.Insert(fieldsEnd, string.Concat("\n", fieldTemplate.Replace("variableName", fieldName), "\n"));
                iHolonBufferCsharp = iHolonBufferCsharp.Insert(fieldsEnd, string.Concat(fieldTemplate.Replace("variableName", fieldName), "\n"));
            }
            else
            {
                fieldsEnd = iHolonBufferCsharp.LastIndexOf("}") - 7;
                iHolonBufferCsharp = iHolonBufferCsharp.Insert(fieldsEnd, string.Concat("\n", fieldTemplate.Replace("variableName", fieldName), "\n"));
            }
        }

        private static OASISResult<bool> BootOASIS(string userName = "", string password = "", string OASISDNAPath = OASIS_DNA_DEFAULT_PATH)
        {
            STAR.OASISDNAPath = OASISDNAPath;

            if (!OASISAPI.IsOASISBooted)
                //return OASISAPI.BootOASIS(userName, password, STAR.OASISDNAPath);
                return STARAPI.BootOASISAPI(userName, password, STAR.OASISDNAPath);
            else
                return new OASISResult<bool>() { Message = "OASIS Already Booted" };
        }

        private static async Task<OASISResult<bool>> BootOASISAsync(string userName = "", string password = "", string OASISDNAPath = OASIS_DNA_DEFAULT_PATH)
        {
            STAR.OASISDNAPath = OASISDNAPath;

            if (!OASISAPI.IsOASISBooted)
                //return await OASISAPI.BootOASISAsync(userName, password, STAR.OASISDNAPath);
                return await STARAPI.BootOASISAsync(userName, password, STAR.OASISDNAPath);
            else
                return new OASISResult<bool>() { Message = "OASIS Already Booted" };
        }

        private static OASISResult<IOmiverse> IgniteInnerStar(OASISResult<IOmiverse> result)
        {
            //  _starId = Guid.Empty; //TODO:Temp, remove after!

            ShowStatusMessage(StarStatusMessageType.Processing, "IGNITING INNER STAR...");
            ShowStatusMessage(StarStatusMessageType.Processing, "Checking If OASIS Omniverse Already Created...");

            if (_starId == Guid.Empty)
                result = OASISOmniverseGenesisAsync().Result;
            else
            {
                result = InitDefaultCelestialBodies(result);
            }

            WireUpEvents();
            return result;
        }

        private static async Task<OASISResult<IOmiverse>> IgniteInnerStarAsync(OASISResult<IOmiverse> result)
        {
            // _starId = Guid.Empty; //TODO:Temp, remove after!

            ShowStatusMessage(StarStatusMessageType.Processing, "IGNITING INNER STAR...");
            ShowStatusMessage(StarStatusMessageType.Processing, "Checking If OASIS Omniverse Already Created...");

            if (_starId == Guid.Empty)
                result = await OASISOmniverseGenesisAsync();
            else
                result = await InitDefaultCelestialBodiesAsync(result);

            WireUpEvents();
            return result;
        }

        private static OASISResult<IOmiverse> InitDefaultCelestialBodies(OASISResult<IOmiverse> result)
        {
            ShowStatusMessage(StarStatusMessageType.Success, "OASIS Omniverse Already Created.");
            ShowStatusMessage(StarStatusMessageType.Processing, "Initializing Default Celestial Bodies...");

            (result, DefaultPlanet) = InitCelestialBody<Planet>(STARDNA.DefaultPlanetId, "Default Planet", result);

            if (result.IsError || DefaultPlanet == null)
                return result;

            (result, DefaultStar) = InitCelestialBody<Star>(STARDNA.DefaultStarId, "Default Star", result);

            if (result.IsError || DefaultStar == null)
                return result;

            (result, DefaultSuperStar) = InitCelestialBody<SuperStar>(STARDNA.DefaultSuperStarId, "Default Super Star", result);

            if (result.IsError || DefaultSuperStar == null)
                return result;

            (result, DefaultGrandSuperStar) = InitCelestialBody<GrandSuperStar>(STARDNA.DefaultGrandSuperStarId, "Default Grand Super Star", result);

            if (result.IsError || DefaultGrandSuperStar == null)
                return result;

            (result, DefaultGreatGrandSuperStar) = InitCelestialBody<GreatGrandSuperStar>(STARDNA.DefaultGreatGrandSuperStarId, "Default Great Grand Super Star", result);

            if (result.IsError || DefaultGreatGrandSuperStar == null)
                return result;

            ShowStatusMessage(StarStatusMessageType.Success, "Default Celestial Bodies Initialized.");
            return result;
        }

        private static async Task<OASISResult<IOmiverse>> InitDefaultCelestialBodiesAsync(OASISResult<IOmiverse> result)
        {
            ShowStatusMessage(StarStatusMessageType.Success, "OASIS Omniverse Already Created.");
            ShowStatusMessage(StarStatusMessageType.Processing, "Initializing Default Celestial Bodies...");

            (result, DefaultPlanet) = await InitCelestialBodyAsync<Planet>(STARDNA.DefaultPlanetId, "Default Planet", result);

            if (result.IsError || DefaultPlanet == null)
                return result;

            (result, DefaultStar) = await InitCelestialBodyAsync<Star>(STARDNA.DefaultStarId, "Default Star", result);

            if (result.IsError || DefaultStar == null)
                return result;

            (result, DefaultSuperStar) = await InitCelestialBodyAsync<SuperStar>(STARDNA.DefaultSuperStarId, "Default Super Star", result);

            if (result.IsError || DefaultSuperStar == null)
                return result;

            (result, DefaultGrandSuperStar) = await InitCelestialBodyAsync<GrandSuperStar>(STARDNA.DefaultGrandSuperStarId, "Default Grand Super Star", result);

            if (result.IsError || DefaultGrandSuperStar == null)
                return result;

            (result, DefaultGreatGrandSuperStar) = await InitCelestialBodyAsync<GreatGrandSuperStar>(STARDNA.DefaultGreatGrandSuperStarId, "Default Great Grand Super Star", result);

            if (result.IsError || DefaultGreatGrandSuperStar == null)
                return result;

            ShowStatusMessage(StarStatusMessageType.Success, "Default Celestial Bodies Initialized.");

            return result;
        }

        private static (OASISResult<IOmiverse>, T) InitCelestialBody<T>(string id, string longName, OASISResult<IOmiverse> result) where T : ICelestialBody, new()
        {
            Guid guidId;
            ICelestialBody celestialBody = null;
            string name = longName.Replace(" ", "");

            ShowStatusMessage(StarStatusMessageType.Processing, $"Initializing {longName}...");

            if (!string.IsNullOrEmpty(id))
            {
                if (Guid.TryParse(id, out guidId))
                {
                    //Normally you would leave autoLoad set to true but if you need to process the result in-line then you need to manually call Load as we do here (otherwise you would process the result from the OnCelestialBodyLoaded or OnCelestialBodyError event handlers).
                    //ICelestialBody celestialBody = new T(guidId, false);
                    celestialBody = new T() {  Id = guidId};
                    OASISResult<T> celestialBodyResult = celestialBody.Load<T>();

                    if (celestialBodyResult.IsError || celestialBodyResult.Result == null)
                    {
                        ShowStatusMessage(StarStatusMessageType.Error, $"Error Initializing {longName}.");
                        HandleCelesitalBodyInitError(result, name, id, celestialBodyResult);
                    }
                    else
                    {
                        ShowStatusMessage(StarStatusMessageType.Success, $"{longName} Initialized.");
                        OnDefaultCeletialBodyInit?.Invoke(null, new DefaultCelestialBodyInitEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(celestialBodyResult) });
                    }
                }
                else
                    HandleCelesitalBodyInitError<T>(result, name, id, $"The {name}Id value in STARDNA.json is not a valid Guid.");
            }
            else
                HandleCelesitalBodyInitError<T>(result, name, id, $"The {name}Id value in STARDNA.json is missing.");

            return (result, (T)celestialBody);
        }

        private static async Task<(OASISResult<IOmiverse>, T)> InitCelestialBodyAsync<T>(string id, string longName, OASISResult<IOmiverse> result) where T : ICelestialBody, new()
        {
            Guid guidId;
            ICelestialBody celestialBody = null;
            string name = longName.Replace(" ", "");

            ShowStatusMessage(StarStatusMessageType.Processing, $"Initializing {longName}..");

            if (!string.IsNullOrEmpty(id))
            {
                if (Guid.TryParse(id, out guidId))
                {
                    //Normally you would leave autoLoad set to true but if you need to process the result in-line then you need to manually call Load as we do here (otherwise you would process the result from the OnCelestialBodyLoaded or OnCelestialBodyError event handlers).
                    //ICelestialBody celestialBody = new T(guidId, false);
                    celestialBody = new T() { Id = guidId };
                    OASISResult<T> celestialBodyResult = await celestialBody.LoadAsync<T>();

                    if (celestialBodyResult.IsError || celestialBodyResult.Result == null)
                    {
                        ShowStatusMessage(StarStatusMessageType.Error, $"Error Initializing {longName}.");
                        HandleCelesitalBodyInitError(result, name, id, celestialBodyResult);
                    }
                    else
                    {
                        ShowStatusMessage(StarStatusMessageType.Success, $"{longName} Initialized.");
                        OnDefaultCeletialBodyInit?.Invoke(null, new DefaultCelestialBodyInitEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(celestialBodyResult) });
                    }
                }
                else
                    HandleCelesitalBodyInitError<T>(result, name, id, $"The {name}Id value in STARDNA.json is not a valid Guid.");
            }
            else
                HandleCelesitalBodyInitError<T>(result, name, id, $"The {name}Id value in STARDNA.json is missing.");

            return (result, (T)celestialBody);
        }

        //private static void HandleCelesitalBodyInitError(OASISResult<IOmiverse> result, string name, string id, string errorMessage, OASISResult<ICelestialBody> celstialBodyResult = null)
        //{
        //    string msg = $"Error occured in IgniteInnerStar initializing {name} with Id {id}. {errorMessage} Please correct or delete STARDNA to reset STAR ODK to then auto-generate new defaults.";

        //    if (celstialBodyResult != null)
        //        msg = string.Concat(msg, " Reason: ", celstialBodyResult.Message);

        //    OASISErrorHandling.HandleError(ref result, msg, celstialBodyResult != null ? celstialBodyResult.DetailedMessage : null);
        //}

        //private static void HandleCelesitalBodyInitError(OASISResult<IOmiverse> result, string name, string id, OASISResult<ICelestialBody> celstialBodyResult)
        //{
        //    HandleCelesitalBodyInitError(result, name, id, "Likely reason is that the id does not exist.", celstialBodyResult);
        //    //OASISErrorHandling.HandleError(ref result, $"Error occured in IgniteInnerStar initializing {name} with Id {id}. Likely reason is that the id does not exist. Please correct or delete STARDNA to reset STAR ODK to then auto-generate new defaults. Reason: {celstialBodyResult.Message}", celstialBodyResult.DetailedMessage);
        //    //OASISErrorHandling.HandleError(ref result, $"Error occured in IgniteInnerStar initializing {name} with Id {id}. Likely reason is that the id does not exist, in this case remove the {name}Id from STARDNA.json and then try again. Reason: {celstialBodyResult.Message}", celstialBodyResult.DetailedMessage);
        //}

        private static void HandleCelesitalBodyInitError<T>(OASISResult<IOmiverse> result, string name, string id, string errorMessage, OASISResult<T> celstialBodyResult = null) where T : ICelestialBody
        {
            string msg = $"Error occured in IgniteInnerStar initializing {name} with Id {id}. {errorMessage} Please correct or delete STARDNA to reset STAR ODK to then auto-generate new defaults.";

            if (celstialBodyResult != null)
                msg = string.Concat(msg, " Reason: ", celstialBodyResult.Message);

            OASISErrorHandling.HandleError(ref result, msg, celstialBodyResult != null ? celstialBodyResult.DetailedMessage : null);
        }

        private static void HandleCelesitalBodyInitError<T>(OASISResult<IOmiverse> result, string name, string id, OASISResult<T> celstialBodyResult) where T : ICelestialBody
        {
            HandleCelesitalBodyInitError(result, name, id, "Likely reason is that the id does not exist.", celstialBodyResult);
        }


        /// <summary>
        /// Create's the OASIS Omniverse along with a new default Multiverse (with it's GrandSuperStar) containing the ThirdDimension containing UniversePrime (simulation) and the MagicVerse (contains OApp's), which itself contains a default GalaxyCluster containing a default Galaxy (along with it's SuperStar) containing a default SolarSystem (along wth it's Star) containing a default planet (Our World).
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static async Task<OASISResult<IOmiverse>> OASISOmniverseGenesisAsync()
        {
            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();
            OASISResult<ICelestialSpace> celestialSpaceResult = new OASISResult<ICelestialSpace>();
            ShowStatusMessage(StarStatusMessageType.Processing, "OASIS Omniverse not found. Initiating Omniverse Genesis Process...");

            //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Omniverse..." });

            //Will create the Omniverse with all the omniverse dimensions (8 - 12) along with one default Multiverse and it's dimensions (1-7), each containing a Universe. 
            //The 3rd Dimension will contain the UniversePrime and MagicVerse.
            //It will also create the GreatGrandCentralStar in the centre of the Omniverse and also a GrandCentralStar at the centre of the Multiverse.
            Omniverse omniverse = new Omniverse();
            celestialSpaceResult = await omniverse.SaveAsync();
            OASISResultHelper.CopyResult(celestialSpaceResult, result);
            result.Result = (IOmiverse)celestialSpaceResult.Result;

            if (!result.IsError && result.Result != null)
            {
                //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "CelestialSpace Omniverse Created." });
                STARDNA.DefaultGreatGrandSuperStarId = omniverse.GreatGrandSuperStar.Id.ToString();
                STARDNA.DefaultGrandSuperStarId = omniverse.Multiverses[0].GrandSuperStar.Id.ToString();


                //TODO: May not need any of the code below because the Omniverse Save method will recursively save all it's child CelestialBodies & CelesitalSpaces...
                //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Multiverse..." });
                //Multiverse multiverse = new Multiverse();
                //celestialSpaceResult = await multiverse.SaveAsync(); //TODO: Check tomorrow if this is better way than using old below method (On the STAR Core).
                ////OASISResult<IMultiverse> multiverseResult = await ((GreatGrandSuperStarCore)result.Result.GreatGrandSuperStar.CelestialBodyCore).AddMultiverseAsync(multiverse);

                //if (!celestialSpaceResult.IsError && celestialSpaceResult.Result != null)
                //{
                //    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Multiverse Created." });
                //    multiverse = (Multiverse)celestialSpaceResult.Result;
                //    STARDNA.DefaultGrandSuperStarId = multiverse.GrandSuperStar.Id.ToString();

                //GalaxyCluster galaxyCluster = new GalaxyCluster();
                //galaxyCluster.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                //galaxyCluster.Name = "Our Milky Way Galaxy Cluster.";
                //galaxyCluster.Description = "Our Galaxy Cluster that our Milky Way Galaxy belongs to, the default Galaxy Cluster.";
                //Mapper<IMultiverse, GalaxyCluster>.MapParentCelestialBodyProperties(multiverse, galaxyCluster);
                //galaxyCluster.ParentMultiverse = multiverse;
                //galaxyCluster.ParentMultiverseId = multiverse.Id;
                //galaxyCluster.ParentDimension = multiverse.Dimensions.ThirdDimension;
                //galaxyCluster.ParentDimensionId = multiverse.Dimensions.ThirdDimension.Id;
                //galaxyCluster.ParentUniverseId = multiverse.Dimensions.ThirdDimension.MagicVerse.Id;
                //galaxyCluster.ParentUniverse = multiverse.Dimensions.ThirdDimension.MagicVerse;

                //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Galaxy Cluster..." });
                //OASISResult<IGalaxyCluster> galaxyClusterResult = await ((GrandSuperStarCore)multiverse.GrandSuperStar.CelestialBodyCore).AddGalaxyClusterToUniverseAsync(multiverse.Dimensions.ThirdDimension.MagicVerse, galaxyCluster);

                GalaxyCluster galaxyCluster = new GalaxyCluster();
                galaxyCluster.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                galaxyCluster.Name = "Our Milky Way Galaxy Cluster (Default Galaxy Cluster).";
                galaxyCluster.Description = "Our Galaxy Cluster that our Milky Way Galaxy belongs to, the default Galaxy Cluster.";
                Mapper<IMultiverse, GalaxyCluster>.MapParentCelestialBodyProperties(omniverse.Multiverses[0], galaxyCluster);
                galaxyCluster.ParentMultiverse = omniverse.Multiverses[0];
                galaxyCluster.ParentMultiverseId = omniverse.Multiverses[0].Id;
                galaxyCluster.ParentHolon = omniverse.Multiverses[0];
                galaxyCluster.ParentHolonId = omniverse.Multiverses[0].Id;
                galaxyCluster.ParentCelestialSpace = omniverse.Multiverses[0];
                galaxyCluster.ParentCelestialSpaceId = omniverse.Multiverses[0].Id;
                galaxyCluster.ParentDimension = omniverse.Multiverses[0].Dimensions.ThirdDimension;
                galaxyCluster.ParentDimensionId = omniverse.Multiverses[0].Dimensions.ThirdDimension.Id;
                galaxyCluster.ParentUniverseId = omniverse.Multiverses[0].Dimensions.ThirdDimension.MagicVerse.Id;
                galaxyCluster.ParentUniverse = omniverse.Multiverses[0].Dimensions.ThirdDimension.MagicVerse;

                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = $"Creating CelestialSpace {galaxyCluster.Name}..." });
                OASISResult<IGalaxyCluster> galaxyClusterResult = await ((GrandSuperStarCore)omniverse.Multiverses[0].GrandSuperStar.CelestialBodyCore).AddGalaxyClusterToUniverseAsync(omniverse.Multiverses[0].Dimensions.ThirdDimension.MagicVerse, galaxyCluster);

                if (!galaxyClusterResult.IsError && galaxyClusterResult.Result != null)
                {
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"CelestialSpace {galaxyCluster.Name} Created." }); ;
                    galaxyCluster = (GalaxyCluster)galaxyClusterResult.Result;

                    Galaxy galaxy = new Galaxy();
                    galaxy.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                    galaxy.Name = "Our Milky Way Galaxy (Default Galaxy)";
                    galaxy.Description = "Our Milky Way Galaxy, which is the default Galaxy.";
                    Mapper<IGalaxyCluster, Galaxy>.MapParentCelestialBodyProperties(galaxyCluster, galaxy);
                    galaxy.ParentGalaxyCluster = galaxyCluster;
                    galaxy.ParentGalaxyClusterId = galaxyCluster.Id;
                    galaxy.ParentHolon = galaxyCluster;
                    galaxy.ParentHolonId = galaxyCluster.Id;
                    galaxy.ParentCelestialSpace = galaxyCluster;
                    galaxy.ParentCelestialSpaceId = galaxyCluster.Id;

                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = $"Creating CelestialSpace {galaxy.Name}..." });
                    //OASISResult<IGalaxy> galaxyResult = await ((GrandSuperStarCore)multiverse.GrandSuperStar.CelestialBodyCore).AddGalaxyToGalaxyClusterAsync(galaxyCluster, galaxy);
                    OASISResult<IGalaxy> galaxyResult = await ((GrandSuperStarCore)omniverse.Multiverses[0].GrandSuperStar.CelestialBodyCore).AddGalaxyToGalaxyClusterAsync(galaxyCluster, galaxy);

                    if (!galaxyResult.IsError && galaxyResult.Result != null)
                    {
                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"CelestialSpace {galaxy.Name} Created." });
                        galaxy = (Galaxy)galaxyResult.Result;
                        STARDNA.DefaultSuperStarId = galaxy.SuperStar.Id.ToString();

                        SolarSystem solarSystem = new SolarSystem();
                        solarSystem.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                        solarSystem.Name = "Our Solar System (Default Solar System)";
                        solarSystem.Description = "Our Solar System, which is the default Solar System.";
                        solarSystem.Id = Guid.NewGuid();
                        solarSystem.IsNewHolon = true;

                        Mapper<IGalaxy, Star>.MapParentCelestialBodyProperties(galaxy, (Star)solarSystem.Star);
                        solarSystem.Star.Name = "Our Sun (Sol) (Default Star)";
                        solarSystem.Star.Description = "The Sun at the centre of our Solar System";
                        solarSystem.Star.ParentGalaxy = galaxy;
                        solarSystem.Star.ParentGalaxyId = galaxy.Id;
                        solarSystem.Star.ParentHolon = galaxy;
                        solarSystem.Star.ParentHolonId = galaxy.Id;
                        solarSystem.Star.ParentCelestialSpace = galaxy;
                        solarSystem.Star.ParentCelestialSpaceId = galaxy.Id;
                        solarSystem.Star.ParentSolarSystem = solarSystem;
                        solarSystem.Star.ParentSolarSystemId = solarSystem.Id;

                        //Star star = new Star();
                        //star.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                        //Mapper<IGalaxy, Star>.MapParentCelestialBodyProperties(galaxy, star);
                        //star.Name = "Our Sun (Sol)";
                        //star.Description = "The Sun at the centre of our Solar System";
                        //star.ParentGalaxy = galaxy;
                        //star.ParentGalaxyId = galaxy.Id;
                        //star.ParentSolarSystem = solarSystem;
                        //star.ParentSolarSystemId = solarSystem.Id;

                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = $"Creating CelestialBody {solarSystem.Star.Name}..." });
                        OASISResult<IStar> starResult = await ((SuperStarCore)galaxy.SuperStar.CelestialBodyCore).AddStarAsync(solarSystem.Star);

                        if (!starResult.IsError && starResult.Result != null)
                        {
                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"CelestialBody {solarSystem.Star.Name} Created." });
                            solarSystem.Star = (Star)starResult.Result;
                            DefaultStar = solarSystem.Star; //TODO: TEMP: For now the default Star in STAR ODK will be our Sun (this will be more dynamic later on).
                            STARDNA.DefaultStarId = DefaultStar.Id.ToString();

                            Mapper<IStar, SolarSystem>.MapParentCelestialBodyProperties(solarSystem.Star, solarSystem);
                            solarSystem.ParentStar = solarSystem.Star;
                            solarSystem.ParentStarId = solarSystem.Star.Id;
                            solarSystem.ParentHolon = solarSystem;
                            solarSystem.ParentHolonId = solarSystem.Id;
                            solarSystem.ParentCelestialSpace = solarSystem;
                            solarSystem.ParentCelestialSpaceId = solarSystem.Id;
                            solarSystem.ParentSolarSystem = null;
                            solarSystem.ParentSolarSystemId = Guid.Empty;

                            //TODO: Not sure if this method should also automatically create a Star inside it like the methods above do for Galaxy, Universe etc?
                            // I like how a Star creates its own Solar System from its StarDust, which is how it works in real life I am pretty sure? So I think this is best... :)
                            //TODO: For some reason I could not get Galaxy and Universe to work the same way? Need to come back to this so they all work in the same consistent manner...

                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = $"Creating CelestialSpace {solarSystem.Name}..." });
                            OASISResult<ISolarSystem> solarSystemResult = await ((StarCore)solarSystem.Star.CelestialBodyCore).AddSolarSystemAsync(solarSystem);

                            if (!solarSystemResult.IsError && solarSystemResult.Result != null)
                            {
                                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"CelestialSpace {solarSystem.Name} Created." });
                                solarSystem = (SolarSystem)solarSystemResult.Result;

                                Planet ourWorld = new Planet();
                                ourWorld.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                                ourWorld.Name = "Our World (Default Planet)";
                                ourWorld.Description = "The digital twin of our planet and the default planet.";
                                Mapper<ISolarSystem, Planet>.MapParentCelestialBodyProperties(solarSystem, ourWorld);
                                ourWorld.ParentSolarSystem = solarSystem;
                                ourWorld.ParentSolarSystemId = solarSystem.Id;
                                ourWorld.ParentHolon = solarSystem;
                                ourWorld.ParentHolonId = solarSystem.Id;
                                ourWorld.ParentCelestialSpace = solarSystem;
                                ourWorld.ParentCelestialSpaceId = solarSystem.Id;
                                // await ourWorld.InitializeAsync();

                                //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Planet (Our World)..." });
                                OASISResult<IPlanet> ourWorldResult = await ((StarCore)solarSystem.Star.CelestialBodyCore).AddPlanetAsync(ourWorld);

                                if (!ourWorldResult.IsError && ourWorldResult.Result != null)
                                {
                                    //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Our World Created." });
                                    ourWorld = (Planet)ourWorldResult.Result;
                                    STARDNA.DefaultPlanetId = ourWorld.Id.ToString();
                                }
                                else
                                {
                                    OASISResultHelper.CopyResult(ourWorldResult, result);
                                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Our World. Reason: {ourWorldResult.Message}." });
                                }
                            }
                            else
                                OASISResultHelper.CopyResult(solarSystemResult, result);
                        }
                        else
                        {
                            OASISResultHelper.CopyResult(starResult, result);
                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Star. Reason: {starResult.Message}." });
                        }
                    }
                    else
                    {
                        OASISResultHelper.CopyResult(galaxyResult, result);
                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Galaxy. Reason: {galaxyResult.Message}." });
                    }
                }
                else
                {
                    OASISResultHelper.CopyResult(galaxyClusterResult, result);
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Galaxy Cluster. Reason: {galaxyClusterResult.Message}." });
                }
                //}
                //else
                //{
                //    OASISResultHelper<IMultiverse, ICelestialBody>.CopyResult(multiverseResult, result);
                //    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Multiverse. Reason: {multiverseResult.Message}." });
                //}
            }
            else
                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Omniverse. Reason: {result.Message}." });

            STARDNAManager.SaveDNA(STARDNAPath, STARDNA);

            if (!result.IsError)
            {
                result.Message = "STAR Ignited and The OASIS Omniverse Created.";
                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Omniverse Genesis Process Complete." });
            }

            return result;
        }

        /*
        /// <summary>
        /// Create's the OASIS Omniverse along with a new default Multiverse (with it's GrandSuperStar) containing the ThirdDimension containing UniversePrime (simulation) and the MagicVerse (contains OApp's), which itself contains a default GalaxyCluster containing a default Galaxy (along with it's SuperStar) containing a default SolarSystem (along wth it's Star) containing a default planet (Our World).
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static async Task<OASISResult<ICelestialBody>> OASISOmniverseGenesisAsync()
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();

            //StarStatus = StarStatus.
            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Omniverse not found. Initiating Omniverse Genesis Process..." });

            Omniverse omniverse = new Omniverse();
            //omniverse.Name = "The OASIS Omniverse";
            //omniverse.Description = "The OASIS Omniverse that contains everything else.";
            //omniverse.IsNewHolon = true;
            //omniverse.Id = Guid.NewGuid();
            //omniverse.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);

            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Great Grand Super Star..." });

            //GreatGrandSuperStar greatGrandSuperStar = new GreatGrandSuperStar(); //GODHEAD ;-)
            //greatGrandSuperStar.IsNewHolon = true;
            //greatGrandSuperStar.Name = "GreatGrandSuperStar";
            //greatGrandSuperStar.Description = "GreatGrandSuperStar at the centre of the Omniverse (The OASIS). Can create Multiverses, Universes, Galaxies, SolarSystems, Stars, Planets (Super OAPPS) and moons (OAPPS)";
            //greatGrandSuperStar.ParentOmniverse = omniverse;
            //greatGrandSuperStar.ParentOmniverseId = omniverse.Id;
            //greatGrandSuperStar.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);

            //omniverse.GreatGrandSuperStar.IsNewHolon = true;
            //omniverse.GreatGrandSuperStar.Name = "GreatGrandSuperStar";
            //omniverse.GreatGrandSuperStar.Description = "";
            //omniverse.GreatGrandSuperStar.ParentOmniverse = omniverse;
            //omniverse.GreatGrandSuperStar.ParentOmniverseId = omniverse.Id;
            //omniverse.ParentGreatGrandSuperStar = omniverse.GreatGrandSuperStar;
            //omniverse.ParentGreatGrandSuperStarId = omniverse.GreatGrandSuperStar.Id;
            //omniverse.GreatGrandSuperStar.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
            //result = await omniverse.GreatGrandSuperStar.SaveAsync(false, false, true); //This would normally save all it's children including the Omniverse but we are creating it seperatley below so no need for that part.
            result = await omniverse.GreatGrandSuperStar.SaveAsync();

            if (!result.IsError && result.Result != null)
            {
                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Great Grand Super Star Created." });
                STARDNA.DefaultGreatGrandSuperStarId = omniverse.GreatGrandSuperStar.Id.ToString();

                //omniverse.Name = "The OASIS Omniverse";
                //omniverse.Description = "The OASIS Omniverse that contains everything else.";
                //omniverse.ParentGreatGrandSuperStar = omniverse.GreatGrandSuperStar;
                //omniverse.ParentGreatGrandSuperStarId = omniverse.GreatGrandSuperStar.Id;

                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Omniverse..." });
                OASISResult<IOmiverse> omiverseResult = await ((GreatGrandSuperStarCore)omniverse.GreatGrandSuperStar.CelestialBodyCore).AddOmiverseAsync(omniverse);

                if (!omiverseResult.IsError && omiverseResult.Result != null)
                {
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Omniverse Created." });
                    Multiverse multiverse = new Multiverse();
                    multiverse.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                    multiverse.Name = "Our Multiverse.";
                    multiverse.Description = "Our Multiverse that our Milky Way Galaxy belongs to, the default Multiverse.";
                    multiverse.ParentOmniverse = omiverseResult.Result;
                    multiverse.ParentOmniverseId = omiverseResult.Result.Id;
                    multiverse.ParentGreatGrandSuperStar = omiverseResult.Result.GreatGrandSuperStar;
                    multiverse.ParentGreatGrandSuperStarId = omiverseResult.Result.GreatGrandSuperStar.Id;
                    multiverse.GrandSuperStar.Name = "The GrandSuperStar at the centre of our Multiverse/Universe.";

                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Multiverse..." });
                    OASISResult<IMultiverse> multiverseResult = await ((GreatGrandSuperStarCore)omiverseResult.Result.GreatGrandSuperStar.CelestialBodyCore).AddMultiverseAsync(multiverse);

                    if (!multiverseResult.IsError && multiverseResult.Result != null)
                    {
                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Multiverse Created." });
                        multiverse = (Multiverse)multiverseResult.Result;
                        STARDNA.DefaultGrandSuperStarId = multiverse.GrandSuperStar.Id.ToString();

                        GalaxyCluster galaxyCluster = new GalaxyCluster();
                        galaxyCluster.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                        galaxyCluster.Name = "Our Milky Way Galaxy Cluster.";
                        galaxyCluster.Description = "Our Galaxy Cluster that our Milky Way Galaxy belongs to, the default Galaxy Cluster.";
                        Mapper<IMultiverse, GalaxyCluster>.MapParentCelestialBodyProperties(multiverse, galaxyCluster);
                        galaxyCluster.ParentMultiverse = multiverse;
                        galaxyCluster.ParentMultiverseId = multiverse.Id;
                        galaxyCluster.ParentDimension = multiverse.Dimensions.ThirdDimension;
                        galaxyCluster.ParentDimensionId = multiverse.Dimensions.ThirdDimension.Id;
                        galaxyCluster.ParentUniverseId = multiverse.Dimensions.ThirdDimension.MagicVerse.Id;
                        galaxyCluster.ParentUniverse = multiverse.Dimensions.ThirdDimension.MagicVerse;

                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Galaxy Cluster..." });
                        OASISResult<IGalaxyCluster> galaxyClusterResult = await ((GrandSuperStarCore)multiverse.GrandSuperStar.CelestialBodyCore).AddGalaxyClusterToUniverseAsync(multiverse.Dimensions.ThirdDimension.MagicVerse, galaxyCluster);

                        if (!galaxyClusterResult.IsError && galaxyClusterResult.Result != null)
                        {
                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Galaxy Cluster Created." }); ;
                            galaxyCluster = (GalaxyCluster)galaxyClusterResult.Result;

                            Galaxy galaxy = new Galaxy();
                            galaxy.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                            galaxy.Name = "Our Milky Way Galaxy";
                            galaxy.Description = "Our Milky Way Galaxy, which is the default Galaxy.";
                            Mapper<IGalaxyCluster, Galaxy>.MapParentCelestialBodyProperties(galaxyCluster, galaxy);
                            galaxy.ParentGalaxyCluster = galaxyCluster;
                            galaxy.ParentGalaxyClusterId = galaxyCluster.Id;

                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Galaxy (Milky Way)..." });
                            OASISResult<IGalaxy> galaxyResult = await ((GrandSuperStarCore)multiverse.GrandSuperStar.CelestialBodyCore).AddGalaxyToGalaxyClusterAsync(galaxyCluster, galaxy);

                            if (!galaxyResult.IsError && galaxyResult.Result != null)
                            {
                                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Galaxy Created." });
                                galaxy = (Galaxy)galaxyResult.Result;
                                STARDNA.DefaultSuperStarId = galaxy.SuperStar.Id.ToString();

                                SolarSystem solarSystem = new SolarSystem();
                                solarSystem.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                                solarSystem.Name = "Our Solar System";
                                solarSystem.Description = "Our Solar System, which is the default Solar System.";
                                solarSystem.Id = Guid.NewGuid();
                                solarSystem.IsNewHolon = true;

                                Mapper<IGalaxy, Star>.MapParentCelestialBodyProperties(galaxy, (Star)solarSystem.Star);
                                solarSystem.Star.Name = "Our Sun (Sol)";
                                solarSystem.Star.Description = "The Sun at the centre of our Solar System";
                                solarSystem.Star.ParentGalaxy = galaxy;
                                solarSystem.Star.ParentGalaxyId = galaxy.Id;
                                solarSystem.Star.ParentSolarSystem = solarSystem;
                                solarSystem.Star.ParentSolarSystemId = solarSystem.Id;

                                //Star star = new Star();
                                //star.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                                //Mapper<IGalaxy, Star>.MapParentCelestialBodyProperties(galaxy, star);
                                //star.Name = "Our Sun (Sol)";
                                //star.Description = "The Sun at the centre of our Solar System";
                                //star.ParentGalaxy = galaxy;
                                //star.ParentGalaxyId = galaxy.Id;
                                //star.ParentSolarSystem = solarSystem;
                                //star.ParentSolarSystemId = solarSystem.Id;

                                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Star (Our Sun)..." });
                                OASISResult<IStar> starResult = await ((SuperStarCore)galaxy.SuperStar.CelestialBodyCore).AddStarAsync(solarSystem.Star);

                                if (!starResult.IsError && starResult.Result != null)
                                {
                                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Star Created." });
                                    solarSystem.Star = (Star)starResult.Result;
                                    DefaultStar = solarSystem.Star; //TODO: TEMP: For now the default Star in STAR ODK will be our Sun (this will be more dynamic later on).
                                    STARDNA.DefaultStarId = DefaultStar.Id.ToString();

                                    Mapper<IStar, SolarSystem>.MapParentCelestialBodyProperties(solarSystem.Star, solarSystem);
                                    solarSystem.ParentStar = solarSystem.Star;
                                    solarSystem.ParentStarId = solarSystem.Star.Id;
                                    solarSystem.ParentSolarSystem = null;
                                    solarSystem.ParentSolarSystemId = Guid.Empty;

                                    //TODO: Not sure if this method should also automatically create a Star inside it like the methods above do for Galaxy, Universe etc?
                                    // I like how a Star creates its own Solar System from its StarDust, which is how it works in real life I am pretty sure? So I think this is best... :)
                                    //TODO: For some reason I could not get Galaxy and Universe to work the same way? Need to come back to this so they all work in the same consistent manner...

                                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Solar System..." });
                                    OASISResult<ISolarSystem> solarSystemResult = await ((StarCore)solarSystem.Star.CelestialBodyCore).AddSolarSystemAsync(solarSystem);

                                    if (!solarSystemResult.IsError && solarSystemResult.Result != null)
                                    {
                                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Solar System Created." });
                                        solarSystem = (SolarSystem)solarSystemResult.Result;

                                        Planet ourWorld = new Planet();
                                        ourWorld.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                                        ourWorld.Name = "Our World";
                                        ourWorld.Description = "The digital twin of our planet and the default planet.";
                                        Mapper<ISolarSystem, Planet>.MapParentCelestialBodyProperties(solarSystem, ourWorld);
                                        ourWorld.ParentSolarSystem = solarSystem;
                                        ourWorld.ParentSolarSystemId = solarSystem.Id;
                                        // await ourWorld.InitializeAsync();

                                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Planet (Our World)..." });
                                        OASISResult<IPlanet> ourWorldResult = await ((StarCore)solarSystem.Star.CelestialBodyCore).AddPlanetAsync(ourWorld);

                                        if (!ourWorldResult.IsError && ourWorldResult.Result != null)
                                        {
                                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Our World Created." });
                                            ourWorld = (Planet)ourWorldResult.Result;
                                            STARDNA.DefaultPlanetId = ourWorld.Id.ToString();
                                        }
                                        else
                                        {
                                            OASISResultHelper<IPlanet, ICelestialBody>.CopyResult(ourWorldResult, result);
                                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Our World. Reason: {ourWorldResult.Message}." });
                                        }
                                    }
                                    else
                                        OASISResultHelper<ISolarSystem, ICelestialBody>.CopyResult(solarSystemResult, result);
                                }
                                else
                                {
                                    OASISResultHelper<IStar, ICelestialBody>.CopyResult(starResult, result);
                                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Star. Reason: {starResult.Message}." });
                                }
                            }
                            else
                            {
                                OASISResultHelper<IGalaxy, ICelestialBody>.CopyResult(galaxyResult, result);
                                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Galaxy. Reason: {galaxyResult.Message}." });
                            }
                        }
                        else
                        {
                            OASISResultHelper<IGalaxyCluster, ICelestialBody>.CopyResult(galaxyClusterResult, result);
                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Galaxy Cluster. Reason: {galaxyClusterResult.Message}." });
                        }
                    }
                    else
                    {
                        OASISResultHelper<IMultiverse, ICelestialBody>.CopyResult(multiverseResult, result);
                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Multiverse. Reason: {multiverseResult.Message}." });
                    }
                }
                else
                {
                    OASISResultHelper<IOmiverse, ICelestialBody>.CopyResult(omiverseResult, result);
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Omniverse. Reason: {omiverseResult.Message}." });
                }
            }

            SaveDNA();

            if (!result.IsError)
            {
                result.Message = "STAR Ignited and The OASIS Omniverse Created.";
                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Omniverse Genesis Process Complete." });
            }

            return result;
        }*/

        private static void HandleErrorMessage<T>(ref OASISResult<T> result, string errorMessage)
        {
            OnStarError?.Invoke(null, new StarErrorEventArgs() { Reason = errorMessage });
            OASISErrorHandling.HandleError(ref result, errorMessage);
        }

        private static void CopyFolder(string OAPPNameSpace, DirectoryInfo source, DirectoryInfo target)
        {
            foreach (FileInfo file in source.GetFiles())
            {
                if (!File.Exists(Path.Combine(target.FullName, file.Name)))
                {
                    if (file.Extension == ".csproj")
                        file.CopyTo(Path.Combine(target.FullName, string.Concat(OAPPNameSpace, ".csproj")));
                    else
                        file.CopyTo(Path.Combine(target.FullName, file.Name));
                }
            }

            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                if (dir.Name != "bin" && dir.Name != "obj")
                {
                    if (!Directory.Exists(Path.Combine(target.FullName, dir.Name)))
                        CopyFolder(OAPPNameSpace, dir, target.CreateSubdirectory(dir.Name));
                }
            }
        }

        private static void ApplyOAPPTemplate(GenesisType genesisType, string OAPPFolder, string oAppNameSpace, string oAppName, string celestialBodyName, string zomeName, string holonName, string firstStringProperty, List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, bool root = true)
        {
            // Generate library references and stubs if GeneratedProxies folder exists
            string generatedProxiesPath = Path.Combine(OAPPFolder, "GeneratedProxies");
            string libraryUsingStatements = "";
            string libraryMethodStubs = "";
            
            if (Directory.Exists(generatedProxiesPath))
            {
                var proxyFiles = Directory.GetFiles(generatedProxiesPath, "*Proxy.cs", SearchOption.TopDirectoryOnly);
                
                if (proxyFiles.Length > 0)
                {
                    // Generate using statement
                    libraryUsingStatements = "using GeneratedProxies;\nusing NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop;\n";
                    
                    // Generate method stubs for each library
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("\n        // Library Integration Methods");
                    sb.AppendLine("        // These methods demonstrate how to use the imported libraries");
                    
                    foreach (var proxyFile in proxyFiles)
                    {
                        string proxyClassName = Path.GetFileNameWithoutExtension(proxyFile);
                        string libraryName = proxyClassName.Replace("Proxy", "");
                        
                        // Example 1: Simple usage (default constructor - just works!)
                        sb.AppendLine($"        // Example 1: Simple usage - proxy handles everything automatically");
                        sb.AppendLine($"        private static async Task Use{libraryName}LibrarySimple()");
                        sb.AppendLine("        {");
                        sb.AppendLine($"            // Create proxy instance - it automatically loads the library on first use");
                        sb.AppendLine($"            var proxy = new {proxyClassName}();");
                        sb.AppendLine($"            ");
                        sb.AppendLine($"            // Call library methods - library is loaded automatically if needed");
                        sb.AppendLine($"            // var result = await proxy.SomeMethodAsync(param1, param2);");
                        sb.AppendLine($"            // if (!result.IsError && result.Result != null)");
                        sb.AppendLine($"            //     Console.WriteLine($\"Result: {{result.Result}}\");");
                        sb.AppendLine("        }");
                        sb.AppendLine("");
                        
                        // Example 2: Advanced usage (only needed for testing or custom setup)
                        sb.AppendLine($"        // Example 2: Advanced usage - only needed for unit testing or custom provider setup");
                        sb.AppendLine($"        // Note: The default constructor is usually sufficient. Only use this for:");
                        sb.AppendLine($"        // - Unit testing with a mock interop manager");
                        sb.AppendLine($"        // - Custom provider configuration");
                        sb.AppendLine($"        // - Reusing a library that was already loaded elsewhere");
                        sb.AppendLine($"        private static async Task Use{libraryName}LibraryAdvanced()");
                        sb.AppendLine("        {");
                        sb.AppendLine($"            // Get shared interop manager (same instance used by all proxies)");
                        sb.AppendLine($"            var interopManager = await LibraryInteropFactory.CreateDefaultManagerAsync();");
                        sb.AppendLine($"            if (interopManager.IsError || interopManager.Result == null)");
                        sb.AppendLine($"            {{");
                        sb.AppendLine($"                Console.WriteLine($\"Error: {{interopManager.Message}}\");");
                        sb.AppendLine($"                return;");
                        sb.AppendLine($"            }}");
                        sb.AppendLine($"            ");
                        sb.AppendLine($"            // If library was already loaded, you can reuse it");
                        sb.AppendLine($"            // var loadResult = await interopManager.Result.LoadLibraryAsync(\"path/to/{libraryName}.dll\");");
                        sb.AppendLine($"            // if (!loadResult.IsError && loadResult.Result != null)");
                        sb.AppendLine($"            // {{");
                        sb.AppendLine($"            //     // Create proxy with pre-loaded library");
                        sb.AppendLine($"            //     var proxy = new {proxyClassName}(interopManager.Result, loadResult.Result.LibraryId);");
                        sb.AppendLine($"            //     // Use proxy methods");
                        sb.AppendLine($"            // }}");
                        sb.AppendLine("        }");
                        sb.AppendLine("");
                    }
                    
                    libraryMethodStubs = sb.ToString();
                }
            }
            
            foreach (DirectoryInfo dir in new DirectoryInfo(OAPPFolder).GetDirectories())
            {
                if (dir.Name != "bin" && dir.Name != "obj")
                    ApplyOAPPTemplate(genesisType, dir.FullName, oAppNameSpace, oAppName, celestialBodyName, zomeName, holonName, firstStringProperty, metaHolonTagMappings, metaTagMappings, false);
            }
            
            if (!OAPPFolder.Contains(STARDNA.OAPPGeneratedCodeFolder))
            {                
                foreach (FileInfo file in new DirectoryInfo(OAPPFolder).GetFiles("*.csproj"))
                {
                    int lineNumber = 1;
                    string line = null;

                    using (TextReader tr = File.OpenText(file.FullName))
                    using (TextWriter tw = File.CreateText(string.Concat(file.FullName, ".temp")))
                    {
                        while ((line = tr.ReadLine()) != null)
                        {
                            line = line.Replace("<Compile Remove=\"Program.cs\" />", "");
                           
                            tw.WriteLine(line);
                            lineNumber++;
                        }
                    }



                    File.Delete(file.FullName);
                    File.Move(string.Concat(file.FullName, ".temp"), file.FullName);
                }

                //TODO: use multiple file extention wildcards so only need one file loop...
                bool foundOASISDNA = false;
                foreach (FileInfo file in new DirectoryInfo(OAPPFolder).GetFiles("*.cs"))
                {
                    int lineNumber = 1;
                    string line = null;

                    if (file.FullName.Contains("OASIS_DNA.json"))
                        foundOASISDNA = true;

                    using (TextReader tr = File.OpenText(file.FullName))
                    using (TextWriter tw = File.CreateText(string.Concat(file.FullName, ".temp")))
                    {
                        bool celestialBodyBlock = false;

                        while ((line = tr.ReadLine()) != null)
                        {
                            if (metaHolonTagMappings != null && metaHolonTagMappings.Count > 0)
                            {
                                string initHolons = "";
                                foreach (MetaHolonTag metaHolonTag in metaHolonTagMappings)
                                {
                                    if (!string.IsNullOrEmpty(initHolons))
                                        initHolons = string.Concat(initHolons, "\n");

                                    initHolons = string.Concat(initHolons, metaHolonTag.HolonName.ToPascalCase(), " ", metaHolonTag.HolonName.ToCamelCase(), " = new ", metaHolonTag.HolonName.ToPascalCase(), "();");

                                    if (line.Contains(metaHolonTag.MetaTag))
                                        line = line.Replace(string.Concat("{{", metaHolonTag.MetaTag, "}}"), string.Concat(metaHolonTag.HolonName.ToPascalCase(), ".Instance.", metaHolonTag.NodeName));
                                        //line = line.Replace(string.Concat("{{", metaHolonTag.MetaTag, "}}"), string.Concat(metaHolonTag.HolonName.ToCamelCase(), ".", metaHolonTag.NodeName));
                                }

                                if (!string.IsNullOrEmpty(initHolons) && line.Contains("{INITCUSTOMTAGHOLONS}"))
                                    line = line.Replace("{INITCUSTOMTAGHOLONS}", initHolons);
                            }

                            if (metaTagMappings != null && metaTagMappings.Count > 0)
                            {
                                string initHolons = "";
                                foreach (string key in metaTagMappings.Keys)
                                {
                                    if (line.Contains(key))
                                        line = line.Replace(string.Concat("[[", key, "]]"), metaTagMappings[key]);
                                }
                            }

                            celestialBodyName = celestialBodyName.Replace(" ", "");
                            line = line.Replace("{OAPPNAMESPACE}", oAppNameSpace);
                            line = line.Replace("{OAPPNAME}", oAppName);

                            if (line.Contains("CelestialBodyOnly:BEGIN"))
                            {
                                celestialBodyBlock = true;
                                continue;

                            }
                            else if (line.Contains("CelestialBodyOnly:END"))
                            {
                                celestialBodyBlock = false;
                                continue;
                            }
                            else if (celestialBodyBlock && genesisType == GenesisType.ZomesAndHolonsOnly)
                                continue;

                            else
                            {
                                if (genesisType == GenesisType.ZomesAndHolonsOnly)
                                {
                                    line = line.Replace("//ZomesAndHolonsOnly:", "");

                                    if (line.Contains("CelestialBodyOnly"))
                                        continue;
                                }
                                else
                                {
                                    line = line.Replace("{CELESTIALBODY}", string.Concat(oAppNameSpace.ToPascalCase() , ".", celestialBodyName.ToPascalCase())).Replace("//CelestialBodyOnly:", "");
                                    line = line.Replace("{CELESTIALBODYVAR}", celestialBodyName.ToCamelCase()).Replace("//CelestialBodyOnly:", "");

                                    if (line.Contains("ZomesAndHolonsOnly"))
                                        continue;
                                }

                                line = line.Replace("{ZOME1}", zomeName.ToPascalCase());
                                line = line.Replace("{HOLON1}", holonName.ToPascalCase());
                                line = line.Replace("{HOLON1_STRINGPROPERTY1}", firstStringProperty.ToPascalCase());
                                //TODO: Add rest of the props, holons, zomes, etc...
                            }

                            // Replace library reference tags
                            if (!string.IsNullOrEmpty(libraryUsingStatements) && line.Contains("{LIBRARYUSINGSTATEMENTS}"))
                                line = line.Replace("{LIBRARYUSINGSTATEMENTS}", libraryUsingStatements);
                            
                            if (!string.IsNullOrEmpty(libraryMethodStubs) && line.Contains("{LIBRARYMETHODSTUBS}"))
                                line = line.Replace("{LIBRARYMETHODSTUBS}", libraryMethodStubs);

                            tw.WriteLine(line);
                            lineNumber++;
                        }
                    }

                    File.Delete(file.FullName);
                    File.Move(string.Concat(file.FullName, ".temp"), file.FullName);
                }

                if (!foundOASISDNA && root)
                {
                    if (File.Exists(Path.Combine(OAPPFolder, "DNA\\OASIS_DNA.json")))
                        File.Delete(Path.Combine(OAPPFolder, "DNA\\OASIS_DNA.json"));

                    if (File.Exists(Path.Combine(OAPPFolder, "DNA\\STAR_DNA.json")))
                        File.Delete(Path.Combine(OAPPFolder, "DNA\\STAR_DNA.json"));

                    if (!File.Exists(Path.Combine(OAPPFolder, "DNA")))
                        Directory.CreateDirectory(Path.Combine(OAPPFolder, "DNA"));

                    File.Copy(OASISDNAPath, Path.Combine(OAPPFolder, "DNA\\OASIS_DNA.json"));
                    File.Copy(STARDNAPath, Path.Combine(OAPPFolder, "DNA\\STAR_DNA.json"));
                    //File.Copy(OASISDNAPath, Path.Combine(OAPPFolder, "OASIS_DNA.json"));
                }
            }
        }

        //private void ReplaceInTemplate(string OAPPFolder, string fileExtention)
        //{
        //    foreach (FileInfo file in new DirectoryInfo(OAPPFolder).GetFiles($"*.{fileExtention}"))
        //    {
        //        int lineNumber = 1;
        //        string line = null;

        //        using (TextReader tr = File.OpenText(file.FullName))
        //        using (TextWriter tw = File.CreateText(string.Concat(file.FullName, ".temp")))
        //        {
        //            while ((line = tr.ReadLine()) != null)
        //            {
        //                line = line.Replace("<Compile Remove=\"Program.cs\" />", "");

        //                tw.WriteLine(line);
        //                lineNumber++;
        //            }
        //        }

        //        File.Delete(file.FullName);
        //        File.Move(string.Concat(file.FullName, ".temp"), file.FullName);
        //    }
        //}

        private static async Task<OASISResult<string>> InitOAPPFolderAsync(OAPPType OAPPType, string OAPPName, string genesisFolder, string genesisNameSpace, Guid OAPPTemplateId, int OAPPTemplateVersion, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            string errorMessage = "An error occured in InitOAPPFolderAsync. Reason:";

            try
            {
                string OAPPFolder = Path.Combine(genesisFolder, OAPPName);

                if (Directory.Exists(OAPPFolder))
                    Directory.Delete(OAPPFolder, true);

                Directory.CreateDirectory(OAPPFolder);

                if (OAPPTemplateId != Guid.Empty)
                {
                    OASISResult<InstalledOAPPTemplate> installedOAPPTemplateResult = await STARAPI.OAPPTemplates.LoadInstalledAsync(BeamedInAvatar.Id, OAPPTemplateId, true, OAPPTemplateVersion, providerType);

                    if (installedOAPPTemplateResult != null && installedOAPPTemplateResult.Result != null && !installedOAPPTemplateResult.IsError)
                        CopyFolder(genesisNameSpace, new DirectoryInfo(installedOAPPTemplateResult.Result.InstalledPath), new DirectoryInfo(OAPPFolder));
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling STARAPI.OAPPTemplates.LoadInstalledOAPPTemplateAsync. Reason: {installedOAPPTemplateResult.Message}");
                        return result;
                    }
                }

                genesisFolder = string.Concat(OAPPFolder, "\\", STARDNA.OAPPGeneratedCodeFolder);

                if (!Directory.Exists(genesisFolder))
                    Directory.CreateDirectory(genesisFolder);

                result.Result = OAPPFolder;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: Reason: {ex}");
            }

            return result;
        }
    }
}
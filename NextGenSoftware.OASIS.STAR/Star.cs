using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.OASISAPIManager;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.EventArgs;
using NextGenSoftware.OASIS.STAR.ErrorEventArgs;
using NextGenSoftware.OASIS.STAR.Enums;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STAR
{
    public static class STAR
    {
        const string STAR_DNA_DEFAULT_PATH = "DNA\\STAR_DNA.json";
        const string OASIS_DNA_DEFAULT_PATH = "DNA\\OASIS_DNA.json";

        private static StarStatus _status;
        private static Guid _starId = Guid.Empty;
        private static OASISAPI _OASISAPI = null;
        private static IPlanet _defaultPlanet = null;
        private static ISuperStar _defaultSuperStar = null;
        private static IGrandSuperStar _defaultGrandSuperStar = null;
        private static IGreatGrandSuperStar _defaultGreatGrandSuperStar = null;

        public static string STARDNAPath { get; set; } = STAR_DNA_DEFAULT_PATH;
        public static string OASISDNAPath { get; set; } = OASIS_DNA_DEFAULT_PATH;
        public static STARDNA STARDNA { get; set; }

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

        public static IAvatar LoggedInAvatar { get; set; }
        public static IAvatarDetail LoggedInAvatarDetail { get; set; }

        public static OASISAPI OASISAPI
        {
            get
            {
                if (_OASISAPI == null)
                    _OASISAPI = new OASISAPI();

                return _OASISAPI;
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

        public static async Task<OASISResult<IOmiverse>> IgniteStarAsync(string STARDNAPath = STAR_DNA_DEFAULT_PATH, string OASISDNAPath = OASIS_DNA_DEFAULT_PATH, string starId = null)
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
                LoadDNA();
            else
            {
                STARDNA = new STARDNA();
                SaveDNA();
            }

            ValidateSTARDNA(STARDNA);
            Status = StarStatus.BootingOASIS;
            OASISResult<bool> oasisResult = await BootOASISAsync(OASISDNAPath);

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
                Status = StarStatus.Ingited;
                OnStarIgnited.Invoke(null, new StarIgnitedEventArgs() { Message = result.Message });
                IsStarIgnited = true;
            }

            return result;
        }

        public static OASISResult<IOmiverse> IgniteStar(string STARDNAPath = STAR_DNA_DEFAULT_PATH, string OASISDNAPath = OASIS_DNA_DEFAULT_PATH, string starId = null)
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
                LoadDNA();
            else
            {
                STARDNA = new STARDNA();
                SaveDNA();
            }

            ValidateSTARDNA(STARDNA);
            Status = StarStatus.BootingOASIS;
            OASISResult<bool> oasisResult = BootOASIS(OASISDNAPath);

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

            result = IgniteInnerStar(result);

            if (result.IsError)
                Status = StarStatus.Error;
            else
            {
                Status = StarStatus.Ingited;
                OnStarIgnited.Invoke(null, new StarIgnitedEventArgs() { Message = result.Message });
                IsStarIgnited = true;
            }

            return result;
        }

        public static OASISResult<bool> ExtinguishSuperStar()
        {
            return OASISAPI.ShutdownOASIS();
        }

        public static async Task<OASISResult<bool>> ExtinguishSuperStarAsync()
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

        public static OASISResult<IAvatar> CreateAvatar(string title, string firstName, string lastName, string email, string username, string password, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
        {
            if (!IsStarIgnited)
                IgniteStar();

            return OASISAPI.Avatar.Register(title, firstName, lastName, email, password, username, AvatarType.User, OASISType.STARCLI, cliColour, favColour);
        }

        public static async Task<OASISResult<IAvatar>> CreateAvatarAsync(string title, string firstName, string lastName, string email, string username, string password, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
        {
            if (!IsStarIgnited)
                await IgniteStarAsync();

            return await OASISAPI.Avatar.RegisterAsync(title, firstName, lastName, email, password, username, AvatarType.User, OASISType.STARCLI, cliColour, favColour);
        }

        public static async Task<OASISResult<IAvatar>> BeamInAsync(string username, string password)
        {
            string hostName = Dns.GetHostName();
            string IPAddress = Dns.GetHostEntry(hostName).AddressList[0].ToString();

            if (!IsStarIgnited)
                await IgniteStarAsync();

            OASISResult<IAvatar> result = await OASISAPI.Avatar.AuthenticateAsync(username, password, IPAddress);

            if (!result.IsError)
            {
                LoggedInAvatar = (Avatar)result.Result;
                OASISAPI.LogAvatarIntoOASISManagers(); //TODO: Is there a better way of doing this?

                OASISResult<IAvatarDetail> loggedInAvatarDetailResult = await OASISAPI.Avatar.LoadAvatarDetailAsync(LoggedInAvatar.Id);

                if (!loggedInAvatarDetailResult.IsError && loggedInAvatarDetailResult.Result != null)
                    LoggedInAvatarDetail = loggedInAvatarDetailResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error Occured In BeamInAsync Calling LoadAvatarDetailAsync. Reason: {loggedInAvatarDetailResult.Message}");
            }

            return result;
        }

        public static OASISResult<IAvatar> BeamIn(string username, string password)
        {
            string IPAddress = "";
            string hostName = Dns.GetHostName();
            IPHostEntry entry = Dns.GetHostEntry(hostName);

            if (entry != null && entry.AddressList.Length > 1)
                IPAddress = Dns.GetHostEntry(hostName).AddressList[1].ToString();

            if (!IsStarIgnited)
                IgniteStar();

            OASISResult<IAvatar> result = OASISAPI.Avatar.Authenticate(username, password, IPAddress);

            if (!result.IsError)
            {
                LoggedInAvatar = (Avatar)result.Result;

                OASISResult<IAvatarDetail> loggedInAvatarDetailResult = OASISAPI.Avatar.LoadAvatarDetail(LoggedInAvatar.Id);

                if (!loggedInAvatarDetailResult.IsError && loggedInAvatarDetailResult.Result != null)
                    LoggedInAvatarDetail = loggedInAvatarDetailResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error Occured In BeamIn Calling LoadAvatarDetail. Reason: {loggedInAvatarDetailResult.Message}");
            }
            return result;
        }

        public static OASISResult<CoronalEjection> Light(OAPPType OAPPType, GenesisType genesisType, string name, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "")
        {
            return Light(OAPPType, genesisType, name, (ICelestialBody)null, celestialBodyDNAFolder, genesisFolder, genesisNameSpace);
        }

        public static OASISResult<CoronalEjection> Light(OAPPType OAPPType, GenesisType genesisType, string name, IStar starToAddPlanetTo = null, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "")
        {
            return Light(OAPPType, genesisType, name, (ICelestialBody)starToAddPlanetTo, celestialBodyDNAFolder, genesisFolder, genesisNameSpace);
        }

        public static OASISResult<CoronalEjection> Light(OAPPType OAPPType, GenesisType genesisType, string name, IPlanet planetToAddMoonTo = null, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "")
        {
            return Light(OAPPType, genesisType, name, (ICelestialBody)planetToAddMoonTo, celestialBodyDNAFolder, genesisFolder, genesisNameSpace);
        }

        //TODO: Create non async version of Light();
        public static OASISResult<CoronalEjection> Light(OAPPType OAPPType, GenesisType genesisType, string name, ICelestialBody celestialBodyParent = null, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "")
        {
            //TODO: Implement Light version ASAP (I just don't know about duplicating every method again and again?!)
            return LightAsync(OAPPType, genesisType, name, celestialBodyParent, celestialBodyDNAFolder, genesisFolder, genesisNameSpace).Result;
        }

        //public static async Task<OASISResult<CoronalEjection>> LightAsync(OAPPType OAPPType, GenesisType genesisType, string name, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisRustFolder = "", string genesisNameSpace = "")
        public static async Task<OASISResult<CoronalEjection>> LightAsync(OAPPType OAPPType, GenesisType genesisType, string name, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "")
        {
            return await LightAsync(OAPPType, genesisType, name, (ICelestialBody)null, celestialBodyDNAFolder, genesisFolder, genesisNameSpace);
        }

        public static async Task<OASISResult<CoronalEjection>> LightAsync(OAPPType OAPPType, GenesisType genesisType, string name, IStar starToAddPlanetTo = null, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "")
        {
            return await LightAsync(OAPPType, genesisType, name, (ICelestialBody)starToAddPlanetTo, celestialBodyDNAFolder, genesisFolder, genesisNameSpace);
        }

        public static async Task<OASISResult<CoronalEjection>> LightAsync(OAPPType OAPPType, GenesisType genesisType, string name, IPlanet planetToAddMoonTo = null, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "")
        {
            return await LightAsync(OAPPType, genesisType, name, (ICelestialBody)planetToAddMoonTo, celestialBodyDNAFolder, genesisFolder, genesisNameSpace);
        }

        //public static async Task<OASISResult<CoronalEjection>> LightAsync(string oAPPName, OAPPType OAPPType, string zomeAndHolonDNAFolder = "", string genesisFolder = "", string genesisRustFolder = "", string genesisNameSpace = "")
        public static async Task<OASISResult<CoronalEjection>> LightAsync(string oAPPName, OAPPType OAPPType, string zomeAndHolonDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "")
        {
            return await LightAsync(OAPPType, GenesisType.ZomesAndHolonsOnly, oAPPName, zomeAndHolonDNAFolder, genesisFolder, genesisNameSpace);
        }

        //TODO: Create non async version of Light();
        public static async Task<OASISResult<CoronalEjection>> LightAsync(OAPPType OAPPType, GenesisType genesisType, string name, ICelestialBody celestialBodyParent = null, string celestialBodyDNAFolder = "", string genesisFolder = "",  string genesisNameSpace = "")
        {
            //OASISResult<CoronalEjection> result = new OASISResult<CoronalEjection>();
            ICelestialBody newBody = null;
            bool holonReached = false;
            string zomeBufferCsharp = "";
            string izomeBufferCsharp = "";
            string holonBufferRust = "";
            string holonBufferCsharp = "";
            string iholonBufferCsharp = "";
            string libBuffer = "";
            string holonName = "";
            string zomeName = "";
            string holonFieldsClone = "";
            int nextLineToWrite = 0;
            bool firstField = true;
            bool secondField = false;
            string celestialBodyBufferCsharp = "";
            bool firstHolon = true;
            string rustcelestialBodyDNAFolder = string.Empty;
            string OAPPFolder = "";
            List<string> holonNames = new List<string>();
            string firstStringProperty = "";

            if (LoggedInAvatarDetail == null)
                return new OASISResult<CoronalEjection>() { IsError = true, Message = "Avatar is not logged in. Please log in before calling this command." };

            if (LoggedInAvatarDetail.Level < 77 && genesisType == GenesisType.Star)
                return new OASISResult<CoronalEjection>() { IsError = true, Message = "Avatar must have reached level 77 before they can create stars. Please create a planet or moon instead..." };

            if (LoggedInAvatarDetail.Level < 33 && genesisType == GenesisType.Planet)
                return new OASISResult<CoronalEjection>() { IsError = true, Message = "Avatar must have reached level 33 before they can create planets. Please create a moon instead..." };

            //if (celestialBodyParent == null && type == GenesisType.Moon)
            //    return new OASISResult<CoronalEjection>() { IsError = true, Message = "You must specify the planet to add the moon to." };

            if (!IsStarIgnited)
                await IgniteStarAsync();

            if (string.IsNullOrEmpty(celestialBodyDNAFolder))
                celestialBodyDNAFolder = STARDNA.CelestialBodyDNA;

            if (string.IsNullOrEmpty(genesisFolder))
                genesisFolder = STARDNA.GenesisFolder;

            if (string.IsNullOrEmpty(genesisNameSpace))
                genesisNameSpace = STARDNA.GenesisNamespace;

            if (DefaultStar == null)
            {
                OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();
                result = await IgniteInnerStarAsync(result);

                if (result.IsError)
                    return new OASISResult<CoronalEjection>() { IsError = true, Message = string.Concat("Error Igniting Inner Star. Reason: ", result.Message) };
            }

            ValidateLightDNA(celestialBodyDNAFolder, genesisFolder);

            //switch (STARDNA.HolochainVersion.ToUpper())
            //{
            //    case "REDUX":
            //        rustcelestialBodyDNAFolder = $"{STARDNA.BasePath}\\{STARDNA.RustDNAReduxTemplateFolder}";
            //        break;

            //    case "RSM":
            //        rustcelestialBodyDNAFolder = $"{STARDNA.BasePath}\\{STARDNA.RustDNARSMTemplateFolder}";
            //        break;
            //}

            rustcelestialBodyDNAFolder = $"{STARDNA.BasePath}\\{STARDNA.RustDNARSMTemplateFolder}";

            string libTemplate = new FileInfo(string.Concat(rustcelestialBodyDNAFolder, "\\", STARDNA.RustTemplateLib)).OpenText().ReadToEnd();
            string createTemplate = new FileInfo(string.Concat(rustcelestialBodyDNAFolder, "\\", STARDNA.RustTemplateCreate)).OpenText().ReadToEnd();
            string readTemplate = new FileInfo(string.Concat(rustcelestialBodyDNAFolder, "\\", STARDNA.RustTemplateRead)).OpenText().ReadToEnd();
            string updateTemplate = new FileInfo(string.Concat(rustcelestialBodyDNAFolder, "\\", STARDNA.RustTemplateUpdate)).OpenText().ReadToEnd();
            string deleteTemplate = new FileInfo(string.Concat(rustcelestialBodyDNAFolder, "\\", STARDNA.RustTemplateDelete)).OpenText().ReadToEnd();
            string listTemplate = new FileInfo(string.Concat(rustcelestialBodyDNAFolder, "\\", STARDNA.RustTemplateList)).OpenText().ReadToEnd();
            string validationTemplate = new FileInfo(string.Concat(rustcelestialBodyDNAFolder, "\\", STARDNA.RustTemplateValidation)).OpenText().ReadToEnd();
            string holonTemplateRust = new FileInfo(string.Concat(rustcelestialBodyDNAFolder, "\\", STARDNA.RustTemplateHolon)).OpenText().ReadToEnd();
            string intTemplateRust = new FileInfo(string.Concat(rustcelestialBodyDNAFolder, "\\", STARDNA.RustTemplateInt)).OpenText().ReadToEnd();
            string stringTemplateRust = new FileInfo(string.Concat(rustcelestialBodyDNAFolder, "\\", STARDNA.RustTemplateString)).OpenText().ReadToEnd();
            string boolTemplateRust = new FileInfo(string.Concat(rustcelestialBodyDNAFolder, "\\", STARDNA.RustTemplateBool)).OpenText().ReadToEnd();

            string iHolonTemplate = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateIHolonDNA)).OpenText().ReadToEnd();
            string holonTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateHolonDNA)).OpenText().ReadToEnd();
            string iZomeTemplate = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateIZomeDNA)).OpenText().ReadToEnd();
            string zomeTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateZomeDNA)).OpenText().ReadToEnd();
            string iCelestialBodyTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateICelestialBodyDNA)).OpenText().ReadToEnd();
            string celestialBodyTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateCelestialBodyDNA)).OpenText().ReadToEnd();
            string loadHolonTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateLoadHolonDNA)).OpenText().ReadToEnd();
            string saveHolonTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateSaveHolonDNA)).OpenText().ReadToEnd();
            string iloadHolonTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateILoadHolonDNA)).OpenText().ReadToEnd();
            string isaveHolonTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateISaveHolonDNA)).OpenText().ReadToEnd();

            string IntTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateInt)).OpenText().ReadToEnd();
            string StringTemplateCSharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateString)).OpenText().ReadToEnd();
            string BoolTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateBool)).OpenText().ReadToEnd();

            //If folder is not passed in via command line args then use default in config file.
            if (string.IsNullOrEmpty(celestialBodyDNAFolder))
                celestialBodyDNAFolder = $"{STARDNA.BasePath}\\{STARDNA.CelestialBodyDNA}";

            if (string.IsNullOrEmpty(genesisFolder))
                genesisFolder = $"{STARDNA.BasePath}\\{STARDNA.GenesisFolder}";

            if (string.IsNullOrEmpty(genesisNameSpace))
                genesisNameSpace = $"{STARDNA.BasePath}\\{STARDNA.GenesisNamespace}";

            if (string.IsNullOrEmpty(genesisNameSpace))
                genesisNameSpace = string.Concat(name, "OApp");

            //Setup the OApp files from the relevant template.
            if (OAPPType != OAPPType.GeneratedCodeOnly)
            {
                OAPPFolder = string.Concat(genesisFolder, "\\", name, " OApp");

                if (Directory.Exists(OAPPFolder))
                    Directory.Delete(OAPPFolder, true);
                    
                Directory.CreateDirectory(string.Concat(OAPPFolder));

                switch (OAPPType)
                {
                    case OAPPType.Blazor:
                        CopyFolder(genesisNameSpace, new DirectoryInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.OAPPBlazorTemplateDNA)), new DirectoryInfo(OAPPFolder));
                        break;

                    case OAPPType.Console:
                        CopyFolder(genesisNameSpace, new DirectoryInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.OAPPConsoleTemplateDNA)), new DirectoryInfo(OAPPFolder));
                        break;
                }

                genesisFolder = string.Concat(OAPPFolder, "\\", STARDNA.OAPPGeneratedCodeFolder);

                if (!Directory.Exists(genesisFolder))
                    Directory.CreateDirectory(genesisFolder);
            }

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

            if (!Directory.Exists(string.Concat(genesisFolder, "\\Rust")))
                Directory.CreateDirectory(string.Concat(genesisFolder, "\\Rust")); //TODO: Soon this will be generic depending on what the target OASIS Providers STAR has been configured to generate OApp code for...

            DirectoryInfo dirInfo = new DirectoryInfo(celestialBodyDNAFolder);
            FileInfo[] files = dirInfo.GetFiles();

            switch (genesisType)
            {
                case GenesisType.Moon:
                    {
                        newBody = new Moon();

                        if (celestialBodyParent == null)
                            celestialBodyParent = DefaultPlanet;

                        Mapper<IPlanet, Moon>.MapParentCelestialBodyProperties((IPlanet)celestialBodyParent, (Moon)newBody);
                        newBody.ParentHolon = celestialBodyParent;
                        newBody.ParentHolonId = celestialBodyParent.Id;
                        newBody.ParentCelestialBody = celestialBodyParent;
                        newBody.ParentCelestialBodyId = celestialBodyParent.Id;
                        newBody.ParentPlanet = (IPlanet)celestialBodyParent;
                        newBody.ParentPlanetId = celestialBodyParent.Id;
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
                        newBody.ParentHolonId = celestialBodyParent.Id;
                        newBody.ParentCelestialBody = celestialBodyParent;
                        newBody.ParentCelestialBodyId = celestialBodyParent.Id;
                        newBody.ParentStar = (IStar)celestialBodyParent;
                        newBody.ParentStarId = celestialBodyParent.Id;
                    }
                break;

                case GenesisType.Star:
                    {
                        newBody = new Star();

                        if (celestialBodyParent == null)
                            celestialBodyParent = DefaultSuperStar;

                        Mapper<ISuperStar, Star>.MapParentCelestialBodyProperties((ISuperStar)celestialBodyParent, (Star)newBody);
                        newBody.ParentHolon = celestialBodyParent;
                        newBody.ParentHolonId = celestialBodyParent.Id;
                        newBody.ParentCelestialBody = celestialBodyParent;
                        newBody.ParentCelestialBodyId = celestialBodyParent.Id;
                        newBody.ParentSuperStar = (ISuperStar)celestialBodyParent;
                        newBody.ParentSuperStarId = celestialBodyParent.Id;
                    }
                break;

                case GenesisType.Galaxy:
                    {
                        newBody = new SuperStar();

                        if (celestialBodyParent == null)
                            celestialBodyParent = DefaultGrandSuperStar;

                        Mapper<IGrandSuperStar, SuperStar>.MapParentCelestialBodyProperties((IGrandSuperStar)celestialBodyParent, (SuperStar)newBody);
                        newBody.ParentHolon = celestialBodyParent;
                        newBody.ParentHolonId = celestialBodyParent.Id;
                        newBody.ParentCelestialBody = celestialBodyParent;
                        newBody.ParentCelestialBodyId = celestialBodyParent.Id;
                        newBody.ParentGrandSuperStar = (IGrandSuperStar)celestialBodyParent;
                        newBody.ParentGrandSuperStarId = celestialBodyParent.Id;
                    }
                    break;

                case GenesisType.Universe:
                    {
                        newBody = new GrandSuperStar();

                        if (celestialBodyParent == null)
                            celestialBodyParent = DefaultGreatGrandSuperStar;

                        Mapper<IGreatGrandSuperStar, GrandSuperStar>.MapParentCelestialBodyProperties((IGreatGrandSuperStar)celestialBodyParent, (GrandSuperStar)newBody);
                        newBody.ParentHolon = celestialBodyParent;
                        newBody.ParentHolonId = celestialBodyParent.Id;
                        newBody.ParentCelestialBody = celestialBodyParent;
                        newBody.ParentCelestialBodyId = celestialBodyParent.Id;
                        newBody.ParentGreatGrandSuperStar = (IGreatGrandSuperStar)celestialBodyParent;
                        newBody.ParentGreatGrandSuperStarId = celestialBodyParent.Id;
                    }
                    break;
            }

            if (genesisType != GenesisType.ZomesAndHolonsOnly)
            {
                newBody.Id = Guid.NewGuid();
                newBody.IsNewHolon = true; //This was commented out, not sure why?
                newBody.Name = name;
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
          
            //TODO: MOVE ALL RUST CODE INTO HOLOOASIS.GENERATENATIVECODE METHOD.
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

                            zomeBufferCsharp = zomeTemplateCsharp.Replace(STARDNA.TemplateNamespace, genesisNameSpace);
                            izomeBufferCsharp = iZomeTemplate.Replace(STARDNA.TemplateNamespace, genesisNameSpace);
                            holonBufferCsharp = holonTemplateCsharp.Replace(STARDNA.TemplateNamespace, genesisNameSpace);
                            iholonBufferCsharp = iHolonTemplate.Replace(STARDNA.TemplateNamespace, genesisNameSpace);
                        }

                        if (buffer.Contains("ZomeDNA"))
                        {
                            string[] parts = buffer.Split(' ');
                            libBuffer = libTemplate.Replace("zome_name", parts[6].ToSnakeCase());

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
                            else
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
                                        GenerateRustField(fieldName, stringTemplateRust, NodeType.String, holonName, currentHolon, ref firstField, ref holonFieldsClone, ref holonBufferRust);
                                    }
                                    break;

                                case "int":
                                    {
                                        GenerateCSharpField(parts[14], IntTemplateCsharp, ref holonBufferCsharp, ref iholonBufferCsharp, ref firstField, ref secondField);
                                        GenerateRustField(fieldName, intTemplateRust, NodeType.Int, holonName, currentHolon, ref firstField, ref holonFieldsClone, ref holonBufferRust);
                                    }
                                    break;

                                case "bool":
                                    {
                                        GenerateCSharpField(parts[14], BoolTemplateCsharp, ref holonBufferCsharp, ref iholonBufferCsharp, ref firstField, ref secondField);
                                        GenerateRustField(fieldName, boolTemplateRust, NodeType.Bool, holonName, currentHolon, ref firstField, ref holonFieldsClone, ref holonBufferRust);
                                    }
                                    break;
                            }
                        }

                        // Write the holon out.
                        if (holonReached && buffer.Length > 1 && buffer.Substring(buffer.Length - 1, 1) == "}" && !buffer.Contains("get;"))
                        {
                            if (holonBufferRust.Length > 2)
                                holonBufferRust = holonBufferRust.Remove(holonBufferRust.Length - 3);

                            holonBufferRust = string.Concat(Environment.NewLine, holonBufferRust, Environment.NewLine, holonTemplateRust.Substring(holonTemplateRust.Length - 1, 1), Environment.NewLine);

                            int zomeIndex = libTemplate.IndexOf("#[zome]");
                            int zomeBodyStartIndex = libTemplate.IndexOf("{", zomeIndex);

                            libBuffer = libBuffer.Insert(zomeIndex - 2, holonBufferRust);

                            if (nextLineToWrite == 0)
                                nextLineToWrite = zomeBodyStartIndex + holonBufferRust.Length;
                            else
                                nextLineToWrite += holonBufferRust.Length;

                            //Now insert the CRUD methods for each holon.
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, createTemplate.Replace("Holon", holonName.ToPascalCase()).Replace("{holon}", holonName), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, readTemplate.Replace("Holon", holonName.ToPascalCase()).Replace("{holon}", holonName), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, updateTemplate.Replace("Holon", holonName.ToPascalCase()).Replace("{holon}", holonName).Replace("//#CopyFields//", holonFieldsClone), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, deleteTemplate.Replace("Holon", holonName.ToPascalCase()).Replace("{holon}", holonName), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, validationTemplate.Replace("Holon", holonName.ToPascalCase()).Replace("{holon}", holonName), Environment.NewLine));
                            holonName = holonName.ToPascalCase();

                            File.WriteAllText(string.Concat(genesisFolder, "\\CSharp\\Interfaces\\Holons\\I", holonName, ".cs"), iholonBufferCsharp);
                            File.WriteAllText(string.Concat(genesisFolder, "\\CSharp\\Holons\\", holonName, ".cs"), holonBufferCsharp);

                            holonBufferRust = "";
                            holonBufferCsharp = "";
                            iholonBufferCsharp = "";
                            holonFieldsClone = "";
                            holonReached = false;
                            firstField = true;
                            firstHolon = false;
                            holonName = "";
                        }

                        if (buffer.Contains("HolonDNA"))
                        {
                            string[] parts = buffer.Split(' ');
                            holonName = parts[10].ToPascalCase();

                            holonBufferRust = holonTemplateRust.Replace("Holon", holonName).Replace("{holon}", holonName.ToSnakeCase());
                            holonBufferRust = holonBufferRust.Substring(0, holonBufferRust.Length - 1);

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

                            zomeBufferCsharp = zomeBufferCsharp.Replace(STARDNA.TemplateNamespace, genesisNameSpace);
                            izomeBufferCsharp = izomeBufferCsharp.Replace(STARDNA.TemplateNamespace, genesisNameSpace);
                            holonBufferCsharp = holonBufferCsharp.Replace(STARDNA.TemplateNamespace, genesisNameSpace);
                            iholonBufferCsharp = iholonBufferCsharp.Replace(STARDNA.TemplateNamespace, genesisNameSpace);

                            if (newBody != null)
                            {
                                if (string.IsNullOrEmpty(celestialBodyBufferCsharp))
                                    celestialBodyBufferCsharp = celestialBodyTemplateCsharp;

                                celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace(STARDNA.TemplateNamespace, genesisNameSpace);
                                celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("NAMESPACE", genesisNameSpace);
                                celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("ID", newBody.Id.ToString());
                                celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("CelestialBodyDNATemplate", name.ToPascalCase());
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
                    nextLineToWrite = 0;

                    File.WriteAllText(string.Concat(genesisFolder, "\\Rust\\lib.rs"), libBuffer); //TODO: Move out to HoloOASIS Provider ASAP.
                    File.WriteAllText(string.Concat(genesisFolder, "\\CSharp\\Interfaces\\Zomes\\I", zomeName, ".cs"), izomeBufferCsharp);
                    File.WriteAllText(string.Concat(genesisFolder, "\\CSharp\\Zomes\\", zomeName, ".cs"), zomeBufferCsharp);
                }
            }

            // Remove any white space from the name.
            if (genesisType != GenesisType.ZomesAndHolonsOnly)
                File.WriteAllText(string.Concat(genesisFolder, "\\CSharp\\", Regex.Replace(name, @"\s+", ""), Enum.GetName(typeof(GenesisType), genesisType), ".cs"), celestialBodyBufferCsharp);

            // Currently the OApp Name is the same as the CelestialBody name (each CelestialBody is a seperate OApp), but in future a OApp may be able to contain more than one celestialBody...
            // TODO: Currently the OApp templates only contain sample load/save for one holon... this may change in future... likely will... ;-) Want to show for every zome/holon inside the celestialbody...
            ApplyOAPPTemplate(genesisType, OAPPFolder, genesisNameSpace, name, name, holonNames[0], firstStringProperty);

            //Generate any native code for the current provider.
            //TODO: Add option to pass into STAR which providers to generate native code for (can be more than one provider).
            ((IOASISSuperStar)ProviderManager.Instance.CurrentStorageProvider).NativeCodeGenesis(newBody);

            //TODO: Need to save this to the StarNET store (still to be made!) (Will of course be written on top of the HDK/ODK...
            //This will be private on the store until the user publishes via the Star.Seed() command.

            switch (genesisType)
            {
                case GenesisType.ZomesAndHolonsOnly:
                    {
                        OASISResult<CoronalEjection> result = new OASISResult<CoronalEjection>(new CoronalEjection());

                        foreach (IZome zome in zomes)
                        {
                            OASISResult<IZome> saveZomeResult = await zome.SaveAsync();

                            if (!(saveZomeResult != null && saveZomeResult.Result != null && !saveZomeResult.IsError))
                                OASISErrorHandling.HandleError(ref result, $"Error occured saving zome {LoggingHelper.GetHolonInfoForLogging(zome, "zome")}. Reason: {saveZomeResult.Message}.", true);
                        }

                        if (!result.IsError)
                            result.Message = "Zomes And Holons Successfully Created.";
                        else
                            result.Message = $"Some errors occured saving zomes and holons: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

                        result.Result.Zomes = new List<IZome>(zomes);
                        return result;
                    }

                case GenesisType.Moon:
                    {
                        //celestialBodyParent will be a Planet (Default is Our World).
                        //TODO: Soon need to add this code to Holon or somewhere so Parent's are lazy loaded when accessed for first time.
                        if (celestialBodyParent.ParentStar == null)
                            celestialBodyParent.ParentStar = new Star(celestialBodyParent.ParentStarId);

                        OASISResult<IMoon> result =  await ((StarCore)celestialBodyParent.ParentStar.CelestialBodyCore).AddMoonAsync(newBody.ParentPlanet, (IMoon)newBody);

                        if (result != null)
                        {
                            if (result.IsError)
                                return new OASISResult<CoronalEjection>() { IsError = true, Message = result.Message, Result = new CoronalEjection() { CelestialBody = result.Result } };
                            else
                                return new OASISResult<CoronalEjection>() { IsError = false, Message = "Moon Successfully Created.", Result = new CoronalEjection() { CelestialBody = result.Result } };
                        }
                        else
                            return new OASISResult<CoronalEjection>() { IsError = true, Message = "Unknown Error Occured Creating Moon." };
                    }

                case GenesisType.Planet:
                    {                      
                        OASISResult<IPlanet> result = await ((StarCore)celestialBodyParent.CelestialBodyCore).AddPlanetAsync((IPlanet)newBody);

                        if (result != null)
                        {
                            if (result.IsError)
                                return new OASISResult<CoronalEjection>() { IsError = true, Message = result.Message, Result = new CoronalEjection() { CelestialBody = result.Result } };
                            else
                                return new OASISResult<CoronalEjection>() { IsError = false, Message = "Planet Successfully Created.", Result = new CoronalEjection() { CelestialBody = result.Result } };
                        }
                        else
                            return new OASISResult<CoronalEjection>() { IsError = true, Message = "Unknown Error Occured Creating Planet." };
                    }

                case GenesisType.Star:
                    {
                        OASISResult<IStar> result = await ((ISuperStarCore)celestialBodyParent.CelestialBodyCore).AddStarAsync((IStar)newBody);

                        if (result != null)
                        {
                            if (result.IsError)
                                return new OASISResult<CoronalEjection>() { IsError = true, Message = result.Message, Result = new CoronalEjection() { CelestialBody = result.Result } };
                            else
                                return new OASISResult<CoronalEjection>() { IsError = false, Message = "Star Successfully Created.", Result = new CoronalEjection() { CelestialBody = result.Result } };
                        }
                        else
                            return new OASISResult<CoronalEjection>() { IsError = true, Message = "Unknown Error Occured Creating Star." };
                    }

                case GenesisType.SoloarSystem:
                    {
                        OASISResult<ISolarSystem> result = await ((StarCore)celestialBodyParent.CelestialBodyCore).AddSolarSystemAsync(new SolarSystem() { Star = (IStar)newBody });

                        if (result != null)
                        {
                            if (result.IsError)
                                return new OASISResult<CoronalEjection>() { IsError = true, Message = result.Message, Result = new CoronalEjection() { CelestialSpace = result.Result, CelestialBody = result.Result.Star } };
                            else
                                return new OASISResult<CoronalEjection>() { IsError = false, Message = "Star/SoloarSystem Successfully Created.", Result = new CoronalEjection() { CelestialSpace = result.Result, CelestialBody = result.Result.Star } };
                        }
                        else
                            return new OASISResult<CoronalEjection>() { IsError = true, Message = "Unknown Error Occured Creating Star/SoloarSystem." };
                    }

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
                    return new OASISResult<CoronalEjection>() { IsError = true, Message = "Unknown Error Occured.", Result = new CoronalEjection() { CelestialBody = newBody } };
            }
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
            if (e.Result != null && e.Result.Result != null)
            {
                if (!e.Result.IsError)
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"{e.Result.Result.Name} Created." });
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

        //Deploy
        public static void Seed(ICelestialBody body)
        {

        }

        public static void Seed(string bodyName)
        {

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
                ValidateFolder("", starDNA.BasePath, "STARDNA.BasePath");
                ValidateFolder(starDNA.BasePath, starDNA.CelestialBodyDNA, "STARDNA.CelestialBodyDNA", true);
                ValidateFolder(starDNA.BasePath, starDNA.GenesisFolder, "STARDNA.GenesisFolder", false, true);
                //ValidateFolder(starDNA.BasePath, starDNA.GenesisRustFolder, "STARDNA.GenesisRustFolder", false, true);
                ValidateFolder(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, "STARDNA.CSharpDNATemplateFolder");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateHolonDNA, "STARDNA.CSharpTemplateHolonDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateZomeDNA, "STARDNA.CSharpTemplateZomeDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateCelestialBodyDNA, "STARDNA.CSharpTemplateCelestialBodyDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateLoadHolonDNA, "STARDNA.CSharpTemplateLoadHolonDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateSaveHolonDNA, "STARDNA.CSharpTemplateSaveHolonDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateILoadHolonDNA, "STARDNA.CSharpTemplateILoadHolonDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateISaveHolonDNA, "STARDNA.CSharpTemplateISaveHolonDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateInt, "STARDNA.CSharpTemplateInt");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateString, "STARDNA.CSharpTemplateString");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateBool, "STARDNA.CSharpTemplateBool");

                ValidateFolder(starDNA.BasePath, starDNA.OAPPBlazorTemplateDNA, "STARDNA.OAPPBlazorTemplateDNA", true);
                ValidateFolder(starDNA.BasePath, starDNA.OAPPConsoleTemplateDNA, "STARDNA.OAPPConsoleTemplateDNA", true);
                //ValidateFolder(starDNA.BasePath, starDNA.OAPPCustomTemplateDNA, "STARDNA.OAPPCustomTemplateDNA", true);
                //ValidateFolder(starDNA.BasePath, starDNA.OAPPGraphQLServiceTemplateDNA, "STARDNA.OAPPGraphQLServiceTemplateDNA", true);
                //ValidateFolder(starDNA.BasePath, starDNA.OAPPgRPCServiceTemplateDNA, "STARDNA.OAPPgRPCServiceTemplateDNA", true);
                //ValidateFolder(starDNA.BasePath, starDNA.OAPPMAUITemplateDNA, "STARDNA.OAPPMAUITemplateDNA", true);
                //ValidateFolder(starDNA.BasePath, starDNA.OAPPRESTServiceTemplateDNA, "STARDNA.OAPPRESTServiceTemplateDNA", true);
                //ValidateFolder(starDNA.BasePath, starDNA.OAPPUnityTemplateDNA, "STARDNA.OAPPUnityTemplateDNA", true);
                //ValidateFolder(starDNA.BasePath, starDNA.OAPPWebMVCTemplateDNA, "STARDNA.OAPPWebMVCTemplateDNA", true);
                //ValidateFolder(starDNA.BasePath, starDNA.OAPPWindowsServiceTemplateDNA, "STARDNA.OAPPWindowsServiceTemplateDNA", true);
                //ValidateFolder(starDNA.BasePath, starDNA.OAPPWinFormsTemplateDNA, "STARDNA.OAPPWinFormsTemplateDNA", true);
                //ValidateFolder(starDNA.BasePath, starDNA.OAPPWPFTemplateDNA, "STARDNA.OAPPWPFTemplateDNA", true);


                ValidateFolder(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, "STARDNA.RustDNARSMTemplateFolder");
                ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateLib, "STARDNA.RustTemplateLib");
                ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateCreate, "STARDNA.RustTemplateCreate");
                ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateDelete, "STARDNA.RustTemplateDelete");
                ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateRead, "STARDNA.RustTemplateRead");
                ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateUpdate, "STARDNA.RustTemplateUpdate");
                ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateList, "STARDNA.RustTemplateList");
                ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateValidation, "STARDNA.RustTemplateValidation");
                ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateInt, "STARDNA.RustTemplateInt");
                ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateString, "STARDNA.RustTemplateString");
                ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateBool, "STARDNA.RustTemplateBool");
                ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateHolon, "STARDNA.RustTemplateHolon");
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

        private static STARDNA LoadDNA()
        {
            using (StreamReader r = new StreamReader(STARDNAPath))
            {
                string json = r.ReadToEnd();
                STARDNA = JsonConvert.DeserializeObject<STARDNA> (json);
                return STARDNA;
            }
        }
        private static bool SaveDNA()
        {
            string json = JsonConvert.SerializeObject(STARDNA);
            StreamWriter writer = new StreamWriter(STARDNAPath);
            writer.Write(json);
            writer.Close();
            
            return true;
        }

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

        private static void GenerateRustField(string fieldName, string fieldTemplate, NodeType nodeType, string holonName, IHolon currentHolon, ref bool firstField, ref string holonFieldsClone, ref string holonBufferRust)
        {
            if (firstField)
                firstField = false;
            else
                holonFieldsClone = string.Concat(holonFieldsClone, "\t");

            holonFieldsClone = string.Concat(holonFieldsClone, holonName, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
            holonBufferRust = string.Concat(holonBufferRust, fieldTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);

            if (currentHolon.Nodes == null)
                currentHolon.Nodes = new ObservableCollection<INode>(); //new List<INode>();

            currentHolon.Nodes.Add(new Node { Id = Guid.NewGuid(), NodeName = fieldName.ToPascalCase(), NodeType = nodeType, ParentId = currentHolon.Id });
        }

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

        private static OASISResult<bool> BootOASIS(string OASISDNAPath = OASIS_DNA_DEFAULT_PATH)
        {
            STAR.OASISDNAPath = OASISDNAPath;

            if (!OASISAPI.IsOASISBooted)
                return OASISAPI.BootOASIS(STAR.OASISDNAPath);
            else
                return new OASISResult<bool>() { Message = "OASIS Already Booted" };
        }

        private static async Task<OASISResult<bool>> BootOASISAsync(string OASISDNAPath = OASIS_DNA_DEFAULT_PATH)
        {
            STAR.OASISDNAPath = OASISDNAPath;

            if (!OASISAPI.IsOASISBooted)
                return await OASISAPI.BootOASISAsync(STAR.OASISDNAPath);
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
                        ShowStatusMessage(StarStatusMessageType.Success, $"{longName} Initialized.");
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
                        ShowStatusMessage(StarStatusMessageType.Success, $"{longName} Initialized.");
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

            SaveDNA();

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

        private static void ApplyOAPPTemplate(GenesisType genesisType, string OAPPFolder, string oAppNameSpace, string oAppName, string celestialBodyName, string holonName, string firstStringProperty)
        {
            foreach (DirectoryInfo dir in new DirectoryInfo(OAPPFolder).GetDirectories())
            {
                if (dir.Name != "bin" && dir.Name != "obj")
                    ApplyOAPPTemplate(genesisType, dir.FullName, oAppNameSpace, oAppName, celestialBodyName, holonName, firstStringProperty);
            }
            
            if (!OAPPFolder.Contains(STAR.STARDNA.OAPPGeneratedCodeFolder))
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
                foreach (FileInfo file in new DirectoryInfo(OAPPFolder).GetFiles("*.cs"))
                {
                    int lineNumber = 1;
                    string line = null;

                    using (TextReader tr = File.OpenText(file.FullName))
                    using (TextWriter tw = File.CreateText(string.Concat(file.FullName, ".temp")))
                    {
                        while ((line = tr.ReadLine()) != null)
                        {
                            celestialBodyName = celestialBodyName.Replace(" ", "");
                            line = line.Replace("{OAPPNAMESPACE}", oAppNameSpace);
                            line = line.Replace("{OAPPNAME}", oAppName);

                            if (genesisType == GenesisType.ZomesAndHolonsOnly)
                            {
                                line = line.Replace("//ZomesAndHolonsOnly:", "");

                                if (line.Contains("CelestialBodyOnly"))
                                    continue;
                            }
                            else
                            {
                                line = line.Replace("{CELESTIALBODY}", celestialBodyName.ToPascalCase()).Replace("//CelestialBodyOnly:", "");
                                line = line.Replace("{CELESTIALBODYVAR}", celestialBodyName.ToCamelCase()).Replace("//CelestialBodyOnly:", "");

                                if (line.Contains("ZomesAndHolonsOnly"))
                                    continue;
                            }

                            line = line.Replace("{HOLON}", holonName.ToPascalCase());
                            line = line.Replace("{STRINGPROPERTY}", firstStringProperty.ToPascalCase());

                            tw.WriteLine(line);
                            lineNumber++;
                        }
                    }

                    File.Delete(file.FullName);
                    File.Move(string.Concat(file.FullName, ".temp"), file.FullName);
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
    }
}
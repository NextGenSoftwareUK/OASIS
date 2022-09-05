using System;
using System.Collections.ObjectModel;
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
using static NextGenSoftware.OASIS.API.Core.Events.Events;

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
                return _defaultGreatGrandSuperStar;
            }
            set
            {
                if (_defaultGreatGrandSuperStar == null)
                {
                    if (STARDNA != null && !string.IsNullOrEmpty(STARDNA.DefaultGreatGrandSuperStarId) && Guid.TryParse(STARDNA.DefaultGreatGrandSuperStarId, out _))
                        _defaultGreatGrandSuperStar = new GreatGrandSuperStar(new Guid(STARDNA.DefaultGreatGrandSuperStarId));
                }
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
            OASISResult<bool> oasisResult = BootOASIS(OASISDNAPath); //TODO: Add Async versions of everything! ;-)

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

        public static async Task<OASISResult<IAvatar>> BeamInAsync(string username, string password)
        {
            string hostName = Dns.GetHostName();
            string IPAddress = Dns.GetHostEntry(hostName).AddressList[0].ToString();

            if (!IsStarIgnited)
                IgniteStar();

            OASISResult<IAvatar> result = await OASISAPI.Avatar.AuthenticateAsync(username, password, IPAddress);

            if (!result.IsError)
                LoggedInAvatar = (Avatar)result.Result;

            return result;
        }

        public static OASISResult<IAvatar> CreateAvatar(string title, string firstName, string lastName, string username, string password, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
        {
            if (!IsStarIgnited)
                IgniteStar();

            return OASISAPI.Avatar.Register(title, firstName, lastName, username, password, AvatarType.User, OASISType.STARCLI, cliColour, favColour);
        }

        public static async Task<OASISResult<IAvatar>> CreateAvatarAsync(string title, string firstName, string lastName, string username, string password, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
        {
            if (!IsStarIgnited)
                IgniteStar();

            //TODO: Implement Async version of Register and call instead of below:
            return OASISAPI.Avatar.Register(title, firstName, lastName, username, password, AvatarType.User, OASISType.STARCLI, cliColour, favColour);
        }

        public static OASISResult<IAvatar> BeamIn(string username, string password)
        {
            string IPAddress = "";
            string hostName = Dns.GetHostName();
            IPHostEntry entry = Dns.GetHostEntry(hostName);

            //if (entry != null && entry.AddressList.Length > 2)
            //    IPAddress = Dns.GetHostEntry(hostName).AddressList[2].ToString();

            if (entry != null && entry.AddressList.Length > 1)
                IPAddress = Dns.GetHostEntry(hostName).AddressList[1].ToString();

            //string IPAddress = Dns.GetHostByName(hostName).AddressList[3].ToString();
            //+string IPAddress = Dns.GetHostByName(hostName).AddressList[4].ToString();

            if (!IsStarIgnited)
                IgniteStar();

            OASISResult<IAvatar> result = OASISAPI.Avatar.Authenticate(username, password, IPAddress);

            if (!result.IsError)
            {
                LoggedInAvatar = (Avatar)result.Result;
                OASISResult<IAvatarDetail> loggedInAvatarDetailResult = OASISAPI.Avatar.LoadAvatarDetail(LoggedInAvatar.Id);

                if (!loggedInAvatarDetailResult.IsError && loggedInAvatarDetailResult.Result != null)
                {
                    LoggedInAvatarDetail = loggedInAvatarDetailResult.Result;

                    if (username == "davidellams@hotmail.com" || username == "davidellams@gmail.com")
                    {
                        LoggedInAvatarDetail = new AvatarDetail();
                        LoggedInAvatarDetail.Karma = 777777;
                        LoggedInAvatarDetail.XP = 2222222;

                        LoggedInAvatarDetail.GeneKeys.Add(new GeneKey() { Name = "Expectation", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });
                        LoggedInAvatarDetail.GeneKeys.Add(new GeneKey() { Name = "Invisibility", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });
                        LoggedInAvatarDetail.GeneKeys.Add(new GeneKey() { Name = "Rapture", Gift = "a gift", Shadow = "a shadow", Sidhi = "a sidhi" });

                        LoggedInAvatarDetail.HumanDesign.Type = "Generator";
                        LoggedInAvatarDetail.Inventory.Add(new InventoryItem() { Name = "Magical Armour" });
                        LoggedInAvatarDetail.Inventory.Add(new InventoryItem() { Name = "Mighty Wizard Sword" });

                        LoggedInAvatarDetail.Spells.Add(new Spell() { Name = "Super Spell" });
                        LoggedInAvatarDetail.Spells.Add(new Spell() { Name = "Super Speed Spell" });
                        LoggedInAvatarDetail.Spells.Add(new Spell() { Name = "Super Srength Spell" });

                        LoggedInAvatarDetail.Achievements.Add(new Achievement() { Name = "Becoming Superman!" });
                        LoggedInAvatarDetail.Achievements.Add(new Achievement() { Name = "Completing STAR!" });

                        LoggedInAvatarDetail.Gifts.Add(new AvatarGift() { GiftType = KarmaTypePositive.BeASuperHero });

                        LoggedInAvatarDetail.Aura.Brightness = 99;
                        LoggedInAvatarDetail.Aura.Level = 77;
                        LoggedInAvatarDetail.Aura.Progress = 88;
                        LoggedInAvatarDetail.Aura.Size = 10;
                        LoggedInAvatarDetail.Aura.Value = 777;

                        LoggedInAvatarDetail.Chakras.Root.Level = 77;
                        LoggedInAvatarDetail.Chakras.Root.Progress = 99;
                        LoggedInAvatarDetail.Chakras.Root.XP = 8783;

                        LoggedInAvatarDetail.Attributes.Dexterity = 99;
                        LoggedInAvatarDetail.Attributes.Endurance = 99;
                        LoggedInAvatarDetail.Attributes.Intelligence = 99;
                        LoggedInAvatarDetail.Attributes.Magic = 99;
                        LoggedInAvatarDetail.Attributes.Speed = 99;
                        LoggedInAvatarDetail.Attributes.Strength = 99;
                        LoggedInAvatarDetail.Attributes.Toughness = 99;
                        LoggedInAvatarDetail.Attributes.Vitality = 99;
                        LoggedInAvatarDetail.Attributes.Wisdom = 99;

                        LoggedInAvatarDetail.Stats.Energy.Current = 99;
                        LoggedInAvatarDetail.Stats.Energy.Max = 99;
                        LoggedInAvatarDetail.Stats.HP.Current = 99;
                        LoggedInAvatarDetail.Stats.HP.Max = 99;
                        LoggedInAvatarDetail.Stats.Mana.Current = 99;
                        LoggedInAvatarDetail.Stats.Mana.Max = 99;
                        LoggedInAvatarDetail.Stats.Staminia.Current = 99;
                        LoggedInAvatarDetail.Stats.Staminia.Max = 99;

                        LoggedInAvatarDetail.SuperPowers.AstralProjection = 99;
                        LoggedInAvatarDetail.SuperPowers.BioLocatation = 88;
                        LoggedInAvatarDetail.SuperPowers.Flight = 99;
                        LoggedInAvatarDetail.SuperPowers.FreezeBreath = 88;
                        LoggedInAvatarDetail.SuperPowers.HeatVision = 99;
                        LoggedInAvatarDetail.SuperPowers.Invulerability = 99;
                        LoggedInAvatarDetail.SuperPowers.SuperSpeed = 99;
                        LoggedInAvatarDetail.SuperPowers.SuperStrength = 99;
                        LoggedInAvatarDetail.SuperPowers.XRayVision = 99;
                        LoggedInAvatarDetail.SuperPowers.Teleportation = 99;
                        LoggedInAvatarDetail.SuperPowers.Telekineseis = 99;

                        LoggedInAvatarDetail.Skills.Computers = 99;
                        LoggedInAvatarDetail.Skills.Engineering = 99;
                    }
                }
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

        public static async Task<OASISResult<CoronalEjection>> LightAsync(OAPPType OAPPType, GenesisType genesisType, string name, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisRustFolder = "", string genesisNameSpace = "")
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

        //TODO: Create non async version of Light();
        public static async Task<OASISResult<CoronalEjection>> LightAsync(OAPPType OAPPType, GenesisType genesisType, string name, ICelestialBody celestialBodyParent = null, string celestialBodyDNAFolder = "", string genesisFolder = "",  string genesisNameSpace = "")
        {
            //OASISResult<CoronalEjection> result = new OASISResult<CoronalEjection>();
            ICelestialBody newBody = null;
            bool holonReached = false;
            string holonBufferRust = "";
            string holonBufferCsharp = "";
            string libBuffer = "";
            string holonName = "";
            string zomeName = "";
            string holonFieldsClone = "";
            int nextLineToWrite = 0;
            bool firstField = true;
            string iholonBufferCsharp = "";
            string zomeBufferCsharp = "";
            string celestialBodyBufferCsharp = "";
            bool firstHolon = true;
            string rustcelestialBodyDNAFolder = string.Empty;

            if (LoggedInAvatarDetail == null)
                return new OASISResult<CoronalEjection>() { IsError = true, Message = "Avatar is not logged in. Please log in before calling this command." };

            if (LoggedInAvatarDetail.Level < 77 && genesisType == GenesisType.Star)
                return new OASISResult<CoronalEjection>() { IsError = true, Message = "Avatar must have reached level 77 before they can create stars. Please create a planet or moon instead..." };

            if (LoggedInAvatarDetail.Level < 33 && genesisType == GenesisType.Planet)
                return new OASISResult<CoronalEjection>() { IsError = true, Message = "Avatar must have reached level 33 before they can create planets. Please create a moon instead..." };

            //if (celestialBodyParent == null && type == GenesisType.Moon)
            //    return new OASISResult<CoronalEjection>() { IsError = true, Message = "You must specify the planet to add the moon to." };

            if (!IsStarIgnited)
                IgniteStar();

            if (DefaultStar == null)
            {
                OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();
                result = await IgniteInnerStarAsync(result);

                if (result.IsError)
                    return new OASISResult<CoronalEjection>() { IsError = true, Message = string.Concat("Error Igniting Inner Star. Reason: ", result.Message) };
            }

            ValidateLightDNA(celestialBodyDNAFolder, genesisFolder);

            switch (STARDNA.HolochainVersion.ToUpper())
            {
                case "REDUX":
                    rustcelestialBodyDNAFolder = $"{STARDNA.BasePath}\\{STARDNA.RustDNAReduxTemplateFolder}";
                    break;

                case "RSM":
                    rustcelestialBodyDNAFolder = $"{STARDNA.BasePath}\\{STARDNA.RustDNARSMTemplateFolder}";
                    break;
            }

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

            string IntTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateInt)).OpenText().ReadToEnd();
            string StringTemplateCSharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateString)).OpenText().ReadToEnd();
            string BoolTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateBool)).OpenText().ReadToEnd();

            //If folder is not passed in via command line args then use default in config file.
            if (string.IsNullOrEmpty(celestialBodyDNAFolder))
                celestialBodyDNAFolder = $"{STARDNA.BasePath}\\{STARDNA.CelestialBodyDNA}";

            if (string.IsNullOrEmpty(genesisFolder))
                genesisFolder = $"{STARDNA.BasePath}\\{STARDNA.GenesisFolder}";

            //if (string.IsNullOrEmpty(genesisRustFolder))
            //    genesisRustFolder = $"{STARDNA.BasePath}\\{STARDNA.GenesisRustFolder}";

            if (string.IsNullOrEmpty(genesisNameSpace))
                genesisNameSpace = $"{STARDNA.BasePath}\\{STARDNA.GenesisNamespace}";


            Directory.CreateDirectory(string.Concat(genesisFolder, "\\CSharp"));
            Directory.CreateDirectory(string.Concat(genesisFolder, "\\Rust")); //TODO: Soon this will be generic depending on what the target OASIS Providers STAR has been configured to generate OAPP code for...

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
            
          
            // No need to save to get Id anymore... :)
            /*
            OASISResult<ICelestialBody> newBodyResult = await newBody.SaveAsync(); //Need to save to get the id to be used for ParentId below (zomes, holons & nodes).

            if (newBodyResult.IsError)
                return new CoronalEjection() { ErrorOccured = true, Message = string.Concat("Error Saving New CelestialBody. Reason: ", newBodyResult.Message) };
            else
                newBody = (CelestialBody)newBodyResult.Result;
            */

            //TODO: MOVE ALL RUST CODE INTO HOLOOASIS.GENERATENATIVECODE METHOD.
            IZome currentZome = null;
            IHolon currentHolon = null;

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
                            holonBufferCsharp = holonTemplateCsharp.Replace(STARDNA.TemplateNamespace, genesisNameSpace);
                        }

                        if (buffer.Contains("ZomeDNA"))
                        {
                            string[] parts = buffer.Split(' ');
                            libBuffer = libTemplate.Replace("zome_name", parts[6].ToSnakeCase());

                            zomeBufferCsharp = zomeBufferCsharp.Replace("ZomeDNATemplate", parts[6].ToPascalCase());
                            zomeName = parts[6].ToPascalCase();

                            currentZome = new Zome()
                            {
                                Id = new Guid(),
                                IsNewHolon = true,
                                Name = zomeName,
                                CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI),
                                HolonType = HolonType.Zome,
                                ParentHolonId = newBody.Id,
                                ParentHolon = newBody,
                                ParentCelestialBodyId = newBody.Id,
                                ParentCelestialBody = newBody,
                                ParentPlanetId = newBody.HolonType == HolonType.Planet ? newBody.Id : Guid.Empty,
                                ParentPlanet = newBody.HolonType == HolonType.Planet ? (IPlanet)newBody : null,
                                ParentMoonId = newBody.HolonType == HolonType.Moon ? newBody.Id : Guid.Empty,
                                ParentMoon = newBody.HolonType == HolonType.Moon ? (IMoon)newBody : null
                            };


                            currentZome.Id = new Guid();
                            zomeBufferCsharp = zomeBufferCsharp.Replace("ID", currentZome.Id.ToString());

                            Mapper.MapParentCelestialBodyProperties(newBody, currentZome);
                            await newBody.CelestialBodyCore.AddZomeAsync(currentZome); //TODO: May need to save this once holons and nodes/fields have been added?
                        }

                        if (holonReached && buffer.Contains("string") || buffer.Contains("int") || buffer.Contains("bool"))
                        {
                            string[] parts = buffer.Split(' ');
                            string fieldName = parts[14].ToSnakeCase();

                            switch (parts[13].ToLower())
                            {
                                case "string":
                                    GenerateCSharpField(parts[14], StringTemplateCSharp, ref holonBufferCsharp, ref iholonBufferCsharp);
                                    GenerateRustField(fieldName, stringTemplateRust, NodeType.String, holonName, currentHolon, ref firstField, ref holonFieldsClone, ref holonBufferRust);
                                    break;

                                case "int":
                                    GenerateCSharpField(parts[14], IntTemplateCsharp, ref holonBufferCsharp, ref iholonBufferCsharp);
                                    GenerateRustField(fieldName, intTemplateRust, NodeType.Int, holonName, currentHolon, ref firstField, ref holonFieldsClone, ref holonBufferRust);
                                    break;

                                case "bool":
                                    GenerateCSharpField(parts[14], BoolTemplateCsharp, ref holonBufferCsharp, ref iholonBufferCsharp);
                                    GenerateRustField(fieldName, boolTemplateRust, NodeType.Bool, holonName, currentHolon, ref firstField, ref holonFieldsClone, ref holonBufferRust);
                                    break;
                            }
                        }

                        // Write the holon out to the rust lib template. 
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

                            //if (!firstHolon)
                            //{
                            //    //TODO: Need to make dynamic so no need to pass length in (had issues before but will try again later... :) )
                            //    zomeBufferCsharp = GenerateDynamicZomeFunc("Load", zomeTemplateCsharp, holonName, zomeBufferCsharp, 170);
                            //    zomeBufferCsharp = GenerateDynamicZomeFunc("Save", zomeTemplateCsharp, holonName, zomeBufferCsharp, 147);
                            //}

                            holonName = holonName.ToPascalCase();

                            File.WriteAllText(string.Concat(genesisFolder, "\\I", holonName, ".cs"), iholonBufferCsharp);
                            File.WriteAllText(string.Concat(genesisFolder, "\\", holonName, ".cs"), holonBufferCsharp);

                            //TDOD: Finish putting in IZomeBuffer etc
                            //   File.WriteAllText(string.Concat(genesisFolder, "\\I", holonName, ".cs"), izomeBuffer);
                            // File.WriteAllText(string.Concat(genesisFolder, "\\", zomeName, ".cs"), zomeBufferCsharp);

                            holonBufferRust = "";
                            holonBufferCsharp = "";
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

                            holonBufferCsharp = holonBufferCsharp.Replace("HolonDNATemplate", parts[10]);
                            iholonBufferCsharp = iHolonTemplate.Replace("IHolonDNATemplate", string.Concat("I", parts[10]));
                            holonBufferCsharp = holonBufferCsharp.Replace(STARDNA.TemplateNamespace, genesisNameSpace);
                            iholonBufferCsharp = iholonBufferCsharp.Replace(STARDNA.TemplateNamespace, genesisNameSpace);

                            //int endSpace = holonBufferCsharp.LastIndexOf("}");
                            //holonBufferCsharp = string.Concat(holonBufferCsharp.Substring(0, endSpace - 8), holonBufferCsharp.Substring(endSpace - 6));

                            zomeBufferCsharp = zomeBufferCsharp.Insert(zomeBufferCsharp.Length - 7, string.Concat(loadHolonTemplateCsharp.Replace(".CelestialBodyCore", ""), "\n"));
                            zomeBufferCsharp = zomeBufferCsharp.Insert(zomeBufferCsharp.Length - 7, string.Concat(saveHolonTemplateCsharp.Replace(".CelestialBodyCore", ""), "\n"));
                            zomeBufferCsharp = zomeBufferCsharp.Replace("HOLON", parts[10].ToPascalCase());
                            zomeBufferCsharp = zomeBufferCsharp.Replace("IHolon", $"I{parts[10].ToPascalCase()}");
                            zomeBufferCsharp = zomeBufferCsharp.Replace(STARDNA.TemplateNamespace, genesisNameSpace);
                            
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

                            // TODO: Current Zome Id will be empty here so need to save the zome before? (above when the zome is first created and added to the newBody zomes collection).
                            currentHolon = new Holon()
                            {
                                Id = new Guid(),
                                IsNewHolon = true,
                                Name = holonName,
                                CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI),
                                HolonType = HolonType.Holon,
                                ParentHolonId = currentZome.Id,
                                ParentHolon = currentZome,
                                ParentZomeId = currentZome.Id,
                                ParentZome = currentZome,
                                ParentCelestialBodyId = newBody.Id,
                                ParentCelestialBody = newBody,
                                ParentPlanetId = newBody.HolonType == HolonType.Planet ? newBody.Id : Guid.Empty,
                                ParentPlanet = newBody.HolonType == HolonType.Planet ? (IPlanet)newBody : null,
                                ParentMoonId = newBody.HolonType == HolonType.Moon ? newBody.Id : Guid.Empty,
                                ParentMoon = newBody.HolonType == HolonType.Moon ? (IMoon)newBody : null 
                            };

                            Mapper.MapParentCelestialBodyProperties(newBody, currentHolon);
                            currentZome.Holons.Add((Holon)currentHolon); 

                            holonName = holonName.ToSnakeCase();
                            holonReached = true;
                        }
                    }

                    reader.Close();
                    nextLineToWrite = 0;

                    File.WriteAllText(string.Concat(genesisFolder, "\\Rust\\lib.rs"), libBuffer);
                    File.WriteAllText(string.Concat(genesisFolder, "\\CSharp\\", zomeName, ".cs"), zomeBufferCsharp);
                }
            }

            // Remove any white space from the name.
            File.WriteAllText(string.Concat(genesisFolder, "\\", Regex.Replace(name, @"\s+", ""), Enum.GetName(typeof(GenesisType), genesisType), ".cs"), celestialBodyBufferCsharp);

          //  if (currentZome != null)
           //     newBody.CelestialBodyCore.Zomes.Add(currentZome);

            //TODO: Need to save the collection of Zomes/Holons that belong to this planet here...
            //await newBody.SaveAsync(); // Need to save again so newly added zomes/holons/nodes are also saved. //TODO: NO NEED TO SAVE BECAUSE IT IS SAVED IN METHODS BELOW...

            switch (genesisType)
            {
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

            //Generate any native code for the current provider.
            //TODO: Add option to pass into STAR which providers to generate native code for (can be more than one provider).
            ((IOASISSuperStar)ProviderManager.CurrentStorageProvider).NativeCodeGenesis(newBody);

            //TODO: Need to save this to the StarNET store (still to be made!) (Will of course be written on top of the HDK/ODK...
            //This will be private on the store until the user publishes via the Star.Seed() command.
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

        //Activate & Launch - Launch & activate a planet (OAPP) by shining the star's light upon it...
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

        // Delete Planet (OAPP)
        public static void Dust(ICelestialBody body)
        {

        }

        // Delete Planet (OAPP)
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

        // Highlight the Planet (OAPP) in the OAPP Store (StarNET)
        public static void Radiate(ICelestialBody body)
        {

        }

        public static void Radiate(string bodyName)
        {

        }

        // Show how much light the planet (OAPP) is emitting into the solar system (StarNET/HoloNET)
        public static void Emit(ICelestialBody body)
        {

        }

        public static void Emit(string bodyName)
        {

        }

        // Show stats of the Planet (OAPP)
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
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateInt, "STARDNA.CSharpTemplateInt");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateString, "STARDNA.CSharpTemplateString");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateBool, "STARDNA.CSharpTemplateBool");

                switch (starDNA.HolochainVersion.ToUpper())
                {
                    case "REDUX":
                        {
                            ValidateFolder(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, "STARDNA.RustDNAReduxTemplateFolder");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateLib, "STARDNA.RustTemplateLib");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateCreate, "STARDNA.RustTemplateCreate");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateDelete, "STARDNA.RustTemplateDelete");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateRead, "STARDNA.RustTemplateRead");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateUpdate, "STARDNA.RustTemplateUpdate");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateList, "STARDNA.RustTemplateList");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateValidation, "STARDNA.RustTemplateValidation");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateInt, "STARDNA.RustTemplateInt");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateString, "STARDNA.RustTemplateString");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateBool, "STARDNA.RustTemplateBool");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateHolon, "STARDNA.RustTemplateHolon");
                        }
                        break;

                    case "RSM":
                        {
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
                        break;
                }
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

        private static void ValidateFolder(string basePath, string folder, string folderParam, bool checkIfContainsFiles = false, bool createIfDoesNotExist = false)
        {
            string path = string.IsNullOrEmpty(basePath) ? folder : $"{basePath}\\{folder}";

            if (string.IsNullOrEmpty(folder))
                throw new ArgumentNullException(folderParam, string.Concat("The ", folderParam, " param in the StarDNA is null, please double check and try again."));

            if (checkIfContainsFiles && Directory.GetFiles(path).Length == 0)
                throw new InvalidOperationException(string.Concat("The ", folderParam, " folder (", path, ") in the StarDNA is empty."));

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
                throw new ArgumentNullException(fileParam, string.Concat("The ", fileParam, " param in the StarDNA is null, please double check and try again."));

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

        private static void GenerateCSharpField(string fieldName, string fieldTemplate, ref string holonBufferCsharp, ref string iHolonBufferCsharp)
        {
            int fieldsEnd = holonBufferCsharp.LastIndexOf("}") - 7;
            holonBufferCsharp = holonBufferCsharp.Insert(fieldsEnd, string.Concat(fieldTemplate.Replace("variableName", fieldName), "\n\n"));

            fieldsEnd = iHolonBufferCsharp.LastIndexOf("}") - 7;
            iHolonBufferCsharp = iHolonBufferCsharp.Insert(fieldsEnd, string.Concat(fieldTemplate.Replace("variableName", fieldName), "\n\n"));
        }

        private static OASISResult<bool> BootOASIS(string OASISDNAPath = OASIS_DNA_DEFAULT_PATH)
        {
            STAR.OASISDNAPath = OASISDNAPath;

            if (!OASISAPI.IsOASISBooted)
                return OASISAPI.BootOASIS(STAR.OASISDNAPath);
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



            /*
            //Normally you would leave autoLoad set to true but if you need to process the result in-line then you need to manually call Load as we do here (otherwise you would process the result from the OnCelestialBodyLoaded or OnCelestialBodyError event handlers).
            if (!string.IsNullOrEmpty(STARDNA.DefaultPlanetId))
            {
                if (Guid.TryParse(STARDNA.DefaultPlanetId, out id))
                {
                    DefaultPlanet = new Planet(id, false);
                    //OASISResult<ICelestialBody> planetResult = DefaultPlanet.Initialize();
                    OASISResult<ICelestialBody> planetResult = DefaultPlanet.Load();

                    if (planetResult.IsError || planetResult.Result == null)
                    {
                        ShowStatusMessage(StarStatusMessageType.Error, "Error Initializing Default Planet.");
                        HandleCelesitalBodyInitError(result, "DefaultPlanet", STARDNA.DefaultPlanetId, planetResult);
                        return result;
                    }
                    else
                        ShowStatusMessage(StarStatusMessageType.Success, "Default Planet Initialized.");
                }
                else
                {
                    HandleCelesitalBodyInitError(result, "DefaultPlanet", STARDNA.DefaultPlanetId, "The DefaultPlanetId value in STARDNA.json is not a valid Guid.");
                    return result;
                }
            }
            else
            {
                HandleCelesitalBodyInitError(result, "DefaultPlanet", STARDNA.DefaultPlanetId, "The DefaultPlanetId value in STARDNA.json is missing.");
                return result;
            }

            ShowStatusMessage(StarStatusMessageType.Processing, "Initializing Default Star...");
            DefaultStar = new Star(id, false); //TODO: Temp set InnerStar as The Sun at the centre of our Solar System.
            //OASISResult<ICelestialBody> starResult = DefaultStar.Initialize();
            OASISResult<ICelestialBody> starResult = DefaultStar.Load();

            if (starResult.IsError || starResult.Result == null)
            {
                ShowStatusMessage(StarStatusMessageType.Error, "Error Initializing Default Star.");
                HandleCelesitalBodyInitError(result, "DefaultStar", _starId.ToString(), starResult);
                return result;
            }
            else
                ShowStatusMessage(StarStatusMessageType.Success, "Default Star Initialized.");


            if (!string.IsNullOrEmpty(STARDNA.DefaultSuperStarId))
            {
                if (Guid.TryParse(STARDNA.DefaultSuperStarId, out id))
                {
                    ShowStatusMessage(StarStatusMessageType.Processing, "Initializing Default Super Star...");
                    DefaultSuperStar = new SuperStar(id);
                    //OASISResult<ICelestialBody> superStarResult = DefaultSuperStar.Initialize();
                    OASISResult<ICelestialBody> superStarResult = DefaultSuperStar.Load();

                    if (superStarResult.IsError || superStarResult.Result == null)
                    {
                        ShowStatusMessage(StarStatusMessageType.Error, "Error Initializing Default Super Star.");
                        HandleCelesitalBodyInitError(result, "DefaultSuperStar", STARDNA.DefaultSuperStarId, superStarResult);
                        return result;
                    }
                    else
                        ShowStatusMessage(StarStatusMessageType.Success, "Default Super Star Initialized.");
                }
                else
                {
                    HandleCelesitalBodyInitError(result, "DefaultSuperStar", STARDNA.DefaultSuperStarId, "The DefaultSuperStar value in STARDNA.json is not a valid Guid.");
                    return result;
                }
            }
            else
            {
                HandleCelesitalBodyInitError(result, "DefaultSuperStar", STARDNA.DefaultSuperStarId, "The DefaultSuperStarId value in STARDNA.json is missing.");
                return result;
            }

            if (!string.IsNullOrEmpty(STARDNA.DefaultGrandSuperStarId))
            {
                if (Guid.TryParse(STARDNA.DefaultGrandSuperStarId, out id))
                {
                    ShowStatusMessage(StarStatusMessageType.Processing, "Initializing Default Grand Super Star...");
                    DefaultGrandSuperStar = new GrandSuperStar(id);
                    //OASISResult<ICelestialBody> grandSuperStarResult = DefaultGrandSuperStar.Initialize();
                    OASISResult<ICelestialBody> grandSuperStarResult = DefaultGrandSuperStar.Load();

                    if (grandSuperStarResult.IsError || grandSuperStarResult.Result == null)
                    {
                        ShowStatusMessage(StarStatusMessageType.Error, "Error Initializing Default Grand Super Star.");
                        HandleCelesitalBodyInitError(result, "DefaultGrandSuperStar", STARDNA.DefaultGrandSuperStarId, grandSuperStarResult);
                        return result;
                    }
                    else
                        ShowStatusMessage(StarStatusMessageType.Success, "Default Super Star Initialized.");
                }
                else
                {
                    HandleCelesitalBodyInitError(result, "DefaultGrandSuperStar", STARDNA.DefaultGrandSuperStarId, "The DefaultGrandSuperStarId value in STARDNA.json is not a valid Guid.");
                    return result;
                }
            }
            else
            {
                HandleCelesitalBodyInitError(result, "DefaultGrandSuperStar", STARDNA.DefaultGrandSuperStarId, "The DefaultGrandSuperStarId value in STARDNA.json is missing.");
                return result;
            }

            if (!string.IsNullOrEmpty(STARDNA.DefaultGreatGrandSuperStarId))
            {
                if (Guid.TryParse(STARDNA.DefaultGreatGrandSuperStarId, out id))
                {
                    ShowStatusMessage(StarStatusMessageType.Processing, "Initializing Default Great Grand Super Star...");
                    DefaultGreatGrandSuperStar = new GreatGrandSuperStar(id);
                    //OASISResult<ICelestialBody> greatGrandSuperStarResult = DefaultGreatGrandSuperStar.Initialize();
                    OASISResult<ICelestialBody> greatGrandSuperStarResult = DefaultGreatGrandSuperStar.Load();

                    if (greatGrandSuperStarResult.IsError || greatGrandSuperStarResult.Result == null)
                    {
                        ShowStatusMessage(StarStatusMessageType.Error, "Error Initializing Default Great Grand Super Star.");
                        HandleCelesitalBodyInitError(result, "DefaultGreatGrandSuperStarId", STARDNA.DefaultGreatGrandSuperStarId, greatGrandSuperStarResult);
                        return result;
                    }
                    else
                        ShowStatusMessage(StarStatusMessageType.Success, "Default Great Grand Super Star Initialized.");
                }
                else
                {
                    HandleCelesitalBodyInitError(result, "DefaultGreatGrandSuperStar", STARDNA.DefaultGreatGrandSuperStarId, "The DefaultGreatGrandSuperStarId value in STARDNA.json is not a valid Guid.");
                    return result;
                }
            }
            else
            {
                HandleCelesitalBodyInitError(result, "DefaultGreatGrandSuperStar", STARDNA.DefaultGreatGrandSuperStarId, "The DefaultGreatGrandSuperStarId value in STARDNA.json is missing.");
                return result;
            }*/

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


            /*
            DefaultPlanet = new Planet(new Guid(STARDNA.DefaultPlanetId), false);
            //OASISResult<ICelestialBody> planetResult = await DefaultPlanet.InitializeAsync();
            OASISResult<ICelestialBody> planetResult = await DefaultPlanet.LoadAsync();

            if (planetResult.IsError || planetResult.Result == null)
            {
                ShowStatusMessage(StarStatusMessageType.Error, "Error Initializing Default Planet.");
                HandleCelesitalBodyInitError(result, "DefaultPlanet", STARDNA.DefaultPlanetId, planetResult);
                return result;
            }
            else
                ShowStatusMessage(StarStatusMessageType.Success, "Default Planet Initialized.");

            ShowStatusMessage(StarStatusMessageType.Processing, "Initializing Default Star...");
            DefaultStar = new Star(_starId, false); //TODO: Temp set InnerStar as The Sun at the centre of our Solar System.
            //OASISResult<ICelestialBody> starResult = await DefaultStar.InitializeAsync();
            OASISResult<ICelestialBody> starResult = await DefaultStar.LoadAsync();

            if (starResult.IsError || starResult.Result == null)
            {
                ShowStatusMessage(StarStatusMessageType.Error, "Error Initializing Default Star.");
                HandleCelesitalBodyInitError(result, "DefaultStar", _starId.ToString(), starResult);
                return result;
            }
            else
                ShowStatusMessage(StarStatusMessageType.Success, "Default Star Initialized.");

            ShowStatusMessage(StarStatusMessageType.Processing, "Initializing Default Super Star...");
            DefaultSuperStar = new SuperStar(new Guid(STARDNA.DefaultSuperStarId));
            //OASISResult<ICelestialBody> superStarResult = await DefaultSuperStar.InitializeAsync();
            OASISResult<ICelestialBody> superStarResult = await DefaultSuperStar.LoadAsync();

            if (superStarResult.IsError || superStarResult.Result == null)
            {
                ShowStatusMessage(StarStatusMessageType.Error, "Error Initializing Default Super Star.");
                HandleCelesitalBodyInitError(result, "DefaultSuperStar", STARDNA.DefaultSuperStarId, superStarResult);
                return result;
            }
            else
                ShowStatusMessage(StarStatusMessageType.Success, "Default Super Star Initialized.");

            ShowStatusMessage(StarStatusMessageType.Processing, "Initializing Default Grand Super Star...");
            DefaultGrandSuperStar = new GrandSuperStar(new Guid(STARDNA.DefaultGrandSuperStarId));
            //OASISResult<ICelestialBody> grandSuperStarResult = await DefaultGrandSuperStar.InitializeAsync();
            OASISResult<ICelestialBody> grandSuperStarResult = await DefaultGrandSuperStar.LoadAsync();

            if (grandSuperStarResult.IsError || grandSuperStarResult.Result == null)
            {
                ShowStatusMessage(StarStatusMessageType.Error, "Error Initializing Default Grand Super Star.");
                HandleCelesitalBodyInitError(result, "DefaultGrandSuperStar", STARDNA.DefaultGrandSuperStarId, grandSuperStarResult);
                return result;
            }
            else
                ShowStatusMessage(StarStatusMessageType.Success, "Default Super Star Initialized.");

            ShowStatusMessage(StarStatusMessageType.Processing, "Initializing Default Great Grand Super Star...");
            DefaultGreatGrandSuperStar = new GreatGrandSuperStar(new Guid(STARDNA.DefaultGreatGrandSuperStarId));
            //OASISResult<ICelestialBody> greatGrandSuperStarResult = await DefaultGreatGrandSuperStar.InitializeAsync();
            OASISResult<ICelestialBody> greatGrandSuperStarResult = await DefaultGreatGrandSuperStar.LoadAsync();

            if (greatGrandSuperStarResult.IsError || greatGrandSuperStarResult.Result == null)
            {
                ShowStatusMessage(StarStatusMessageType.Error, "Error Initializing Default Great Grand Super Star.");
                HandleCelesitalBodyInitError(result, "DefaultGreatGrandSuperStarId", STARDNA.DefaultGreatGrandSuperStarId, greatGrandSuperStarResult);
                return result;
            }
            else
                ShowStatusMessage(StarStatusMessageType.Success, "Default Great Grand Super Star Initialized.");
            */

            ShowStatusMessage(StarStatusMessageType.Success, "Default Celestial Bodies Initialized.");

            return result;
        }

        //private static (OASISResult<IOmiverse>, T) InitCelestialBody<T>(string id, string name, OASISResult<IOmiverse> result, Func<OASISResult<ICelestialBody>> loadFunc) where T : ICelestialBody, new()
        private static (OASISResult<IOmiverse>, T) InitCelestialBody<T>(string id, string longName, OASISResult<IOmiverse> result) where T : ICelestialBody, new()
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
                    celestialBody = new T() {  Id = guidId};

                    //OASISResult<ICelestialBody> celestialBodyResult = celestialBody.Initialize();
                    OASISResult<ICelestialBody> celestialBodyResult = celestialBody.Load();
                    //OASISResult<ICelestialBody> celestialBodyResult = loadFunc();

                    if (celestialBodyResult.IsError || celestialBodyResult.Result == null)
                    {
                        ShowStatusMessage(StarStatusMessageType.Error, $"Error Initializing {longName}.");
                        HandleCelesitalBodyInitError(result, name, id, celestialBodyResult);
                    }
                    else
                        ShowStatusMessage(StarStatusMessageType.Success, $"{longName} Initialized.");
                }
                else
                    HandleCelesitalBodyInitError(result, name, id, $"The {name}Id value in STARDNA.json is not a valid Guid.");
            }
            else
                HandleCelesitalBodyInitError(result, name, id, $"The {name}Id value in STARDNA.json is missing.");

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

                    //OASISResult<ICelestialBody> celestialBodyResult = celestialBody.Initialize();
                    OASISResult<ICelestialBody> celestialBodyResult = await celestialBody.LoadAsync();
                    //OASISResult<ICelestialBody> celestialBodyResult = loadFunc();

                    if (celestialBodyResult.IsError || celestialBodyResult.Result == null)
                    {
                        ShowStatusMessage(StarStatusMessageType.Error, $"Error Initializing {longName}.");
                        HandleCelesitalBodyInitError(result, name, id, celestialBodyResult);
                    }
                    else
                        ShowStatusMessage(StarStatusMessageType.Success, $"{longName} Initialized.");
                }
                else
                    HandleCelesitalBodyInitError(result, name, id, $"The {name}Id value in STARDNA.json is not a valid Guid.");
            }
            else
                HandleCelesitalBodyInitError(result, name, id, $"The {name}Id value in STARDNA.json is missing.");

            return (result, (T)celestialBody);
        }

        private static void HandleCelesitalBodyInitError(OASISResult<IOmiverse> result, string name, string id, string errorMessage, OASISResult<ICelestialBody> celstialBodyResult = null)
        {
            string msg = $"Error occured in IgniteInnerStar initializing {name} with Id {id}. {errorMessage} Please correct or delete STARDNA to reset STAR ODK to then auto-generate new defaults.";

            if (celstialBodyResult != null)
                msg = string.Concat(msg, " Reason: ", celstialBodyResult.Message);

            ErrorHandling.HandleError(ref result, msg, celstialBodyResult != null ? celstialBodyResult.DetailedMessage : null);
        }

        private static void HandleCelesitalBodyInitError(OASISResult<IOmiverse> result, string name, string id, OASISResult<ICelestialBody> celstialBodyResult)
        {
            HandleCelesitalBodyInitError(result, name, id, "Likely reason is that the id does not exist.", celstialBodyResult);
            //ErrorHandling.HandleError(ref result, $"Error occured in IgniteInnerStar initializing {name} with Id {id}. Likely reason is that the id does not exist. Please correct or delete STARDNA to reset STAR ODK to then auto-generate new defaults. Reason: {celstialBodyResult.Message}", celstialBodyResult.DetailedMessage);
            //ErrorHandling.HandleError(ref result, $"Error occured in IgniteInnerStar initializing {name} with Id {id}. Likely reason is that the id does not exist, in this case remove the {name}Id from STARDNA.json and then try again. Reason: {celstialBodyResult.Message}", celstialBodyResult.DetailedMessage);
        }


        /// <summary>
        /// Create's the OASIS Omniverse along with a new default Multiverse (with it's GrandSuperStar) containing the ThirdDimension containing UniversePrime (simulation) and the MagicVerse (contains OAPP's), which itself contains a default GalaxyCluster containing a default Galaxy (along with it's SuperStar) containing a default SolarSystem (along wth it's Star) containing a default planet (Our World).
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
            OASISResultHelper<ICelestialSpace, IOmiverse>.CopyResult(celestialSpaceResult, result);
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
                                    OASISResultHelper<IPlanet, IOmiverse>.CopyResult(ourWorldResult, result);
                                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Our World. Reason: {ourWorldResult.Message}." });
                                }
                            }
                            else
                                OASISResultHelper<ISolarSystem, IOmiverse>.CopyResult(solarSystemResult, result);
                        }
                        else
                        {
                            OASISResultHelper<IStar, IOmiverse>.CopyResult(starResult, result);
                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Star. Reason: {starResult.Message}." });
                        }
                    }
                    else
                    {
                        OASISResultHelper<IGalaxy, IOmiverse>.CopyResult(galaxyResult, result);
                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Galaxy. Reason: {galaxyResult.Message}." });
                    }
                }
                else
                {
                    OASISResultHelper<IGalaxyCluster, IOmiverse>.CopyResult(galaxyClusterResult, result);
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
        /// Create's the OASIS Omniverse along with a new default Multiverse (with it's GrandSuperStar) containing the ThirdDimension containing UniversePrime (simulation) and the MagicVerse (contains OAPP's), which itself contains a default GalaxyCluster containing a default Galaxy (along with it's SuperStar) containing a default SolarSystem (along wth it's Star) containing a default planet (Our World).
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
            ErrorHandling.HandleError(ref result, errorMessage);
        }
    }
}
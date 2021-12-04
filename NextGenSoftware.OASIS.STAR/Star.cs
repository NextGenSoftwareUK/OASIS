using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
using NextGenSoftware.OASIS.STAR.ExtensionMethods;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.OASISAPIManager;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.EventArgs;
using NextGenSoftware.OASIS.STAR.ErrorEventArgs;
using NextGenSoftware.OASIS.STAR.Enums;

namespace NextGenSoftware.OASIS.STAR
{
    public static class STAR
    {
        const string STAR_DNA_DEFAULT_PATH = "DNA\\STAR_DNA.json";
        const string OASIS_DNA_DEFAULT_PATH = "DNA\\OASIS_DNA.json";

        private static StarStatus _status;
        private static Guid _starId = Guid.Empty;
        private static OASISAPI _OASISAPI = null;

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
        //public static GreatGrandSuperStar InnerStar { get; set; } //Only ONE of these can ever exist and is at the centre of the Omiverse (also only ONE).

        public static IGreatGrandSuperStar DefaultGreatGrandSuperStar { get; set; } //Will default to the GreatGrandSuperStar at the centre of our Omniverse.
        public static IGrandSuperStar DefaultGrandSuperStar { get; set; } //Will default to the GrandSuperStar at the centre of our Universe.
        public static ISuperStar DefaultSuperStar { get; set; } //Will default to the SuperStar at the centre of our Galaxy.
        public static IStar DefaultStar { get; set; } //Will default to our Sun.
        public static IPlanet DefaultPlanet { get; set; } //Will default to Our World.

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

        public static Avatar LoggedInAvatar { get; set; }
        public static AvatarDetail LoggedInAvatarDetail { get; set; }

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

        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        public static event HolonsLoaded OnHolonsLoaded;

        public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        public static event ZomesLoaded OnZomesLoaded;

        public delegate void HolonSaved(object sender, HolonSavedEventArgs e);
        public static event HolonSaved OnHolonSaved;

        public delegate void HolonLoaded(object sender, HolonLoadedEventArgs e);
        public static event HolonLoaded OnHolonLoaded;

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

        public delegate void ZomeError(object sender, ZomeErrorEventArgs e);
        public static event ZomeError OnZomeError;

        //TODO: Not sure if we want to expose the HoloNETClient events at this level? They can subscribe to them through the HoloNETClient property below...
        //public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        //public static event Disconnected OnDisconnected;

        //public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        //public static event DataReceived OnDataReceived;
       
        public static OASISResult<ICelestialBody> IgniteStar(string STARDNAPath = STAR_DNA_DEFAULT_PATH, string OASISDNAPath = OASIS_DNA_DEFAULT_PATH, string starId = null)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
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

            IgniteInnerStar(ref result);

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

            return OASISAPI.Avatar.Register(title, firstName, lastName, username, password, AvatarType.User, "https://api.oasisplatform.world/api", OASISType.STARCLI, cliColour, favColour);
        }

        public static async Task<OASISResult<IAvatar>> CreateAvatarAsync(string title, string firstName, string lastName, string username, string password, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
        {
            if (!IsStarIgnited)
                IgniteStar();

            //TODO: Implement Async version of Register and call instead of below:
            return OASISAPI.Avatar.Register(title, firstName, lastName, username, password, AvatarType.User, "https://api.oasisplatform.world/api", OASISType.STARCLI, cliColour, favColour);
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
                LoggedInAvatarDetail = (AvatarDetail)OASISAPI.Avatar.LoadAvatarDetail(LoggedInAvatar.Id);

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

            return result;
        }

        public static async Task<CoronalEjection> LightAsync(GenesisType type, string name, string dnaFolder = "", string genesisCSharpFolder = "", string genesisRustFolder = "", string genesisNameSpace = "")
        {
            return await LightAsync(type, name, (ICelestialBody)null, dnaFolder, genesisCSharpFolder, genesisRustFolder, genesisNameSpace);
        }

        public static async Task<CoronalEjection> LightAsync(GenesisType type, string name, IStar starToAddPlanetTo = null, string dnaFolder = "", string genesisCSharpFolder = "", string genesisRustFolder = "", string genesisNameSpace = "")
        {
            return await LightAsync(type, name, (ICelestialBody)starToAddPlanetTo, dnaFolder, genesisCSharpFolder, genesisRustFolder, genesisNameSpace);
        }

        public static async Task<CoronalEjection> LightAsync(GenesisType type, string name, IPlanet planetToAddMoonTo = null, string dnaFolder = "", string genesisCSharpFolder = "", string genesisRustFolder = "", string genesisNameSpace = "")
        {
            return await LightAsync(type, name, (ICelestialBody)planetToAddMoonTo, dnaFolder, genesisCSharpFolder, genesisRustFolder, genesisNameSpace);
        }

        //TODO: Create non async version of Light();
        private static async Task<CoronalEjection> LightAsync(GenesisType type, string name, ICelestialBody celestialBodyParent = null, string dnaFolder = "", string genesisCSharpFolder = "", string genesisRustFolder = "", string genesisNameSpace = "")
        {
            CelestialBody newBody = null;
            bool holonReached = false;
            string holonBufferRust = "";
            string holonBufferCsharp = "";
            string libBuffer = "";
            string holonName = "";
            string zomeName = "";
            string holonFieldsClone = "";
            int nextLineToWrite = 0;
            bool firstField = true;
            string iholonBuffer = "";
            string zomeBufferCsharp = "";
            string celestialBodyBufferCsharp = "";
            bool firstHolon = true;
            string rustDNAFolder = string.Empty;

            if (LoggedInAvatar == null)
                return new CoronalEjection() { ErrorOccured = true, Message = "Avatar is not logged in. Please log in before calling this command." };

            if (LoggedInAvatar.Level < 77 && type == GenesisType.Star)
                return new CoronalEjection() { ErrorOccured = true, Message = "Avatar must have reached level 77 before they can create stars. Please create a planet or moon instead..." };

            if (LoggedInAvatar.Level < 33 && type == GenesisType.Planet)
                return new CoronalEjection() { ErrorOccured = true, Message = "Avatar must have reached level 33 before they can create planets. Please create a moon instead..." };

            if (celestialBodyParent == null && type == GenesisType.Moon)
                return new CoronalEjection() { ErrorOccured = true, Message = "You must specify the planet to add the moon to." };

            if (!IsStarIgnited)
                IgniteStar();

            if (DefaultStar == null)
            {
                OASISResult<ICelestialBody> result = await IgniteInnerStarAsync();

                if (result.IsError)
                    return new CoronalEjection() { ErrorOccured = true, Message = string.Concat("Error Igniting Inner Star. Reason: ", result.Message) };
            }

            ValidateLightDNA(dnaFolder, genesisCSharpFolder, genesisRustFolder);

            switch (STARDNA.HolochainVersion.ToUpper())
            {
                case "REDUX":
                    rustDNAFolder = $"{STARDNA.BasePath}\\{STARDNA.RustDNAReduxTemplateFolder}";
                    break;

                case "RSM":
                    rustDNAFolder = $"{STARDNA.BasePath}\\{STARDNA.RustDNARSMTemplateFolder}";
                    break;
            }

            string libTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", STARDNA.RustTemplateLib)).OpenText().ReadToEnd();
            string createTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", STARDNA.RustTemplateCreate)).OpenText().ReadToEnd();
            string readTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", STARDNA.RustTemplateRead)).OpenText().ReadToEnd();
            string updateTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", STARDNA.RustTemplateUpdate)).OpenText().ReadToEnd();
            string deleteTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", STARDNA.RustTemplateDelete)).OpenText().ReadToEnd();
            string listTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", STARDNA.RustTemplateList)).OpenText().ReadToEnd();
            string validationTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", STARDNA.RustTemplateValidation)).OpenText().ReadToEnd();
            string holonTemplateRust = new FileInfo(string.Concat(rustDNAFolder, "\\", STARDNA.RustTemplateHolon)).OpenText().ReadToEnd();
            string intTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", STARDNA.RustTemplateInt)).OpenText().ReadToEnd();
            string stringTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", STARDNA.RustTemplateString)).OpenText().ReadToEnd();
            string boolTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", STARDNA.RustTemplateBool)).OpenText().ReadToEnd();

            string iHolonTemplate = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateIHolonDNA)).OpenText().ReadToEnd();
            string holonTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateHolonDNA)).OpenText().ReadToEnd();
            string zomeTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateZomeDNA)).OpenText().ReadToEnd();
            string iStarTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateIStarDNA)).OpenText().ReadToEnd();
            string starTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateStarDNA)).OpenText().ReadToEnd();
            string iPlanetTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateIPlanetDNA)).OpenText().ReadToEnd();
            string planetTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplatePlanetDNA)).OpenText().ReadToEnd();
            string iMoonTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateIMoonDNA)).OpenText().ReadToEnd();
            string moonTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateMoonDNA)).OpenText().ReadToEnd();
            string TemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplatePlanetDNA)).OpenText().ReadToEnd();

            string iCelestialBodyTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateICelestialBodyDNA)).OpenText().ReadToEnd();
            string celestialBodyTemplateCsharp = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateCelestialBodyDNA)).OpenText().ReadToEnd();
            string iZomeTemplate = new FileInfo(string.Concat(STARDNA.BasePath, "\\", STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateIZomeDNA)).OpenText().ReadToEnd();

            //If folder is not passed in via command line args then use default in config file.
            if (string.IsNullOrEmpty(dnaFolder))
                dnaFolder = $"{STARDNA.BasePath}\\{STARDNA.CelestialBodyDNA}";

            if (string.IsNullOrEmpty(genesisCSharpFolder))
                genesisCSharpFolder = $"{STARDNA.BasePath}\\{STARDNA.GenesisCSharpFolder}";

            if (string.IsNullOrEmpty(genesisRustFolder))
                genesisRustFolder = $"{STARDNA.BasePath}\\{STARDNA.GenesisRustFolder}";

            if (string.IsNullOrEmpty(genesisNameSpace))
                genesisNameSpace = $"{STARDNA.BasePath}\\{STARDNA.GenesisNamespace}";

            DirectoryInfo dirInfo = new DirectoryInfo(dnaFolder);
            FileInfo[] files = dirInfo.GetFiles();

            switch (type)
            {
                case GenesisType.Moon:
                    {
                        newBody = new Moon();

                        if (celestialBodyParent == null)
                            celestialBodyParent = DefaultPlanet;

                        Mapper<IPlanet, Moon>.MapParentCelestialBodyProperties((IPlanet)celestialBodyParent, (Moon)newBody);
                        //newBody.ParentHolon = celestialBodyParent;
                        //newBody.ParentHolonId = celestialBodyParent.Id;
                        //newBody.ParentPlanet = (IPlanet)celestialBodyParent;
                        //newBody.ParentPlanetId = celestialBodyParent.ParentPlanetId;
                        //newBody.ParentStar = celestialBodyParent.ParentStar;
                        //newBody.ParentStarId = celestialBodyParent.ParentStarId;
                    }
                    break;

                case GenesisType.Planet:
                    {
                        newBody = new Planet();

                        //If no parent Star is passed in then set the parent star to our Sun.
                        if (celestialBodyParent == null)
                            celestialBodyParent = DefaultStar;

                        Mapper<IStar, Planet>.MapParentCelestialBodyProperties((IStar)celestialBodyParent, (Planet)newBody);
                        //newBody.ParentHolon = celestialBodyParent;
                        //newBody.ParentHolonId = celestialBodyParent.Id;
                        //newBody.ParentStar = (IStar)celestialBodyParent;
                        //newBody.ParentStarId = celestialBodyParent.Id;
                    }
                break;

                case GenesisType.Star:
                    {
                        newBody = new Star();

                        if (celestialBodyParent == null)
                            celestialBodyParent = DefaultSuperStar;

                        Mapper<ISuperStar, Star>.MapParentCelestialBodyProperties((ISuperStar)celestialBodyParent, (Star)newBody);
                        //newBody.ParentHolon = celestialBodyParent;
                        //newBody.ParentHolonId = celestialBodyParent.Id;
                        //newBody.ParentStar = (IStar)celestialBodyParent;
                        //newBody.ParentStarId = celestialBodyParent.Id;
                    }
                break;

                case GenesisType.Galaxy:
                    {
                        newBody = new SuperStar();

                        if (celestialBodyParent == null)
                            celestialBodyParent = DefaultGrandSuperStar;

                        Mapper<IGrandSuperStar, SuperStar>.MapParentCelestialBodyProperties((IGrandSuperStar)celestialBodyParent, (SuperStar)newBody);
                    }
                    break;

                case GenesisType.Universe:
                    {
                        newBody = new GrandSuperStar();

                        if (celestialBodyParent == null)
                            celestialBodyParent = DefaultGreatGrandSuperStar;

                        Mapper<IGreatGrandSuperStar, GrandSuperStar>.MapParentCelestialBodyProperties((IGreatGrandSuperStar)celestialBodyParent, (GrandSuperStar)newBody);
                    }
                    break;
            }

            // newBody.CelestialBody = newBody; //TODO: Causes an infinite recursion because CelestialBody is a Holon itself so its linking to itself.
            newBody.Id = Guid.NewGuid(); //TODO: Not sure if to put this into HolonBase or not?
            //newBody.IsNewHolon = true;
            newBody.Name = name;
            newBody.OnZomeError += NewBody_OnZomeError;
            //await newBody.InitializeAsync();

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
                            zomeBufferCsharp = zomeBufferCsharp.Replace("{zome}", parts[6].ToSnakeCase());
                            zomeName = parts[6].ToPascalCase();

                            currentZome = new Zome()
                            {
                                Name = zomeName,
                                CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI),
                                HolonType = HolonType.Zome,
                                ParentHolonId = newBody.Id,
                                ParentPlanetId = newBody.HolonType == HolonType.Planet ? newBody.Id : Guid.Empty,
                                ParentMoonId = newBody.HolonType == HolonType.Moon ? newBody.Id : Guid.Empty
                            };

                            //currentZome = new Zome() { Name = zomeName, HolonType = HolonType.Zome, ParentId = newBody.Id, ParentCelestialBodyId = newBody.Id };
                            await newBody.CelestialBodyCore.AddZomeAsync(currentZome); //TODO: May need to save this once holons and nodes/fields have been added?
                        }

                        if (holonReached && buffer.Contains("string") || buffer.Contains("int") || buffer.Contains("bool"))
                        {
                            string[] parts = buffer.Split(' ');
                            string fieldName = parts[14].ToSnakeCase();

                            switch (parts[13].ToLower())
                            {
                                case "string":
                                    GenerateRustField(fieldName, stringTemplate, NodeType.String, holonName, currentHolon, ref firstField, ref holonFieldsClone, ref holonBufferRust);
                                    break;

                                case "int":
                                    GenerateRustField(fieldName, intTemplate, NodeType.Int, holonName, currentHolon, ref firstField, ref holonFieldsClone, ref holonBufferRust);
                                    break;

                                case "bool":
                                    GenerateRustField(fieldName, boolTemplate, NodeType.Bool, holonName, currentHolon, ref firstField, ref holonFieldsClone, ref holonBufferRust);
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

                            if (!firstHolon)
                            {
                                //TODO: Need to make dynamic so no need to pass length in (had issues before but will try again later... :) )
                                zomeBufferCsharp = GenerateDynamicZomeFunc("Load", zomeTemplateCsharp, holonName, zomeBufferCsharp, 170);
                                zomeBufferCsharp = GenerateDynamicZomeFunc("Save", zomeTemplateCsharp, holonName, zomeBufferCsharp, 147);
                            }

                            holonName = holonName.ToPascalCase();

                            File.WriteAllText(string.Concat(genesisCSharpFolder, "\\I", holonName, ".cs"), iholonBuffer);
                            File.WriteAllText(string.Concat(genesisCSharpFolder, "\\", holonName, ".cs"), holonBufferCsharp);

                            //TDOD: Finish putting in IZomeBuffer etc
                            //   File.WriteAllText(string.Concat(genesisCSharpFolder, "\\I", holonName, ".cs"), izomeBuffer);
                            // File.WriteAllText(string.Concat(genesisCSharpFolder, "\\", zomeName, ".cs"), zomeBufferCsharp);

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
                            iholonBuffer = iHolonTemplate.Replace("IHolonDNATemplate", string.Concat("I", parts[10]));

                            zomeBufferCsharp = zomeBufferCsharp.Replace("HOLON", parts[10].ToPascalCase());
                            zomeBufferCsharp = zomeBufferCsharp.Replace("{holon}", parts[10].ToSnakeCase());

                            zomeBufferCsharp = zomeBufferCsharp.Replace(STARDNA.TemplateNamespace, genesisNameSpace);
                            holonBufferCsharp = holonBufferCsharp.Replace(STARDNA.TemplateNamespace, genesisNameSpace);
                            iholonBuffer = iholonBuffer.Replace(STARDNA.TemplateNamespace, genesisNameSpace);

                            if (string.IsNullOrEmpty(celestialBodyBufferCsharp))
                                celestialBodyBufferCsharp = celestialBodyTemplateCsharp;

                            celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace(STARDNA.TemplateNamespace, genesisNameSpace);
                            celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("CelestialBodyDNATemplate", name.ToPascalCase());
                            celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("{holon}", parts[10].ToSnakeCase()).Replace("HOLON", parts[10].ToPascalCase());
                            celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("CelestialBody", Enum.GetName(typeof(GenesisType), type)).Replace("ICelestialBody", string.Concat("I", Enum.GetName(typeof(GenesisType), type)));
                            celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("ICelestialBody", string.Concat("I", Enum.GetName(typeof(GenesisType), type)));
                            //celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("GenesisType.Star", string.Concat("GenesisType.", Enum.GetName(typeof(GenesisType), type)));
                            celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace(", GenesisType.Star", "");

                            /*
                            switch (type)
                            {
                                case GenesisType.Star:
                                    celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("CelestialBody", "Star").Replace("ICelestialBody", "IStar");
                                    break;

                                case GenesisType.Planet:
                                    celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("CelestialBody", "Planet").Replace("ICelestialBody", "IPlanet");
                                    break;

                                case GenesisType.Moon:
                                    celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("CelestialBody", "Moon").Replace("ICelestialBody", "IMoon");
                                    break;
                            }*/

                            // TODO: Current Zome Id will be empty here so need to save the zome before? (above when the zome is first created and added to the newBody zomes collection).
                            currentHolon = new Holon() 
                            { 
                                Name = holonName,
                                CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI),
                                HolonType = HolonType.Holon, 
                                ParentHolonId = currentZome.Id, 
                                ParentZomeId = currentZome.Id, 
                                ParentPlanetId = newBody.HolonType == HolonType.Planet ? newBody.Id : Guid.Empty, 
                                ParentMoonId = newBody.HolonType == HolonType.Moon ? newBody.Id : Guid.Empty 
                            };

                            currentZome.Holons.Add((Holon)currentHolon); 

                            holonName = holonName.ToSnakeCase();
                            holonReached = true;
                        }
                    }

                    reader.Close();
                    nextLineToWrite = 0;

                    File.WriteAllText(string.Concat(genesisRustFolder, "\\lib.rs"), libBuffer);
                    File.WriteAllText(string.Concat(genesisCSharpFolder, "\\", zomeName, ".cs"), zomeBufferCsharp);
                }
            }

            // Remove any white space from the name.
            File.WriteAllText(string.Concat(genesisCSharpFolder, "\\", Regex.Replace(name, @"\s+", ""), Enum.GetName(typeof(GenesisType), type), ".cs"), celestialBodyBufferCsharp);

          //  if (currentZome != null)
           //     newBody.CelestialBodyCore.Zomes.Add(currentZome);

            //TODO: Need to save the collection of Zomes/Holons that belong to this planet here...
            await newBody.SaveAsync(); // Need to save again so newly added zomes/holons/nodes are also saved.

            switch (type)
            {
                case GenesisType.Moon:
                    {
                        OASISResult<IMoon> result =  await ((StarCore)celestialBodyParent.CelestialBodyCore).AddMoonAsync(newBody.ParentPlanet, (IMoon)newBody);

                        if (result != null)
                        {
                            if (result.IsError)
                                return new CoronalEjection() { ErrorOccured = true, Message = result.Message, CelestialBody = result.Result };
                            else
                                return new CoronalEjection() { ErrorOccured = false, Message = "Moon Successfully Created.", CelestialBody = result.Result };
                        }
                        else
                            return new CoronalEjection() { ErrorOccured = true, Message = "Unknown Error Occured Creating Moon." };
                    }

                case GenesisType.Planet:
                    {
                        OASISResult<IPlanet> result = await ((StarCore)celestialBodyParent.CelestialBodyCore).AddPlanetAsync((IPlanet)newBody);

                        if (result != null)
                        { 
                            if (result.IsError)
                                return new CoronalEjection() { ErrorOccured = true, Message = result.Message, CelestialBody = result.Result };
                            else
                                return new CoronalEjection() { ErrorOccured = false, Message = "Planet Successfully Created.", CelestialBody = result.Result };
                        }
                        else
                            return new CoronalEjection() { ErrorOccured = true, Message = "Unknown Error Occured Creating Planet." };
                    }

                case GenesisType.Star:
                    {
                        OASISResult<IStar> result = await ((ISuperStarCore)celestialBodyParent.CelestialBodyCore).AddStarAsync((IStar)newBody);

                        if (result != null)
                        {
                            if (result.IsError)
                                return new CoronalEjection() { ErrorOccured = true, Message = result.Message, CelestialBody = result.Result };
                            else
                                return new CoronalEjection() { ErrorOccured = false, Message = "Star Successfully Created.", CelestialBody = result.Result };
                        }
                        else
                            return new CoronalEjection() { ErrorOccured = true, Message = "Unknown Error Occured Creating Star." };
                    }

                case GenesisType.SoloarSystem:
                    {
                        OASISResult<ISolarSystem> result = await ((StarCore)celestialBodyParent.CelestialBodyCore).AddSolarSystemAsync(new SolarSystem() { Star = (IStar)newBody });

                        if (result != null)
                        {
                            if (result.IsError)
                                return new CoronalEjection() { ErrorOccured = true, Message = result.Message, CelestialSpace = result.Result, CelestialBody = result.Result.Star };
                            else
                                return new CoronalEjection() { ErrorOccured = false, Message = "Star/SoloarSystem Successfully Created.", CelestialSpace = result.Result, CelestialBody = result.Result.Star };
                        }
                        else
                            return new CoronalEjection() { ErrorOccured = true, Message = "Unknown Error Occured Creating Star/SoloarSystem." };
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
                    return new CoronalEjection() { ErrorOccured = true, Message = "Unknown Error Occured.", CelestialBody = newBody };
            }

            //Generate any native code for the current provider.
            //TODO: Add option to pass into STAR which providers to generate native code for (can be more than one provider).
            ((IOASISSuperStar)ProviderManager.CurrentStorageProvider).NativeCodeGenesis(newBody);

            //TODO: Need to save this to the StarNET store (still to be made!) (Will of course be written on top of the HDK/ODK...
            //This will be private on the store until the user publishes via the Star.Seed() command.
        }

        // Build
        public static CoronalEjection Flare(string bodyName)
        {
            //TODO: Build rust code using hc conductor and .net code using dotnet compiler.
            return new CoronalEjection();
        }

        public static CoronalEjection Flare(CelestialBody body)
        {
            //TODO: Build rust code using hc conductor and .net code using dotnet compiler.
            return new CoronalEjection();
        }

        //Activate & Launch - Launch & activate a planet (OAPP) by shining the star's light upon it...
        public static void Shine(CelestialBody body)
        {

        }

        public static void Shine(string bodyName)
        {

        }

        //Dractivate
        public static void Dim(CelestialBody body)
        {

        }

        public static void Dim(string bodyName)
        {

        }

        //Deploy
        public static void Seed(CelestialBody body)
        {

        }

        public static void Seed(string bodyName)
        {

        }

        // Run Tests
        public static void Twinkle(CelestialBody body)
        {

        }

        public static void Twinkle(string bodyName)
        {

        }

        // Delete Planet (OAPP)
        public static void Dust(CelestialBody body)
        {

        }

        public static void Dust(string bodyName)
        {

        }

        // Delete Planet (OAPP)
        public static void Evolve(CelestialBody body)
        {

        }

        public static void Evolve(string bodyName)
        {

        }

        // Delete Planet (OAPP)
        public static void Mutate(CelestialBody body)
        {

        }

        public static void Mutate(string bodyName)
        {

        }

        // Highlight the Planet (OAPP) in the OAPP Store (StarNET)
        public static void Radiate(CelestialBody body)
        {

        }

        public static void Radiate(string bodyName)
        {

        }

        // Show how much light the planet (OAPP) is emitting into the solar system (StarNET/HoloNET)
        public static void Emit(CelestialBody body)
        {

        }

        public static void Emit(string bodyName)
        {

        }

        // Show stats of the Planet (OAPP)
        public static void Reflect(CelestialBody body)
        {

        }

        public static void Reflect(string bodyName)
        {

        }

        // Send/Receive Love
        public static void Love(CelestialBody body)
        {

        }

        public static void Love(string body)
        {

        }

        // Show network stats/management/settings
        public static void Burst(CelestialBody body)
        {

        }

        public static void Burst(string body)
        {

        }

        // ????
        public static void Pulse(CelestialBody body)
        {

        }

        public static void Pulse(string body)
        {

        }

        // Reserved For Future Use...
        public static void Super(CelestialBody body)
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
                ValidateFolder(starDNA.BasePath, starDNA.GenesisCSharpFolder, "STARDNA.GenesisCSharpFolder", false, true);
                ValidateFolder(starDNA.BasePath, starDNA.GenesisRustFolder, "STARDNA.GenesisRustFolder", false, true);
                ValidateFolder(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, "STARDNA.CSharpDNATemplateFolder");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateHolonDNA, "STARDNA.CSharpTemplateHolonDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateZomeDNA, "STARDNA.CSharpTemplateZomeDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateIStarDNA, "STARDNA.CSharpTemplateIStarDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateStarDNA, "STARDNA.CSharpTemplateStarDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateIPlanetDNA, "STARDNA.CSharpTemplateIPlanetDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateIPlanetDNA, "STARDNA.CSharpTemplatePlanetDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateIMoonDNA, "STARDNA.CSharpTemplateIMoonDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateIMoonDNA, "STARDNA.CSharpTemplateMoonDNA");
                ValidateFile(starDNA.BasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateCelestialBodyDNA, "STARDNA.CSharpTemplateCelestialBodyDNA");

                switch (starDNA.HolochainVersion.ToUpper())
                {
                    case "REDUX":
                        {
                            ValidateFolder(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, "STARDNA.RustDNAReduxTemplateFolder");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateCreate, "STARDNA.RustTemplateCreate");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateDelete, "STARDNA.RustTemplateDelete");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateLib, "STARDNA.RustTemplateLib");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateRead, "STARDNA.RustTemplateRead");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateUpdate, "STARDNA.RustTemplateUpdate");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateList, "STARDNA.RustTemplateList");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateValidation, "STARDNA.RustTemplateValidation");
                        }
                        break;

                    case "RSM":
                        {
                            ValidateFolder(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, "STARDNA.RustDNARSMTemplateFolder");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateCreate, "STARDNA.RustTemplateCreate");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateDelete, "STARDNA.RustTemplateDelete");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateLib, "STARDNA.RustTemplateLib");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateRead, "STARDNA.RustTemplateRead");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateUpdate, "STARDNA.RustTemplateUpdate");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateList, "STARDNA.RustTemplateList");
                            ValidateFile(starDNA.BasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateValidation, "STARDNA.RustTemplateValidation");
                        }
                        break;
                }
            }
            else
                throw new ArgumentNullException("STARDNA is null, please check and try again.");
        }

        private static void ValidateLightDNA(string dnaFolder, string genesisCSharpFolder, string genesisRustFolder)
        {
            ValidateFolder("", dnaFolder, "dnaFolder");
            ValidateFolder("", genesisCSharpFolder, "genesisCSharpFolder", false, true);
            ValidateFolder("", genesisRustFolder, "genesisRustFolder", false, true);
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

            currentHolon.Nodes.Add(new Node { NodeName = fieldName.ToPascalCase(), NodeType = nodeType, ParentId = currentHolon.Id });
        }
       
        private static OASISResult<bool> BootOASIS(string OASISDNAPath = OASIS_DNA_DEFAULT_PATH)
        {
            STAR.OASISDNAPath = OASISDNAPath;

            if (!OASISAPI.IsOASISBooted)
                return OASISAPI.BootOASIS(STAR.OASISDNAPath);
            else
                return new OASISResult<bool>() { Message = "OASIS Already Booted" };
        }
        
        private static OASISResult<ICelestialBody> IgniteInnerStar(ref OASISResult<ICelestialBody> result)
        {
            _starId = Guid.Empty; //TODO:Temp, remove after!

            if (_starId == Guid.Empty)
                result = OASISOmniverseGenesisAsync().Result;
            else
                DefaultStar = new Star(_starId); //TODO: Temp set InnerStar as The Sun at the centre of our Solar System.

            WireUpEvents();
            return result;
        }

        private static async Task<OASISResult<ICelestialBody>> IgniteInnerStarAsync()
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();

            _starId = Guid.Empty; //TODO:Temp, remove after!

            if (_starId == Guid.Empty)
                result = await OASISOmniverseGenesisAsync();
            else
                DefaultStar = new Star(_starId); //TODO: Temp set InnerStar as The Sun at the centre of our Solar System.

            WireUpEvents();
            return result;
        }

        /// <summary>
        /// Create's the OASIS Omniverse along with a new default Multiverse (with it's GrandSuperStar) containing the ThirdDimension containing UniversePrime (simulation) and the MagicVerse (contains OAPP's), which itself contains a default GalaxyCluster containing a default Galaxy (along with it's SuperStar) containing a default SolarSystem (along wth it's Star) containing a default planet (Our World).
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static async Task<OASISResult<ICelestialBody>> OASISOmniverseGenesisAsync()
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();

            Omiverse omniverse = new Omiverse();
            omniverse.IsNewHolon = true;
            omniverse.Id = Guid.NewGuid();
            omniverse.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);

            GreatGrandSuperStar greatGrandSuperStar = new GreatGrandSuperStar(); //GODHEAD ;-)
            greatGrandSuperStar.IsNewHolon = true;
            greatGrandSuperStar.Name = "GreatGrandSuperStar";
            greatGrandSuperStar.Description = "GreatGrandSuperStar at the centre of the Omniverse (The OASIS). Can create Multiverses, Universes, Galaxies, SolarSystems, Stars, Planets (Super OAPPS) and moons (OAPPS)";
            greatGrandSuperStar.ParentOmiverse = omniverse;
            greatGrandSuperStar.ParentOmiverseId = omniverse.Id;
            greatGrandSuperStar.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
            //await greatGrandSuperStar.InitializeAsync();
            result = await greatGrandSuperStar.SaveAsync();

            if (!result.IsError && result.Result != null)
            {
                STARDNA.DefaultGreatGrandSuperStarId = greatGrandSuperStar.Id.ToString();

                omniverse.Name = "The OASIS Omniverse";
                omniverse.Description = "The OASIS Omniverse that contains everything else.";
                omniverse.GreatGrandSuperStar = greatGrandSuperStar;
                omniverse.ParentGreatGrandSuperStar = greatGrandSuperStar;
                omniverse.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                OASISResult<IOmiverse> omiverseResult = await ((GreatGrandSuperStarCore)greatGrandSuperStar.CelestialBodyCore).AddOmiverseAsync(omniverse);

                if (!omiverseResult.IsError && omiverseResult.Result != null)
                {
                    Multiverse multiverse = new Multiverse();
                    multiverse.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                    multiverse.Name = "Our Multiverse.";
                    multiverse.Description = "Our Multiverse that our Milky Way Galaxy belongs to, the default Multiverse.";
                    multiverse.ParentOmiverse = omiverseResult.Result;
                    multiverse.ParentOmiverseId = omiverseResult.Result.Id;
                    multiverse.ParentGreatGrandSuperStar = greatGrandSuperStar;
                    multiverse.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;
                    multiverse.GrandSuperStar.Name = "The GrandSuperStar at the centre of our Multiverse/Universe.";

                    OASISResult<IMultiverse> multiverseResult = await ((GreatGrandSuperStarCore)greatGrandSuperStar.CelestialBodyCore).AddMultiverseAsync(multiverse);

                    if (!multiverseResult.IsError && multiverseResult.Result != null)
                    {
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

                        OASISResult<IGalaxyCluster> galaxyClusterResult = await ((GrandSuperStarCore)multiverse.GrandSuperStar.CelestialBodyCore).AddGalaxyClusterToUniverseAsync(multiverse.Dimensions.ThirdDimension.MagicVerse, galaxyCluster);

                        if (!galaxyClusterResult.IsError && galaxyClusterResult.Result != null)
                        {
                            galaxyCluster = (GalaxyCluster)galaxyClusterResult.Result;

                            Galaxy galaxy = new Galaxy();
                            galaxy.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                            galaxy.Name = "Our Milky Way Galaxy";
                            galaxy.Description = "Our Milky Way Galaxy, which is the default Galaxy.";
                            Mapper<IGalaxyCluster, Galaxy>.MapParentCelestialBodyProperties(galaxyCluster, galaxy);
                            galaxy.ParentGalaxyCluster = galaxyCluster;
                            galaxy.ParentGalaxyClusterId = galaxyCluster.Id;

                            OASISResult<IGalaxy> galaxyResult = await ((GrandSuperStarCore)multiverse.GrandSuperStar.CelestialBodyCore).AddGalaxyToGalaxyClusterAsync(galaxyCluster, galaxy);

                            if (!galaxyResult.IsError && galaxyResult.Result != null)
                            {
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

                                OASISResult<IStar> starResult = await ((SuperStarCore)galaxy.SuperStar.CelestialBodyCore).AddStarAsync(solarSystem.Star);

                                if (!starResult.IsError && starResult.Result != null)
                                {
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
                                    OASISResult<ISolarSystem> solarSystemResult = await ((StarCore)solarSystem.Star.CelestialBodyCore).AddSolarSystemAsync(solarSystem);

                                    if (!solarSystemResult.IsError && solarSystemResult.Result != null)
                                    {
                                        solarSystem = (SolarSystem)solarSystemResult.Result;

                                        Planet ourWorld = new Planet();
                                        ourWorld.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                                        ourWorld.Name = "Our World";
                                        ourWorld.Description = "The digital twin of our planet and the default planet.";
                                        Mapper<ISolarSystem, Planet>.MapParentCelestialBodyProperties(solarSystem, ourWorld);
                                        ourWorld.ParentSolarSystem = solarSystem;
                                        ourWorld.ParentSolarSystemId = solarSystem.Id;
                                       // await ourWorld.InitializeAsync();

                                        OASISResult<IPlanet> ourWorldResult = await ((StarCore)solarSystem.Star.CelestialBodyCore).AddPlanetAsync(ourWorld);

                                        if (!ourWorldResult.IsError && ourWorldResult.Result != null)
                                        {
                                            ourWorld = (Planet)ourWorldResult.Result;
                                            STARDNA.DefaultPlanetId = ourWorld.Id.ToString();
                                        }
                                        else
                                            OASISResultHolonToHolonHelper<IPlanet, ICelestialBody>.CopyResult(ourWorldResult, result);
                                    }
                                    else
                                        OASISResultHolonToHolonHelper<ISolarSystem, ICelestialBody>.CopyResult(solarSystemResult, result);
                                }
                                else
                                    OASISResultHolonToHolonHelper<IStar, ICelestialBody>.CopyResult(starResult, result);
                            }
                            else
                                OASISResultHolonToHolonHelper<IGalaxy, ICelestialBody>.CopyResult(galaxyResult, result);
                        }
                        else
                            OASISResultHolonToHolonHelper<IGalaxyCluster, ICelestialBody>.CopyResult(galaxyClusterResult, result);
                    }
                    else
                        OASISResultHolonToHolonHelper<IMultiverse, ICelestialBody>.CopyResult(multiverseResult, result);
                }
                else
                    OASISResultHolonToHolonHelper<IOmiverse, ICelestialBody>.CopyResult(omiverseResult, result);
            }

            SaveDNA();

            if (!result.IsError)
                result.Message = "STAR Ignited and The OASIS Omniverse Created.";
            
            return result;
        }

        private static void HandleErrorMessage<T>(ref OASISResult<T> result, string errorMessage)
        {
            OnStarError?.Invoke(null, new StarErrorEventArgs() { Reason = errorMessage });
            ErrorHandling.HandleError(ref result, errorMessage);
        }
    }
}
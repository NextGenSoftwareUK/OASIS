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
    public static class Star
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

        public static bool IsSuperStarIgnited { get; private set; }
        public static GreatGrandSuperStar InnerStar { get; set; } //Only ONE of these can ever exist and is at the centre of the Omiverse (also only ONE).

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

        public static Avatar LoggedInUser { get; set; }

        public static OASISAPI OASISAPI
        {
            get
            {
                if (_OASISAPI == null)
                    _OASISAPI = new OASISAPI();

                return _OASISAPI;
            }
        }

        public static IMapper Mapper { get; set; }

        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        public static event HolonsLoaded OnHolonsLoaded;

        public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        public static event ZomesLoaded OnZomesLoaded;

        public delegate void HolonSaved(object sender, HolonSavedEventArgs e);
        public static event HolonSaved OnHolonSaved;

        public delegate void HolonLoaded(object sender, HolonLoadedEventArgs e);
        public static event HolonLoaded OnHolonLoaded;

        public delegate void SuperStarIgnited(object sender, StarIgnitedEventArgs e);
        public static event SuperStarIgnited OnSuperStarIgnited;

        public delegate void SuperStarCoreIgnited(object sender, System.EventArgs e);
        public static event SuperStarCoreIgnited OnSuperStarCoreIgnited;

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
       
        public static OASISResult<ICelestialBody> IgniteSuperStar(string STARDNAPath = STAR_DNA_DEFAULT_PATH, string OASISDNAPath = OASIS_DNA_DEFAULT_PATH, string starId = null)
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
            else if (!string.IsNullOrEmpty(STARDNA.StarId) && !string.IsNullOrWhiteSpace(STARDNA.StarId) && !Guid.TryParse(STARDNA.StarId, out _starId))
            {
                HandleErrorMessage(ref result, "StarID defined in the STARDNA file in is invalid. It needs to be a valid Guid.");
                return result;
            }

            // SuperStarCore = new SuperStarCore(_starId);
            // WireUpEvents();

            //IgniteInnerStar(ref result, starId);

            //if (!result.IsError)
            //{
            //    SuperStarCore = new SuperStarCore(InnerStar.Id);
            //    WireUpEvents();
            //}

            Status = StarStatus.Ingited;
            OnSuperStarIgnited.Invoke(null, new StarIgnitedEventArgs() { Message = result.Message });
            IsSuperStarIgnited = true;
            return result;



            // OASISResult<ICelestialBody> result = IgniteSuperStarInternal(STARDNAPath, OASISDNAPath, starId);

            /*
            if (!result.IsError && InnerStar.Id == Guid.Empty)
            {
                //TODO: Implement Save method (non async version) and call instead of below:
                result = InnerStar.SaveAsync().Result;

                if (!result.IsError && result.IsSaved)
                {
                    result.Message = "SuperSTAR Ignited";
                    STARDNA.StarId = InnerStar.Id.ToString(); 
                    SaveDNA();
                }
            }*/

        }

        /*
        public static async Task<OASISResult<ICelestialBody>> IgniteSuperStarAsync(string STARDNAPath = STAR_DNA_DEFAULT_PATH, string OASISDNAPath = OASIS_DNA_DEFAULT_PATH, string starId = null)
        {
            OASISResult<ICelestialBody> result = IgniteSuperStarInternal(STARDNAPath, OASISDNAPath, starId);

            if (!result.IsError && InnerStar.Id == Guid.Empty)
            {
                result = await InnerStar.SaveAsync();

                if (!result.IsError && result.IsSaved)
                {
                    result.Message = "SuperSTAR Ignited";
                    STARDNA.StarId = InnerStar.Id.ToString(); //TODO: May just store this internally by adding a LoadSuperStar method which would call LoadHolon passing in HolonType SuperStar (depends if if there will be more than one SuperStar in future? ;-) ) Maybe for distributing so can easier handle load? It's one SuperStar per Galaxy so could have more than one Galaxy? So The OASIS and COSMIC would be a full Universe with multiple Galaxies with their own SuperStar in the centre... ;-) YES! But would we need a GrandSuperStar then? For the centre of the Universe? Which will connect to other Universes and creates SuperStars? Or could a SuperStar just create other SuperStars? :) Yes think better to just for now allow SuperStar to create other SuperStars... ;-)
                    SaveDNA();
                }
            }

            return result;
        }*/

        public static OASISResult<bool> ExtinguishSuperStar()
        {
            return OASISAPI.ShutdownOASIS();
        }

        private static void WireUpEvents()
        {
            InnerStar.OnHolonLoaded += InnerStar_OnHolonLoaded;
            InnerStar.OnHolonSaved += InnerStar_OnHolonSaved;
            InnerStar.OnHolonsLoaded += InnerStar_OnHolonsLoaded;
            InnerStar.OnZomeError += InnerStar_OnZomeError;
            InnerStar.OnInitialized += InnerStar_OnInitialized;
        }

        private static void InnerStar_OnInitialized(object sender, System.EventArgs e)
        {
            OnSuperStarCoreIgnited?.Invoke(sender, e);
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

            if (!IsSuperStarIgnited)
                IgniteSuperStar();

            OASISResult<IAvatar> result = await OASISAPI.Avatar.AuthenticateAsync(username, password, IPAddress);

            if (!result.IsError)
                LoggedInUser = (Avatar)result.Result;

            return result;
        }

        public static OASISResult<IAvatar> CreateAvatar(string title, string firstName, string lastName, string username, string password, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
        {
            if (!IsSuperStarIgnited)
                IgniteSuperStar();

            return OASISAPI.Avatar.Register(title, firstName, lastName, username, password, AvatarType.User, "https://api.oasisplatform.world/api", OASISType.STARCLI, cliColour, favColour);
        }

        public static async Task<OASISResult<IAvatar>> CreateAvatarAsync(string title, string firstName, string lastName, string username, string password, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green)
        {
            if (!IsSuperStarIgnited)
                IgniteSuperStar();

            //TODO: Implement Async version of Register and call instead of below:
            return OASISAPI.Avatar.Register(title, firstName, lastName, username, password, AvatarType.User, "https://api.oasisplatform.world/api", OASISType.STARCLI, cliColour, favColour);
        }

        public static OASISResult<IAvatar> BeamIn(string username, string password)
        {
            string hostName = Dns.GetHostName();
            string IPAddress = Dns.GetHostEntry(hostName).AddressList[2].ToString();
            //string IPAddress = Dns.GetHostByName(hostName).AddressList[3].ToString();
            //+string IPAddress = Dns.GetHostByName(hostName).AddressList[4].ToString();

            if (!IsSuperStarIgnited)
                IgniteSuperStar();

            OASISResult<IAvatar> result = OASISAPI.Avatar.Authenticate(username, password, IPAddress);

            if (!result.IsError)
                LoggedInUser = (Avatar)result.Result;

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

            if (LoggedInUser == null)
                return new CoronalEjection() { ErrorOccured = true, Message = "Avatar is not logged in. Please log in before calling this command." };

            if (LoggedInUser.Level < 77 && type == GenesisType.Planet)
                return new CoronalEjection() { ErrorOccured = true, Message = "Avatar must have reached level 77 before they can create stars. Please create a planet or moon instead..." };

            if (LoggedInUser.Level < 33 && type == GenesisType.Planet)
                return new CoronalEjection() { ErrorOccured = true, Message = "Avatar must have reached level 33 before they can create planets. Please create a moon instead..." };

            if (celestialBodyParent == null && type == GenesisType.Moon)
                return new CoronalEjection() { ErrorOccured = true, Message = "You must specify the planet to add the moon to." };

            if (!IsSuperStarIgnited)
                IgniteSuperStar();

            if (InnerStar == null)
            {
                OASISResult<ICelestialBody> result = await IgniteInnerStarAsync();

                if (result.IsError)
                    return new CoronalEjection() { ErrorOccured = true, Message = string.Concat("Error Igniting Inner Star. Reason: ", result.Message) };
            }

            ValidateLightDNA(dnaFolder, genesisCSharpFolder, genesisRustFolder);

            switch (STARDNA.HolochainVersion.ToUpper())
            {
                case "REDUX":
                    rustDNAFolder = STARDNA.RustDNAReduxTemplateFolder;
                    break;

                case "RSM":
                    rustDNAFolder = STARDNA.RustDNARSMTemplateFolder;
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

            string iHolonTemplate = new FileInfo(string.Concat(STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateIHolonDNA)).OpenText().ReadToEnd();
            string holonTemplateCsharp = new FileInfo(string.Concat(STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateHolonDNA)).OpenText().ReadToEnd();
            string zomeTemplateCsharp = new FileInfo(string.Concat(STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateZomeDNA)).OpenText().ReadToEnd();
            string iStarTemplateCsharp = new FileInfo(string.Concat(STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateIStarDNA)).OpenText().ReadToEnd();
            string starTemplateCsharp = new FileInfo(string.Concat(STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateStarDNA)).OpenText().ReadToEnd();
            string iPlanetTemplateCsharp = new FileInfo(string.Concat(STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateIPlanetDNA)).OpenText().ReadToEnd();
            string planetTemplateCsharp = new FileInfo(string.Concat(STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplatePlanetDNA)).OpenText().ReadToEnd();
            string iMoonTemplateCsharp = new FileInfo(string.Concat(STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateIMoonDNA)).OpenText().ReadToEnd();
            string moonTemplateCsharp = new FileInfo(string.Concat(STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateMoonDNA)).OpenText().ReadToEnd();
            string TemplateCsharp = new FileInfo(string.Concat(STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplatePlanetDNA)).OpenText().ReadToEnd();

            string iCelestialBodyTemplateCsharp = new FileInfo(string.Concat(STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateICelestialBodyDNA)).OpenText().ReadToEnd();
            string celestialBodyTemplateCsharp = new FileInfo(string.Concat(STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateCelestialBodyDNA)).OpenText().ReadToEnd();
            string iZomeTemplate = new FileInfo(string.Concat(STARDNA.CSharpDNATemplateFolder, "\\", STARDNA.CSharpTemplateIZomeDNA)).OpenText().ReadToEnd();

            //If folder is not passed in via command line args then use default in config file.
            if (string.IsNullOrEmpty(dnaFolder))
                dnaFolder = STARDNA.CelestialBodyDNA;

            if (string.IsNullOrEmpty(genesisCSharpFolder))
                genesisCSharpFolder = STARDNA.GenesisCSharpFolder;

            if (string.IsNullOrEmpty(genesisRustFolder))
                genesisRustFolder = STARDNA.GenesisRustFolder;

            if (string.IsNullOrEmpty(genesisNameSpace))
                genesisNameSpace = STARDNA.GenesisNamespace;

            DirectoryInfo dirInfo = new DirectoryInfo(dnaFolder);
            FileInfo[] files = dirInfo.GetFiles();

            switch (type)
            {
                case GenesisType.Moon:
                    {
                        newBody = new Moon();
                        newBody.ParentHolon = celestialBodyParent;
                        newBody.ParentHolonId = celestialBodyParent.Id;
                        newBody.ParentPlanet = (IPlanet)celestialBodyParent;
                        newBody.ParentPlanetId = celestialBodyParent.ParentPlanetId;
                        newBody.ParentStar = celestialBodyParent.ParentStar;
                        newBody.ParentStarId = celestialBodyParent.ParentStarId;
                    }
                    break;

                case GenesisType.Planet:
                    {
                        newBody = new Planet();

                        //If no parent Star is passed in then set the parent star to SuperStar.
                        if (celestialBodyParent == null)
                            celestialBodyParent = InnerStar;

                        newBody.ParentHolon = celestialBodyParent;
                        newBody.ParentHolonId = celestialBodyParent.Id;
                        newBody.ParentStar = (IStar)celestialBodyParent;
                        newBody.ParentStarId = celestialBodyParent.Id;
                    }
                break;

                case GenesisType.Star:
                    {
                        newBody = new CelestialBodies.Star();
                        newBody.ParentHolon = InnerStar;
                        newBody.ParentHolonId = InnerStar.Id;
                        newBody.ParentStar = InnerStar;
                        newBody.ParentStarId = InnerStar.Id;
                    }
                break;
            }

           // newBody.CelestialBody = newBody; //TODO: Causes an infinite recursion because CelestialBody is a Holon itself so its linking to itself.
            newBody.Name = name;
            newBody.OnZomeError += NewBody_OnZomeError;
            await newBody.InitializeAsync();
            OASISResult<ICelestialBody> newBodyResult = await newBody.SaveAsync(); //Need to save to get the id to be used for ParentId below (zomes, holons & nodes).

            if (newBodyResult.IsError)
                return new CoronalEjection() { ErrorOccured = true, Message = string.Concat("Error Saving New CelestialBody. Reason: ", newBodyResult.Message) };
            else
                newBody = (CelestialBody)newBodyResult.Result;

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
                        await ((PlanetCore)celestialBodyParent.CelestialBodyCore).AddMoonAsync((IMoon)newBody);
                        return new CoronalEjection() { ErrorOccured = false, Message = "Moon Successfully Created.", CelestialBody = newBody };
                    }

                case GenesisType.Planet:
                    {
                        // If a star is not passed in, then add the planet to the main star.
                        if (celestialBodyParent == null)
                            celestialBodyParent = InnerStar;

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
                        await ((ISuperStarCore)celestialBodyParent.CelestialBodyCore).AddStarAsync((IStar)newBody);
                        return new CoronalEjection() { ErrorOccured = false, Message = "Star Successfully Created.", CelestialBody = newBody };
                    }

                case GenesisType.SoloarSystem:
                    {
                        await ((ISuperStarCore)celestialBodyParent.CelestialBodyCore).AddSolarSystemAsync(new SolarSystem() { Star = (IStar)newBody });
                        return new CoronalEjection() { ErrorOccured = false, Message = "Star/SolarSystem Successfully Created.", CelestialBody = newBody };
                    }

                case GenesisType.Galaxy:
                    {
                        await ((IGrandSuperStarCore)celestialBodyParent.CelestialBodyCore).AddGalaxyAsync(new Galaxy() { SuperStar = (ISuperStar)newBody });
                        return new CoronalEjection() { ErrorOccured = false, Message = "SuperStar/Galaxy Successfully Created.", CelestialBody = newBody };
                    }

                case GenesisType.Universe:
                    {
                        await ((IGreatGrandSuperStarCore)celestialBodyParent.CelestialBodyCore).AddUniverseAsync(new Universe() { GrandSuperStar = (IGrandSuperStar)newBody });
                        return new CoronalEjection() { ErrorOccured = false, Message = "GrandSuperStar/Universe Successfully Created.", CelestialBody = newBody };
                    }

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
                ValidateFolder(starDNA.CelestialBodyDNA, "starDNA.CelestialBodyDNA", true);
                ValidateFolder(starDNA.GenesisCSharpFolder, "starDNA.GenesisCSharpFolder", false, true);
                ValidateFolder(starDNA.GenesisRustFolder, "starDNA.GenesisRustFolder", false, true);
                ValidateFolder(starDNA.CSharpDNATemplateFolder, "starDNA.CSharpDNATemplateFolder");
                ValidateFile(starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateHolonDNA, "starDNA.CSharpTemplateHolonDNA");
                ValidateFile(starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateZomeDNA, "starDNA.CSharpTemplateZomeDNA");
                ValidateFile(starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateIStarDNA, "starDNA.CSharpTemplateIStarDNA");
                ValidateFile(starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateStarDNA, "starDNA.CSharpTemplateStarDNA");
                ValidateFile(starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateIPlanetDNA, "starDNA.CSharpTemplateIPlanetDNA");
                ValidateFile(starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateIPlanetDNA, "starDNA.CSharpTemplatePlanetDNA");
                ValidateFile(starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateIMoonDNA, "starDNA.CSharpTemplateIMoonDNA");
                ValidateFile(starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateIMoonDNA, "starDNA.CSharpTemplateMoonDNA");
                ValidateFile(starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateCelestialBodyDNA, "starDNA.CSharpTemplateCelestialBodyDNA");

                switch (starDNA.HolochainVersion.ToUpper())
                {
                    case "REDUX":
                        {
                            ValidateFolder(starDNA.RustDNAReduxTemplateFolder, "starDNA.RustDNAReduxTemplateFolder");
                            ValidateFile(starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateCreate, "starDNA.RustTemplateCreate");
                            ValidateFile(starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateDelete, "starDNA.RustTemplateDelete");
                            ValidateFile(starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateLib, "starDNA.RustTemplateLib");
                            ValidateFile(starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateRead, "starDNA.RustTemplateRead");
                            ValidateFile(starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateUpdate, "starDNA.RustTemplateUpdate");
                            ValidateFile(starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateList, "starDNA.RustTemplateList");
                            ValidateFile(starDNA.RustDNAReduxTemplateFolder, starDNA.RustTemplateValidation, "starDNA.RustTemplateValidation");
                        }
                        break;

                    case "RSM":
                        {
                            ValidateFolder(starDNA.RustDNARSMTemplateFolder, "starDNA.RustDNARSMTemplateFolder");
                            ValidateFile(starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateCreate, "starDNA.RustTemplateCreate");
                            ValidateFile(starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateDelete, "starDNA.RustTemplateDelete");
                            ValidateFile(starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateLib, "starDNA.RustTemplateLib");
                            ValidateFile(starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateRead, "starDNA.RustTemplateRead");
                            ValidateFile(starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateUpdate, "starDNA.RustTemplateUpdate");
                            ValidateFile(starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateList, "starDNA.RustTemplateList");
                            ValidateFile(starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateValidation, "starDNA.RustTemplateValidation");
                        }
                        break;
                }
            }
            else
                throw new ArgumentNullException("starDNA is null, please check and try again.");
        }

        private static void ValidateLightDNA(string dnaFolder, string genesisCSharpFolder, string genesisRustFolder)
        {
            ValidateFolder(dnaFolder, "dnaFolder");
            ValidateFolder(genesisCSharpFolder, "genesisCSharpFolder", false, true);
            ValidateFolder(genesisRustFolder, "genesisRustFolder", false, true);
        }

        private static void ValidateFolder(string folder, string folderParam, bool checkIfContainsFiles = false, bool createIfDoesNotExist = false)
        {
            if (string.IsNullOrEmpty(folder))
                throw new ArgumentNullException(folderParam, string.Concat("The ", folderParam, " param in the StarDNA is null, please double check and try again."));

            if (checkIfContainsFiles && Directory.GetFiles(folder).Length == 0)
                throw new InvalidOperationException(string.Concat("The ", folderParam, " folder in the StarDNA is empty."));

            if (!Directory.Exists(folder))
            {
                if (createIfDoesNotExist)
                    Directory.CreateDirectory(folder);
                else
                    throw new InvalidOperationException(string.Concat("The ", folderParam, " was not found, please double check and try again."));
            }
        }

        private static void ValidateFile(string folder, string file, string fileParam)
        {
            if (string.IsNullOrEmpty(file))
                throw new ArgumentNullException(fileParam, string.Concat("The ", fileParam, " param in the StarDNA is null, please double check and try again."));

            if (!File.Exists(string.Concat(folder, "\\", file)))
                throw new FileNotFoundException(string.Concat("The ", fileParam, " file is not valid, please double check and try again."), string.Concat(folder, "\\", file));
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
            Star.OASISDNAPath = OASISDNAPath;

            if (!OASISAPI.IsOASISBooted)
                return OASISAPI.BootOASIS(Star.OASISDNAPath);
            else
                return new OASISResult<bool>() { Message = "OASIS Already Booted" };
        }

        /*
        private static OASISResult<ICelestialBody> IgniteCOSMIC()
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
            InnerStar = new GreatGrandSuperStar(_starId);
            InnerStar.Initialize();

            CreateNewInnerStar(ref result);

            if (!result.IsError && InnerStar.Id == Guid.Empty)
            {
                //result = InnerStar.Save(); //TODO: Implement non-async version...
                result = InnerStar.SaveAsync().Result;
                PostIgniteInnerStar(result);
                // result = PostIgniteInnerStar(Task.Run(IgniteInnerStarAsync).GetAwaiter().GetResult());
            }

            return result;
        }*/

        
        private static OASISResult<ICelestialBody> IgniteInnerStar()
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
            //InnerStar = new GreatGrandSuperStar(_starId); //Only one of these exists (at the centre of the Omiverse).
            InnerStar = new GreatGrandSuperStar(); //Only one of these exists (at the centre of the Omiverse).
            InnerStar.Initialize();

            CreateNewInnerStar(ref result);

            if (!result.IsError && InnerStar.Id == Guid.Empty)
            {
                //result = InnerStar.Save(); //TODO: Implement non-async version...
                result = InnerStar.SaveAsync().Result;
                PostIgniteInnerStar(result);
               // result = PostIgniteInnerStar(Task.Run(IgniteInnerStarAsync).GetAwaiter().GetResult());
            }

            return result;
        }


        private static async Task<OASISResult<ICelestialBody>> IgniteInnerStarAsync()
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
            //InnerStar = new GreatGrandSuperStar(_starId); //Only one of these exists (at the centre of the Omiverse).
            InnerStar = new GreatGrandSuperStar(); //Only one of these exists (at the centre of the Omiverse).
            await InnerStar.InitializeAsync();
            WireUpEvents();

            CreateNewInnerStar(ref result);

            if (!result.IsError && InnerStar.Id == Guid.Empty)
            {
                result = await InnerStar.SaveAsync();
                await PostIgniteInnerStarAsync(result);
            }

            return result;
        }

        private static void CreateNewInnerStar(ref OASISResult<ICelestialBody> result)
        {
            if (InnerStar.Id == Guid.Empty)
            {
                // TODO: May possibly have one SuperStar per Provider Type? Or list of ProviderTypes? People can host whichever provider(s) they wish as a ONODE. Each ONODE will be a GrandSuperStar (Universe), which can choose which Glaxies/Provider Types to host. Therefore the entire ONET (OASIS Network) is the distributed de-centralised network of GrandSuperStars/Universes forming the OASIS meta-verse/magicverse/Omiverse. :)
                InnerStar.Name = "GreatGrandSuperStar";
                InnerStar.Description = "GreatGrandSuperStar at the centre of the Omiverse (The OASIS). Can create Multiverses, Universes, Galaxies, SolarSystems, Stars, Planets (Super OAPPS) and moons (OAPPS)";
                InnerStar.HolonType = HolonType.GreatGrandSuperStar;
            }
            else
                result.Message = "STAR Ignited";
        }

        //private static OASISResult<ICelestialBody> PostIgniteInnerStar(OASISResult<ICelestialBody> result)
        //{
        //    if (!result.IsError && result.IsSaved)
        //    {
        //        result.Message = "STAR Ignited";
        //        STARDNA.StarId = InnerStar.Id.ToString();
        //        SaveDNA();

        //        OASISResult<IOmiverse> omiverseResult = ((GreatGrandSuperStarCore)InnerStar.CelestialBodyCore).AddOmiverse(new Omiverse() { GreatGrandSuperStar = InnerStar });

        //        if (!omiverseResult.IsError && omiverseResult.Result != null)
        //        {
        //            InnerStar.ParentOmiverse = omiverseResult.Result;
        //            InnerStar.ParentOmiverseId = omiverseResult.Result.Id;
        //            InnerStar.Save();

        //            OASISResult<IOmiverse> omiverseResult = ((GreatGrandSuperStarCore)InnerStar.CelestialBodyCore).AddMultiverse(new Omiverse() { GreatGrandSuperStar = InnerStar });
        //        }



        //        //Then need to create a default Universe, Galaxy, SolarSystem, Star & Planet (Our World).
        //    }

        //    return result;
        //}

        private static async Task<OASISResult<ICelestialBody>> PostIgniteInnerStarAsync(OASISResult<ICelestialBody> result)
        {
            if (!result.IsError && result.IsSaved)
            {
                result.Message = "STAR Ignited";
                STARDNA.StarId = InnerStar.Id.ToString();
                SaveDNA();

                OASISResult<IOmiverse> omiverseResult = await ((GreatGrandSuperStarCore)InnerStar.CelestialBodyCore).AddOmiverseAsync(new Omiverse() { GreatGrandSuperStar = InnerStar });

                if (!omiverseResult.IsError && omiverseResult.Result != null)
                {
                    InnerStar.ParentOmiverse = omiverseResult.Result;
                    InnerStar.ParentOmiverseId = omiverseResult.Result.Id;
                    InnerStar.Save();

                    Multiverse multiverse = new Multiverse();
                    multiverse.ParentOmiverse = omiverseResult.Result;
                    multiverse.ParentOmiverseId = omiverseResult.Result.Id;
                    multiverse.ParentGreatGrandSuperStar = InnerStar;
                    multiverse.ParentGreatGrandSuperStarId = InnerStar.Id;

                    OASISResult<IMultiverse> multiverseResult = await ((GreatGrandSuperStarCore)InnerStar.CelestialBodyCore).AddMultiverseAsync(multiverse);

                    if (!multiverseResult.IsError && multiverseResult.Result != null)
                    {
                        multiverse = (Multiverse)multiverseResult.Result;

                        GalaxyCluster galaxyCluster = new GalaxyCluster();
                        galaxyCluster.ParentOmiverse = omiverseResult.Result;
                        galaxyCluster.ParentOmiverseId = omiverseResult.Result.Id;
                        galaxyCluster.ParentGreatGrandSuperStar = InnerStar;
                        galaxyCluster.ParentGreatGrandSuperStarId = InnerStar.Id;
                        galaxyCluster.ParentGrandSuperStar = multiverse.ParentGrandSuperStar;
                        galaxyCluster.ParentGrandSuperStarId = multiverse.ParentGrandSuperStarId;
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
                            galaxy.ParentOmiverse = omiverseResult.Result;
                            galaxy.ParentOmiverseId = omiverseResult.Result.Id;
                            galaxy.ParentGreatGrandSuperStar = InnerStar;
                            galaxy.ParentGreatGrandSuperStarId = InnerStar.Id;
                            galaxy.ParentGrandSuperStar = multiverse.ParentGrandSuperStar;
                            galaxy.ParentGrandSuperStarId = multiverse.ParentGrandSuperStarId;
                            galaxy.ParentMultiverse = multiverse;
                            galaxy.ParentMultiverseId = multiverse.Id;
                            galaxy.ParentDimension = multiverse.Dimensions.ThirdDimension;
                            galaxy.ParentDimensionId = multiverse.Dimensions.ThirdDimension.Id;
                            galaxy.ParentUniverseId = multiverse.Dimensions.ThirdDimension.MagicVerse.Id;
                            galaxy.ParentUniverse = multiverse.Dimensions.ThirdDimension.MagicVerse;
                            galaxy.ParentGalaxyCluster = galaxyCluster;
                            galaxy.ParentGalaxyClusterId = galaxyCluster.Id;

                            OASISResult<IGalaxy> galaxyResult = await ((GrandSuperStarCore)multiverse.GrandSuperStar.CelestialBodyCore).AddGalaxyToGalaxyClusterAsync( galaxy);

                            if (!galaxyClusterResult.IsError && galaxyClusterResult.Result != null)
                            {
                                galaxyCluster = (GalaxyCluster)galaxyClusterResult.Result;
                            }
                            else
                            {

                            }
                        }
                    }
                }



                //Then need to create a default Universe, Galaxy, SolarSystem, Star & Planet (Our World).
            }

            return result;
        }

        private static void HandleErrorMessage<T>(ref OASISResult<T> result, string errorMessage)
        {
            OnStarError?.Invoke(null, new StarErrorEventArgs() { Reason = errorMessage });
            ErrorHandling.HandleError(ref result, errorMessage);
        }
    }
}
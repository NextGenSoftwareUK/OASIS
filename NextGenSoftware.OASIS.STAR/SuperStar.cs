using Newtonsoft.Json;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Config;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.OASISAPIManager;
using NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.STAR
{
    //TODO: Inherit from IStar
    //public static class Star : IStar
    public static class SuperStar
    {
        const string STAR_DNA = "DNA\\STAR_DNA.json";
        const string OASIS_DNA = "DNA\\OASIS_DNA.json";
        public static Star InnerStar { get; set; }
        public static SuperStarCore SuperStarCore { get; set; }
        public static List<Star> Stars { get; set; }
        public static List<Planet> Planets { get; set; }
        public static Avatar LoggedInUser { get; set; }

        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        public static event HolonsLoaded OnHolonsLoaded;

        public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        public static event ZomesLoaded OnZomesLoaded;

        public delegate void HolonSaved(object sender, HolonSavedEventArgs e);
        public static event HolonSaved OnHolonSaved;

        public delegate void HolonLoaded(object sender, HolonLoadedEventArgs e);
        public static event HolonLoaded OnHolonLoaded;

        public delegate void Initialized(object sender, EventArgs e);
        public static event Initialized OnInitialized;

        public delegate void ZomeError(object sender, ZomeErrorEventArgs e);
        public static event ZomeError OnZomeError;

        public delegate void StarError(object sender, StarErrorEventArgs e);
        public static event StarError OnStarError;

        //TODO: Not sure if we want to expose the HoloNETClient events at this level? They can subscribe to them through the HoloNETClient property below...
        public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        public static event Disconnected OnDisconnected;

        public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        public static event DataReceived OnDataReceived;

        // public static List<IPlanet> Planets { get; set; }

        public static OASISAPIManager OASISAPI = new OASISAPIManager(new List<IOASISProvider>() { new SEEDSOASIS() });

        // Possible to override settings in DNA file if this method is manually called...
        //public static void Initialize(string holochainConductorURI, HoloNETClientType type, string providerKey)
        public static void Initialize(string providerKey)
        {
            //By default the OASISProviderManager will load the settings from appsettings.json but you can override using below:
           OASISProviderManager.OASISDNAFileName = OASIS_DNA;

            // Will initialize the default OASIS Provider defined OASIS_DNA config file.
            OASISProviderManager.GetAndActivateProvider();

            SuperStarCore = new SuperStarCore(providerKey);
            InnerStar = new Star(providerKey);

            SuperStarCore.OnHolonLoaded += SuperStarCore_OnHolonLoaded;
            SuperStarCore.OnHolonSaved += SuperStarCore_OnHolonSaved;
            SuperStarCore.OnHolonsLoaded += SuperStarCore_OnHolonsLoaded;
            SuperStarCore.OnZomeError += SuperStarCore_OnZomeError;
            SuperStarCore.OnInitialized += SuperStarCore_OnInitialized;

            //StarBody.Initialize(holochainConductorURI, type);
            //StarBody.Initialize(holochainConductorURI, type);
            //StarBody.HoloNETClient.OnError += HoloNETClient_OnError;
        }

        //private static void HoloNETClient_OnError(object sender, HoloNETErrorEventArgs e)
        //{
        //    //ALL HoloNET errors should now come through this handler (because this client is passed to everything else).
        //    OnStarError?.Invoke(sender, new StarErrorEventArgs() { EndPoint = StarBody.HoloNETClient.EndPoint, Reason = e.Reason, ErrorDetails = e.ErrorDetails, HoloNETErrorDetails = e });
        //}

        private static void SuperStarCore_OnInitialized(object sender, EventArgs e)
        {
            OnInitialized?.Invoke(sender, e);
        }

        private static void SuperStarCore_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private static void SuperStarCore_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            OnHolonLoaded?.Invoke(sender, e);
        }

        private static void SuperStarCore_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);
        }

        private static void SuperStarCore_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            OnHolonsLoaded?.Invoke(sender, e);
        }

        //Log in
        public static async Task<Avatar> BeamIn(string username, string password)
        {
            //TODO: Implement login code here.
            LoggedInUser = new Avatar() { Karma = 777, Name = "David Ellams", HolonType = HolonType.Avatar };
            return LoggedInUser;
        }

        public static async Task<CoronalEjection> Light(GenesisType type, string name, string dnaFolder = "", string genesisCSharpFolder = "", string genesisRustFolder = "", string genesisNameSpace = "")
        {
            return await Light(type, name, (ICelestialBody)null, dnaFolder, genesisCSharpFolder, genesisRustFolder, genesisNameSpace);
            //return await Light(type, name, new Planet("", HoloNETClientType.Desktop, ""), dnaFolder, genesisCSharpFolder, genesisRustFolder, genesisNameSpace);
        }


        public static async Task<CoronalEjection> Light(GenesisType type, string name, IStar starToAddPlanetTo = null, string dnaFolder = "", string genesisCSharpFolder = "", string genesisRustFolder = "", string genesisNameSpace = "")
        {
            return await Light(type, name, (ICelestialBody)starToAddPlanetTo, dnaFolder, genesisCSharpFolder, genesisRustFolder, genesisNameSpace);
        }

        public static async Task<CoronalEjection> Light(GenesisType type, string name, IPlanet planetToAddMoonTo = null, string dnaFolder = "", string genesisCSharpFolder = "", string genesisRustFolder = "", string genesisNameSpace = "")
        {
            return await Light(type, name, (ICelestialBody)planetToAddMoonTo, dnaFolder, genesisCSharpFolder, genesisRustFolder, genesisNameSpace);
        }

        private static async Task<CoronalEjection> Light(GenesisType type, string name, ICelestialBody celestialBodyParent = null, string dnaFolder = "", string genesisCSharpFolder = "", string genesisRustFolder = "", string genesisNameSpace = "")
        {
            StarDNA starDNA;
            //OASIS.API.Core.ICelestialBody newBody = null;
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
            string planetBufferCsharp = "";
            string celestialBodyBufferCsharp = "";
            bool firstHolon = true;

            if (LoggedInUser == null)
                return new CoronalEjection() { ErrorOccured = true, Message = "Avatar is not logged in. Please log in before calling this command." };

            if (LoggedInUser.Level < 33 && type == GenesisType.Planet)
                return new CoronalEjection() { ErrorOccured = true, Message = "Avatar must have reached level 33 before they can create planets. Please create a moon instead..." };

            if (celestialBodyParent == null && type == GenesisType.Moon)
                return new CoronalEjection() { ErrorOccured = true, Message = "You must specify the planet to add the moon to." };

            if (File.Exists(STAR_DNA))
                starDNA = LoadDNA();
            else
            {
                starDNA = new StarDNA();
                SaveDNA(starDNA);
            }

            ValidateDNA(starDNA, dnaFolder, genesisCSharpFolder, genesisRustFolder);

            //if (StarCore == null)
            if (SuperStarCore == null)
                Initialize(starDNA.StarProviderKey);
                //Initialize(starDNA.HolochainConductorURI, (HoloNETClientType)Enum.Parse(typeof(HoloNETClientType), starDNA.HoloNETClientType), starDNA.StarProviderKey);

            string rustDNAFolder = string.Empty;

            switch (starDNA.HolochainVersion.ToUpper())
            {
                case "REDUX":
                    rustDNAFolder = starDNA.RustDNAReduxTemplateFolder;
                    break;

                case "RSM":
                    rustDNAFolder = starDNA.RustDNARSMTemplateFolder;
                    break;
            }

            string libTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", starDNA.RustTemplateLib)).OpenText().ReadToEnd();
            string createTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", starDNA.RustTemplateCreate)).OpenText().ReadToEnd();
            string readTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", starDNA.RustTemplateRead)).OpenText().ReadToEnd();
            string updateTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", starDNA.RustTemplateUpdate)).OpenText().ReadToEnd();
            string deleteTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", starDNA.RustTemplateDelete)).OpenText().ReadToEnd();
            string listTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", starDNA.RustTemplateList)).OpenText().ReadToEnd();
            string validationTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", starDNA.RustTemplateValidation)).OpenText().ReadToEnd();
            string holonTemplateRust = new FileInfo(string.Concat(rustDNAFolder, "\\", starDNA.RustTemplateHolon)).OpenText().ReadToEnd();
            string intTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", starDNA.RustTemplateInt)).OpenText().ReadToEnd();
            string stringTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", starDNA.RustTemplateString)).OpenText().ReadToEnd();
            string boolTemplate = new FileInfo(string.Concat(rustDNAFolder, "\\", starDNA.RustTemplateBool)).OpenText().ReadToEnd();

            string iHolonTemplate = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIHolonDNA)).OpenText().ReadToEnd();
            string holonTemplateCsharp = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateHolonDNA)).OpenText().ReadToEnd();
            string zomeTemplateCsharp = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateZomeDNA)).OpenText().ReadToEnd();
            string iStarTemplateCsharp = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIStarDNA)).OpenText().ReadToEnd();
            string starTemplateCsharp = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateStarDNA)).OpenText().ReadToEnd();
            string iPlanetTemplateCsharp = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIPlanetDNA)).OpenText().ReadToEnd();
            string planetTemplateCsharp = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplatePlanetDNA)).OpenText().ReadToEnd();
            string iMoonTemplateCsharp = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIMoonDNA)).OpenText().ReadToEnd();
            string moonTemplateCsharp = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateMoonDNA)).OpenText().ReadToEnd();
            string TemplateCsharp = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplatePlanetDNA)).OpenText().ReadToEnd();

            string iCelestialBodyTemplateCsharp = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateICelestialBodyDNA)).OpenText().ReadToEnd();
            string celestialBodyTemplateCsharp = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateCelestialBodyDNA)).OpenText().ReadToEnd();
            string iZomeTemplate = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIZomeDNA)).OpenText().ReadToEnd();

            //If folder is not passed in via command line args then use default in config file.
            if (string.IsNullOrEmpty(dnaFolder))
                dnaFolder = starDNA.CelestialBodyDNA;

            if (string.IsNullOrEmpty(genesisCSharpFolder))
                genesisCSharpFolder = starDNA.GenesisCSharpFolder;

            if (string.IsNullOrEmpty(genesisRustFolder))
                genesisRustFolder = starDNA.GenesisRustFolder;

            if (string.IsNullOrEmpty(genesisNameSpace))
                genesisNameSpace = starDNA.GenesisNamespace;

            DirectoryInfo dirInfo = new DirectoryInfo(dnaFolder);
            FileInfo[] files = dirInfo.GetFiles();

            switch (type)
            {
                case GenesisType.Moon:
                    //newBody = new Moon(StarBody.HoloNETClient);
                    newBody = new Moon();
                    break;

                case GenesisType.Planet:
                    newBody = new Planet();
                    //newBody = new Planet(StarBody.HoloNETClient);
                    break;

                case GenesisType.Star:
                    //newBody = new StarBody(StarBody.HoloNETClient);
                    newBody = new Star();
                    break;
            }

            newBody.Id = Guid.NewGuid();
            newBody.Name = name;
            newBody.OnZomeError += NewBody_OnZomeError;
            //await newBody.Initialize(StarBody.HoloNETClient);
            await newBody.Initialize();

            /*
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

                            zomeBufferCsharp = zomeTemplateCsharp.Replace(starDNA.TemplateNamespace, genesisNameSpace);
                            holonBufferCsharp = holonTemplateCsharp.Replace(starDNA.TemplateNamespace, genesisNameSpace);
                        }

                        if (buffer.Contains("ZomeDNA"))
                        {
                            string[] parts = buffer.Split(' ');
                            libBuffer = libTemplate.Replace("zome_name", parts[6].ToSnakeCase());

                            zomeBufferCsharp = zomeBufferCsharp.Replace("ZomeDNATemplate", parts[6].ToPascalCase());
                            zomeBufferCsharp = zomeBufferCsharp.Replace("{zome}", parts[6].ToSnakeCase());
                            zomeName = parts[6].ToPascalCase();

                            //Zome newZome = new Zome(StarBody.HoloNETClient, zomeName);
                            Zome newZome = new Zome() { Name = zomeName };
                            newBody.CelestialBodyCore.Zomes.Add(newZome);

                            //TODO: Not sure await this? 
                            await newBody.CelestialBodyCore.AddZome(newZome);
                            //await newBody.SaveHolonAsync(zomeName, new Holon() { HolonType = HolonType.Zome, Id = Guid.NewGuid(), Name = zomeName });


                            newZome.Holons.Add(new Holon() { Name = "", HolonType = HolonType.Holon });
                            //newZome.SaveHolonAsync() //TODO: Finish this...
                        }

                        if (holonReached && buffer.Contains("string") || buffer.Contains("int") || buffer.Contains("bool"))
                        {
                            string[] parts = buffer.Split(' ');
                            string fieldName = string.Empty;

                            switch (parts[13].ToLower())
                            {
                                case "string":
                                    {
                                        //TODO: Get this working so one line for each type! :)
                                        ///GenerateDynamicZomeFunc()

                                        if (firstField)
                                            firstField = false;
                                        else
                                            holonFieldsClone = string.Concat(holonFieldsClone, "\t");

                                        fieldName = parts[14].ToSnakeCase();
                                        holonFieldsClone = string.Concat(holonFieldsClone, holonName, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);

                                        holonBufferRust = string.Concat(holonBufferRust, stringTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);
                                    }
                                    break;

                                case "int":
                                    {
                                        if (firstField)
                                            firstField = false;
                                        else
                                            holonFieldsClone = string.Concat(holonFieldsClone, "\t");

                                        fieldName = parts[14].ToSnakeCase();
                                        holonFieldsClone = string.Concat(holonFieldsClone, holonName, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
                                        holonBufferRust = string.Concat(holonBufferRust, intTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);
                                    }
                                    break;

                                case "bool":
                                    {
                                        if (firstField)
                                            firstField = false;
                                        else
                                            holonFieldsClone = string.Concat(holonFieldsClone, "\t");

                                        fieldName = parts[14].ToSnakeCase();
                                        holonFieldsClone = string.Concat(holonFieldsClone, holonName, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
                                        holonBufferRust = string.Concat(holonBufferRust, boolTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);
                                    }
                                    break;
                            }
                        }

                        // Write the holon out to the rust lib template. 
                        if (holonReached && buffer.Length > 1 && buffer.Substring(buffer.Length-1 ,1) == "}" && !buffer.Contains("get;"))
                        {
                            if (holonBufferRust.Length >2)
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

                            zomeBufferCsharp = zomeBufferCsharp.Replace(starDNA.TemplateNamespace, genesisNameSpace);
                            holonBufferCsharp = holonBufferCsharp.Replace(starDNA.TemplateNamespace, genesisNameSpace);
                            iholonBuffer = iholonBuffer.Replace(starDNA.TemplateNamespace, genesisNameSpace);

                            //if (string.IsNullOrEmpty(planetBufferCsharp))
                            //    planetBufferCsharp = planetTemplateCsharp;

                            //planetBufferCsharp = planetBufferCsharp.Replace(starDNA.TemplateNamespace, genesisNameSpace);
                            //planetBufferCsharp = planetBufferCsharp.Replace("{holon}", parts[10].ToSnakeCase()).Replace("HOLON", parts[10].ToPascalCase());

                            if (string.IsNullOrEmpty(celestialBodyBufferCsharp))
                                celestialBodyBufferCsharp = celestialBodyTemplateCsharp;

                            celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace(starDNA.TemplateNamespace, genesisNameSpace);
                            celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("CelestialBodyDNATemplate", name.ToPascalCase());
                            celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("{holon}", parts[10].ToSnakeCase()).Replace("HOLON", parts[10].ToPascalCase());
                            celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("CelestialBody", Enum.GetName(typeof(GenesisType), type)).Replace("ICelestialBody", string.Concat("I", Enum.GetName(typeof(GenesisType), type)));
                            celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("ICelestialBody", string.Concat("I", Enum.GetName(typeof(GenesisType), type)));
                            //celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("GenesisType.Star", string.Concat("GenesisType.", Enum.GetName(typeof(GenesisType), type)));
                            celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace(", GenesisType.Star", "");

                            
                            //switch (type)
                            //{
                            //    case GenesisType.Star:
                            //        celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("CelestialBody", "Star").Replace("ICelestialBody", "IStar");
                            //        break;

                            //    case GenesisType.Planet:
                            //        celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("CelestialBody", "Planet").Replace("ICelestialBody", "IPlanet");
                            //        break;

                            //    case GenesisType.Moon:
                            //        celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace("CelestialBody", "Moon").Replace("ICelestialBody", "IMoon");
                            //        break;
                            //}

                            holonName = holonName.ToSnakeCase();
                            holonReached = true;
                        }
                    }

                    reader.Close();
                    nextLineToWrite = 0;

                    File.WriteAllText(string.Concat(genesisRustFolder, "\\lib.rs"), libBuffer);
                    File.WriteAllText(string.Concat(genesisCSharpFolder, "\\", zomeName, ".cs"), zomeBufferCsharp);
                }
            }*/

            //TODO: MOVE ALL RUST CODE INTO HOLOOASIS.GENERATENATIVECODE METHOD.
            Zome currentZome = null;
            Holon currentHolon = null;

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

                            zomeBufferCsharp = zomeTemplateCsharp.Replace(starDNA.TemplateNamespace, genesisNameSpace);
                            holonBufferCsharp = holonTemplateCsharp.Replace(starDNA.TemplateNamespace, genesisNameSpace);
                        }

                        if (buffer.Contains("ZomeDNA"))
                        {
                            string[] parts = buffer.Split(' ');
                            libBuffer = libTemplate.Replace("zome_name", parts[6].ToSnakeCase());

                            zomeBufferCsharp = zomeBufferCsharp.Replace("ZomeDNATemplate", parts[6].ToPascalCase());
                            zomeBufferCsharp = zomeBufferCsharp.Replace("{zome}", parts[6].ToSnakeCase());
                            zomeName = parts[6].ToPascalCase();

                            currentZome = new Zome() { Name = zomeName };
                            //newBody.CelestialBodyCore.Zomes.Add(currentZome);

                            //TODO: Not sure await this? 
                            //await newBody.CelestialBodyCore.AddZome(currentZome); //TODO: May need to save this once holons and nodes/fields have been added?
                        }

                        if (holonReached && buffer.Contains("string") || buffer.Contains("int") || buffer.Contains("bool"))
                        {
                            string[] parts = buffer.Split(' ');
                            string fieldName = string.Empty;

                            switch (parts[13].ToLower())
                            {
                                case "string":
                                    {
                                        //TODO: Get this working so one line for each type! :)
                                        ///GenerateDynamicZomeFunc()

                                        if (firstField)
                                            firstField = false;
                                        else
                                            holonFieldsClone = string.Concat(holonFieldsClone, "\t");

                                        fieldName = parts[14].ToSnakeCase();
                                        holonFieldsClone = string.Concat(holonFieldsClone, holonName, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);

                                        holonBufferRust = string.Concat(holonBufferRust, stringTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);

                                        if (currentHolon.Nodes == null)
                                            currentHolon.Nodes = new List<INode>();

                                        currentHolon.Nodes.Add(new Node { NodeName = fieldName, NodeType = NodeType.String });
                                    }
                                    break;

                                case "int":
                                    {
                                        if (firstField)
                                            firstField = false;
                                        else
                                            holonFieldsClone = string.Concat(holonFieldsClone, "\t");

                                        fieldName = parts[14].ToSnakeCase();
                                        holonFieldsClone = string.Concat(holonFieldsClone, holonName, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
                                        holonBufferRust = string.Concat(holonBufferRust, intTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);

                                        if (currentHolon.Nodes == null)
                                            currentHolon.Nodes = new List<INode>();

                                        currentHolon.Nodes.Add(new Node { NodeName = fieldName, NodeType = NodeType.Int });
                                    }
                                    break;

                                case "bool":
                                    {
                                        if (firstField)
                                            firstField = false;
                                        else
                                            holonFieldsClone = string.Concat(holonFieldsClone, "\t");

                                        fieldName = parts[14].ToSnakeCase();
                                        holonFieldsClone = string.Concat(holonFieldsClone, holonName, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
                                        holonBufferRust = string.Concat(holonBufferRust, boolTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);

                                        if (currentHolon.Nodes == null)
                                            currentHolon.Nodes = new List<INode>();

                                        currentHolon.Nodes.Add(new Node { NodeName = fieldName, NodeType = NodeType.Bool });
                                    }
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

                            zomeBufferCsharp = zomeBufferCsharp.Replace(starDNA.TemplateNamespace, genesisNameSpace);
                            holonBufferCsharp = holonBufferCsharp.Replace(starDNA.TemplateNamespace, genesisNameSpace);
                            iholonBuffer = iholonBuffer.Replace(starDNA.TemplateNamespace, genesisNameSpace);

                            if (string.IsNullOrEmpty(celestialBodyBufferCsharp))
                                celestialBodyBufferCsharp = celestialBodyTemplateCsharp;

                            celestialBodyBufferCsharp = celestialBodyBufferCsharp.Replace(starDNA.TemplateNamespace, genesisNameSpace);
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

                            holonName = holonName.ToSnakeCase();
                            holonReached = true;

                            currentHolon = new Holon() { Name = holonName, HolonType = HolonType.Holon };
                            currentZome.Holons.Add(currentHolon); //TODO: May need to add this once all nodes/fields have been added to it?
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

            


            //TODO: Need to save the collection of Zomes/Holons that belong to this planet here...
           // await newBody.Save();

            //TODO: Might be more efficient if the planet can be saved and then added to the list of planets in the star in one go?
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

                    await ((StarCore)celestialBodyParent.CelestialBodyCore).AddPlanetAsync((IPlanet)newBody);
                    return new CoronalEjection() { ErrorOccured = false, Message = "Planet Successfully Created.", CelestialBody = newBody };
                }

                case GenesisType.Star:
                {
                    await SuperStarCore.AddStarAsync((IStar)newBody);
                    return new CoronalEjection() { ErrorOccured = false, Message = "Star Successfully Created.", CelestialBody = newBody };
                }
                default:
                    return new CoronalEjection() { ErrorOccured = true, Message = "Unknown Error Occured.", CelestialBody = newBody };
            }


            //Generate any native code for the current provider.
            //TODO: Add option to pass into STAR which providers to generate native code for (can be more than one provider).
            ((IOASISSuperStar)ProviderManager.CurrentStorageProvider).NativeCodeGenesis(newBody);

            //TODO: Need to save this to the StarNET store (still to be made!) (Will of course be written on top of the HDK/ODK...
            //This will be private on the store until the user publishes via the Star.Seed() command.
        }

        private static void NewBody_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            //OnZomeError?.Invoke(sender, new ZomeErrorEventArgs() { EndPoint = StarBody.HoloNETClient.EndPoint, Reason = e.Reason, ErrorDetails = e.ErrorDetails, HoloNETErrorDetails = e.HoloNETErrorDetails });
           // OnStarError?.Invoke(sender, new StarErrorEventArgs() { EndPoint = StarBody.HoloNETClient.EndPoint, Reason = e.Reason, ErrorDetails = e.ErrorDetails, HoloNETErrorDetails = e.HoloNETErrorDetails });
        }

        //TODO: Get this working... :)
        private static string GenerateDynamicZomeFunc(string funcName, string zomeTemplateCsharp, string holonName, string zomeBufferCsharp, int funcLength)
        {
            int funcHolonIndex = zomeTemplateCsharp.IndexOf(funcName);
            string funct = zomeTemplateCsharp.Substring(funcHolonIndex - 26, funcLength); //170
            funct = funct.Replace("{holon}", holonName.ToSnakeCase()).Replace("HOLON", holonName.ToPascalCase());
            zomeBufferCsharp = zomeBufferCsharp.Insert(zomeBufferCsharp.Length - 6, funct);
            return zomeBufferCsharp;
        }

        public static void Login(string username, string password)
        {
            //TODO: Login and load the users Avatar here...
            LoggedInUser = new Avatar();
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

        private static void ValidateDNA(StarDNA starDNA, string dnaFolder, string genesisCSharpFolder, string genesisRustFolder)
        {
            if (!string.IsNullOrEmpty(dnaFolder) && !Directory.Exists(dnaFolder))
                throw new ArgumentOutOfRangeException("dnaFolder", dnaFolder, "The folder is not valid, please double check and try again.");

            if (!string.IsNullOrEmpty(genesisCSharpFolder) && !Directory.Exists(genesisCSharpFolder))
                throw new ArgumentOutOfRangeException("genesisCSharpFolder", genesisCSharpFolder, "The folder is not valid, please double check and try again.");

            if (!string.IsNullOrEmpty(genesisRustFolder) && !Directory.Exists(genesisRustFolder))
                throw new ArgumentOutOfRangeException("genesisRustFolder", genesisRustFolder, "The folder is not valid, please double check and try again.");

            if (starDNA != null)
            {
                if (!Directory.Exists(starDNA.CelestialBodyDNA))
                    throw new ArgumentOutOfRangeException("CelestialBodyDNA", starDNA.CelestialBodyDNA, "The CelestialBodyDNA is not valid, please double check and try again.");

                if (!Directory.Exists(starDNA.GenesisCSharpFolder))
                    throw new ArgumentOutOfRangeException("GenesisCSharpFolder", starDNA.GenesisCSharpFolder, "The GenesisCSharpFolder is not valid, please double check and try again.");

                if (!Directory.Exists(starDNA.GenesisRustFolder))
                    throw new ArgumentOutOfRangeException("GenesisRustFolder", starDNA.GenesisCSharpFolder, "The GenesisRustFolder is not valid, please double check and try again.");

                switch (starDNA.HolochainVersion.ToUpper())
                {
                    case "REDUX":
                    {
                        if (!File.Exists(string.Concat(starDNA.RustDNAReduxTemplateFolder, "\\", starDNA.RustTemplateCreate)))
                            throw new ArgumentOutOfRangeException("RustTemplateCreate", string.Concat(starDNA.RustDNAReduxTemplateFolder, "\\", starDNA.RustTemplateCreate), "The RustTemplateCreate file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.RustDNAReduxTemplateFolder, "\\", starDNA.RustTemplateDelete)))
                            throw new ArgumentOutOfRangeException("RustTemplateDelete", string.Concat(starDNA.RustDNAReduxTemplateFolder, "\\", starDNA.RustTemplateDelete), "The RustTemplateDelete file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.RustDNAReduxTemplateFolder, "\\", starDNA.RustTemplateLib)))
                            throw new ArgumentOutOfRangeException("RustTemplateLib", string.Concat(starDNA.RustDNAReduxTemplateFolder, "\\", starDNA.RustTemplateLib), "The RustTemplateLib file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.RustDNAReduxTemplateFolder, "\\", starDNA.RustTemplateRead)))
                            throw new ArgumentOutOfRangeException("RustTemplateRead", string.Concat(starDNA.RustDNAReduxTemplateFolder, "\\", starDNA.RustTemplateRead), "The RustTemplateRead file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.RustDNAReduxTemplateFolder, "\\", starDNA.RustTemplateUpdate)))
                            throw new ArgumentOutOfRangeException("RustTemplateUpdate", string.Concat(starDNA.RustDNAReduxTemplateFolder, "\\", starDNA.RustTemplateUpdate), "The RustTemplateUpdate file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.RustDNAReduxTemplateFolder, "\\", starDNA.RustTemplateList)))
                            throw new ArgumentOutOfRangeException("RustTemplateList", string.Concat(starDNA.RustDNAReduxTemplateFolder, "\\", starDNA.RustTemplateList), "The RustTemplateList file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.RustDNAReduxTemplateFolder, "\\", starDNA.RustTemplateValidation)))
                            throw new ArgumentOutOfRangeException("RustTemplateValidation", string.Concat(starDNA.RustTemplateValidation, "\\", starDNA.RustTemplateList), "The RustTemplateValidation file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateHolonDNA)))
                            throw new ArgumentOutOfRangeException("CSharpTemplateHolonDNA", string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateHolonDNA), "The CSharpTemplateMyholon file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateZomeDNA)))
                            throw new ArgumentOutOfRangeException("CSharpTemplateZomeDNA", string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateZomeDNA), "The CSharpTemplateMyZome file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIStarDNA)))
                            throw new ArgumentOutOfRangeException("CSharpTemplateIStarDNA", string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIStarDNA), "The CSharpTemplateIStarDNA file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateStarDNA)))
                            throw new ArgumentOutOfRangeException("CSharpTemplateStarDNA", string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateStarDNA), "The CSharpTemplateStarDNA file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIPlanetDNA)))
                            throw new ArgumentOutOfRangeException("CSharpTemplateIPlanetDNA", string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIPlanetDNA), "The CSharpTemplateIPlanetDNA file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplatePlanetDNA)))
                            throw new ArgumentOutOfRangeException("CSharpTemplatePlanetDNA", string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplatePlanetDNA), "The CSharpTemplatePlanetDNA file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIMoonDNA)))
                            throw new ArgumentOutOfRangeException("CSharpTemplateIMoonDNA", string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIMoonDNA), "The CSharpTemplateIMoonDNA file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplatePlanetDNA)))
                            throw new ArgumentOutOfRangeException("CSharpTemplateMoonDNA", string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateMoonDNA), "The CSharpTemplateMoonDNA file is not valid, please double check and try again.");

                        if (!File.Exists(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateCelestialBodyDNA)))
                            throw new ArgumentOutOfRangeException("CSharpTemplateCelestialBodyDNA", string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateCelestialBodyDNA), "The CSharpTemplatePlanetDNA file is not valid, please double check and try again.");
                        }
                        break;

                    case "RSM":
                    {
                        //TODO: RSM support coming soon... Currently blocked on HoloNET RSM upgrade because have not received support from hc community as yet to get RSM up and running on Windows...
                    }break;
                }

                
                //TODO: Add missing properties...
            }
        }

        //private void ProcessField(string fieldNameRaw, out string holonFieldsClone, out string holonBuffer, string template, string holonName)
        //{
        //    string fieldName = template.Replace("variableName", fieldNameRaw.ToSnakeCase());
        //    holonFieldsClone = string.Concat(holonFieldsClone, holonName, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
        //    holonBuffer = string.Concat(holonBuffer, fieldName, ",", Environment.NewLine);
        //}

        private static StarDNA LoadDNA()
        {
            using (StreamReader r = new StreamReader(STAR_DNA))
            {
                string json = r.ReadToEnd();
                StarDNA starDNA = JsonConvert.DeserializeObject<StarDNA> (json);
                return starDNA;
            }
        }

        private static bool SaveDNA(StarDNA starDNA)
        {
            string json = JsonConvert.SerializeObject(starDNA);
            StreamWriter writer = new StreamWriter(STAR_DNA);
            writer.Write(json);
            writer.Close();
            
            return true;
        }
    }
}

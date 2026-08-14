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
using System.Text.RegularExpressions;


namespace NextGenSoftware.OASIS.STAR
{
    public static partial class STAR
    {
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
                    celestialBodyDNAFolder = Path.Combine(STARDNA.STARBasePath, STARDNA.OAPPMetaDataDNAFolder, OAPPName, "CelestialBodyDNA");
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

            string dnaCSharpRoot = string.IsNullOrEmpty(STARDNA.STARBasePath)
                ? (STARDNA.CSharpDNATemplateFolder ?? string.Empty)
                : Path.Combine(STARDNA.STARBasePath, STARDNA.CSharpDNATemplateFolder ?? string.Empty);
            string iHolonTemplate = File.ReadAllText(Path.Combine(dnaCSharpRoot, STARDNA.CSharpTemplateIHolonDNA));
            string holonTemplateCsharp = File.ReadAllText(Path.Combine(dnaCSharpRoot, STARDNA.CSharpTemplateHolonDNA));
            string iZomeTemplate = File.ReadAllText(Path.Combine(dnaCSharpRoot, STARDNA.CSharpTemplateIZomeDNA));
            string zomeTemplateCsharp = File.ReadAllText(Path.Combine(dnaCSharpRoot, STARDNA.CSharpTemplateZomeDNA));
            string iCelestialBodyTemplateCsharp = File.ReadAllText(Path.Combine(dnaCSharpRoot, STARDNA.CSharpTemplateICelestialBodyDNA));
            string celestialBodyTemplateCsharp = File.ReadAllText(Path.Combine(dnaCSharpRoot, STARDNA.CSharpTemplateCelestialBodyDNA));
            string loadHolonTemplateCsharp = File.ReadAllText(Path.Combine(dnaCSharpRoot, STARDNA.CSharpTemplateLoadHolonDNA));
            string saveHolonTemplateCsharp = File.ReadAllText(Path.Combine(dnaCSharpRoot, STARDNA.CSharpTemplateSaveHolonDNA));
            string iloadHolonTemplateCsharp = File.ReadAllText(Path.Combine(dnaCSharpRoot, STARDNA.CSharpTemplateILoadHolonDNA));
            string isaveHolonTemplateCsharp = File.ReadAllText(Path.Combine(dnaCSharpRoot, STARDNA.CSharpTemplateISaveHolonDNA));

            string IntTemplateCsharp = File.ReadAllText(Path.Combine(dnaCSharpRoot, STARDNA.CSharpTemplateInt));
            string StringTemplateCSharp = File.ReadAllText(Path.Combine(dnaCSharpRoot, STARDNA.CSharpTemplateString));
            string BoolTemplateCsharp = File.ReadAllText(Path.Combine(dnaCSharpRoot, STARDNA.CSharpTemplateBool));

            
            if (string.IsNullOrEmpty(genesisFolder))
                genesisFolder = Path.Combine(STARDNA.STARNETBasePath ?? string.Empty, STARDNA.DefaultOAPPsSourcePath ?? string.Empty);
                //genesisFolder = $"{STARDNA.STARBasePath}\\{STARDNA.GenesisFolder}";

            if (string.IsNullOrEmpty(genesisNameSpace))
                genesisNameSpace = $"{STARDNA.DefaultGenesisNamespace}";
                //genesisNameSpace = $"{STARDNA.STARBasePath}\\{STARDNA.DefaultGenesisNamespace}";

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

            genesisFolder = Path.Combine(OAPPFolder, STARDNA.OAPPGeneratedCodeFolder ?? string.Empty);

            string gfCSharp = Path.Combine(genesisFolder, "CSharp");
            if (!Directory.Exists(gfCSharp))
                Directory.CreateDirectory(gfCSharp);

            if (!Directory.Exists(Path.Combine(gfCSharp, "Zomes")))
                Directory.CreateDirectory(Path.Combine(gfCSharp, "Zomes"));

            if (!Directory.Exists(Path.Combine(gfCSharp, "Holons")))
                Directory.CreateDirectory(Path.Combine(gfCSharp, "Holons"));

            if (!Directory.Exists(Path.Combine(gfCSharp, "Interfaces")))
                Directory.CreateDirectory(Path.Combine(gfCSharp, "Interfaces"));

            if (!Directory.Exists(Path.Combine(gfCSharp, "Interfaces", "Zomes")))
                Directory.CreateDirectory(Path.Combine(gfCSharp, "Interfaces", "Zomes"));

            if (!Directory.Exists(Path.Combine(gfCSharp, "Interfaces", "Holons")))
                Directory.CreateDirectory(Path.Combine(gfCSharp, "Interfaces", "Holons"));

            if (genesisType != GenesisType.ZomesAndHolonsOnly)
            {
                if (!Directory.Exists(Path.Combine(gfCSharp, "CelestialBodies")))
                    Directory.CreateDirectory(Path.Combine(gfCSharp, "CelestialBodies"));

                if (!Directory.Exists(Path.Combine(gfCSharp, "Interfaces", "CelestialBodies")))
                    Directory.CreateDirectory(Path.Combine(gfCSharp, "Interfaces", "CelestialBodies"));
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

                            File.WriteAllText(Path.Combine(genesisFolder, "CSharp", "Interfaces", "Holons", string.Concat("I", holonName, ".cs")), iholonBufferCsharp);
                            File.WriteAllText(Path.Combine(genesisFolder, "CSharp", "Holons", string.Concat(holonName, ".cs")), holonBufferCsharp);

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
                    File.WriteAllText(Path.Combine(genesisFolder, "CSharp", "Interfaces", "Zomes", string.Concat("I", zomeName, ".cs")), izomeBufferCsharp);
                    File.WriteAllText(Path.Combine(genesisFolder, "CSharp", "Zomes", string.Concat(zomeName, ".cs")), zomeBufferCsharp);
                }
            }

            // Remove any white space from the name.
            if (genesisType != GenesisType.ZomesAndHolonsOnly)
                File.WriteAllText(Path.Combine(genesisFolder, "CSharp", "CelestialBodies", string.Concat(OAPPName, Enum.GetName(typeof(GenesisType), genesisType), ".cs")), celestialBodyBufferCsharp);
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

    }
}

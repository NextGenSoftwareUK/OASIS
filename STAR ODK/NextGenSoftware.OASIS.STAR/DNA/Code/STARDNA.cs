using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.DNA
{
    public class STARDNA
    {
        //Default values that are used to generate a new STARDNA.json file if it is not found.

        //TODO: If BasePath is blank then all other paths below are absolute otherwise they are relative.
        //public string BasePath { get; set; } = @"C:\Users\\USER\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK";
        public string BasePath { get; set; } = @"C:\OASIS\STAR ODK\Release";
        //public string OASISRunTimePath { get; set; } = @"Runtimes\OASIS Runtime\OASIS Runtime (With Holochain Conductors Embedded) v3.3.1";
        //public string STARRunTimePath { get; set; } = @"Runtimes\STAR Runtime\STAR ODK Runtime v2.2.0 (With OASIS Runtime v3.3.1 HC Conductor Embedded)";
        //public string OASISBaseRuntimesPath { get; set; } = "Runtimes\\OASIS Runtimes";
        //public string STARBaseRuntimesPath { get; set; } = "Runtimes\\STAR Runtimes";
        //public string OAPPDNATemplatePath { get; set; } = @"STAR OAPP DNA Templates";
        //public string RustDNARSMTemplateFolder { get; set; } = @"NextGenSoftware.OASIS.STAR\DNATemplates\RustDNATemplates\RSM";
        //public string CSharpDNATemplateFolder { get; set; } = @"NextGenSoftware.OASIS.STAR\DNATemplates\CSharpDNATemplates";

        public string RustDNARSMTemplateFolder { get; set; } = @"DNATemplates\RustDNATemplates\RSM";
        public string CSharpDNATemplateFolder { get; set; } = @"DNATemplates\CSharpDNATemplates";

        //public string CelestialBodyDNA { get; set; } = @"NextGenSoftware.OASIS.STAR.TestHarness\CelestialBodyDNA";
        public string CelestialBodyDNA { get; set; } = "CelestialBodyDNA";
        //public string GenesisFolder { get; set; } = @"NextGenSoftware.OASIS.STAR.TestHarness\bin\Release\net8.0\Genesis";
        //public string GenesisNamespace { get; set; } = "NextGenSoftware.OASIS.STAR.TestHarness.Genesis";
        public string GenesisNamespace { get; set; } = "NextGenSoftware.OASIS.STAR.Genesis";
        public string TemplateNamespace { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates";
        public string RustTemplateLib { get; set; } = @"core\lib.rs";
        public string RustTemplateHolon { get; set; } = @"core\holon.rs";
        public string RustTemplateValidation { get; set; } = @"core\validation.rs";
        public string RustTemplateCreate { get; set; } = @"crud\create.rs";
        public string RustTemplateRead { get; set; } = @"crud\read.rs"; 
        public string RustTemplateUpdate { get; set; } = @"crud\update.rs";
        public string RustTemplateDelete { get; set; } = @"crud\delete.rs";
        public string RustTemplateList { get; set; } = @"crud\list.rs";
        public string RustTemplateInt { get; set; } = @"types\int.rs";
        public string RustTemplateString { get; set; } = @"types\string.rs";
        public string RustTemplateBool { get; set; } = @"types\bool.rs";
        public string CSharpTemplateIHolonDNA { get; set; } = @"Interfaces\IHolonDNATemplate.cs";
        public string CSharpTemplateHolonDNA { get; set; } = "HolonDNATemplate.cs";
        public string CSharpTemplateIZomeDNA { get; set; } = @"Interfaces\IZomeDNATemplate.cs";
        public string CSharpTemplateZomeDNA { get; set; } = "ZomeDNATemplate.cs";
        public string CSharpTemplateICelestialBodyDNA { get; set; } = @"Interfaces\ICelestialBodyDNATemplate.cs";
        public string CSharpTemplateCelestialBodyDNA { get; set; } = "CelestialBodyDNATemplate.cs";
        public string CSharpTemplateLoadHolonDNA { get; set; } = "LoadHolonDNATemplate.cs";
        public string CSharpTemplateSaveHolonDNA { get; set; } = "SaveHolonDNATemplate.cs";
        public string CSharpTemplateILoadHolonDNA { get; set; } = @"Interfaces\ILoadHolonDNATemplate.cs";
        public string CSharpTemplateISaveHolonDNA { get; set; } = @"Interfaces\ISaveHolonDNATemplate.cs";
        public string CSharpTemplateInt { get; set; } = @"types\int.cs";
        public string CSharpTemplateString { get; set; } = @"types\string.cs";
        public string CSharpTemplateBool { get; set; } = @"types\bool.cs";
        //public string OAPPConsoleTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.Console.DLL";
        //public string OAPPBlazorTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WebBlazor";
        //public string OAPPWebMVCTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WebMVC";
        //public string OAPPMAUITemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.MAUI";
        //public string OAPPUnityTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.Unity";
        //public string OAPPWinFormsTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WinForms";
        //public string OAPPWPFTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WPF";
        //public string OAPPWindowsServiceTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.WindowsService";
        //public string OAPPRESTServiceTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.RESTService";
        //public string OAPPgRPCServiceTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.gRPCService";
        //public string OAPPGraphQLServiceTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.GraphQLService";
        //public string OAPPCustomTemplateDNA { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.OApp.Custom";
        public string OAPPGeneratedCodeFolder { get; set; } = "Generated Code";
        public Dictionary<ProviderType, string> StarProviderKey { get; set; } = new Dictionary<ProviderType, string>();
        public string DefaultGreatGrandSuperStarId { get; set; }
        public string DefaultGrandSuperStarId { get; set; }
        public string DefaultSuperStarId { get; set; }
        public string DefaultStarId { get; set; }
        public string DefaultPlanetId { get; set; }

        public string DefaultOAPPsSourcePath { get; set; } = "OAPPs\\Source"; //Use to be the Genesis folder above. Not sure what sounds better? Source or Genesis?
        public string DefaultOAPPsPublishedPath { get; set; } = "OAPPs\\Published";
        public string DefaultOAPPsDownloadedPath { get; set; } = "OAPPs\\Downloaded";
        public string DefaultOAPPsInstalledPath { get; set; } = "OAPPs\\Installed";

        public string DefaultOAPPTemplatesSourcePath { get; set; } = "OAPPTemplates\\Source";
        public string DefaultOAPPTemplatesPublishedPath { get; set; } = "OAPPTemplates\\Published";
        public string DefaultOAPPTemplatesDownloadedPath { get; set; } = "OAPPTemplates\\Downloaded";
        public string DefaultOAPPTemplatesInstalledPath { get; set; } = "OAPPTemplates\\Installed";

        public string DefaultRuntimesSourcePath { get; set; } = "Runtimes\\Source";
        public string DefaultRuntimesPublishedPath { get; set; } = "Runtimes\\Published";
        public string DefaultRuntimesDownloadedPath { get; set; } = "Runtimes\\Downloaded";
        public string DefaultRuntimesInstalledPath { get; set; } = "Runtimes\\Installed\\Other";
        public string DefaultRuntimesInstalledOASISPath { get; set; } = "Runtimes\\Installed\\OASIS";
        public string DefaultRuntimesInstalledSTARPath { get; set; } = "Runtimes\\Installed\\STAR";

        public string DefaultChaptersSourcePath { get; set; } = "Chapters\\Source";
        public string DefaultChaptersPublishedPath { get; set; } = "Chapters\\Published";
        public string DefaultChaptersDownloadedPath { get; set; } = "Chapters\\Downloaded";
        public string DefaultChaptersInstalledPath { get; set; } = "Chapters\\Installed";

        public string DefaultMissionsSourcePath { get; set; } = "Missions\\Source";
        public string DefaultMissionsPublishedPath { get; set; } = "Missions\\Published";
        public string DefaultMissionsDownloadedPath { get; set; } = "Missions\\Downloaded";
        public string DefaultMissionsInstalledPath { get; set; } = "Missions\\Installed";

        public string DefaultQuestsSourcePath { get; set; } = "Quests\\Source";
        public string DefaultQuestsPublishedPath { get; set; } = "Quests\\Published";
        public string DefaultQuestsDownloadedPath { get; set; } = "Quests\\Downloaded";
        public string DefaultQuestsInstalledPath { get; set; } = "Quests\\Installed";

        public string DefaultNFTsSourcePath { get; set; } = "NFTs\\Source";
        public string DefaultNFTsPublishedPath { get; set; } = "NFTs\\Published";
        public string DefaultNFTsDownloadedPath { get; set; } = "NFTs\\Downloaded";
        public string DefaultNFTsInstalledPath { get; set; } = "NFTs\\Installed";

        public string DefaultGeoNFTsSourcePath { get; set; } = "GeoNFTs\\Source";
        public string DefaultGeoNFTsPublishedPath { get; set; } = "GeoNFTs\\Published";
        public string DefaultGeoNFTsDownloadedPath { get; set; } = "GeoNFTs\\Downloaded";
        public string DefaultGeoNFTsInstalledPath { get; set; } = "GeoNFTs\\Installed";

        public string DefaultGeoHotSpotsSourcePath { get; set; } = "GeoHotSpots\\Source";
        public string DefaultGeoHotSpotsPublishedPath { get; set; } = "GeoHotSpots\\Published";
        public string DefaultGeoHotSpotsDownloadedPath { get; set; } = "GeoHotSpots\\Downloaded";
        public string DefaultGeoHotSpotsInstalledPath { get; set; } = "GeoHotSpots\\Installed";

        public string DefaultInventoryItemsSourcePath { get; set; } = "InventoryItems\\Source";
        public string DefaultInventoryItemsPublishedPath { get; set; } = "InventoryItems\\Published";
        public string DefaultInventoryItemsDownloadedPath { get; set; } = "InventoryItems\\Downloaded";
        public string DefaultInventoryItemsInstalledPath { get; set; } = "InventoryItems\\Installed";

        public string DefaultCelestialSpacesSourcePath { get; set; } = "CelestialSpaces\\Source";
        public string DefaultCelestialSpacesPublishedPath { get; set; } = "CelestialSpaces\\Published";
        public string DefaultCelestialSpacesDownloadedPath { get; set; } = "CelestialSpaces\\Downloaded";
        public string DefaultCelestialSpacesInstalledPath { get; set; } = "CelestialSpaces\\Installed";

        public string DefaultCelestialBodiesSourcePath { get; set; } = "CelestialBodies\\Source";
        public string DefaultCelestialBodiesPublishedPath { get; set; } = "CelestialBodies\\Published";
        public string DefaultCelestialBodiesDownloadedPath { get; set; } = "CelestialBodies\\Downloaded";
        public string DefaultCelestialBodiesInstalledPath { get; set; } = "CelestialBodies\\Installed";

        public string DefaultZomesSourcePath { get; set; } = "Zomes\\Source";
        public string DefaultZomesPublishedPath { get; set; } = "Zomes\\Published";
        public string DefaultZomesDownloadedPath { get; set; } = "Zomes\\Downloaded";
        public string DefaultZomesInstalledPath { get; set; } = "Zomes\\Installed";

        public string DefaultHolonsSourcePath { get; set; } = "Holons\\Source";
        public string DefaultHolonsPublishedPath { get; set; } = "Holons\\Published";
        public string DefaultHolonsDownloadedPath { get; set; } = "Holons\\Downloaded";
        public string DefaultHolonsInstalledPath { get; set; } = "Holons\\Installed";

        public bool DetailedCOSMICOutputEnabled { get; set; } = false;
        public bool DetailedSTARStatusOutputEnabled { get; set; } = false;
        public bool DetailedLoggingEnabled { get; set; } = false;
    }
}
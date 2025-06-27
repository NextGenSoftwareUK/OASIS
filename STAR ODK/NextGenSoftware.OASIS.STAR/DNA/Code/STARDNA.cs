using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.DNA
{
    public class STARDNA
    {
        // Default values that are used to generate a new STARDNA.json file if it is not found.
        // If STARBasePath is blank then all other paths below are absolute otherwise they are relative to STARBasePath.
        public string BaseSTARPath { get; set; } = @"C:\Source\OASIS\STAR ODK\Release\STAR_ODK_v3.0.0";
        public string RustDNARSMTemplateFolder { get; set; } = @"DNATemplates\RustDNATemplates\RSM";  //Rust DNA Templates that hAPPs are built from (releative to STARBasePath above).
        public string CSharpDNATemplateFolder { get; set; } = @"DNATemplates\CSharpDNATemplates";  //C# DNA Templates (CelestialBodies, Zomes & Holons) that are used to generate OAPPs from (releative to STARBasePath above).
        public string CSharpDNATemplateNamespace { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates"; //The default namespace for the C# DNA Templates above.
        public string CelestialBodyDNA { get; set; } = "OAPP_Example_MetaData_DNA\\CelestialBodyDNA"; //Example Celestial Body DNA MetaData folder (that an OAPP is generated from and can contain zome and holon metadta), relative to the BaseSTARPath above.
        public string DefaultGenesisNamespace { get; set; } = "NextGenSoftware.OASIS.STAR.Genesis"; //The default namespace to be used when generating OAPPs (CelestialBodies).
        public string RustTemplateLib { get; set; } = @"core\lib.rs"; //releative to RustDNARSMTemplateFolder above.
        public string RustTemplateHolon { get; set; } = @"core\holon.rs"; //releative to RustDNARSMTemplateFolder above.
        public string RustTemplateValidation { get; set; } = @"core\validation.rs"; //releative to RustDNARSMTemplateFolder above.
        public string RustTemplateCreate { get; set; } = @"crud\create.rs"; //releative to RustDNARSMTemplateFolder above.
        public string RustTemplateRead { get; set; } = @"crud\read.rs";  //releative to RustDNARSMTemplateFolder above.
        public string RustTemplateUpdate { get; set; } = @"crud\update.rs"; //releative to RustDNARSMTemplateFolder above.
        public string RustTemplateDelete { get; set; } = @"crud\delete.rs"; //releative to RustDNARSMTemplateFolder above.
        public string RustTemplateList { get; set; } = @"crud\list.rs"; //releative to RustDNARSMTemplateFolder above.
        public string RustTemplateInt { get; set; } = @"types\int.rs"; //releative to RustDNARSMTemplateFolder above.
        public string RustTemplateString { get; set; } = @"types\string.rs"; //releative to RustDNARSMTemplateFolder above.
        public string RustTemplateBool { get; set; } = @"types\bool.rs"; //releative to RustDNARSMTemplateFolder above.
        public string CSharpTemplateIHolonDNA { get; set; } = @"Interfaces\IHolonDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateHolonDNA { get; set; } = "HolonDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateIZomeDNA { get; set; } = @"Interfaces\IZomeDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateZomeDNA { get; set; } = "ZomeDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateICelestialBodyDNA { get; set; } = @"Interfaces\ICelestialBodyDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateCelestialBodyDNA { get; set; } = "CelestialBodyDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateLoadHolonDNA { get; set; } = "LoadHolonDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateSaveHolonDNA { get; set; } = "SaveHolonDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateILoadHolonDNA { get; set; } = @"Interfaces\ILoadHolonDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateISaveHolonDNA { get; set; } = @"Interfaces\ISaveHolonDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateInt { get; set; } = @"types\int.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateString { get; set; } = @"types\string.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateBool { get; set; } = @"types\bool.cs"; //releative to CSharpDNATemplateFolder above.
        public string OAPPGeneratedCodeFolder { get; set; } = "Generated Code"; //The folder where the generated code for OAPPs is placed (releative to the root of the generated OAPP).
        public Dictionary<ProviderType, string> StarProviderKey { get; set; } = new Dictionary<ProviderType, string>();
        public string DefaultGreatGrandSuperStarId { get; set; } //The default Great Grand Super Star ID (at the centre of the Omniverse/God Head/Source) to use when creating new OAPPs and using COSMIC.
        public string DefaultGrandSuperStarId { get; set; } //The default Grand Super Star ID (at the centre of the Universe/Prime Creator) to use when creating new OAPPs and using COSMIC.
        public string DefaultSuperStarId { get; set; } //The default Super Star ID (Great Central Sun at the centre of the Milky Way) to use when creating new OAPPs and using COSMIC.
        public string DefaultStarId { get; set; } //The default Star ID (The Sun) to use when creating new OAPPs and using COSMIC.
        public string DefaultPlanetId { get; set; } //The default Planet ID (Our World) to use when creating new OAPPs and using COSMIC.

        public string BaseSTARNETPath { get; set; } = @"C:\Source\OASIS\STAR ODK\Release\STAR_ODK_v3.0.0\STARNET"; //If this is left blank then all STARNET paths below will be absolute otherwise they will be relative (NOTE: This is NOT STARBasePath above to allow the user data to be stored in a different location if needed).

        public string DefaultOAPPsSourcePath { get; set; } = "OAPPs\\Source"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultOAPPsPublishedPath { get; set; } = "OAPPs\\Published"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultOAPPsDownloadedPath { get; set; } = "OAPPs\\Downloaded"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultOAPPsInstalledPath { get; set; } = "OAPPs\\Installed"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).

        public string DefaultOAPPTemplatesSourcePath { get; set; } = "OAPPTemplates\\Source"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultOAPPTemplatesPublishedPath { get; set; } = "OAPPTemplates\\Published"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultOAPPTemplatesDownloadedPath { get; set; } = "OAPPTemplates\\Downloaded"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultOAPPTemplatesInstalledPath { get; set; } = "OAPPTemplates\\Installed"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).

        public string DefaultRuntimesSourcePath { get; set; } = "Runtimes\\Source"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultRuntimesPublishedPath { get; set; } = "Runtimes\\Published"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultRuntimesDownloadedPath { get; set; } = "Runtimes\\Downloaded"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultRuntimesInstalledPath { get; set; } = "Runtimes\\Installed\\Other"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultRuntimesInstalledOASISPath { get; set; } = "Runtimes\\Installed\\OASIS"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultRuntimesInstalledSTARPath { get; set; } = "Runtimes\\Installed\\STAR"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).

        public string DefaultChaptersSourcePath { get; set; } = "Chapters\\Source"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultChaptersPublishedPath { get; set; } = "Chapters\\Published"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultChaptersDownloadedPath { get; set; } = "Chapters\\Downloaded"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultChaptersInstalledPath { get; set; } = "Chapters\\Installed"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).

        public string DefaultMissionsSourcePath { get; set; } = "Missions\\Source"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultMissionsPublishedPath { get; set; } = "Missions\\Published"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultMissionsDownloadedPath { get; set; } = "Missions\\Downloaded"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultMissionsInstalledPath { get; set; } = "Missions\\Installed"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).

        public string DefaultQuestsSourcePath { get; set; } = "Quests\\Source"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultQuestsPublishedPath { get; set; } = "Quests\\Published"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultQuestsDownloadedPath { get; set; } = "Quests\\Downloaded"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultQuestsInstalledPath { get; set; } = "Quests\\Installed"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).

        public string DefaultNFTsSourcePath { get; set; } = "NFTs\\Source"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultNFTsPublishedPath { get; set; } = "NFTs\\Published"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultNFTsDownloadedPath { get; set; } = "NFTs\\Downloaded"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultNFTsInstalledPath { get; set; } = "NFTs\\Installed"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).

        public string DefaultGeoNFTsSourcePath { get; set; } = "GeoNFTs\\Source"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultGeoNFTsPublishedPath { get; set; } = "GeoNFTs\\Published"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultGeoNFTsDownloadedPath { get; set; } = "GeoNFTs\\Downloaded"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultGeoNFTsInstalledPath { get; set; } = "GeoNFTs\\Installed"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).

        public string DefaultGeoHotSpotsSourcePath { get; set; } = "GeoHotSpots\\Source"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultGeoHotSpotsPublishedPath { get; set; } = "GeoHotSpots\\Published"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultGeoHotSpotsDownloadedPath { get; set; } = "GeoHotSpots\\Downloaded"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).v
        public string DefaultGeoHotSpotsInstalledPath { get; set; } = "GeoHotSpots\\Installed"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).v

        public string DefaultInventoryItemsSourcePath { get; set; } = "InventoryItems\\Source"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultInventoryItemsPublishedPath { get; set; } = "InventoryItems\\Published"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultInventoryItemsDownloadedPath { get; set; } = "InventoryItems\\Downloaded"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).v
        public string DefaultInventoryItemsInstalledPath { get; set; } = "InventoryItems\\Installed"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).

        public string DefaultCelestialSpacesSourcePath { get; set; } = "CelestialSpaces\\Source"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultCelestialSpacesPublishedPath { get; set; } = "CelestialSpaces\\Published"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultCelestialSpacesDownloadedPath { get; set; } = "CelestialSpaces\\Downloaded"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultCelestialSpacesInstalledPath { get; set; } = "CelestialSpaces\\Installed"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).

        public string DefaultCelestialBodiesSourcePath { get; set; } = "CelestialBodies\\Source"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultCelestialBodiesPublishedPath { get; set; } = "CelestialBodies\\Published"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultCelestialBodiesDownloadedPath { get; set; } = "CelestialBodies\\Downloaded"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultCelestialBodiesInstalledPath { get; set; } = "CelestialBodies\\Installed"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).

        public string DefaultZomesSourcePath { get; set; } = "Zomes\\Source"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultZomesPublishedPath { get; set; } = "Zomes\\Published"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultZomesDownloadedPath { get; set; } = "Zomes\\Downloaded"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultZomesInstalledPath { get; set; } = "Zomes\\Installed"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).

        public string DefaultHolonsSourcePath { get; set; } = "Holons\\Source"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultHolonsPublishedPath { get; set; } = "Holons\\Published"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultHolonsDownloadedPath { get; set; } = "Holons\\Downloaded"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).
        public string DefaultHolonsInstalledPath { get; set; } = "Holons\\Installed"; //Can be releative to STARNETBasePath above or absolute (if STARNETBasePath is blank).

        public bool DetailedCOSMICOutputEnabled { get; set; } = false; //Turn on to get detailed output from COSMIC (the Cosmic Operating System for the Omniverse).
        public bool DetailedSTARStatusOutputEnabled { get; set; } = false; //Turn on to get detailed output from STAR (the status of the STAR ODK).
        public bool DetailedOASISHyperdriveLoggingEnabled { get; set; } = false; //Turn on to get detailed logging output from the OASIS Hyperdrive (this will log to the OASIS log file as well as the screen/console).
    }
}
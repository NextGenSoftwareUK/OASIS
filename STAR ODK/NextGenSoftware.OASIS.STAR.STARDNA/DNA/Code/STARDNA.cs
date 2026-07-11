using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.DNA
{
    /// <summary>
    /// STAR DNA configuration for Windows (templates, paths, defaults). Loaded from DNA/STAR_DNA.json.
    /// STARBasePath and STARNETBasePath are resolved at runtime when blank — see <see cref="STARDNAManager.ResolveRuntimeBasePaths"/>.
    /// Paths use forward slashes in defaults for cross-platform JSON; Path.Combine normalizes at runtime.
    /// </summary>
    public class STARDNA
    {
        // --- Paths resolved at runtime when blank (same folder as star executable / app output) ---

        /// <summary>Path to OASIS_DNA.json. Blank = use built-in SYSTEM OASIS DNA.</summary>
        public string OASISDNAPath { get; set; }

        /// <summary>Root for STAR templates and generated OAPP meta data. Blank = folder containing the STAR executable (publish/bin). Set to an absolute path, or a path relative to that folder (e.g. "STARData"). All paths below can be relative to this or absolute.</summary>
        public string STARBasePath { get; set; } = "";

        /// <summary>Root for STARNET user data (OAPPs, runtimes, libs, etc.). Blank = STARBasePath/STARNET so data sits in a STARNET subfolder. Set to override (absolute or relative to app folder). Paths below are relative to this or absolute.</summary>
        public string STARNETBasePath { get; set; } = "STARNET";

        // --- Paths under STARBasePath (relative or absolute) ---

        public string MetaDataDNATemplateFolder { get; set; } = "DNATemplates/MetaDataDNATemplates";
        public string CSharpDNATemplateFolder { get; set; } = "DNATemplates/CSharpDNATemplates";
        public string CSharpDNATemplateNamespace { get; set; } = "NextGenSoftware.OASIS.STAR.DNATemplates.CSharpTemplates";
        public string OAPPMetaDataDNAFolder { get; set; } = "OAPPMetaDataDNA";
        public string DefaultGenesisNamespace { get; set; } = "NextGenSoftware.OASIS.STAR.Genesis";
        public string ZomeMetaDataDNA { get; set; } = "ZomeMetaDataDNA.cs";
        public string HolonMetaDataDNA { get; set; } = "HolonMetaDataDNA.cs";
        public string CSharpTemplateIHolonDNA { get; set; } = "Interfaces/IHolonDNATemplate.cs";
        public string CSharpTemplateHolonDNA { get; set; } = "HolonDNATemplate.cs";
        public string CSharpTemplateIZomeDNA { get; set; } = "Interfaces/IZomeDNATemplate.cs";
        public string CSharpTemplateZomeDNA { get; set; } = "ZomeDNATemplate.cs";
        public string CSharpTemplateICelestialBodyDNA { get; set; } = "Interfaces/ICelestialBodyDNATemplate.cs";
        public string CSharpTemplateCelestialBodyDNA { get; set; } = "CelestialBodyDNATemplate.cs";
        public string CSharpTemplateLoadHolonDNA { get; set; } = "LoadHolonDNATemplate.cs";
        public string CSharpTemplateSaveHolonDNA { get; set; } = "SaveHolonDNATemplate.cs";
        public string CSharpTemplateILoadHolonDNA { get; set; } = "Interfaces/ILoadHolonDNATemplate.cs";
        public string CSharpTemplateISaveHolonDNA { get; set; } = "Interfaces/ISaveHolonDNATemplate.cs";
        public string CSharpTemplateInt { get; set; } = "Types/int.cs";
        public string CSharpTemplateString { get; set; } = "Types/string.cs";
        public string CSharpTemplateBool { get; set; } = "Types/bool.cs";
        public string OAPPGeneratedCodeFolder { get; set; } = "Generated Code";
        public Dictionary<ProviderType, string> StarProviderKey { get; set; } = new Dictionary<ProviderType, string>();
        public string DefaultGreatGrandSuperStarId { get; set; }
        public string DefaultGrandSuperStarId { get; set; }
        public string DefaultSuperStarId { get; set; }
        public string DefaultStarId { get; set; }
        public string DefaultPlanetId { get; set; }

        // --- Paths under STARNETBasePath (relative or absolute) ---
        public string DefaultOAPPsSourcePath { get; set; } = "OAPPs/Source";
        public string DefaultOAPPsPublishedPath { get; set; } = "OAPPs/Published";
        public string DefaultOAPPsDownloadedPath { get; set; } = "OAPPs/Downloaded";
        public string DefaultOAPPsInstalledPath { get; set; } = "OAPPs/Installed";

        public string DefaultOAPPTemplatesSourcePath { get; set; } = "OAPPTemplates/Source";
        public string DefaultOAPPTemplatesPublishedPath { get; set; } = "OAPPTemplates/Published";
        public string DefaultOAPPTemplatesDownloadedPath { get; set; } = "OAPPTemplates/Downloaded";
        public string DefaultOAPPTemplatesInstalledPath { get; set; } = "OAPPTemplates/Installed";

        public string DefaultRuntimesSourcePath { get; set; } = "Runtimes/Source";
        public string DefaultRuntimesPublishedPath { get; set; } = "Runtimes/Published";
        public string DefaultRuntimesDownloadedPath { get; set; } = "Runtimes/Downloaded";
        public string DefaultRuntimesInstalledPath { get; set; } = "Runtimes/Installed";
        public string DefaultRuntimesInstalledOASISPath { get; set; } = "Runtimes/Installed";
        public string DefaultRuntimesInstalledSTARPath { get; set; } = "Runtimes/Installed";

        public string DefaultLibsSourcePath { get; set; } = "Libs/Source";
        public string DefaultLibsPublishedPath { get; set; } = "Libs/Published";
        public string DefaultLibsDownloadedPath { get; set; } = "Libs/Downloaded";
        public string DefaultLibsInstalledPath { get; set; } = "Libs/Installed";

        public string DefaultChaptersSourcePath { get; set; } = "Chapters/Source";
        public string DefaultChaptersPublishedPath { get; set; } = "Chapters/Published";
        public string DefaultChaptersDownloadedPath { get; set; } = "Chapters/Downloaded";
        public string DefaultChaptersInstalledPath { get; set; } = "Chapters/Installed";

        public string DefaultMissionsSourcePath { get; set; } = "Missions/Source";
        public string DefaultMissionsPublishedPath { get; set; } = "Missions/Published";
        public string DefaultMissionsDownloadedPath { get; set; } = "Missions/Downloaded";
        public string DefaultMissionsInstalledPath { get; set; } = "Missions/Installed";

        public string DefaultQuestsSourcePath { get; set; } = "Quests/Source";
        public string DefaultQuestsPublishedPath { get; set; } = "Quests/Published";
        public string DefaultQuestsDownloadedPath { get; set; } = "Quests/Downloaded";
        public string DefaultQuestsInstalledPath { get; set; } = "Quests/Installed";

        public string DefaultGamesSourcePath { get; set; } = "Games/Source";
        public string DefaultGamesPublishedPath { get; set; } = "Games/Published";
        public string DefaultGamesDownloadedPath { get; set; } = "Games/Downloaded";
        public string DefaultGamesInstalledPath { get; set; } = "Games/Installed";

        public string DefaultNFTsSourcePath { get; set; } = "NFTs/Source";
        public string DefaultNFTsPublishedPath { get; set; } = "NFTs/Published";
        public string DefaultNFTsDownloadedPath { get; set; } = "NFTs/Downloaded";
        public string DefaultNFTsInstalledPath { get; set; } = "NFTs/Installed";

        public string DefaultGeoNFTsSourcePath { get; set; } = "GeoNFTs/Source";
        public string DefaultGeoNFTsPublishedPath { get; set; } = "GeoNFTs/Published";
        public string DefaultGeoNFTsDownloadedPath { get; set; } = "GeoNFTs/Downloaded";
        public string DefaultGeoNFTsInstalledPath { get; set; } = "GeoNFTs/Installed";

        public string DefaultNFTCollectionsSourcePath { get; set; } = "NFTCollections/Source";
        public string DefaultNFTCollectionsPublishedPath { get; set; } = "NFTCollections/Published";
        public string DefaultNFTCollectionsDownloadedPath { get; set; } = "NFTCollections/Downloaded";
        public string DefaultNFTCollectionsInstalledPath { get; set; } = "NFTCollections/Installed";

        public string DefaultGeoNFTCollectionsSourcePath { get; set; } = "GeoNFTCollections/Source";
        public string DefaultGeoNFTCollectionsPublishedPath { get; set; } = "GeoNFTCollections/Published";
        public string DefaultGeoNFTCollectionsDownloadedPath { get; set; } = "GeoNFTCollections/Downloaded";
        public string DefaultGeoNFTCollectionsInstalledPath { get; set; } = "GeoNFTCollections/Installed";

        public string DefaultGeoHotSpotsSourcePath { get; set; } = "GeoHotSpots/Source";
        public string DefaultGeoHotSpotsPublishedPath { get; set; } = "GeoHotSpots/Published";
        public string DefaultGeoHotSpotsDownloadedPath { get; set; } = "GeoHotSpots/Downloaded";
        public string DefaultGeoHotSpotsInstalledPath { get; set; } = "GeoHotSpots/Installed";

        public string DefaultInventoryItemsSourcePath { get; set; } = "InventoryItems/Source";
        public string DefaultInventoryItemsPublishedPath { get; set; } = "InventoryItems/Published";
        public string DefaultInventoryItemsDownloadedPath { get; set; } = "InventoryItems/Downloaded";
        public string DefaultInventoryItemsInstalledPath { get; set; } = "InventoryItems/Installed";

        public string DefaultCelestialSpacesSourcePath { get; set; } = "CelestialSpaces/Source";
        public string DefaultCelestialSpacesPublishedPath { get; set; } = "CelestialSpaces/Published";
        public string DefaultCelestialSpacesDownloadedPath { get; set; } = "CelestialSpaces/Downloaded";
        public string DefaultCelestialSpacesInstalledPath { get; set; } = "CelestialSpaces/Installed";

        public string DefaultCelestialBodiesSourcePath { get; set; } = "CelestialBodies/Source";
        public string DefaultCelestialBodiesPublishedPath { get; set; } = "CelestialBodies/Published";
        public string DefaultCelestialBodiesDownloadedPath { get; set; } = "CelestialBodies/Downloaded";
        public string DefaultCelestialBodiesInstalledPath { get; set; } = "CelestialBodies/Installed";

        public string DefaultZomesSourcePath { get; set; } = "Zomes/Source";
        public string DefaultZomesPublishedPath { get; set; } = "Zomes/Published";
        public string DefaultZomesDownloadedPath { get; set; } = "Zomes/Downloaded";
        public string DefaultZomesInstalledPath { get; set; } = "Zomes/Installed";

        public string DefaultHolonsSourcePath { get; set; } = "Holons/Source";
        public string DefaultHolonsPublishedPath { get; set; } = "Holons/Published";
        public string DefaultHolonsDownloadedPath { get; set; } = "Holons/Downloaded";
        public string DefaultHolonsInstalledPath { get; set; } = "Holons/Installed";

        public string DefaultCelestialBodiesMetaDataDNASourcePath { get; set; } = "CelestialBodiesMetaDataDNA/Source";
        public string DefaultCelestialBodiesMetaDataDNAPublishedPath { get; set; } = "CelestialBodiesMetaDataDNA/Published";
        public string DefaultCelestialBodiesMetaDataDNADownloadedPath { get; set; } = "CelestialBodiesMetaDataDNA/Downloaded";
        public string DefaultCelestialBodiesMetaDataDNAInstalledPath { get; set; } = "CelestialBodiesMetaDataDNA/Installed";

        public string DefaultZomesMetaDataDNASourcePath { get; set; } = "ZomesMetaDataDNA/Source";
        public string DefaultZomesMetaDataDNAPublishedPath { get; set; } = "ZomesMetaDataDNA/Published";
        public string DefaultZomesMetaDataDNADownloadedPath { get; set; } = "ZomesMetaDataDNA/Downloaded";
        public string DefaultZomesMetaDataDNAInstalledPath { get; set; } = "ZomesMetaDataDNA/Installed";

        public string DefaultHolonsMetaDataDNASourcePath { get; set; } = "HolonsMetaDataDNA/Source";
        public string DefaultHolonsMetaDataDNAPublishedPath { get; set; } = "HolonsMetaDataDNA/Published";
        public string DefaultHolonsMetaDataDNADownloadedPath { get; set; } = "HolonsMetaDataDNA/Downloaded";
        public string DefaultHolonsMetaDataDNAInstalledPath { get; set; } = "HolonsMetaDataDNA/Installed";

        public string DefaultPluginsSourcePath { get; set; } = "Plugins/Source";
        public string DefaultPluginsPublishedPath { get; set; } = "Plugins/Published";
        public string DefaultPluginsDownloadedPath { get; set; } = "Plugins/Downloaded";
        public string DefaultPluginsInstalledPath { get; set; } = "Plugins/Installed";

        public bool DetailedCOSMICOutputEnabled { get; set; } = false;
        public bool DetailedSTARStatusOutputEnabled { get; set; } = false;
        public bool DetailedOASISHyperdriveLoggingEnabled { get; set; } = false;
    }
}

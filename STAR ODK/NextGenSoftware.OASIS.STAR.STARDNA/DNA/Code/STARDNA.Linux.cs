using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.DNA
{
    /// <summary>
    /// STAR DNA configuration (templates, paths, defaults). Loaded from DNA/STAR_DNA.json.
    /// STARBasePath and STARNETBasePath are resolved at runtime when blank — see <see cref="STARDNAManager.ResolveRuntimeBasePaths"/>.
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
        public string DefaultGenesisNamespace { get; set; } = "NextGenSoftware.OASIS.STAR.Genesis"; //The default namespace to be used when generating OAPPs (CelestialBodies).
        public string ZomeMetaDataDNA { get; set; } = "ZomeMetaDataDNA.cs"; //Can be relative to MetaDataDNATemplateFolder or absolute.
        public string HolonMetaDataDNA { get; set; } = "HolonMetaDataDNA.cs"; //Can be relative to MetaDataDNATemplateFolder or absolute.
        public string CSharpTemplateIHolonDNA { get; set; } = @"Interfaces/IHolonDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateHolonDNA { get; set; } = "HolonDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateIZomeDNA { get; set; } = @"Interfaces/IZomeDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateZomeDNA { get; set; } = "ZomeDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateICelestialBodyDNA { get; set; } = @"Interfaces/ICelestialBodyDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateCelestialBodyDNA { get; set; } = "CelestialBodyDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateLoadHolonDNA { get; set; } = "LoadHolonDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateSaveHolonDNA { get; set; } = "SaveHolonDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateILoadHolonDNA { get; set; } = @"Interfaces/ILoadHolonDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateISaveHolonDNA { get; set; } = @"Interfaces/ISaveHolonDNATemplate.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateInt { get; set; } = @"Types/int.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateString { get; set; } = @"Types/string.cs"; //releative to CSharpDNATemplateFolder above.
        public string CSharpTemplateBool { get; set; } = @"Types/bool.cs"; //releative to CSharpDNATemplateFolder above.
        public string OAPPGeneratedCodeFolder { get; set; } = "Generated Code"; //The folder where the generated code for OAPPs is placed (releative to the root of the generated OAPP).
        public Dictionary<ProviderType, string> StarProviderKey { get; set; } = new Dictionary<ProviderType, string>();
        public string DefaultGreatGrandSuperStarId { get; set; } //The default Great Grand Super Star ID (at the centre of the Omniverse/God Head/Source) to use when creating new OAPPs and using COSMIC.
        public string DefaultGrandSuperStarId { get; set; } //The default Grand Super Star ID (at the centre of the Universe/Prime Creator) to use when creating new OAPPs and using COSMIC.
        public string DefaultSuperStarId { get; set; } //The default Super Star ID (Great Central Sun at the centre of the Milky Way) to use when creating new OAPPs and using COSMIC.
        public string DefaultStarId { get; set; } //The default Star ID (The Sun) to use when creating new OAPPs and using COSMIC.
        public string DefaultPlanetId { get; set; } //The default Planet ID (Our World) to use when creating new OAPPs and using COSMIC.

        // --- Paths under STARNETBasePath (relative or absolute) ---
        public string DefaultOAPPsSourcePath { get; set; } = "OAPPs/Source";
        public string DefaultOAPPsPublishedPath { get; set; } = "OAPPs/Published";
        public string DefaultOAPPsDownloadedPath { get; set; } = "OAPPs/Downloaded";
        public string DefaultOAPPsInstalledPath { get; set; } = "OAPPs/Installed";


        //OAPP Templates are used to create OAPPs along with Runtimes.
        public string DefaultOAPPTemplatesSourcePath { get; set; } = "OAPPTemplates/Source";
        public string DefaultOAPPTemplatesPublishedPath { get; set; } = "OAPPTemplates/Published";
        public string DefaultOAPPTemplatesDownloadedPath { get; set; } = "OAPPTemplates/Downloaded";
        public string DefaultOAPPTemplatesInstalledPath { get; set; } = "OAPPTemplates/Installed";


        //Runtimes are used to run OAPPs and can be installed on the user's machine or downloaded from the OASIS. Different runtimes can be combined with different OAPP Templates making unique combinations for OAPPs.
        public string DefaultRuntimesSourcePath { get; set; } = "Runtimes/Source";
        public string DefaultRuntimesPublishedPath { get; set; } = "Runtimes/Published";
        public string DefaultRuntimesDownloadedPath { get; set; } = "Runtimes/Downloaded";
        //public string DefaultRuntimesInstalledPath { get; set; } = "Runtimes/Installed/Other"; 
        //public string DefaultRuntimesInstalledOASISPath { get; set; } = "Runtimes/Installed/OASIS"; 
        //public string DefaultRuntimesInstalledSTARPath { get; set; } = "Runtimes/Installed/STAR";
        public string DefaultRuntimesInstalledPath { get; set; } = "Runtimes/Installed"; //TODO: NEED TO MAKE CUSTOM CHANGES TO RUNTIME MANAGER/UI ETC TO WORK WITH PATHS ABOVE!
        public string DefaultRuntimesInstalledOASISPath { get; set; } = "Runtimes/Installed";
        public string DefaultRuntimesInstalledSTARPath { get; set; } = "Runtimes/Installed";


        //Libs are used in OAPPs and can be installed on the user's machine or downloaded from the OASIS. Different libs can be combined with different OAPP Templates & Runtimes making unique combinations for OAPPs.
        public string DefaultLibsSourcePath { get; set; } = "Libs/Source";
        public string DefaultLibsPublishedPath { get; set; } = "Libs/Published";
        public string DefaultLibsDownloadedPath { get; set; } = "Libs/Downloaded";
        public string DefaultLibsInstalledPath { get; set; } = "Libs/Installed";


        //Chapters contain Quests and are used to break down big quests into seperate Chapters.
        public string DefaultChaptersSourcePath { get; set; } = "Chapters/Source";
        public string DefaultChaptersPublishedPath { get; set; } = "Chapters/Published";
        public string DefaultChaptersDownloadedPath { get; set; } = "Chapters/Downloaded";
        public string DefaultChaptersInstalledPath { get; set; } = "Chapters/Installed";


        //Missions contain Quests and optionally Chapters.
        public string DefaultMissionsSourcePath { get; set; } = "Missions/Source";
        public string DefaultMissionsPublishedPath { get; set; } = "Missions/Published";
        public string DefaultMissionsDownloadedPath { get; set; } = "Missions/Downloaded";
        public string DefaultMissionsInstalledPath { get; set; } = "Missions/Installed";


        //Quests contain GeoNFTs, GeoHotSpots and InventoryItems (that are rewarded when you complete a quest).
        public string DefaultQuestsSourcePath { get; set; } = "Quests/Source";
        public string DefaultQuestsPublishedPath { get; set; } = "Quests/Published";
        public string DefaultQuestsDownloadedPath { get; set; } = "Quests/Downloaded";
        public string DefaultQuestsInstalledPath { get; set; } = "Quests/Installed";

        //Games support cross-game interoperability, shared inventory, avatar SSO, karma scores, NFTs, and keycards.
        public string DefaultGamesSourcePath { get; set; } = "Games/Source";
        public string DefaultGamesPublishedPath { get; set; } = "Games/Published";
        public string DefaultGamesDownloadedPath { get; set; } = "Games/Downloaded";
        public string DefaultGamesInstalledPath { get; set; } = "Games/Installed";


        //OASIS NFTs (wrap around all types of web3 NFTs and form an abstraction layer to convert between standards and chains) are Non-Fungible Tokens that can be used to represent unique items, assets or collectibles in the Omniverse/OASIS/Our World.
        public string DefaultNFTsSourcePath { get; set; } = "NFTs/Source";
        public string DefaultNFTsPublishedPath { get; set; } = "NFTs/Published";
        public string DefaultNFTsDownloadedPath { get; set; } = "NFTs/Downloaded";
        public string DefaultNFTsInstalledPath { get; set; } = "NFTs/Installed";


        //GeoNFTs are Geographical Non-Fungible Tokens that can be used to represent unique geographical locations or assets in the Omniverse/OASIS/Our World. (GeoNFTs can be created from OASIS NFTs).
        public string DefaultGeoNFTsSourcePath { get; set; } = "GeoNFTs/Source";
        public string DefaultGeoNFTsPublishedPath { get; set; } = "GeoNFTs/Published";
        public string DefaultGeoNFTsDownloadedPath { get; set; } = "GeoNFTs/Downloaded";
        public string DefaultGeoNFTsInstalledPath { get; set; } = "GeoNFTs/Installed";

        //NFT Collections are collections of NFTs.
        public string DefaultNFTCollectionsSourcePath { get; set; } = "NFTCollections/Source";
        public string DefaultNFTCollectionsPublishedPath { get; set; } = "NFTCollections/Published";
        public string DefaultNFTCollectionsDownloadedPath { get; set; } = "NFTCollections/Downloaded";
        public string DefaultNFTCollectionsInstalledPath { get; set; } = "NFTCollections/Installed";


        //GeoNFT Collections are collections of GeoNFTs.
        public string DefaultGeoNFTCollectionsSourcePath { get; set; } = "GeoNFTCollections/Source";
        public string DefaultGeoNFTCollectionsPublishedPath { get; set; } = "GeoNFTCollections/Published";
        public string DefaultGeoNFTCollectionsDownloadedPath { get; set; } = "GeoNFTCollections/Downloaded";
        public string DefaultGeoNFTCollectionsInstalledPath { get; set; } = "GeoNFTCollections/Installed";


        //GeoHotSpots are special geolocations within Our World/Omniverse that can be triggered when you arrive at the location, when you activate AR mode or interact with a virtual object at that location.
        public string DefaultGeoHotSpotsSourcePath { get; set; } = "GeoHotSpots/Source";
        public string DefaultGeoHotSpotsPublishedPath { get; set; } = "GeoHotSpots/Published";
        public string DefaultGeoHotSpotsDownloadedPath { get; set; } = "GeoHotSpots/Downloaded";
        public string DefaultGeoHotSpotsInstalledPath { get; set; } = "GeoHotSpots/Installed";


        //InventoryItems are items that can be collected/rewarded, traded or used within the Omniverse/OASIS/Our World such as weapons, shields, armor, potions, power ups etc for your Avatar and much much more! ;-)
        public string DefaultInventoryItemsSourcePath { get; set; } = "InventoryItems/Source";
        public string DefaultInventoryItemsPublishedPath { get; set; } = "InventoryItems/Published";
        public string DefaultInventoryItemsDownloadedPath { get; set; } = "InventoryItems/Downloaded";
        public string DefaultInventoryItemsInstalledPath { get; set; } = "InventoryItems/Installed";


        //CelestialSpaces (such as SolarSystem's, Galaxies, Universes etc) contain CelestialBodies.
        public string DefaultCelestialSpacesSourcePath { get; set; } = "CelestialSpaces/Source";
        public string DefaultCelestialSpacesPublishedPath { get; set; } = "CelestialSpaces/Published";
        public string DefaultCelestialSpacesDownloadedPath { get; set; } = "CelestialSpaces/Downloaded";
        public string DefaultCelestialSpacesInstalledPath { get; set; } = "CelestialSpaces/Installed";


        //CelestialBodies are the planets, moons, stars, galaxies etc in the Omniverse and can contain Zomes and Holons. They also represet your OAPP in the Omniverse.
        public string DefaultCelestialBodiesSourcePath { get; set; } = "CelestialBodies/Source";
        public string DefaultCelestialBodiesPublishedPath { get; set; } = "CelestialBodies/Published";
        public string DefaultCelestialBodiesDownloadedPath { get; set; } = "CelestialBodies/Downloaded";
        public string DefaultCelestialBodiesInstalledPath { get; set; } = "CelestialBodies/Installed";


        //Zomes are the building blocks of Celestial Bodies and can contain Holons. They are like modules that can be used to build Celestial Bodies.
        public string DefaultZomesSourcePath { get; set; } = "Zomes/Source";
        public string DefaultZomesPublishedPath { get; set; } = "Zomes/Published";
        public string DefaultZomesDownloadedPath { get; set; } = "Zomes/Downloaded";
        public string DefaultZomesInstalledPath { get; set; } = "Zomes/Installed";


        //Holons are the individual components of Zomes and can contain data, logic and functionality. They are like the building blocks of Zomes.//Holons are the individual components of Zomes and can contain data, logic and functionality. They are like the building blocks of Zomes.
        public string DefaultHolonsSourcePath { get; set; } = "Holons/Source";
        public string DefaultHolonsPublishedPath { get; set; } = "Holons/Published";
        public string DefaultHolonsDownloadedPath { get; set; } = "Holons/Downloaded";
        public string DefaultHolonsInstalledPath { get; set; } = "Holons/Installed";


        //CelestialBodiesMetaDataDNA is the DNA that contains the metadata for Celestial Bodies.
        public string DefaultCelestialBodiesMetaDataDNASourcePath { get; set; } = "CelestialBodies/Source";
        public string DefaultCelestialBodiesMetaDataDNAPublishedPath { get; set; } = "CelestialBodies/Published";
        public string DefaultCelestialBodiesMetaDataDNADownloadedPath { get; set; } = "CelestialBodies/Downloaded";
        public string DefaultCelestialBodiesMetaDataDNAInstalledPath { get; set; } = "CelestialBodies/Installed";


        //ZomesMetaDataDNA is the DNA that contains the metadata for Zomes.
        public string DefaultZomesMetaDataDNASourcePath { get; set; } = "Zomes/Source";
        public string DefaultZomesMetaDataDNAPublishedPath { get; set; } = "Zomes/Published";
        public string DefaultZomesMetaDataDNADownloadedPath { get; set; } = "Zomes/Downloaded";
        public string DefaultZomesMetaDataDNAInstalledPath { get; set; } = "Zomes/Installed";


        //HolonsMetaDataDNA is the DNA that contains the metadata for Holons.
        public string DefaultHolonsMetaDataDNASourcePath { get; set; } = "Holons/Source";
        public string DefaultHolonsMetaDataDNAPublishedPath { get; set; } = "Holons/Published";
        public string DefaultHolonsMetaDataDNADownloadedPath { get; set; } = "Holons/Downloaded";
        public string DefaultHolonsMetaDataDNAInstalledPath { get; set; } = "Holons/Installed";


        //Plugins to extend STAR & STARNET.
        public string DefaultPluginsSourcePath { get; set; } = "Plugins/Source";
        public string DefaultPluginsPublishedPath { get; set; } = "Plugins/Published";
        public string DefaultPluginsDownloadedPath { get; set; } = "Plugins/Downloaded";
        public string DefaultPluginsInstalledPath { get; set; } = "Plugins/Installed";


        //OASIS Settings
        public bool DetailedCOSMICOutputEnabled { get; set; } = false; //Turn on to get detailed output from COSMIC (the Cosmic Operating System for the Omniverse).
        public bool DetailedSTARStatusOutputEnabled { get; set; } = false; //Turn on to get detailed output from STAR (the status of the STAR ODK).
        public bool DetailedOASISHyperdriveLoggingEnabled { get; set; } = false; //Turn on to get detailed logging output from the OASIS Hyperdrive (this will log to the OASIS log file as well as the screen/console).
    }
}
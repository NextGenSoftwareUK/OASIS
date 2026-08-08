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
        private static void ValidateSTARDNA(STARDNA starDNA)
        {
            if (starDNA != null)
            {
                STARDNAManager.ResolveRuntimeBasePaths(starDNA);

                ValidateFolder("", starDNA.STARBasePath, "STARDNA.STARBasePath");
                ValidateFolder(starDNA.STARBasePath, starDNA.MetaDataDNATemplateFolder, "STARDNA.MetaDataDNATemplateFolder");
                ValidateFolder(starDNA.STARBasePath, starDNA.OAPPMetaDataDNAFolder, "STARDNA.OAPPMetaDataDNAFolder", false, true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.GenesisFolder, "STARDNA.GenesisFolder", false, true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.GenesisRustFolder, "STARDNA.GenesisRustFolder", false, true);
                ValidateFolder(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, "STARDNA.CSharpDNATemplateFolder");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateHolonDNA, "STARDNA.CSharpTemplateHolonDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateZomeDNA, "STARDNA.CSharpTemplateZomeDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateCelestialBodyDNA, "STARDNA.CSharpTemplateCelestialBodyDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateLoadHolonDNA, "STARDNA.CSharpTemplateLoadHolonDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateSaveHolonDNA, "STARDNA.CSharpTemplateSaveHolonDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateILoadHolonDNA, "STARDNA.CSharpTemplateILoadHolonDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateISaveHolonDNA, "STARDNA.CSharpTemplateISaveHolonDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateInt, "STARDNA.CSharpTemplateInt");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateString, "STARDNA.CSharpTemplateString");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateBool, "STARDNA.CSharpTemplateBool");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateIHolonDNA, "STARDNA.CSharpTemplateIHolonDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateIZomeDNA, "STARDNA.CSharpTemplateIZomeDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateICelestialBodyDNA, "STARDNA.CSharpTemplateICelestialBodyDNA");

                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPBlazorTemplateDNA, "STARDNA.OAPPBlazorTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPConsoleTemplateDNA, "STARDNA.OAPPConsoleTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPCustomTemplateDNA, "STARDNA.OAPPCustomTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPGraphQLServiceTemplateDNA, "STARDNA.OAPPGraphQLServiceTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPgRPCServiceTemplateDNA, "STARDNA.OAPPgRPCServiceTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPMAUITemplateDNA, "STARDNA.OAPPMAUITemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPRESTServiceTemplateDNA, "STARDNA.OAPPRESTServiceTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPUnityTemplateDNA, "STARDNA.OAPPUnityTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPWebMVCTemplateDNA, "STARDNA.OAPPWebMVCTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPWindowsServiceTemplateDNA, "STARDNA.OAPPWindowsServiceTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPWinFormsTemplateDNA, "STARDNA.OAPPWinFormsTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPWPFTemplateDNA, "STARDNA.OAPPWPFTemplateDNA", true);


                // Rust template validation moved to HoloOASIS - commented out for rollback purposes:
                //ValidateFolder(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, "STARDNA.RustDNARSMTemplateFolder");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateLib, "STARDNA.RustTemplateLib");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateCreate, "STARDNA.RustTemplateCreate");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateDelete, "STARDNA.RustTemplateDelete");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateRead, "STARDNA.RustTemplateRead");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateUpdate, "STARDNA.RustTemplateUpdate");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateList, "STARDNA.RustTemplateList");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateValidation, "STARDNA.RustTemplateValidation");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateInt, "STARDNA.RustTemplateInt");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateString, "STARDNA.RustTemplateString");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateBool, "STARDNA.RustTemplateBool");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateHolon, "STARDNA.RustTemplateHolon");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPsSourcePath))
                    starDNA.DefaultOAPPsSourcePath = Path.Combine("OAPPs", "Source");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPsPublishedPath))
                    starDNA.DefaultOAPPsPublishedPath = Path.Combine("OAPPs", "Published");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPsDownloadedPath))
                    starDNA.DefaultOAPPsDownloadedPath = Path.Combine("OAPPs", "Downloaded");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPsInstalledPath))
                    starDNA.DefaultOAPPsInstalledPath = Path.Combine("OAPPs", "Installed");


                if (string.IsNullOrEmpty(starDNA.DefaultOAPPTemplatesSourcePath))
                    starDNA.DefaultOAPPTemplatesSourcePath = Path.Combine("OAPPTemplates", "Source");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPTemplatesPublishedPath))
                    starDNA.DefaultOAPPTemplatesPublishedPath = Path.Combine("OAPPTemplates", "Published");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPTemplatesDownloadedPath))
                    starDNA.DefaultOAPPTemplatesDownloadedPath = Path.Combine("OAPPTemplates", "Downloaded");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPTemplatesInstalledPath))
                    starDNA.DefaultOAPPTemplatesInstalledPath = Path.Combine("OAPPTemplates", "Installed");


                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesSourcePath))
                    starDNA.DefaultRuntimesSourcePath = Path.Combine("Runtimes", "Source");

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesPublishedPath))
                    starDNA.DefaultRuntimesPublishedPath = Path.Combine("Runtimes", "Published");

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesDownloadedPath))
                    starDNA.DefaultRuntimesDownloadedPath = Path.Combine("Runtimes", "Downloaded");

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesInstalledPath))
                    starDNA.DefaultRuntimesInstalledPath = Path.Combine("Runtimes", "Installed", "Other");

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesInstalledOASISPath))
                    starDNA.DefaultRuntimesInstalledOASISPath = Path.Combine("Runtimes", "Installed", "OASIS");

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesInstalledSTARPath))
                    starDNA.DefaultRuntimesInstalledSTARPath = Path.Combine("Runtimes", "Installed", "STAR");

                if (string.IsNullOrEmpty(starDNA.DefaultLibsSourcePath))
                    starDNA.DefaultLibsSourcePath = Path.Combine("Libs", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultLibsPublishedPath))
                    starDNA.DefaultLibsPublishedPath = Path.Combine("Libs", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultLibsDownloadedPath))
                    starDNA.DefaultLibsDownloadedPath = Path.Combine("Libs", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultLibsInstalledPath))
                    starDNA.DefaultLibsInstalledPath = Path.Combine("Libs", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultChaptersSourcePath))
                    starDNA.DefaultChaptersSourcePath = Path.Combine("Chapters", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultChaptersPublishedPath))
                    starDNA.DefaultChaptersPublishedPath = Path.Combine("Chapters", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultChaptersDownloadedPath))
                    starDNA.DefaultChaptersDownloadedPath = Path.Combine("Chapters", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultChaptersInstalledPath))
                    starDNA.DefaultChaptersInstalledPath = Path.Combine("Chapters", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultMissionsSourcePath))
                    starDNA.DefaultMissionsSourcePath = Path.Combine("Missions", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultMissionsPublishedPath))
                    starDNA.DefaultMissionsPublishedPath = Path.Combine("Missions", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultMissionsDownloadedPath))
                    starDNA.DefaultMissionsDownloadedPath = Path.Combine("Missions", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultMissionsInstalledPath))
                    starDNA.DefaultMissionsInstalledPath = Path.Combine("Missions", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultQuestsSourcePath))
                    starDNA.DefaultQuestsSourcePath = Path.Combine("Quests", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultQuestsPublishedPath))
                    starDNA.DefaultQuestsPublishedPath = Path.Combine("Quests", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultQuestsDownloadedPath))
                    starDNA.DefaultQuestsDownloadedPath = Path.Combine("Quests", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultQuestsInstalledPath))
                    starDNA.DefaultQuestsInstalledPath = Path.Combine("Quests", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultGamesSourcePath))
                    starDNA.DefaultGamesSourcePath = Path.Combine("Games", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultGamesPublishedPath))
                    starDNA.DefaultGamesPublishedPath = Path.Combine("Games", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultGamesDownloadedPath))
                    starDNA.DefaultGamesDownloadedPath = Path.Combine("Games", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultGamesInstalledPath))
                    starDNA.DefaultGamesInstalledPath = Path.Combine("Games", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultNFTsSourcePath))
                    starDNA.DefaultNFTsSourcePath = Path.Combine("NFTs", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultNFTsPublishedPath))
                    starDNA.DefaultNFTsPublishedPath = Path.Combine("NFTs", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultNFTsDownloadedPath))
                    starDNA.DefaultNFTsDownloadedPath = Path.Combine("NFTs", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultNFTsInstalledPath))
                    starDNA.DefaultNFTsInstalledPath = Path.Combine("NFTs", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTsSourcePath))
                    starDNA.DefaultGeoNFTsSourcePath = Path.Combine("GeoNFTs", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTsPublishedPath))
                    starDNA.DefaultGeoNFTsPublishedPath = Path.Combine("GeoNFTs", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTsDownloadedPath))
                    starDNA.DefaultGeoNFTsDownloadedPath = Path.Combine("GeoNFTs", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTsInstalledPath))
                    starDNA.DefaultGeoNFTsInstalledPath = Path.Combine("GeoNFTs", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultNFTCollectionsSourcePath))
                    starDNA.DefaultNFTCollectionsSourcePath = Path.Combine("NFTCollections", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultNFTCollectionsPublishedPath))
                    starDNA.DefaultNFTCollectionsPublishedPath = Path.Combine("NFTCollections", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultNFTCollectionsDownloadedPath))
                    starDNA.DefaultNFTCollectionsDownloadedPath = Path.Combine("NFTCollections", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultNFTCollectionsInstalledPath))
                    starDNA.DefaultNFTCollectionsInstalledPath = Path.Combine("NFTCollections", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTCollectionsSourcePath))
                    starDNA.DefaultGeoNFTCollectionsSourcePath = Path.Combine("GeoNFTCollections", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTCollectionsPublishedPath))
                    starDNA.DefaultGeoNFTCollectionsPublishedPath = Path.Combine("GeoNFTCollections", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTCollectionsDownloadedPath))
                    starDNA.DefaultGeoNFTCollectionsDownloadedPath = Path.Combine("GeoNFTCollections", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTCollectionsInstalledPath))
                    starDNA.DefaultGeoNFTCollectionsInstalledPath = Path.Combine("GeoNFTCollections", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultGeoHotSpotsSourcePath))
                    starDNA.DefaultGeoHotSpotsSourcePath = Path.Combine("GeoHotSpots", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoHotSpotsPublishedPath))
                    starDNA.DefaultGeoHotSpotsPublishedPath = Path.Combine("GeoHotSpots", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoHotSpotsDownloadedPath))
                    starDNA.DefaultGeoHotSpotsDownloadedPath = Path.Combine("GeoHotSpots", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoHotSpotsInstalledPath))
                    starDNA.DefaultGeoHotSpotsInstalledPath = Path.Combine("GeoHotSpots", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultInventoryItemsSourcePath))
                    starDNA.DefaultInventoryItemsSourcePath = Path.Combine("InventoryItems", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultInventoryItemsPublishedPath))
                    starDNA.DefaultInventoryItemsPublishedPath = Path.Combine("InventoryItems", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultInventoryItemsDownloadedPath))
                    starDNA.DefaultInventoryItemsDownloadedPath = Path.Combine("InventoryItems", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultInventoryItemsInstalledPath))
                    starDNA.DefaultInventoryItemsInstalledPath = Path.Combine("InventoryItems", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultCelestialSpacesSourcePath))
                    starDNA.DefaultCelestialSpacesSourcePath = Path.Combine("CelestialSpaces", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialSpacesPublishedPath))
                    starDNA.DefaultCelestialSpacesPublishedPath = Path.Combine("CelestialSpaces", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialSpacesDownloadedPath))
                    starDNA.DefaultCelestialSpacesDownloadedPath = Path.Combine("CelestialSpaces", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialSpacesInstalledPath))
                    starDNA.DefaultCelestialSpacesInstalledPath = Path.Combine("CelestialSpaces", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesSourcePath))
                    starDNA.DefaultCelestialBodiesSourcePath = Path.Combine("CelestialBodies", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesPublishedPath))
                    starDNA.DefaultCelestialBodiesPublishedPath = Path.Combine("CelestialBodies", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesDownloadedPath))
                    starDNA.DefaultCelestialBodiesDownloadedPath = Path.Combine("CelestialBodies", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesInstalledPath))
                    starDNA.DefaultCelestialBodiesInstalledPath = Path.Combine("CelestialBodies", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultZomesSourcePath))
                    starDNA.DefaultZomesSourcePath = Path.Combine("Zomes", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultZomesPublishedPath))
                    starDNA.DefaultZomesPublishedPath = Path.Combine("Zomes", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultZomesDownloadedPath))
                    starDNA.DefaultZomesDownloadedPath = Path.Combine("Zomes", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultZomesInstalledPath))
                    starDNA.DefaultZomesInstalledPath = Path.Combine("Zomes", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultHolonsSourcePath))
                    starDNA.DefaultHolonsSourcePath = Path.Combine("Holons", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultHolonsPublishedPath))
                    starDNA.DefaultHolonsPublishedPath = Path.Combine("Holons", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultHolonsDownloadedPath))
                    starDNA.DefaultHolonsDownloadedPath = Path.Combine("Holons", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultHolonsInstalledPath))
                    starDNA.DefaultHolonsInstalledPath = Path.Combine("Holons", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesMetaDataDNASourcePath))
                    starDNA.DefaultCelestialBodiesMetaDataDNASourcePath = Path.Combine("CelestialBodies", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesMetaDataDNAPublishedPath))
                    starDNA.DefaultCelestialBodiesMetaDataDNAPublishedPath = Path.Combine("CelestialBodies", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesMetaDataDNADownloadedPath))
                    starDNA.DefaultCelestialBodiesMetaDataDNADownloadedPath = Path.Combine("CelestialBodies", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesMetaDataDNAInstalledPath))
                    starDNA.DefaultCelestialBodiesMetaDataDNAInstalledPath = Path.Combine("CelestialBodies", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultZomesMetaDataDNASourcePath))
                    starDNA.DefaultZomesMetaDataDNASourcePath = Path.Combine("Zomes", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultZomesMetaDataDNAPublishedPath))
                    starDNA.DefaultZomesMetaDataDNAPublishedPath = Path.Combine("Zomes", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultZomesMetaDataDNADownloadedPath))
                    starDNA.DefaultZomesMetaDataDNADownloadedPath = Path.Combine("Zomes", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultZomesMetaDataDNAInstalledPath))
                    starDNA.DefaultZomesMetaDataDNAInstalledPath = Path.Combine("Zomes", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultHolonsMetaDataDNASourcePath))
                    starDNA.DefaultHolonsMetaDataDNASourcePath = Path.Combine("Holons", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultHolonsMetaDataDNAPublishedPath))
                    starDNA.DefaultHolonsMetaDataDNAPublishedPath = Path.Combine("Holons", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultHolonsMetaDataDNADownloadedPath))
                    starDNA.DefaultHolonsMetaDataDNADownloadedPath = Path.Combine("Holons", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultHolonsMetaDataDNAInstalledPath))
                    starDNA.DefaultHolonsMetaDataDNAInstalledPath = Path.Combine("Holons", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultPluginsSourcePath))
                    starDNA.DefaultPluginsSourcePath = Path.Combine("Plugins", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultPluginsPublishedPath))
                    starDNA.DefaultPluginsPublishedPath = Path.Combine("Plugins", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultPluginsDownloadedPath))
                    starDNA.DefaultPluginsDownloadedPath = Path.Combine("Plugins", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultPluginsInstalledPath))
                    starDNA.DefaultPluginsInstalledPath = Path.Combine("Plugins", "Installed");

                STARDNAManager.SaveDNA(STARDNAPath, STARDNA);

                ValidateFolder("", starDNA.STARNETBasePath, "STARDNA.STARNETBasePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPsSourcePath, "STARDNA.DefaultOAPPsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPsPublishedPath, "STARDNA.DefaultOAPPsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPsDownloadedPath, "STARDNA.DefaultOAPPsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPsInstalledPath, "STARDNA.DefaultOAPPsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPTemplatesSourcePath, "STARDNA.DefaultOAPPTemplatesSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPTemplatesPublishedPath, "STARDNA.DefaultOAPPTemplatesPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPTemplatesDownloadedPath, "STARDNA.DefaultOAPPTemplatesDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPTemplatesInstalledPath, "STARDNA.DefaultOAPPTemplatesInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultRuntimesSourcePath, "STARDNA.DefaultRuntimesSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultRuntimesPublishedPath, "STARDNA.DefaultRuntimesPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultRuntimesDownloadedPath, "STARDNA.DefaultRuntimesDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultRuntimesInstalledPath, "STARDNA.DefaultRuntimesInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultRuntimesInstalledOASISPath, "STARDNA.DefaultRuntimesInstalledOASISPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultRuntimesInstalledSTARPath, "STARDNA.DefaultRuntimesInstalledSTARPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultLibsSourcePath, "STARDNA.DefaultLibsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultLibsPublishedPath, "STARDNA.DefaultLibsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultLibsDownloadedPath, "STARDNA.DefaultLibsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultLibsInstalledPath, "STARDNA.DefaultLibsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultChaptersSourcePath, "STARDNA.DefaultChaptersSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultChaptersPublishedPath, "STARDNA.DefaultChaptersPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultChaptersDownloadedPath, "STARDNA.DefaultChaptersDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultChaptersInstalledPath, "STARDNA.DefaultChaptersInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultMissionsSourcePath, "STARDNA.DefaultMissionsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultMissionsPublishedPath, "STARDNA.DefaultMissionsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultMissionsDownloadedPath, "STARDNA.DefaultMissionsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultMissionsInstalledPath, "STARDNA.DefaultMissionsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultQuestsSourcePath, "STARDNA.DefaultQuestsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultQuestsPublishedPath, "STARDNA.DefaultQuestsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultQuestsDownloadedPath, "STARDNA.DefaultQuestsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultQuestsInstalledPath, "STARDNA.DefaultQuestsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGamesSourcePath, "STARDNA.DefaultGamesSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGamesPublishedPath, "STARDNA.DefaultGamesPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGamesDownloadedPath, "STARDNA.DefaultGamesDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGamesInstalledPath, "STARDNA.DefaultGamesInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTsSourcePath, "STARDNA.DefaultNFTsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTsPublishedPath, "STARDNA.DefaultNFTsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTsDownloadedPath, "STARDNA.DefaultNFTsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTsInstalledPath, "STARDNA.DefaultNFTsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTsSourcePath, "STARDNA.DefaultGeoNFTsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTsPublishedPath, "STARDNA.DefaultGeoNFTsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTsDownloadedPath, "STARDNA.DefaultGeoNFTsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTsInstalledPath, "STARDNA.DefaultGeoNFTsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTCollectionsSourcePath, "STARDNA.DefaultNFTCollectionsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTCollectionsPublishedPath, "STARDNA.DefaultNFTCollectionsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTCollectionsDownloadedPath, "STARDNA.DefaultNFTCollectionsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTCollectionsInstalledPath, "STARDNA.DefaultNFTCollectionsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTCollectionsSourcePath, "STARDNA.DefaultGeoNFTCollectionsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTCollectionsPublishedPath, "STARDNA.DefaultGeoNFTCollectionsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTCollectionsDownloadedPath, "STARDNA.DefaultGeoNFTCollectionsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTCollectionsInstalledPath, "STARDNA.DefaultGeoNFTCollectionsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoHotSpotsSourcePath, "STARDNA.DefaultGeoHotSpotsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoHotSpotsPublishedPath, "STARDNA.DefaultGeoHotSpotsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoHotSpotsDownloadedPath, "STARDNA.DefaultGeoHotSpotsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoHotSpotsInstalledPath, "STARDNA.DefaultGeoHotSpotsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultInventoryItemsSourcePath, "STARDNA.DefaultInventoryItemsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultInventoryItemsPublishedPath, "STARDNA.DefaultInventoryItemsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultInventoryItemsDownloadedPath, "STARDNA.DefaultInventoryItemsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultInventoryItemsInstalledPath, "STARDNA.DefaultInventoryItemsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialSpacesSourcePath, "STARDNA.DefaultCelestialSpacesSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialSpacesPublishedPath, "STARDNA.DefaultCelestialSpacesPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialSpacesDownloadedPath, "STARDNA.DefaultCelestialSpacesDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialSpacesInstalledPath, "STARDNA.DefaultCelestialSpacesInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesSourcePath, "STARDNA.DefaultCelestialBodiesSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesPublishedPath, "STARDNA.DefaultCelestialBodiesPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesDownloadedPath, "STARDNA.DefaultCelestialBodiesDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesInstalledPath, "STARDNA.DefaultCelestialBodiesInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesSourcePath, "STARDNA.DefaultZomesSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesPublishedPath, "STARDNA.DefaultZomesPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesDownloadedPath, "STARDNA.DefaultZomesDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesInstalledPath, "STARDNA.DefaultZomesInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsSourcePath, "STARDNA.DefaultHolonsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsPublishedPath, "STARDNA.DefaultHolonsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsDownloadedPath, "STARDNA.DefaultHolonsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsInstalledPath, "STARDNA.DefaultHolonsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesMetaDataDNASourcePath, "STARDNA.DefaultCelestialBodiesMetaDataDNASourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesMetaDataDNAPublishedPath, "STARDNA.DefaultCelestialBodiesMetaDataDNAPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesMetaDataDNADownloadedPath, "STARDNA.DefaultCelestialBodiesMetaDataDNADownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesMetaDataDNAInstalledPath, "STARDNA.DefaultCelestialBodiesMetaDataDNAInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesMetaDataDNASourcePath, "STARDNA.DefaultZomesMetaDataDNASourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesMetaDataDNAPublishedPath, "STARDNA.DefaultZomesMetaDataDNAPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesMetaDataDNADownloadedPath, "STARDNA.DefaultZomesMetaDataDNADownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesMetaDataDNAInstalledPath, "STARDNA.DefaultZomesMetaDataDNAInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsMetaDataDNASourcePath, "STARDNA.DefaultHolonsMetaDataDNASourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsMetaDataDNAPublishedPath, "STARDNA.DefaultHolonsMetaDataDNAPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsMetaDataDNADownloadedPath, "STARDNA.DefaultHolonsMetaDataDNADownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsMetaDataDNAInstalledPath, "STARDNA.DefaultHolonsMetaDataDNAInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultPluginsSourcePath, "STARDNA.DefaultPluginsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultPluginsPublishedPath, "STARDNA.DefaultPluginsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultPluginsDownloadedPath, "STARDNA.DefaultPluginsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultPluginsInstalledPath, "STARDNA.DefaultPluginsInstalledPath", false, true);
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
            //string path = string.IsNullOrEmpty(basePath) ? folder : $"{basePath}\\{folder}";
            string path = string.IsNullOrEmpty(basePath) ? folder : Path.Combine(basePath, folder);

            if (Path.IsPathRooted(folder))
                path = folder; //If the folder is rooted, use it as is.

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
            //string path = $"{basePath}\\{folder}";
            string path = Path.Combine(basePath, folder);

            if (string.IsNullOrEmpty(file))
                throw new ArgumentNullException(fileParam, string.Concat("The ", fileParam, " param in the STARDNA is null, please double check and try again."));

            //if (!File.Exists(string.Concat(path, "\\", file)))
            if (!File.Exists(Path.Combine(path, file)))
                throw new FileNotFoundException(string.Concat("The ", fileParam, " file is not valid, the file does not exist, please double check and try again."), Path.Combine(path, file));
        }

        //private static STARDNA LoadDNA()
        //{
        //    using (StreamReader r = new StreamReader(STARDNAPath))
        //    {
        //        string json = r.ReadToEnd();
        //        STARDNA = JsonConvert.DeserializeObject<STARDNA> (json);
        //        return STARDNA;
        //    }
        //}
        //private static bool SaveDNA()
        //{
        //    try
        //    {
        //        string json = JsonConvert.SerializeObject(STARDNA);

        //        if (!Directory.Exists(Path.GetDirectoryName(STARDNAPath)))
        //            Directory.CreateDirectory(Path.GetDirectoryName(STARDNAPath));

        //        StreamWriter writer = new StreamWriter(STARDNAPath);
        //        writer.Write(json);
        //        writer.Close();
        //    }
        //    catch (Exception e)
        //    {
                
        //    }

        //    return true;
        //}

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

        // GenerateRustField method moved to HoloOASIS.NativeCodeGenesis

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

    }
}
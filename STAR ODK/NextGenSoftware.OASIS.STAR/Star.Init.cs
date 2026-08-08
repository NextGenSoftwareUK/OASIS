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
        public static string STARDNAPath { get; set; } = STAR_DNA_DEFAULT_PATH;
        public static string OASISDNAPath { get; set; } = OASIS_DNA_DEFAULT_PATH;

        /// <summary>
        /// Before loading STAR DNA, copy OS-specific templates onto the base files STAR expects.
        /// <list type="bullet">
        /// <item><description><c>STAR_DNA.Windows.json</c> → <c>STAR_DNA.json</c> (Windows)</description></item>
        /// <item><description><c>STAR_DNA.Linux.json</c> → <c>STAR_DNA.json</c> (Linux)</description></item>
        /// <item><description><c>STAR_DNA.OSX.json</c> then <c>STAR_DNA.Linux.json</c> (macOS)</description></item>
        /// <item><description>Same pattern for optional <c>STARDNA.json</c> in the same folder (C# uses pre-build copy of <c>STARDNA.*.cs</c> → <c>STARDNA.cs</c>, not runtime)</description></item>
        /// </list>
        /// Set environment variable <c>OASIS_SKIP_PLATFORM_DNA=1</c> to skip.
        /// </summary>
        public static void ApplyPlatformSpecificDnaFiles(string starDnaPath)
        {
            if (string.IsNullOrWhiteSpace(starDnaPath))
                return;
            string skip = Environment.GetEnvironmentVariable("OASIS_SKIP_PLATFORM_DNA");
            if (!string.IsNullOrEmpty(skip) && (skip == "1" || skip.Equals("true", StringComparison.OrdinalIgnoreCase)))
                return;

            string fullStarPath = AppPathHelper.ResolvePathFromAppRoot(starDnaPath);
            string dnaDir = Path.GetDirectoryName(fullStarPath);
            if (string.IsNullOrEmpty(dnaDir))
                dnaDir = AppPathHelper.ResolveAppRootDirectory();

            // Optional safety net to keep platform-specific DNA in sync (prevents accidentally reintroducing Windows-only defaults)
            // Enable with: OASIS_VALIDATE_PLATFORM_DNA_SYNC=1 (or true)
            string validateEnv = Environment.GetEnvironmentVariable("OASIS_VALIDATE_PLATFORM_DNA_SYNC");
            if (!string.IsNullOrWhiteSpace(validateEnv) &&
                (validateEnv == "1" || validateEnv.Equals("true", StringComparison.OrdinalIgnoreCase)))
            {
                // Non-fatal validation; warnings are logged via OASISErrorHandling.
                ValidatePlatformDnaFilesSync(fullStarPath);
            }

            string[] suffixes;
            if (OperatingSystem.IsWindows())
                suffixes = new[] { "Windows" };
            else if (OperatingSystem.IsMacOS())
                suffixes = new[] { "OSX", "Linux" };
            else
                suffixes = new[] { "Linux" };

            static bool TryCopy(string dir, string baseName, string extDot, string[] suf)
            {
                foreach (string s in suf)
                {
                    string src = Path.Combine(dir, string.Concat(baseName, ".", s, extDot));
                    if (!File.Exists(src))
                        continue;
                    string dst = Path.Combine(dir, string.Concat(baseName, extDot));
                    try
                    {
                        File.Copy(src, dst, overwrite: true);
                    }
                    catch
                    {
                        // Non-fatal: continue boot with existing base file
                    }
                    return true;
                }
                return false;
            }

            string mainBase = Path.GetFileNameWithoutExtension(fullStarPath);
            string mainExt = Path.GetExtension(fullStarPath);
            TryCopy(dnaDir, mainBase, mainExt, suffixes);

            if (!string.Equals(mainBase, "STARDNA", StringComparison.OrdinalIgnoreCase) || !string.Equals(mainExt, ".json", StringComparison.OrdinalIgnoreCase))
                TryCopy(dnaDir, "STARDNA", ".json", suffixes);
        }

        /// <summary>
        /// Validates that platform-specific STAR DNA files (e.g. STAR_DNA.Linux.json) are in sync with the expected schema:
        /// - Required keys exist: STARBasePath / STARNETBasePath
        /// - On Linux/macOS, required path strings do not contain Windows drive/path separators.
        ///
        /// This is a lightweight check meant to prevent accidental drift while still allowing platform-specific overrides.
        /// </summary>
        public static OASISResult<bool> ValidatePlatformDnaFilesSync(string starDnaPath)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            result.Result = true;

            if (string.IsNullOrWhiteSpace(starDnaPath))
            {
                OASISErrorHandling.HandleWarning(ref result, "ValidatePlatformDnaFilesSync skipped: starDnaPath is null/empty.");
                return result;
            }

            try
            {
                string fullStarPath = AppPathHelper.ResolvePathFromAppRoot(starDnaPath);

                string dnaDir = Path.GetDirectoryName(fullStarPath);
                if (string.IsNullOrEmpty(dnaDir))
                    dnaDir = AppPathHelper.ResolveAppRootDirectory();

                string mainBase = Path.GetFileNameWithoutExtension(fullStarPath);
                string mainExt = Path.GetExtension(fullStarPath); // includes dot (e.g. ".json")
                if (string.IsNullOrEmpty(mainExt))
                    mainExt = ".json";

                // All path-related keys from STAR DNA schema (for schema drift and accidental Windows path check on non-Windows).
                string[] requiredKeys = new[]
                {
                    "STARBasePath",
                    "STARNETBasePath",
                    "MetaDataDNATemplateFolder",
                    "CSharpDNATemplateFolder",
                    "CSharpDNATemplateNamespace",
                    "OAPPMetaDataDNAFolder",
                    "ZomeMetaDataDNA",
                    "HolonMetaDataDNA",
                    "DefaultGenesisNamespace",
                    "OAPPGeneratedCodeFolder",
                    "CSharpTemplateIHolonDNA",
                    "CSharpTemplateHolonDNA",
                    "CSharpTemplateIZomeDNA",
                    "CSharpTemplateZomeDNA",
                    "CSharpTemplateICelestialBodyDNA",
                    "CSharpTemplateCelestialBodyDNA",
                    "CSharpTemplateLoadHolonDNA",
                    "CSharpTemplateSaveHolonDNA",
                    "CSharpTemplateILoadHolonDNA",
                    "CSharpTemplateISaveHolonDNA",
                    "CSharpTemplateInt",
                    "CSharpTemplateString",
                    "CSharpTemplateBool",
                    "DefaultOAPPsSourcePath",
                    "DefaultOAPPsPublishedPath",
                    "DefaultOAPPsDownloadedPath",
                    "DefaultOAPPsInstalledPath",
                    "DefaultOAPPTemplatesSourcePath",
                    "DefaultOAPPTemplatesPublishedPath",
                    "DefaultOAPPTemplatesDownloadedPath",
                    "DefaultOAPPTemplatesInstalledPath",
                    "DefaultRuntimesSourcePath",
                    "DefaultRuntimesPublishedPath",
                    "DefaultRuntimesDownloadedPath",
                    "DefaultRuntimesInstalledPath",
                    "DefaultRuntimesInstalledOASISPath",
                    "DefaultRuntimesInstalledSTARPath",
                    "DefaultLibsSourcePath",
                    "DefaultLibsPublishedPath",
                    "DefaultLibsDownloadedPath",
                    "DefaultLibsInstalledPath",
                    "DefaultChaptersSourcePath",
                    "DefaultChaptersPublishedPath",
                    "DefaultChaptersDownloadedPath",
                    "DefaultChaptersInstalledPath",
                    "DefaultMissionsSourcePath",
                    "DefaultMissionsPublishedPath",
                    "DefaultMissionsDownloadedPath",
                    "DefaultMissionsInstalledPath",
                    "DefaultQuestsSourcePath",
                    "DefaultQuestsPublishedPath",
                    "DefaultQuestsDownloadedPath",
                    "DefaultQuestsInstalledPath",
                    "DefaultGamesSourcePath",
                    "DefaultGamesPublishedPath",
                    "DefaultGamesDownloadedPath",
                    "DefaultGamesInstalledPath",
                    "DefaultNFTsSourcePath",
                    "DefaultNFTsPublishedPath",
                    "DefaultNFTsDownloadedPath",
                    "DefaultNFTsInstalledPath",
                    "DefaultGeoNFTsSourcePath",
                    "DefaultGeoNFTsPublishedPath",
                    "DefaultGeoNFTsDownloadedPath",
                    "DefaultGeoNFTsInstalledPath",
                    "DefaultNFTCollectionsSourcePath",
                    "DefaultNFTCollectionsPublishedPath",
                    "DefaultNFTCollectionsDownloadedPath",
                    "DefaultNFTCollectionsInstalledPath",
                    "DefaultGeoNFTCollectionsSourcePath",
                    "DefaultGeoNFTCollectionsPublishedPath",
                    "DefaultGeoNFTCollectionsDownloadedPath",
                    "DefaultGeoNFTCollectionsInstalledPath",
                    "DefaultGeoHotSpotsSourcePath",
                    "DefaultGeoHotSpotsPublishedPath",
                    "DefaultGeoHotSpotsDownloadedPath",
                    "DefaultGeoHotSpotsInstalledPath",
                    "DefaultInventoryItemsSourcePath",
                    "DefaultInventoryItemsPublishedPath",
                    "DefaultInventoryItemsDownloadedPath",
                    "DefaultInventoryItemsInstalledPath",
                    "DefaultCelestialSpacesSourcePath",
                    "DefaultCelestialSpacesPublishedPath",
                    "DefaultCelestialSpacesDownloadedPath",
                    "DefaultCelestialSpacesInstalledPath",
                    "DefaultCelestialBodiesSourcePath",
                    "DefaultCelestialBodiesPublishedPath",
                    "DefaultCelestialBodiesDownloadedPath",
                    "DefaultCelestialBodiesInstalledPath",
                    "DefaultZomesSourcePath",
                    "DefaultZomesPublishedPath",
                    "DefaultZomesDownloadedPath",
                    "DefaultZomesInstalledPath",
                    "DefaultHolonsSourcePath",
                    "DefaultHolonsPublishedPath",
                    "DefaultHolonsDownloadedPath",
                    "DefaultHolonsInstalledPath",
                    "DefaultCelestialBodiesMetaDataDNASourcePath",
                    "DefaultCelestialBodiesMetaDataDNAPublishedPath",
                    "DefaultCelestialBodiesMetaDataDNADownloadedPath",
                    "DefaultCelestialBodiesMetaDataDNAInstalledPath",
                    "DefaultZomesMetaDataDNASourcePath",
                    "DefaultZomesMetaDataDNAPublishedPath",
                    "DefaultZomesMetaDataDNADownloadedPath",
                    "DefaultZomesMetaDataDNAInstalledPath",
                    "DefaultHolonsMetaDataDNASourcePath",
                    "DefaultHolonsMetaDataDNAPublishedPath",
                    "DefaultHolonsMetaDataDNADownloadedPath",
                    "DefaultHolonsMetaDataDNAInstalledPath",
                    "DefaultPluginsSourcePath",
                    "DefaultPluginsPublishedPath",
                    "DefaultPluginsDownloadedPath",
                    "DefaultPluginsInstalledPath"
                };

                string[] platformSuffixes = new[] { "Windows", "Linux", "OSX" };
                foreach (string suffix in platformSuffixes)
                {
                    string platformFile = Path.Combine(dnaDir, string.Concat(mainBase, ".", suffix, mainExt));
                    if (!File.Exists(platformFile))
                        continue;

                    string json = File.ReadAllText(platformFile);

                    foreach (string key in requiredKeys)
                    {
                        var valueResult = TryExtractJsonStringValue(json, key);
                        if (valueResult.IsWarning || valueResult.Result == null)
                        {
                            OASISErrorHandling.HandleWarning(
                                ref result,
                                $"Platform DNA key drift detected in '{platformFile}': missing required key '{key}'.");
                            continue;
                        }

                        string value = valueResult.Result;

                        // For Linux/macOS, ensure we don't accidentally embed Windows path separators/drive letters
                        // in the required path-related values.
                        if (!string.Equals(suffix, "Windows", StringComparison.OrdinalIgnoreCase))
                        {
                            if (value.Contains(':') || value.Contains("\\"))
                            {
                                OASISErrorHandling.HandleWarning(
                                    ref result,
                                    $"Platform DNA path drift detected in '{platformFile}': key '{key}' contains Windows-style separators or drive letters ('{value}').");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"ValidatePlatformDnaFilesSync failed. Reason: {ex.Message}");
            }

            return result;
        }

        private static OASISResult<string> TryExtractJsonStringValue(string json, string key)
        {
            OASISResult<string> result = new OASISResult<string>();

            // NOTE: STAR DNA JSON uses // comments. Since we only need lightweight validation,
            // this regex is good enough for extracting string values from present keys.
            // It intentionally does not attempt to fully parse JSON.
            string pattern = $"\"{Regex.Escape(key)}\"\\s*:\\s*\"(?<val>[^\"]*)\"";
            Match match = Regex.Match(json, pattern);
            if (!match.Success)
            {
                result.IsWarning = true;
                result.Message = $"Key '{key}' not found.";
                return result;
            }

            result.Result = match.Groups["val"].Value;
            return result;
        }

        public static STARDNA STARDNA
        {
            get
            {
                return STARDNAManager.STARDNA;
            }
            set
            {
                STARDNAManager.STARDNA = value;
            }
        }

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
        public static bool IsDetailedCOSMICOutputsEnabled { get; set; } = false;
        public static bool IsDetailedStatusUpdatesEnabled { get; set; }

        //public static GreatGrandSuperStar InnerStar { get; set; } //Only ONE of these can ever exist and is at the centre of the Omniverse (also only ONE).

        //Will default to the GreatGrandSuperStar at the centre of our Omniverse.
        public static IGreatGrandSuperStar DefaultGreatGrandSuperStar
        {
            get
            {
                //return _defaultGreatGrandSuperStar;

                if (_defaultGreatGrandSuperStar == null)
                {
                    if (STARDNA != null && !string.IsNullOrEmpty(STARDNA.DefaultGreatGrandSuperStarId) && Guid.TryParse(STARDNA.DefaultGreatGrandSuperStarId, out _))
                        _defaultGreatGrandSuperStar = new GreatGrandSuperStar(new Guid(STARDNA.DefaultGreatGrandSuperStarId));
                }

                return _defaultGreatGrandSuperStar;
            }
            set
            {
                _defaultGreatGrandSuperStar = value;

                //if (_defaultGreatGrandSuperStar == null)
                //{
                //    if (STARDNA != null && !string.IsNullOrEmpty(STARDNA.DefaultGreatGrandSuperStarId) && Guid.TryParse(STARDNA.DefaultGreatGrandSuperStarId, out _))
                //        _defaultGreatGrandSuperStar = new GreatGrandSuperStar(new Guid(STARDNA.DefaultGreatGrandSuperStarId));
                //}
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

        public static IAvatar BeamedInAvatar { get; set; }
        public static IAvatarDetail BeamedInAvatarDetail { get; set; }

        //public static OASISAPI OASISAPI
        //{
        //    get
        //    {
        //        if (_OASISAPI == null)
        //            _OASISAPI = new OASISAPI();

        //        return _OASISAPI;
        //    }
        //}

        public static OASISAPI OASISAPI
        {
            get
            {
                if (_OASISAPI == null)
                    _OASISAPI = new OASISAPI();

                return _OASISAPI;
            }
        }

        public static STARAPI STARAPI
        {
            get
            {
                if (_STARAPI == null)
                    _STARAPI = new STARAPI(STARDNA, OASISAPI);

                return _STARAPI;
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

        public delegate void DefaultCeletialBodyInit(object sender, DefaultCelestialBodyInitEventArgs e);
        public static event DefaultCeletialBodyInit OnDefaultCeletialBodyInit;

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

    }
}
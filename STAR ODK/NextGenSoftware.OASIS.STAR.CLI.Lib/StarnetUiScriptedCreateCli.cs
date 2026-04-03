using System;
using System.Collections.Generic;
using System.Globalization;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    /// <summary>
    /// Single place for argv → <see cref="NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.ISTARNETCreateOptions{T1,T2}.CustomCreateParams"/>
    /// used by <see cref="STARNETUIBase{T1,T2,T3,T4}.CreateAsync"/>'s scripted non-interactive branch.
    /// Keeps holon-specific knowledge out of <c>Program.cs</c>; new holons that delegate to <c>base.CreateAsync</c> usually need no CLI changes.
    /// </summary>
    public static class StarnetUiScriptedCreateCli
    {
        /// <summary>
        /// <see cref="STARNETUIBase{T1,T2,T3,T4}.CreateAsync"/> handles scripted create when <see cref="StarCliNonInteractiveCreateKeys.Scripted"/> is set.
        /// Holon managers that <strong>fully override</strong> <c>CreateAsync</c> and never reach that base path (or prompt before it) must be listed in
        /// <see cref="HolonSubCommandLabelsThatBypassBaseScriptedCreate"/> so the STAR CLI errors instead of blocking on stdin.
        /// <c>plugin</c> uses <c>Plugins.CreateAsync</c> scripted argv (not the STARNETUIBase generic path). <c>OAPP</c>/<c>hApp</c>: scripted <c>create</c> delegates to <c>base.CreateAsync</c>; <c>create light &lt;json&gt;</c> uses <see cref="StarCliLightRequest"/> via <see cref="StarCliNonInteractiveCreateKeys.LightRequestJsonPath"/>.
        /// </summary>
        private static readonly string[] HolonSubCommandLabelsThatBypassBaseScriptedCreate = System.Array.Empty<string>();

        /// <summary>
        /// True when this <paramref name="holonSubCommandLabel"/> (the same string passed to <c>ShowSubCommandAsync</c> as <c>subCommand</c>)
        /// does not support scripted argv create yet.
        /// </summary>
        public static bool HolonLabelBypassesBaseScriptedCreate(string holonSubCommandLabel)
        {
            if (string.IsNullOrEmpty(holonSubCommandLabel))
                return false;

            if (string.Equals(holonSubCommandLabel, "nft", StringComparison.OrdinalIgnoreCase)
                || string.Equals(holonSubCommandLabel, "geo-nft", StringComparison.OrdinalIgnoreCase)
                || string.Equals(holonSubCommandLabel, "geonft", StringComparison.OrdinalIgnoreCase))
            {
                // Scripted wrap-only create is handled in Program + NFTs / GeoNFTs.
            }
            else if (holonSubCommandLabel.IndexOf("NFT", StringComparison.OrdinalIgnoreCase) >= 0
                     && !string.Equals(holonSubCommandLabel, "nft collection", StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(holonSubCommandLabel, "geo-nft collection", StringComparison.OrdinalIgnoreCase))
                return true;

            for (int i = 0; i < HolonSubCommandLabelsThatBypassBaseScriptedCreate.Length; i++)
            {
                if (string.Equals(holonSubCommandLabel, HolonSubCommandLabelsThatBypassBaseScriptedCreate[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary><c>plugin create &lt;name&gt; &lt;description&gt; [parentFolder]</c> (same optional template/metadata/collection shift as other holons).</summary>
        public static bool TryParsePluginCreateArgv(string[] inputArgs, out string name, out string description, out string parentFolder, out string errorMessage)
        {
            name = description = parentFolder = null;
            errorMessage = null;

            if (inputArgs == null || inputArgs.Length < 2)
            {
                errorMessage = "Not enough arguments for plugin create.";
                return false;
            }

            bool shifted = inputArgs.Length > 1 &&
                (string.Equals(inputArgs[1], "template", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(inputArgs[1], "metadata", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(inputArgs[1], "collection", StringComparison.OrdinalIgnoreCase));

            int createIndex = shifted ? 2 : 1;
            if (inputArgs.Length <= createIndex || !string.Equals(inputArgs[createIndex], "create", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Expected subcommand 'create'.";
                return false;
            }

            int nameIndex = createIndex + 1;
            if (inputArgs.Length < nameIndex + 2)
            {
                errorMessage = "Non-interactive plugin create requires: create <name> <description> [parentFolder]";
                return false;
            }

            name = inputArgs[nameIndex]?.Trim();
            description = inputArgs[nameIndex + 1]?.Trim() ?? "";
            if (inputArgs.Length > nameIndex + 2)
                parentFolder = inputArgs[nameIndex + 2]?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                errorMessage = "Plugin name must be non-empty.";
                return false;
            }

            return true;
        }

        public static Dictionary<string, object> BuildPluginScriptedCustomCreateParams(string name, string description, string parentFolder)
        {
            var d = new Dictionary<string, object>
            {
                [StarCliNonInteractiveCreateKeys.Scripted] = true,
                [StarCliNonInteractiveCreateKeys.Name] = name,
                [StarCliNonInteractiveCreateKeys.Description] = description ?? "",
                [StarCliNonInteractiveCreateKeys.SubType] = "Plugin"
            };

            if (!string.IsNullOrWhiteSpace(parentFolder))
                d[StarCliNonInteractiveCreateKeys.ParentFolder] = parentFolder;

            return d;
        }

        /// <summary>
        /// Same argv layout as <c>ShowSubCommandAsync</c> (optional <c>template</c>/<c>metadata</c>/<c>collection</c> prefix).
        /// Default: <c>create &lt;name&gt; &lt;description&gt; &lt;categoryEnum&gt; [parentFolder]</c>.
        /// <paramref name="holonSubCommandLabel"/> <c>library</c>: <c>create &lt;name&gt; &lt;description&gt; &lt;categoryEnum&gt; &lt;languageEnum&gt; [parentFolder]</c> (<see cref="Languages"/>).
        /// </summary>
        public static bool TryParseCreateArgv(string[] inputArgs, string holonSubCommandLabel, out string name, out string description, out string categoryToken, out string libraryLanguageToken, out string parentFolder, out string errorMessage)
        {
            name = description = categoryToken = libraryLanguageToken = parentFolder = null;
            errorMessage = null;

            if (inputArgs == null || inputArgs.Length < 2)
            {
                errorMessage = "Not enough arguments for create.";
                return false;
            }

            bool shifted = inputArgs.Length > 1 &&
                (string.Equals(inputArgs[1], "template", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(inputArgs[1], "metadata", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(inputArgs[1], "collection", StringComparison.OrdinalIgnoreCase));

            int createIndex = shifted ? 2 : 1;
            if (inputArgs.Length <= createIndex || !string.Equals(inputArgs[createIndex], "create", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Expected subcommand 'create'.";
                return false;
            }

            int nameIndex = createIndex + 1;
            bool isLibrary = string.Equals(holonSubCommandLabel, "library", StringComparison.OrdinalIgnoreCase);
            int minAfterName = isLibrary ? 4 : 3;
            if (inputArgs.Length < nameIndex + minAfterName)
            {
                errorMessage = isLibrary
                    ? "Non-interactive library create requires: create <name> <description> <categoryEnum> <languageEnum> [parentFolder]"
                    : "Non-interactive create requires: create <name> <description> <categoryEnum> [parentFolder]";
                return false;
            }

            name = inputArgs[nameIndex]?.Trim();
            description = inputArgs[nameIndex + 1]?.Trim() ?? "";
            categoryToken = inputArgs[nameIndex + 2]?.Trim();

            int parentArgIndex;
            if (isLibrary)
            {
                libraryLanguageToken = inputArgs[nameIndex + 3]?.Trim();
                if (string.IsNullOrEmpty(libraryLanguageToken))
                {
                    errorMessage = "library create requires a non-empty languageEnum (Languages).";
                    return false;
                }

                if (!Enum.TryParse<Languages>(libraryLanguageToken, ignoreCase: true, out _))
                {
                    errorMessage = $"Invalid Languages value '{libraryLanguageToken}'.";
                    return false;
                }

                parentArgIndex = nameIndex + 4;
            }
            else
            {
                libraryLanguageToken = null;
                parentArgIndex = nameIndex + 3;
            }

            if (inputArgs.Length > parentArgIndex)
                parentFolder = inputArgs[parentArgIndex]?.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(categoryToken))
            {
                errorMessage = "Name and categoryEnum must be non-empty.";
                return false;
            }

            return true;
        }

        public static Dictionary<string, object> BuildScriptedCustomCreateParams(string name, string description, string categoryToken, string parentFolder, string libraryLanguageToken = null)
        {
            var d = new Dictionary<string, object>
            {
                [StarCliNonInteractiveCreateKeys.Scripted] = true,
                [StarCliNonInteractiveCreateKeys.Name] = name,
                [StarCliNonInteractiveCreateKeys.Description] = description ?? "",
                [StarCliNonInteractiveCreateKeys.SubType] = categoryToken
            };

            if (!string.IsNullOrWhiteSpace(parentFolder))
                d[StarCliNonInteractiveCreateKeys.ParentFolder] = parentFolder;

            if (!string.IsNullOrWhiteSpace(libraryLanguageToken)
                && Enum.TryParse<Languages>(libraryLanguageToken, ignoreCase: true, out Languages lang))
                d[StarCliNonInteractiveCreateKeys.LibraryStarnetSubCategory] = lang;

            return d;
        }

        /// <summary>
        /// <c>geo-hotspot create &lt;name&gt; &lt;description&gt; &lt;GeoHotSpotType&gt; &lt;lat&gt; &lt;long&gt; &lt;radiusMetres&gt; &lt;GeoHotSpotTriggeredType&gt; [timeSeconds] [parentFolder]</c>
        /// </summary>
        public static bool TryParseGeoHotSpotCreateArgv(string[] inputArgs, out string name, out string description, out string geoHotSpotTypeToken, out double lat, out double lon, out int radiusM, out string triggerToken, out int? timeSeconds, out string parentFolder, out string errorMessage)
        {
            name = description = geoHotSpotTypeToken = triggerToken = parentFolder = null;
            lat = lon = 0;
            radiusM = 0;
            timeSeconds = null;
            errorMessage = null;

            if (inputArgs == null || inputArgs.Length < 2)
            {
                errorMessage = "Not enough arguments for geo-hotspot create.";
                return false;
            }

            bool shifted = inputArgs.Length > 1 &&
                (string.Equals(inputArgs[1], "template", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(inputArgs[1], "metadata", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(inputArgs[1], "collection", StringComparison.OrdinalIgnoreCase));

            int createIndex = shifted ? 2 : 1;
            if (inputArgs.Length <= createIndex || !string.Equals(inputArgs[createIndex], "create", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Expected subcommand 'create'.";
                return false;
            }

            int i = createIndex + 1;
            if (inputArgs.Length < i + 7)
            {
                errorMessage = "geo-hotspot create requires: create <name> <description> <GeoHotSpotType> <lat> <long> <radiusMetres> <GeoHotSpotTriggeredType> [timeSeconds] [parentFolder]";
                return false;
            }

            name = inputArgs[i++]?.Trim();
            description = inputArgs[i++]?.Trim() ?? "";
            geoHotSpotTypeToken = inputArgs[i++]?.Trim();
            if (!double.TryParse(inputArgs[i++], NumberStyles.Float, CultureInfo.InvariantCulture, out lat)
                || !double.TryParse(inputArgs[i++], NumberStyles.Float, CultureInfo.InvariantCulture, out lon)
                || !int.TryParse(inputArgs[i++], NumberStyles.Integer, CultureInfo.InvariantCulture, out radiusM))
            {
                errorMessage = "Invalid lat, long, or radiusMetres (use invariant numbers, e.g. 51.5 -0.12 25).";
                return false;
            }

            triggerToken = inputArgs[i++]?.Trim();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(geoHotSpotTypeToken) || string.IsNullOrEmpty(triggerToken))
            {
                errorMessage = "name, GeoHotSpotType, and GeoHotSpotTriggeredType must be non-empty.";
                return false;
            }

            if (!Enum.TryParse<GeoHotSpotType>(geoHotSpotTypeToken, ignoreCase: true, out _))
            {
                errorMessage = $"Invalid GeoHotSpotType '{geoHotSpotTypeToken}'.";
                return false;
            }

            if (!Enum.TryParse<GeoHotSpotTriggeredType>(triggerToken, ignoreCase: true, out GeoHotSpotTriggeredType trig))
            {
                errorMessage = $"Invalid GeoHotSpotTriggeredType '{triggerToken}'.";
                return false;
            }

            bool needsTime = trig == GeoHotSpotTriggeredType.WhenAtGeoLocationForXSeconds
                || trig == GeoHotSpotTriggeredType.WhenLookingAtObjectOrImageForXSecondsInARMode;

            if (inputArgs.Length > i)
            {
                if (needsTime && int.TryParse(inputArgs[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int ts) && ts >= 0)
                {
                    timeSeconds = ts;
                    i++;
                }
            }

            if (inputArgs.Length > i)
                parentFolder = inputArgs[i]?.Trim();

            if (needsTime && !timeSeconds.HasValue)
            {
                errorMessage = $"Trigger type '{trig}' requires a non-negative timeSeconds token after the trigger enum.";
                return false;
            }

            return true;
        }

        public static Dictionary<string, object> BuildGeoHotSpotScriptedCustomCreateParams(string name, string description, string geoHotSpotTypeToken, double lat, double lon, int radiusM, string triggerToken, int? timeSeconds, string parentFolder)
        {
            var d = BuildScriptedCustomCreateParams(name, description, geoHotSpotTypeToken, parentFolder);
            d[StarCliNonInteractiveCreateKeys.GeoHotSpotLat] = lat;
            d[StarCliNonInteractiveCreateKeys.GeoHotSpotLong] = lon;
            d[StarCliNonInteractiveCreateKeys.GeoHotSpotRadiusMetres] = radiusM;
            d[StarCliNonInteractiveCreateKeys.GeoHotSpotTriggeredType] = triggerToken;
            if (timeSeconds.HasValue)
                d[StarCliNonInteractiveCreateKeys.GeoHotSpotTimeSeconds] = timeSeconds.Value;
            return d;
        }

        /// <summary>Reads optional <c>--audio-url</c>, <c>--video-url</c>, <c>--audio-file</c>, <c>--video-file</c>, <c>--text-content</c>, <c>--website-url</c> from full argv (non-interactive geo-hotspot create).</summary>
        public static void ApplyGeoHotSpotMediaOptionalArgs(string[] inputArgs, IDictionary<string, object> customParams)
        {
            if (inputArgs == null || customParams == null) return;
            if (TryGetDoubleDashArg(inputArgs, "--audio-url", out string au))
                customParams[StarCliNonInteractiveCreateKeys.GeoHotSpotAudioUrl] = au;
            if (TryGetDoubleDashArg(inputArgs, "--video-url", out string vu))
                customParams[StarCliNonInteractiveCreateKeys.GeoHotSpotVideoUrl] = vu;
            if (TryGetDoubleDashArg(inputArgs, "--audio-file", out string af))
                customParams[StarCliNonInteractiveCreateKeys.GeoHotSpotAudioFilePath] = af;
            if (TryGetDoubleDashArg(inputArgs, "--video-file", out string vf))
                customParams[StarCliNonInteractiveCreateKeys.GeoHotSpotVideoFilePath] = vf;
            if (TryGetDoubleDashArg(inputArgs, "--text-content", out string tx))
                customParams[StarCliNonInteractiveCreateKeys.GeoHotSpotTextContent] = tx;
            if (TryGetDoubleDashArg(inputArgs, "--website-url", out string wu))
                customParams[StarCliNonInteractiveCreateKeys.GeoHotSpotWebsiteUrl] = wu;
        }

        private static bool TryGetDoubleDashArg(string[] args, string flag, out string value)
        {
            value = null;
            if (args == null || args.Length < 2) return false;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i]?.Trim(), flag, StringComparison.OrdinalIgnoreCase))
                {
                    value = args[i + 1]?.Trim();
                    return !string.IsNullOrEmpty(value);
                }
            }
            return false;
        }

        /// <summary>Optional <c>--linked-geo-hotspot-id &lt;guid&gt;</c> and <c>--external-handoff-uri &lt;uri&gt;</c> on scripted <c>quest create</c>.</summary>
        public static bool TryParseOptionalQuestLinkedHandoffArgv(string[] inputArgs, out string linkedGeoHotSpotId, out string externalHandoffUri)
        {
            linkedGeoHotSpotId = null;
            externalHandoffUri = null;
            bool any = false;
            if (TryGetDoubleDashArg(inputArgs, "--linked-geo-hotspot-id", out var lg))
            {
                linkedGeoHotSpotId = lg;
                any = true;
            }
            if (TryGetDoubleDashArg(inputArgs, "--external-handoff-uri", out var ho))
            {
                externalHandoffUri = ho;
                any = true;
            }
            return any;
        }

        /// <summary><c>nft collection create &lt;web4CollectionIdOrName&gt;</c> / <c>geo-nft collection create &lt;idOrName&gt;</c>.</summary>
        public static bool TryParseWrapOnlyWeb4CollectionCreateArgv(string[] inputArgs, out string wrapCollectionId, out string errorMessage)
        {
            wrapCollectionId = null;
            errorMessage = null;
            // Exactly 4 tokens: <entity> collection create <web4IdOrName> — longer argv is minimal create (name + description [+ parent]).
            if (inputArgs == null || inputArgs.Length != 4)
            {
                errorMessage = "Non-interactive collection wrap requires exactly: <nft|geo-nft|geonft> collection create <web4CollectionIdOrName>";
                return false;
            }

            if (!string.Equals(inputArgs[1], "collection", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Expected second token 'collection' (e.g. nft collection create <id>).";
                return false;
            }

            if (!string.Equals(inputArgs[2], "create", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Expected subcommand 'create' after 'collection'.";
                return false;
            }

            wrapCollectionId = inputArgs[3]?.Trim();
            if (string.IsNullOrEmpty(wrapCollectionId))
            {
                errorMessage = "web4CollectionIdOrName must be non-empty.";
                return false;
            }

            return true;
        }

        /// <summary><c>nft collection create &lt;name&gt; &lt;description&gt;</c> — new WEB4 collection + WEB5 wrap (non-interactive).</summary>
        public static bool TryParseNewWeb4NftCollectionCreateArgv(string[] inputArgs, out string name, out string description, out string errorMessage)
        {
            name = description = null;
            errorMessage = null;
            if (inputArgs == null || inputArgs.Length != 5)
                return false;
            if (!string.Equals(inputArgs[0], "nft", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.Equals(inputArgs[1], "collection", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(inputArgs[2], "create", StringComparison.OrdinalIgnoreCase))
                return false;

            name = inputArgs[3]?.Trim();
            description = inputArgs[4]?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
            {
                errorMessage = "NFT collection name (title) must be non-empty.";
                return true;
            }

            return true;
        }

        /// <summary><c>geo-nft collection create &lt;name&gt; &lt;description&gt;</c> / <c>geonft collection create …</c>.</summary>
        public static bool TryParseNewWeb4GeoNftCollectionCreateArgv(string[] inputArgs, out string name, out string description, out string errorMessage)
        {
            name = description = null;
            errorMessage = null;
            if (inputArgs == null || inputArgs.Length != 5)
                return false;
            if (!string.Equals(inputArgs[0], "geo-nft", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(inputArgs[0], "geonft", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.Equals(inputArgs[1], "collection", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(inputArgs[2], "create", StringComparison.OrdinalIgnoreCase))
                return false;

            name = inputArgs[3]?.Trim();
            description = inputArgs[4]?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
            {
                errorMessage = "Geo-NFT collection name (title) must be non-empty.";
                return true;
            }

            return true;
        }

        public static Dictionary<string, object> BuildMinimalWeb4NFTCollectionScriptedParams(string collectionName, string description) =>
            new Dictionary<string, object>
            {
                [StarCliNonInteractiveCreateKeys.Scripted] = true,
                [StarCliNonInteractiveCreateKeys.CreateMinimalNftCollection] = true,
                [StarCliNonInteractiveCreateKeys.Name] = collectionName,
                [StarCliNonInteractiveCreateKeys.Description] = description ?? ""
            };

        public static Dictionary<string, object> BuildMinimalWeb4GeoNFTCollectionScriptedParams(string collectionName, string description) =>
            new Dictionary<string, object>
            {
                [StarCliNonInteractiveCreateKeys.Scripted] = true,
                [StarCliNonInteractiveCreateKeys.CreateMinimalGeoNftCollection] = true,
                [StarCliNonInteractiveCreateKeys.Name] = collectionName,
                [StarCliNonInteractiveCreateKeys.Description] = description ?? ""
            };

        public static Dictionary<string, object> BuildWrapWeb4NFTCollectionScriptedParams(string web4CollectionIdOrName) =>
            new Dictionary<string, object>
            {
                [StarCliNonInteractiveCreateKeys.Scripted] = true,
                [StarCliNonInteractiveCreateKeys.WrapWeb4NFTCollectionId] = web4CollectionIdOrName
            };

        public static Dictionary<string, object> BuildWrapWeb4GeoNFTCollectionScriptedParams(string web4CollectionIdOrName) =>
            new Dictionary<string, object>
            {
                [StarCliNonInteractiveCreateKeys.Scripted] = true,
                [StarCliNonInteractiveCreateKeys.WrapWeb4GeoNFTCollectionId] = web4CollectionIdOrName
            };

        /// <summary><c>nft create &lt;web4NftIdOrGuid&gt;</c> / <c>geonft create &lt;web4GeoNftIdOrGuid&gt;</c> (wrap existing WEB4 asset).</summary>
        public static bool TryParseWrapOnlyWeb4CreateArgv(string[] inputArgs, out string wrapId, out string errorMessage)
        {
            wrapId = null;
            errorMessage = null;
            if (inputArgs == null || inputArgs.Length < 3)
            {
                errorMessage = "wrap create requires: create <web4AssetId>";
                return false;
            }

            bool shifted = inputArgs.Length > 1 &&
                (string.Equals(inputArgs[1], "template", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(inputArgs[1], "metadata", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(inputArgs[1], "collection", StringComparison.OrdinalIgnoreCase));

            int createIndex = shifted ? 2 : 1;
            if (inputArgs.Length <= createIndex || !string.Equals(inputArgs[createIndex], "create", StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Expected subcommand 'create'.";
                return false;
            }

            if (inputArgs.Length < createIndex + 2)
            {
                errorMessage = "Non-interactive wrap create requires: create <web4NftIdOrGuid>";
                return false;
            }

            wrapId = inputArgs[createIndex + 1]?.Trim();
            if (string.IsNullOrEmpty(wrapId))
            {
                errorMessage = "web4 id must be non-empty.";
                return false;
            }

            return true;
        }

        public static Dictionary<string, object> BuildWrapWeb4NftScriptedParams(string web4Id)
        {
            return new Dictionary<string, object>
            {
                [StarCliNonInteractiveCreateKeys.Scripted] = true,
                [StarCliNonInteractiveCreateKeys.WrapWeb4NFTId] = web4Id
            };
        }

        public static Dictionary<string, object> BuildWrapWeb4GeoSpatialNftScriptedParams(string web4GeoId)
        {
            return new Dictionary<string, object>
            {
                [StarCliNonInteractiveCreateKeys.Scripted] = true,
                [StarCliNonInteractiveCreateKeys.WrapWeb4GeoSpatialNFTId] = web4GeoId
            };
        }

        /// <summary><c>oapp create light &lt;path&gt;</c> / <c>happ create light &lt;path&gt;</c> (argv[0] is <c>oapp</c> or <c>happ</c>).</summary>
        /// <returns><c>false</c> if this is not a light-json create; <c>true</c> if it is (check <paramref name="errorMessage"/> and <paramref name="jsonPath"/>).</returns>
        public static bool TryParseOappLightJsonCreateArgv(string[] inputArgs, out string jsonPath, out string errorMessage)
        {
            jsonPath = null;
            errorMessage = null;

            if (inputArgs == null || inputArgs.Length < 3)
                return false;

            string root = inputArgs[0]?.Trim();
            if (!string.Equals(root, "oapp", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(root, "happ", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.Equals(inputArgs[1], "create", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.Equals(inputArgs[2], "light", StringComparison.OrdinalIgnoreCase))
                return false;

            if (inputArgs.Length < 4)
            {
                errorMessage = "oapp create light requires a path to LightRequest JSON. Example: oapp create light ./LightRequest.json";
                return true;
            }

            jsonPath = inputArgs[3]?.Trim();
            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                errorMessage = "Light JSON path is empty.";
                return true;
            }

            return true;
        }

        /// <summary><c>oapp light &lt;path&gt;</c> / <c>happ light &lt;path&gt;</c> — same Light JSON as <c>create light</c>.</summary>
        /// <returns><c>false</c> if argv is not this form; <c>true</c> if it is (check <paramref name="errorMessage"/> / path).</returns>
        public static bool TryParseOappLightDirectArgv(string[] inputArgs, out string jsonPath, out string errorMessage)
        {
            jsonPath = null;
            errorMessage = null;

            if (inputArgs == null || inputArgs.Length < 2)
                return false;

            string root = inputArgs[0]?.Trim();
            if (!string.Equals(root, "oapp", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(root, "happ", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.Equals(inputArgs[1], "light", StringComparison.OrdinalIgnoreCase))
                return false;

            if (inputArgs.Length < 3)
            {
                errorMessage = "oapp light requires a path to LightRequest JSON. Example: oapp light ./LightRequest.json";
                return true;
            }

            jsonPath = inputArgs[2]?.Trim();
            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                errorMessage = "Light JSON path is empty.";
                return true;
            }

            return true;
        }

        public static Dictionary<string, object> BuildOappLightJsonCustomCreateParams(string jsonPath) =>
            new Dictionary<string, object>
            {
                [StarCliNonInteractiveCreateKeys.LightRequestJsonPath] = jsonPath
            };

        /// <summary>Scans argv for <c>--objectives-json &lt;path&gt;</c> (scripted <c>quest create</c> / <c>quest update</c>). File may be a JSON array of objectives or an object with <c>objectives</c>, optional <c>linkedGeoHotSpotId</c>, <c>externalHandoffUri</c> (create requires <c>objectives</c>; update may omit <c>objectives</c> to only set handoff fields).</summary>
        public static bool TryParseOptionalQuestObjectivesJsonPath(string[] inputArgs, out string objectivesJsonPath)
        {
            objectivesJsonPath = null;
            if (inputArgs == null)
                return false;

            for (int i = 0; i < inputArgs.Length - 1; i++)
            {
                if (string.Equals(inputArgs[i], "--objectives-json", StringComparison.OrdinalIgnoreCase))
                {
                    objectivesJsonPath = inputArgs[i + 1]?.Trim();
                    return !string.IsNullOrWhiteSpace(objectivesJsonPath);
                }
            }

            return false;
        }

        /// <summary>Non-interactive <c>quest update &lt;id&gt; --objectives-json &lt;path&gt;</c>: replaces authored objectives from JSON.</summary>
        public static bool TryParseQuestUpdateArgv(string[] inputArgs, out QuestCliEditParams editParams)
        {
            editParams = null;
            if (!TryParseOptionalQuestObjectivesJsonPath(inputArgs, out string path) || string.IsNullOrWhiteSpace(path))
                return false;

            editParams = new QuestCliEditParams { ObjectivesJsonPath = path };
            return true;
        }
    }

    /// <summary>Non-interactive <see cref="Quests.UpdateAsync"/> payload (e.g. <c>--objectives-json</c>).</summary>
    public sealed class QuestCliEditParams
    {
        public string ObjectivesJsonPath { get; set; }
    }
}

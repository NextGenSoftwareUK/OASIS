namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    /// <summary>
    /// Keys in <see cref="NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.ISTARNETCreateOptions{T1,T2}.CustomCreateParams"/> for scripted CLI create.
    /// Consumed by <see cref="STARNETUIBase{T1,T2,T3,T4}"/>; argv is parsed in <see cref="StarnetUiScriptedCreateCli"/>.
    /// </summary>
    public static class StarCliNonInteractiveCreateKeys
    {
        /// <summary>Absolute or relative path to a <see cref="StarCliLightRequest"/> JSON file for non-interactive full Light (<c>oapp create light</c> / <c>LightFromJsonFileAsync</c>).</summary>
        public const string LightRequestJsonPath = "star.cli.lightRequestJsonPath";

        public const string Scripted = "star.cli.scriptedCreate";
        public const string Name = "star.cli.createName";
        public const string Description = "star.cli.createDescription";
        public const string SubType = "star.cli.createSubType";
        /// <summary>Optional parent directory; holon folder is <c>Path.Combine(ParentFolder, Name)</c>. If omitted, STARDNA default source path is used (no prompts).</summary>
        public const string ParentFolder = "star.cli.createParentFolder";

        /// <summary>Matches <see cref="Libs"/> interactive create: value is a <see cref="NextGenSoftware.OASIS.API.Core.Enums.Languages"/> enum instance.</summary>
        public const string LibraryStarnetSubCategory = "STARNETSubCategory";

        /// <summary>WEB4 NFT id (GUID string) for scripted <c>nft create</c> wrap-only flow.</summary>
        public const string WrapWeb4NFTId = "star.cli.wrapWeb4NftId";

        /// <summary>WEB4 GeoSpatial NFT id for scripted <c>geonft create</c> / <c>geo-nft create</c> wrap-only flow.</summary>
        public const string WrapWeb4GeoSpatialNFTId = "star.cli.wrapWeb4GeoNftId";

        public const string GeoHotSpotLat = "star.cli.geoHotSpotLat";
        public const string GeoHotSpotLong = "star.cli.geoHotSpotLong";
        public const string GeoHotSpotRadiusMetres = "star.cli.geoHotSpotRadiusMetres";
        public const string GeoHotSpotTriggeredType = "star.cli.geoHotSpotTriggeredType";
        public const string GeoHotSpotTimeSeconds = "star.cli.geoHotSpotTimeSeconds";

        /// <summary>Optional plugin namespace for scripted <c>plugin create</c>; default NextGenSoftware.OASIS.Plugins.&lt;Name&gt;.</summary>
        public const string PluginNamespace = "star.cli.pluginNamespace";
        public const string PluginManagerClassName = "star.cli.pluginManagerClassName";
        public const string PluginCliClassName = "star.cli.pluginCliClassName";
        /// <summary>Comma-separated CLI subcommand names for generated plugin CLI (optional).</summary>
        public const string PluginCliCommandsCsv = "star.cli.pluginCliCommandsCsv";

        /// <summary>WEB4 NFT collection id or name for scripted <c>nft collection create</c> wrap.</summary>
        public const string WrapWeb4NFTCollectionId = "star.cli.wrapWeb4NftCollectionId";

        /// <summary>WEB4 GeoNFT collection id or name for scripted <c>geo-nft collection create</c> wrap.</summary>
        public const string WrapWeb4GeoNFTCollectionId = "star.cli.wrapWeb4GeoNftCollectionId";

        /// <summary>When <c>true</c> with <see cref="Scripted"/>, <see cref="Name"/>, <see cref="Description"/>: create a new WEB4 NFT collection via API then WEB5 wrap (non-interactive).</summary>
        public const string CreateMinimalNftCollection = "star.cli.createMinimalNftCollection";

        /// <summary>When <c>true</c> with <see cref="Scripted"/>, <see cref="Name"/>, <see cref="Description"/>: create a new WEB4 Geo-NFT collection via API then WEB5 wrap (non-interactive).</summary>
        public const string CreateMinimalGeoNftCollection = "star.cli.createMinimalGeoNftCollection";

        /// <summary>Optional path to JSON array of quest objectives for scripted <c>quest create</c> (<c>--objectives-json</c>).</summary>
        public const string QuestObjectivesJsonPath = "star.cli.questObjectivesJsonPath";
    }
}

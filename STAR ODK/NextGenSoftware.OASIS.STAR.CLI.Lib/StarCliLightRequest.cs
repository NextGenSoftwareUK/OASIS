using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    /// <summary>
    /// JSON request for non-interactive full Light generation (<see cref="OAPPs.LightFromJsonFileAsync"/> / <c>light json &lt;file&gt;</c> / <c>oapp create light &lt;file&gt;</c>).
    /// All wizard prompts are replaced by explicit fields; optional template/meta/geo fields match what the interactive Light wizard can produce.
    /// </summary>
    public sealed class StarCliLightRequest
    {
        [JsonProperty("oappName")]
        public string OAPPName { get; set; }

        [JsonProperty("oappDescription")]
        public string OAPPDescription { get; set; }

        /// <summary>Case-insensitive <see cref="NextGenSoftware.OASIS.API.Core.Enums.OAPPType"/>; default <c>OAPPTemplate</c>.</summary>
        [JsonProperty("oappType")]
        public string OAPPType { get; set; }

        /// <summary>When false, runs as <see cref="NextGenSoftware.OASIS.API.Core.Enums.OAPPType.GeneratedCodeOnly"/> (no template copy).</summary>
        [JsonProperty("useOappTemplate")]
        public bool UseOappTemplate { get; set; } = true;

        [JsonProperty("oappTemplateId")]
        public Guid? OAPPTemplateId { get; set; }

        /// <summary>Passed to <c>LoadInstalledAsync</c> (template version).</summary>
        [JsonProperty("oappTemplateVersion")]
        public int? OAPPTemplateVersion { get; set; }

        /// <summary>Version sequence passed to <see cref="NextGenSoftware.OASIS.STAR.Star.LightAsync"/>; defaults to <see cref="OAPPTemplateVersion"/> or 1.</summary>
        [JsonProperty("oappTemplateVersionSequence")]
        public int? OAPPTemplateVersionSequence { get; set; }

        [JsonProperty("oappTemplateType")]
        public string OAPPTemplateType { get; set; } = "Console";

        [JsonProperty("genesisType")]
        public string GenesisType { get; set; }

        [JsonProperty("celestialBodyDnaFolder")]
        public string CelestialBodyDnaFolder { get; set; }

        [JsonProperty("genesisFolder")]
        public string GenesisFolder { get; set; }

        [JsonProperty("genesisNamespace")]
        public string GenesisNamespace { get; set; }

        [JsonProperty("parentCelestialBodyId")]
        public Guid? ParentCelestialBodyId { get; set; }

        [JsonProperty("metaHolonTags")]
        public List<MetaHolonTag> MetaHolonTags { get; set; }

        [JsonProperty("metaTags")]
        public Dictionary<string, string> MetaTags { get; set; }

        /// <summary>When true, only runs code generation (<see cref="NextGenSoftware.OASIS.STAR.Star.LightAsync"/>); skips STARNET OAPP registration and runtime install.</summary>
        [JsonProperty("skipStarnetOappCreate")]
        public bool SkipStarnetOappCreate { get; set; }

        [JsonProperty("providerType")]
        public string ProviderType { get; set; }

        [JsonProperty("ourWorldLat")]
        public long OurWorldLat { get; set; }

        [JsonProperty("ourWorldLong")]
        public long OurWorldLong { get; set; }

        [JsonProperty("ourWorld3dObjectPath")]
        public string OurWorld3dObjectPath { get; set; }

        [JsonProperty("ourWorld3dObjectUri")]
        public string OurWorld3dObjectUri { get; set; }

        [JsonProperty("ourWorld2dSpritePath")]
        public string OurWorld2dSpritePath { get; set; }

        [JsonProperty("ourWorld2dSpriteUri")]
        public string OurWorld2dSpriteUri { get; set; }

        [JsonProperty("oneWorldLat")]
        public long OneWorldLat { get; set; }

        [JsonProperty("oneWorldLong")]
        public long OneWorldLong { get; set; }

        [JsonProperty("oneWorld3dObjectPath")]
        public string OneWorld3dObjectPath { get; set; }

        [JsonProperty("oneWorld3dObjectUri")]
        public string OneWorld3dObjectUri { get; set; }

        [JsonProperty("oneWorld2dSpritePath")]
        public string OneWorld2dSpritePath { get; set; }

        [JsonProperty("oneWorld2dSpriteUri")]
        public string OneWorld2dSpriteUri { get; set; }

        [JsonProperty("celestialBodyMetaDataGeneratedPath")]
        public string CelestialBodyMetaDataGeneratedPath { get; set; }
    }
}

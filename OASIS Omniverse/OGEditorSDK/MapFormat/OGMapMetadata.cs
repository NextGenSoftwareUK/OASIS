using System.Collections.Generic;

namespace OGEditorSDK.MapFormat
{
    public class OGMapMetadata
    {
        public string   SkyTexture   { get; set; }
        public string   MusicTrack   { get; set; }
        public string   Author       { get; set; }
        public string   Description  { get; set; }
        public OGVector3 WorldMin    { get; set; }
        public OGVector3 WorldMax    { get; set; }
        public OGColor  AmbientLight { get; set; }
        public bool     HasFog       { get; set; }
        public OGFog    Fog          { get; set; }

        // Format-specific key/values that have no IR equivalent — preserved for round-trips
        public Dictionary<string, string> Extra { get; set; } = new Dictionary<string, string>();
    }
}

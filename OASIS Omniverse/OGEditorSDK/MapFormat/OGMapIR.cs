using System.Collections.Generic;

namespace OGEditorSDK.MapFormat
{
    /// <summary>
    /// Neutral intermediate representation of a game map.
    /// All format adapters read into and write from this type.
    /// </summary>
    public class OGMapIR
    {
        public string          MapName       { get; set; }
        public string          SourceFormat  { get; set; }
        public OGMapMetadata   Metadata      { get; set; } = new OGMapMetadata();

        public List<OGPointEntity>       PointEntities { get; set; } = new List<OGPointEntity>();
        public List<OGBrushEntity>       BrushEntities { get; set; } = new List<OGBrushEntity>();

        // World geometry not owned by a specific entity (worldspawn brushes / sectors / tiles)
        public List<OGGeometryPrimitive> WorldGeometry { get; set; } = new List<OGGeometryPrimitive>();

        // OASIS portal topology preserved through conversion (raw oasis_*.json sidecar content)
        public string OASISSidecarJson { get; set; }
    }
}

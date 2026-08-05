using System.Collections.Generic;

namespace OGEditorSDK.MapFormat
{
    public class OGEntity
    {
        public string                     Classname      { get; set; }
        public int                        OASISThingType { get; set; } = -1;
        public Dictionary<string, string> Keys           { get; set; } = new Dictionary<string, string>();
        public OGVector3                  Origin         { get; set; }
        public float                      Angle          { get; set; }

        public string GetKey(string key, string defaultValue = "")
        {
            Keys.TryGetValue(key, out var v);
            return v ?? defaultValue;
        }
    }

    public class OGPointEntity : OGEntity { }

    public class OGBrushEntity : OGEntity
    {
        public List<OGGeometryPrimitive> Geometry { get; set; } = new List<OGGeometryPrimitive>();
    }
}

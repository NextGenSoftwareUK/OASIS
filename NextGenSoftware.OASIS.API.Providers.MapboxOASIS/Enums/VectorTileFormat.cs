using System.ComponentModel;

namespace Enums
{
    public enum VectorTileFormat
    {
        [Description(".mvt")]
        Mvt,
        [Description(".vector.pbf")]
        VectorPbf
    }
}
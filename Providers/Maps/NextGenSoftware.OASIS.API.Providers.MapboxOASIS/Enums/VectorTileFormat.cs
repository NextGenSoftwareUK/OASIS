using System.ComponentModel;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Enums
{
    public enum VectorTileFormat
    {
        [Description(".mvt")]
        Mvt,
        [Description(".vector.pbf")]
        VectorPbf
    }
}
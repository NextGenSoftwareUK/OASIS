using System.ComponentModel;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Enums
{
    public enum RasterTileFormat
    {
        [Description(".grid.json")]
        GridJson,
        [Description(".png")]
        Png,
        [Description(".png32")]
        Png32,
        [Description(".png64")]
        Png64,
        [Description(".png128")]
        Png128,
        [Description(".png256")]
        Png256,
        [Description(".jpg70")]
        Jpg70,
        [Description(".jpg80")]
        Jpg80,
	    [Description(".jpg90")]
	    Jpg90
    }
}
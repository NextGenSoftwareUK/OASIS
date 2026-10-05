
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STAR.EventArgs
{
    public class DefaultCelestialBodyInitEventArgs : System.EventArgs
    {
        public OASISResult<ICelestialBody> Result { get; set; }
    }
}
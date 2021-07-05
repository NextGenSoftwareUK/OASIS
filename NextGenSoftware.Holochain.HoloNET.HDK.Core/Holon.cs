
using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{ 
    public class Holon : OASIS.API.Core.Holon, IHolon
    {
       public string RustHolonType { get; set; }
    }
}

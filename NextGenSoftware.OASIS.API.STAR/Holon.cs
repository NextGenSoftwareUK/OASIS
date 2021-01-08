
using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.STAR
{ 
    public class Holon : OASIS.API.Core.Holon, IHolon
    {
       public string RustHolonType { get; set; }
    }
}

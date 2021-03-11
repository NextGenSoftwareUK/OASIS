
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.STAR
{ 
    public class Holon : API.Core.Holons.Holon, IHolon
    {
       public string RustHolonType { get; set; }
    }
}

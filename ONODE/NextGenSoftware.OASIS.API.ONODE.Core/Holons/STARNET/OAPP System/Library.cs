using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class Library : STARNETHolon, ILibrary
    {
        public Library() : base("LibraryDNAJSON")
        {
            this.HolonType = HolonType.Library;
        }
    }
}
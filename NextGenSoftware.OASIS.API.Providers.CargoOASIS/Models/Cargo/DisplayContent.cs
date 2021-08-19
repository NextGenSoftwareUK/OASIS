using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Enum;

namespace Models.Cargo
{
    public class DisplayContent
    {
        public DisplayContentType Type { get; set; }
        public IEnumerable<byte[]> Files { get; set; }
    }
}
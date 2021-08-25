using System.Collections.Generic;
using Models.Cargo;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class ResaleItemGroups
    {
        public IEnumerable<ResaleItemWithMetadata> ResaleItemWithMetadata { get; set; }
    }
}
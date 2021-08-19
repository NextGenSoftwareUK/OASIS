using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class ContractV3
    {
        public string Id { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public bool SupportsMetadata { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public bool Owned { get; set; }
        public int TotalOwned { get; set; }
        public string CreateAt { get; set; }
    }
}
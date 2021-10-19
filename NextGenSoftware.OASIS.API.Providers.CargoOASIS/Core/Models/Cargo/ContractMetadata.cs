using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class ContractMetadata
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public bool SupportsMetadata { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public bool IsOwned { get; set; }
        public string Owner { get; set; }
        public string TotalSupply { get; set; }
        public string Id { get; set; }
    }
}
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class TokenMetadata
    {
        public string Schema { get; set; }
        public string Evidence { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public IEnumerable<string> Metadata { get; set; }
        public string Name { get; set; }
    }
}
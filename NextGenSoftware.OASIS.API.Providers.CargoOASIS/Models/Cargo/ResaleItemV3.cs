using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class ResaleItemV3
    {
        public string Seller { get; set; }
        public string Contract { get; set; }
        public string TokenId { get; set; }
        public string Price { get; set; }
        public string GroupId { get; set; }
        public bool SignatureGenerated { get; set; }
        public string Crate { get; set; }
        public string CreateAt { get; set; }
        public IDictionary<string, string> Metadata { get; set; }
    }
}
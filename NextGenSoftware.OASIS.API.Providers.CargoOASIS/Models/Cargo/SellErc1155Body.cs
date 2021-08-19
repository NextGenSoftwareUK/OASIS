using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class SellErc1155Body
    {
        public IEnumerable<string> Id { get; set; }
        public IEnumerable<string> Values { get; set; }
        public string Price { get; set; }
        public string ContractAddress { get; set; }
        public string CrateId { get; set; }
        public string Sender { get; set; }
        public string Signature { get; set; }
    }
}
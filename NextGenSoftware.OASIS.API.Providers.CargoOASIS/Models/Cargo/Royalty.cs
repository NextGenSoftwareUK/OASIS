using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class Royalty
    {
        public string ContractAddress { get; set; }
        public string TokenId { get; set; }
        public IEnumerable<string> Payees { get; set; }
        public IEnumerable<int> Commissions { get; set; }
    }
}
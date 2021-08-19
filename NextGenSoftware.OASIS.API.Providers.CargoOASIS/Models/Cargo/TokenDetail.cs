using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;

namespace Models.Cargo
{
    public class TokenDetail
    {
        public string Owner { get; set; }
        public string OwnerAddress { get; set; }
        public ResaleItemV3 ResaleItem { get; set; }
        public string TokenId { get; set; }
        public IDictionary<string, string> Metadata { get; set; }
        public string TokenUrl { get; set; }
        public string ContractName { get; set; }
        public string ContractSymbol { get; set; }
        public string ContractAddress { get; set; }
    }
}
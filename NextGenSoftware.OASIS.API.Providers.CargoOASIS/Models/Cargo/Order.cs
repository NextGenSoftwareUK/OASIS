using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo;

namespace Models.Cargo
{
    public class Order
    { 
        public string Seller { get; set; }
        public string Buyer { get; set; }
        public string Uuid { get; set; }
        public string Contract { get; set; }
        public string TokenId { get; set; }
        public string Price { get; set; }
        public Fee Fees { get; set; }
        public IEnumerable<Beneficiary> Beneficiaries { get; set; }
        public string CrateId { get; set; }
        public string CreatedAt { get; set; }
    }
}
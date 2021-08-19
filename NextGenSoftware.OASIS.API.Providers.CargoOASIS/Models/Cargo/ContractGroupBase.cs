using System.Collections.Generic;

namespace Models.Cargo
{
    public class ContractGroupBase
    {
        public string ContractId { get; set; }
        public string CurrentPage { get; set; }
        public IEnumerable<Token> Tokens { get; set; }
        public string VendorId { get; set; }
        public string VendorAddress { get; set; }
        public string TotalPages { get; set; }
        public string TotalSupply { get; set; }
    }
}
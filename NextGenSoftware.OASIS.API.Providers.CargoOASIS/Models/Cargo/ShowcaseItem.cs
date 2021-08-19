using System.Collections.Generic;

namespace Models.Cargo
{
    public class ShowcaseItem
    {
        public string TokenId { get; set; }
        public IDictionary<string, object> Metadata { get; set; }
        public ResaleItem ResaleItem { get; set; }
        public ContractV3 Collection { get; set; }
    }
}
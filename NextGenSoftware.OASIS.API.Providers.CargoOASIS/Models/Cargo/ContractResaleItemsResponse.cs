using System.Collections.Generic;
using Models.Cargo;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class ContractResaleItemsResponse
    {
        public string CurrentPage { get; set; }
        public string TotalPages { get; set; }
        public string Total { get; set; }
        public IEnumerable<ResaleItem> ResaleItems { get; set; }
    }
}
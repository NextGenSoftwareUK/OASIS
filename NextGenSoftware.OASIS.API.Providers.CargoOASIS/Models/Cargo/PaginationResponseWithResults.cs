using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Models.Cargo
{
    public class PaginationResponseWithResults
    {
        public string TokenId { get; set; }
        public string TokenUrl { get; set; }
        public IDictionary<string, object> Metadata { get; set; }
    }

    public class PaginationResponseWithResults<T>
    {
        public string Page { get; set; }
        public string TotalPage { get; set; }
        public string Limit { get; set; }
        public T Result { get; set; }
    }
}
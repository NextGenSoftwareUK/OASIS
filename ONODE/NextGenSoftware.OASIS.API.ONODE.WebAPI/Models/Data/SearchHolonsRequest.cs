
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class SearchHolonsRequest : BaseLoadHolonRequest
    {
        public string SearchTerm { get; set; }
        public Guid ParentId { get; set; }
        public string HolonType { get; set; } = "All";
        public Dictionary<string, string> FilterByMetaData { get; set; }
        public string MetaKeyValuePairMatchMode { get; set; } = "All";
        public bool SearchOnlyForCurrentAvatar { get; set; } = true;
    }
}

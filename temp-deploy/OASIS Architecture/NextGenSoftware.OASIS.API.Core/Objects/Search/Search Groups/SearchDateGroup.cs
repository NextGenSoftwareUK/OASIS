
using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Objects.Search
{
    public class SearchDateGroup : SearchGroupBase, ISearchDateGroup
    {
        public SearchOperatorType DateOperator { get; set; }
        public DateTime Date { get; set; }
    }
}

using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Objects.Search
{
    public interface ISearchDateGroup : ISearchGroupBase
    {
        public SearchOperatorType DateOperator { get; set; }
        public DateTime Date { get; set; }
    }
}
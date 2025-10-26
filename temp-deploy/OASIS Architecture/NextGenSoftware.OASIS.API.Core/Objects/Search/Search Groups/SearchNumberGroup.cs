
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Objects.Search
{
    public class SearchNumberGroup : SearchGroupBase, ISearchNumberGroup
    {
        public SearchOperatorType NumberOperator { get; set; }
        public int Number { get; set; }
    }
}

using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Objects.Search
{
    public interface ISearchNumberGroup : ISearchGroupBase
    {
        public SearchOperatorType NumberOperator { get; set; }
        public int Number { get; set; }
    }
}
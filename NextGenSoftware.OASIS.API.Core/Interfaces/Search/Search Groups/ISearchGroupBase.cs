﻿
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search.Holon;

namespace NextGenSoftware.OASIS.API.Core.Objects.Search
{
    public interface ISearchGroupBase
    {
        public SearchParamGroupOperator PreviousSearchGroupOperator { get; set; }   
        public bool SearchAvatars { get; set; }
        public bool SearchHolons { get; set; }
        public ISearchAvatarParams AvatarSerachParams { get; set; }
        public ISearchHolonParams HolonSearchParams { get; set; }
    }
}

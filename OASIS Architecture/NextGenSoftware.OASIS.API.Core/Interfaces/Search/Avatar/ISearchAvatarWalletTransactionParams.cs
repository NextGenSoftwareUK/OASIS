
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Collections.Generic;
using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Search.Avatar
{
    public interface ISearchAvatarWalletTransactionParams
    {
        public bool Amount { get; set; }
        public bool FromWalletAddress { get; set; }
        public bool ToWalletAddress { get; set; }
        public bool Token { get; set; }
        public bool MemoText { get; set; }
        public bool ProviderType { get; set; }
    }
}

using NextGenSoftware.OASIS.API.Core.Interfaces.Search.Avatar;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public class SearchAvatarWalletTransactionParams : ISearchAvatarWalletTransactionParams
    {
        public bool Amount { get; set; }
        public bool FromWalletAddress { get; set; }
        public bool ToWalletAddress { get; set; }
        public bool Token { get; set; }
        public bool MemoText { get; set; }
        public bool ProviderType { get; set; }
    }
}
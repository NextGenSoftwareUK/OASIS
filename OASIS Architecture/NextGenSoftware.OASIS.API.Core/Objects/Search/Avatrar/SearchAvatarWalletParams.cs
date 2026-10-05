
using NextGenSoftware.OASIS.API.Core.Interfaces.Search.Avatar;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public class SearchAvatarWalletParams : ISearchAvatarWalletParams
    {
        public bool WalletId { get; set; }
        public bool PublicKey { get; set; }
        public bool WalletAddress { get; set; } 
        public bool ProviderType { get; set; }
        public bool Balance { get; set; }
        public bool IsDefaultWallet { get; set; }

        public ISearchAvatarWalletTransactionParams SearchAvatarWalletTransactionParams { get; set; }  
    }
}
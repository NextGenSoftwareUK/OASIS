
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Objects;

namespace NextGenSoftware.OASIS.API.ONode.Core.Objects
{
    public class CreateNftTransactionRequest : ICreateNftTransactionRequest
    {
        public NFTProviderType NFTProviderType { get; set; }
        public string MintWalletAddress { get; set; }
        public string FromWalletAddress { get; set; }
        public string ToWalletAddress { get; set; }
        public int Amount { get; set; }
        public string Token { get; set; }
        public string MemoText { get; set; }
    }
}
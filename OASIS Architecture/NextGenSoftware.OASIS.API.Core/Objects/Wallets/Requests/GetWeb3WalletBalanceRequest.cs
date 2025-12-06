
namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public class GetWeb3WalletBalanceRequest : IGetWeb3WalletBalanceRequest
    {
        public string WalletAddress { get; set; }
    }
}
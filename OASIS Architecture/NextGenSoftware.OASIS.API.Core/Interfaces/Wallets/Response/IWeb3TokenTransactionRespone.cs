using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response
{
    public interface IWeb3TokenTransactionRespone : ITransactionRespone
    {
        IWeb3Token Web3Token { get; set; }
    }
}
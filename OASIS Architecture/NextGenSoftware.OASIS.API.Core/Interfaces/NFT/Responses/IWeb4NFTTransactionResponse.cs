using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response
{
    public interface IWeb4NFTTransactionRespone : ITransactionRespone
    {
        IWeb4OASISNFT Web4OASISNFT { get; set; }
    }
}
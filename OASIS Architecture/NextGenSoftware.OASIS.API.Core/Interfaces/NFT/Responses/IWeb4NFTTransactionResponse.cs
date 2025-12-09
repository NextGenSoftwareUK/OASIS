using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses
{
    public interface IWeb4NFTTransactionResponse : ITransactionResponse
    {
        IWeb4NFT Web4OASISNFT { get; set; }
    }
}
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    public interface INFTTransactionRespone : ITransactionRespone
    {
        IOASISNFT OASISNFT { get; set; }
    }
}
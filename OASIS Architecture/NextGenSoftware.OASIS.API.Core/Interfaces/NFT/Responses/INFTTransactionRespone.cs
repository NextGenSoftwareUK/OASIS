using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response
{
    public interface INFTTransactionRespone : ITransactionRespone
    {
        IOASISNFT OASISNFT { get; set; }
    }
}
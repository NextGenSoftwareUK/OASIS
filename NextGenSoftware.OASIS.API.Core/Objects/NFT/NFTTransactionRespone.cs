
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallets
{
    public class NFTTransactionRespone : TransactionRespone, INFTTransactionRespone
    {
        public IOASISNFT OASISNFT { get; set; }
    }
}
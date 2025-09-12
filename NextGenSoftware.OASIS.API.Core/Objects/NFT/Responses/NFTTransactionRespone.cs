
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Responses;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response
{
    public class NFTTransactionRespone : TransactionRespone, INFTTransactionRespone
    {
        public IOASISNFT OASISNFT { get; set; }
        public string SendNFTTransactionResult { get; set; }
    }
}
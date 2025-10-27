using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISNFTProvider : IOASISProvider
    {
        //TODO: More to come soon... ;-)
        public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transation);
        public Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transation);
        public OASISResult<INFTTransactionRespone> MintNFT(IMintNFTTransactionRequest transation);
        public Task<OASISResult<INFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequest transation);

        //TODO: Implement ASAP!
        //public OASISResult<INFTTransactionRespone> BurnNFT(IMintNFTTransactionRequest transation);
        //public Task<OASISResult<INFTTransactionRespone>> BurnNFTAsync(IMintNFTTransactionRequest transation);
        public OASISResult<IOASISNFT> LoadOnChainNFTData(string nftTokenAddress);
        public Task<OASISResult<IOASISNFT>> LoadOnChainNFTDataAsync(string nftTokenAddress);
    }
}
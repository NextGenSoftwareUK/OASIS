using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISNFTProvider : IOASISProvider
    {
        //TODO: More to come soon... ;-)
        public OASISResult<IWeb3NFTTransactionRespone> SendNFT(IWeb3NFTWalletTransactionRequest transation);
        public Task<OASISResult<IWeb3NFTTransactionRespone>> SendNFTAsync(IWeb3NFTWalletTransactionRequest transation);
        public OASISResult<IWeb3NFTTransactionRespone> MintNFT(IMintWeb3NFTRequest transation);
        public Task<OASISResult<IWeb3NFTTransactionRespone>> MintNFTAsync(IMintWeb3NFTRequest transation);

        //TODO: Implement ASAP!
        //public OASISResult<IWeb4NFTTransactionRespone> BurnNFT(IMintNFTTransactionRequest transation);
        //public Task<OASISResult<IWeb4NFTTransactionRespone>> BurnNFTAsync(IMintNFTTransactionRequest transation);
        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress);
        public Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress);
    }
}
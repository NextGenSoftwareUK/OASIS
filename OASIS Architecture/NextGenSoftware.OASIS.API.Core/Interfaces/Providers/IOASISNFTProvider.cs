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
        public OASISResult<IWeb4NFTTransactionRespone> SendNFT(IWeb4NFTWalletTransactionRequest transation);
        public Task<OASISResult<IWeb4NFTTransactionRespone>> SendNFTAsync(IWeb4NFTWalletTransactionRequest transation);
        public OASISResult<IWeb4NFTTransactionRespone> MintNFT(IMintWeb4NFTTRequest transation);
        public Task<OASISResult<IWeb4NFTTransactionRespone>> MintNFTAsync(IMintWeb4NFTTRequest transation);

        //TODO: Implement ASAP!
        //public OASISResult<IWeb4NFTTransactionRespone> BurnNFT(IMintNFTTransactionRequest transation);
        //public Task<OASISResult<IWeb4NFTTransactionRespone>> BurnNFTAsync(IMintNFTTransactionRequest transation);
        public OASISResult<IWeb4OASISNFT> LoadOnChainNFTData(string nftTokenAddress);
        public Task<OASISResult<IWeb4OASISNFT>> LoadOnChainNFTDataAsync(string nftTokenAddress);
    }
}
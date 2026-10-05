using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISNFTProvider : IOASISProvider
    {
        //TODO: More to come soon... ;-)
        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation);
        public Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transation);
        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation);
        public Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transation);
        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request);
        public Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request);
        public OASISResult<IWeb3NFTTransactionResponse> LockToken(ILockWeb3TokenRequest request);
        public Task<OASISResult<IWeb3NFTTransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request);
        public OASISResult<IWeb3NFTTransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request);
        public Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request);
        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress);
        public Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress);
    }
}
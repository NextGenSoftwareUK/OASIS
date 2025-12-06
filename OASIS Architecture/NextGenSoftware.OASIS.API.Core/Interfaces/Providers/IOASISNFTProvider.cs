using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
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
        public OASISResult<IWeb3NFTTransactionRespone> BurnNFT(IBurnWeb3NFTRequest request);
        public Task<OASISResult<IWeb3NFTTransactionRespone>> BurnNFTAsync(IBurnWeb3NFTRequest request);
        public OASISResult<IWeb3NFTTransactionRespone> LockToken(ILockWeb3TokenRequest request);
        public Task<OASISResult<IWeb3NFTTransactionRespone>> LockTokenAsync(ILockWeb3TokenRequest request);
        public OASISResult<IWeb3NFTTransactionRespone> UnlockToken(IUnlockWeb3TokenRequest request);
        public Task<OASISResult<IWeb3NFTTransactionRespone>> UnlockTokenAsync(IUnlockWeb3TokenRequest request);
        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress);
        public Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress);
    }
}
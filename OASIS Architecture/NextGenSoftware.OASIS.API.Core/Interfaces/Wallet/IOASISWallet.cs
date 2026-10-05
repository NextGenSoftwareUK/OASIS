using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISWallet 
    {
        public List<IProviderWallet> Wallets { get; set; }
        public List<IWalletTransaction> Transactions { get; set; }
        public int Balance { get; set; }

        public OASISResult<ISendWeb4TokenResponse> SendToken(ISendWeb4TokenRequest transation);
        public Task<OASISResult<ISendWeb4TokenResponse>> SendTokenAsync(ISendWeb4TokenRequest transation);
        public OASISResult<ISendWeb4NFTResponse> SendNFT(ISendWeb4NFTRequest transation);
        public Task<OASISResult<ISendWeb4NFTResponse>> SendNFTAsync(ISendWeb4NFTRequest transation);
    }
}
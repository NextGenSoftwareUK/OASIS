using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    //TODO: Need to research how other web3 wallets work and then improve upon them in OASIS style! ;-)
    public class ProviderWallet : HolonBase, IProviderWallet
    {
        public Guid WalletId 
        { 
            get 
            {
                return base.Id;
            } 
            set
            {
                base.Id = value;
            }
        }

        [Obsolete]
        public Guid AvatarId { get; set; } //TODO: REMOVE LATER ON WHEN WE NEXT RE-SET THE DB AND AVATARS! ;-)
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public string WalletAddress { get; set; } //Hash of Public Key (shorter version).
        public string WalletAddressSegwitP2SH { get; set; }
        public string SecretRecoveryPhrase { get; set; }
        public List<IWalletTransaction> Transactions {get;set;}
        public ProviderType ProviderType { get; set; }
        public double Balance { get; set; }
        public bool IsDefaultWallet { get; set; }

        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation)
        {
            throw new System.NotImplementedException();
        }

        public Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transation)
        {
            throw new System.NotImplementedException();
        }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest transation)
        {
            throw new System.NotImplementedException();
        }

        public Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest transation)
        {
            throw new System.NotImplementedException();
        }
    }
}

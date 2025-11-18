using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        public List<IWalletTransactionRequest> Transactions {get;set;}
        public ProviderType ProviderType { get; set; }
        public double Balance { get; set; }
        public bool IsDefaultWallet { get; set; }

        public OASISResult<bool> SendNFT(IWalletTransactionRequest transation)
        {
            throw new System.NotImplementedException();
        }

        public Task<OASISResult<bool>> SendNFTAsync(IWalletTransactionRequest transation)
        {
            throw new System.NotImplementedException();
        }

        public OASISResult<bool> SendTrasaction(IWalletTransactionRequest transation)
        {
            throw new System.NotImplementedException();
        }

        public Task<OASISResult<bool>> SendTrasactionAsync(IWalletTransactionRequest transation)
        {
            throw new System.NotImplementedException();
        }
    }
}

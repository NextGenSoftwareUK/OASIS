﻿using NextGenSoftware.OASIS.API.Core.Enums;
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

        public Guid AvatarId { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public string WalletAddress { get; set; } //Hash of Public Key (shorter version).
        public string SecretRecoveryPhrase { get; set; }
        public List<IWalletTransactionRequest> Transactions {get;set;}
        public ProviderType ProviderType { get; set; }
        public int Balance { get; set; }
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

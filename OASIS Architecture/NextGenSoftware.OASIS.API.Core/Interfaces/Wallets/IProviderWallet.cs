﻿
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    //TODO: Need to research how other web3 wallets work and then improve upon them in OASIS style! ;-)
    //TODO: Implement WalletConnect v2 when C# provider has been updated to work with v2, currently it is only v1.
    //TODO: Integrate other Wallet Providers such as MetaMask, Zabo (https://zabo.com) & others...
    //TODO: Allow people to import wallets using JSON file as other providers allow (need to find out how they work and what format the JSON file is?)
    //TODO: Allow people to import wallets using SecretRecoveryPhrase (the public & private key can be dervided & calculated from it, need to find out how?) as others do such as MetaMask, etc.
    //TODO: Currently you need to link the private key and public key seperatley using the walletId for increased security, others seem to only need to import just the private key? I am guessing the public key is then dervived and calculated from the private key? Need to look into this more...

    public interface IProviderWallet : IHolonBase
    {
        public Guid WalletId { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public string WalletAddress { get; set; } //Hash of Public Key (shorter version).
        public string SecretRecoveryPhrase { get; set; }
        public List<IWalletTransactionRequest> Transactions {get;set;}
        public ProviderType ProviderType { get; set; }
        public int Balance { get; set; }
        public bool IsDefaultWallet { get; set; }

        public OASISResult<bool> SendTrasaction(IWalletTransactionRequest transation);
        public Task<OASISResult<bool>> SendTrasactionAsync(IWalletTransactionRequest transation);
        public OASISResult<bool> SendNFT(IWalletTransactionRequest transation);
        public Task<OASISResult<bool>> SendNFTAsync(IWalletTransactionRequest transation);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS
{
    public partial class PolkadotOASIS
    {



        /// <summary>
        /// Legacy storage calls have no reviewed deployed contract protocol/SDK encoder.
        /// Native wallet and asset operations use their separate real SDK owners.
        /// </summary>
        private Task<string> CreatePolkadotTransaction(string method, string data)
            => Task.FromException<string>(new NotSupportedException(
                "Polkadot contract storage is not implemented: these legacy calls have no reviewed deployed ABI or real SDK call encoder. Native wallet and asset SDK operations are separate."));

        /// <summary>
        /// Parse Polkadot blockchain response to Avatar object
        /// </summary>
        private Avatar ParsePolkadotToAvatar(string polkadotJson)
            => ParseCanonicalPolkadotRecord<Avatar>(polkadotJson);




        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        // This constructor-configured signer facade shares the SAME admitted execution,
        // exact-unit SDK preparation, durable original submission and finalized receipt owner.
        public Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
            => SendOwnedNativeTransferAsync(new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.SendWeb3TokenRequest {
                FromWalletAddress=fromWalletAddress,ToWalletAddress=toWalletAddress,Amount=amount,
                MemoText=memoText,OwnerPrivateKey=_privateKey
            });



        public void Dispose()
        {
            _lifecycleGate.Wait();
            try
            {
                if(_isDisposed)return;
                _isDisposed=true;_isActivated=false;IsProviderActivated=false;
                ReportedChainName=null;ReportedGenesisHash=null;_recordStorage.Dispose();_httpClient.Dispose();
            }
            finally{_lifecycleGate.Release();}
        }

    }
}

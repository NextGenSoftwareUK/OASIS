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
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NBitcoin;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS
{
    public partial class BitcoinOASIS
    {
        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            if (request == null)
            {
                OASISErrorHandling.HandleError(ref result, "A Bitcoin transfer request is required.");
                return Task.FromResult(result);
            }

            if (!string.IsNullOrWhiteSpace(request.FromTokenAddress))
                return MoveBitcoinRuneAsync(request.FromTokenAddress, request.FromWalletAddress, request.ToWalletAddress,
                    request.OwnerPrivateKey, request.OwnerSeedPhrase, request.Amount, burn: false);

            if (request.Web3TokenId != Guid.Empty)
            {
                OASISErrorHandling.HandleError(ref result, "Provide the existing Rune ID in block:index form for a Bitcoin token transfer.");
                return Task.FromResult(result);
            }

            string signingKey;
            try
            {
                signingKey = ResolveWalletSigningKey(request.OwnerPrivateKey, request.OwnerSeedPhrase);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Bitcoin wallet signer validation failed ({ex.GetType().Name}).");
                return Task.FromResult(result);
            }

            var memo = Encoding.UTF8.GetBytes(request.MemoText ?? string.Empty);
            return SendSignedTransactionAsync(request.FromWalletAddress, request.ToWalletAddress, request.Amount, memo,
                request.MemoText, signingKey, commitmentOnly: false);
        }

        private string ResolveWalletSigningKey(string signingKey, string seedPhrase)
        {
            if (string.IsNullOrWhiteSpace(seedPhrase))
                return string.IsNullOrWhiteSpace(signingKey) ? _privateKey : signingKey;

            var mnemonic = new Mnemonic(seedPhrase);
            if (!mnemonic.IsValidChecksum)
                throw new ArgumentException("Invalid Bitcoin seed phrase checksum.");

            using var seedKey = mnemonic.DeriveExtKey().PrivateKey;
            var seedWif = seedKey.GetWif(SigningNetwork).ToString();
            if (!string.IsNullOrWhiteSpace(signingKey) && signingKey != seedWif)
                throw new ArgumentException("Bitcoin signing key and seed phrase do not match.");
            return seedWif;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            return MintBitcoinRuneAsync(request);
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request?.TokenAddress) && !string.Equals(request.TokenAddress, "BTC", StringComparison.OrdinalIgnoreCase))
                return MoveBitcoinRuneAsync(request.TokenAddress, null, null, request.OwnerPrivateKey,
                    request.OwnerSeedPhrase, request.Amount, burn: true);
            return BurnNativeBitcoinAsync(request);
        }

    }
}

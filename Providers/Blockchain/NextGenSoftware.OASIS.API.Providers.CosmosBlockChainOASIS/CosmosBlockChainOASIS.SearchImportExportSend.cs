using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
// using Microsoft.Azure.Cosmos;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS
{
    public partial class CosmosBlockChainOASIS
    {
        public void Dispose() => _httpClient?.Dispose();
        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
            => SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).GetAwaiter().GetResult();
        public Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
            => SendBankTransferAsync(fromWalletAddress, toWalletAddress, amount, memoText, _nativeDenom, _privateKey);
        private async Task<OASISResult<ITransactionResponse>> SendBankTransferAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText, string denom, string signingKey)
        {
            var response = new OASISResult<ITransactionResponse>();
            try
            {
                if (string.IsNullOrWhiteSpace(fromWalletAddress) || string.IsNullOrWhiteSpace(toWalletAddress))
                    throw new ArgumentException("Both wallet addresses are required.");
                if (string.IsNullOrWhiteSpace(signingKey)) throw new InvalidOperationException("Sender signing key is required.");
                if (string.IsNullOrWhiteSpace(denom)) throw new ArgumentException("Bank denomination is required.");
                if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Transfer amount must be positive.");
                decimal scale = 1;
                // Native amounts are display units; other bank denominations are base units.
                if (denom == _nativeDenom)
                    for (var i = 0; i < _nativeDecimals; i++) scale *= 10;
                var units = checked(amount * scale);
                if (units != decimal.Truncate(units))
                    throw new ArgumentException("Amount exceeds the native asset precision.", nameof(amount));
                var committed = await ((CosmosSdkBackend)Backend).InvokeAsync("transfer", new
                {
                    fromWalletAddress,
                    toWalletAddress,
                    amountUnits = units.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                    denom,
                    privateKey = signingKey,
                    memo = memoText ?? string.Empty
                });
                var hash = committed.GetProperty("transactionHash").GetString();
                if (string.IsNullOrWhiteSpace(hash))
                    throw new InvalidOperationException("Cosmos SDK returned no committed transaction hash.");
                response.Result = new TransactionResponse { TransactionResult = hash };
                response.Message = "Signed Cosmos transaction committed successfully.";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error sending transaction to Cosmos: {ex.Message}");
            }
            return response;
        }


        private async Task<OASISResult<ITransactionResponse>> SendWalletTransferAsync(
            Task<OASISResult<IProviderWallet>> senderTask, Task<OASISResult<IProviderWallet>> recipientTask, decimal amount, string denomination)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                var sender = await senderTask;
                var recipient = await recipientTask;
                if (sender.IsError || sender.Result == null || recipient.IsError || recipient.Result == null)
                    throw new InvalidOperationException($"Cosmos default wallets could not be loaded. Sender: {sender.Message}; recipient: {recipient.Message}");
                return await SendBankTransferAsync(sender.Result.WalletAddress, recipient.Result.WalletAddress,
                    amount, "OASIS avatar wallet transfer", denomination, sender.Result.PrivateKey);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, ex.Message, ex);
                return result;
            }
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
            => SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).GetAwaiter().GetResult();
        public Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
            => SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, _nativeDenom);
        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
            => SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).GetAwaiter().GetResult();
        public Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
            => SendWalletTransferAsync(WalletManager.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.CosmosBlockChainOASIS, showPrivateKeys: true), WalletManager.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.CosmosBlockChainOASIS), amount, token);

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
            => SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).GetAwaiter().GetResult();
        public Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
            => SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, _nativeDenom);
        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
            => SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).GetAwaiter().GetResult();
        public Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
            => SendWalletTransferAsync(WalletManager.GetAvatarDefaultWalletByUsernameAsync(fromAvatarUsername, showPrivateKeys: true, providerType: Core.Enums.ProviderType.CosmosBlockChainOASIS), WalletManager.GetAvatarDefaultWalletByUsernameAsync(toAvatarUsername, providerType: Core.Enums.ProviderType.CosmosBlockChainOASIS), amount, token);

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
            => SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).GetAwaiter().GetResult();
        public Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
            => SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, _nativeDenom);
        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
            => SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).GetAwaiter().GetResult();
        public Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
            => SendWalletTransferAsync(WalletManager.GetAvatarDefaultWalletByEmailAsync(fromAvatarEmail, Core.Enums.ProviderType.CosmosBlockChainOASIS, showPrivateKeys: true), WalletManager.GetAvatarDefaultWalletByEmailAsync(toAvatarEmail, Core.Enums.ProviderType.CosmosBlockChainOASIS), amount, token);

        public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
            => SendTransactionById(fromAvatarId, toAvatarId, amount);
        public Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
            => SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount);
    }
}

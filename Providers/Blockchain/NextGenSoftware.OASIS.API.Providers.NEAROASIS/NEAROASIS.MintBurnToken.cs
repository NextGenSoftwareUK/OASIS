using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
    private const decimal CustomTokenScale = 100_000_000m;

    private static string ToTokenUnits(decimal amount) =>
        new BigInteger(decimal.Round(amount * CustomTokenScale, 0, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);

    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request) =>
        MintTokenAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Symbol) || request.Amount <= 0)
                throw new ArgumentException("Token symbol and a positive amount are required.");
            if (!await EnsureSdkActiveAsync(result)) return result;
            var tx = await _sdk.CallAsync("token_mint", new
            {
                symbol = request.Symbol,
                owner_id = _accountId,
                amount = ToTokenUnits(request.Amount),
                metadata_json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    request.Title, request.Description, request.MetaData, request.Tags
                })
            });
            result.Result = new TransactionResponse { TransactionResult = tx };
            result.Message = "Token minted by the deployed NEAR contract.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error minting NEAR token: {ex.Message}", ex); }
        return result;
    }

    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request) =>
        BurnTokenAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                throw new ArgumentException("Token address is required.");
            if (!await EnsureSdkActiveAsync(result)) return result;
            var tx = await _sdk.CallAsync("token_burn", new { symbol = request.TokenAddress });
            result.Result = new TransactionResponse { TransactionResult = tx };
            result.Message = "Token burned by the deployed NEAR contract.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error burning NEAR token: {ex.Message}", ex); }
        return result;
    }

    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request) =>
        LockTokenAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request) =>
        await SetTokenLockAsync(request?.TokenAddress, true);

    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request) =>
        UnlockTokenAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request) =>
        await SetTokenLockAsync(request?.TokenAddress, false);

    private async Task<OASISResult<ITransactionResponse>> SetTokenLockAsync(string symbol, bool locked)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Token address is required.");
            if (!await EnsureSdkActiveAsync(result)) return result;
            var tx = await _sdk.CallAsync("token_set_locked", new { symbol, locked });
            result.Result = new TransactionResponse { TransactionResult = tx };
            result.Message = $"Token {(locked ? "locked" : "unlocked")} by the deployed NEAR contract.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error changing NEAR token lock: {ex.Message}", ex); }
        return result;
    }

    public async Task<OASISResult<decimal>> GetCustomTokenBalanceAsync(string symbol, string accountId)
    {
        var result = new OASISResult<decimal>();
        try
        {
            if (!await EnsureSdkActiveAsync(result)) return result;
            var value = await _sdk.ViewAsync("token_balance", new { symbol, account_id = accountId });
            var units = BigInteger.Parse(value.GetString(), CultureInfo.InvariantCulture);
            result.Result = (decimal)units / CustomTokenScale;
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error reading NEAR token balance: {ex.Message}", ex); }
        return result;
    }

    public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request) =>
        GetBalanceAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
    {
        var result = new OASISResult<double>();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                throw new ArgumentException("Wallet address is required.");
            if (!await EnsureSdkActiveAsync(result)) return result;
            var yocto = BigInteger.Parse(await _sdk.GetBalanceYoctoAsync(request.WalletAddress), CultureInfo.InvariantCulture);
            result.Result = (double)((decimal)yocto / 1_000_000_000_000_000_000_000_000m);
            result.Message = "Balance read through the official near-api-js SDK.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error reading NEAR balance: {ex.Message}", ex); }
        return result;
    }

    public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request) =>
        GetTransactionsAsync(request).GetAwaiter().GetResult();

    public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
    {
        var result = new OASISResult<IList<IWalletTransaction>>();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                throw new ArgumentException("Wallet address is required.");
            if (!await EnsureSdkActiveAsync(result)) return result;
            result.Result = (await _sdk.EntriesAsync($"wallet-transaction:{request.WalletAddress}:"))
                .Select(item => (IWalletTransaction)OasisJson.Deserialize<WalletTransaction>(item.Value))
                .OrderByDescending(item => item.CreatedDate).ToList();
            result.Message = $"Retrieved {result.Result.Count} NEAR transactions recorded by OASIS contract storage.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error reading NEAR transaction history: {ex.Message}", ex); }
        return result;
    }

    private async Task RecordWalletTransactionAsync(string transactionHash, string from, string to, decimal amount,
        string description, Core.Enums.TransactionCategory category = Core.Enums.TransactionCategory.Other)
    {
        if (string.IsNullOrWhiteSpace(transactionHash)) return;
        var id = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(transactionHash)).Take(16).ToArray());
        var created = DateTime.UtcNow;
        foreach (var entry in new[]
        {
            (Address: from, Type: Core.Enums.TransactionType.Debit),
            (Address: to, Type: Core.Enums.TransactionType.Credit)
        }.Where(entry => !string.IsNullOrWhiteSpace(entry.Address)).DistinctBy(entry => entry.Address))
        {
            var transaction = new WalletTransaction
            {
                TransactionId = id,
                FromWalletAddress = from ?? string.Empty,
                ToWalletAddress = to ?? string.Empty,
                Amount = (double)amount,
                Description = description,
                CreatedDate = created,
                TransactionType = entry.Type,
                TransactionCategory = category
            };
            await _sdk.PutAsync($"wallet-transaction:{entry.Address}:{transactionHash}", OasisJson.Serialize(transaction));
        }
    }

    public OASISResult<IKeyPairAndWallet> GenerateKeyPair() => GenerateKeyPairAsync().GetAwaiter().GetResult();

    public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
    {
        var result = new OASISResult<IKeyPairAndWallet>();
        try
        {
            var nearKey = await _sdk.GenerateKeyPairAsync();
            var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
            keyPair.PublicKey = nearKey.PublicKey;
            keyPair.PrivateKey = nearKey.PrivateKey;
            keyPair.WalletAddressLegacy = nearKey.ImplicitAccountId;
            result.Result = keyPair;
            result.Message = "Ed25519 NEAR key pair generated by near-api-js.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error generating NEAR key pair: {ex.Message}", ex); }
        return result;
    }

    public OASISResult<IKeyPairAndWallet> GenerateKeyPair(IGetWeb3WalletBalanceRequest request) => GenerateKeyPair();
    public Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync(IGetWeb3WalletBalanceRequest request) => GenerateKeyPairAsync();
}

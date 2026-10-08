using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aptos;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using KeyPairAndWallet = NextGenSoftware.Utilities.KeyHelper.KeyPairAndWallet;
using OasisTransactionResponse = NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS;

public partial class AptosOASIS
{
    private const string TokenRecordType = "token";
    private sealed class AptosTokenState
    {
        public string Symbol { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Supply { get; set; }
        public bool Locked { get; set; }
        public Dictionary<string, decimal> Balances { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> MetaData { get; set; } = new();
    }

    private static OASISResult<ITransactionResponse> TxError(string message, Exception ex = null) => new() { IsError = true, Message = message, Exception = ex };
    private static OASISResult<ITransactionResponse> TxOk(string hash, string message) => new(new OasisTransactionResponse { TransactionResult = hash }) { Message = message };
    private static string NormalizeAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.StartsWith("0x", StringComparison.Ordinal))
            normalized = normalized[2..];
        normalized = normalized.TrimStart('0');
        return $"0x{(normalized.Length == 0 ? "0" : normalized)}";
    }

    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request) => SendTokenAsync(request).Result;
    public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        try
        {
            if (!_isActivated) return TxError("Aptos provider is not activated.");
            EnsureTransactionAccount();
            if (request.Amount <= 0 || string.IsNullOrWhiteSpace(request.ToWalletAddress)) return TxError("A positive amount and destination address are required.");
            var sender = _account.Address.ToString();
            if (!string.IsNullOrWhiteSpace(request.FromWalletAddress) && NormalizeAddress(request.FromWalletAddress) != NormalizeAddress(sender))
                return TxError("FromWalletAddress does not match the configured Aptos signing account.");

            if (string.IsNullOrWhiteSpace(request.FromTokenAddress) || request.FromTokenAddress == "0x1::aptos_coin::AptosCoin")
            {
                var octas = checked((ulong)decimal.Round(request.Amount * 100_000_000m, 0, MidpointRounding.AwayFromZero));
                var hash = await ExecuteEntryFunctionAsync("0x1::aptos_account::transfer", Array.Empty<object>(), request.ToWalletAddress, octas);
                return TxOk(hash, "APT transferred by an Aptos SDK signed transaction.");
            }

            var state = await GetRecordAsync<AptosTokenState>(TokenRecordType, request.FromTokenAddress);
            if (state == null) return TxError($"Custom Aptos token '{request.FromTokenAddress}' was not found.");
            if (state.Locked) return TxError($"Custom Aptos token '{state.Symbol}' is locked.");
            sender = NormalizeAddress(sender); var receiver = NormalizeAddress(request.ToWalletAddress);
            state.Balances.TryGetValue(sender, out var senderBalance);
            if (senderBalance < request.Amount) return TxError("Insufficient custom token balance.");
            state.Balances[sender] = senderBalance - request.Amount;
            state.Balances.TryGetValue(receiver, out var receiverBalance); state.Balances[receiver] = receiverBalance + request.Amount;
            var customHash = await UpsertRecordWithHashAsync(TokenRecordType, state.Symbol, state);
            return TxOk(customHash, "Custom OASIS token transferred in Aptos Move storage.");
        }
        catch (Exception ex) { return TxError($"Error sending Aptos token: {ex.Message}", ex); }
    }

    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request) => MintTokenAsync(request).Result;
    public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Symbol) || request.Amount <= 0) return TxError("Token symbol and positive amount are required.");
            if (await HasRecordAsync(TokenRecordType, request.Symbol)) return TxError($"Token '{request.Symbol}' already exists.");
            var state = new AptosTokenState { Symbol = request.Symbol, Title = request.Title, Description = request.Description, Supply = request.Amount, MetaData = request.MetaData ?? new() };
            state.Balances[NormalizeAddress(_account.Address.ToString())] = request.Amount;
            var hash = await UpsertRecordWithHashAsync(TokenRecordType, state.Symbol, state);
            return TxOk(hash, $"Custom OASIS token '{state.Symbol}' minted in Aptos Move storage.");
        }
        catch (Exception ex) { return TxError($"Error minting Aptos token: {ex.Message}", ex); }
    }

    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request) => BurnTokenAsync(request).Result;
    public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        try { var hash = await DeleteRecordWithHashAsync(TokenRecordType, request.TokenAddress); return TxOk(hash, $"Custom OASIS token '{request.TokenAddress}' burned on Aptos."); }
        catch (Exception ex) { return TxError($"Error burning Aptos token: {ex.Message}", ex); }
    }

    public async Task<OASISResult<decimal>> GetCustomTokenBalanceAsync(string symbol, string walletAddress)
    {
        try
        {
            var state = await GetRecordAsync<AptosTokenState>(TokenRecordType, symbol);
            if (state == null)
                return new OASISResult<decimal> { IsError = true, Message = $"Custom Aptos token '{symbol}' was not found." };

            state.Balances.TryGetValue(NormalizeAddress(walletAddress), out var balance);
            return new OASISResult<decimal>(balance)
            {
                Message = $"Custom Aptos token '{symbol}' balance retrieved from signed Move storage."
            };
        }
        catch (Exception ex)
        {
            return new OASISResult<decimal> { IsError = true, Message = ex.Message, Exception = ex };
        }
    }

    private async Task<OASISResult<ITransactionResponse>> SetTokenLockAsync(string symbol, bool locked)
    {
        try
        {
            var state = await GetRecordAsync<AptosTokenState>(TokenRecordType, symbol);
            if (state == null) return TxError($"Custom Aptos token '{symbol}' was not found.");
            state.Locked = locked;
            var hash = await UpsertRecordWithHashAsync(TokenRecordType, symbol, state);
            return TxOk(hash, locked ? "Custom Aptos token locked." : "Custom Aptos token unlocked.");
        }
        catch (Exception ex) { return TxError(ex.Message, ex); }
    }
    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request) => LockTokenAsync(request).Result;
    public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request) => SetTokenLockAsync(request.TokenAddress, true);
    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request) => UnlockTokenAsync(request).Result;
    public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request) => SetTokenLockAsync(request.TokenAddress, false);

    public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request) => GetBalanceAsync(request).Result;
    public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
    {
        try { return new(await GetNativeAptBalanceOctasAsync(request.WalletAddress) / 100_000_000d) { Message = "Native APT balance retrieved through the Aptos SDK." }; }
        catch (Exception ex) { return new() { IsError = true, Message = ex.Message, Exception = ex }; }
    }

    public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request) => GetTransactionsAsync(request).Result;
    public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
    {
        var result = new OASISResult<IList<IWalletTransaction>>();
        try
        {
            using var response = await _httpClient.GetAsync($"/v1/accounts/{request.WalletAddress}/transactions?limit=100"); response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var list = new List<IWalletTransaction>();
            foreach (var tx in json.RootElement.EnumerateArray())
            {
                var hash = tx.TryGetProperty("hash", out var h) ? h.GetString() : tx.GetRawText();
                list.Add(new WalletTransaction { TransactionId = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(hash)).Take(16).ToArray()), FromWalletAddress = tx.TryGetProperty("sender", out var s) ? s.GetString() : "", Description = $"Aptos transaction: {hash}" });
            }
            result.Result = list; result.Message = $"Retrieved {list.Count} Aptos transactions.";
        }
        catch (Exception ex) { result.IsError = true; result.Message = ex.Message; result.Exception = ex; }
        return result;
    }

    public OASISResult<IKeyPairAndWallet> GenerateKeyPair() => GenerateKeyPairAsync().Result;
    public Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
    {
        var account = Ed25519Account.Generate();
        return Task.FromResult(new OASISResult<IKeyPairAndWallet>(new KeyPairAndWallet { PrivateKey = account.PrivateKey.ToAIP80String(), PublicKey = account.PublicKey.ToString(), WalletAddressLegacy = account.Address.ToString() }) { Message = "Aptos SDK key pair generated." });
    }
    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string address, CancellationToken token = default) => new(await GetNativeAptBalanceOctasAsync(address) / 100_000_000m);
    public Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
    {
        var mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve); var account = Ed25519Account.FromDerivationPath("m/44'/637'/0'/0'/0'", mnemonic.ToString());
        return Task.FromResult(new OASISResult<(string, string, string)>((account.PublicKey.ToString(), account.PrivateKey.ToAIP80String(), mnemonic.ToString())));
    }
    public Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
    {
        try { var account = seedPhrase.Contains(' ') ? Ed25519Account.FromDerivationPath("m/44'/637'/0'/0'/0'", seedPhrase) : new Ed25519Account(new Ed25519PrivateKey(seedPhrase, false)); return Task.FromResult(new OASISResult<(string, string)>((account.PublicKey.ToString(), account.PrivateKey.ToAIP80String()))); }
        catch (Exception ex) { return Task.FromResult(new OASISResult<(string, string)> { IsError = true, Message = ex.Message, Exception = ex }); }
    }
}

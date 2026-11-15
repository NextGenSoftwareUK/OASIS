using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.RPC;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

public partial class BitcoinOASIS
{
    private static readonly Script BitcoinBurnScript = new(new byte[] { 0x6a, 0x00, 0x02, 0x00, 0x00 });

    private static HttpClient CreateOrdClient()
    {
        var endpoint = Environment.GetEnvironmentVariable("OASIS_BITCOIN_ORD_API");
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var ord) ||
            (ord.Scheme != Uri.UriSchemeHttps && !(ord.Scheme == Uri.UriSchemeHttp && ord.IsLoopback)) ||
            ord.UserInfo.Length != 0 || ord.Query.Length != 0 || ord.Fragment.Length != 0)
            throw new InvalidOperationException("Configure OASIS_BITCOIN_ORD_API with an HTTPS or loopback ord server root.");

        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(ord.AbsoluteUri.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        return client;
    }

    private async Task<OASISResult<IWeb3NFT>> ReadOrdinalInscriptionAsync(string identifier, HttpClient client = null)
    {
        var result = new OASISResult<IWeb3NFT>();
        try
        {
            if (identifier == null || identifier.Length < 66 || identifier[64] != 'i' ||
                !uint256.TryParse(identifier[..64], out var reveal) ||
                !uint.TryParse(identifier.AsSpan(65), NumberStyles.None, CultureInfo.InvariantCulture, out var index))
                throw new ArgumentException("A Bitcoin Ordinals inscription ID is required.", nameof(identifier));

            var inscriptionId = reveal + "i" + index.ToString(CultureInfo.InvariantCulture);
            using var ownedClient = client == null ? CreateOrdClient() : null;
            client ??= ownedClient;
            var genesis = await client.GetFromJsonAsync<string>("r/blockhash/0");
            if (!uint256.TryParse(genesis, out var genesisHash) || genesisHash != SigningNetwork.GetGenesis().GetHash())
                throw new InvalidOperationException("The ord server is indexing a different Bitcoin network.");

            using var response = await client.GetAsync("inscription/" + inscriptionId);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                OASISErrorHandling.HandleError(ref result, "The configured ord server has no matching Bitcoin inscription.");
                return result;
            }
            response.EnsureSuccessStatusCode();

            using var data = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var inscription = data.RootElement;
            if (inscription.GetProperty("id").GetString() != inscriptionId)
                throw new InvalidOperationException("The ord server returned a different inscription ID.");

            var metadata = new Dictionary<string, string>
            {
                ["InscriptionNumber"] = inscription.GetProperty("number").GetInt64().ToString(CultureInfo.InvariantCulture),
                ["InscriptionHeight"] = inscription.GetProperty("height").GetInt64().ToString(CultureInfo.InvariantCulture),
                ["Satpoint"] = inscription.GetProperty("satpoint").GetString(),
                ["ContentUrl"] = new Uri(client.BaseAddress, "content/" + inscriptionId).AbsoluteUri
            };
            foreach (var charm in inscription.GetProperty("charms").EnumerateArray())
                if (charm.GetString() == "burned")
                    metadata["Burned"] = "true";
            if (inscription.GetProperty("address").GetString() is string owner)
                metadata["CurrentOwnerAddress"] = BitcoinAddress.Create(owner, SigningNetwork).ToString();
            if (inscription.GetProperty("content_type").GetString() is string contentType)
                metadata["ContentType"] = contentType;

            result.Result = new Web3NFT
            {
                TokenId = inscriptionId,
                NFTTokenAddress = inscriptionId,
                MintTransactionHash = reveal.ToString(),
                MintedOn = DateTimeOffset.FromUnixTimeSeconds(inscription.GetProperty("timestamp").GetInt64()).UtcDateTime,
                OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.BitcoinOASIS),
                StoreNFTMetaDataOnChain = true,
                MetaData = metadata
            };
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex is ArgumentException or InvalidOperationException
                ? ex.Message : $"Bitcoin inscription lookup failed ({ex.GetType().Name}).");
        }

        return result;
    }

    private Task<OASISResult<IWeb3NFTTransactionResponse>> SendOrdinalInscriptionAsync(ISendWeb3NFTRequest request)
    {
        var identifier = request?.FromNFTTokenAddress;
        if (string.IsNullOrWhiteSpace(identifier))
            identifier = string.IsNullOrWhiteSpace(request?.TokenAddress) ? request?.TokenId : request.TokenAddress;
        return MoveOrdinalInscriptionAsync(identifier, request?.FromWalletAddress, request?.ToWalletAddress,
            request?.OwnerPrivateKey, request?.OwnerSeedPhrase, request?.MemoText, burn: false);
    }

    private async Task<OASISResult<IWeb3NFTTransactionResponse>> MoveOrdinalInscriptionAsync(string identifier,
        string sourceAddress, string recipientAddress, string privateKey, string seedPhrase, string memo, bool burn,
        OutPoint expectedSource = null)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            if (string.IsNullOrWhiteSpace(identifier) ||
                (!burn && (string.IsNullOrWhiteSpace(sourceAddress) || string.IsNullOrWhiteSpace(recipientAddress))))
                throw new ArgumentException("The inscription ID is required. Transfers also require a source and destination address.");

            if (!_isActivated)
            {
                var activation = await ActivateProviderAsync();
                if (activation.IsError)
                    throw new InvalidOperationException("Could not activate the Bitcoin provider for the inscription transaction.");
            }

            var network = SigningNetwork;
            if (!uint256.TryParse((await _httpClient.GetStringAsync("block-height/0")).Trim(), out var genesis) ||
                genesis != network.GetGenesis().GetHash())
                throw new InvalidOperationException("Esplora is serving a different Bitcoin network.");

            using var ord = CreateOrdClient();
            var tip = await ReadSharedOrdinalTipAsync(ord);
            var inscription = await ReadOrdinalInscriptionAsync(identifier, ord);
            if (inscription.IsError)
            {
                result.ErrorCode = inscription.ErrorCode;
                OASISErrorHandling.HandleError(ref result, inscription.Message);
                return result;
            }

            if (!inscription.Result.MetaData.TryGetValue("CurrentOwnerAddress", out var indexedOwner))
                throw new InvalidOperationException("The inscription has no spendable indexed owner.");
            if (burn && string.IsNullOrWhiteSpace(privateKey) && string.IsNullOrWhiteSpace(seedPhrase))
                throw new ArgumentException("A Bitcoin inscription burn requires the owner signing key.");

            var source = BitcoinAddress.Create(burn ? indexedOwner : sourceAddress, network);
            var destination = burn
                ? BitcoinBurnScript
                : BitcoinAddress.Create(recipientAddress, network).ScriptPubKey;
            var signer = new BitcoinSecret(ResolveWalletSigningKey(privateKey, seedPhrase), network);
            var wrappedSegwit = source.Equals(signer.GetAddress(ScriptPubKeyType.SegwitP2SH));
            if (!wrappedSegwit && !source.Equals(signer.GetAddress(ScriptPubKeyType.Legacy)) &&
                !source.Equals(signer.GetAddress(ScriptPubKeyType.Segwit)) &&
                !source.Equals(signer.GetAddress(ScriptPubKeyType.TaprootBIP86)))
                throw new ArgumentException("The Bitcoin inscription source must belong to the signing key.");

            var satpoint = inscription.Result.MetaData["Satpoint"]?.Split(':');
            if (satpoint == null || satpoint.Length != 3 || !uint256.TryParse(satpoint[0], out var txid) ||
                !uint.TryParse(satpoint[1], NumberStyles.None, CultureInfo.InvariantCulture, out var vout) ||
                !long.TryParse(satpoint[2], NumberStyles.None, CultureInfo.InvariantCulture, out var offset))
                throw new InvalidOperationException("The inscription has no spendable indexed satpoint.");

            var inscribedOutpoint = new OutPoint(txid, vout);
            if (expectedSource != null && !expectedSource.Equals(inscribedOutpoint))
                throw new InvalidOperationException("The inscription no longer occupies the reviewed custody output.");
            var inscribedCoin = await ReadOrdinalCoinAsync(ord, inscribedOutpoint, source, signer, wrappedSegwit, inscription.Result.TokenId);
            if (inscribedCoin == null || offset >= inscribedCoin.TxOut.Value.Satoshi)
                throw new InvalidOperationException("The source output cannot move this inscription independently.");

            var feeRate = await ReadBitcoinPaymentFeeRateAsync();
            var prefix = new TxOut(Money.Satoshis(offset), source.ScriptPubKey);
            var requiresPadding = burn && offset > 0 && prefix.IsDust();
            Coin padding = null;
            using var utxos = JsonDocument.Parse(await _httpClient.GetStringAsync($"address/{source}/utxo"));
            foreach (var utxo in utxos.RootElement.EnumerateArray())
            {
                if (!utxo.GetProperty("status").GetProperty("confirmed").GetBoolean() ||
                    !uint256.TryParse(utxo.GetProperty("txid").GetString(), out var feeTxid))
                    continue;

                var feeOutpoint = new OutPoint(feeTxid, utxo.GetProperty("vout").GetUInt32());
                if (feeOutpoint.Equals(inscribedOutpoint))
                    continue;

                var feeCoin = await ReadOrdinalCoinAsync(ord, feeOutpoint, source, signer, wrappedSegwit, null);
                if (feeCoin == null)
                    continue;

                if (requiresPadding && padding == null)
                {
                    if (prefix.Value + feeCoin.TxOut.Value >= prefix.GetDustThreshold())
                        padding = feeCoin;
                    continue;
                }

                var builder = network.CreateTransactionBuilder().AddCoins(inscribedCoin, feeCoin).AddKeys(signer.PrivateKey);
                var transaction = network.CreateTransaction();
                if (padding != null)
                {
                    builder.AddCoins(padding);
                    transaction.Inputs.Add(padding.Outpoint);
                }
                transaction.Inputs.Add(inscribedCoin.Outpoint);
                transaction.Inputs.Add(feeCoin.Outpoint);

                // Input/output order determines the satoshi destination. A burn sends exactly one satoshi to OP_RETURN.
                if (burn && offset > 0)
                    transaction.Outputs.Add(prefix.Value + (padding?.TxOut.Value ?? Money.Zero), source.ScriptPubKey);
                transaction.Outputs.Add(burn ? Money.Satoshis(1) : inscribedCoin.TxOut.Value, destination);

                var changeAmount = feeCoin.TxOut.Value;
                if (burn)
                    changeAmount += inscribedCoin.TxOut.Value - Money.Satoshis(offset + 1);
                var change = transaction.Outputs.Add(changeAmount, source.ScriptPubKey);
                var fee = builder.EstimateFees(transaction, feeRate);
                if (fee >= change.Value)
                    continue;
                change.Value -= fee;
                if (change.IsDust())
                    continue;

                builder.SignTransactionInPlace(transaction);
                if (!builder.Verify(transaction))
                    continue;
                if (await ReadSharedOrdinalTipAsync(ord) != tip)
                    throw new InvalidOperationException("The Bitcoin chain advanced during inscription transaction preparation. Recheck the indexed inputs.");

                var sent = burn
                    ? await BroadcastBitcoinBurnAsync(transaction, Money.Satoshis(1), tip)
                    : await BroadcastSignedBitcoinAsync(transaction, memo);
                foreach (var entry in sent.MetaData)
                    result.MetaData[entry.Key] = entry.Value;
                if (sent.IsError)
                {
                    result.ErrorCode = sent.ErrorCode;
                    OASISErrorHandling.HandleError(ref result, sent.Message);
                    return result;
                }

                var transactionId = sent.Result.TransactionResult;
                if (burn)
                    inscription.Result.MetaData["BurnTransactionId"] = transactionId;
                else
                    inscription.Result.SendNFTTransactionHash = transactionId;
                result.Result = new Web3NFTTransactionResponse
                {
                    TransactionResult = transactionId,
                    SendNFTTransactionResult = burn ? null : transactionId,
                    Web3NFT = inscription.Result
                };
                return result;
            }

            throw new InvalidOperationException("The signing address has insufficient confirmed inscription-free and rune-free funding.");
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex is ArgumentException or InvalidOperationException
                ? ex.Message : $"Bitcoin inscription transaction failed ({ex.GetType().Name}).");
        }

        return result;
    }

    private static HttpClient CreateBitcoinCoreTransport() =>
        new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(30) };

    private RPCClient CreateBitcoinCoreClient(HttpClient transport)
    {
        var endpoint = Environment.GetEnvironmentVariable("OASIS_BITCOIN_CORE_RPC_URL");
        var credentials = Environment.GetEnvironmentVariable("OASIS_BITCOIN_CORE_RPC_AUTH");
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var address) ||
            (address.Scheme != Uri.UriSchemeHttps && !(address.Scheme == Uri.UriSchemeHttp && address.IsLoopback)) ||
            address.UserInfo.Length != 0 || address.Query.Length != 0 || address.Fragment.Length != 0 ||
            string.IsNullOrWhiteSpace(credentials))
            throw new InvalidOperationException("Bitcoin Ordinals publishing requires a protected Bitcoin Core RPC endpoint and credentials.");

        return new RPCClient(credentials, address, SigningNetwork) { HttpClient = transport };
    }

    private async Task<OASISResult<ITransactionResponse>> BroadcastBitcoinBurnAsync(Transaction transaction, Money amount, uint256 tip)
    {
        using var transport = CreateBitcoinCoreTransport();
        var core = CreateBitcoinCoreClient(transport);
        if (await core.GetBestBlockHashAsync() != tip)
            throw new InvalidOperationException("Bitcoin Core does not have the same chain tip as ord and Esplora.");

        var result = new OASISResult<ITransactionResponse>();
        var transactionId = transaction.GetHash();
        result.MetaData[BitcoinTransactionIdMetadataKey] = transactionId.ToString();
        try
        {
            // Core defaults to refusing burns. Authorize only the intended amount, with its default fee-rate limit.
            var receipt = await core.SendCommandAsync("sendrawtransaction", transaction.ToHex(), 0.10m, amount.ToDecimal(MoneyUnit.BTC));
            if (!uint256.TryParse(receipt.Result?.ToString(), out var accepted) || accepted != transactionId)
                throw new InvalidOperationException("Bitcoin Core returned an unexpected burn transaction ID.");

            result.Result = new BitcoinTransactionResponse { TransactionResult = transactionId.ToString() };
        }
        catch (RPCException ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Bitcoin Core rejected burn {transactionId} ({ex.RPCCode}).");
        }
        catch (Exception ex)
        {
            result.ErrorCode = BitcoinBroadcastOutcomeUnknown;
            OASISErrorHandling.HandleError(ref result, $"Bitcoin Core did not acknowledge burn {transactionId} ({ex.GetType().Name}).");
        }

        return result;
    }

    private async Task<uint256> ReadSharedOrdinalTipAsync(HttpClient ord)
    {
        using var status = JsonDocument.Parse(await ord.GetStringAsync("status"));
        if (!status.RootElement.GetProperty("rune_index").GetBoolean() ||
            status.RootElement.GetProperty("unrecoverably_reorged").GetBoolean())
            throw new InvalidOperationException("Bitcoin inscription transactions require a healthy ord server with Rune indexing enabled.");

        var ordTip = await ord.GetFromJsonAsync<string>("r/blockhash");
        var esploraTip = (await _httpClient.GetStringAsync("blocks/tip/hash")).Trim();
        if (!uint256.TryParse(ordTip, out var ordHash) || !uint256.TryParse(esploraTip, out var esploraHash) ||
            ordHash != esploraHash)
            throw new InvalidOperationException("The Bitcoin Ordinals index and Esplora do not have the same indexed chain tip.");
        return ordHash;
    }

    private async Task<Coin> ReadOrdinalCoinAsync(HttpClient ord, OutPoint outpoint, BitcoinAddress source,
        BitcoinSecret signer, bool wrappedSegwit, string inscriptionId)
    {
        var funding = await ReadOrdinalFundingAsync(ord, outpoint, source, signer, wrappedSegwit, inscriptionId);
        return funding.Coin;
    }

    private async Task<(Coin Coin, BigInteger RuneUnits)> ReadOrdinalFundingAsync(HttpClient ord, OutPoint outpoint, BitcoinAddress source,
        BitcoinSecret signer, bool wrappedSegwit, string inscriptionId, string expectedRune = null, bool requireOnlyRune = false)
    {
        var expectedOutpoint = outpoint.Hash + ":" + outpoint.N.ToString(CultureInfo.InvariantCulture);
        using var data = JsonDocument.Parse(await ord.GetStringAsync("output/" + expectedOutpoint));
        var output = data.RootElement;
        var inscriptions = output.GetProperty("inscriptions");
        var runes = output.GetProperty("runes");
        if (runes.ValueKind != JsonValueKind.Object ||
            output.GetProperty("outpoint").GetString() != expectedOutpoint ||
            !output.GetProperty("indexed").GetBoolean() || output.GetProperty("spent").GetBoolean() ||
            (expectedRune == null && runes.EnumerateObject().MoveNext()) ||
            (requireOnlyRune && runes.EnumerateObject().Any(balance => balance.Name != expectedRune)) ||
            (inscriptionId == null ? inscriptions.GetArrayLength() != 0 :
                inscriptions.GetArrayLength() != 1 || inscriptions[0].GetString() != inscriptionId))
            return default;

        var transaction = await ReadVerifiedTransactionAsync(outpoint.Hash);
        if (transaction.IsCoinBase || outpoint.N >= transaction.Outputs.Count)
            return default;

        var previous = transaction.Outputs[(int)outpoint.N];
        if (previous.ScriptPubKey != source.ScriptPubKey || previous.Value.Satoshi != output.GetProperty("value").GetInt64())
            return default;

        using var spent = JsonDocument.Parse(await _httpClient.GetStringAsync($"tx/{outpoint.Hash}/outspend/{outpoint.N}"));
        if (spent.RootElement.GetProperty("spent").GetBoolean())
            return default;

        var coin = wrappedSegwit
            ? new ScriptCoin(outpoint, previous, signer.PrivateKey.PubKey.WitHash.ScriptPubKey)
            : new Coin(outpoint, previous);
        var units = expectedRune != null && runes.TryGetProperty(expectedRune, out var balance)
            ? BigInteger.Parse(balance.GetProperty("amount").GetRawText(), CultureInfo.InvariantCulture)
            : BigInteger.Zero;
        if (units < BigInteger.Zero || units > UInt128.MaxValue)
            throw new InvalidOperationException("The indexed Rune balance is outside the protocol range.");
        return (coin, units);
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

public partial class BitcoinOASIS
{
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

    private async Task<OASISResult<IWeb3NFTTransactionResponse>> SendOrdinalInscriptionAsync(ISendWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FromWalletAddress) ||
                string.IsNullOrWhiteSpace(request.ToWalletAddress))
                throw new ArgumentException("The Bitcoin inscription transfer requires a source and destination address.");

            if (!_isActivated)
            {
                var activation = await ActivateProviderAsync();
                if (activation.IsError)
                    throw new InvalidOperationException("Could not activate the Bitcoin provider for the inscription transfer.");
            }

            var network = SigningNetwork;
            var source = BitcoinAddress.Create(request.FromWalletAddress, network);
            var recipient = BitcoinAddress.Create(request.ToWalletAddress, network);
            var signer = new BitcoinSecret(ResolveWalletSigningKey(request.OwnerPrivateKey, request.OwnerSeedPhrase), network);
            var wrappedSegwit = source.Equals(signer.GetAddress(ScriptPubKeyType.SegwitP2SH));
            if (!wrappedSegwit && !source.Equals(signer.GetAddress(ScriptPubKeyType.Legacy)) &&
                !source.Equals(signer.GetAddress(ScriptPubKeyType.Segwit)) &&
                !source.Equals(signer.GetAddress(ScriptPubKeyType.TaprootBIP86)))
                throw new ArgumentException("The Bitcoin inscription source must belong to the signing key.");

            if (!uint256.TryParse((await _httpClient.GetStringAsync("block-height/0")).Trim(), out var genesis) ||
                genesis != network.GetGenesis().GetHash())
                throw new InvalidOperationException("Esplora is serving a different Bitcoin network.");

            using var ord = CreateOrdClient();
            var tip = await ReadSharedOrdinalTipAsync(ord);
            var identifier = request.FromNFTTokenAddress;
            if (string.IsNullOrWhiteSpace(identifier))
                identifier = string.IsNullOrWhiteSpace(request.TokenAddress) ? request.TokenId : request.TokenAddress;
            var inscription = await ReadOrdinalInscriptionAsync(identifier, ord);
            if (inscription.IsError)
            {
                result.ErrorCode = inscription.ErrorCode;
                OASISErrorHandling.HandleError(ref result, inscription.Message);
                return result;
            }

            var satpoint = inscription.Result.MetaData["Satpoint"]?.Split(':');
            if (satpoint == null || satpoint.Length != 3 || !uint256.TryParse(satpoint[0], out var txid) ||
                !uint.TryParse(satpoint[1], NumberStyles.None, CultureInfo.InvariantCulture, out var vout) ||
                !long.TryParse(satpoint[2], NumberStyles.None, CultureInfo.InvariantCulture, out var offset))
                throw new InvalidOperationException("The inscription has no spendable indexed satpoint.");

            var inscribedOutpoint = new OutPoint(txid, vout);
            var inscribedCoin = await ReadOrdinalCoinAsync(ord, inscribedOutpoint, source, signer, wrappedSegwit, inscription.Result.TokenId);
            if (inscribedCoin == null || offset >= inscribedCoin.TxOut.Value.Satoshi)
                throw new InvalidOperationException("The source output cannot transfer this inscription independently.");

            var feeRate = await ReadBitcoinPaymentFeeRateAsync();
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

                var builder = network.CreateTransactionBuilder().AddCoins(inscribedCoin, feeCoin).AddKeys(signer.PrivateKey);
                var transaction = network.CreateTransaction();
                // Ordinal satoshis are assigned in input/output order. Preserve the full inscription output value.
                transaction.Inputs.Add(inscribedCoin.Outpoint);
                transaction.Inputs.Add(feeCoin.Outpoint);
                transaction.Outputs.Add(inscribedCoin.TxOut.Value, recipient.ScriptPubKey);
                var change = transaction.Outputs.Add(feeCoin.TxOut.Value, source.ScriptPubKey);
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
                    throw new InvalidOperationException("The Bitcoin chain advanced during inscription transfer preparation. Recheck the indexed inputs.");

                var sent = await BroadcastSignedBitcoinAsync(transaction, request.MemoText);
                foreach (var entry in sent.MetaData)
                    result.MetaData[entry.Key] = entry.Value;
                if (sent.IsError)
                {
                    result.ErrorCode = sent.ErrorCode;
                    OASISErrorHandling.HandleError(ref result, sent.Message);
                    return result;
                }

                inscription.Result.SendNFTTransactionHash = sent.Result.TransactionResult;
                result.Result = new Web3NFTTransactionResponse
                {
                    TransactionResult = sent.Result.TransactionResult,
                    Web3NFT = inscription.Result
                };
                return result;
            }

            throw new InvalidOperationException("No confirmed inscription-free and rune-free output from the signing address could pay the network fee.");
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex is ArgumentException or InvalidOperationException
                ? ex.Message : $"Bitcoin inscription transfer failed ({ex.GetType().Name}).");
        }

        return result;
    }

    private async Task<uint256> ReadSharedOrdinalTipAsync(HttpClient ord)
    {
        using var status = JsonDocument.Parse(await ord.GetStringAsync("status"));
        if (!status.RootElement.GetProperty("rune_index").GetBoolean() ||
            status.RootElement.GetProperty("unrecoverably_reorged").GetBoolean())
            throw new InvalidOperationException("Bitcoin inscription transfers require a healthy ord server with Rune indexing enabled.");

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
        var expectedOutpoint = outpoint.Hash + ":" + outpoint.N.ToString(CultureInfo.InvariantCulture);
        using var data = JsonDocument.Parse(await ord.GetStringAsync("output/" + expectedOutpoint));
        var output = data.RootElement;
        var inscriptions = output.GetProperty("inscriptions");
        if (output.GetProperty("outpoint").GetString() != expectedOutpoint ||
            !output.GetProperty("indexed").GetBoolean() || output.GetProperty("spent").GetBoolean() ||
            output.GetProperty("runes").EnumerateObject().MoveNext() ||
            (inscriptionId == null ? inscriptions.GetArrayLength() != 0 :
                inscriptions.GetArrayLength() != 1 || inscriptions[0].GetString() != inscriptionId))
            return null;

        var transaction = await ReadVerifiedTransactionAsync(outpoint.Hash);
        if (transaction.IsCoinBase || outpoint.N >= transaction.Outputs.Count)
            return null;

        var previous = transaction.Outputs[(int)outpoint.N];
        if (previous.ScriptPubKey != source.ScriptPubKey || previous.Value.Satoshi != output.GetProperty("value").GetInt64())
            return null;

        using var spent = JsonDocument.Parse(await _httpClient.GetStringAsync($"tx/{outpoint.Hash}/outspend/{outpoint.N}"));
        if (spent.RootElement.GetProperty("spent").GetBoolean())
            return null;

        return wrappedSegwit
            ? new ScriptCoin(outpoint, previous, signer.PrivateKey.PubKey.WitHash.ScriptPubKey)
            : new Coin(outpoint, previous);
    }
}

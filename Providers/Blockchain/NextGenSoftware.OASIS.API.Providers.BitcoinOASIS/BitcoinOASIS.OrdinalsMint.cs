using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

public partial class BitcoinOASIS
{
    private const int OrdinalPostageSatoshis = 10_000;
    private const int MaximumOrdinalBodyBytes = 350_000;

    private async Task<OASISResult<IWeb3NFTTransactionResponse>> MintOrdinalInscriptionAsync(IMintWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>();
        try
        {
            if (request == null || (request.NumberToMint ?? 1) != 1)
                throw new ArgumentException("A Bitcoin inscription mint request must specify one inscription.");
            if (request.WaitTillNFTMinted == true &&
                ((request.WaitForNFTToMintInSeconds ?? 180) <= 0 || (request.AttemptToMintEveryXSeconds ?? 1) <= 0))
                throw new ArgumentException("Bitcoin mint confirmation requires a positive timeout and polling interval.");

            var content = SerializeOrdinalMintBody(request);
            if (!_isActivated)
            {
                var activation = await ActivateProviderAsync();
                if (activation.IsError)
                    throw new InvalidOperationException("Could not activate the Bitcoin provider for inscription minting.");
            }

            var network = SigningNetwork;
            var signer = new BitcoinSecret(_privateKey, network);
            var source = signer.GetAddress(ScriptPubKeyType.TaprootBIP86);
            var recipient = string.IsNullOrWhiteSpace(request.SendToAddressAfterMinting)
                ? source
                : BitcoinAddress.Create(request.SendToAddressAfterMinting, network);

            using var ord = CreateOrdClient();
            using var transport = CreateBitcoinCoreTransport();
            var core = CreateBitcoinCoreClient(transport);
            var tip = await ReadSharedOrdinalTipAsync(ord);
            if (await core.GetBlockHashAsync(0) != network.GetGenesis().GetHash() ||
                await core.GetBestBlockHashAsync() != tip)
                throw new InvalidOperationException("Bitcoin Core is not serving the configured indexed chain.");

            var publicKey = signer.PrivateKey.PubKey.TaprootInternalKey;
            var tapScript = CreateOrdinalTapScript(publicKey, content);
            var commitment = publicKey.GetTaprootFullPubKey(tapScript.LeafHash);
            var controlBlock = new byte[33];
            controlBlock[0] = (byte)(0xc0 | (commitment.OutputKeyParity ? 1 : 0));
            publicKey.ToBytes().CopyTo(controlBlock, 1);
            var scriptBytes = tapScript.Script.ToBytes();
            var feeRate = await ReadBitcoinPaymentFeeRateAsync();

            var reveal = network.CreateTransaction();
            reveal.Version = 2;
            reveal.Inputs.Add(new OutPoint(uint256.Zero, 0));
            reveal.Outputs.Add(Money.Satoshis(OrdinalPostageSatoshis), recipient.ScriptPubKey);
            // Default Taproot signatures have a fixed 64-byte witness encoding.
            reveal.Inputs[0].WitScript = new WitScript(new[] { new byte[64], scriptBytes, controlBlock });
            var commitValue = reveal.Outputs[0].Value + feeRate.GetFee(reveal.GetVirtualSize());

            using var utxos = JsonDocument.Parse(await _httpClient.GetStringAsync($"address/{source}/utxo"));
            foreach (var utxo in utxos.RootElement.EnumerateArray())
            {
                if (!utxo.GetProperty("status").GetProperty("confirmed").GetBoolean() ||
                    !uint256.TryParse(utxo.GetProperty("txid").GetString(), out var previousId))
                    continue;

                var outpoint = new OutPoint(previousId, utxo.GetProperty("vout").GetUInt32());
                var coin = await ReadOrdinalCoinAsync(ord, outpoint, source, signer, false, null);
                if (coin == null || coin.Amount <= commitValue)
                    continue;

                var commit = network.CreateTransaction();
                commit.Version = 2;
                commit.Inputs.Add(coin.Outpoint);
                commit.Outputs.Add(commitValue, commitment.ScriptPubKey);
                var change = commit.Outputs.Add(coin.Amount - commitValue, source.ScriptPubKey);
                var builder = network.CreateTransactionBuilder().AddCoins(coin).AddKeys(signer.PrivateKey);
                var commitFee = builder.EstimateFees(commit, feeRate);
                if (change.Value <= commitFee)
                    continue;
                change.Value -= commitFee;
                if (change.IsDust())
                    continue;

                builder.SignTransactionInPlace(commit);
                if (!builder.Verify(commit))
                    continue;

                var commitId = commit.GetHash();
                reveal.Inputs[0].PrevOut = new OutPoint(commitId, 0);
                var signatureHash = reveal.GetSignatureHashTaproot(new[] { commit.Outputs[0] },
                    new TaprootExecutionData(0, tapScript.LeafHash));
                var signature = signer.PrivateKey.SignTaprootScriptSpend(signatureHash);
                reveal.Inputs[0].WitScript = new WitScript(new[] { signature.ToBytes(), scriptBytes, controlBlock });

                if (await ReadSharedOrdinalTipAsync(ord) != tip || await core.GetBestBlockHashAsync() != tip)
                    throw new InvalidOperationException("The Bitcoin chain advanced during inscription preparation. Recheck the funding output.");

                var revealId = reveal.GetHash();
                var inscriptionId = revealId + "i0";
                result.MetaData["BitcoinCommitTransactionId"] = commitId.ToString();
                result.MetaData[BitcoinTransactionIdMetadataKey] = revealId.ToString();
                result.MetaData["BitcoinInscriptionId"] = inscriptionId;
                try
                {
                    var package = await core.SendCommandAsync("submitpackage", (object)new[] { commit.ToHex(), reveal.ToHex() });
                    var entries = package.Result?["tx-results"];
                    var commitReceipt = entries?[commit.GetWitHash().ToString()];
                    var revealReceipt = entries?[reveal.GetWitHash().ToString()];
                    if (package.Result?["package_msg"]?.ToString() != "success" ||
                        commitReceipt?["txid"]?.ToString() != commitId.ToString() ||
                        revealReceipt?["txid"]?.ToString() != revealId.ToString() ||
                        commitReceipt?["other-wtxid"] != null || revealReceipt?["other-wtxid"] != null)
                        throw new InvalidOperationException("Bitcoin Core did not acknowledge the exact commit and reveal package.");
                }
                catch (Exception ex)
                {
                    result.ErrorCode = BitcoinBroadcastOutcomeUnknown;
                    OASISErrorHandling.HandleError(ref result, $"Bitcoin inscription package {commitId}, {revealId} requires reconciliation ({ex.GetType().Name}).");
                    return result;
                }

                result.Result = new Web3NFTTransactionResponse
                {
                    TransactionResult = revealId.ToString(),
                    Web3NFT = new Web3NFT
                    {
                        TokenId = inscriptionId,
                        NFTTokenAddress = inscriptionId,
                        MintTransactionHash = revealId.ToString(),
                        NFTMintedUsingWalletAddress = source.ToString(),
                        OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.BitcoinOASIS),
                        StoreNFTMetaDataOnChain = true,
                        MetaData = new Dictionary<string, string> { ["CommitTransactionId"] = commitId.ToString() }
                    }
                };
                if (request.WaitTillNFTMinted != true)
                    return result;

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(request.WaitForNFTToMintInSeconds ?? 180));
                try
                {
                    while (!timeout.IsCancellationRequested)
                    {
                        var read = await ReadOrdinalInscriptionAsync(inscriptionId, ord).WaitAsync(timeout.Token);
                        if (!read.IsError && read.Result.MetaData.TryGetValue("CurrentOwnerAddress", out var owner) &&
                            owner == recipient.ToString())
                        {
                            read.Result.MetaData["CommitTransactionId"] = commitId.ToString();
                            read.Result.NFTMintedUsingWalletAddress = source.ToString();
                            result.Result.Web3NFT = read.Result;
                            return result;
                        }
                        await Task.Delay(TimeSpan.FromSeconds(request.AttemptToMintEveryXSeconds ?? 1), timeout.Token);
                    }
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                {
                    // The acknowledged package identities remain available for confirmation.
                }

                result.ErrorCode = "BITCOIN_CONFIRMATION_PENDING";
                OASISErrorHandling.HandleError(ref result, $"Bitcoin Core accepted inscription {inscriptionId}. Its confirmation is still pending.");
                return result;
            }

            throw new InvalidOperationException("The inscription signing address has no confirmed cardinal output large enough to fund the mint.");
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex is ArgumentException or InvalidOperationException
                ? ex.Message : $"Bitcoin inscription mint failed ({ex.GetType().Name}).");
        }

        return result;
    }

    private static byte[] SerializeOrdinalMintBody(IMintWeb3NFTRequest request)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(new
        {
            title = request.Title,
            description = request.Description,
            symbol = request.Symbol,
            metadata = request.MetaData,
            jsonMetadata = request.JSONMetaData,
            jsonMetadataUrl = request.JSONMetaDataURL,
            image = request.Image,
            imageUrl = request.ImageUrl,
            tags = request.Tags
        });
        if (body.Length > MaximumOrdinalBodyBytes)
            throw new ArgumentException($"The encoded Bitcoin inscription body exceeds {MaximumOrdinalBodyBytes.ToString(CultureInfo.InvariantCulture)} bytes.");
        return body;
    }

    private static TapScript CreateOrdinalTapScript(TaprootInternalPubKey publicKey, byte[] content)
    {
        var envelope = new Script(
            Op.GetPushOp(publicKey.ToBytes()),
            OpcodeType.OP_CHECKSIG,
            OpcodeType.OP_0,
            OpcodeType.OP_IF,
            Op.GetPushOp(Encoding.ASCII.GetBytes("ord")));
        var fields = new List<Op>
        {
            Op.GetPushOp(Encoding.ASCII.GetBytes("application/json")),
            OpcodeType.OP_0
        };
        foreach (var chunk in content.Chunk(520))
            fields.Add(Op.GetPushOp(chunk));
        fields.Add(OpcodeType.OP_ENDIF);

        // The content-type tag is a byte push, as in ord's canonical inscription envelope.
        byte[] contentTypeTag = [0x01, 0x01];
        byte[] scriptBytes = [.. envelope.ToBytes(), .. contentTypeTag, .. new Script(fields).ToBytes()];
        return new TapScript(new Script(scriptBytes), TapLeafVersion.C0);
    }
}

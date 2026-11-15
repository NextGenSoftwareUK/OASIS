using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Numerics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

public partial class BitcoinOASIS
{
    private async Task<OASISResult<ITransactionResponse>> MintBitcoinRuneAsync(IMintWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            var id = request?.TokenAddress?.Split(':');
            if (id == null || id.Length != 2 ||
                !ulong.TryParse(id[0], NumberStyles.None, CultureInfo.InvariantCulture, out var block) || block == 0 ||
                !uint.TryParse(id[1], NumberStyles.None, CultureInfo.InvariantCulture, out var index))
                throw new ArgumentException("Bitcoin token mint requires the existing Rune ID in block:index form.");

            var runeId = FormattableString.Invariant($"{block}:{index}");
            if (!_isActivated)
            {
                var activation = await ActivateProviderAsync();
                if (activation.IsError)
                    throw new InvalidOperationException("Could not activate the Bitcoin provider for Rune minting.");
            }

            var signer = new BitcoinSecret(_privateKey, SigningNetwork);
            var source = signer.GetAddress(ScriptPubKeyType.TaprootBIP86);
            var recipient = string.IsNullOrWhiteSpace(request.ToWalletAddress)
                ? source
                : BitcoinAddress.Create(request.ToWalletAddress, SigningNetwork);

            using var ord = CreateOrdClient();
            var tip = await ReadSharedOrdinalTipAsync(ord);
            var genesis = await ord.GetFromJsonAsync<string>("r/blockhash/0");
            if (!uint256.TryParse(genesis, out var genesisHash) || genesisHash != SigningNetwork.GetGenesis().GetHash())
                throw new InvalidOperationException("The ord server is indexing a different Bitcoin network.");

            using var rune = JsonDocument.Parse(await ord.GetStringAsync("rune/" + runeId));
            var definition = rune.RootElement;
            if (definition.GetProperty("id").GetString() != runeId || !definition.GetProperty("mintable").GetBoolean())
                throw new InvalidOperationException("The indexed Rune does not have an open mint.");

            var entry = definition.GetProperty("entry");
            var terms = entry.GetProperty("terms");
            var divisibility = entry.GetProperty("divisibility").GetInt32();
            var tranche = BigInteger.Parse(terms.GetProperty("amount").GetRawText(), CultureInfo.InvariantCulture);
            if (tranche <= BigInteger.Zero || divisibility < 0 || divisibility > 38)
                throw new InvalidOperationException("The Rune does not specify a valid mint tranche.");
            if (request.Amount < 0 || (request.Amount > 0 && ConvertRuneAmount(request.Amount, divisibility) != tranche))
                throw new ArgumentException("The requested amount must match one tranche of the Rune's existing mint terms.");

            // Mint uses two occurrences of tag 20 and allocates unassigned units to output zero.
            var payload = new List<byte>();
            WriteRunestoneInteger(payload, 20);
            WriteRunestoneInteger(payload, block);
            WriteRunestoneInteger(payload, 20);
            WriteRunestoneInteger(payload, index);
            WriteRunestoneInteger(payload, 22);
            WriteRunestoneInteger(payload, 0);
            var runestone = new Script(OpcodeType.OP_RETURN, OpcodeType.OP_13, Op.GetPushOp(payload.ToArray()));
            var feeRate = await ReadBitcoinPaymentFeeRateAsync();
            var transaction = await CreateSignedCardinalPaymentAsync(ord, signer, source, feeRate,
                new TxOut(Money.Satoshis(OrdinalPostageSatoshis), recipient.ScriptPubKey),
                new TxOut(Money.Zero, runestone));
            if (transaction == null)
                throw new InvalidOperationException("The Rune mint signer has insufficient confirmed cardinal BTC.");
            if (await ReadSharedOrdinalTipAsync(ord) != tip)
                throw new InvalidOperationException("The Bitcoin chain advanced while preparing the Rune mint. Recheck the mint terms.");

            result = await BroadcastSignedBitcoinAsync(transaction, null);
            result.MetaData["BitcoinRuneId"] = runeId;
            result.MetaData["BitcoinRuneMintUnits"] = tranche.ToString(CultureInfo.InvariantCulture);
            return result;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex is ArgumentException or InvalidOperationException
                ? ex.Message : $"Bitcoin Rune mint failed ({ex.GetType().Name}).");
        }
        return result;
    }

    private static BigInteger ConvertRuneAmount(decimal amount, int divisibility)
    {
        var parts = decimal.GetBits(amount);
        var scale = (parts[3] >> 16) & 0xff;
        var mantissa = ((BigInteger)(uint)parts[2] << 64) | ((BigInteger)(uint)parts[1] << 32) | (uint)parts[0];
        var units = BigInteger.DivRem(mantissa * BigInteger.Pow(10, divisibility), BigInteger.Pow(10, scale), out var fraction);
        if (!fraction.IsZero || units > UInt128.MaxValue)
            throw new ArgumentException("The amount exceeds the Rune precision or protocol range.");
        return units;
    }

    private static void WriteRunestoneInteger(List<byte> payload, BigInteger value)
    {
        while (value >= 128)
        {
            payload.Add((byte)(0x80 | (value & 0x7f)));
            value >>= 7;
        }
        payload.Add((byte)value);
    }

    private async Task<OASISResult<ITransactionResponse>> MoveBitcoinRuneAsync(string identifier, string senderAddress,
        string receiverAddress, string privateKey, string seedPhrase, decimal amount, bool burn,
        Func<Transaction, string, BigInteger, Task> reservePublication = null)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            var (runeId, block, index) = ParseBitcoinRuneId(identifier);
            if (amount <= 0)
                throw new ArgumentException("A positive Rune amount is required.");
            if (string.IsNullOrWhiteSpace(privateKey) && string.IsNullOrWhiteSpace(seedPhrase))
                throw new ArgumentException("The Rune owner's signing key is required.");

            if (!_isActivated)
            {
                var activation = await ActivateProviderAsync();
                if (activation.IsError)
                    throw new InvalidOperationException("Could not activate the Bitcoin provider for the Rune transaction.");
            }

            var network = SigningNetwork;
            var signer = new BitcoinSecret(ResolveWalletSigningKey(privateKey, seedPhrase), network);
            var source = burn ? signer.GetAddress(ScriptPubKeyType.TaprootBIP86) : BitcoinAddress.Create(senderAddress, network);
            var recipient = burn ? null : BitcoinAddress.Create(receiverAddress, network);
            var wrappedSegwit = source.Equals(signer.GetAddress(ScriptPubKeyType.SegwitP2SH));
            if (!wrappedSegwit && !source.Equals(signer.GetAddress(ScriptPubKeyType.Legacy)) &&
                !source.Equals(signer.GetAddress(ScriptPubKeyType.Segwit)) &&
                !source.Equals(signer.GetAddress(ScriptPubKeyType.TaprootBIP86)))
                throw new ArgumentException("The Rune source address does not belong to the signing key.");

            using var ord = CreateOrdClient();
            var tip = await ReadSharedOrdinalTipAsync(ord);
            var genesis = await ord.GetFromJsonAsync<string>("r/blockhash/0");
            if (!uint256.TryParse(genesis, out var genesisHash) || genesisHash != network.GetGenesis().GetHash())
                throw new InvalidOperationException("The ord server is indexing a different Bitcoin network.");

            var (runeName, divisibility) = await ReadBitcoinRuneDefinitionAsync(ord, runeId);
            var units = ConvertRuneAmount(amount, divisibility);

            // Output zero receives every unallocated Rune. A burn allocates only the requested units to OP_RETURN.
            var transaction = network.CreateTransaction();
            var change = transaction.Outputs.Add(Money.Zero, source.ScriptPubKey);
            var postage = burn ? Money.Zero : Money.Satoshis(OrdinalPostageSatoshis);
            if (!burn)
                transaction.Outputs.Add(postage, recipient.ScriptPubKey);
            transaction.Outputs.Add(Money.Zero, CreateBitcoinRuneAllocation(block, index, units, 1));

            var feeRate = await ReadBitcoinPaymentFeeRateAsync();
            var builder = network.CreateTransactionBuilder().AddKeys(signer.PrivateKey);
            var available = Money.Zero;
            var availableRunes = BigInteger.Zero;
            using var utxos = JsonDocument.Parse(await _httpClient.GetStringAsync($"address/{source}/utxo"));
            foreach (var utxo in utxos.RootElement.EnumerateArray())
            {
                if (!utxo.GetProperty("status").GetProperty("confirmed").GetBoolean() ||
                    !uint256.TryParse(utxo.GetProperty("txid").GetString(), out var previousId))
                    continue;

                var outpoint = new OutPoint(previousId, utxo.GetProperty("vout").GetUInt32());
                var funding = await ReadOrdinalFundingAsync(ord, outpoint, source, signer, wrappedSegwit, null, runeName);
                if (funding.Coin == null)
                    continue;

                builder.AddCoins(funding.Coin);
                transaction.Inputs.Add(outpoint);
                available += funding.Coin.Amount;
                availableRunes += funding.RuneUnits;
                if (availableRunes < units)
                    continue;

                var fee = builder.EstimateFees(transaction, feeRate);
                if (available <= postage + fee)
                    continue;
                change.Value = available - postage - fee;
                if (change.IsDust())
                    continue;

                builder.SignTransactionInPlace(transaction);
                if (!builder.Verify(transaction))
                    throw new InvalidOperationException("The signed Rune transaction failed local policy.");
                if (await ReadSharedOrdinalTipAsync(ord) != tip)
                    throw new InvalidOperationException("The indexed Bitcoin chain changed while preparing the Rune transaction.");

                if (reservePublication != null)
                    await reservePublication(transaction, runeId, units);
                result = await BroadcastSignedBitcoinAsync(transaction, null);
                result.MetaData["BitcoinRuneId"] = runeId;
                result.MetaData["BitcoinRuneUnits"] = units.ToString(CultureInfo.InvariantCulture);
                return result;
            }

            throw new InvalidOperationException("The signing address has insufficient confirmed Rune units or BTC fees.");
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex is ArgumentException or InvalidOperationException
                ? ex.Message : $"Bitcoin Rune transaction failed ({ex.GetType().Name}).");
        }
        return result;
    }
    private static (string Id, ulong Block, uint Index) ParseBitcoinRuneId(string identifier)
    {
        var parts = identifier?.Split(':');
        if (parts == null || parts.Length != 2 ||
            !ulong.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var block) || block == 0 ||
            !uint.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            throw new ArgumentException("A Bitcoin Rune ID in block:index form is required.");

        return (FormattableString.Invariant($"{block}:{index}"), block, index);
    }

    private static async Task<(string Name, int Divisibility)> ReadBitcoinRuneDefinitionAsync(HttpClient ord, string runeId)
    {
        using var rune = JsonDocument.Parse(await ord.GetStringAsync("rune/" + runeId));
        if (rune.RootElement.GetProperty("id").GetString() != runeId)
            throw new InvalidOperationException("The Rune index returned a different native asset.");

        var entry = rune.RootElement.GetProperty("entry");
        var name = entry.GetProperty("spaced_rune").GetString();
        var divisibility = entry.GetProperty("divisibility").GetInt32();
        if (string.IsNullOrWhiteSpace(name) || divisibility < 0 || divisibility > 38)
            throw new InvalidOperationException("The Rune index returned invalid asset parameters.");

        return (name, divisibility);
    }

    private static Script CreateBitcoinRuneAllocation(ulong block, uint index, BigInteger units, uint output)
    {
        var payload = new List<byte>();
        foreach (var value in new BigInteger[] { 22, 0, 0, block, index, units, output })
            WriteRunestoneInteger(payload, value);

        return new Script(OpcodeType.OP_RETURN, OpcodeType.OP_13, Op.GetPushOp(payload.ToArray()));
    }

}

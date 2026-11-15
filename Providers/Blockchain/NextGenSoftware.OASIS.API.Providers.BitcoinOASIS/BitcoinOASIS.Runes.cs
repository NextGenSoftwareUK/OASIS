using System;
using System.Collections.Generic;
using System.Globalization;
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
            if (request.Amount < 0 || (request.Amount > 0 && !MatchesRuneAmount(request.Amount, divisibility, tranche)))
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

    private static bool MatchesRuneAmount(decimal amount, int divisibility, BigInteger tranche)
    {
        var parts = decimal.GetBits(amount);
        var scale = (parts[3] >> 16) & 0xff;
        var mantissa = ((BigInteger)(uint)parts[2] << 64) | ((BigInteger)(uint)parts[1] << 32) | (uint)parts[0];
        return mantissa * BigInteger.Pow(10, divisibility) == tranche * BigInteger.Pow(10, scale);
    }

    private static void WriteRunestoneInteger(List<byte> payload, ulong value)
    {
        while (value >= 128)
        {
            payload.Add((byte)(0x80 | (value & 0x7f)));
            value >>= 7;
        }
        payload.Add((byte)value);
    }
}

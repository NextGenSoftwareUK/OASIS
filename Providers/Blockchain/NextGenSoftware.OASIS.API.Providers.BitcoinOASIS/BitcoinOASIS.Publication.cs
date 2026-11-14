using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NBitcoin;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

public partial class BitcoinOASIS
{
    private static string GetPublicationPinPrefix(Network network, string address) =>
        $"oasis-bitcoin-v1:{network.GetGenesis().GetHash()}:{address}:";

    private static string GetPublicationPinName(Network network, string address, uint256 transactionId) =>
        GetPublicationPinPrefix(network, address) + transactionId;

    /// <summary>Lists transaction IDs recorded by the configured wallet before broadcast.</summary>
    public async Task<OASISResult<IReadOnlyList<string>>> ListRecordPublicationsAsync()
    {
        var result = new OASISResult<IReadOnlyList<string>>();
        try
        {
            var network = SigningNetwork;
            var signer = new BitcoinSecret(_privateKey, network);
            var prefix = GetPublicationPinPrefix(network, signer.GetAddress(ScriptPubKeyType.Segwit).ToString());
            using var kubo = CreateRecordBlobClient();
            using var pins = await ReadPublicationPinsAsync(kubo, prefix);
            var identifiers = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pin in pins.RootElement.GetProperty("Keys").EnumerateObject())
            {
                var name = pin.Value.GetProperty("Name").GetString();
                if (!name.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                if (!uint256.TryParse(name[prefix.Length..], out var transactionId) ||
                    !identifiers.Add(transactionId.ToString()))
                    throw new InvalidOperationException("The Bitcoin publication pin has an invalid or duplicate transaction ID.");
            }

            result.Result = identifiers.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Bitcoin publication listing failed ({ex.GetType().Name}).");
        }

        return result;
    }

    private static async Task<JsonDocument> ReadPublicationPinsAsync(HttpClient kubo, string name)
    {
        using var response = await kubo.PostAsync($"api/v0/pin/ls?type=recursive&name={Uri.EscapeDataString(name)}", null);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    /// <summary>Checks a recorded publication against the configured Kubo and Bitcoin backends.</summary>
    public async Task<OASISResult<bool>> ReconcileRecordPublicationAsync(string providerKey)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!uint256.TryParse(providerKey, out var transactionId))
                throw new ArgumentException("A Bitcoin transaction ID is required.", nameof(providerKey));

            var network = SigningNetwork;
            var signer = new BitcoinSecret(_privateKey, network);
            var source = signer.GetAddress(ScriptPubKeyType.Segwit).ToString();
            var name = GetPublicationPinName(network, source, transactionId);
            using var kubo = CreateRecordBlobClient();
            using var pins = await ReadPublicationPinsAsync(kubo, name);
            var cid = pins.RootElement.GetProperty("Keys").EnumerateObject()
                .Where(pin => pin.Value.GetProperty("Name").GetString() == name)
                .Select(pin => pin.Name).SingleOrDefault();
            if (cid == null)
            {
                result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                OASISErrorHandling.HandleError(ref result, "No matching Bitcoin record publication pin was found.");
                return result;
            }

            result.MetaData[BitcoinTransactionIdMetadataKey] = transactionId.ToString();
            var genesis = (await _httpClient.GetStringAsync("block-height/0")).Trim();
            if (!uint256.TryParse(genesis, out var genesisHash) || genesisHash != network.GetGenesis().GetHash())
                throw new InvalidOperationException("Esplora is serving a different Bitcoin network.");

            var transaction = await ReadVerifiedTransactionAsync(transactionId);
            using var codec = CreateRecordCodec();
            var commitments = from output in transaction.Outputs
                              where output.Value == Money.Zero
                              let pushes = new TxNullDataTemplate(output.ScriptPubKey.Length).ExtractScriptPubKeyParameters(output.ScriptPubKey)
                              where pushes?.Length == 1
                              from kind in Enum.GetValues<BitcoinRecordKind>()
                              where BitcoinRecordBlobStore.IsCommitment(pushes[0], kind)
                              select (Payload: pushes[0], Kind: kind);
            var commitment = commitments.Single();
            await new BitcoinRecordBlobStore(kubo, codec, commitment.Kind)
                .VerifyPinnedCommitmentAsync(cid, commitment.Payload);
            result.Result = true;
        }
        catch (BitcoinTransactionNotFoundException)
        {
            result.ErrorCode = BitcoinBroadcastOutcomeUnknown;
            OASISErrorHandling.HandleError(ref result, "The pinned Bitcoin publication is not visible through Esplora.");
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Bitcoin publication reconciliation failed ({ex.GetType().Name}).");
        }

        return result;
    }
}

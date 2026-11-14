using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

public partial class BitcoinOASIS
{
    private async Task<OASISResult<T>> LoadWalletRecordAsync<T>(
        Func<Transaction, Task<T>> decode, Func<T, bool> matches, int version) where T : class, IHolonBase
    {
        var result = new OASISResult<T>();
        try
        {
            result.Result = (await ReadWalletRecordsAsync(decode, version)).Where(matches).SingleOrDefault();
            if (result.Result == null)
            {
                result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                OASISErrorHandling.HandleError(ref result, "The Bitcoin wallet has no matching record.");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Bitcoin wallet record lookup failed ({ex.GetType().Name}).");
        }

        return result;
    }

    private async Task<IReadOnlyList<T>> ReadWalletRecordsAsync<T>(Func<Transaction, Task<T>> decode, int version)
        where T : class, IHolonBase
    {
        if (version < 0)
            throw new ArgumentOutOfRangeException(nameof(version));

        var network = SigningNetwork;
        var wallet = new BitcoinSecret(_privateKey, network).GetAddress(ScriptPubKeyType.Segwit);
        var genesis = (await _httpClient.GetStringAsync("block-height/0")).Trim();
        if (!uint256.TryParse(genesis, out var genesisHash) || genesisHash != network.GetGenesis().GetHash())
            throw new InvalidOperationException("Esplora is serving a different Bitcoin network.");

        var tip = (await _httpClient.GetStringAsync("blocks/tip/hash")).Trim();
        var history = $"address/{wallet}/txs";
        var candidates = new HashSet<uint256>();
        var mempool = await ReadPageAsync(history + "/mempool");
        if (mempool.Length >= 50)
            throw new InvalidOperationException("The Bitcoin wallet mempool history exceeds Esplora's complete response range.");
        AddCandidates(mempool);

        const int confirmedPageSize = 25;
        var cursors = new HashSet<uint256>();
        var endpoint = history + "/chain";
        while (true)
        {
            var page = await ReadPageAsync(endpoint);
            AddCandidates(page);
            if (page.Length < confirmedPageSize)
                break;
            var cursor = uint256.Parse(page[^1].GetProperty("txid").GetString());
            if (page.Length != confirmedPageSize || !cursors.Add(cursor))
                throw new InvalidOperationException("Esplora returned invalid wallet history pagination.");
            endpoint = history + "/chain/" + cursor;
        }

        var records = new List<T>();
        foreach (var candidate in candidates)
        {
            var transaction = await ReadVerifiedTransactionAsync(candidate);
            if (!await SpendsWalletOutputsAsync(transaction, wallet.ScriptPubKey))
                continue;
            var record = await decode(transaction);
            if (record == null || (version > 0 && record.Version != version))
                continue;
            record.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
            record.ProviderUniqueStorageKey[Core.Enums.ProviderType.BitcoinOASIS] = candidate.ToString();
            records.Add(record);
        }

        if ((await _httpClient.GetStringAsync("blocks/tip/hash")).Trim() != tip)
            throw new InvalidOperationException("The Bitcoin tip changed during wallet history lookup.");

        return records.GroupBy(record => record.Id)
            .Where(group => !group.Any(record => record.IsDeleted))
            .Select(group =>
            {
                var latestVersion = group.Max(record => record.Version);
                return group.Single(record => record.Version == latestVersion);
            })
            .OrderBy(record => record.Id)
            .ToArray();

        void AddCandidates(JsonElement[] page)
        {
            foreach (var entry in page)
                if (entry.GetProperty("vout").EnumerateArray().Any(output =>
                    output.GetProperty("scriptpubkey_type").GetString() == "op_return"))
                    candidates.Add(uint256.Parse(entry.GetProperty("txid").GetString()));
        }
    }

    private async Task<JsonElement[]> ReadPageAsync(string endpoint) =>
        await _httpClient.GetFromJsonAsync<JsonElement[]>(endpoint) ??
        throw new InvalidOperationException("Esplora did not return Bitcoin wallet history.");

    private async Task<bool> SpendsWalletOutputsAsync(Transaction transaction, Script wallet)
    {
        if (transaction.IsCoinBase || transaction.Inputs.Count == 0)
            return false;

        foreach (var input in transaction.Inputs)
        {
            var previous = await ReadVerifiedTransactionAsync(input.PrevOut.Hash);
            if (input.PrevOut.N >= previous.Outputs.Count)
                throw new InvalidOperationException("A Bitcoin wallet transaction references a missing output.");
            if (previous.Outputs[(int)input.PrevOut.N].ScriptPubKey != wallet)
                return false;
        }

        return true;
    }
}

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
    private async Task<OASISResult<IAvatar>> LoadWalletAvatarAsync(Func<IAvatar, bool> matches, int version)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            result.Result = (await ReadWalletAvatarsAsync(version)).Where(matches).SingleOrDefault();
            if (result.Result == null)
            {
                result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                OASISErrorHandling.HandleError(ref result, "The Bitcoin wallet has no matching avatar.");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Bitcoin avatar lookup failed ({ex.GetType().Name}).");
        }

        return result;
    }

    private async Task<IReadOnlyList<IAvatar>> ReadWalletAvatarsAsync(int version)
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

        var avatars = new List<IAvatar>();
        foreach (var candidate in candidates)
        {
            var transaction = await ReadVerifiedTransactionAsync(candidate);
            if (!await SpendsWalletOutputsAsync(transaction, wallet.ScriptPubKey))
                continue;
            var avatar = await DecodeAvatarTransactionAsync(transaction);
            if (avatar == null || (version > 0 && avatar.Version != version))
                continue;
            avatar.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.BitcoinOASIS] = candidate.ToString();
            avatars.Add(avatar);
        }

        if ((await _httpClient.GetStringAsync("blocks/tip/hash")).Trim() != tip)
            throw new InvalidOperationException("The Bitcoin tip changed during wallet history lookup.");

        return avatars.GroupBy(avatar => avatar.Id)
            .Select(group =>
            {
                var latestVersion = group.Max(avatar => avatar.Version);
                return group.Single(avatar => avatar.Version == latestVersion);
            })
            .OrderBy(avatar => avatar.Id)
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

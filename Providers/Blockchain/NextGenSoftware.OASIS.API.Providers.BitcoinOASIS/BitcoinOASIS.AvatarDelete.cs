using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

public partial class BitcoinOASIS
{
    private async Task<OASISResult<IAvatar>> ResolveWalletAvatarForDeleteAsync(string providerKey)
    {
        var referenced = await LoadAvatarByProviderKeyAsync(providerKey);
        return await ResolveCurrentWalletRecordAsync(referenced, providerKey, id => LoadAvatarAsync(id));
    }

    private static async Task<OASISResult<T>> ResolveCurrentWalletRecordAsync<T>(
        OASISResult<T> referenced, string providerKey, Func<Guid, Task<OASISResult<T>>> loadCurrent)
        where T : class, IHolonBase
    {
        if (referenced.IsError)
            return referenced;

        var current = await loadCurrent(referenced.Result.Id);
        if (!current.IsError &&
            !string.Equals(current.Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.BitcoinOASIS],
                providerKey, StringComparison.OrdinalIgnoreCase))
            OASISErrorHandling.HandleError(ref current, "Bitcoin deletion requires the current wallet record key.");

        return current;
    }

    private async Task<OASISResult<bool>> DeleteWalletAvatarAsync(
        Func<Task<OASISResult<IAvatar>>> resolve, bool softDelete)
    {
        var result = new OASISResult<bool>();
        var current = await resolve();
        if (current.IsError)
        {
            result.ErrorCode = current.ErrorCode;
            OASISErrorHandling.HandleError(ref result, current.Message, current.Exception);
            return result;
        }

        if (current.Result.Version == int.MaxValue)
        {
            OASISErrorHandling.HandleError(ref result, "The Bitcoin avatar version has reached its limit.");
            return result;
        }

        // Hard deletion on an append-only ledger is crypto-shredding: the tombstone records which publications are
        // erased, then their encrypted envelopes are unpinned and collected so the content can no longer be read.
        var erasedAvatars = softDelete ? Array.Empty<string>() : await ReadPublicationIdsAsync<IAvatar>(DecodeAvatarTransactionAsync, current.Result.Id);
        var erasedDetails = softDelete ? Array.Empty<string>() : await ReadPublicationIdsAsync<IAvatarDetail>(DecodeAvatarDetailTransactionAsync, current.Result.Id);

        var tombstone = new Avatar
        {
            Id = current.Result.Id,
            Version = current.Result.Version + 1,
            IsActive = false,
            DeletedDate = DateTime.UtcNow
        };
        if (erasedAvatars.Length > 0)
            tombstone.MetaData[ErasedPublicationsMetadataKey] = string.Join(",", erasedAvatars);
        var publication = await SaveAvatarAsync(tombstone);
        result.MetaData = publication.MetaData;
        result.ErrorCode = publication.ErrorCode;
        if (publication.IsError || !publication.IsSaved)
        {
            OASISErrorHandling.HandleError(ref result, publication.Message, publication.Exception);
            return result;
        }

        if (erasedDetails.Length > 0)
        {
            var detailHistory = await ReadWalletRecordHistoryAsync<IAvatarDetail>(DecodeAvatarDetailTransactionAsync, 0);
            var detailTombstone = new AvatarDetail
            {
                Id = current.Result.Id,
                Version = detailHistory.Where(detail => detail.Id == current.Result.Id).Max(detail => detail.Version) + 1,
                IsActive = false,
                DeletedDate = DateTime.UtcNow
            };
            detailTombstone.MetaData[ErasedPublicationsMetadataKey] = string.Join(",", erasedDetails);
            var detailPublication = await SaveAvatarDetailAsync(detailTombstone);
            if (detailPublication.IsError || !detailPublication.IsSaved)
            {
                OASISErrorHandling.HandleError(ref result, detailPublication.Message, detailPublication.Exception);
                return result;
            }
        }

        if (!softDelete)
            await EraseRecordPublicationsAsync(erasedAvatars.Concat(erasedDetails));

        result.Result = true;
        result.IsDeleted = true;
        result.DeletedCount = 1;
        return result;
    }

    private async Task<string[]> ReadPublicationIdsAsync<T>(Func<NBitcoin.Transaction, Task<T>> decode, Guid id) where T : class, IHolonBase
        => (await ReadWalletRecordHistoryAsync(decode, 0))
            .Where(record => record.Id == id)
            .Select(record => record.ProviderUniqueStorageKey[Core.Enums.ProviderType.BitcoinOASIS])
            .ToArray();

    /// <summary>Unpins the encrypted envelopes of the given wallet publications and garbage-collects them from Kubo.</summary>
    private async Task EraseRecordPublicationsAsync(IEnumerable<string> transactionIds)
    {
        var network = SigningNetwork;
        var source = new NBitcoin.BitcoinSecret(_privateKey, network).GetAddress(NBitcoin.ScriptPubKeyType.Segwit).ToString();
        using var kubo = CreateRecordBlobClient();
        var erased = 0;
        foreach (var transactionId in transactionIds)
        {
            var name = GetPublicationPinName(network, source, NBitcoin.uint256.Parse(transactionId));
            using var pins = await ReadPublicationPinsAsync(kubo, name);
            var cid = EnumeratePublicationPins(pins).Where(pin => pin.Value.GetProperty("Name").GetString() == name)
                .Select(pin => pin.Name).SingleOrDefault()
                ?? throw new InvalidOperationException("A Bitcoin publication to erase has no record pin.");
            using var unpinned = await kubo.PostAsync($"api/v0/pin/rm?arg={cid}&recursive=true", null);
            unpinned.EnsureSuccessStatusCode();
            erased++;
        }
        if (erased == 0)
            return;
        using var collected = await kubo.PostAsync("api/v0/repo/gc", null);
        collected.EnsureSuccessStatusCode();
        await collected.Content.ReadAsStringAsync();
    }
}

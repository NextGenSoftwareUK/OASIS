using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS;

public partial class AlgorandOASIS
{
    private async Task<OASISResult<IEnumerable<IHolon>>> LoadEnrolledCollectionAsync(Guid? parentId, HolonType type,
        bool loadChildren, bool recursive, int maxChildDepth, int currentChildDepth, bool continueOnError,
        bool loadChildrenFromProvider, int version)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (parentId == Guid.Empty)
                throw new ArgumentException("An enrolled Algorand parent ID is required.", nameof(parentId));
            ArgumentOutOfRangeException.ThrowIfNegative(version);
            ArgumentOutOfRangeException.ThrowIfNegative(maxChildDepth);
            ArgumentOutOfRangeException.ThrowIfNegative(currentChildDepth);
            using var deadline = new CancellationTokenSource(_algod.Timeout);
            var discovered = await DiscoverStoredHolonIdsAsync(deadline.Token).ConfigureAwait(false);
            if (discovered.IsError)
                throw new InvalidOperationException(discovered.Message, discovered.Exception);
            var ids = discovered.Result;
            var loaded = new OASISResult<IHolon>[ids.Count];
            await Parallel.ForEachAsync(Enumerable.Range(0, ids.Count),
                new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = deadline.Token },
                async (index, token) =>
                {
                    loaded[index] = await LoadStoredHolonAsync(ids[index], version, token).ConfigureAwait(false);
                }).ConfigureAwait(false);

            var records = new List<IHolon>();
            var skipped = new List<string>();
            for (var index = 0; index < loaded.Length; index++)
            {
                var read = loaded[index];
                if (!read.IsError && read.Result != null)
                {
                    records.Add(read.Result);
                    continue;
                }
                if (version != 0 && read.ErrorCode == OASISResultErrorCodes.RecordNotFound)
                    continue;
                if (!continueOnError || read.Exception is not HttpRequestException)
                    throw new InvalidOperationException(read.Message ?? "An enumerated Algorand record is unavailable.", read.Exception);
                skipped.Add(ids[index].ToString("D"));
            }
            if (records.Count == 0 && skipped.Count != 0)
                throw new InvalidOperationException("No Algorand collection could be read.");
            var project = HolonGraphProjection.Create(records, loadChildren, recursive, maxChildDepth,
                currentChildDepth, loadChildrenFromProvider, item => OasisJson.DeserializeStored<Holon>(OasisJson.Serialize(item)),
                rootsAreChildren: parentId.HasValue);
            var selected = records.Where(item =>
                    (type == HolonType.All || item.HolonType == type) &&
                    (!parentId.HasValue || item.ParentHolonId == parentId.Value))
                .Select(project).ToArray();
            result.Result = selected;
            result.IsLoaded = true;
            result.LoadedCount = selected.Length;
            if (skipped.Count > 0)
            {
                result.IsWarning = true;
                result.WarningCount = skipped.Count;
                result.Message = "Returned a partial Algorand collection.";
                result.MetaData = new Dictionary<string, string>
                {
                    ["partialHolonReads"] = "true",
                    ["failedHolonReadCount"] = skipped.Count.ToString(CultureInfo.InvariantCulture),
                    ["failedHolonReads"] = OasisJson.Serialize(skipped)
                };
            }
        }
        catch (Exception exception)
        {
            OASISErrorHandling.HandleError(ref result, "Could not load the enrolled Algorand collection.", exception);
        }
        return result;
    }

    public Task<OASISResult<IReadOnlyList<Guid>>> DiscoverStoredHolonIdsAsync(CancellationToken cancellationToken = default)
        => DiscoverStoredRecordIdsAsync("holon", cancellationToken);

    private async Task<OASISResult<IReadOnlyList<Guid>>> DiscoverStoredRecordIdsAsync(string recordKind,
        CancellationToken cancellationToken)
    {
        var result = new OASISResult<IReadOnlyList<Guid>>();
        try
        {
            var settings = _recordBoxes;
            if (settings == null || !IsProviderActivated)
                throw new InvalidOperationException("Activate an enrolled Algorand record application before discovering records.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(_algod.Timeout);
            using var codec = new EncryptedRecordCodec(settings.StorageKey);
            var ids = new List<Guid>();
            var seen = new HashSet<(string Kind, Guid Id)>();
            var cursors = new HashSet<string>(StringComparer.Ordinal);
            const int maxRecords = 10000;
            const int pageSize = 64;
            var firstPath = $"{ApplicationPath(settings)}/boxes?prefix=b64%3AZA%3D%3D&limit={pageSize}&include=values";
            ulong? catalogRound = null;
            var cursor = string.Empty;
            do
            {
                var path = catalogRound.HasValue
                    ? $"{firstPath}&round={catalogRound.Value.ToString(CultureInfo.InvariantCulture)}&next={Uri.EscapeDataString(cursor)}"
                    : firstPath;
                using var page = await ReadBoxTransactionJsonAsync(path, deadline.Token).ConfigureAwait(false);
                var root = page.RootElement;
                var round = root.GetProperty("round").GetUInt64();
                if (round == 0 || catalogRound.HasValue && catalogRound.Value != round)
                    throw new InvalidDataException("Algod did not retain the requested discovery snapshot round.");
                catalogRound = round;
                foreach (var entry in root.GetProperty("boxes").EnumerateArray())
                {
                    var name = entry.GetProperty("name").GetBytesFromBase64();
                    if (name.Length != 1 + HashLength || name[0] != (byte)'d')
                        throw new InvalidDataException("Algod returned a box outside the record discovery namespace.");
                    var (kind, id) = DecodeDiscoveredRecord(settings, codec, name[1..],
                        entry.GetProperty("value").GetBytesFromBase64(), deadline.Token);
                    if (!seen.Add((kind, id)) || seen.Count > maxRecords)
                        throw new InvalidDataException("Algorand discovery returned a duplicate or exceeded its result limit.");
                    if (kind == recordKind) ids.Add(id);
                }
                cursor = root.TryGetProperty("next-token", out var next) ? next.GetString() ?? string.Empty : string.Empty;
                if (cursor.Length > 0 && (!cursors.Add(cursor) || cursors.Count > maxRecords))
                    throw new InvalidDataException("Algod returned an invalid discovery continuation.");
            } while (cursor.Length > 0);

            result.Result = ids;
            result.LoadedCount = ids.Count;
            result.IsLoaded = true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            OASISErrorHandling.HandleError(ref result, "Could not discover Algorand records.", exception);
        }
        return result;
    }

    private async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadEnrolledAvatarDetailsAsync(int version)
    {
        var result = new OASISResult<IEnumerable<IAvatarDetail>>();
        try
        {
            ArgumentOutOfRangeException.ThrowIfNegative(version);
            using var deadline = new CancellationTokenSource(_algod.Timeout);
            var discovered = await DiscoverStoredRecordIdsAsync("avatar-detail", deadline.Token).ConfigureAwait(false);
            if (discovered.IsError)
                throw new InvalidOperationException(discovered.Message, discovered.Exception);

            var details = new IAvatarDetail?[discovered.Result.Count];
            await Parallel.ForEachAsync(Enumerable.Range(0, details.Length),
                new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = deadline.Token },
                async (index, token) =>
                {
                    var read = await LoadStoredRecordAsync<AvatarDetail, IAvatarDetail>(discovered.Result[index], version,
                        "avatar-detail", token).ConfigureAwait(false);
                    if (!read.IsError && read.Result != null)
                        details[index] = read.Result;
                    else if (version == 0 || read.ErrorCode != OASISResultErrorCodes.RecordNotFound)
                        throw new InvalidOperationException(read.Message ?? "An enumerated Algorand avatar detail is unavailable.", read.Exception);
                }).ConfigureAwait(false);

            var loaded = details.OfType<IAvatarDetail>().Where(detail => !detail.IsDeleted).ToArray();
            result.Result = loaded;
            result.IsLoaded = true;
            result.LoadedCount = loaded.Length;
        }
        catch (Exception exception)
        {
            OASISErrorHandling.HandleError(ref result, "Could not load the enrolled Algorand avatar details.", exception);
        }
        return result;
    }

    private static string DiscoveryContext(AlgorandRecordBoxSettings settings, byte[] key)
        => FormattableString.Invariant(
            $"oasis:algorand:discovery:v1:{settings.GenesisHash}:{settings.ApplicationId}:{Convert.ToHexString(key)}");

    private static (string Kind, Guid Id) DecodeDiscoveredRecord(AlgorandRecordBoxSettings settings, EncryptedRecordCodec codec,
        byte[] key, byte[] descriptor, CancellationToken cancellationToken)
    {
        if (descriptor is not { Length: > 0 and <= 256 })
            throw new InvalidDataException("An Algorand discovery descriptor has an invalid length.");
        var value = codec.Decrypt(DiscoveryContext(settings, key), StrictUtf8.GetString(descriptor), cancellationToken);
        var separator = value.IndexOf(':');
        var kind = separator >= 0 ? value[..separator] : "holon";
        if (kind is not ("holon" or "avatar-detail") || !Guid.TryParseExact(value[(separator + 1)..], "D", out var id) || id == Guid.Empty ||
            !CryptographicOperations.FixedTimeEquals(StoredRecordBoxKey(settings, id, kind), key))
            throw new InvalidDataException("An Algorand discovery descriptor does not match its opaque record key.");
        return (kind, id);
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS;

public partial class AlgorandOASIS
{
    public async Task<OASISResult<IReadOnlyList<Guid>>> DiscoverStoredHolonIdsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<IReadOnlyList<Guid>>();
        try
        {
            var settings = _recordBoxes;
            if (settings == null || !IsProviderActivated)
                throw new InvalidOperationException("Activate an enrolled Algorand record application before discovering holons.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(_algod.Timeout);
            using var codec = new EncryptedRecordCodec(settings.StorageKey);
            var ids = new List<Guid>();
            var seen = new HashSet<Guid>();
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
                    var id = DecodeDiscoveredId(settings, codec, name[1..],
                        entry.GetProperty("value").GetBytesFromBase64(), deadline.Token);
                    if (!seen.Add(id) || ids.Count == maxRecords)
                        throw new InvalidDataException("Algorand discovery returned a duplicate or exceeded its result limit.");
                    ids.Add(id);
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

    private static string DiscoveryContext(AlgorandRecordBoxSettings settings, byte[] key)
        => FormattableString.Invariant(
            $"oasis:algorand:discovery:v1:{settings.GenesisHash}:{settings.ApplicationId}:{Convert.ToHexString(key)}");

    private static Guid DecodeDiscoveredId(AlgorandRecordBoxSettings settings, EncryptedRecordCodec codec,
        byte[] key, byte[] descriptor, CancellationToken cancellationToken)
    {
        if (descriptor is not { Length: > 0 and <= 256 } ||
            !Guid.TryParseExact(codec.Decrypt(DiscoveryContext(settings, key), StrictUtf8.GetString(descriptor),
                cancellationToken), "D", out var id) || id == Guid.Empty ||
            !CryptographicOperations.FixedTimeEquals(StoredHolonBoxKey(settings, id), key))
            throw new InvalidDataException("An Algorand discovery descriptor does not match its opaque record key.");
        return id;
    }
}

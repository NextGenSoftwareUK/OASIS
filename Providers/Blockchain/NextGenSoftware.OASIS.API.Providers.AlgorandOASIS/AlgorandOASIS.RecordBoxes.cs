using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AlgorandOASIS;

public partial class AlgorandOASIS
{
    private const int HashLength = 32;
    private const int ManifestHeaderLength = 3 * HashLength;
    private const int MaximumChunks = 6;
    private const int MaximumChunkLength = 896;
    private const int MaximumHistoryDepth = 10000;
    private static readonly byte[] EmptyHash = new byte[HashLength];
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly AlgorandRecordBoxSettings? _recordBoxes;

    private async Task VerifyRecordApplicationAsync(AlgorandRecordBoxSettings settings, string genesisHash)
    {
        if (!string.Equals(settings.GenesisHash, genesisHash, StringComparison.Ordinal))
            throw new InvalidOperationException("The configured record application belongs to a different Algorand ledger.");
        using var application = await ReadJsonAsync(_algod, ApplicationPath(settings)).ConfigureAwait(false);
        var root = application.RootElement;
        var parameters = root.GetProperty("params");
        var program = parameters.GetProperty("approval-program").GetBytesFromBase64();
        if (root.GetProperty("id").GetUInt64() != settings.ApplicationId ||
            parameters.GetProperty("creator").GetString() != settings.WriterAddress ||
            !CryptographicOperations.FixedTimeEquals(SHA256.HashData(program), settings.ProgramDigest))
            throw new InvalidDataException("Algod returned a record application with a different identity or approval program.");
    }

    public async Task<OASISResult<IHolon>> LoadStoredHolonAsync(Guid id, int version = 0,
        CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (_recordBoxes == null || !IsProviderActivated)
                throw new InvalidOperationException("Activate an enrolled Algorand record application before reading stored holons.");
            if (id == Guid.Empty)
                throw new ArgumentException("A stored holon ID is required.", nameof(id));
            ArgumentOutOfRangeException.ThrowIfNegative(version);

            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(_algod.Timeout);
            var settings = _recordBoxes;
            var key = HMACSHA256.HashData(settings.StorageKey, StrictUtf8.GetBytes($"oasis:algorand:box:v1:holon:{id:D}"));
            var manifest = await ReadBoxAsync(settings, [(byte)'h', ..key], deadline.Token).ConfigureAwait(false);
            using var codec = new EncryptedRecordCodec(settings.StorageKey);
            int? lastVersion = null;
            for (var depth = 0; manifest != null; depth++)
            {
                if (depth == MaximumHistoryDepth)
                    throw new InvalidDataException("Algorand history exceeds its traversal limit.");
                var (record, previousRevision, previousDigest) = await ReadStoredHolonAsync(settings, id, key,
                    manifest, codec, deadline.Token).ConfigureAwait(false);
                if (record.Version <= 0 || (lastVersion.HasValue && record.Version >= lastVersion.Value))
                    throw new InvalidDataException("The Algorand record has an invalid version sequence.");
                lastVersion = record.Version;
                if (version == 0 || record.Version == version)
                {
                    result.Result = record;
                    result.IsLoaded = true;
                    result.LoadedCount = 1;
                    return result;
                }
                if (record.Version < version || CryptographicOperations.FixedTimeEquals(previousDigest, EmptyHash))
                    break;

                var previousName = BoxName('m', [..key, ..previousRevision]);
                manifest = await ReadBoxAsync(settings, previousName, deadline.Token).ConfigureAwait(false);
                if (manifest == null || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(manifest), previousDigest))
                    throw new InvalidDataException("Algorand history does not match the committed predecessor digest.");
            }
            result.IsError = true;
            result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
            result.Message = "The requested Algorand holon or version was not found.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            OASISErrorHandling.HandleError(ref result, "Could not read the Algorand record application.", exception);
        }
        return result;
    }

    private async Task<(Holon Record, byte[] PreviousRevision, byte[] PreviousDigest)> ReadStoredHolonAsync(
        AlgorandRecordBoxSettings settings, Guid id, byte[] key, byte[] manifest, EncryptedRecordCodec codec,
        CancellationToken cancellationToken)
    {
        var hashBytes = manifest.Length - ManifestHeaderLength;
        if (hashBytes < HashLength || hashBytes > MaximumChunks * HashLength || hashBytes % HashLength != 0)
            throw new InvalidDataException("The Algorand record manifest has an invalid length.");
        var revision = manifest[..HashLength];
        var previousRevision = manifest[HashLength..(2 * HashLength)];
        var previousDigest = manifest[(2 * HashLength)..ManifestHeaderLength];
        if (CryptographicOperations.FixedTimeEquals(revision, EmptyHash) ||
            CryptographicOperations.FixedTimeEquals(previousRevision, EmptyHash) !=
            CryptographicOperations.FixedTimeEquals(previousDigest, EmptyHash))
            throw new InvalidDataException("The Algorand record manifest has an invalid revision link.");

        using var encrypted = new MemoryStream();
        var chunks = new Task<byte[]?>[hashBytes / HashLength];
        for (var index = 0; index < chunks.Length; index++)
        {
            var indexBytes = new byte[8];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(indexBytes, (ulong)index);
            var name = BoxName('c', [..key, ..revision, ..indexBytes]);
            chunks[index] = ReadBoxAsync(settings, name, cancellationToken);
        }
        var values = await Task.WhenAll(chunks).ConfigureAwait(false);
        for (var index = 0; index < values.Length; index++)
        {
            var chunk = values[index];
            var expected = manifest.AsSpan(ManifestHeaderLength + index * HashLength, HashLength);
            if (chunk is not { Length: > 0 and <= MaximumChunkLength } ||
                !CryptographicOperations.FixedTimeEquals(SHA256.HashData(chunk), expected))
                throw new InvalidDataException("An Algorand chunk does not match the committed digest.");
            encrypted.Write(chunk);
        }
        var context = FormattableString.Invariant(
            $"oasis:algorand:v1:{settings.GenesisHash}:{settings.ApplicationId}:holon:{id:D}:{Convert.ToHexString(revision)}");
        var json = codec.Decrypt(context, StrictUtf8.GetString(encrypted.ToArray()), cancellationToken);
        var record = OasisJson.DeserializeStored<Holon>(json);
        if (record == null || record.Id != id)
            throw new InvalidDataException("The Algorand record does not match the requested holon identity.");
        return (record, previousRevision, previousDigest);
    }

    private async Task<byte[]?> ReadBoxAsync(AlgorandRecordBoxSettings settings, byte[] name,
        CancellationToken cancellationToken)
    {
        var encodedName = Uri.EscapeDataString("b64:" + Convert.ToBase64String(name));
        using var response = await _algod.GetAsync($"{ApplicationPath(settings)}/box?name={encodedName}",
            HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);
        var box = document.RootElement;
        if (!box.GetProperty("name").GetBytesFromBase64().AsSpan().SequenceEqual(name))
            throw new InvalidDataException("Algod returned a different record box.");
        return box.GetProperty("value").GetBytesFromBase64();
    }

    private static string ApplicationPath(AlgorandRecordBoxSettings settings)
        => "/v2/applications/" + settings.ApplicationId.ToString(CultureInfo.InvariantCulture);

    private static byte[] BoxName(char prefix, byte[] material)
        => [(byte)prefix, ..SHA256.HashData(material)];
}

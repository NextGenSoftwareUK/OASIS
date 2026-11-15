using System;
using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
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
    private Task<OASISResult<IHolon>> SaveEnrolledHolonAsync(IHolon holon, bool saveChildren)
    {
        if (holon == null || holon.Version < 0 || saveChildren && holon.Children is { Count: > 0 })
            return Task.FromResult(new OASISResult<IHolon>
            {
                IsError = true,
                Message = "An Algorand root save requires a valid holon version and a single record."
            });
        // HolonManager stamps the proposed revision before invoking the provider.
        return SaveStoredHolonAsync(holon, Math.Max(0, holon.Version - 1));
    }

    public async Task<OASISResult<IHolon>> SoftDeleteStoredHolonAsync(Guid id, Guid deletedByAvatarId,
        CancellationToken cancellationToken = default)
    {
        if (deletedByAvatarId == Guid.Empty)
            return new OASISResult<IHolon> { IsError = true, Message = "An Algorand deletion requires an avatar ID." };
        var current = await LoadStoredHolonAsync(id, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (current.IsError || current.Result == null)
            return current;
        if (current.Result.IsDeleted)
            return new OASISResult<IHolon>(current.Result) { IsDeleted = true, DeletedCount = 0 };

        var version = current.Result.Version;
        current.Result.DeletedByAvatarId = deletedByAvatarId;
        current.Result.DeletedDate = DateTime.UtcNow;
        current.Result.IsActive = false;
        var published = await SaveStoredHolonAsync(current.Result, version, cancellationToken).ConfigureAwait(false);
        if (!published.IsError && published.Result != null)
        {
            published.IsSaved = false;
            published.SavedCount = 0;
            published.IsDeleted = true;
            published.DeletedCount = 1;
        }
        return published;
    }

    public async Task<OASISResult<IHolon>> SaveStoredHolonAsync(IHolon holon, int expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            var settings = _recordBoxes;
            if (settings?.WriterSeed == null || !IsProviderActivated)
                throw new InvalidOperationException("Activate an enrolled Algorand application with a writer before publishing records.");
            ArgumentNullException.ThrowIfNull(holon);
            ArgumentOutOfRangeException.ThrowIfNegative(expectedVersion);
            if (holon.Id == Guid.Empty)
                throw new ArgumentException("The proposed record must have a stable OASIS ID.", nameof(holon));

            var id = holon.Id;
            var proposal = OasisJson.DeserializeStored<Holon>(OasisJson.Serialize(holon));
            if (proposal == null || proposal.Id != id)
                throw new InvalidDataException("Could not snapshot the proposed record identity.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(_algod.Timeout);
            var token = deadline.Token;
            var key = StoredHolonBoxKey(settings, id);
            byte[] headName = [(byte)'h', ..key];
            var head = await ReadBoxAsync(settings, headName, token).ConfigureAwait(false);
            using var codec = new EncryptedRecordCodec(settings.StorageKey);
            var current = head == null ? null : (await ReadStoredHolonAsync(settings, id, key,
                head, codec, token).ConfigureAwait(false)).Record;
            if ((current?.Version ?? 0) != expectedVersion)
                throw new InvalidOperationException("The requested Algorand predecessor version is no longer current.");
            var version = checked(expectedVersion + 1);
            if (proposal.VersionId == Guid.Empty || proposal.Version != version || proposal.VersionId == current?.VersionId)
                proposal.VersionId = Guid.NewGuid();
            proposal.PreviousVersionId = current?.VersionId ?? Guid.Empty;
            proposal.Version = version;
            proposal.Children = [];
            proposal.IsNewHolon = false;
            proposal.ProviderUniqueStorageKey ??= [];
            proposal.ProviderUniqueStorageKey[Core.Enums.ProviderType.AlgorandOASIS] = id.ToString("D");

            var revision = RandomNumberGenerator.GetBytes(HashLength);
            var envelope = StrictUtf8.GetBytes(codec.Encrypt(StoredHolonContext(settings, id, revision),
                OasisJson.Serialize(proposal), token));
            if (envelope.Length > MaximumChunks * MaximumChunkLength)
                throw new InvalidDataException("The encrypted Algorand record exceeds the contract's publication limit.");

            var count = (envelope.Length + MaximumChunkLength - 1) / MaximumChunkLength;
            var names = new byte[count + 2][];
            names[0] = headName;
            names[1] = BoxName('m', [..key, ..revision]);
            var digests = new byte[count * HashLength];
            for (var index = 0; index < count; index++)
            {
                var offset = index * MaximumChunkLength;
                var payload = envelope[offset..Math.Min(offset + MaximumChunkLength, envelope.Length)];
                var indexBytes = new byte[8];
                BinaryPrimitives.WriteUInt64BigEndian(indexBytes, (ulong)index);
                var name = BoxName('c', [..key, ..revision, ..indexBytes]);
                names[index + 2] = name;
                SHA256.HashData(payload).CopyTo(digests, index * HashLength);
                await SubmitBoxCallAsync(settings, ["stage"u8.ToArray(), key, revision, indexBytes, payload],
                    [name], token).ConfigureAwait(false);
            }

            var predecessor = head == null ? EmptyHash : SHA256.HashData(head);
            await SubmitBoxCallAsync(settings, ["publish"u8.ToArray(), key, predecessor, revision, digests],
                names, token).ConfigureAwait(false);
            var manifest = await ReadBoxAsync(settings, names[1], token).ConfigureAwait(false);
            byte[] previousRevision = head == null ? EmptyHash : head[..HashLength];
            byte[] expectedManifest = [..revision, ..previousRevision, ..predecessor, ..digests];
            if (manifest == null || !manifest.AsSpan().SequenceEqual(expectedManifest))
                throw new InvalidDataException("Algorand confirmed publication without the exact durable manifest.");

            result.Result = proposal;
            result.IsSaved = true;
            result.SavedCount = 1;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            OASISErrorHandling.HandleError(ref result, "Could not complete the Algorand record publication.", exception);
        }
        return result;
    }

    private async Task SubmitBoxCallAsync(AlgorandRecordBoxSettings settings, byte[][] arguments, byte[][] boxes,
        CancellationToken cancellationToken)
    {
        using var parameters = await ReadBoxTransactionJsonAsync("/v2/transactions/params", cancellationToken).ConfigureAwait(false);
        var native = parameters.RootElement;
        var genesis = ReadNetworkIdentity(native, "genesis-id", "genesis-hash");
        if (genesis.Hash != settings.GenesisHash)
            throw new InvalidDataException("Algod's signing parameters identify a different ledger.");
        var round = native.GetProperty("last-round").GetUInt64();
        var (signed, id) = AlgorandBoxTransaction.Sign(settings, genesis.Id, round,
            native.GetProperty("min-fee").GetUInt64(), native.GetProperty("fee").GetUInt64(), arguments, boxes);
        using var content = new ByteArrayContent(signed);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-binary");
        using var response = await _algod.PostAsync("/v2/transactions", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var body = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var receipt = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (receipt.RootElement.GetProperty("txId").GetString() != id)
            throw new InvalidDataException("Algod returned an unexpected signed transaction ID.");

        for (var attempt = 0; attempt < 8; attempt++)
        {
            using var pending = await ReadBoxTransactionJsonAsync($"/v2/transactions/pending/{id}",
                cancellationToken).ConfigureAwait(false);
            if (pending.RootElement.TryGetProperty("confirmed-round", out var confirmed) && confirmed.GetUInt64() > 0)
                return;
            if (!string.IsNullOrEmpty(pending.RootElement.GetProperty("pool-error").GetString()))
                throw new InvalidOperationException($"Algod rejected record transaction {id}.");

            var waitRound = checked(round + (ulong)attempt).ToString(CultureInfo.InvariantCulture);
            using var wait = await ReadBoxTransactionJsonAsync($"/v2/status/wait-for-block-after/{waitRound}",
                cancellationToken).ConfigureAwait(false);
        }
        throw new InvalidOperationException($"Confirmation is unresolved for Algorand record transaction {id}.");
    }

    private async Task<JsonDocument> ReadBoxTransactionJsonAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await _algod.GetAsync(path, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

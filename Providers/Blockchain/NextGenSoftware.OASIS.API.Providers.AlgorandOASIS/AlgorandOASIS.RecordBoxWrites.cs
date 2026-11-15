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
        => await SaveStoredRecordAsync<Holon, IHolon>(holon, expectedVersion, "holon", cancellationToken).ConfigureAwait(false);

    private async Task<OASISResult<TContract>> SaveStoredRecordAsync<TRecord, TContract>(TContract holon, int expectedVersion,
        string kind, CancellationToken cancellationToken = default)
        where TRecord : class, TContract
        where TContract : class, IHolonBase
    {
        var result = new OASISResult<TContract>();
        try
        {
            var settings = _recordBoxes;
            if (settings?.WriterSeed == null || !IsProviderActivated)
                throw new InvalidOperationException("Activate an enrolled Algorand application with a writer before publishing records.");
            ArgumentNullException.ThrowIfNull(holon);
            ArgumentOutOfRangeException.ThrowIfNegative(expectedVersion);
            ArgumentOutOfRangeException.ThrowIfNegative(holon.Version);
            if (holon.Id == Guid.Empty)
                throw new ArgumentException("The proposed record must have a stable OASIS ID.", nameof(holon));

            var id = holon.Id;
            var proposal = OasisJson.DeserializeStored<TRecord>(OasisJson.Serialize(holon));
            if (proposal == null || proposal.Id != id)
                throw new InvalidDataException("Could not snapshot the proposed record identity.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(_algod.Timeout);
            var token = deadline.Token;
            var key = StoredRecordBoxKey(settings, id, kind);
            byte[] headName = [(byte)'h', ..key];
            var head = await ReadBoxAsync(settings, headName, token).ConfigureAwait(false);
            if (head?.Length == PurgeCheckpointLength)
                throw new InvalidOperationException("The Algorand record is fenced for permanent deletion.");
            using var codec = new EncryptedRecordCodec(settings.StorageKey, compress: kind != "holon");
            var current = head == null ? null : (await ReadStoredRecordAsync<TRecord>(settings, id, key,
                head, codec, kind, token).ConfigureAwait(false)).Record;
            if ((current?.Version ?? 0) != expectedVersion)
                throw new InvalidOperationException("The requested Algorand predecessor version is no longer current.");
            if (kind != "holon" && proposal.PreviousVersionId != (current?.VersionId ?? Guid.Empty))
                throw new InvalidOperationException("The proposed Algorand avatar record does not reference the current predecessor.");
            var version = checked(expectedVersion + 1);
            if (proposal.VersionId == Guid.Empty || proposal.Version != version || proposal.VersionId == current?.VersionId)
                proposal.VersionId = Guid.NewGuid();
            proposal.PreviousVersionId = current?.VersionId ?? Guid.Empty;
            proposal.Version = version;
            if (proposal is IHolon graph) graph.Children = [];
            proposal.IsNewHolon = false;
            proposal.ProviderUniqueStorageKey ??= [];
            proposal.ProviderUniqueStorageKey[Core.Enums.ProviderType.AlgorandOASIS] = id.ToString("D");

            var revision = RandomNumberGenerator.GetBytes(HashLength);
            var envelope = StrictUtf8.GetBytes(codec.Encrypt(StoredRecordContext(settings, id, revision, kind),
                OasisJson.Serialize(proposal), token));
            if (envelope.Length > MaximumChunks * MaximumChunkLength)
                throw new InvalidDataException("The encrypted Algorand record exceeds the contract's publication limit.");

            var count = (envelope.Length + MaximumChunkLength - 1) / MaximumChunkLength;
            var names = new byte[count + 3][];
            names[0] = headName;
            names[1] = BoxName('m', [..key, ..revision]);
            names[2] = [(byte)'d', ..key];
            var descriptor = head == null
                ? StrictUtf8.GetBytes(codec.Encrypt(DiscoveryContext(settings, key), kind == "holon" ? id.ToString("D") : $"{kind}:{id:D}", token))
                : await ReadBoxAsync(settings, names[2], token).ConfigureAwait(false)
                    ?? throw new InvalidDataException("The Algorand record is missing its discovery descriptor.");
            if (DecodeDiscoveredRecord(settings, codec, key, descriptor, token) != (kind, id))
                throw new InvalidDataException("The Algorand discovery descriptor identifies a different record.");
            await EnsureRecordApplicationFundingAsync(settings, head?.Length, envelope.Length,
                count, descriptor.Length, token).ConfigureAwait(false);
            var digests = new byte[count * HashLength];
            var calls = new AlgorandBoxTransaction.Call[count + 1];
            for (var index = 0; index < count; index++)
            {
                var offset = index * MaximumChunkLength;
                var payload = envelope[offset..Math.Min(offset + MaximumChunkLength, envelope.Length)];
                var indexBytes = new byte[8];
                BinaryPrimitives.WriteUInt64BigEndian(indexBytes, (ulong)index);
                var name = BoxName('c', [..key, ..revision, ..indexBytes]);
                names[index + 3] = name;
                SHA256.HashData(payload).CopyTo(digests, index * HashLength);
                calls[index] = new(["stage"u8.ToArray(), key, revision, indexBytes, payload], [headName, name]);
            }

            var predecessor = head == null ? EmptyHash : SHA256.HashData(head);
            calls[count] = new(["publish"u8.ToArray(), key, predecessor, revision, digests, descriptor], names);
            await SubmitBoxCallsAsync(settings, calls, token).ConfigureAwait(false);
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

    private Task SubmitBoxCallAsync(AlgorandRecordBoxSettings settings, byte[][] arguments, byte[][] boxes,
        CancellationToken cancellationToken)
        => SubmitBoxCallsAsync(settings, [new(arguments, boxes)], cancellationToken);

    private async Task SubmitBoxCallsAsync(AlgorandRecordBoxSettings settings, AlgorandBoxTransaction.Call[] calls,
        CancellationToken cancellationToken)
    {
        using var parameters = await ReadBoxTransactionJsonAsync("/v2/transactions/params", cancellationToken).ConfigureAwait(false);
        var native = parameters.RootElement;
        var genesis = ReadNetworkIdentity(native, "genesis-id", "genesis-hash");
        if (genesis.Hash != settings.GenesisHash)
            throw new InvalidDataException("Algod's signing parameters identify a different ledger.");
        var round = native.GetProperty("last-round").GetUInt64();
        var (signed, id) = AlgorandBoxTransaction.Sign(settings, genesis.Id, round,
            native.GetProperty("min-fee").GetUInt64(), native.GetProperty("fee").GetUInt64(), calls);
        using var content = new ByteArrayContent(signed);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-binary");
        using var response = await _algod.PostAsync("/v2/transactions", content, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            using var rejectedStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var rejection = await JsonDocument.ParseAsync(rejectedStream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var reason = rejection.RootElement.GetProperty("message").GetString();
            if (reason?.Length > 240)
                reason = reason[..240];
            throw new HttpRequestException($"Algod rejected record transaction {id}: {reason}", null, response.StatusCode);
        }
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

    private async Task EnsureRecordApplicationFundingAsync(AlgorandRecordBoxSettings settings, int? previousHeadLength,
        int encryptedLength, int chunks, int descriptorLength, CancellationToken cancellationToken)
    {
        const int nameLength = 33;
        const int boxBaseCost = 2500;
        const int byteCost = 400;
        const int namedBoxCost = boxBaseCost + byteCost * nameLength;
        var manifestLength = checked(ManifestHeaderLength + chunks * HashLength);
        var stagingCost = checked(chunks * namedBoxCost + encryptedLength * byteCost);
        var publicationCost = checked(namedBoxCost + manifestLength * byteCost +
            (previousHeadLength.HasValue ? (manifestLength - previousHeadLength.Value) * byteCost :
                2 * namedBoxCost + (manifestLength + descriptorLength) * byteCost));
        var required = (ulong)checked(stagingCost + Math.Max(0, publicationCost));
        var address = AlgorandBoxTransaction.ApplicationAddress(settings.ApplicationId);
        using var account = await ReadBoxTransactionJsonAsync($"/v2/accounts/{address}?exclude=all",
            cancellationToken).ConfigureAwait(false);
        var amount = account.RootElement.GetProperty("amount").GetUInt64();
        var minimum = account.RootElement.GetProperty("min-balance").GetUInt64();
        if (amount < minimum || amount - minimum < required)
            throw new InvalidOperationException("The Algorand application lacks the balance required to publish the encrypted record.");
    }

    private async Task<JsonDocument> ReadBoxTransactionJsonAsync(string path, CancellationToken cancellationToken)
    {
        using var response = await _algod.GetAsync(path, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

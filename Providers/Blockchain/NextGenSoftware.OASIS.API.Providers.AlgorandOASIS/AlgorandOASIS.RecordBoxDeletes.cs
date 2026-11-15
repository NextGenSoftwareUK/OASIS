using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
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
    private const int PurgeCheckpointLength = 2 * HashLength;
    private static readonly byte[] CompletedPurge = new byte[PurgeCheckpointLength];

    public async Task<OASISResult<IHolon>> DeleteStoredHolonAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            var settings = _recordBoxes;
            if (settings?.WriterSeed == null || !IsProviderActivated)
                throw new InvalidOperationException("Activate an enrolled Algorand record writer before deletion.");
            if (id == Guid.Empty)
                throw new ArgumentException("A stored holon ID is required.", nameof(id));
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(_algod.Timeout);
            var token = deadline.Token;
            var key = StoredRecordBoxKey(settings, id);
            byte[] headName = [(byte)'h', ..key];
            var head = await ReadBoxAsync(settings, headName, token).ConfigureAwait(false);
            if (head == null)
                return new OASISResult<IHolon>
                {
                    IsError = true,
                    ErrorCode = OASISResultErrorCodes.RecordNotFound,
                    Message = "The requested Algorand record was not found."
                };

            IHolon? original = null;
            var changed = false;
            if (head.Length != PurgeCheckpointLength)
            {
                using var codec = new EncryptedRecordCodec(settings.StorageKey);
                original = (await ReadStoredHolonAsync(settings, id, key, head, codec, token)
                    .ConfigureAwait(false)).Record;
                var digest = SHA256.HashData(head);
                await SubmitBoxCallAsync(settings, ["freeze"u8.ToArray(), key, digest],
                    [headName, [(byte)'d', ..key]], token).ConfigureAwait(false);
                head = await ReadExpectedCheckpointAsync(settings, headName, [..head[..HashLength], ..digest], token)
                    .ConfigureAwait(false);
                changed = true;
            }

            changed |= await PurgeFrozenRecordAsync(settings, key, head, token).ConfigureAwait(false);

            result.Result = original;
            result.IsDeleted = true;
            result.DeletedCount = changed ? 1 : 0;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            OASISErrorHandling.HandleError(ref result, "Could not confirm completion of the Algorand record purge.", exception);
        }
        return result;
    }

    private async Task<bool> DeleteEnrolledAvatarPermanentlyAsync(Guid id, AlgorandRecordBoxSettings settings,
        CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_algod.Timeout);
        var token = deadline.Token;
        var avatarKey = StoredRecordBoxKey(settings, id, "avatar");
        var detailKey = StoredRecordBoxKey(settings, id, "avatar-detail");
        byte[] avatarHeadName = [(byte)'h', ..avatarKey];
        byte[] detailHeadName = [(byte)'h', ..detailKey];
        var avatarRead = ReadBoxAsync(settings, avatarHeadName, token);
        var detailRead = ReadBoxAsync(settings, detailHeadName, token);
        await Task.WhenAll(avatarRead, detailRead).ConfigureAwait(false);
        var avatarHead = await avatarRead.ConfigureAwait(false);
        var detailHead = await detailRead.ConfigureAwait(false);
        if (avatarHead == null)
            return false;

        using var codec = new EncryptedRecordCodec(settings.StorageKey, compress: true);
        var freezes = new List<AlgorandBoxTransaction.Call>(2);
        if (avatarHead.Length != PurgeCheckpointLength)
        {
            await ReadStoredRecordAsync<Avatar>(settings, id, avatarKey, avatarHead, codec, "avatar", token)
                .ConfigureAwait(false);
            freezes.Add(PrepareFreeze(avatarKey, avatarHeadName, avatarHead));
        }
        if (detailHead?.Length != PurgeCheckpointLength)
        {
            if (detailHead != null)
                await ReadStoredRecordAsync<AvatarDetail>(settings, id, detailKey, detailHead, codec, "avatar-detail", token)
                    .ConfigureAwait(false);
            freezes.Add(PrepareFreeze(detailKey, detailHeadName, detailHead));
        }

        if (freezes.Count != 0)
        {
            await SubmitBoxCallsAsync(settings, freezes.ToArray(), token).ConfigureAwait(false);
            if (avatarHead.Length != PurgeCheckpointLength)
                avatarHead = await ReadExpectedCheckpointAsync(settings, avatarHeadName, FreezeCheckpoint(avatarHead), token)
                    .ConfigureAwait(false);
            if (detailHead?.Length != PurgeCheckpointLength)
                detailHead = await ReadExpectedCheckpointAsync(settings, detailHeadName, FreezeCheckpoint(detailHead), token)
                    .ConfigureAwait(false);
        }

        await PurgeFrozenRecordAsync(settings, avatarKey, avatarHead, token).ConfigureAwait(false);
        if (detailHead != null)
            await PurgeFrozenRecordAsync(settings, detailKey, detailHead, token).ConfigureAwait(false);
        return true;
    }

    private static AlgorandBoxTransaction.Call PrepareFreeze(byte[] key, byte[] headName, byte[]? head) =>
        new(["freeze"u8.ToArray(), key, head == null ? EmptyHash : SHA256.HashData(head)], [headName, [(byte)'d', ..key]]);

    private static byte[] FreezeCheckpoint(byte[]? head) => head == null ? CompletedPurge : [..head[..HashLength], ..SHA256.HashData(head)];

    private async Task<bool> PurgeFrozenRecordAsync(AlgorandRecordBoxSettings settings, byte[] key, byte[] head,
        CancellationToken token)
    {
        byte[] headName = [(byte)'h', ..key];
        var changed = false;
        for (var depth = 0; !head.AsSpan().SequenceEqual(CompletedPurge); depth++)
        {
            if (depth == MaximumHistoryDepth)
                throw new InvalidDataException("The Algorand purge exceeds its traversal limit and can be resumed.");
            var revision = head[..HashLength];
            var digest = head[HashLength..];
            if (revision.AsSpan().SequenceEqual(EmptyHash) || digest.AsSpan().SequenceEqual(EmptyHash))
                throw new InvalidDataException("The Algorand purge checkpoint is invalid.");
            var manifestName = BoxName('m', [..key, ..revision]);
            var manifest = await ReadBoxAsync(settings, manifestName, token).ConfigureAwait(false);
            if (manifest == null || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(manifest), digest) ||
                !manifest.AsSpan(0, HashLength).SequenceEqual(revision))
                throw new InvalidDataException("The Algorand purge manifest does not match the recorded checkpoint.");
            var digestLength = manifest.Length - ManifestHeaderLength;
            if (digestLength < HashLength || digestLength > MaximumChunks * HashLength || digestLength % HashLength != 0)
                throw new InvalidDataException("The Algorand purge manifest has an invalid length.");
            var references = new byte[2 + digestLength / HashLength][];
            references[0] = headName;
            references[1] = manifestName;
            for (var index = 0; index < references.Length - 2; index++)
            {
                var ordinal = new byte[8];
                BinaryPrimitives.WriteUInt64BigEndian(ordinal, (ulong)index);
                references[index + 2] = BoxName('c', [..key, ..revision, ..ordinal]);
            }
            await SubmitBoxCallAsync(settings, ["purge"u8.ToArray(), key, head], references, token)
                .ConfigureAwait(false);
            head = await ReadExpectedCheckpointAsync(settings, headName, manifest[HashLength..ManifestHeaderLength], token)
                .ConfigureAwait(false);
            changed = true;
        }

        return changed;
    }

    private async Task<byte[]> ReadExpectedCheckpointAsync(AlgorandRecordBoxSettings settings, byte[] name, byte[] expected,
        CancellationToken cancellationToken)
    {
        var checkpoint = await ReadBoxAsync(settings, name, cancellationToken).ConfigureAwait(false);
        if (checkpoint == null || !checkpoint.AsSpan().SequenceEqual(expected))
            throw new InvalidDataException("The Algorand purge checkpoint changed. Read the current checkpoint before resuming.");
        return checkpoint;
    }
}

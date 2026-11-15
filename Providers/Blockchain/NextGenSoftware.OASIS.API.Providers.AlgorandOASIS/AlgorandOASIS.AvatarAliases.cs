using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private const ulong AliasBoxFunding = 2500 + 400 * (33 + HashLength);

    private static byte[]? AvatarAlias(AlgorandRecordBoxSettings settings, string kind, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return HMACSHA256.HashData(settings.StorageKey,
            StrictUtf8.GetBytes($"oasis:algorand:avatar-alias:v1:{kind}:{value.Trim().ToUpperInvariant()}"));
    }

    private async Task<AlgorandBoxTransaction.Call[]> PrepareAvatarAliasesAsync(AlgorandRecordBoxSettings settings,
        byte[] owner, IAvatar? before, IAvatar? after, CancellationToken cancellationToken)
    {
        var username = PlanAliasAsync(settings, owner, "username", before?.Username, after?.Username, cancellationToken);
        var email = PlanAliasAsync(settings, owner, "email", before?.Email, after?.Email, cancellationToken);
        var operations = await Task.WhenAll(username, email).ConfigureAwait(false);
        return [.. operations.SelectMany(calls => calls)];
    }

    private async Task<List<AlgorandBoxTransaction.Call>> PlanAliasAsync(AlgorandRecordBoxSettings settings, byte[] owner,
        string kind, string? before, string? after, CancellationToken cancellationToken)
    {
        var prior = AvatarAlias(settings, kind, before);
        var next = AvatarAlias(settings, kind, after);
        var changes = new List<AlgorandBoxTransaction.Call>(2);
        if (prior != null && (next == null || !prior.AsSpan().SequenceEqual(next)))
        {
            var current = await ReadBoxAsync(settings, [(byte)'a', ..prior], cancellationToken).ConfigureAwait(false);
            if (current != null)
            {
                if (!current.AsSpan().SequenceEqual(owner))
                    throw new InvalidDataException("The prior Algorand alias belongs to a different record.");
                changes.Add(AliasCall(prior, owner, EmptyHash));
            }
        }

        if (next != null)
        {
            var current = await ReadBoxAsync(settings, [(byte)'a', ..next], cancellationToken).ConfigureAwait(false);
            if (current == null)
                changes.Add(AliasCall(next, EmptyHash, owner));
            else if (!current.AsSpan().SequenceEqual(owner))
                throw new InvalidOperationException($"The Algorand avatar {kind} is already assigned.");
        }

        return changes;
    }

    private static AlgorandBoxTransaction.Call AliasCall(byte[] alias, byte[] expected, byte[] next)
    {
        byte[] name = [(byte)'a', ..alias];
        byte[][] boxes = next.AsSpan().SequenceEqual(EmptyHash) ? [name] : [name, [(byte)'h', ..next]];
        return new(["set_alias"u8.ToArray(), alias, expected, next], boxes);
    }

    private async Task<OASISResult<IAvatar>> LoadEnrolledAvatarByAliasAsync(string kind, string value, int version)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            var settings = _recordBoxes;
            if (settings == null || !IsProviderActivated)
                throw new InvalidOperationException("Activate an enrolled Algorand application before reading the avatar index.");
            ArgumentOutOfRangeException.ThrowIfNegative(version);
            var alias = AvatarAlias(settings, kind, value);
            if (alias == null)
                throw new ArgumentException("An avatar lookup value is required.", nameof(value));
            using var deadline = new CancellationTokenSource(_algod.Timeout);
            var token = deadline.Token;
            var owner = await ReadBoxAsync(settings, [(byte)'a', ..alias], token).ConfigureAwait(false);
            if (owner == null)
                return new OASISResult<IAvatar>
                {
                    IsError = true,
                    ErrorCode = OASISResultErrorCodes.RecordNotFound,
                    Message = "The enrolled Algorand avatar was not found."
                };
            if (owner.Length != HashLength || owner.AsSpan().SequenceEqual(EmptyHash))
                throw new InvalidDataException("The Algorand avatar alias owner is invalid.");

            var descriptor = await ReadBoxAsync(settings, [(byte)'d', ..owner], token).ConfigureAwait(false)
                ?? throw new InvalidDataException("The Algorand avatar alias no longer identifies a discoverable record.");
            using var codec = new EncryptedRecordCodec(settings.StorageKey, compress: true);
            var (recordKind, id) = DecodeDiscoveredRecord(settings, codec, owner, descriptor, token);
            if (recordKind != "avatar")
                throw new InvalidDataException("The Algorand alias belongs to a non-avatar record.");
            result = await LoadStoredRecordAsync<Avatar, IAvatar>(id, 0, "avatar", token).ConfigureAwait(false);
            if (result.IsError || result.Result == null)
                return result;
            var candidate = AvatarAlias(settings, kind, kind == "username" ? result.Result.Username : result.Result.Email);
            if (candidate == null || !CryptographicOperations.FixedTimeEquals(alias, candidate))
                throw new InvalidDataException("The Algorand avatar index changed during lookup.");
            return version == 0 || version == result.Result.Version
                ? result
                : await LoadStoredRecordAsync<Avatar, IAvatar>(id, version, "avatar", token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "Could not load the enrolled Algorand avatar alias.", exception);
            return result;
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

/// <summary>Private admission projection. Never stores credentials or the full avatar.</summary>
public sealed class BootstrapAuthorityRecord
{
    public string Id { get; set; }
    public long Revision { get; set; }
    public bool Ready { get; set; }
    public bool Enabled { get; set; }
    // Nullable deliberately: AvatarType's zero/default value is Wizard, not an unprivileged role.
    public AvatarType? Role { get; set; }
    public long TokenVersion { get; set; }
    public long KarmaVersion { get; set; }
    public long Karma { get; set; }
    public DateTime VerifiedUntilUtc { get; set; }
}

/// <summary>
/// Mongo-only authority: no ProviderManager, native reader or paid-chain fallback.
/// Writers must invalidate BEFORE changing canonical security/karma state, then publish
/// against the exact returned revision. A failed or uncertain write stays unavailable.
/// </summary>
public sealed class MongoBootstrapAuthority
{
    private readonly IMongoCollection<BootstrapAuthorityRecord> _records;
    private readonly TimeProvider _clock;
    public MongoBootstrapAuthority(IMongoDatabase database, TimeProvider clock = null)
    {
        ArgumentNullException.ThrowIfNull(database);
        _clock = clock ?? TimeProvider.System;
        _records = database.GetCollection<BootstrapAuthorityRecord>("private_bootstrap_authority")
            .WithReadConcern(ReadConcern.Majority).WithReadPreference(ReadPreference.Primary)
            .WithWriteConcern(WriteConcern.WMajority);
    }

    public async Task<BootstrapAuthorityRecord> ReadAsync(Guid avatarId, long tokenVersion,
        CancellationToken token = default)
    {
        RequireId(avatarId);
        var record = await _records.Find(x => x.Id == avatarId.ToString("D"))
            .SingleOrDefaultAsync(token);
        token.ThrowIfCancellationRequested();
        // Never reuse a timestamp captured before a slow database read.
        Validate(record, avatarId, tokenVersion, _clock.GetUtcNow().UtcDateTime);
        return record;
    }

    // No upsert: creating the initial trusted projection is a separate reviewed provisioning operation.
    public async Task<long> InvalidateAsync(Guid avatarId, long expectedRevision, CancellationToken token = default)
    {
        RequireId(avatarId);
        if (expectedRevision < 1 || expectedRevision == long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(expectedRevision));
        var result = await _records.UpdateOneAsync(
            x => x.Id == avatarId.ToString("D") && x.Revision == expectedRevision && x.Ready,
            Builders<BootstrapAuthorityRecord>.Update.Set(x => x.Ready, false).Inc(x => x.Revision, 1),
            cancellationToken: token);
        if (result.ModifiedCount != 1)
            throw new InvalidOperationException("Bootstrap invalidation conflicted; canonical mutation must not start.");
        return expectedRevision + 1;
    }

    public async Task PublishAsync(BootstrapAuthorityRecord verified,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(verified);
        if (!Guid.TryParseExact(verified.Id, "D", out var id) || id == Guid.Empty)
            throw new ArgumentException("A canonical avatar ID is required.", nameof(verified));
        ValidateShape(verified, _clock.GetUtcNow().UtcDateTime);
        if (!verified.Ready) throw new ArgumentException("Published authority must be ready.", nameof(verified));
        var result = await _records.ReplaceOneAsync(
            x => x.Id == verified.Id && x.Revision == verified.Revision && !x.Ready
                && (x.TokenVersion < verified.TokenVersion
                    || (x.TokenVersion == verified.TokenVersion && x.Enabled == verified.Enabled && x.Role == verified.Role))
                && (x.KarmaVersion < verified.KarmaVersion
                    || (x.KarmaVersion == verified.KarmaVersion && x.Karma == verified.Karma)),
            verified, cancellationToken: token);
        if (result.ModifiedCount != 1)
            throw new InvalidOperationException("Bootstrap publication conflicted; authority remains unavailable.");
    }

    public static void Validate(BootstrapAuthorityRecord record, Guid avatarId, long tokenVersion, DateTime utcNow)
    {
        RequireId(avatarId);
        if (tokenVersion < 0) throw new ArgumentOutOfRangeException(nameof(tokenVersion));
        if (record == null || record.Id != avatarId.ToString("D"))
            throw new InvalidOperationException("Bootstrap authority is missing or mismatched.");
        ValidateShape(record, utcNow);
        if (!record.Ready) throw new InvalidOperationException("Bootstrap authority has an unfinished mutation.");
        if (!record.Enabled || record.TokenVersion != tokenVersion)
            throw new UnauthorizedAccessException("Avatar disabled or token revoked.");
    }

    private static void ValidateShape(BootstrapAuthorityRecord record, DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc) throw new ArgumentException("An explicit UTC time is required.", nameof(utcNow));
        if (record.Revision < 1 || record.TokenVersion < 0 || record.KarmaVersion < 1
            || !record.Role.HasValue || !Enum.IsDefined(typeof(AvatarType), record.Role.Value)
            || record.VerifiedUntilUtc.Kind != DateTimeKind.Utc || record.VerifiedUntilUtc <= utcNow)
            throw new InvalidOperationException("Bootstrap authority is invalid or expired.");
    }

    private static void RequireId(Guid id)
    { if (id == Guid.Empty) throw new ArgumentException("A nonempty avatar ID is required.", nameof(id)); }
}

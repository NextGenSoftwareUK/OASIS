using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.NearbySync;

namespace NextGenSoftware.OASIS.Edge.NearbySync.UnitTests;

internal sealed class EchoExchangeHandler : INearbyExchangeHandler
{
    public int Calls;
    public Task<OASISResult<SyncExchangeResponse>> HandleAsync(SyncExchangeRequest request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref Calls);
        return Task.FromResult(new OASISResult<SyncExchangeResponse>
        {
            Result = new SyncExchangeResponse { NextPullCheckpoint = "cp:" + request.PullCheckpoint }
        });
    }
}

internal static class NearbyTestSupport
{
    public static readonly Guid AvatarId = Guid.NewGuid();

    public static SyncExchangeRequest Request(Guid? avatar = null) => new()
    {
        DeviceId = Guid.NewGuid(),
        AvatarId = avatar ?? AvatarId,
        PullCheckpoint = "start"
    };

    /// <summary>Makes <paramref name="a"/> and <paramref name="b"/> trust each other, binding each to an avatar.</summary>
    public static void Pair(NearbyIdentity a, Guid avatarOfA, NearbyIdentity b, Guid avatarOfB)
    {
        a.TrustPeer(b.PublicKey, avatarOfB);
        b.TrustPeer(a.PublicKey, avatarOfA);
    }
}

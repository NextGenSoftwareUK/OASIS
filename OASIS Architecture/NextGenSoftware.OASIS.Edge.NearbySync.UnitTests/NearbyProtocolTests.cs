using FluentAssertions;
using Xunit;

namespace NextGenSoftware.OASIS.Edge.NearbySync.UnitTests;

public class NearbyProtocolTests
{
    [Fact]
    public void Envelope_RoundTrips_AndTamperingBreaksTheSignature()
    {
        using var alice = new NearbyIdentity();
        using var bob = new NearbyIdentity();
        var avatar = Guid.NewGuid();
        bob.TrustPeer(alice.PublicKey, avatar);

        var envelope = new NearbyEnvelope
        {
            Id = Guid.NewGuid(), CorrelationId = Guid.NewGuid(), Kind = NearbyEnvelopeKind.Request,
            SenderNodeId = alice.NodeId, RecipientNodeId = bob.NodeId, HopsRemaining = 3, Payload = new byte[] { 1, 2, 3 }
        };
        envelope.Signature = alice.Sign(envelope.GetSignedBytes());

        NearbyEnvelope.TryParse(envelope.ToBytes(), out var parsed).Should().BeTrue();
        bob.TryVerify(parsed.SenderNodeId, parsed.GetSignedBytes(), parsed.Signature, out var verifiedAvatar).Should().BeTrue();
        verifiedAvatar.Should().Be(avatar);

        parsed.Payload[0] = 99;
        bob.TryVerify(parsed.SenderNodeId, parsed.GetSignedBytes(), parsed.Signature, out _).Should().BeFalse();
    }

    [Fact]
    public void Relays_CanChangeHopCountWithoutBreakingTheSignature()
    {
        using var alice = new NearbyIdentity();
        using var bob = new NearbyIdentity();
        bob.TrustPeer(alice.PublicKey, Guid.NewGuid());
        var envelope = new NearbyEnvelope { Id = Guid.NewGuid(), SenderNodeId = alice.NodeId, RecipientNodeId = bob.NodeId, HopsRemaining = 4, Kind = NearbyEnvelopeKind.Request };
        envelope.Signature = alice.Sign(envelope.GetSignedBytes());
        envelope.HopsRemaining = 1;
        bob.TryVerify(alice.NodeId, envelope.GetSignedBytes(), envelope.Signature, out _).Should().BeTrue();
    }

    [Fact]
    public void Parse_RejectsGarbage_WithoutThrowing()
    {
        NearbyEnvelope.TryParse(new byte[] { 1, 255, 255, 255, 127 }, out _).Should().BeFalse();
        NearbyEnvelope.TryParse(Array.Empty<byte>(), out _).Should().BeFalse();
        NearbyEnvelope.TryParse(new byte[64], out _).Should().BeFalse();
    }

    [Fact]
    public void UnknownPeer_IsNeverVerified()
    {
        using var alice = new NearbyIdentity();
        using var stranger = new NearbyIdentity();
        var data = new byte[] { 1 };
        alice.TryVerify(stranger.NodeId, data, stranger.Sign(data), out _).Should().BeFalse();
    }

    [Fact]
    public void Identity_RestoresFromExportedKey()
    {
        using var first = new NearbyIdentity();
        using var restored = new NearbyIdentity(first.ExportPrivateKey());
        restored.NodeId.Should().Be(first.NodeId);
    }

    [Theory]
    [InlineData(0, 185)]
    [InlineData(1, 185)]
    [InlineData(200_000, 185)]
    [InlineData(5000, 20)]
    public void ChunkAndReassemble_RoundTripsOutOfOrder(int size, int frameSize)
    {
        var message = new byte[size];
        new Random(7).NextBytes(message);
        var frames = NearbyFrameChunker.Split(message, frameSize, 42).Reverse().ToList();
        frames.Should().OnlyContain(f => f.Length <= frameSize);
        var reassembler = new NearbyReassembler();
        byte[] result = null;
        foreach (var f in frames) result = reassembler.Add("p", f) ?? result;
        result.Should().Equal(message);
        reassembler.PendingCount.Should().Be(0);
    }

    [Fact]
    public void Reassembler_ExpiresStalledMessages_AndCapsPartials()
    {
        var now = DateTime.UtcNow;
        var reassembler = new NearbyReassembler(maximumPartials: 2, timeout: TimeSpan.FromSeconds(10), clock: () => now);
        var frames = NearbyFrameChunker.Split(new byte[500], 100, 1);
        reassembler.Add("p", frames[0]);
        reassembler.Add("p", NearbyFrameChunker.Split(new byte[500], 100, 2)[0]);
        reassembler.Add("p", NearbyFrameChunker.Split(new byte[500], 100, 3)[0]).Should().BeNull();
        reassembler.PendingCount.Should().Be(2);
        now = now.AddSeconds(11);
        reassembler.Add("p", NearbyFrameChunker.Split(new byte[500], 100, 4)[0]);
        reassembler.PendingCount.Should().Be(1);
    }

    [Fact]
    public void Dedupe_AcceptsEachIdOnce()
    {
        var cache = new NearbyDedupeCache(2);
        var a = Guid.NewGuid();
        cache.TryAdd(a).Should().BeTrue();
        cache.TryAdd(a).Should().BeFalse();
        cache.TryAdd(Guid.NewGuid()); cache.TryAdd(Guid.NewGuid());
        cache.TryAdd(a).Should().BeTrue();
    }
}

public class ConnectivityRoutedSyncTransportTests
{
    private sealed class Marker : NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization.IHyperDriveSyncTransport
    {
        private readonly string _name;
        public Marker(string name) => _name = name;
        public Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization.SyncExchangeResponse>> ExchangeAsync(
            NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization.SyncExchangeRequest request, CancellationToken ct) =>
            Task.FromResult(new NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization.SyncExchangeResponse>
            { Result = new NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization.SyncExchangeResponse { NextPullCheckpoint = _name } });
    }

    [Theory]
    [InlineData(true, "hosted")]
    [InlineData(false, "nearby")]
    public async Task RoutesByConnectivity(bool online, string expected)
    {
        var transport = new ConnectivityRoutedSyncTransport(new Marker("hosted"), new Marker("nearby"), () => online);
        var result = await transport.ExchangeAsync(new NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization.SyncExchangeRequest(), CancellationToken.None);
        result.Result.NextPullCheckpoint.Should().Be(expected);
    }
}

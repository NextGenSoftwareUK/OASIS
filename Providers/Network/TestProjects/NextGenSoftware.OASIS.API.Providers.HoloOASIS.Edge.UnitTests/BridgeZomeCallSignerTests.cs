using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Runtime.Holochain;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge.UnitTests;

public sealed class BridgeZomeCallSignerTests
{
    [Fact]
    public async Task Signs_as_the_cell_agent_without_a_cap_secret()
    {
        var bridge = new SigningBridge(new OASISResult<HolochainSignedZomeCall>(
            new HolochainSignedZomeCall { Bytes = new byte[] { 1 }, Signature = new byte[64] }));
        var call = new ZomeCallToSign
        {
            CellIdDnaHash = new byte[] { 7 }, CellIdAgentPubKey = new byte[] { 9 }, ZomeName = "oasis",
            FnName = "get_all_avatars", Payload = new byte[] { 0xc0 }, Nonce = new byte[32], ExpiresAt = 42
        };

        var signed = await new BridgeZomeCallSigner(bridge).SignZomeCallAsync(call);

        signed.bytes.Should().Equal(1);
        signed.signature.Should().HaveCount(64);
        bridge.Last.Provenance.Should().Equal(9);
        bridge.Last.AgentPubKey.Should().Equal(9);
        bridge.Last.DnaHash.Should().Equal(7);
        bridge.Last.CapSecret.Should().BeNull();
        bridge.Last.ExpiresAt.Should().Be(42);
    }

    [Fact]
    public async Task Bridge_errors_surface_as_exceptions()
    {
        var bridge = new SigningBridge(new OASISResult<HolochainSignedZomeCall>
        {
            IsError = true, ErrorCode = "HOLO_ANDROID_SIGN_FAILED", Message = "no agent"
        });

        var act = () => new BridgeZomeCallSigner(bridge).SignZomeCallAsync(new ZomeCallToSign());

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*HOLO_ANDROID_SIGN_FAILED*");
    }

    private sealed class SigningBridge : IHolochainZomeCallSigningBridge
    {
        private readonly OASISResult<HolochainSignedZomeCall> _result;
        public HolochainZomeCallToSign Last { get; private set; }
        public SigningBridge(OASISResult<HolochainSignedZomeCall> result) => _result = result;

        public Task<OASISResult<HolochainSignedZomeCall>> SignZomeCallAsync(
            HolochainZomeCallToSign call, CancellationToken cancellationToken)
        {
            Last = call;
            return Task.FromResult(_result);
        }
    }
}

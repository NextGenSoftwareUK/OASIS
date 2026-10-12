using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.OASIS.Edge.Runtime.Holochain;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge
{
    /// <summary>
    /// HoloNET zome-call signer backed by the Android service bridge: the conductor signs with the
    /// agent key it holds, so HoloNET never needs admin access to authorize its own credentials.
    /// </summary>
    public sealed class BridgeZomeCallSigner : IZomeCallSigner
    {
        private readonly IHolochainZomeCallSigningBridge _bridge;

        public BridgeZomeCallSigner(IHolochainZomeCallSigningBridge bridge) =>
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));

        public async Task<ZomeCallParamsSigned> SignZomeCallAsync(ZomeCallToSign call)
        {
            if (call == null) throw new ArgumentNullException(nameof(call));
            var signed = await _bridge.SignZomeCallAsync(new HolochainZomeCallToSign
            {
                // The app's own agent makes the call, so no cap secret is needed.
                Provenance = call.CellIdAgentPubKey,
                DnaHash = call.CellIdDnaHash,
                AgentPubKey = call.CellIdAgentPubKey,
                ZomeName = call.ZomeName,
                FnName = call.FnName,
                Payload = call.Payload,
                Nonce = call.Nonce,
                ExpiresAt = call.ExpiresAt
            }, CancellationToken.None).ConfigureAwait(false);

            if (signed == null || signed.IsError || signed.Result?.Bytes == null || signed.Result.Signature == null)
                throw new InvalidOperationException(
                    $"The Holochain conductor did not sign the zome call: {signed?.ErrorCode} {signed?.Message}".Trim());
            return new ZomeCallParamsSigned { bytes = signed.Result.Bytes, signature = signed.Result.Signature };
        }
    }
}

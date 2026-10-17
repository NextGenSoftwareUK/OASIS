using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Runtime.Holochain
{
    /// <summary>Immutable input for the lightweight Android Holochain service boundary.</summary>
    public sealed class HolochainAndroidRuntimeOptions
    {
        public byte[] AppBundle { get; set; }
        public string InstalledAppId { get; set; } = "oasis";
        public string NetworkSeed { get; set; }
        public string BootstrapUrl { get; set; } = "https://dev-test-bootstrap2.holochain.org";
        public string RelayUrl { get; set; } = "https://use1-1.relay.n0.iroh-canary.iroh.link./";

        /// <summary>
        /// Use the phone's shared Holochain Service Runtime app when it is installed (one conductor for
        /// every hApp). The player approves access once via a notification; if they don't within
        /// <see cref="SharedRuntimeApprovalTimeoutMs"/>, or shared setup fails, the failure is reported
        /// and logged before automatically switching to the built-in conductor. Embedded mode is also
        /// used when the shared runtime is missing or this preference is disabled.
        /// </summary>
        public bool PreferSharedRuntime { get; set; } = true;
        public long SharedRuntimeApprovalTimeoutMs { get; set; } = 60_000;
    }

    /// <summary>
    /// Platform-neutral Edge boundary for the official Android foreground service. Keeping this
    /// contract in Edge Runtime prevents Unity JNI code from depending on the full OASIS provider graph.
    /// </summary>
    public interface IHolochainAndroidServiceBridge : IAsyncDisposable
    {
        Task<OASISResult<HolochainAndroidAppSession>> StartAndSetupAsync(
            HolochainAndroidRuntimeOptions options, CancellationToken cancellationToken);
        Task<OASISResult<bool>> SuspendAsync(CancellationToken cancellationToken);
        Task<OASISResult<HolochainAndroidAppSession>> ResumeAsync(
            string installedAppId, CancellationToken cancellationToken);
        Task<OASISResult<bool>> StopAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Optional bridge capability: sign a zome call with the agent key held by the conductor. Needed
    /// because an app using the Android service has no admin access to grant itself signing credentials.
    /// </summary>
    public interface IHolochainZomeCallSigningBridge
    {
        Task<OASISResult<HolochainSignedZomeCall>> SignZomeCallAsync(
            HolochainZomeCallToSign call, CancellationToken cancellationToken);
    }

    public sealed class HolochainZomeCallToSign
    {
        public byte[] Provenance { get; set; }
        public byte[] DnaHash { get; set; }
        public byte[] AgentPubKey { get; set; }
        public string ZomeName { get; set; }
        public string FnName { get; set; }
        public byte[] CapSecret { get; set; }
        public byte[] Payload { get; set; }
        public byte[] Nonce { get; set; }
        /// <summary>Microseconds since the Unix epoch.</summary>
        public long ExpiresAt { get; set; }
    }

    public sealed class HolochainSignedZomeCall
    {
        public byte[] Bytes { get; set; }
        public byte[] Signature { get; set; }
    }

    public sealed class HolochainAndroidAppSession
    {
        public int Port { get; set; }
        public byte[] AuthenticationToken { get; set; }
        /// <summary>"shared" or "embedded" when the bridge reports it; null otherwise.</summary>
        public string Mode { get; set; }
    }
}

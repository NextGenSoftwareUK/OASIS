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

    public sealed class HolochainAndroidAppSession
    {
        public int Port { get; set; }
        public byte[] AuthenticationToken { get; set; }
    }
}

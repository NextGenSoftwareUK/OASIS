using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>
/// Asks the container supervisor to start a source's sidecar before it is used. OASIS_SIDECAR_CONTROL is set only
/// by the supervisor when sidecars run on demand (OASIS_SANDBOX_IDLE_MINUTES); otherwise they are already running.
/// </summary>
internal static class CqlSandboxSidecar
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(90) };

    public static async Task EnsureAsync(ProviderType provider, CancellationToken cancellationToken)
    {
        var control = Environment.GetEnvironmentVariable("OASIS_SIDECAR_CONTROL");
        if (string.IsNullOrEmpty(control)) return;
        using var response = await Client.PostAsync(new Uri(new Uri(control), "ensure/" + provider), null, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("CQL_SOURCE_ACTIVATION_FAILED");
    }
}

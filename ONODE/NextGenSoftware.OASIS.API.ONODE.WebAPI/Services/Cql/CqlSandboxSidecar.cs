using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>
/// Leases a source's sidecar from the container supervisor, which starts it if stopped and does not stop it while leased.
/// OASIS_SIDECAR_CONTROL is set only by the supervisor when sidecars run on demand (OASIS.Sandbox.Sidecars.OnDemand);
/// without it every sidecar is already running and a lease does nothing.
/// </summary>
internal static class CqlSandboxSidecar
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(90) };

    private static Uri Control(string path)
    {
        var control = Environment.GetEnvironmentVariable("OASIS_SIDECAR_CONTROL");
        return string.IsNullOrEmpty(control) ? null : new Uri(new Uri(control), path);
    }

    private static readonly object Sync = new();
    private static System.Collections.Generic.HashSet<string> deferred;

    /// <summary>
    /// True when this source's sidecar starts only for a real query: the supervisor runs it on demand and it is not
    /// always on. Start-up must then neither start the sidecar nor activate the source.
    /// </summary>
    public static bool IsDeferred(ProviderType provider)
    {
        var status = Control("status");
        if (status == null) return false;
        lock (Sync)
        {
            if (deferred == null)
            {
                using var response = Client.Send(new HttpRequestMessage(HttpMethod.Get, status));
                Require(response);
                using var json = System.Text.Json.JsonDocument.Parse(response.Content.ReadAsStream());
                var names = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                foreach (var item in json.RootElement.EnumerateArray())
                    if (!item.GetProperty("alwaysOn").GetBoolean()) names.Add(item.GetProperty("name").GetString());
                deferred = names;
            }
            return deferred.Contains(provider.ToString());
        }
    }

    /// <summary>For the synchronous source resolver. Returns the action that releases the lease, or null if none was taken.</summary>
    public static Action Acquire(ProviderType provider)
    {
        var ensure = Control("ensure/" + provider);
        if (ensure == null) return null;
        using (var response = Client.Send(new HttpRequestMessage(HttpMethod.Post, ensure))) Require(response);
        return () => { using var released = Client.Send(new HttpRequestMessage(HttpMethod.Post, Control("release/" + provider))); };
    }

    /// <summary>Holds a lease while an asynchronous operation, such as source activation, runs.</summary>
    public static async Task<IAsyncDisposable> AcquireAsync(ProviderType provider, CancellationToken cancellationToken)
    {
        var ensure = Control("ensure/" + provider);
        if (ensure == null) return new Lease(null);
        // Not cancellable once sent: the supervisor counts the lease, so its answer is needed to release it.
        cancellationToken.ThrowIfCancellationRequested();
        using (var response = await Client.PostAsync(ensure, null, CancellationToken.None)) Require(response);
        return new Lease(Control("release/" + provider));
    }

    /// <summary>The supervisor's JSON list of sidecar states, or null when sidecars do not run on demand.</summary>
    public static async Task<string> StatusAsync(CancellationToken cancellationToken)
    {
        var status = Control("status");
        return status == null ? null : await Client.GetStringAsync(status, cancellationToken);
    }

    private static void Require(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("CQL_SOURCE_SIDECAR_UNAVAILABLE: the source's sidecar failed to start.");
    }

    private sealed class Lease : IAsyncDisposable
    {
        private readonly Uri release;
        public Lease(Uri release) => this.release = release;
        public async ValueTask DisposeAsync()
        {
            if (release != null) { using var released = await Client.PostAsync(release, null, CancellationToken.None); }
        }
    }
}

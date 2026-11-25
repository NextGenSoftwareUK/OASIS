using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Weaviate.Client;

namespace NextGenSoftware.OASIS.API.Providers.WeaviateOASIS;

/// <summary>
/// Read-only hosted CQL source over WeaviateOASIS: a restricted reader API key and an existing public
/// probe. Activation never creates schema; it builds the gRPC query client and proves the probe reads
/// as one live record. Query logic is the provider's own.
/// </summary>
public sealed class WeaviateQuerySource : WeaviateOASIS, ICosmicReadOnlyQueryActivation
{
    private readonly string origin, readerApiKey;
    private readonly Guid probeId;
    private readonly SemaphoreSlim activation = new(1, 1);

    public WeaviateQuerySource(HttpClient client, string origin, string readerApiKey, Guid probeId)
        : base(client, origin, readerApiKey)
    {
        if (string.IsNullOrWhiteSpace(readerApiKey)) throw new ArgumentException("A reader API key is required.", nameof(readerApiKey));
        if (probeId == Guid.Empty) throw new ArgumentException("An existing public teaching probe is required.", nameof(probeId));
        this.origin = origin.TrimEnd('/'); this.readerApiKey = readerApiKey; this.probeId = probeId;
    }

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        await activation.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            IsProviderActivated = false;
            if (!queryClientBound)
            {
                var built = await WeaviateClientBuilder.Cloud(origin, readerApiKey).WithoutRetries()
                    .WithQueryTimeout(TimeSpan.FromSeconds(15)).BuildAsync().ConfigureAwait(false);
                BindQueryClient(built); queryClientBound = true;
            }
            var found = 0;
            await foreach (var row in ReadNativeCqlAsync(new List<Guid> { probeId }, cancellationToken).WithCancellation(cancellationToken))
            {
                if (row.IsDeleted || !Guid.TryParseExact(row.Key, "D", out var id) || id != probeId)
                    return Failure("CQL_QUERY_SOURCE_PROBE_INVALID", "The Weaviate probe is not one live record.");
                found++;
            }
            if (found != 1) return Failure("CQL_QUERY_SOURCE_PROBE_MISSING", "The Weaviate reader cannot read exactly one probe record.");
            IsProviderActivated = true;
            return new OASISResult<bool>(true) { Message = "Weaviate read-only query source verified." };
        }
        catch (Exception cause)
        {
            return Failure(cause is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                "The Weaviate query source could not be verified.", cause);
        }
        finally { activation.Release(); }
    }

    private bool queryClientBound;

    private static OASISResult<bool> Failure(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}

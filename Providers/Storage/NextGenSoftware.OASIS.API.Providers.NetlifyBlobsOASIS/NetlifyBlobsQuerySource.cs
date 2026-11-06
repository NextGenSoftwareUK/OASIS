using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NetlifyBlobsOASIS;

/// <summary>Bounded public reads from one reviewed Netlify site store.</summary>
public sealed class NetlifyBlobsQuerySource : NetlifyBlobsOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly Guid probeId;

    public NetlifyBlobsQuerySource(Guid siteId, string readerToken, string store, Uri objectOrigin, Guid probeId)
        : this(siteId, readerToken, store, objectOrigin, probeId, null) { }

    internal NetlifyBlobsQuerySource(Guid siteId, string readerToken, string store, Uri objectOrigin, Guid probeId, HttpMessageHandler handler)
        : base(CreateBackend(siteId, readerToken, store, objectOrigin, probeId, handler))
    {
        this.probeId = probeId;
    }

    private static NetlifyBlobsBackend CreateBackend(Guid siteId, string token, string store, Uri origin, Guid probe, HttpMessageHandler handler)
    {
        if (siteId == Guid.Empty || probe == Guid.Empty || token is not { Length: > 0 and <= 1024 } ||
            token.Any(character => character <= ' ' || character > '~') ||
            store is not { Length: > 0 and <= 64 } || store.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_')) ||
            origin is not { IsAbsoluteUri: true, Scheme: "https", HostNameType: UriHostNameType.Dns, IsLoopback: false,
                IsDefaultPort: true, AbsolutePath: "/", UserInfo.Length: 0, Query.Length: 0, Fragment.Length: 0 })
            throw new ArgumentException("A reviewed Netlify site, teaching store, object origin, reader and probe are required.");
        return new NetlifyBlobsBackend(siteId.ToString("D"), token, store, "https://api.netlify.com", handler, origin);
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.NetlifyBlobsOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var row = await ReadRecordAsync(probeId, deadline.Token);
            if (row is not { IsPublic: true, IsDeleted: false })
                throw new InvalidDataException("The Netlify teaching probe must be an existing public holon.");
            deadline.Token.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception error)
        {
            return Failure<bool>(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE",
                "The Netlify teaching probe was not verified.", error);
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Failure<IReadOnlyList<IHolon>>("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Netlify target is required.");
            if (maximumResults is < 1 or > 10000)
                return Failure<IReadOnlyList<IHolon>>("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Failure<IReadOnlyList<IHolon>>(admission.ErrorCode, admission.Message);
            var selection = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
            if (selection.IsError) return Failure<IReadOnlyList<IHolon>>(selection.ErrorCode, selection.Message);
            if (IsVersionControlEnabled)
                return Failure<IReadOnlyList<IHolon>>("CQL_CAPABILITY_UNSUPPORTED", "Netlify historical CQL reads are unavailable.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Failure<IReadOnlyList<IHolon>>("CQL_PROVIDER_INACTIVE", "Activate the configured Netlify source first.");

            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            var rows = new List<IHolon>();
            var bytes = 0;
            foreach (var id in selection.Result)
            {
                deadline.Token.ThrowIfCancellationRequested();
                var row = await ReadRecordAsync(id, deadline.Token, count => bytes += count);
                if (bytes > 4 * 1024 * 1024)
                    return Failure<IReadOnlyList<IHolon>>("CQL_QUERY_LIMIT", "The complete native response exceeds 4 MiB.");
                if (row == null || row.IsDeleted) continue;
                if (!row.IsPublic || !plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("A Netlify teaching holon differs from the admitted read authority.");
                rows.Add(row);
            }

            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Failure<IReadOnlyList<IHolon>>("CQL_QUERY_LIMIT", "The complete page exceeds the result budget.");
            deadline.Token.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (Exception error)
        {
            return Failure<IReadOnlyList<IHolon>>(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_FAILED",
                "The native Netlify read failed without publishing partial results.", error);
        }
    }

    private async Task<Holon> ReadRecordAsync(Guid id, CancellationToken token, Action<int> accountBytes = null)
    {
        var json = await Backend.GetAsync(RecordKey("holon", id), token);
        if (json == null) return null;
        accountBytes?.Invoke(Encoding.UTF8.GetByteCount(json));
        using var document = JsonDocument.Parse(json);
        using var projected = new MemoryStream();
        using (var writer = new Utf8JsonWriter(projected))
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            writer.WriteStartObject();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate Netlify holon property.");
                if (!property.Name.Equals("Children", StringComparison.OrdinalIgnoreCase)) property.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        var row = OasisJson.DeserializeStored<Holon>(Encoding.UTF8.GetString(projected.ToArray()));
        if (row == null || row.Id != id) throw new InvalidDataException("Netlify holon identity differs from the admitted object key.");
        row.Children = Array.Empty<IHolon>();
        return row;
    }

    private static OASISResult<T> Failure<T>(string code, string message, Exception error = null) =>
        new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };

    public void Dispose()
    {
        IsProviderActivated = false;
        ((NetlifyBlobsBackend)Backend).Dispose();
    }
}

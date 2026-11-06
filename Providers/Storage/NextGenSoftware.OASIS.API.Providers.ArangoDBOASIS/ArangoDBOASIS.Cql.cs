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

namespace NextGenSoftware.OASIS.API.Providers.ArangoDBOASIS;

public partial class ArangoDBOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    public const string NativeIdentityQuery = "FOR doc IN DOCUMENT(holons, @keys) RETURN UNSET(doc, 'children')";
    private readonly Guid? _queryProbe;

    public ProviderType QueryProviderType => Core.Enums.ProviderType.ArangoDBOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    /// <summary>Read-only public profile with a caller-owned HTTP transport.</summary>
    public ArangoDBOASIS(string origin, string username, string password, HttpClient transport, Guid queryProbe)
        : this(origin, username, password, transport)
    {
        if (queryProbe == Guid.Empty) throw new ArgumentException("An existing public teaching probe is required.", nameof(queryProbe));
        _queryProbe = queryProbe;
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_queryProbe is not Guid probe)
                throw new InvalidOperationException("An explicit teaching probe is required.");
            var rows = await ReadNativeRecordsAsync(new[] { probe }, cancellationToken).ConfigureAwait(false);
            if (!rows.TryGetValue(probe, out var row) || row.IsDeleted || !row.IsPublic)
                throw new InvalidDataException("The teaching probe must be an existing public record.");
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception cause)
        {
            return new()
            {
                IsError = true, ErrorCount = 1,
                ErrorCode = cause is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "The native ArangoDB query source did not verify its teaching probe.",
                Exception = cause
            };
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string? code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit ArangoDB target is required.");
            if (maximumResults is < 1 or > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selection = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
            if (selection.IsError) return Error(selection.ErrorCode, selection.Message);
            if (IsVersionControlEnabled)
                return Error("CQL_CAPABILITY_UNSUPPORTED", "ArangoDB historical record selection is not available.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured ArangoDB source before querying.");
            if (selection.Result.Count == 0 || plan.Query.Limit == 0)
                return new(Array.AsReadOnly(Array.Empty<IHolon>())) { IsLoaded = true, LoadedCount = 0 };

            var rows = await ReadNativeRecordsAsync(selection.Result, cancellationToken).ConfigureAwait(false);
            var visible = new List<IHolon>(rows.Count);
            foreach (var id in selection.Result)
            {
                if (!rows.TryGetValue(id, out var row)) continue;
                if (!plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("Native ArangoDB record differs from read authority.");
                if (row.IsDeleted) continue;
                if (_queryProbe.HasValue && !row.IsPublic)
                    throw new InvalidDataException("Native teaching record is no longer public.");
                visible.Add(row);
            }
            var page = visible.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? visible.Count).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "Complete native page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Native query cancelled; no partial result published.", cause); }
        catch (PayloadLimitException cause)
        { return Error("CQL_QUERY_LIMIT", "Complete decoded payload exceeds 4 MiB.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native ArangoDB query failed; no partial result published.", cause); }
    }

    private sealed class PayloadLimitException : IOException { }

    private async Task<Dictionary<Guid, Holon>> ReadNativeRecordsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var admitted = new HashSet<Guid>(ids);
        if (ids.Count is < 1 or > 100 || admitted.Count != ids.Count || admitted.Contains(Guid.Empty))
            throw new ArgumentException("One to one hundred distinct canonical document keys are required.", nameof(ids));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl("/_api/cursor"))
        {
            Content = Json(new
            {
                query = NativeIdentityQuery,
                bindVars = new { keys = ids.Select(id => id.ToString("D")).ToArray() },
                batchSize = 101,
                options = new { maxRuntime = 15, failOnWarning = true }
            })
        };
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await response.Content.LoadIntoBufferAsync(8 * 1024 * 1024, deadline.Token).ConfigureAwait(false);
        await using var body = await response.Content.ReadAsStreamAsync(deadline.Token).ConfigureAwait(false);
        using var json = await JsonDocument.ParseAsync(body, cancellationToken: deadline.Token).ConfigureAwait(false);
        var root = json.RootElement;
        if (root.GetProperty("error").ValueKind != JsonValueKind.False ||
            root.GetProperty("hasMore").ValueKind != JsonValueKind.False)
            throw new InvalidDataException("The native cursor did not return a complete successful result.");
        var rows = new Dictionary<Guid, Holon>();
        var bytes = 0;
        foreach (var record in root.GetProperty("result").EnumerateArray())
        {
            deadline.Token.ThrowIfCancellationRequested();
            var key = record.GetProperty("_key").GetString();
            if (!Guid.TryParseExact(key, "D", out var id) || key != id.ToString("D") || !admitted.Contains(id) || rows.ContainsKey(id))
                throw new InvalidDataException("Native document key differs from the admitted catalogue.");
            bytes = checked(bytes + Encoding.UTF8.GetByteCount(record.GetRawText()));
            if (bytes > 4 * 1024 * 1024) throw new PayloadLimitException();
            var row = record.Deserialize<Holon>(_jsonOpts);
            if (row == null || row.Id != id)
                throw new InvalidDataException("Native document payload identity differs from its key.");
            row.Children = Array.Empty<IHolon>();
            rows.Add(id, row);
        }
        return rows;
    }
}

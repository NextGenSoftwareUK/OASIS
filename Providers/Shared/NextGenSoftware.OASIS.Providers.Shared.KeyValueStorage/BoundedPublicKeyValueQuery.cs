using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Bounded reads of public canonical holons through a provider's exact native key reader.</summary>
public sealed class BoundedPublicKeyValueQuery(ProviderType provider, Func<Guid, CancellationToken, Task<string>> read,
    int maximumConcurrentReads, IReadOnlySet<Guid> admittedRecordIds = null)
{
    private readonly int concurrentReads = maximumConcurrentReads is >= 1 and <= 4
        ? maximumConcurrentReads : throw new ArgumentOutOfRangeException(nameof(maximumConcurrentReads));
    private readonly HashSet<Guid> recordIds = admittedRecordIds == null ? null : new(admittedRecordIds);

    public BoundedPublicKeyValueQuery(ProviderType provider, Func<Guid, CancellationToken, Task<string>> read)
        : this(provider, read, 1) { }

    public async Task<OASISResult<bool>> VerifyProbeAsync(Guid probe, CancellationToken cancellationToken)
    {
        try
        {
            using var deadline = CreateDeadline(cancellationToken);
            deadline.Token.ThrowIfCancellationRequested();
            var row = await ReadRecordAsync(probe, deadline.Token);
            if (row is not { IsPublic: true, IsDeleted: false })
                throw new InvalidDataException("The teaching probe must be an existing public holon.");
            deadline.Token.ThrowIfCancellationRequested();
            return new(true);
        }
        catch (Exception error)
        {
            return Failure<bool>(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_SOURCE_UNAVAILABLE",
                $"The {provider} teaching probe was not verified.", error);
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults, bool active, bool versioned, CancellationToken cancellationToken) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception error = null) =>
            Failure<IReadOnlyList<IHolon>>(code, message, error);

        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != provider)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", $"One explicit {provider} target is required.");
            if (maximumResults is < 1 or > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admission = BoundedIdentityCqlCompiler.Capabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selection = BoundedIdentityCqlCompiler.Compile(plan.Query, provider, provider);
            if (selection.IsError) return Error(selection.ErrorCode, selection.Message);
            if (versioned) return Error("CQL_CAPABILITY_UNSUPPORTED", $"{provider} historical CQL reads are unavailable.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!active) return Error("CQL_PROVIDER_INACTIVE", $"Activate the configured {provider} source first.");
            if (recordIds != null && selection.Result.Any(id => !recordIds.Contains(id)))
                return Error("CQL_QUERY_FAILED", "A selected ID is outside the configured public catalogue.");

            using var deadline = CreateDeadline(cancellationToken);
            var rows = new List<IHolon>();
            var bytes = 0;
            foreach (var batch in selection.Result.Chunk(concurrentReads))
            {
                deadline.Token.ThrowIfCancellationRequested();
                var loaded = await Task.WhenAll(batch.Select(id => ReadRecordAsync(id, deadline.Token,
                    count => Interlocked.Add(ref bytes, count))));
                if (bytes > 4 * 1024 * 1024)
                    return Error("CQL_QUERY_LIMIT", "The complete native response exceeds 4 MiB.");
                foreach (var row in loaded)
                {
                    if (row == null || row.IsDeleted) continue;
                    if (!row.IsPublic || !plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("A teaching holon differs from the admitted read authority.");
                    rows.Add(row);
                }
            }

            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "The complete page exceeds the result budget.");
            deadline.Token.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (Exception error)
        {
            return Error(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_FAILED",
                $"The native {provider} read failed without publishing partial results.", error);
        }
    }

    private async Task<Holon> ReadRecordAsync(Guid id, CancellationToken token, Action<int> accountBytes = null)
    {
        var json = await read(id, token);
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
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate canonical holon property.");
                if (!property.Name.Equals("Children", StringComparison.OrdinalIgnoreCase)) property.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        var row = OasisJson.DeserializeStored<Holon>(Encoding.UTF8.GetString(projected.ToArray()));
        if (row == null || row.Id != id) throw new InvalidDataException("Canonical holon identity differs from the admitted key.");
        row.Children = Array.Empty<IHolon>();
        return row;
    }

    private static CancellationTokenSource CreateDeadline(CancellationToken token)
    {
        var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        return deadline;
    }

    private static OASISResult<T> Failure<T>(string code, string message, Exception error) =>
        new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = error };
}

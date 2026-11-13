using System;
using System.Collections.Generic;
using System.Data;
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

namespace NextGenSoftware.OASIS.API.Providers.QuestDBOASIS;

public partial class QuestDBOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.QuestDBOASIS;
    public CosmicQueryCapabilities QueryCapabilities => QuestDBCqlCompiler.Capabilities;
    private sealed class NativePayloadLimitException : IOException { }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit QuestDB target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between1and10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = QuestDBCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "QuestDB history is not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Error("CQL_PROVIDER_INACTIVE", "Activate explicit QuestDB tables before querying.");
            var rows = new Dictionary<Guid, IHolon>();
            if (selected.Result.Count != 0)
            {
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                deadline.CancelAfter(TimeSpan.FromSeconds(15));
                using var connection = await OpenConnectionAsync(deadline.Token);
                var parameters = selected.Result.Select((_, index) => "@id" + index).ToArray();
                using var command = Command(connection,
                    "SELECT id, cast(ts AS LONG) AS cosmic_ts, data FROM oasis_holons WHERE id IN (" +
                    string.Join(",", parameters) + ") LIMIT " + (selected.Result.Count + 1) + ";");
                for (var index = 0; index < parameters.Length; index++)
                    TextParameter(command, parameters[index][1..], selected.Result[index].ToString("D"));
                using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, deadline.Token);
                if (reader.FieldCount != 3 || reader.GetName(0) != "id" || reader.GetName(1) != "cosmic_ts" || reader.GetName(2) != "data")
                    throw new InvalidDataException("QuestDB query projection differs from the storage contract.");
                var seen = new HashSet<Guid>();
                var physicalRows = 0;
                var bytesRemaining = 4 * 1024 * 1024;
                while (await reader.ReadAsync(deadline.Token))
                {
                    if (++physicalRows > selected.Result.Count) throw new NativePayloadLimitException();
                    var key = reader.GetString(0);
                    if (!Guid.TryParseExact(key, "D", out var id) || key != id.ToString("D") ||
                        !selected.Result.Contains(id) || !seen.Add(id) || reader.GetInt64(1) != 0)
                        throw new InvalidDataException("QuestDB native identity/timestamp differs from the admitted current-row contract.");
                    if (await reader.IsDBNullAsync(2, deadline.Token)) throw new InvalidDataException("QuestDB document is missing.");
                    using var text = reader.GetTextReader(2);
                    var (json, bytes) = await ReadBoundedJsonAsync(text, Math.Min(1024 * 1024, bytesRemaining), deadline.Token);
                    bytesRemaining -= bytes;
                    var row = JsonSerializer.Deserialize<Holon>(json, _jsonOpts);
                    if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("QuestDB payload differs from native key/read authority.");
                    if (row.IsDeleted) continue;
                    row.Children = Array.Empty<IHolon>();
                    rows.Add(id, row);
                }
            }
            var page = selected.Result.Where(rows.ContainsKey).Select(id => rows[id])
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "Complete page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "QuestDB query cancelled; no partial rows published.", cause); }
        catch (NativePayloadLimitException cause)
        { return Error("CQL_QUERY_LIMIT", "QuestDB result exceeds the admitted row/document budget.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native QuestDB query failed; no partial rows published.", cause); }
    }

    private static async Task<(string Json, int Bytes)> ReadBoundedJsonAsync(TextReader input, int byteBudget, CancellationToken token)
    {
        var encoder = new UTF8Encoding(false, true).GetEncoder();
        var json = new StringBuilder();
        var buffer = new char[4096];
        var encoded = new byte[Encoding.UTF8.GetMaxByteCount(buffer.Length)];
        var bytes = 0;
        int count;
        while ((count = await input.ReadAsync(buffer.AsMemory(), token)) != 0)
        {
            encoder.Convert(buffer, 0, count, encoded, 0, encoded.Length, false, out var used, out var encodedCount, out _);
            if (used != count) throw new InvalidDataException("QuestDB UTF8 encoding did not consume the bounded character chunk.");
            bytes += encodedCount;
            if (bytes > byteBudget) throw new NativePayloadLimitException();
            json.Append(buffer, 0, count);
        }
        encoder.Convert(Array.Empty<char>(), 0, 0, encoded, 0, encoded.Length, true, out _, out var finalBytes, out _);
        bytes += finalBytes;
        if (bytes > byteBudget) throw new NativePayloadLimitException();
        return (json.ToString(), bytes);
    }
}

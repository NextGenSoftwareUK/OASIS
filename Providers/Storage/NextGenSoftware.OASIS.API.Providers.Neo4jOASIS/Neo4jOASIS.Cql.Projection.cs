using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicQueryProjectionStorageProvider
{
    public static CosmicProjectionCapabilities CqlProjectionCapabilities { get; } = new(
        CqlCapabilities.CanonicalFields.Select(field => new CosmicProjectionFieldCapability(field.Name, field.ValueType)).ToArray(),
        CqlCapabilities.MetadataTypes.Select(type => type.ValueType).ToArray());
    public CosmicProjectionCapabilities ProjectionCapabilities => CqlProjectionCapabilities;

    public async Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteCosmicProjectionAsync<T, TResult>(
        CosmicProjectionPlan<T, TResult> plan, int maximumResults = 100,
        int maximumPayloadBytes = CosmicProjectionResults.MaximumPayloadLimit,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            var admission = CosmicProjectionPlanner.Admit(plan, Core.Enums.ProviderType.Neo4jOASIS, CqlCapabilities, CqlProjectionCapabilities);
            if (admission.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, IReadOnlyList<CosmicProjectionRow>>(admission);
            if (maximumResults < 0 || maximumResults > CosmicProjectionResults.MaximumResultLimit
                || maximumPayloadBytes < 0 || maximumPayloadBytes > CosmicProjectionResults.MaximumPayloadLimit)
                return ProjectionCqlFailure("CQL_QUERY_LIMIT", "Projection output bounds are invalid.");
            var source = plan.Query.Source;
            var compiled = CompileCqlSource(source);
            var translator = compiled.Translator;
            var columns = translator.TranslateProjection(plan.Query.Columns);
            var keys = translator.TranslateOrdering(source.Orderings);
            var aliases = keys.Select((_, index) => "cqlSelectSort" + index).ToArray();
            var keyValues = string.Concat(keys.Select((key, index) => "," + key.Expression + " AS " + aliases[index]));
            var returnedKeys = string.Concat(aliases.Select(alias => "," + alias));
            var emptyKeys = string.Concat(aliases.Select(alias => ",null AS " + alias));
            var directions = keys.Select((key, index) => aliases[index] + (key.Descending ? " DESC" : " ASC")).ToArray();
            var pageOrder = string.Join(",", directions.Concat(new[] { "p.f_Id ASC" }));
            var finalOrder = string.Join(",", new[] { "valid ASC" }.Concat(directions).Concat(new[] { "id ASC" }));
            var nativeColumns = string.Concat(columns.Select((column, index) => "," + column.State + " AS cqlSelectState" + index
                + "," + column.Value + " AS cqlSelectValue" + index));
            var cells = "[" + string.Join(",", columns.Select((_, index) => "{s:cqlSelectState" + index + ",v:cqlSelectValue" + index + "}")) + "]";
            // Character count is a lower bound on UTF-8 bytes. Reject impossible
            // rows before transferring values; the budget owner checks exact bytes.
            var lowerBytes = (16 + columns.Count * 16).ToString(CultureInfo.InvariantCulture)
                + string.Concat(plan.Query.Columns.Select((column, index) => column.Path.ValueType == typeof(string)
                    ? "+CASE WHEN cqlSelectValue" + index + " IS NULL THEN 16 ELSE size(cqlSelectValue" + index + ") END" : "+16"));
            var fetch = source.Limit.HasValue ? Math.Min(source.Limit.Value, maximumResults + 1) : maximumResults + 1;
            translator.Parameters.Add("projectionLimit", fetch); translator.Parameters.Add("projectionOffset", source.Offset);
            translator.Parameters.Add("projectionBytes", maximumPayloadBytes);
            cancellationToken.ThrowIfCancellationRequested();
            if (fetch == 0) return new OASISResult<IReadOnlyList<CosmicProjectionRow>>(Array.Empty<CosmicProjectionRow>()) { IsLoaded = true };
            if (!IsProviderActivated || _driver == null) return ProjectionCqlFailure("CQL_PROVIDER_INACTIVE", "Neo4j provider is not activated.");

            // Invalid ownership/traversal branches remain outside the page and
            // selected columns. No entity snapshot is returned or hydrated.
            var traversalError = translator.TraversalClauses.Count == 0 ? "" : " UNION ALL " + compiled.EligibleSource
                + "WHERE " + compiled.Cycle + " OR " + compiled.TraversalLimit
                + " RETURN h.Id AS id,false AS valid," + compiled.Cycle + " AS traversalCycle,"
                + compiled.TraversalLimit + " AS traversalLimit,false AS payloadLimit,[] AS cells" + emptyKeys + " LIMIT 1";
            var query = "CALL { " + compiled.Source
                + "WHERE NOT valid RETURN h.Id AS id,false AS valid,false AS traversalCycle,false AS traversalLimit,false AS payloadLimit,[] AS cells"
                + emptyKeys + " LIMIT 1" + traversalError + " UNION ALL " + compiled.EligibleSource
                + "WHERE (" + compiled.DescendantPredicate + ") AND NOT(" + compiled.Cycle + " OR " + compiled.TraversalLimit + ") "
                + "WITH h,p" + keyValues + " ORDER BY " + pageOrder + " SKIP $projectionOffset LIMIT $projectionLimit "
                + "WITH h,p" + returnedKeys + nativeColumns + " WITH h,p" + returnedKeys + "," + cells + " AS cqlSelectCells,"
                + lowerBytes + " AS cqlSelectBytes "
                + "RETURN p.f_Id AS id,true AS valid,false AS traversalCycle,false AS traversalLimit,"
                + "cqlSelectBytes > $projectionBytes AS payloadLimit,"
                + "CASE WHEN cqlSelectBytes > $projectionBytes THEN [] ELSE cqlSelectCells END AS cells" + returnedKeys
                + " } RETURN id,valid,traversalCycle,traversalLimit,payloadLimit,cells" + returnedKeys + " ORDER BY " + finalOrder;
            var budget = new NeoCqlReadBudget(maximumPayloadBytes, cancellationToken);
            budget.Check();
            using var session = _driver.AsyncSession(config => config.WithDefaultAccessMode(AccessMode.Read).WithFetchSize(1));
            using var transaction = await session.BeginTransactionAsync(config => config.WithTimeout(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
            budget.Check();
            var cursor = await transaction.RunAsync(query, translator.Parameters).ConfigureAwait(false);
            var rows = new List<CosmicProjectionRow>();
            while (true)
            {
                budget.Check();
                if (!await cursor.FetchAsync().ConfigureAwait(false)) break;
                budget.Check();
                var record = cursor.Current;
                if (record["traversalCycle"].As<bool>()) throw new InvalidDataException("Projection traversal encountered a cycle.");
                if (record["traversalLimit"].As<bool>()) throw new NeoCqlLimitException("Projection traversal exceeds its visited-node budget.");
                if (!record["valid"].As<bool>()) throw new InvalidDataException("Projection ownership or membership is invalid.");
                if (record["payloadLimit"].As<bool>()) throw new NeoCqlLimitException("Projection row exceeds its native payload bound.");
                if (rows.Count >= maximumResults) throw new NeoCqlLimitException("Projection exceeds its result-count budget.");
                if (record["cells"] is not IReadOnlyList<object> native) throw new InvalidDataException("Projection cell buffer is invalid.");
                var row = ReadNativeProjectionRow(native, plan.Query.Columns);
                budget.ReserveProjection(row.Values);
                rows.Add(row);
            }
            budget.Check();
            await transaction.CommitAsync().ConfigureAwait(false);
            budget.Check();
            return new OASISResult<IReadOnlyList<CosmicProjectionRow>>(rows.AsReadOnly()) { IsLoaded = true };
        }
        catch (NeoCqlLimitException cause) { return ProjectionCqlFailure("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (NeoCqlTimeoutException cause) { return ProjectionCqlFailure("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (Neo4jException cause) when (cause.Code == "Neo.ClientError.Transaction.TransactionTimedOut"
            || cause.Code == "Neo.TransientError.Transaction.TransactionTimedOut" || cause.Code == "Neo.ClientError.Transaction.TransactionTimedOutClientConfiguration")
        { return ProjectionCqlFailure(cancellationToken.IsCancellationRequested ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (OperationCanceledException cause) { return ProjectionCqlFailure("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (NotSupportedException cause) { return ProjectionCqlFailure("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OverflowException cause) { return ProjectionCqlFailure("CQL_DATA_INVALID", "Projection scalar exceeds its declared CLR domain.", cause); }
        catch (InvalidDataException cause) { return ProjectionCqlFailure("CQL_DATA_INVALID", cause.Message, cause); }
        catch (Exception cause) { return ProjectionCqlFailure("CQL_PROVIDER_ERROR", cause.Message, cause); }
    }

    internal static CosmicProjectionRow ReadNativeProjectionRow(IReadOnlyList<object> native, IReadOnlyList<CosmicProjectionColumn> columns)
    {
        if (native.Count != columns.Count) throw new InvalidDataException("Native projection width differs from its admitted shape.");
        var values = new object[columns.Count];
        for (var index = 0; index < values.Length; index++)
        {
            if (native[index] is not IDictionary<string, object> cell || cell.Count != 2
                || !cell.TryGetValue("s", out var rawState) || rawState is not long state || state is < 0 or > 2
                || !cell.TryGetValue("v", out var value))
                throw new InvalidDataException("Selected projection cell has an invalid native domain.");
            var declared = columns[index].Path.ValueType;
            if (state < 2)
            {
                if (value != null || declared.IsValueType && Nullable.GetUnderlyingType(declared) == null)
                    throw new InvalidDataException("Selected required value is null or missing.");
                values[index] = null;
            }
            else values[index] = DecodeNativeProjectionValue(value, Nullable.GetUnderlyingType(declared) ?? declared);
        }
        return new CosmicProjectionRow(values);
    }

    private static object DecodeNativeProjectionValue(object value, Type type)
    {
        if (type == typeof(string) && value is string text) return text;
        if (type == typeof(bool) && value is bool boolean) return boolean;
        if (type == typeof(Guid) && value is string guid && Guid.TryParseExact(guid, "D", out var id)) return id;
        if (value is long integer)
        {
            if (type.IsEnum)
            {
                var member = Convert.ChangeType(integer, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture);
                if (Enum.IsDefined(type, member)) return Enum.ToObject(type, member);
            }
            else if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
                || type == typeof(uint) || type == typeof(int) || type == typeof(long))
                return Convert.ChangeType(integer, type, CultureInfo.InvariantCulture);
        }
        throw new InvalidDataException("Native selected value does not match its exact CLR domain.");
    }

    private static OASISResult<IReadOnlyList<CosmicProjectionRow>> ProjectionCqlFailure(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}

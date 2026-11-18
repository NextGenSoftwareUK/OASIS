using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Endpoint
{
    public static async Task<IActionResult> ExecuteProjectionAsync(HttpContext http, CqlProjectionV1Request request, ICqlV1Providers providers, bool mixed = false)
    {
        var rejected = CaptureAdmission(http, out var actor, out var admission, out var execution);
        if (rejected != null) return rejected;
        try
        {
            http.RequestAborted.ThrowIfCancellationRequested();
            var requested = CqlV1Compiler.RequestedProvider(request?.Provider, includeCloudReadProfile: true);
            var policy=CaptureReadPolicy(http,actor,requested,providers,admission,includeCloudReadProfile:true);
            if (request?.VirtualMappingId == Guid.Empty || mixed != (request?.VirtualMappingId.HasValue == true))
                return Failure(400, "CQL_REQUEST_INVALID",
                    "Use the mixed projection route with a nonempty mapping ID, or the ordinary route without a mapping.");
            if (mixed && execution.MaximumProviderAttempts != CosmicQueryHttpRoutes.MixedProjectionAttempts)
                return Failure(503, "CQL_ADMISSION_REQUIRED", "Mixed projection requires three admitted provider attempts.");
            var virtualProjection = request?.VirtualMappingId.HasValue == true
                ? CqlV1Compiler.CompileVirtualProjection(request)
                : null;
            var intent = CqlV1Compiler.CompileProjection(request, actor, policy?.Selected??requested, policy?.Access,
                virtualProjection == null ? null : CqlVirtualProjection.MaximumRows);
            if (!TryResolveEligible(providers, requested, admission, out var storage, out var rowAdapter, out var selected,policy,includeCloudReadProfile:true))
                return Failure(403, "CQL_PROVIDER_NOT_ELIGIBLE", "The provider is not eligible for this admitted projection.");
            if (rowAdapter is not ICosmicQueryProjectionStorageProvider adapter)
                return Failure(400, "CQL_CAPABILITY_UNSUPPORTED", "The provider does not implement native selected columns.");
            var planned = CosmicProjectionPlanner.Plan(intent, selected,
                new[] { new CosmicProjectionTarget(selected, adapter.QueryCapabilities, adapter.ProjectionCapabilities) });
            if (planned.IsError) return Failure(400, planned.ErrorCode, "The provider does not support the requested projection.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            var manager = providers.CreateManager(storage);
            http.Items[CqlV1ResourceFilter.DispatchedKey] = true;
            var result = await manager.ExecuteProjectionQueryAsync(planned.Result,
                new ICosmicQueryStorageProvider[] { adapter }, maximumResults: 100,
                maximumPayloadBytes: MaximumResponseBytes, cancellationToken: deadline.Token);
            if (result.IsError) return QueryFailure(http, result.ErrorCode);
            var output = result.Result.Providers.Single();
            if (output.Provider != selected) return Failure(503, "CQL_PROVIDER_RESULT_INVALID", "Projection provenance differs from its admitted provider.");
            if (virtualProjection != null)
                return await CompleteVirtualProjectionAsync(http, request, intent, output.Rows, virtualProjection,
                    providers, actor, selected, deadline.Token);
            var columns = SelectedWireColumns(intent.Columns);
            var rows = SelectedWireRows(output.Rows, deadline.Token);
            deadline.Token.ThrowIfCancellationRequested();
            var response = JsonResponse(new OASISResult<object>(new
            { version = "cql-projection-v1", provider = selected.ToString(), columns, rows }) { IsLoaded = true });
            deadline.Token.ThrowIfCancellationRequested();
            return response;
        }
        catch(ReadPolicyRejected cause) { return Failure(cause.Status,cause.Code,"The trusted read-access policy did not authorize this request."); }
        catch (ArgumentException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(400, "CQL_REQUEST_INVALID", "Invalid bounded CQL projection description."); }
        catch (InvalidOperationException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(503, "CQL_PROVIDER_UNAVAILABLE", "The requested provider is unavailable."); }
        catch (ProjectionWireException) { return Failure(503, "CQL_PROVIDER_RESULT_INVALID", "A selected scalar has an invalid public wire domain."); }
        catch (ResponseLimitException) { return Failure(413, "CQL_RESPONSE_LIMIT", "Projection response exceeds the public output limit."); }
        catch (OperationCanceledException) { return Failure(http.RequestAborted.IsCancellationRequested ? 499 : 504, "CQL_QUERY_CANCELLED", "The projection query was cancelled."); }
        catch (Exception) { return Failure(503, "CQL_EXECUTION_FAILED", "The projection did not complete. Retain the usage operation ID for reconciliation."); }
    }

    private static async Task<IActionResult> CompleteVirtualProjectionAsync(HttpContext http,
        CqlProjectionV1Request request, CosmicProjectionQuery<Holon, CosmicProjectionRecord> intent,
        IReadOnlyList<CosmicProjectionRecord> materialized, CqlVirtualProjection virtualProjection,
        ICqlV1Providers providers, Guid actor, ProviderType selected, CancellationToken cancellationToken)
    {
        if (materialized.Count >= CqlVirtualProjection.MaximumRows)
            return Failure(400, "CQL_VIRTUAL_SOURCE_LIMIT", "Narrow the filter so both branches fit the mixed projection budget.");

        if (providers is not ICqlVirtualMappingReader virtualReader)
            return Failure(503, "VIRTUAL_HOLON_SOURCE_UNAVAILABLE", "Virtual mapping queries are unavailable on this host.");
        if (http.Items["Avatar"] is not IAvatar avatar || avatar.Id != actor || actor == Guid.Empty)
            return Failure(401, "CQL_AUTHENTICATION_REQUIRED", "Sign in to use a private virtual mapping.");
        var native = await virtualReader.QueryVirtualMappingAsync(request.VirtualMappingId.Value, avatar,
            virtualProjection, cancellationToken);
        if (native.IsError)
            return Failure(native.ErrorCode switch
            {
                "VIRTUAL_MAPPING_NOT_FOUND" => 404,
                "NATIVE_QUERY_PAYLOAD_LIMIT" => 413,
                "VIRTUAL_HOLON_QUERY_INVALID" or "NATIVE_QUERY_INVALID" or "VIRTUAL_HOLON_CONNECTION_CHANGED" => 400,
                _ => 503
            }, native.ErrorCode, native.Message);
        if (native.Result.Count >= CqlVirtualProjection.MaximumRows)
            return Failure(400, "CQL_VIRTUAL_SOURCE_LIMIT", "Narrow the filter so both branches fit the mixed projection budget.");

        var imported = materialized.ToDictionary(row => (Guid)row.Values[0]);
        var live = native.Result.ToDictionary(row => row.Id);
        var rows = new List<object[]>();
        var provenance = new List<object>();
        var ids = imported.Keys.Union(live.Keys);
        var ordered = request.Descending ? ids.OrderByDescending(id => id) : ids.OrderBy(id => id);
        foreach (var id in ordered.Skip(request.Skip).Take(request.Take))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (live.TryGetValue(id, out var row))
            {
                object[] values;
                try { values = virtualProjection.Select(row); }
                catch (ArgumentException)
                { return Failure(503, "CQL_PROVIDER_RESULT_INVALID", "The mapped field value differs from its selected type."); }
                rows.Add(values.Select(ProjectionWireValue).ToArray());
                provenance.Add(new { representation = "Virtual", provider = row.Provider.ToString(), row.MappingId,
                    row.MappingVersion, row.FetchedAt, alsoMaterialized = imported.ContainsKey(id) });
            }
            else
            {
                rows.Add(imported[id].Values.Select(ProjectionWireValue).ToArray());
                provenance.Add(new { representation = "Materialized", provider = selected.ToString() });
            }
        }
        return JsonResponse(new OASISResult<object>(new { version = "cql-projection-v1", provider = selected.ToString(),
            columns = SelectedWireColumns(intent.Columns), rows, provenance, canPrepareSnapshot = true }) { IsLoaded = true });
    }

    private static object SelectedWireColumns(IReadOnlyList<CosmicProjectionColumn> columns)
        => columns.Select(column => new
        {
            alias = column.Alias,
            valueType = ProjectionWireType(Nullable.GetUnderlyingType(column.ValueType) ?? column.ValueType),
            nullable = !column.ValueType.IsValueType || Nullable.GetUnderlyingType(column.ValueType) != null
        }).ToArray();

    private static List<object[]> SelectedWireRows(IEnumerable<CosmicProjectionRecord> records, CancellationToken token)
    {
        var rows = new List<object[]>();
        foreach (var record in records)
        {
            token.ThrowIfCancellationRequested();
            var values = new object[record.Values.Count];
            for (var index = 0; index < values.Length; index++)
            {
                token.ThrowIfCancellationRequested();
                values[index] = ProjectionWireValue(record.Values[index]);
            }
            rows.Add(values);
        }
        return rows;
    }

    private static string ProjectionWireType(Type type)
    {
        if (type == typeof(string)) return "String";
        if (type == typeof(bool)) return "Boolean";
        if (type == typeof(Guid)) return "Guid";
        if (type == typeof(DateTime)) return "DateTime";
        if (type == typeof(long)) return "Int64";
        if (type == typeof(ulong)) return "UInt64";
        if (type == typeof(float)) return "Single";
        if (type == typeof(double)) return "Double";
        if (type == typeof(decimal)) return "Decimal";
        if (type == typeof(int)) return "Int32";
        if (type == typeof(byte)) return "Byte";
        if (type == typeof(sbyte)) return "SByte";
        if (type == typeof(short)) return "Int16";
        if (type == typeof(ushort)) return "UInt16";
        if (type == typeof(uint)) return "UInt32";
        if (type == typeof(HolonType)) return "HolonType";
        if (type == typeof(ProviderType)) return "ProviderType";
        throw new ProjectionWireException();
    }

    private static object ProjectionWireValue(object value)
    {
        if (value == null || value is string || value is bool || value is byte || value is sbyte
            || value is short || value is ushort || value is int || value is uint) return value;
        if (value is long integer) return integer.ToString(CultureInfo.InvariantCulture);
        if (value is ulong unsigned) return unsigned.ToString(CultureInfo.InvariantCulture);
        if (value is float single && !float.IsNaN(single) && !float.IsInfinity(single))
            return (single == 0f ? 0f : single).ToString("R", CultureInfo.InvariantCulture);
        if (value is double number && !double.IsNaN(number) && !double.IsInfinity(number))
            return (number == 0d ? 0d : number).ToString("R", CultureInfo.InvariantCulture);
        if (value is decimal exact) return exact.ToString(CultureInfo.InvariantCulture);
        if (value is Guid guid) return guid.ToString("D");
        if (value is DateTime date && date.Kind == DateTimeKind.Utc) return date.ToString("O", CultureInfo.InvariantCulture);
        if (value is HolonType holon && Enum.IsDefined(typeof(HolonType), holon)) return holon.ToString();
        if (value is ProviderType provider && Enum.IsDefined(typeof(ProviderType), provider)) return provider.ToString();
        throw new ProjectionWireException();
    }
    private sealed class ProjectionWireException : Exception { }
}

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
    public static async Task<IActionResult> ExecuteProjectionAsync(HttpContext http, CqlProjectionV1Request request, ICqlV1Providers providers)
    {
        var rejected = CaptureAdmission(http, out var actor, out var admission);
        if (rejected != null) return rejected;
        try
        {
            http.RequestAborted.ThrowIfCancellationRequested();
            var requested = CqlV1Compiler.RequestedProvider(request?.Provider);
            var intent = CqlV1Compiler.CompileProjection(request, actor, requested);
            if (!TryResolveEligible(providers, requested, admission, out var storage, out var rowAdapter, out var selected))
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
            var columns = intent.Columns.Select(column => new
            {
                alias = column.Alias,
                valueType = ProjectionWireType(Nullable.GetUnderlyingType(column.Path.ValueType) ?? column.Path.ValueType),
                nullable = !column.Path.ValueType.IsValueType || Nullable.GetUnderlyingType(column.Path.ValueType) != null
            }).ToArray();
            var rows = new List<object[]>();
            foreach (var record in output.Rows)
            {
                deadline.Token.ThrowIfCancellationRequested();
                var values = new object[record.Values.Count];
                for (var index = 0; index < values.Length; index++)
                {
                    deadline.Token.ThrowIfCancellationRequested();
                    values[index] = ProjectionWireValue(record.Values[index]);
                }
                rows.Add(values);
            }
            deadline.Token.ThrowIfCancellationRequested();
            var response = JsonResponse(new OASISResult<object>(new
            { version = "cql-projection-v1", provider = selected.ToString(), columns, rows }) { IsLoaded = true });
            deadline.Token.ThrowIfCancellationRequested();
            return response;
        }
        catch (ArgumentException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(400, "CQL_REQUEST_INVALID", "Invalid bounded CQL projection description."); }
        catch (InvalidOperationException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(503, "CQL_PROVIDER_UNAVAILABLE", "The requested provider is unavailable."); }
        catch (ProjectionWireException) { return Failure(503, "CQL_PROVIDER_RESULT_INVALID", "A selected scalar has an invalid public wire domain."); }
        catch (ResponseLimitException) { return Failure(413, "CQL_RESPONSE_LIMIT", "Projection response exceeds the public output limit."); }
        catch (OperationCanceledException) { return Failure(http.RequestAborted.IsCancellationRequested ? 499 : 504, "CQL_QUERY_CANCELLED", "The projection query was cancelled."); }
        catch (Exception) { return Failure(503, "CQL_EXECUTION_FAILED", "The projection did not complete. Retain the usage operation ID for reconciliation."); }
    }

    private static string ProjectionWireType(Type type)
    {
        if (type == typeof(string)) return "String";
        if (type == typeof(bool)) return "Boolean";
        if (type == typeof(Guid)) return "Guid";
        if (type == typeof(DateTime)) return "DateTime";
        if (type == typeof(long)) return "Int64";
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
        if (value is decimal exact) return exact.ToString(CultureInfo.InvariantCulture);
        if (value is Guid guid) return guid.ToString("D");
        if (value is DateTime date && date.Kind == DateTimeKind.Utc) return date.ToString("O", CultureInfo.InvariantCulture);
        if (value is HolonType holon && Enum.IsDefined(typeof(HolonType), holon)) return holon.ToString();
        if (value is ProviderType provider && Enum.IsDefined(typeof(ProviderType), provider)) return provider.ToString();
        throw new ProjectionWireException();
    }
    private sealed class ProjectionWireException : Exception { }
}

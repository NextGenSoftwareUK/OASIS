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
    public static async Task<IActionResult> ExecuteGroupedAsync(HttpContext http, CqlGroupedV1Request request, ICqlV1Providers providers)
    {
        var rejected = CaptureAdmission(http, out var actor, out var admission);
        if (rejected != null) return rejected;
        try
        {
            http.RequestAborted.ThrowIfCancellationRequested();
            var requested = CqlV1Compiler.RequestedProvider(request?.Provider);
            var intent = CqlV1Compiler.CompileGrouped(request, actor, requested);
            if (!TryResolveEligible(providers, requested, admission, out var storage, out var rowAdapter, out var selected))
                return Failure(403, "CQL_PROVIDER_NOT_ELIGIBLE", "The provider is not eligible for this admitted grouped query.");
            if (rowAdapter is not ICosmicQueryGroupedStorageProvider adapter)
                return Failure(400, "CQL_CAPABILITY_UNSUPPORTED", "The provider does not implement native selected columns.");
            var planned = CosmicGroupedPlanner.Plan(intent, selected,
                new[] { new CosmicGroupedTarget(selected, adapter.QueryCapabilities, adapter.GroupedCapabilities) });
            if (planned.IsError) return Failure(400, planned.ErrorCode, "The provider does not support the requested grouped query.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            var manager = providers.CreateManager(storage);
            http.Items[CqlV1ResourceFilter.DispatchedKey] = true;
            var result = await manager.ExecuteGroupedQueryAsync(planned.Result,
                new ICosmicQueryStorageProvider[] { adapter }, cancellationToken: deadline.Token);
            if (result.IsError) return QueryFailure(http, result.ErrorCode);
            var output = result.Result.Providers.Single();
            if (output.Provider != selected) return Failure(503, "CQL_PROVIDER_RESULT_INVALID", "Grouped provenance differs from its admitted provider.");
            var columns = intent.Columns.Select(column => new
            {
                alias = column.Alias,
                valueType = ProjectionWireType(Nullable.GetUnderlyingType(column.ValueType) ?? column.ValueType),
                nullable = !column.ValueType.IsValueType || Nullable.GetUnderlyingType(column.ValueType) != null
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
            { version = "cql-grouped-v1", provider = selected.ToString(), columns, rows }) { IsLoaded = true });
            deadline.Token.ThrowIfCancellationRequested();
            return response;
        }
        catch (ArgumentException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(400, "CQL_REQUEST_INVALID", "Invalid bounded CQL grouped query description."); }
        catch (InvalidOperationException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(503, "CQL_PROVIDER_UNAVAILABLE", "The requested provider is unavailable."); }
        catch (ProjectionWireException) { return Failure(503, "CQL_PROVIDER_RESULT_INVALID", "A selected scalar has an invalid public wire domain."); }
        catch (ResponseLimitException) { return Failure(413, "CQL_RESPONSE_LIMIT", "Grouped response exceeds the public output limit."); }
        catch (OperationCanceledException) { return Failure(http.RequestAborted.IsCancellationRequested ? 499 : 504, "CQL_QUERY_CANCELLED", "The grouped query query was cancelled."); }
        catch (Exception) { return Failure(503, "CQL_EXECUTION_FAILED", "The grouped query did not complete. Retain the usage operation ID for reconciliation."); }
    }

}

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
    public static async Task<IActionResult> ExecuteCompositionAsync(HttpContext http, CqlCompositionV1Request request, ICqlV1Providers providers)
    {
        var rejected = CaptureAdmission(http, out var actor, out var admission);
        if (rejected != null) return rejected;
        try
        {
            http.RequestAborted.ThrowIfCancellationRequested();
            var requested = CqlV1Compiler.RequestedProvider(request?.Provider);
            var policy=CaptureReadPolicy(http,actor,requested,providers,admission);
            var intent = CqlV1Compiler.CompileComposition(request, actor, policy?.Selected??requested,policy?.Access);
            if (!TryResolveEligible(providers, requested, admission, out var storage, out var rowAdapter, out var selected,policy))
                return Failure(403, "CQL_PROVIDER_NOT_ELIGIBLE", "The provider is not eligible for this admitted composition.");
            if (rowAdapter is not ICosmicQueryProjectionCompositionStorageProvider adapter)
                return Failure(400, "CQL_CAPABILITY_UNSUPPORTED", "The provider does not implement native selected-result composition.");
            var planned = CosmicProjectionCompositionPlanner.Plan(intent, selected,
                new[] { new CosmicProjectionCompositionTarget(selected, adapter.QueryCapabilities, adapter.ProjectionCapabilities, adapter.CompositionCapabilities) },
                new CosmicProjectionCompositionOptions(100, MaximumResponseBytes, 1600, 25600));
            if (planned.IsError) return Failure(400, planned.ErrorCode, "The provider does not support the requested composition.");
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            var manager = providers.CreateManager(storage);
            http.Items[CqlV1ResourceFilter.DispatchedKey] = true;
            var result = await manager.ExecuteProjectionCompositionQueryAsync(planned.Result,
                new ICosmicQueryStorageProvider[] { adapter }, maximumResults: 100,
                maximumPayloadBytes: MaximumResponseBytes, cancellationToken: deadline.Token);
            if (result.IsError) return QueryFailure(http, result.ErrorCode);
            var output = result.Result.Providers.Single();
            if (output.Provider != selected) return Failure(503, "CQL_PROVIDER_RESULT_INVALID", "Projection provenance differs from its admitted provider.");
            var columns = SelectedWireColumns(intent.Columns);
            var rows = SelectedWireRows(output.Rows, deadline.Token);
            deadline.Token.ThrowIfCancellationRequested();
            var response = JsonResponse(new OASISResult<object>(new
            { version = "cql-composition-v1", provider = selected.ToString(), columns, rows }) { IsLoaded = true });
            deadline.Token.ThrowIfCancellationRequested();
            return response;
        }
        catch(ReadPolicyRejected cause) { return Failure(cause.Status,cause.Code,"The trusted read-access policy did not authorize this request."); }
        catch (ArgumentException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(400, "CQL_REQUEST_INVALID", "Invalid bounded CQL composition description."); }
        catch (InvalidOperationException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(503, "CQL_PROVIDER_UNAVAILABLE", "The requested provider is unavailable."); }
        catch (ProjectionWireException) { return Failure(503, "CQL_PROVIDER_RESULT_INVALID", "A selected scalar has an invalid public wire domain."); }
        catch (ResponseLimitException) { return Failure(413, "CQL_RESPONSE_LIMIT", "Composition response exceeds the public output limit."); }
        catch (OperationCanceledException) { return Failure(http.RequestAborted.IsCancellationRequested ? 499 : 504, "CQL_QUERY_CANCELLED", "The composition query was cancelled."); }
        catch (Exception) { return Failure(503, "CQL_EXECUTION_FAILED", "The composition did not complete. Retain the usage operation ID for reconciliation."); }
    }

}

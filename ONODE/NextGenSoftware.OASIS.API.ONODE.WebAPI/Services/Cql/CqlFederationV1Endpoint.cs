using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Endpoint
{
    public static async Task<IActionResult> ExecuteFederationAsync(HttpContext http, CqlFederationV1Request request,
        ICqlV1Providers providers)
    {
        var rejected = CaptureAdmission(http, out var actor, out var admission);
        if (rejected != null) return rejected;
        try
        {
            http.RequestAborted.ThrowIfCancellationRequested();
            if (request == null || request.Providers == null || request.ProviderPriority == null
                || request.Providers.Length > 3 || request.ProviderPriority.Length > 3
                || request.Take is < 1 or > 100 || request.Skip is < 0 or > 1000
                || request.MaximumOutputBytes is < 1 or > MaximumResponseBytes
                || request.MaximumDurationMilliseconds is < 1 or > 5000
                || request.MaximumRowsPerProvider is < 1 or > 100
                || request.MaximumInputRows is < 1 or > 300 || request.MaximumProviderAttempts is < 1 or > 3)
                throw new ArgumentException("Invalid bounded federation request.");

            ProviderType Concrete(string value) => CqlV1Compiler.RequestedProvider(value)
                ?? throw new ArgumentException("A concrete provider is required.");
            ProviderType?[] requested;
            switch (request.Scope)
            {
                case "CurrentDefault" when request.Providers.Length == 0: requested = new ProviderType?[] { null }; break;
                case "Named" when request.Providers.Length > 0:
                    requested = request.Providers.Select(value => (ProviderType?)Concrete(value)).ToArray(); break;
                case "AllEligible" when request.Providers.Length == 0:
                    requested = new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS, ProviderType.Neo4jOASIS }
                        .Where(admission.HasQueryTariff).Select(value => (ProviderType?)value).ToArray(); break;
                default: throw new ArgumentException("Scope must be CurrentDefault, Named or AllEligible with coherent selectors.");
            }
            if (requested.Length == 0 || requested.Distinct().Count() != requested.Length)
                throw new ArgumentException("Scope requires unique admitted concrete targets.");
            var identity = request.IdentityMode switch
            {
                "PreserveCopies" => CosmicFederationIdentityMode.PreserveCopies,
                "ProviderPriority" => CosmicFederationIdentityMode.ProviderPriority,
                _ => throw new ArgumentException("Use PreserveCopies or explicit ProviderPriority.")
            };
            var options = CosmicFederationOptions.Create(identity, providerPriority: request.ProviderPriority.Select(Concrete).ToArray(),
                skip: request.Skip, take: request.Take, maximumInputRows: request.MaximumInputRows,
                maximumRowsPerProvider: request.MaximumRowsPerProvider, maximumInputBytes: request.MaximumInputBytes,
                maximumOutputBytes: request.MaximumOutputBytes, maximumWork: request.MaximumWork,
                maximumProviderAttempts: request.MaximumProviderAttempts, maximumDurationMilliseconds: request.MaximumDurationMilliseconds);
            if (options.IsError) throw new ArgumentException(options.Message);
            if (request.MaximumProviderAttempts < requested.Length)
                throw new ArgumentException("Every resolved target requires a provider-attempt reservation.");
            var source = CqlV1Compiler.CompileFederationSource(request.Source, actor);
            var observations = new List<ReadPolicyObservation>();
            foreach (var provider in requested)
            {
                http.RequestAborted.ThrowIfCancellationRequested();
                var policy = CaptureReadPolicy(http, actor, provider, providers, admission);
                if (!TryResolveEligible(providers, provider, admission, out var storage, out var adapter, out var selected, policy))
                    return Failure(403, "CQL_PROVIDER_NOT_ELIGIBLE", "Every federation target must be eligible for this admitted request.");
                observations.Add(policy ?? new ReadPolicyObservation(storage, adapter, selected,
                    CosmicQueryReadAccess.OwnerOrPublic(actor).Result));
            }
            if (observations.Select(value => value.Selected).Distinct().Count() != observations.Count)
                throw new ArgumentException("Resolved targets must be unique.");
            var selectedProviders = observations.Select(value => value.Selected).ToArray();
            source = source.WithScope(CosmicQueryScope.Named(selectedProviders).Result);
            var plan = CosmicFederationPlanner.Plan(source, options.Result, selectedProviders[0],
                observations.Select(value => new CosmicQueryTarget(value.Selected, value.Adapter.QueryCapabilities)).ToArray(),
                observations.Select(value => new CosmicFederationReadPolicy(value.Selected, value.Access)).ToArray());
            if (plan.IsError) return Failure(400, plan.ErrorCode, "The complete federation plan was not admitted.");
            var manager = providers.CreateManager(observations[0].Storage);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
            deadline.CancelAfter(request.MaximumDurationMilliseconds);
            http.Items[CqlV1ResourceFilter.DispatchedKey] = true;
            var result = await manager.ExecuteFederationQueryAsync(plan.Result,
                observations.Select(value => value.Adapter).ToArray(), deadline.Token);
            if (result.IsError)
            {
                if (result.ErrorCode == "CQL_FEDERATION_DEADLINE")
                    return Failure(504, result.ErrorCode, "Federation exceeded its cumulative deadline.");
                if (result.ErrorCode == "CQL_FEDERATION_OUTPUT_LIMIT")
                    return Failure(413, result.ErrorCode, "Federation exceeded its admitted output budget.");
                return QueryFailure(http, result.ErrorCode);
            }

            var policies = observations.ToDictionary(value => value.Selected, value => value.Access);
            object Copy(CosmicFederationCopy copy)
            {
                deadline.Token.ThrowIfCancellationRequested();
                if (!policies.TryGetValue(copy.Provider, out var access) || !access.CanRead(copy.Row))
                    throw new CqlV1Projection.Rejected("CQL_VISIBILITY_VIOLATION");
                return new { provider = copy.Provider.ToString(),
                    row = CqlV1Projection.Create(new[] { copy.Row }, access, request.Source.IncludeMetadata, null, deadline.Token).Single() };
            }
            var rows = result.Result.Rows.Select(row => new
            {
                selected = Copy(row.Selected), copies = row.Copies.Select(Copy).ToArray(),
                selectionReason = row.SelectionReason.ToString(), causalAgreementEstablished = row.CausalAgreementEstablished
            }).ToArray();
            deadline.Token.ThrowIfCancellationRequested();
            var response = (FileContentResult)JsonResponse(new OASISResult<object>(new
            {
                version = "cql-federation-v1", providers = selectedProviders.Select(value => value.ToString()).ToArray(),
                consistency = result.Result.Consistency.ToString(), result.Result.ObservedInputRows, result.Result.ResolvedRows, rows
            }) { IsLoaded = true });
            if (response.FileContents.Length > request.MaximumOutputBytes)
                return Failure(413, "CQL_FEDERATION_OUTPUT_LIMIT", "Federation exceeded its admitted public wire budget.");
            deadline.Token.ThrowIfCancellationRequested();
            return response;
        }
        catch (ReadPolicyRejected cause) { return Failure(cause.Status, cause.Code, "The trusted policy did not authorize the complete federation request."); }
        catch (ArgumentException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(400, "CQL_REQUEST_INVALID", "Invalid bounded federation description."); }
        catch (InvalidOperationException) when (!http.Items.ContainsKey(CqlV1ResourceFilter.DispatchedKey))
        { return Failure(503, "CQL_PROVIDER_UNAVAILABLE", "A required provider is unavailable; no substitute was selected."); }
        catch (CqlV1Projection.Rejected cause) { return Failure(cause.Code == "CQL_RESPONSE_LIMIT" ? 413 : 503, cause.Code, "Federation output violated the public contract."); }
        catch (ResponseLimitException) { return Failure(413, "CQL_RESPONSE_LIMIT", "Federation exceeded the public response limit."); }
        catch (OperationCanceledException) { return Failure(http.RequestAborted.IsCancellationRequested ? 499 : 504, "CQL_QUERY_CANCELLED", "Federation was cancelled."); }
        catch (Exception) { return Failure(503, "CQL_EXECUTION_FAILED", "Federation did not complete. Retain the operation ID for reconciliation."); }
    }
}

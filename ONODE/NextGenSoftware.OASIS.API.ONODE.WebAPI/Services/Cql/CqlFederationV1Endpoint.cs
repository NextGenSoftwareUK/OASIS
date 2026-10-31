using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static partial class CqlV1Endpoint
{
    public static async Task<IActionResult> ExecuteFederationAsync(HttpContext http, CqlFederationV1Request request,
        ICqlV1Providers providers)
    {
        var rejected = CaptureAdmission(http, out var actor, out var admission, out var admittedExecution);
        if (rejected != null) return rejected;
        try
        {
            var active = Stopwatch.StartNew();
            http.RequestAborted.ThrowIfCancellationRequested();
            request = CqlRequestCapture.CaptureAndValidateFederation(request);

            CqlFederationCursorHost cursorHost = null;
            CqlFederationCursorState cursor = null;
            DateTimeOffset issued = default, expires = default;
            var lineage = Guid.Empty;
            if (request.EnableContinuation)
            {
                cursorHost = http.RequestServices?.GetService<CqlFederationCursorHost>();
                if (cursorHost == null)
                    return Failure(503, "CQL_CURSOR_UNAVAILABLE", "The host must enroll shared protected cursor keys and a deployment contract.");
                issued = cursorHost.UtcNow;
                expires = issued.AddMinutes(5);
                if (request.Cursor != null)
                {
                    var decoded = cursorHost.Read(request.Cursor, actor);
                    if (decoded.IsError) return Failure(400, "CQL_CURSOR_INVALID", "Protected continuation state was rejected.");
                    cursor = decoded.Result;
                    issued = cursor.Issued;
                    expires = cursor.Expires;
                }
                if (cursorHost.BindRequest(http, actor, cursor).IsError)
                    return Failure(400, "CQL_CURSOR_REQUEST_BINDING", "The exact continuation key must match this durable WEB4 admission.");
                // Preserve the validated root identity before application callbacks or provider awaits.
                lineage = cursor?.Lineage ?? Guid.Parse(admittedExecution.OperationId);
            }

            ProviderType Concrete(string value) => CqlV1Compiler.RequestedProvider(value, includeCloudReadProfile: true)
                ?? throw new ArgumentException("A concrete provider is required.");
            ProviderType?[] requested;
            switch (request.Scope)
            {
                case "CurrentDefault" when request.Providers.Length == 0: requested = new ProviderType?[] { null }; break;
                case "Named" when request.Providers.Length > 0:
                    requested = request.Providers.Select(value => (ProviderType?)Concrete(value)).ToArray(); break;
                case "AllEligible" when request.Providers.Length == 0:
                    requested = CqlV1Compiler.ReadProfiles
                        .Where(admission.HasQueryTariff).Select(value => (ProviderType?)value).ToArray(); break;
                default: throw new ArgumentException("Scope must be CurrentDefault, Named or AllEligible with coherent selectors.");
            }
            if (requested.Length == 0 || requested.Distinct().Count() != requested.Length)
                throw new ArgumentException("Scope requires unique admitted concrete targets.");
            // Resolve once on page one. A later default/eligibility change cannot substitute a target.
            if (cursor != null) requested = cursor.Providers.Select(value => (ProviderType?)value).ToArray();
            var identity = request.IdentityMode switch
            {
                "PreserveCopies" => CosmicFederationIdentityMode.PreserveCopies,
                "ProviderPriority" => CosmicFederationIdentityMode.ProviderPriority,
                _ => throw new ArgumentException("Use PreserveCopies or explicit ProviderPriority.")
            };
            OASISResult<CosmicFederationOptions> Options(int offset) => CosmicFederationOptions.Create(identity,
                providerPriority: request.ProviderPriority.Select(Concrete).ToArray(),
                skip: offset, take: request.Take, maximumInputRows: request.MaximumInputRows,
                maximumRowsPerProvider: request.MaximumRowsPerProvider, maximumInputBytes: request.MaximumInputBytes,
                maximumOutputBytes: request.MaximumOutputBytes, maximumWork: request.MaximumWork,
                maximumProviderAttempts: request.MaximumProviderAttempts, maximumDurationMilliseconds: request.MaximumDurationMilliseconds);
            var options = Options(request.Skip);
            if (options.IsError) throw new ArgumentException(options.Message);
            if (request.MaximumProviderAttempts < requested.Length)
                throw new ArgumentException("Every resolved target requires a provider-attempt reservation.");
            if (admittedExecution.MaximumProviderAttempts < requested.Length)
                return Failure(400, "CQL_FEDERATION_ADMISSION_BUDGET", "The server usage reservation cannot cover every requested provider.");
            var source = CqlV1Compiler.CompileFederationSource(request.Source, actor);
            var observations = new List<ReadPolicyObservation>();
            foreach (var provider in requested)
            {
                http.RequestAborted.ThrowIfCancellationRequested();
                var policy = CaptureReadPolicy(http, actor, provider, providers, admission, includeCloudReadProfile: true);
                if (!TryResolveEligible(providers, provider, admission, out var storage, out var adapter, out var selected, policy,
                    includeCloudReadProfile: true))
                    return Failure(403, "CQL_PROVIDER_NOT_ELIGIBLE", "Every federation target must be eligible for this admitted request.");
                observations.Add(policy ?? new ReadPolicyObservation(storage, adapter, selected,
                    CqlV1Compiler.DefaultReadAccess(actor, selected).Result));
            }
            if (observations.Select(value => value.Selected).Distinct().Count() != observations.Count)
                throw new ArgumentException("Resolved targets must be unique.");
            var selectedProviders = observations.Select(value => value.Selected).ToArray();
            // New read profiles have no protected continuation contract yet. Do not
            // silently apply the existing three-database cursor protocol to them.
            if (request.EnableContinuation && (selectedProviders.Any(type => !CqlV1Compiler.Supported(type))
                || source.Orderings.Count == 0))
                return Failure(400, "CQL_CURSOR_SOURCE_UNSUPPORTED", "This source requires a bounded federation without continuation.");
            source = source.WithScope(CosmicQueryScope.Named(selectedProviders).Result);
            var plan = CosmicFederationPlanner.Plan(source, options.Result, selectedProviders[0],
                observations.Select(value => new CosmicQueryTarget(value.Selected, value.Adapter.QueryCapabilities)).ToArray(),
                observations.Select(value => new CosmicFederationReadPolicy(value.Selected, value.Access)).ToArray());
            if (plan.IsError) return Failure(400, plan.ErrorCode, "The complete federation plan was not admitted.");
            var adapters = observations.Select(value => value.Adapter).ToArray();
            CosmicFederationRemainingBudget remaining = null;
            var originalActiveMilliseconds = 0;
            string intent = null;
            var offset = cursor?.Offset ?? request.Skip;
            if (cursorHost != null)
            {
                var binding = cursorHost.BindIntent(request, plan.Result, adapters);
                if (binding.IsError || cursor != null && binding.Result != cursor.Intent)
                    return Failure(400, "CQL_CURSOR_INTENT_INVALID", "The current intent, deployment or read policy differs from the protected page chain.");
                intent = binding.Result;
                var restored = cursor == null
                    ? CosmicFederationRemainingBudget.Restore(options.Result, options.Result.MaximumInputRows,
                        options.Result.MaximumInputBytes, options.Result.MaximumOutputBytes, options.Result.MaximumWork,
                        options.Result.MaximumProviderAttempts, options.Result.MaximumDurationMilliseconds,
                        selectedProviders.ToDictionary(value => value, _ => options.Result.MaximumRowsPerProvider))
                    : cursorHost.Restore(cursor, options.Result);
                if (restored.IsError) return Failure(400, "CQL_CURSOR_INVALID", "Remaining protected accounting is invalid.");
                remaining = restored.Result;
                originalActiveMilliseconds = remaining.DurationMilliseconds;
                var preMilliseconds = (int)Math.Ceiling(active.Elapsed.TotalMilliseconds);
                var preWork = selectedProviders.Length + 1;
                if (preMilliseconds >= remaining.DurationMilliseconds)
                    return Failure(504, "CQL_FEDERATION_DEADLINE", "Continuation preparation exceeded the remaining active deadline.");
                if (preWork > remaining.Work)
                    return Failure(413, "CQL_FEDERATION_WORK_LIMIT", "Continuation preparation exceeded the remaining work budget.");
                remaining = CosmicFederationRemainingBudget.Restore(options.Result, remaining.InputRows, remaining.InputBytes,
                    remaining.OutputBytes, remaining.Work - preWork, remaining.Attempts,
                    remaining.DurationMilliseconds - preMilliseconds, remaining.ProviderRows).Result;
                var preflight = remaining.ValidateReadSet(options.Result, selectedProviders, http.RequestAborted);
                if (preflight.IsError)
                    return preflight.ErrorCode == "CQL_QUERY_CANCELLED" ? QueryFailure(http, preflight.ErrorCode)
                        : Failure(400, preflight.ErrorCode, "Remaining accounting cannot execute the complete provider set.");
                if (cursor != null)
                {
                    var pageOptions = Options(offset);
                    if (pageOptions.IsError) return Failure(400, "CQL_CURSOR_INVALID", "Continuation offset is outside its bounded domain.");
                    plan = CosmicFederationPlanner.Plan(source, pageOptions.Result, selectedProviders[0],
                        observations.Select(value => new CosmicQueryTarget(value.Selected, value.Adapter.QueryCapabilities)).ToArray(),
                        observations.Select(value => new CosmicFederationReadPolicy(value.Selected, value.Access)).ToArray());
                    if (plan.IsError) return Failure(400, plan.ErrorCode, "The protected next page was not admitted.");
                }
            }
            var manager = providers.CreateManager(observations[0].Storage);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
            var executionMilliseconds = remaining?.DurationMilliseconds ?? request.MaximumDurationMilliseconds;
            if (cursorHost != null)
            {
                executionMilliseconds = Math.Min(executionMilliseconds,
                    (int)Math.Ceiling((expires - cursorHost.UtcNow).TotalMilliseconds));
                if (executionMilliseconds <= 0)
                    return Failure(400, "CQL_CURSOR_INVALID", "The fixed continuation lifetime expired before dispatch.");
            }
            deadline.CancelAfter(executionMilliseconds);
            http.Items[CqlV1ResourceFilter.DispatchedKey] = true;
            var result = remaining == null
                ? await manager.ExecuteFederationQueryAsync(plan.Result, adapters, deadline.Token)
                : await manager.ExecuteFederationQueryWithBudgetAsync(plan.Result, adapters, remaining, deadline.Token);
            if (result.IsError)
            {
                if (result.ErrorCode == "CQL_FEDERATION_DEADLINE")
                    return Failure(504, result.ErrorCode, "Federation exceeded its cumulative deadline.");
                if (result.ErrorCode == "CQL_FEDERATION_OUTPUT_LIMIT")
                    return Failure(413, result.ErrorCode, "Federation exceeded its admitted output budget.");
                return QueryFailure(http, result.ErrorCode);
            }

            var policies = observations.ToDictionary(value => value.Selected, value => value.Access);
            var publicCopies = 0;
            object Copy(CosmicFederationCopy copy)
            {
                publicCopies++;
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
            if (cursorHost != null)
            {
                // Reserve a complete maximum-size public envelope before protecting state. The fixed
                // token bound avoids circular accounting or retrying protection after key rotation.
                var nextOffset = checked(offset + rows.Length);
                var hasMore = nextOffset < result.Result.ResolvedRows;
                var nextKey = Guid.NewGuid();
                object Envelope(string token, string unavailable) => new OASISResult<object>(new
                {
                    version = "cql-federation-v1", providers = selectedProviders.Select(value => value.ToString()).ToArray(),
                    consistency = result.Result.Consistency.ToString(), result.Result.ObservedInputRows,
                    result.Result.ResolvedRows, rows, hasMore,
                    continuation = token == null ? null : new { cursor = token, nextRequestKey = nextKey.ToString("D") },
                    continuationUnavailable = unavailable
                }) { IsLoaded = true };
                var wireBound = (FileContentResult)JsonResponse(Envelope(hasMore ? new string('a', 4096) : null, null));
                remaining = result.Result.RemainingBudget;
                if (wireBound.FileContents.Length > remaining.OutputBytes)
                    return Failure(413, "CQL_FEDERATION_OUTPUT_LIMIT", "Canonical output and the bounded public cursor envelope exceed the cumulative budget.");
                var postWork = publicCopies + rows.Length + 1;
                const int finalizationReserveMilliseconds = 100;
                var spentMilliseconds = checked((int)Math.Ceiling(active.Elapsed.TotalMilliseconds)
                    + finalizationReserveMilliseconds);
                if (postWork > remaining.Work)
                    return Failure(413, "CQL_FEDERATION_WORK_LIMIT", "Public projection exceeded the remaining work budget.");
                if (spentMilliseconds >= originalActiveMilliseconds)
                    return Failure(504, "CQL_FEDERATION_DEADLINE", "Public projection exceeded the remaining active deadline.");
                var tail = CosmicFederationRemainingBudget.Restore(options.Result, remaining.InputRows, remaining.InputBytes,
                    remaining.OutputBytes - wireBound.FileContents.Length, remaining.Work - postWork, remaining.Attempts,
                    Math.Min(remaining.DurationMilliseconds, originalActiveMilliseconds - spentMilliseconds), remaining.ProviderRows);
                if (tail.IsError) return Failure(503, "CQL_CURSOR_ACCOUNTING_INVALID", "Final continuation accounting is inconsistent.");
                var finalizationStarted = active.Elapsed;
                string token = null, unavailable = null;
                if (hasMore)
                {
                    if (tail.Result.ValidateReadSet(options.Result, selectedProviders, deadline.Token).IsError)
                        unavailable = "BudgetExhausted";
                    else
                    {
                        if (cursorHost.UtcNow >= expires)
                            return Failure(400, "CQL_CURSOR_INVALID", "The fixed continuation lifetime expired.");
                        var state = new CqlFederationCursorState(actor, intent, cursorHost.Deployment, lineage, nextKey,
                            issued, expires, nextOffset, selectedProviders, tail.Result.InputRows,
                            selectedProviders.Select(value => tail.Result.ProviderRows[value]).ToArray(),
                            checked((int)tail.Result.InputBytes), checked((int)tail.Result.OutputBytes), checked((int)tail.Result.Work),
                            tail.Result.Attempts, tail.Result.DurationMilliseconds);
                        var protectedState = cursorHost.Codec.Protect(state);
                        if (protectedState.IsError) return Failure(503, "CQL_CURSOR_PROTECTION_FAILED", "The next page could not be protected.");
                        token = protectedState.Result;
                    }
                }
                var pageResponse = (FileContentResult)JsonResponse(Envelope(token, unavailable));
                if (pageResponse.FileContents.Length > wireBound.FileContents.Length
                    || pageResponse.FileContents.Length > request.MaximumOutputBytes)
                    return Failure(413, "CQL_FEDERATION_OUTPUT_LIMIT", "The public page exceeded its reserved wire bound.");
                if ((active.Elapsed - finalizationStarted).TotalMilliseconds > finalizationReserveMilliseconds)
                    return Failure(504, "CQL_FEDERATION_DEADLINE", "Cursor finalization exceeded its reserved active-time allowance.");
                if (cursorHost.UtcNow >= expires)
                    return Failure(400, "CQL_CURSOR_INVALID", "The fixed continuation lifetime expired before publication.");
                deadline.Token.ThrowIfCancellationRequested();
                return pageResponse;
            }
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

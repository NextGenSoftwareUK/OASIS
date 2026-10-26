using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Explicit host composition for protected federation state. The supplied key owner must
/// provide the deployment's intended persistence, sharing and rotation. No key store is created.</summary>
public sealed class CqlFederationCursorHost
{
    internal CqlFederationCursorCodec Codec { get; }
    internal string Deployment { get; }
    private readonly TimeProvider clock;
    internal DateTimeOffset UtcNow => clock.GetUtcNow();

    /// <param name="keys">Host-owned persistent/shared Data Protection provider.</param>
    /// <param name="deploymentContractSha256">Lowercase SHA256 identifying the host's provider
    /// deployments, connections and policy contract. Never pass connection strings or credentials.</param>
    /// <param name="clock">Host clock, or system UTC when omitted.</param>
    public CqlFederationCursorHost(IDataProtectionProvider keys, string deploymentContractSha256,
        TimeProvider clock = null)
    {
        if (deploymentContractSha256 == null || deploymentContractSha256.Length != 64
            || deploymentContractSha256.Any(character => !(character is >= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new ArgumentException("A lowercase SHA256 deployment contract identity is required.", nameof(deploymentContractSha256));
        Codec = new CqlFederationCursorCodec(keys);
        this.clock = clock ?? TimeProvider.System;
        Deployment = Digest(new { Version = "cql-federation-host-v1", Contract = deploymentContractSha256,
            Host = typeof(CqlFederationCursorHost).Module.ModuleVersionId,
            Core = typeof(CosmicFederationExecutor).Module.ModuleVersionId });
    }

    internal OASISResult<CqlFederationCursorState> Read(string token, Guid actor)
        => Codec.Unprotect(token, actor, Deployment, UtcNow);

    // Header equality is not admission evidence. The handle must belong to the same
    // WEB4/avatar/key tuple already claimed by the durable execution protocol.
    internal OASISResult<bool> BindRequest(HttpContext http, Guid actor, CqlFederationCursorState state)
    {
        if (http == null || !http.Request.Headers.TryGetValue("Idempotency-Key", out var keys) || keys.Count != 1
            || !Guid.TryParseExact(keys[0], "D", out var key) || keys[0] != key.ToString("D") || key == Guid.Empty
            || state != null && (state.Actor != actor || state.NextRequestKey != key)
            || !http.Items.TryGetValue(Web4UsageMeasurement.ExecutionKey, out var value)
            || value is not UsageExecutionHandle handle || !handle.IsBoundToRequest("WEB4", actor, key.ToString("D")))
            return Error<bool>("CQL_CURSOR_REQUEST_BINDING", "The exact continuation request key must belong to this durable admission.");
        return new OASISResult<bool>(true);
    }

    internal OASISResult<string> BindIntent(CqlFederationV1Request request, CosmicFederationPlan<Holon> plan,
        IReadOnlyList<ICosmicQueryStorageProvider> adapters)
    {
        try
        {
            if (request == null || request.Source == null || request.Providers == null || request.Providers.Length > 3
                || request.Providers.Any(value => string.IsNullOrEmpty(value) || value.Length > 40)
                || request.Scope is not ("CurrentDefault" or "Named" or "AllEligible")
                || adapters == null || adapters.Count != plan?.Targets.Count || adapters.Count is < 1 or > 3)
                throw new ArgumentException("A complete bounded resolved intent is required.");
            var core = CosmicFederationIntentFingerprint.Create(plan);
            if (core.IsError) return Error<string>(core.ErrorCode!, core.Message);
            var identities = new object[adapters.Count];
            for (var index = 0; index < adapters.Count; index++)
            {
                var adapter = adapters[index] ?? throw new ArgumentException("A target adapter is missing.");
                if (adapter.QueryProviderType != plan.Targets[index].Targets.Single().Provider)
                    throw new ArgumentException("Adapter scope/order does not match the captured plan.");
                var type = adapter.GetType();
                identities[index] = new { Provider = adapter.QueryProviderType.ToString(),
                    Type = type.AssemblyQualifiedName, Artifact = type.Module.ModuleVersionId };
            }
            return new OASISResult<string>(Digest(new { Version = "cql-federation-http-intent-v1",
                Deployment, Core = core.Result, request.Scope, request.Providers,
                IncludeMetadata = request.Source?.IncludeMetadata, Adapters = identities }));
        }
        catch (Exception)
        { return Error<string>("CQL_CURSOR_INTENT_INVALID", "The complete continuation intent could not be bound."); }
    }

    internal OASISResult<CosmicFederationRemainingBudget> Restore(CqlFederationCursorState state,
        CosmicFederationOptions originalOptions)
    {
        var now = UtcNow;
        if (state == null || state.Deployment != Deployment || now < state.Issued || now >= state.Expires)
            return Error<CosmicFederationRemainingBudget>("CQL_CURSOR_INVALID", "Protected state is missing or expired.");
        return CosmicFederationRemainingBudget.Restore(originalOptions, state.InputRows, state.InputBytes,
            state.OutputBytes, state.Work, state.Attempts, state.DurationMilliseconds,
            state.Providers.Select((provider, index) => (provider, rows: state.ProviderRows[index]))
                .ToDictionary(pair => pair.provider, pair => pair.rows));
    }

    private static string Digest<T>(T value)
        => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))).ToLowerInvariant();
    private static OASISResult<T> Error<T>(string code, string message)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
}

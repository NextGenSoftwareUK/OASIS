using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>
/// Application-owned authorization, evaluated once per admitted request. Return
/// a complete AuthoritativeRecords policy for this authenticated actor/provider.
/// Request tenant IDs, metadata hints and caller roles are not authorization.
/// Any storage used by an implementation must follow the host's admission rules.
/// </summary>
public interface ICqlReadAccessPolicy
{
    OASISResult<CosmicQueryReadAccess> Authorize(Guid authenticatedActor, ProviderType authority);
}

public static partial class CqlV1Endpoint
{
    private sealed class ReadPolicyRejected(int status, string code, Exception cause = null) : Exception(code, cause)
    {
        public int Status { get; } = status;
        public string Code { get; } = code;
    }

    // Stack-owned observation: provider selection and authorization must never
    // be repeated after a mutable default/catalogue changes during compilation.
    private sealed record ReadPolicyObservation(IOASISStorageProvider Storage,
        ICosmicQueryStorageProvider Adapter, ProviderType Selected, CosmicQueryReadAccess Access);

    private static ReadPolicyObservation CaptureReadPolicy(HttpContext http, Guid actor,
        ProviderType? requested, ICqlV1Providers providers, PreparedStorageExecution admission, bool includeCloudReadProfile=false)
    {
        var resolved = http.RequestServices?.GetService(typeof(ICqlReadAccessPolicy));
        var keyed = false;
        if (resolved == null)
        {
            var registered = http.RequestServices?.GetService(typeof(IServiceProviderIsService)) as IServiceProviderIsService;
            if (registered?.IsService(typeof(ICqlReadAccessPolicy)) == true)
                throw new ReadPolicyRejected(503, "CQL_ACCESS_POLICY_INVALID");
            if ((http.RequestServices?.GetService(typeof(CqlReadPolicyRequirement)) as CqlReadPolicyRequirement)?.Required == true)
                throw new ReadPolicyRejected(503, "CQL_ACCESS_POLICY_REQUIRED");
            // Keyed policies belong to a concrete provider, including within a
            // federated request. Unenrolled hosts keep their existing contract.
            var keyedServices = http.RequestServices?.GetService<IServiceProviderIsKeyedService>();
            keyed = keyedServices != null && CqlV1Compiler.ReadProfiles.Any(provider =>
                keyedServices.IsKeyedService(typeof(ICqlReadAccessPolicy), provider));
            if (!keyed) return null;
        }
        if (!keyed && resolved is not ICqlReadAccessPolicy)
            throw new ReadPolicyRejected(503, "CQL_ACCESS_POLICY_INVALID");

        if (!TryResolveEligible(providers, requested, admission, out var storage, out var adapter, out var selected,
            includeCloudReadProfile:includeCloudReadProfile))
            throw new ReadPolicyRejected(403, "CQL_PROVIDER_NOT_ELIGIBLE");

        if (keyed)
        {
            resolved = http.RequestServices.GetKeyedService<ICqlReadAccessPolicy>(selected);
            if (resolved == null)
            {
                if (http.RequestServices.GetRequiredService<IServiceProviderIsKeyedService>()
                    .IsKeyedService(typeof(ICqlReadAccessPolicy), selected))
                    throw new ReadPolicyRejected(503, "CQL_ACCESS_POLICY_INVALID");
                // Freeze the resolved target once even when its visibility rule
                // remains ordinary owner/public. No grant crosses provider scope.
                var ordinary = CosmicQueryReadAccess.OwnerOrPublic(actor);
                if (ordinary.IsError) throw new ReadPolicyRejected(503, "CQL_ACCESS_POLICY_INVALID");
                return new ReadPolicyObservation(storage, adapter, selected, ordinary.Result);
            }
        }
        var service = (ICqlReadAccessPolicy)resolved;

        OASISResult<CosmicQueryReadAccess> authorization;
        try { authorization = service.Authorize(actor, selected); }
        catch (Exception cause) { throw new ReadPolicyRejected(503, "CQL_ACCESS_POLICY_FAILED", cause); }
        if (authorization?.IsError == true)
            throw new ReadPolicyRejected(403, "CQL_ACCESS_FORBIDDEN", authorization.Exception);
        if (authorization == null || authorization.IsWarning || authorization.Result == null
            || authorization.Result.ActorId != actor || authorization.Result.Authority != selected)
            throw new ReadPolicyRejected(503, "CQL_ACCESS_POLICY_INVALID");

        return new ReadPolicyObservation(storage, adapter, selected, authorization.Result);
    }
}

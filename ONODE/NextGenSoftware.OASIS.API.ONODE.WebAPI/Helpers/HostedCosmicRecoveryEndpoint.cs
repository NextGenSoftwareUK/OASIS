using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cosmic;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

internal static class HostedCosmicRecoveryEndpoint
{
    internal static async Task<OASISResult<ConditionalHolonWriteOutcome>> RecoverAsync(HttpContext context,
        string engineMode, Guid operation, string requestedAuthority, Func<HolonManager> acquireManager)
    {
        var actor = (context?.Items["Avatar"] as IAvatar)?.Id ?? Guid.Empty;
        if (actor == Guid.Empty) return Error("COSMIC_RECOVERY_ACTOR_REQUIRED", "An authenticated actor is required.");
        if (engineMode != HyperDriveModes.V2) return Error("COSMIC_RECOVERY_V2_REQUIRED", "Recovery requires HyperDrive V2.");
        if (operation == Guid.Empty || !Enum.TryParse(requestedAuthority, true, out ProviderType authority)
            || !Enum.IsDefined(typeof(ProviderType), authority) || authority is ProviderType.Default or ProviderType.None or ProviderType.All
            || !string.Equals(requestedAuthority, authority.ToString(), StringComparison.OrdinalIgnoreCase))
            return Error("COSMIC_RECOVERY_INPUT_INVALID", "An original operation ID and explicit named authority are required.");
        try
        {
            var routes = context.RequestServices?.GetService(typeof(StorageAdmissionRoutes)) as StorageAdmissionRoutes;
            var admission = PreparedStorageExecution.Current;
            if (routes?.Resolve(context) == null || admission == null || !admission.IsForAvatar(actor))
                return Error("COSMIC_RECOVERY_ADMISSION_REQUIRED", "Recovery requires an enrolled priced route admitted for this actor.");
            var policy = context.RequestServices.GetService(typeof(ICosmicOperationRecoveryPolicy)) as ICosmicOperationRecoveryPolicy;
            if (policy == null) return Error("COSMIC_RECOVERY_POLICY_REQUIRED", "The host must configure authorized recovery protection and runtime contracts.");
            var authorized = policy.Authorize(actor, authority);
            if (authorized == null) return Error("COSMIC_RECOVERY_POLICY_REQUIRED", "The host returned no authorization result.");
            if (authorized.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<CosmicOperationRecoveryGrant, ConditionalHolonWriteOutcome>(authorized);
            var grant = authorized.Result;
            if (grant == null || grant.ActorId != actor || grant.Authority != authority)
                return Error("COSMIC_RECOVERY_POLICY_BINDING_INVALID", "The host grant does not match this actor and authority.");
            var manager = acquireManager?.Invoke();
            if (manager == null) return Error("COSMIC_RECOVERY_MANAGER_REQUIRED", "A configured recovery manager is required.");
            var scoped = manager.CreateRequestContext(new ProviderRoutingOverrides(), authority);
            if (scoped.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<HolonRequestContext, ConditionalHolonWriteOutcome>(scoped);
            return await scoped.Result.Manager.RecoverConditionalHolonWriteOutcomeAsync(operation, actor, authority,
                grant.AuthorizeRecord, grant.AdmitStoredType).ConfigureAwait(false);
        }
        catch (Exception cause)
        {
            var result = Error("COSMIC_RECOVERY_UNVERIFIED", "The original operation outcome could not be verified.");
            result.Exception = cause;
            return result;
        }
    }

    internal static HttpStatusCode Status(OASISResult<ConditionalHolonWriteOutcome> result) => !result.IsError ? HttpStatusCode.OK
        : result.ErrorCode switch
        {
            "COSMIC_RECOVERY_ACTOR_REQUIRED" => HttpStatusCode.Unauthorized,
            "COSMIC_RECOVERY_INPUT_INVALID" => HttpStatusCode.BadRequest,
            "COSMIC_RECOVERY_FORBIDDEN" or "COSMIC_PREPARATION_ACTOR_MISMATCH" => HttpStatusCode.Forbidden,
            "COSMIC_PREPARATION_NOT_FOUND" => HttpStatusCode.NotFound,
            _ => HttpStatusCode.ServiceUnavailable
        };

    private static OASISResult<ConditionalHolonWriteOutcome> Error(string code, string message)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
}

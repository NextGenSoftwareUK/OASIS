using System;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cosmic;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

/// <summary>Apply a trusted application grant only to a new request-owned
/// manager. Never assign actor-specific policy to a shared manager.</summary>
internal static class HostedCosmicProtection
{
    internal static OASISResult<HolonManager> Enroll(HolonManager manager, Guid actor, ref ProviderType provider,
        ICosmicOperationRecoveryPolicy policy)
    {
        if (policy == null) return new(manager);
        try
        {
            var scoped = manager.CreateRequestContext(new ProviderRoutingOverrides(), provider);
            if (scoped.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<HolonRequestContext, HolonManager>(scoped);
            var authority = scoped.Result.Policy.PreferredProvider;
            var authorized = policy.Authorize(actor, authority);
            if (authorized == null) return Error("COSMIC_WRITE_PROTECTION_REQUIRED", "The host returned no write protection authorization.");
            if (authorized.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<CosmicOperationRecoveryGrant, HolonManager>(authorized);
            var grant = authorized.Result;
            if (grant == null || grant.ActorId != actor || grant.Authority != authority)
                return Error("COSMIC_WRITE_PROTECTION_BINDING_INVALID", "The protection grant differs from the authenticated actor and pinned authority.");
            scoped.Result.Manager.PreparationPolicyResolver = selected =>
            {
                var type = selected.RuntimeType;
                if (grant.AdmitStoredType(type.FullName, type.Assembly.GetName().Name) != type)
                    throw new InvalidOperationException("The host has not admitted this holon's recovery contract.");
                return grant.AuthorizeRecord(selected.RecordId);
            };
            provider = authority;
            return new(scoped.Result.Manager);
        }
        catch (Exception cause)
        {
            var result = Error("COSMIC_WRITE_PROTECTION_UNVERIFIED", "The host could not authorize write protection.");
            result.Exception = cause;
            return result;
        }
    }

    private static OASISResult<HolonManager> Error(string code, string message)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
}

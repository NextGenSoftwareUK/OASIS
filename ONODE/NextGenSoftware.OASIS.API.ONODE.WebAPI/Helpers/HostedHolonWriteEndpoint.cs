using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.Common;
using SaveHolonRequest = NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.SaveHolonRequest;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

/// <summary>V2 non-controller transports use the same authenticated conditional
/// save boundary as REST. Manager acquisition is lazy and follows validation.</summary>
internal static class HostedHolonWriteEndpoint
{
    internal static Task<OASISResult<IHolon>> SaveAsync(HttpContext context,
        string engineMode, SaveHolonRequest request, Func<HolonManager> acquireManager)
        => ExecuteAsync(context, engineMode, request, acquireManager,
            (manager, actor, wizard, header) => HolonWriteEndpoint.SaveAsync(manager, request, actor, wizard, header));

    internal static Task<OASISResult<IHolon>> DeleteAsync(HttpContext context,
        string engineMode, DeleteHolonRequest request, Func<HolonManager> acquireManager)
    {
        var bound = HolonWriteEndpoint.BindDeleteRevision(request,
            context?.Request.Headers[HolonWriteEndpoint.ExpectedRevisionHeader].ToString());
        return bound.IsError ? Task.FromResult(bound)
            : ExecuteAsync(context, engineMode, request, acquireManager,
                (manager, actor, wizard, header) => HolonWriteEndpoint.DeleteAsync(manager, request, actor, wizard, header));
    }

    private static async Task<OASISResult<IHolon>> ExecuteAsync(HttpContext context,
        string engineMode, IHolonWriteIntentRequest request, Func<HolonManager> acquireManager,
        Func<HolonManager, Guid, bool, string, Task<OASISResult<IHolon>>> operation)
    {
        var avatar = context?.Items["Avatar"] as IAvatar;
        var actor = avatar?.Id ?? Guid.Empty;
        var wizard = avatar?.AvatarType?.Value == AvatarType.Wizard;
        var operationHeader = context?.Request.Headers[HolonWriteEndpoint.OperationHeader].ToString();
        var intent = HolonWriteEndpoint.ValidateIntent<IHolon>(request, actor, operationHeader,
            context?.RequestServices?.GetService(typeof(StorageAdmissionRoutes)) != null);
        if (intent.IsError) return intent;
        if (engineMode != HyperDriveModes.V2)
            return new OASISResult<IHolon> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_OWNED_WRITE_V2_REQUIRED", Message = "This hosted boundary requires HyperDrive V2." };

        var manager = acquireManager();
        if (manager == null)
            return new OASISResult<IHolon> { IsError = true, ErrorCount = 1,
                ErrorCode = "COSMIC_WRITE_MANAGER_REQUIRED", Message = "A configured write manager is required." };
        var provider = string.IsNullOrWhiteSpace(request.ProviderType)
            ? ProviderType.Default
            : Enum.Parse<ProviderType>(request.ProviderType, true);
        var routing = new ProviderRoutingOverrides();
        if (request.SetGlobally)
        {
            var published = ProviderManager.Instance.PublishRoutingOverrides(routing, provider);
            if (published.IsError)
                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ProviderRoutingPolicy, IHolon>(published);
            routing = published.Result.ToOverrides();
        }
        var scoped = manager.CreateRequestContext(routing, provider);
        if (scoped.IsError)
            return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<HolonRequestContext, IHolon>(scoped);

        return await operation(scoped.Result.Manager, actor, wizard, operationHeader);
    }
}

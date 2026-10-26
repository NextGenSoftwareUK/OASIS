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
    internal static async Task<OASISResult<IHolon>> SaveAsync(HttpContext context,
        string engineMode, SaveHolonRequest request, Func<HolonManager> acquireManager)
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
        var scoped = manager.CreateRequestContext(new ProviderRoutingOverrides(), provider);
        if (scoped.IsError)
            return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<HolonRequestContext, IHolon>(scoped);

        return await HolonWriteEndpoint.SaveAsync(scoped.Result.Manager, request, actor, wizard, operationHeader);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly IList<AvatarType> _avatarTypes;

    public AuthorizeAttribute(params AvatarType[] avatarTypes)
    {
        _avatarTypes = avatarTypes ?? new AvatarType[] { };
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var avatar = (Avatar)context.HttpContext.Items["Avatar"];

        // Not authenticated at all -> 401 and tell them how to log in.
        if (avatar == null)
        {
            context.Result = new JsonResult(new OASISResult<bool>(false)
            {
                IsError = true,
                Message = "Unauthorized. Try Logging In First With api/avatar/authenticate REST API Route.",
                DetailedMessage = "No authenticated avatar was found on the request. Send a valid JWT in the Authorization: Bearer header."
            })
            { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        // Authenticated but the wrong avatar type -> 403, NOT "try logging in". Telling a correctly
        // authenticated caller to log in (as load-all-web3-nfts did for non-Wizards) is misleading, and the
        // response previously carried HTTP 200 because no StatusCode was ever set on the JsonResult.
        if (_avatarTypes.Any() && !_avatarTypes.Contains(avatar.AvatarType.Value))
        {
            string requiredTypes = string.Join(", ", _avatarTypes);

            context.Result = new JsonResult(new OASISResult<bool>(false)
            {
                IsError = true,
                Message = $"Forbidden. This operation requires an avatar of type: {requiredTypes}. Your avatar is of type {avatar.AvatarType.Value}.",
                DetailedMessage = "You are authenticated correctly, but your avatar type is not permitted to call this endpoint. No re-authentication will help; the avatar needs the required type/role."
            })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
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

        if (avatar == null || (_avatarTypes.Any() && !_avatarTypes.Contains(avatar.AvatarType.Value)))
        {
            // not logged in or role not authorized
            //context.Result = new JsonResult(new { message = "Unauthorized. Try Logging In First With api/avatar/auth REST API Route." }) { StatusCode = StatusCodes.Status401Unauthorized };
            context.Result = new JsonResult(new OASISResult<bool>(false) { IsError = true, Message = "Unauthorized. Try Logging In First With api/avatar/authenticate REST API Route." });            
        }
    }
}
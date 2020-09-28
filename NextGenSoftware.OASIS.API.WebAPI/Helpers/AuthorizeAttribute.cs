using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Linq;

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
        //var avatar = (IAvatar)context.HttpContext.Items["Avatar"];
        var avatar = (NextGenSoftware.OASIS.API.Core.Avatar)context.HttpContext.Items["Avatar"];

        if (avatar == null || (_avatarTypes.Any() && !_avatarTypes.Contains(avatar.AvatarType)))
        {
            // not logged in or role not authorized
            context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
        }
    }
}
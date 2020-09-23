using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.ONODE.WebAPI;
using System;
using System.Collections.Generic;
using System.Linq;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly IList<AvatarType> _roles;

    public AuthorizeAttribute(params AvatarType[] roles)
    {
        _roles = roles ?? new AvatarType[] { };
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var avatar = (IAvatar)context.HttpContext.Items["Avatar"];

        if (avatar == null || (_roles.Any() && !_roles.Contains(avatar.AvatarType)))
        {
            // not logged in or role not authorized
            context.Result = new JsonResult(new { message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
        }
    }
}
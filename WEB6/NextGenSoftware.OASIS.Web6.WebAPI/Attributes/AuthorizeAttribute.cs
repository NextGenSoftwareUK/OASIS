using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Accept either a fully-loaded avatar (Avatar DB available) or just a validated avatar ID
            // from the JWT claims (AI-only deployments where the avatar DB is not configured).
            var avatar   = context.HttpContext.Items["Avatar"] as IAvatar;
            var avatarId = context.HttpContext.Items["AvatarId"] is Guid id && id != Guid.Empty ? id : Guid.Empty;

            if (avatar == null && avatarId == Guid.Empty)
            {
                context.Result = new JsonResult(new { message = "Unauthorized" })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }
        }
    }
}

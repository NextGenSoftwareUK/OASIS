using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace NextGenSoftware.OASIS.Web9.WebAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // JWT middleware supplies the validated identity; WEB4 remains the subscriber authority.
            // A full local avatar projection is not required by these stateless API endpoints.
            if (context.HttpContext.User.Identity?.IsAuthenticated != true ||
                context.HttpContext.Items["AvatarId"] is not Guid avatarId || avatarId == Guid.Empty ||
                !Guid.TryParse(context.HttpContext.User.FindFirst("id")?.Value, out var claimId) || claimId != avatarId)
            {
                context.Result = new JsonResult(new { message = "Unauthorized" })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }
        }
    }
}

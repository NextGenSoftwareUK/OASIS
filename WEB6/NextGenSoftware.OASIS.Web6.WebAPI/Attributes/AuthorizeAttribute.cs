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
            // Accept a pre-shared service API key (X-Web6-Api-Key header) for server-to-server calls
            // (e.g. Vercel serverless functions calling the Web6 AI layer directly).
            string apiKey = Environment.GetEnvironmentVariable("WEB6_API_KEY");
            if (!string.IsNullOrEmpty(apiKey))
            {
                string incomingKey = context.HttpContext.Request.Headers["X-Web6-Api-Key"].ToString();
                if (incomingKey == apiKey)
                    return; // authenticated via API key
            }

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

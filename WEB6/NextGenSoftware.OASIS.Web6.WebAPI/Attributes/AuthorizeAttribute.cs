using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Attributes
{
    /// <summary>
    /// Custom authorization filter that accepts either a pre-shared API key or a valid OASIS JWT.
    /// Replaces the default ASP.NET Core [Authorize] attribute because OASIS uses a custom
    /// JwtMiddleware rather than the built-in Bearer scheme.
    ///
    /// AUTH FLOW (2026-07-05 — fixed during Web6 / Leela chatbot debugging):
    ///
    /// 1. API KEY (checked first — for server-to-server calls, e.g. Vercel → Web6)
    ///    If the WEB6_API_KEY environment variable is set AND the incoming X-Web6-Api-Key header
    ///    matches, the request is authenticated immediately. No JWT processing is needed.
    ///    This path was added as a parallel auth mechanism so Vercel serverless functions
    ///    could call Web6 without a user-bound JWT. It must be checked FIRST so that a request
    ///    carrying both a valid API key and an expired/malformed JWT is not rejected by JWT logic.
    ///    (Ordering matters: JwtMiddleware runs before this filter, and previously a bad JWT caused
    ///    a direct 401 write that bypassed the API key check entirely — that bug has been fixed in
    ///    JwtMiddleware by changing the catch to be silent.)
    ///
    /// 2. JWT (checked second — for user-bound requests from the browser)
    ///    JwtMiddleware (Program.cs) validates the Bearer token and sets either:
    ///      - context.Items["Avatar"]   — full IAvatar loaded from the storage provider (ideal), OR
    ///      - context.Items["AvatarId"] — just the Guid from the JWT claims (fallback when DB is
    ///        unavailable or not configured on this deployment).
    ///    Either is sufficient to authenticate. Pure AI routing doesn't need the full avatar object;
    ///    the ID is enough for karma attribution and routing metadata.
    ///
    /// 3. REJECTION
    ///    If neither check passes, a 401 JSON response is returned. The middleware pipeline continues
    ///    normally (JwtMiddleware already called _next before this filter runs).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // 1. Accept a pre-shared service API key for server-to-server calls (e.g. Vercel → Web6).
            //    Checked FIRST so a valid key is never blocked by an expired user JWT (see note 1 above).
            string apiKey = Environment.GetEnvironmentVariable("WEB6_API_KEY");
            if (!string.IsNullOrEmpty(apiKey))
            {
                string incomingKey = context.HttpContext.Request.Headers["X-Web6-Api-Key"].ToString();
                if (incomingKey == apiKey)
                    return; // authenticated via API key
            }

            // 2. Accept either a fully-loaded avatar (DB available) or just the validated avatar ID
            //    from JWT claims (AI-only deployments where the avatar DB is not configured).
            var avatar   = context.HttpContext.Items["Avatar"] as IAvatar;
            var avatarId = context.HttpContext.Items["AvatarId"] is Guid id && id != Guid.Empty ? id : Guid.Empty;

            if (avatar == null && avatarId == Guid.Empty)
            {
                // 3. Both paths failed — reject.
                context.Result = new JsonResult(new { message = "Unauthorized" })
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };
            }
        }
    }
}

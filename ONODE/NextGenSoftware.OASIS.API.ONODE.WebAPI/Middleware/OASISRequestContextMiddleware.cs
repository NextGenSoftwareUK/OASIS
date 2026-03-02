using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware
{
    /// <summary>
    /// Clears request-scoped OASISRequestContext at the end of each request.
    /// Must run first in the pipeline so its finally runs after all other middleware (safe for multiple clients).
    /// </summary>
    public class OASISRequestContextMiddleware
    {
        private readonly RequestDelegate _next;

        public OASISRequestContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            finally
            {
                OASISRequestContext.CurrentAvatar = null;
                OASISRequestContext.CurrentAvatarId = null;
            }
        }
    }
}

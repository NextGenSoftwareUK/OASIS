using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Hubs;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware
{
    /// <summary>Routes <c>/ws/messaging</c> WebSocket upgrades to <see cref="MessagingRealtimeHub"/>.</summary>
    public sealed class MessagingWebSocketMiddleware
    {
        private readonly RequestDelegate _next;

        public MessagingWebSocketMiddleware(RequestDelegate next) => _next = next;

        public Task InvokeAsync(HttpContext context) =>
            context.Request.Path.StartsWithSegments("/ws/messaging") && context.WebSockets.IsWebSocketRequest
                ? MessagingRealtimeHub.HandleAsync(context)
                : _next(context);
    }
}

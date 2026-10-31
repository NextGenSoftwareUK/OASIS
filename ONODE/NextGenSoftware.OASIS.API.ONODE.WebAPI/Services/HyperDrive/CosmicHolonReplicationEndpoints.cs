using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;

public static class CosmicHolonReplicationEndpoints
{
    public const string ReadinessPath="/health/cosmic/ready";
    public static IEndpointConventionBuilder MapCosmicHolonReplicationReadiness(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        return endpoints.MapGet(ReadinessPath,async context=>{
            context.Response.Headers.CacheControl="no-store";
            var health=context.RequestServices.GetService<HealthCheckService>();
            if(health==null){await Write(context,"not-configured",false);return;}
            var report=await health.CheckHealthAsync(registration=>registration.Name=="cosmic-holon-replication",context.RequestAborted).ConfigureAwait(false);
            // An empty aggregate is Healthy in the framework; it is not proof that COSMIC has been configured.
            if(!report.Entries.TryGetValue("cosmic-holon-replication",out var entry))
            {await Write(context,"not-configured",false);return;}
            var ready=entry.Status==HealthStatus.Healthy;
            await Write(context,ready ? "ready":"not-ready",ready);
        }).AllowAnonymous();
    }
    private static Task Write(HttpContext context,string value,bool ready)
    {
        context.Response.StatusCode=ready ? StatusCodes.Status200OK:StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType="text/plain; charset=utf-8";
        return context.Response.WriteAsync(value,context.RequestAborted);
    }
}

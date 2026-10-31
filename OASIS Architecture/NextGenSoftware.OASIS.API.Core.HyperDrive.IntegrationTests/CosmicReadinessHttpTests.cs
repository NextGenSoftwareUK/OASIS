using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class CosmicReadinessHttpTests
{
    [Theory]
    [InlineData("absent",HttpStatusCode.ServiceUnavailable,"not-configured")]
    [InlineData("empty",HttpStatusCode.ServiceUnavailable,"not-configured")]
    [InlineData("other",HttpStatusCode.ServiceUnavailable,"not-configured")]
    [InlineData("unhealthy",HttpStatusCode.ServiceUnavailable,"not-ready")]
    [InlineData("healthy",HttpStatusCode.OK,"ready")]
    public async Task EndpointRequiresTheNamedCosmicCheckAndNeverExposesDiagnostics(string mode,HttpStatusCode expected,string body)
    {
        var builder=WebApplication.CreateBuilder(new WebApplicationOptions{EnvironmentName="Development"});
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        if(mode!="absent")
        {
            var checks=builder.Services.AddHealthChecks();
            if(mode=="other")checks.AddCheck("unrelated",()=>HealthCheckResult.Healthy());
            if(mode=="healthy")checks.AddCheck("cosmic-holon-replication",()=>HealthCheckResult.Healthy("private diagnostic",new Dictionary<string,object>{{"payload","must not escape"}}));
            if(mode=="unhealthy")checks.AddCheck("cosmic-holon-replication",()=>HealthCheckResult.Unhealthy("private diagnostic",new Exception("credential must not escape")));
        }
        await using var app=builder.Build();app.MapCosmicHolonReplicationReadiness();await app.StartAsync();
        try
        {
            var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client=new HttpClient{BaseAddress=new Uri(address)};
            var response=await client.GetAsync(CosmicHolonReplicationEndpoints.ReadinessPath);
            Assert.Equal(expected,response.StatusCode);Assert.Equal(body,await response.Content.ReadAsStringAsync());
            Assert.True(response.Headers.CacheControl?.NoStore);Assert.Equal("text/plain",response.Content.Headers.ContentType!.MediaType);
        }
        finally{using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(5));await app.StopAsync(stop.Token);}
    }
}

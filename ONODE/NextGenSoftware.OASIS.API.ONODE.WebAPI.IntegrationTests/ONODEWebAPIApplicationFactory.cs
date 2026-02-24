using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests;

/// <summary>
/// WebApplicationFactory that runs the ONODE WebAPI in the "Testing" environment
/// so Startup skips HTTPS redirection (test server is HTTP-only).
/// </summary>
public class ONODEWebAPIApplicationFactory : WebApplicationFactory<Startup>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}

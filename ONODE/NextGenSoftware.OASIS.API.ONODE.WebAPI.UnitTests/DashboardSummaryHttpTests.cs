using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class DashboardSummaryHttpTests
{
    [Fact]
    public async Task DashboardSummaryRoutesRejectAnonymousHttpRequests()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(FilesController).Assembly);

        await using var app = builder.Build();
        app.MapControllers();
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        foreach (var route in new[]
        {
            "/api/files/get-file-count-for-current-logged-in-avatar",
            "/api/wallet/current-avatar/summary"
        })
        {
            using var response = await client.GetAsync(route);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(body.RootElement.GetProperty("isError").GetBoolean());
        }
    }
}

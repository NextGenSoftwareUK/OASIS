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
    public Task DashboardSummaryRoutesRejectAnonymousHttpRequests() => VerifyUnauthorizedAsync(
        "/api/files/get-file-count-for-current-logged-in-avatar",
        "/api/wallet/current-avatar/summary",
        "/api/avatar/inventory/count");

    [Fact]
    public Task DashboardMessageSummaryRejectsAnonymousHttpRequests() =>
        VerifyUnauthorizedAsync("/api/messaging/current-avatar/summary");

    [Fact]
    public Task DashboardGiftSummaryRejectsAnonymousHttpRequests() =>
        VerifyUnauthorizedAsync("/api/gifts/my-gifts/summary", "/api/gifts/history", "/api/gifts/stats");

    private static async Task VerifyUnauthorizedAsync(params string[] routes)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(FilesController).Assembly);

        await using var app = builder.Build();
        app.MapControllers();
        await app.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        foreach (var route in routes)
        {
            using var response = await client.GetAsync(route);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(body.RootElement.GetProperty("isError").GetBoolean());
        }
    }
}

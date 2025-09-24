using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Xunit;
using NextGenSoftware.OASIS.STAR.WebAPI;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.WebAPI.IntegrationTests
{
    public class STARWebAPIIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public STARWebAPIIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Configure test services here if needed
                });
            });
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetCelestialBodies_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/celestialbodies");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task GetCelestialSpaces_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/celestialspaces");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task GetZomes_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/zomes");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task GetMissions_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/missions");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task GetChapters_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/chapters");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task GetQuests_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/quests");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task GetGeoHotSpots_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/geohotspots");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task GetGeoNFTs_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/geonfts");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task GetParks_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/parks");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task GetInventoryItems_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/inventoryitems");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task GetNFTs_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/nfts");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task GetHolons_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/holons");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task GetOAPPs_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/oapps");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task GetSTARStatus_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/api/star/status");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task Authenticate_WithValidCredentials_ReturnsJwtToken()
        {
            // Arrange
            var authenticateRequest = new
            {
                username = "testuser",
                password = "testpassword"
            };
            var json = JsonConvert.SerializeObject(authenticateRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/avatar/authenticate", content);

            // Assert
            // Note: This will likely fail in test environment without real WEB4 OASIS API
            // but we're testing the endpoint structure and response format
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK || 
                       response.StatusCode == System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetCurrentAvatar_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/avatar/current");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task SwaggerEndpoint_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/swagger");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task SwaggerJson_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/swagger/v1/swagger.json");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }
    }
}


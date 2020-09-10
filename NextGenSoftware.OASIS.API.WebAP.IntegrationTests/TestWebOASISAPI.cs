using Microsoft.AspNetCore.TestHost;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests
{
    public class TestWebAPI
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public TestWebAPI()
        {
            // Arrange
            //_server = new TestServer(new WebHostBuilder()
            //   .UseStartup<Startup>());

            _client = _server.CreateClient();
        }

        [Fact]
        public async Task ReturnHelloWorld()
        {
            // Act
            var response = await _client.GetAsync("/");
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            // Assert
            Assert.Equal("Hello World!", responseString);
        }
    }
}

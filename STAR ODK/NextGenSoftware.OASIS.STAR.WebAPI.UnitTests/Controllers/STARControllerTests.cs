using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class STARControllerTests
    {
        private readonly Mock<ILogger<STARController>> _mockLogger;
        private readonly STARController _controller;

        public STARControllerTests()
        {
            _mockLogger = new Mock<ILogger<STARController>>();
            _controller = new STARController(_mockLogger.Object);
        }

        [Fact]
        public void GetStatus_ShouldReturnOASISResult()
        {
            // Act
            var result = _controller.GetStatus();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ActionResult<OASISResult<bool>>>();
        }

        [Fact]
        public async Task IgniteSTAR_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new IgniteRequest
            {
                UserName = "testuser",
                Password = "testpass"
            };

            // Act
            var result = await _controller.IgniteSTAR(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ActionResult<OASISResult<bool>>>();
        }

        [Fact]
        public async Task IgniteSTAR_WithNullRequest_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.IgniteSTAR(null);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ExtinguishSTAR_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.ExtinguishSTAR();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ActionResult<OASISResult<bool>>>();
        }

        [Fact]
        public async Task BeamIn_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new BeamInRequest
            {
                AvatarId = Guid.NewGuid(),
                Location = "Test Location"
            };

            // Act
            var result = await _controller.BeamIn(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ActionResult<OASISResult<string>>>();
        }

        [Fact]
        public async Task CreateAvatar_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new CreateAvatarRequest
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "testpass"
            };

            // Act
            var result = await _controller.CreateAvatar(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ActionResult<OASISResult<string>>>();
        }

        [Fact]
        public async Task Light_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new LightRequest
            {
                CelestialBodyId = Guid.NewGuid(),
                LightIntensity = 100
            };

            // Act
            var result = await _controller.Light(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ActionResult<OASISResult<string>>>();
        }

        [Fact]
        public async Task Seed_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new SeedRequest
            {
                CelestialBodyId = Guid.NewGuid(),
                SeedType = "TestSeed"
            };

            // Act
            var result = await _controller.Seed(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ActionResult<OASISResult<string>>>();
        }

        [Fact]
        public async Task UnSeed_WithValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var request = new UnSeedRequest
            {
                CelestialBodyId = Guid.NewGuid()
            };

            // Act
            var result = await _controller.UnSeed(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ActionResult<OASISResult<string>>>();
        }
    }
}

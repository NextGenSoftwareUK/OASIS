using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class CelestialBodiesControllerTests
    {
        private readonly CelestialBodiesController _controller;

        public CelestialBodiesControllerTests()
        {
            _controller = new CelestialBodiesController();
            STARControllerTestHelper.SetUpControllerContext(_controller);
        }

        [Fact]
        public async Task GetAllCelestialBodies_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllCelestialBodies();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task GetCelestialBody_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetCelestialBody(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task CreateCelestialBody_WithValidBody_ShouldReturnOASISResult()
        {
            // Arrange
            var body = new STARCelestialBody { Id = Guid.NewGuid(), Name = "Test Celestial Body" };

            // Act
            var result = await _controller.CreateCelestialBody(body);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task UpdateCelestialBody_WithValidIdAndBody_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var body = new STARCelestialBody { Id = id, Name = "Updated Celestial Body" };

            // Act
            var result = await _controller.UpdateCelestialBody(id, body);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task DeleteCelestialBody_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteCelestialBody(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task GetCelestialBodiesByType_WithValidType_ShouldReturnOASISResult()
        {
            // Arrange
            var type = "Planet";

            // Act
            var result = await _controller.GetCelestialBodiesByType(type);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task GetCelestialBodiesInSpace_WithValidSpaceId_ShouldReturnOASISResult()
        {
            // Arrange
            var spaceId = Guid.NewGuid();

            // Act
            var result = await _controller.GetCelestialBodiesInSpace(spaceId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class CelestialBodiesControllerTests
    {
        private readonly Mock<ILogger<CelestialBodiesController>> _mockLogger;
        private readonly CelestialBodiesController _controller;

        public CelestialBodiesControllerTests()
        {
            _mockLogger = new Mock<ILogger<CelestialBodiesController>>();
            _controller = new CelestialBodiesController();
        }

        [Fact]
        public async Task GetAllCelestialBodies_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllCelestialBodies();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
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
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task CreateCelestialBody_WithValidBody_ShouldReturnOASISResult()
        {
            // Arrange
            var mockBody = new Mock<ICelestialBody>();
            mockBody.Setup(x => x.Id).Returns(Guid.NewGuid());
            mockBody.Setup(x => x.Name).Returns("Test Celestial Body");

            // Act
            var result = await _controller.CreateCelestialBody(mockBody.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task UpdateCelestialBody_WithValidIdAndBody_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var mockBody = new Mock<ICelestialBody>();
            mockBody.Setup(x => x.Id).Returns(id);
            mockBody.Setup(x => x.Name).Returns("Updated Celestial Body");

            // Act
            var result = await _controller.UpdateCelestialBody(id, mockBody.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
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
            result.Should().BeOfType<IActionResult>();
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
            result.Should().BeOfType<IActionResult>();
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
            result.Should().BeOfType<IActionResult>();
        }
    }
}

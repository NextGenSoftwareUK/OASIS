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
    public class CelestialSpacesControllerTests
    {
        private readonly Mock<ILogger<CelestialSpacesController>> _mockLogger;
        private readonly CelestialSpacesController _controller;

        public CelestialSpacesControllerTests()
        {
            _mockLogger = new Mock<ILogger<CelestialSpacesController>>();
            _controller = new CelestialSpacesController();
        }

        [Fact]
        public async Task GetAllCelestialSpaces_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllCelestialSpaces();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task GetCelestialSpace_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetCelestialSpace(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task CreateCelestialSpace_WithValidSpace_ShouldReturnOASISResult()
        {
            // Arrange
            var mockSpace = new Mock<ICelestialSpace>();
            mockSpace.Setup(x => x.Id).Returns(Guid.NewGuid());
            mockSpace.Setup(x => x.Name).Returns("Test Celestial Space");

            // Act
            var result = await _controller.CreateCelestialSpace(mockSpace.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task UpdateCelestialSpace_WithValidIdAndSpace_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var mockSpace = new Mock<ICelestialSpace>();
            mockSpace.Setup(x => x.Id).Returns(id);
            mockSpace.Setup(x => x.Name).Returns("Updated Celestial Space");

            // Act
            var result = await _controller.UpdateCelestialSpace(id, mockSpace.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task DeleteCelestialSpace_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteCelestialSpace(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }
    }
}

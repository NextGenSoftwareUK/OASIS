using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Tests.Unit.Controllers
{
    public class GeoHotSpotsControllerTests
    {
        private readonly Mock<ILogger<GeoHotSpotsController>> _mockLogger;
        private readonly GeoHotSpotsController _controller;

        public GeoHotSpotsControllerTests()
        {
            _mockLogger = new Mock<ILogger<GeoHotSpotsController>>();
            _controller = new GeoHotSpotsController();
        }

        [Fact]
        public async Task GetAllGeoHotSpots_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllGeoHotSpots();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task GetGeoHotSpot_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetGeoHotSpot(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task CreateGeoHotSpot_WithValidGeoHotSpot_ShouldReturnOASISResult()
        {
            // Arrange
            var mockGeoHotSpot = new Mock<IGeoHotSpot>();
            mockGeoHotSpot.Setup(x => x.Id).Returns(Guid.NewGuid());
            mockGeoHotSpot.Setup(x => x.Name).Returns("Test GeoHotSpot");

            // Act
            var result = await _controller.CreateGeoHotSpot(mockGeoHotSpot.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task UpdateGeoHotSpot_WithValidIdAndGeoHotSpot_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var mockGeoHotSpot = new Mock<IGeoHotSpot>();
            mockGeoHotSpot.Setup(x => x.Id).Returns(id);
            mockGeoHotSpot.Setup(x => x.Name).Returns("Updated GeoHotSpot");

            // Act
            var result = await _controller.UpdateGeoHotSpot(id, mockGeoHotSpot.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task DeleteGeoHotSpot_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteGeoHotSpot(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }
    }
}

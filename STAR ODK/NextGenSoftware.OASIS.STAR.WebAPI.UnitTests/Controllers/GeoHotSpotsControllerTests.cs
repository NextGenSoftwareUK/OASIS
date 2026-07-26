using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class GeoHotSpotsControllerTests
    {
        private readonly GeoHotSpotsController _controller;

        public GeoHotSpotsControllerTests()
        {
            _controller = new GeoHotSpotsController();
            STARControllerTestHelper.SetUpControllerContext(_controller);
        }

        [Fact]
        public async Task GetAllGeoHotSpots_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllGeoHotSpots();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
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
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task CreateGeoHotSpot_WithValidGeoHotSpot_ShouldReturnOASISResult()
        {
            // Arrange
            var hotSpot = new GeoHotSpot { Id = Guid.NewGuid(), Name = "Test GeoHotSpot" };

            // Act
            var result = await _controller.CreateGeoHotSpot(hotSpot);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task UpdateGeoHotSpot_WithValidIdAndGeoHotSpot_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var hotSpot = new GeoHotSpot { Id = id, Name = "Updated GeoHotSpot" };

            // Act
            var result = await _controller.UpdateGeoHotSpot(id, hotSpot);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
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
            result.Should().BeAssignableTo<IActionResult>();
        }
    }
}

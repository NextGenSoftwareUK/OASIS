using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class GeoNFTsControllerTests
    {
        private readonly GeoNFTsController _controller;

        public GeoNFTsControllerTests()
        {
            _controller = new GeoNFTsController();
        }

        [Fact]
        public async Task GetAllGeoNFTs_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllGeoNFTs();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task GetGeoNFT_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetGeoNFT(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task CreateGeoNFT_WithValidGeoNFT_ShouldReturnOASISResult()
        {
            // Arrange
            var geoNFT = new STARGeoNFT { Id = Guid.NewGuid(), Name = "Test GeoNFT" };

            // Act
            var result = await _controller.CreateGeoNFT(geoNFT);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task UpdateGeoNFT_WithValidIdAndGeoNFT_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var geoNFT = new STARGeoNFT { Id = id, Name = "Updated GeoNFT" };

            // Act
            var result = await _controller.UpdateGeoNFT(id, geoNFT);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task DeleteGeoNFT_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteGeoNFT(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }
    }
}

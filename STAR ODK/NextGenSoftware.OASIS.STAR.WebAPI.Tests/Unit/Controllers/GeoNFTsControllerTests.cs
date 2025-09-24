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
    public class GeoNFTsControllerTests
    {
        private readonly Mock<ILogger<GeoNFTsController>> _mockLogger;
        private readonly GeoNFTsController _controller;

        public GeoNFTsControllerTests()
        {
            _mockLogger = new Mock<ILogger<GeoNFTsController>>();
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
            var mockGeoNFT = new Mock<ISTARGeoNFT>();
            mockGeoNFT.Setup(x => x.Id).Returns(Guid.NewGuid());
            mockGeoNFT.Setup(x => x.Name).Returns("Test GeoNFT");

            // Act
            var result = await _controller.CreateGeoNFT(mockGeoNFT.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task UpdateGeoNFT_WithValidIdAndGeoNFT_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var mockGeoNFT = new Mock<ISTARGeoNFT>();
            mockGeoNFT.Setup(x => x.Id).Returns(id);
            mockGeoNFT.Setup(x => x.Name).Returns("Updated GeoNFT");

            // Act
            var result = await _controller.UpdateGeoNFT(id, mockGeoNFT.Object);

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

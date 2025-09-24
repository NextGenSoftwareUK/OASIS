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
    public class NFTsControllerTests
    {
        private readonly Mock<ILogger<NFTsController>> _mockLogger;
        private readonly NFTsController _controller;

        public NFTsControllerTests()
        {
            _mockLogger = new Mock<ILogger<NFTsController>>();
            _controller = new NFTsController();
        }

        [Fact]
        public async Task GetAllNFTs_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllNFTs();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task GetNFT_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetNFT(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task CreateNFT_WithValidNFT_ShouldReturnOASISResult()
        {
            // Arrange
            var mockNFT = new Mock<INFT>();
            mockNFT.Setup(x => x.Id).Returns(Guid.NewGuid());
            mockNFT.Setup(x => x.Name).Returns("Test NFT");

            // Act
            var result = await _controller.CreateNFT(mockNFT.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task UpdateNFT_WithValidIdAndNFT_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var mockNFT = new Mock<INFT>();
            mockNFT.Setup(x => x.Id).Returns(id);
            mockNFT.Setup(x => x.Name).Returns("Updated NFT");

            // Act
            var result = await _controller.UpdateNFT(id, mockNFT.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task DeleteNFT_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteNFT(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }
    }
}

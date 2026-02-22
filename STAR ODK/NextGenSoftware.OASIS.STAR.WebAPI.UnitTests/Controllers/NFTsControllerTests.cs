using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class NFTsControllerTests
    {
        private readonly NFTsController _controller;

        public NFTsControllerTests()
        {
            _controller = new NFTsController();
            STARControllerTestHelper.SetUpControllerContext(_controller);
        }

        [Fact]
        public async Task GetAllNFTs_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllNFTs();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
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
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task CreateNFT_WithValidNFT_ShouldReturnOASISResult()
        {
            // Arrange
            var nft = new STARNFT { Id = Guid.NewGuid(), Name = "Test NFT" };

            // Act
            var result = await _controller.CreateNFT(nft);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
        }

        [Fact]
        public async Task UpdateNFT_WithValidIdAndNFT_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var nft = new STARNFT { Id = id, Name = "Updated NFT" };

            // Act
            var result = await _controller.UpdateNFT(id, nft);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IActionResult>();
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
            result.Should().BeAssignableTo<IActionResult>();
        }
    }
}

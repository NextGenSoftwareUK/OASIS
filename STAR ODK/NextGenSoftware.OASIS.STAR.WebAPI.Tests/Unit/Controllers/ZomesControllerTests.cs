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
    public class ZomesControllerTests
    {
        private readonly Mock<ILogger<ZomesController>> _mockLogger;
        private readonly ZomesController _controller;

        public ZomesControllerTests()
        {
            _mockLogger = new Mock<ILogger<ZomesController>>();
            _controller = new ZomesController();
        }

        [Fact]
        public async Task GetAllZomes_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllZomes();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task GetZome_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetZome(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task CreateZome_WithValidZome_ShouldReturnOASISResult()
        {
            // Arrange
            var mockZome = new Mock<IZome>();
            mockZome.Setup(x => x.Id).Returns(Guid.NewGuid());
            mockZome.Setup(x => x.Name).Returns("Test Zome");

            // Act
            var result = await _controller.CreateZome(mockZome.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task UpdateZome_WithValidIdAndZome_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var mockZome = new Mock<IZome>();
            mockZome.Setup(x => x.Id).Returns(id);
            mockZome.Setup(x => x.Name).Returns("Updated Zome");

            // Act
            var result = await _controller.UpdateZome(id, mockZome.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task DeleteZome_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteZome(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Controllers;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class HolonsControllerTests
    {
        private readonly Mock<ILogger<HolonsController>> _mockLogger;
        private readonly HolonsController _controller;

        public HolonsControllerTests()
        {
            _mockLogger = new Mock<ILogger<HolonsController>>();
            _controller = new HolonsController();
        }

        [Fact]
        public async Task GetAllHolons_ShouldReturnOASISResult()
        {
            // Act
            var result = await _controller.GetAllHolons();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task GetHolon_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.GetHolon(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task CreateHolon_WithValidHolon_ShouldReturnOASISResult()
        {
            // Arrange
            var mockHolon = new Mock<IHolon>();
            mockHolon.Setup(x => x.Id).Returns(Guid.NewGuid());
            mockHolon.Setup(x => x.Name).Returns("Test Holon");

            // Act
            var result = await _controller.CreateHolon(mockHolon.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task UpdateHolon_WithValidIdAndHolon_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();
            var mockHolon = new Mock<IHolon>();
            mockHolon.Setup(x => x.Id).Returns(id);
            mockHolon.Setup(x => x.Name).Returns("Updated Holon");

            // Act
            var result = await _controller.UpdateHolon(id, mockHolon.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }

        [Fact]
        public async Task DeleteHolon_WithValidId_ShouldReturnOASISResult()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteHolon(id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<IActionResult>();
        }
    }
}
